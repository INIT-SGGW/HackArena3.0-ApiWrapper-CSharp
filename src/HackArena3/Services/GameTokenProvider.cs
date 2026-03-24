using Grpc.Core;
using Grpc.Net.Client;
using HA3.Proto.Auth.V1;
using src.HackArena3.Grpc;
using src.HackArena3.Models;

namespace src.HackArena3.Services;

/// <summary>
/// Odpowiedzialny za pozyskiwanie i odświeżanie tokenów gry (GameToken) z API.
/// Implementuje IAsyncDisposable do poprawnego zarządzania kanałem gRPC.
/// </summary>
internal sealed class GameTokenProvider : IAsyncDisposable
{
    private const int RpcTimeoutSeconds = 10;
    private readonly string _memberJwt;
    private readonly GrpcChannel _channel;
    private readonly GameTokenIssuerService.GameTokenIssuerServiceClient _client;
    private bool _requestInfoLogged = false;

    public GameToken? CurrentToken { get; private set; }

    public GameTokenProvider(string apiAddr, string memberJwt)
    {
        if (string.IsNullOrWhiteSpace(memberJwt))
        {
            throw new GameTokenException("member_jwt is empty; cannot request game token.");
        }
        _memberJwt = memberJwt;

        _channel = GrpcChannelFactory.CreateGameTokenChannel(apiAddr);

        _client = new GameTokenIssuerService.GameTokenIssuerServiceClient(_channel);
    }

    /// <summary>
    /// Wymusza odświeżenie tokenu gry, wysyłając żądanie do API.
    /// </summary>
    /// <returns>Nowo pozyskany token gry.</returns>
    public async Task<GameToken> RefreshAsync(CancellationToken cancellationToken = default)
    {
        CurrentToken = await RequestNewGameTokenAsync(cancellationToken);
        return CurrentToken;
    }

    private async Task<GameToken> RequestNewGameTokenAsync(CancellationToken cancellationToken)
    {
        var request = new IssueGameTokenRequest
        {
            TokenType = GameTokenIssueType.TeamBotDev
        };

        var metadata = new Metadata
        {
            { "authorization", $"Bearer {_memberJwt}" },
            { "cookie", $"auth_token={_memberJwt}" }
        };

        if (!_requestInfoLogged)
        {
            // Logujemy teraz adres bazowy, a nie cel kanału, co jest bardziej intuicyjne
            Console.Error.WriteLine($"[ha3-wrapper] Requesting game token via gRPC: base_address={_channel.Target}, path_prefix=/gametoken");
            _requestInfoLogged = true;
        }

        try
        {
            var response = await _client.IssueGameTokenAsync(
                request,
                headers: metadata,
                deadline: DateTime.UtcNow.AddSeconds(RpcTimeoutSeconds),
                cancellationToken: cancellationToken
            );

            var tokenPayload = response.Token;
            if (string.IsNullOrWhiteSpace(tokenPayload?.Jwt))
            {
                throw new GameTokenException("Game token gRPC response has empty `token.jwt`.");
            }
            if (tokenPayload.ExpUtc is null || tokenPayload.ExpUtc.Seconds <= 0)
            {
                throw new GameTokenException("Game token response is missing a valid token.exp_utc timestamp.");
            }

            return new GameToken
            {
                Token = tokenPayload.Jwt.Trim(),
                ExpirationEpoch = tokenPayload.ExpUtc.Seconds,
                Kid = string.IsNullOrWhiteSpace(tokenPayload.Kid) ? null : tokenPayload.Kid.Trim()
            };
        }
        catch (RpcException ex)
        {
            if (ex.StatusCode == StatusCode.Unimplemented)
            {
                throw new GameTokenException("Game token service unavailable (UNIMPLEMENTED).", ex);
            }
            throw new GameTokenException(
                $"Game token gRPC request failed: code={ex.StatusCode}; details={ex.Status.Detail ?? "no details"}", ex
            );
        }
    }

    /// <summary>
    /// Zapewnia, że token jest świeży. Jeśli zbliża się jego wygaśnięcie, odświeża go.
    /// </summary>
    /// <param name="refreshSkewSeconds">Liczba sekund przed wygaśnięciem, kiedy należy odświeżyć token.</param>
    /// <returns>True, jeśli token został odświeżony, w przeciwnym razie false.</returns>
    public async Task<bool> EnsureFreshAsync(int refreshSkewSeconds = 30)
    {
        if (CurrentToken == null)
        {
            await RefreshAsync();
            return true;
        }

        var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (nowEpoch >= CurrentToken.ExpirationEpoch - refreshSkewSeconds)
        {
            var previousTokenValue = CurrentToken.Token;
            var refreshedToken = await RefreshAsync();
            return refreshedToken.Token != previousTokenValue;
        }

        return false;
    }

    /// <summary>
    /// Bezpiecznie zamyka kanał gRPC.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _channel.ShutdownAsync();
        _channel.Dispose();
    }
}