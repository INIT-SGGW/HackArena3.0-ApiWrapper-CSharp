using CommandLine;
using Hackarena3.Wrapper.Configuration;
using src.HackArena3.Interfaces;
using src.HackArena3.Runtime;
using src.HackArena3.Services;

namespace src.HackArena3;

public static class Client
{
    public static async Task<int> RunBot(IBot bot, string[] args)
    {
        try
        {
            var orchestrator = new RuntimeOrchestrator(bot);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { cts.Cancel(); e.Cancel = true; };

            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(async options =>
                {
                    if (options.Official)
                    {
                        var officialConfig = ConfigLoader.LoadOfficialConfiguration();
                        await orchestrator.RunOfficialModeAsync(officialConfig, cts.Token);
                    }
                    else
                    {
                        var runtimeConfig = ConfigLoader.LoadConfigurationFromEnvironment();
                        if (options.SandboxId != null)
                        {
                            runtimeConfig = runtimeConfig with { SandboxId = options.SandboxId };
                        }
                        await orchestrator.RunSandboxModeAsync(runtimeConfig, cts.Token);
                    }
                });

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("\n[ha3-wrapper] Bot execution cancelled by user.");
            return 130;
        }
        catch (GameTokenException ex)
        {
            Console.Error.WriteLine($"[ha3-wrapper] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ha3-wrapper] Runtime error: {ex.Message}");
            return 1;
        }
    }
}
