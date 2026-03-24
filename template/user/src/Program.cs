using CommandLine;
using Hackarena3.Wrapper.Configuration;
using src.HackArena3;
using src.HackArena3.Auth;
using template.user.src.bot;

// Używamy `async Task<int>` aby móc używać `await` na najwyższym poziomie
return await MainAsync(args);

async Task<int> MainAsync(string[] args)
{
    try
    {
        string? cliSandboxId = null;
        Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(options =>
            {
                if (!string.IsNullOrWhiteSpace(options.SandboxId))
                {
                    cliSandboxId = options.SandboxId.Trim();
                }
                else if (options.SandboxId != null)
                {
                    throw new ConfigException("Empty value for --sandbox_id.");
                }
            });

        var runtimeConfig = ConfigLoader.LoadConfigurationFromEnvironment();

        if (cliSandboxId != null)
        {
            runtimeConfig = runtimeConfig with { SandboxId = cliSandboxId };
        }

        var bot = new Bot();
        // Przekazujemy `runtimeConfig` do `RunBot`
        return await Client.RunBot(bot, runtimeConfig);
    }
    catch (ConfigException ex)
    {
        Console.Error.WriteLine($"[ha3-wrapper] {ex.Message}");
        return 1;
    }
    catch (AuthException ex) // Dodajemy obsługę błędów autoryzacji
    {
        Console.Error.WriteLine($"[ha3-wrapper] {ex.Message}");
        return 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ha3-wrapper] Unexpected error: {ex.Message}");
        return 1;
    }
}