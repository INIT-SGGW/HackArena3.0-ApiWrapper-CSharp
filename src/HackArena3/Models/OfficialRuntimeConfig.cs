using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.HackArena3.Models;

internal record OfficialRuntimeConfig
{
    public required string GrpcTarget { get; init; }

    public required string RpcPrefix { get; init; }

    public required string TeamToken { get; init; }

    public required string AuthToken { get; init; }
}
