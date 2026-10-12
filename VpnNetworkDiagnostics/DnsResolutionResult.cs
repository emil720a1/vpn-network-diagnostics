using System.Net;

namespace VpnNetworkDiagnostics;

public sealed record DnsResolutionResult(
    IPAddress[] Addresses,
    long ElapsedMilliseconds);