param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$Version
)

$ErrorActionPreference = "Stop"

$DefaultExcludes = @(
    "*__pycache__/*",
    "*.pyc",
    "*.pyo",
    "*.pyd",
    ".DS_Store",
    "*/bin/*",
	"bin/*",
    "*/obj/*",
	"obj/*",
	"*/Properties/*",
	"Properties/*",
    "*.user",
    "*.suo"
)

function Get-VersionFromXmlFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    try {
        [xml]$xml = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    }
    catch {
        return $null
    }

    foreach ($tag in @("Version", "PackageVersion")) {
        $node = $xml.SelectSingleNode("//Project/PropertyGroup/$tag")
        if ($null -ne $node -and -not [string]::IsNullOrWhiteSpace($node.InnerText)) {
            return $node.InnerText.Trim()
        }
    }

    return $null
}

function Resolve-Version {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [string]$ExplicitVersion
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitVersion)) {
        return $ExplicitVersion.Trim()
    }

    $propsPath = Join-Path $Root "Directory.Build.props"
    $fromProps = Get-VersionFromXmlFile -Path $propsPath
    if (-not [string]::IsNullOrWhiteSpace($fromProps)) {
        return $fromProps
    }

    $projectFile = Get-ChildItem -Path $Root -Recurse -File -Filter "*.csproj" |
        Sort-Object FullName |
        Select-Object -First 1
    if ($null -ne $projectFile) {
        $fromProject = Get-VersionFromXmlFile -Path $projectFile.FullName
        if (-not [string]::IsNullOrWhiteSpace($fromProject)) {
            return $fromProject
        }
    }

    throw "Cannot resolve version from Directory.Build.props or any .csproj. Pass -Version explicitly."
}

function Render-ManifestWithVersion {
    param(
        [Parameter(Mandatory = $true)][string]$ManifestPath,
        [Parameter(Mandatory = $true)][string]$TemplateVersion
    )

    $content = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8
    $pattern = '(?m)^template_version\s*=\s*"[^"]*"\s*$'
    $matches = [regex]::Matches($content, $pattern).Count
    if ($matches -ne 1) {
        throw "Cannot update template_version in template/system/manifest.toml"
    }

    return [regex]::Replace(
        $content,
        $pattern,
        "template_version = `"$TemplateVersion`"",
        1
    )
}

function Should-Skip {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string[]]$ExcludePatterns
    )

    $lower = $RelativePath.ToLowerInvariant()
    foreach ($pattern in $ExcludePatterns) {
        if ($RelativePath -like $pattern -or $lower -like $pattern.ToLowerInvariant()) {
            return $true
        }
    }
    return $false
}

$resolvedRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$templateRoot = Join-Path $resolvedRoot "template"
$manifestRel = "system/manifest.toml"
$manifestPath = Join-Path $templateRoot $manifestRel

if (-not (Test-Path -LiteralPath $templateRoot -PathType Container)) {
    throw "Missing template/ directory."
}
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Missing template/system/manifest.toml."
}

$resolvedVersion = Resolve-Version -Root $resolvedRoot -ExplicitVersion $Version
$manifestRendered = Render-ManifestWithVersion -ManifestPath $manifestPath -TemplateVersion $resolvedVersion

$outputDir = Join-Path $resolvedRoot "dist"
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$outputZip = Join-Path $outputDir ("wrapper-csharp-v{0}.zip" -f $resolvedVersion)
if (Test-Path -LiteralPath $outputZip -PathType Leaf) {
    Remove-Item -LiteralPath $outputZip -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$added = 0
$zip = [System.IO.Compression.ZipFile]::Open(
    $outputZip,
    [System.IO.Compression.ZipArchiveMode]::Create
)
try {
    $files = Get-ChildItem -Path $templateRoot -Recurse -File | Sort-Object FullName
    foreach ($file in $files) {
        $rel = $file.FullName.Substring($templateRoot.Length).TrimStart('\', '/').Replace('\', '/')
        if (Should-Skip -RelativePath $rel -ExcludePatterns $DefaultExcludes) {
            continue
        }

        if ($rel -eq $manifestRel.Replace('\', '/')) {
            $entry = $zip.CreateEntry($rel, [System.IO.Compression.CompressionLevel]::Optimal)
            $stream = $entry.Open()
            try {
                $encoding = New-Object System.Text.UTF8Encoding($false)
                $writer = New-Object System.IO.StreamWriter($stream, $encoding)
                try {
                    $writer.Write($manifestRendered)
                }
                finally {
                    $writer.Dispose()
                }
            }
            finally {
                $stream.Dispose()
            }
        }
        else {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $file.FullName,
                $rel,
                [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
        $added++
    }
}
finally {
    $zip.Dispose()
}

if ($added -eq 0) {
    throw "Template archive is empty."
}

Write-Host "Created template archive: $outputZip"
Write-Host "Files in archive: $added"
