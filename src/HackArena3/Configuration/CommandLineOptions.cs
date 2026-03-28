using CommandLine;

namespace Hackarena3.Wrapper.Configuration;

internal class CommandLineOptions
{
    [Option("sandbox_id", Required = false, HelpText = "ID sandboxa do dołączenia (tryb deweloperski).", SetName = "sandbox")]
    public string? SandboxId { get; set; }
    [Option("official", Required = false, HelpText = "Uruchamia bota w trybie oficjalnym (turniejowym).", SetName = "official")]
    public bool Official { get; set; }
}