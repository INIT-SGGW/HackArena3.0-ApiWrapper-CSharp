using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace src.HackArena3.Grpc;

internal static class GrpcChannelFactory
{
    public static GrpcChannel CreateBrokerChannel(string apiAddress)
    {
        var handler = new PathPrefixDelegatingHandler("broker", new SocketsHttpHandler
        {
            UseProxy = false,
            Proxy = null
        });

        return GrpcChannel.ForAddress(apiAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }

    public static GrpcChannel CreateGameTokenChannel(string apiAddress)
    {
        var handler = new PathPrefixDelegatingHandler("gametoken", new SocketsHttpHandler
        {
            UseProxy = false,
            Proxy = null
        });

        return GrpcChannel.ForAddress(apiAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }

    public static GrpcChannel CreateInsecureBackendChannel(string backendTarget)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        return GrpcChannel.ForAddress(backendTarget);
    }

    public static GrpcChannel CreateOfficialChannel(string grpcTarget, string rpcPrefix)
    {
        var handler = new PathPrefixDelegatingHandler(rpcPrefix, new SocketsHttpHandler
        {
            UseProxy = false,
            Proxy = null
        });

        var baseAddress = new Uri($"https://{grpcTarget}");

        return GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }
}
