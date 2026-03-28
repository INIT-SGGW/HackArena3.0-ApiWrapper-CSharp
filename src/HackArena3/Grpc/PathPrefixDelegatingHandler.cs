using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.HackArena3.Grpc;

internal class PathPrefixDelegatingHandler : DelegatingHandler
{
    private readonly string _prefix;

    public PathPrefixDelegatingHandler(string prefix, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _prefix = "/" + prefix.Trim('/');
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null)
        {
            var originalPath = request.RequestUri.AbsolutePath;

            var newUri = new UriBuilder(request.RequestUri)
            {
                Path = _prefix + originalPath
            }.Uri;

            request.RequestUri = newUri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
