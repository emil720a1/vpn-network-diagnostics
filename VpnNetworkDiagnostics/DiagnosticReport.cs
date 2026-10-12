using System.Net;

namespace VpnNetworkDiagnostics;

public class DiagnosticReport
{
    public string Host { get; }

    public IPAddress SelectedIp { get; }

    public long DnsTime { get; }

    public long TcpTime { get; }

    public long TlsTime { get; }

    public long HttpTime { get; }

    public long TotalTime { get; }

    public int StatusCode { get; }

    public bool IsSuccess =>
        StatusCode >= 200 &&
        StatusCode <= 299;

    private DiagnosticReport(
        string host,
        IPAddress selectedIp,
        long dnsTime,
        long tcpTime,
        long tlsTime,
        long httpTime,
        long totalTime,
        int statusCode)
    {
        Host = host;
        SelectedIp = selectedIp;
        DnsTime = dnsTime;
        TcpTime = tcpTime;
        TlsTime = tlsTime;
        HttpTime = httpTime;
        TotalTime = totalTime;
        StatusCode = statusCode;
    }

    public static DiagnosticReport Create(
        string host,
        IPAddress selectedIp,
        long dnsTime,
        long tcpTime,
        long tlsTime,
        long httpTime,
        long totalTime,
        int statusCode
        )
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException(
               "Host cannot be null or whitespace.",
               nameof(host));
        }

        if (selectedIp is null)
        {
            throw new ArgumentNullException(
                nameof(selectedIp),
                "Selected IP cannot be null.");
        }

        if (statusCode < 100 || statusCode > 599)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusCode),
                statusCode,
                "Status code must be between 100 and 599.");
        }

        if (dnsTime < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dnsTime),
                dnsTime,
                "DNS time cannot be negative.");
        }

        if (tcpTime < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tcpTime),
                tcpTime,
                "TCP time cannot be negative.");
        }

        if (tlsTime < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tlsTime),
                tlsTime,
                "TLS time cannot be negative.");
        }

        if (httpTime < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(httpTime),
                httpTime,
                "HTTP time cannot be negative.");
        }

        if (totalTime < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalTime),
                totalTime,
                "Total time cannot be negative.");
        }


        return new DiagnosticReport(
            host,
            selectedIp,
            dnsTime,
            tcpTime,
            tlsTime,
            httpTime,
            totalTime,
            statusCode);
    }
}