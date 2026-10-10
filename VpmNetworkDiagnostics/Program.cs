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

    IPAddress[] addresses =
        await Dns.GetHostAddressesAsync(host);

    Console.WriteLine("DNS resolution succeeded. Addresses found:");

    foreach (IPAddress address in addresses)
    {
        Console.WriteLine($"- {address}");
    }

    IPAddress selectedAddress = addresses[0];
    const int port = 443;

    Console.WriteLine(
        $"\nChecking TCP connection to {selectedAddress}:{port}");

    Stopwatch stopwatch = Stopwatch.StartNew();

    using TcpClient tcpClient = new();

    await tcpClient.ConnectAsync(selectedAddress, port);
    Console.WriteLine("TCP connection succeeded.");

    using NetworkStream networkStream = tcpClient.GetStream();

    using SslStream sslStream = new(
        networkStream,
        leaveInnerStreamOpen: false);

    await sslStream.AuthenticateAsClientAsync(host);
    Console.WriteLine("TLS handshake succeeded.");

    using StreamWriter writer = new(
        sslStream,
        leaveOpen: true);

    writer.AutoFlush = true;

    await writer.WriteAsync(
        $"GET / HTTP/1.1\r\n" +
        $"Host: {host}\r\n" +
        "Connection: close\r\n" +
        "\r\n");

    Console.WriteLine("HTTP request sent.");

    using StreamReader reader = new(
        sslStream,
        leaveOpen: false);

    string response = await reader.ReadToEndAsync();

    stopwatch.Stop();

    HttpResponseParser parser = new();
    parser.Parse(response);

    Console.WriteLine(
        $"\nTCP + TLS + HTTP time: {stopwatch.ElapsedMilliseconds} ms");
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
