using System;
using System.Text.RegularExpressions;

namespace Gubal;

public static partial class UrlValidator
{
    [GeneratedRegex(@"^https:\/\/[a-zA-Z0-9\-\.]+(:\d+)?(\/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    public static bool IsValidUrl(string? url) => !string.IsNullOrWhiteSpace(url) && UrlRegex().IsMatch(url.Trim());
}
