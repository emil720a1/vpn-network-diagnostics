using System.Diagnostics;
using System.Net.Security;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using VpnNetworkDiagnostics;

Console.WriteLine("Enter a hostname: ");
string? host = Console.ReadLine();

if (string.IsNullOrWhiteSpace(host))
{
    Console.WriteLine("Hostname was not provided.");
    return;
}

try
{
    Console.WriteLine($"\nResolving IP addresses for: {host}");

    Stopwatch totalStopwatch = Stopwatch.StartNew();
    Stopwatch dnsStopwatch = Stopwatch.StartNew();

    using CancellationTokenSource dnsTimeoutCts =
        new(TimeSpan.FromSeconds(5));

    IPAddress[] addresses =
        await Dns.GetHostAddressesAsync(host, dnsTimeoutCts.Token);

    dnsStopwatch.Stop();

    Console.WriteLine("DNS resolution succeeded. Addresses found:");

    const int port = 443;

    Stopwatch tcpStopwatch = Stopwatch.StartNew();

    TcpClient? connectedClient = null;
    IPAddress? selectedAddress = null;

     foreach (IPAddress address in addresses)
     {
         Console.WriteLine(
             $"Trying TCP connection to {address}:{port}");

         TcpClient candidateClient = new();

         using CancellationTokenSource attemptTimeoutCts =
             new(TimeSpan.FromSeconds(5));

         try
         {
             await candidateClient.ConnectAsync(
                 address,
                 port,
                 attemptTimeoutCts.Token);

             connectedClient = candidateClient;
             selectedAddress = address;

             Console.WriteLine(
                 $"TCP connection succeeded: {address}:{port}");

             break;
         }
         catch (SocketException exception)
         {
             candidateClient.Dispose();

             Console.WriteLine(
                 $"TCP connection failed for {address}: {exception.Message}");
         }
         catch (OperationCanceledException)
         {
             candidateClient.Dispose();

             Console.WriteLine(
                 $"TCP connection timed out for {address}.");
         }
     }

     if (connectedClient is null || selectedAddress is null)
     {
         Console.WriteLine(
             "TCP connection failed for all resolved addresses.");

         return;
     }

     using TcpClient tcpClient = connectedClient;

     tcpStopwatch.Stop();

     using NetworkStream networkStream = tcpClient.GetStream();

     using SslStream sslStream = new(
        networkStream,
        leaveInnerStreamOpen: false);

    using CancellationTokenSource tlsTimeoutCts =
        new(TimeSpan.FromSeconds(5));

    Stopwatch tlsStopwatch = Stopwatch.StartNew();

    var options = new SslClientAuthenticationOptions
    {
        TargetHost = host
    };

    await sslStream.AuthenticateAsClientAsync(
        options,
        tlsTimeoutCts.Token);

    tlsStopwatch.Stop();

    Console.WriteLine("TLS handshake succeeded.");

    using StreamWriter writer = new(
        sslStream,
        leaveOpen: true);

    writer.AutoFlush = true;

    Stopwatch httpStopwatch = Stopwatch.StartNew();

    await writer.WriteAsync(
        $"GET / HTTP/1.1\r\n" +
        $"Host: {host}\r\n" +
        "Connection: close\r\n" +
        "\r\n");

    Console.WriteLine("HTTP request sent.");

    using StreamReader reader = new(
        sslStream,
        leaveOpen: false);

    using CancellationTokenSource httpTimeoutCts =
        new(TimeSpan.FromSeconds(10));

    string response = await reader.ReadToEndAsync(httpTimeoutCts.Token);

    httpStopwatch.Stop();
    totalStopwatch.Stop();

    HttpResponseParser parser = new();

    int statusCode = parser.Parse(response);

    var diagnosticReport = DiagnosticReport.Create(
        host,
        selectedAddress,
        dnsStopwatch.ElapsedMilliseconds,
        tcpStopwatch.ElapsedMilliseconds,
        tlsStopwatch.ElapsedMilliseconds,
        httpStopwatch.ElapsedMilliseconds,
        totalStopwatch.ElapsedMilliseconds,
        statusCode);

    PrintReport(diagnosticReport);
}
catch (SocketException exception)
{
    Console.WriteLine("Network operation failed.");
    Console.WriteLine($"Error: {exception.Message}");
}
catch (AuthenticationException exception)
{
    Console.WriteLine("TLS handshake failed.");
    Console.WriteLine($"Error: {exception.Message}");
}
catch (IOException exception)
{
    Console.WriteLine("Network stream operation failed.");
    Console.WriteLine($"Error: {exception.Message}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Network operation timed out.");
}


static void PrintReport(DiagnosticReport report)
{
        Console.WriteLine("\n=== Network Diagnostic Report ===");
        Console.WriteLine($"Host: {report.Host}");
        Console.WriteLine($"Selected IP: {report.SelectedIp}");
        Console.WriteLine($"Status code: {report.StatusCode}");
        Console.WriteLine(
            $"Result: {(report.IsSuccess ? "SUCCESS" : "FAILURE")}");

        Console.WriteLine("\n=== Timing ===");
        Console.WriteLine($"DNS:   {report.DnsTime} ms");
        Console.WriteLine($"TCP:   {report.TcpTime} ms");
        Console.WriteLine($"TLS:   {report.TlsTime} ms");
        Console.WriteLine($"HTTP:  {report.HttpTime} ms");
        Console.WriteLine($"Total: {report.TotalTime} ms");
}
