using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace src.HackArena3.Grpc;

internal static class GrpcChannelFactory
{
    /// <summary>
    /// Tworzy kanał gRPC skonfigurowany do komunikacji z usługą Broker,
    /// która znajduje się za proxy routującym po ścieżce "/broker".
    /// </summary>
    public static GrpcChannel CreateBrokerChannel(string apiAddress)
    {
        var handler = new PathPrefixDelegatingHandler("broker", new SocketsHttpHandler
        {
            // Wyłączamy proxy systemowe, tak jak w kodzie w Pythonie
            UseProxy = false,
            Proxy = null
        });

        return GrpcChannel.ForAddress(apiAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }

    /// <summary>
    /// Tworzy kanał gRPC skonfigurowany do komunikacji z usługą GameToken,
    /// która znajduje się za proxy routującym po ścieżce "/gametoken".
    /// </summary>
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

    /// <summary>
    /// Tworzy standardowy, niezabezpieczony kanał do bezpośredniej komunikacji z backendem.
    /// </summary>
    public static GrpcChannel CreateInsecureBackendChannel(string backendTarget)
    {
        // Ta opcja jest potrzebna do zezwolenia na HTTP w .NET Core 3.1+
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        return GrpcChannel.ForAddress(backendTarget);
    }
}
