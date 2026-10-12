using System.Globalization;

namespace VpnNetworkDiagnostics;

public class HttpResponseParser
{
    public int Parse(string response)
    {
        int lineEndIndex = response.IndexOf("\r\n");

        if (lineEndIndex == -1)
        {
            Console.WriteLine("Invalid HTTP response.");
            return -1;
        }

        string statusLine = response[..lineEndIndex];

        int headersEndIndex = response.IndexOf("\r\n\r\n");

        if (headersEndIndex == -1)
        {
            Console.WriteLine("HTTP headers were not found.");
            return -1;
        }

        int bodyStartIndex = headersEndIndex + 4;

        string body = response[bodyStartIndex..];

        string headersText = response[
            (lineEndIndex + 2)..headersEndIndex];

        Console.WriteLine($"Status line: {statusLine}");

        string[] statusParts = statusLine.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        if (statusParts.Length < 2 ||
            !int.TryParse(statusParts[1], out int statusCode))
        {
            Console.WriteLine("Invalid HTTP status line.");
            return -1;
        }

        Console.WriteLine($"Status code: {statusCode}");

        string[] headerLines =
            headersText.Split("\r\n");

        Dictionary<string, string> headers = new(
            StringComparer.OrdinalIgnoreCase);

        foreach (string headerLine in headerLines)
        {
            int separatorIndex = headerLine.IndexOf(':');

            if (separatorIndex == -1)
            {
                continue;
            }

            string name = headerLine[..separatorIndex].Trim();

            string value = headerLine[(separatorIndex + 1)..].Trim();

            headers[name] = value;
        }

        Console.WriteLine("\nParsed headers:");

        foreach (KeyValuePair<string, string> header in headers)
        {
            Console.WriteLine($"{header.Key} = {header.Value}");
        }

        if (headers.TryGetValue(
                "Transfer-Encoding",
                out string? transferEncoding) &&
            transferEncoding.Equals(
                "chunked",
                StringComparison.OrdinalIgnoreCase))
        {
            body = DecodeChunkedBody(body);
        }

        Console.WriteLine("\nDecoded response body:");
        Console.WriteLine(body);

        return statusCode;
    }

    private string DecodeChunkedBody(string body)
    {
        List<string> chunks = new();
        int position = 0;

        while (position < body.Length)
        {
            int sizeLineEnd = body.IndexOf("\r\n", position);

            if (sizeLineEnd == -1)
            {
                throw new FormatException(
                    "Invalid chunked response: chunk size is incomplete.");
            }

            string sizeText = body[position..sizeLineEnd];
            int extensionSeparator = sizeText.IndexOf(';');

            if (extensionSeparator >= 0)
            {
                sizeText = sizeText[..extensionSeparator];
            }

            if (!int.TryParse(
                    sizeText.Trim(),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out int chunkSize) ||
                chunkSize < 0)
            {
                throw new FormatException(
                    "Invalid chunked response: invalid chunk size.");
            }

            position = sizeLineEnd + 2;

            if (chunkSize == 0)
            {
                break;
            }

            if (position + chunkSize > body.Length)
            {
                throw new FormatException(
                    "Invalid chunked response: chunk exceeds body length.");
            }

            chunks.Add(body.Substring(position, chunkSize));
            position += chunkSize;

            if (position + 2 > body.Length ||
                !body.AsSpan(position, 2).SequenceEqual("\r\n"))
            {
                throw new FormatException(
                    "Invalid chunked response: missing chunk terminator.");
            }

            position += 2;
        }

        return string.Concat(chunks);
    }
}
