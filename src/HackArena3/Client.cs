using CommandLine;
using Hackarena3.Wrapper.Configuration;
using src.HackArena3.Auth;
using src.HackArena3.Interfaces;
using src.HackArena3.Models;
using src.HackArena3.Runtime;
using src.HackArena3.Services;

namespace src.HackArena3;

public static class Client
{
    public static async Task<int> RunBot(IBot bot, RuntimeConfig config)
    {
        Console.WriteLine("[ha3-wrapper] Bot starting with configuration:");
        Console.WriteLine($"  API Address: {config.ApiAddr}");
        Console.WriteLine($"  Auth Binary: {(config.HaAuthBin ?? "Not set, will search")}");
        Console.WriteLine($"  Sandbox ID:  {(config.SandboxId ?? "Not set, will prompt")}");

        GameTokenProvider? tokenProvider = null;
        try
        {
            var jwtProvider = new MemberJwtProvider(config.HaAuthBin);
            Console.WriteLine("[ha3-wrapper] Fetching member JWT...");
            var memberJwt = await jwtProvider.FetchMemberJwtAsync();
            Console.WriteLine("[ha3-wrapper] Successfully fetched member JWT.");

            // Krok 2: Odkryj i wybierz sandbox
            var discoverer = new SandboxDiscoverer(config, memberJwt);
            var selectedSandbox = await discoverer.DiscoverAndChooseSandboxAsync();
            Console.WriteLine($"[ha3-wrapper] Selected sandbox: {selectedSandbox.SandboxName} ({selectedSandbox.SandboxId})");

            // Krok 2: Utwórz dostawcę tokenu gry i pobierz pierwszy token
            tokenProvider = new GameTokenProvider(config.ApiAddr, memberJwt);
            Console.WriteLine("[ha3-wrapper] Fetching initial game token...");
            await tokenProvider.RefreshAsync();
            Console.WriteLine("[ha3-wrapper] Successfully fetched initial game token.");
            // Console.WriteLine($"  Game Token: {tokenProvider.CurrentToken?.Token.Substring(0, 15)}...");

            var gameLoop = new GameLoop(bot, selectedSandbox, tokenProvider);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n[ha3-wrapper] Cancellation requested...");
                cts.Cancel();
                e.Cancel = true; // Zapobiegaj natychmiastowemu zamknięciu aplikacji
            };

            await gameLoop.RunAsync(cts.Token);

            Console.WriteLine("[ha3-wrapper] Game loop finished.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[ha3-wrapper] Bot execution cancelled by user.");
            return 130; // Kod wyjścia dla przerwania przez użytkownika
        }
        catch (GameTokenException ex) // Dodajemy obsługę błędów tokenu gry
        {
            Console.Error.WriteLine($"[ha3-wrapper] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ha3-wrapper] Runtime error: {ex.Message}");
            return 1;
        }
        finally
        {
            // `await using` nie jest tutaj dostępne, więc robimy to ręcznie
            if (tokenProvider != null)
            {
                await tokenProvider.DisposeAsync();
            }
        }
    }
}
