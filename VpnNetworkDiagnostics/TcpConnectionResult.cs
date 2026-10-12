using System.Net;
using System.Net.Sockets;

namespace VpnNetworkDiagnostics;

public sealed record TcpConnectionResult(
    TcpClient Client,
    IPAddress SelectedAddress,
    long ElapsedMilliseconds);