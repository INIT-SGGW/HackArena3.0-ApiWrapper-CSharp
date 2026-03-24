using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.HackArena3.Grpc;

/// <summary>
/// DelegatingHandler, który dodaje stały prefiks do ścieżki każdego wychodzącego żądania HTTP.
/// Niezbędny do pracy z proxy (np. Envoy), które routują wywołania gRPC na podstawie ścieżki URL.
/// </summary>
public class PathPrefixDelegatingHandler : DelegatingHandler
{
    private readonly string _prefix;

    public PathPrefixDelegatingHandler(string prefix, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        // Upewnij się, że prefiks zaczyna się od '/' i nie kończy się na '/'
        _prefix = "/" + prefix.Trim('/');
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null)
        {
            var originalPath = request.RequestUri.AbsolutePath;

            // Budujemy nowy URI z dodanym prefiksem
            var newUri = new UriBuilder(request.RequestUri)
            {
                Path = _prefix + originalPath
            }.Uri;

            request.RequestUri = newUri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
