using CommandLine;

namespace Hackarena3.Wrapper.Configuration;

internal class CommandLineOptions
{
    [Option("sandbox_id", Required = false, HelpText = "ID sandboxa, do którego należy dołączyć, nadpisuje inne konfiguracje.")]
    public string? SandboxId { get; set; }
}