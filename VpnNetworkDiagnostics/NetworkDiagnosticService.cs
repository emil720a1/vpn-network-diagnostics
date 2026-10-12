using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace VpnNetworkDiagnostics;

public class NetworkDiagnosticService
{
    public async Task<DiagnosticReport> RunAsync(
        string hostname,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException(
                "Hostname cannot be empty.",
                nameof(hostname));
        }

        DnsResolutionResult dnsResult =
            await ResolveAddressesAsync(
                hostname,
                cancellationToken);

        if (dnsResult.Addresses.Length == 0)
        {
            throw new InvalidOperationException(
                "No IP addresses were resolved.");
        }

        Console.WriteLine("DNS resolution succeeded. Addresses found:");

        foreach (IPAddress address in dnsResult.Addresses)
        {
            Console.WriteLine($"- {address}");
        }

        TcpConnectionResult tcpResult =
            await ConnectTcpAsync(
                dnsResult.Addresses,
                cancellationToken);

        Console.WriteLine(
            $"TCP connection succeeded: {tcpResult.SelectedAddress}");

        tcpResult.Client.Dispose();

        throw new NotImplementedException();
    }

    private async Task<DnsResolutionResult> ResolveAddressesAsync(
        string hostname,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource dnsTimeoutCts =
            new(TimeSpan.FromSeconds(5));

        using CancellationTokenSource linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                dnsTimeoutCts.Token);

        Stopwatch stopwatch = Stopwatch.StartNew();

        IPAddress[] addresses =
            await Dns.GetHostAddressesAsync(
                hostname,
                linkedCts.Token);

        stopwatch.Stop();

        return new DnsResolutionResult(
            addresses,
            stopwatch.ElapsedMilliseconds);
    }

    private async Task<TcpConnectionResult> ConnectTcpAsync(
        IPAddress[] addresses,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        foreach (IPAddress address in addresses)
        {
            TcpClient client = new();

            using CancellationTokenSource attemptTimeoutCts =
                new(TimeSpan.FromSeconds(5));

            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    attemptTimeoutCts.Token);

            try
            {
                await client.ConnectAsync(
                    address,
                    443,
                    linkedCts.Token);

                stopwatch.Stop();

                return new TcpConnectionResult(
                    client,
                    address,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (SocketException exception)
            {
                client.Dispose();

                Console.WriteLine(
                    $"TCP connection failed for {address}: {exception.Message}");
            }
            catch (OperationCanceledException)
            {
                client.Dispose();

                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                Console.WriteLine(
                    $"TCP connection timed out for {address}.");
            }
        }

        throw new InvalidOperationException(
            "TCP connection failed for all resolved addresses.");
    }
}