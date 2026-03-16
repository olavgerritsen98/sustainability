using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace GenAiIncubator.LlmUtils.Core.Services.WebContentNormalization;

/// <summary>
/// Default implementation that fetches a webpage and converts it into normalized text for LLM consumption.
/// </summary>
public partial class WebContentNormalizationService(HttpClient httpClient) : IWebContentNormalizationService
{
    private readonly HttpClient _httpClient = httpClient;

    [GeneratedRegex(@"\n\s*\n\s*\n+")]
    private static partial Regex MultipleLineBreaksRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex MultipleSpacesRegex();

    /// <inheritdoc />
    public async Task<string> FetchAndNormalizeAsync(string url, CancellationToken cancellationToken = default)
    {
        string raw = await FetchWebpageAsync(url, cancellationToken);
        string normalized = NormalizeForLlm(raw, url);
        return normalized;
    }

    private async Task<string> FetchWebpageAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Fetched content was empty.");

        return content;
    }

    private static string NormalizeForLlm(string htmlOrRaw, string? baseUrl)
    {
        // If it's plain text already, return as-is
        if (!htmlOrRaw.TrimStart().StartsWith('<'))
        {
            // Normalize common problematic whitespace characters (e.g. non-breaking spaces)
            return CleanupSpecialWhitespace(htmlOrRaw);
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlOrRaw);

        // Determine base URI from passed page URL and optional <base href> element
        Uri? baseUri = null;
        if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedBase))
        {
            baseUri = parsedBase;
        }
        var baseNode = doc.DocumentNode.SelectSingleNode("//base[@href]");
        if (baseNode != null)
        {
            var baseHref = baseNode.GetAttributeValue("href", "").Trim();
            if (!string.IsNullOrWhiteSpace(baseHref))
            {
                if (Uri.TryCreate(baseHref, UriKind.Absolute, out var absoluteBase))
                {
                    baseUri = absoluteBase;
                }
                else if (baseUri != null && Uri.TryCreate(baseUri, baseHref, out var combinedBase))
                {
                    baseUri = combinedBase;
                }
            }
        }

        // Remove script and style elements completely
        RemoveNodes(doc, "//script | //style | //noscript");
        
        // Remove common non-content elements
        RemoveNodes(doc, "//nav | //header | //footer | //aside | //*[@class='advertisement'] | //*[@class='ads'] | //*[@class='navigation'] | //*[@class='menu']");

        // Try to find main content area
        var contentNode = FindMainContent(doc);
        
        // Extract text with some structure preservation
        var normalizedText = ExtractStructuredText(contentNode ?? doc.DocumentNode, baseUri);
        
        // Clean up excessive whitespace and normalize special spaces
        normalizedText = CleanupSpecialWhitespace(normalizedText);
        normalizedText = CleanupWhitespace(normalizedText);
        
        return normalizedText;
    }

    private static string CleanupSpecialWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Replace non-breaking spaces with regular spaces
        return text
            .Replace('\u00A0', ' ');
    }

    private static void RemoveNodes(HtmlDocument doc, string xpath)
    {
        var nodes = doc.DocumentNode.SelectNodes(xpath);
        if (nodes != null)
        {
            foreach (var node in nodes.ToList())
                node.Remove();
        }
    }

    private static HtmlNode? FindMainContent(HtmlDocument doc)
    {
        // Try to find semantic content areas in order of preference
        var selectors = new[]
        {
            "//main",
            "//article", 
            "//*[@role='main']",
            "//*[@class='content'] | //*[@id='content']",
            "//*[@class='main'] | //*[@id='main']",
            "//body"
        };

        foreach (var selector in selectors)
        {
            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null)
                return node;
        }

        return null;
    }

    private static string ExtractStructuredText(HtmlNode node, Uri? baseUri)
    {
        var sb = new StringBuilder();
        
        ExtractTextRecursive(node, sb, baseUri);
        
        return sb.ToString();
    }

    private static void ExtractTextRecursive(HtmlNode node, StringBuilder sb, Uri? baseUri)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Text)
            {
                var text = HtmlEntity.DeEntitize(child.InnerText).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.Append(text);
                    sb.Append(' ');
                }
            }
            else if (child.NodeType == HtmlNodeType.Element)
            {
                // Add structural breaks for block elements
                if (IsBlockElement(child.Name))
                {
                    sb.AppendLine();
                }
                
                // Add special formatting for headings
                if (child.Name.StartsWith('h') && child.Name.Length == 2)
                {
                    sb.AppendLine();
                    sb.Append("## ");
                }
                
                // Handle links to preserve URLs
                if (child.Name.Equals("a", StringComparison.OrdinalIgnoreCase))
                {
                    var href = child.GetAttributeValue("href", "");
                    var linkText = child.InnerText.Trim();
                    
                    if (!string.IsNullOrWhiteSpace(href) && !string.IsNullOrWhiteSpace(linkText))
                    {
                        var resolved = ResolveHrefToAbsolute(href, baseUri) ?? href;
                        sb.Append($"{linkText} ({resolved}) ");
                    }
                    else if (!string.IsNullOrWhiteSpace(linkText))
                    {
                        sb.Append($"{linkText} ");
                    }
                }
                else
                {
                    ExtractTextRecursive(child, sb, baseUri);
                }
                
                // Add line breaks after block elements
                if (IsBlockElement(child.Name))
                {
                    sb.AppendLine();
                }
            }
        }
    }

    private static string? ResolveHrefToAbsolute(string href, Uri? baseUri)
    {
        href = href.Trim();
        if (string.IsNullOrWhiteSpace(href))
            return null;

        // Protocol-relative (e.g., //example.com/path)
        if (href.StartsWith("//", StringComparison.Ordinal))
        {
            if (baseUri != null)
            {
                var scheme = baseUri.Scheme;
                if (Uri.TryCreate($"{scheme}:{href}", UriKind.Absolute, out var protoAbsolute))
                    return protoAbsolute.ToString();
            }
            // Default to https if no base
            if (Uri.TryCreate($"https:{href}", UriKind.Absolute, out var httpsAbsolute))
                return httpsAbsolute.ToString();
        }

        // Relative to base
        if (baseUri != null && Uri.TryCreate(baseUri, href, out var combined))
            return combined.ToString();

        // Already absolute (http, https, mailto, etc.)
        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        return null;
    }

    private static bool IsBlockElement(string tagName)
    {
        var blockElements = new HashSet<string>
        {
            "div", "p", "h1", "h2", "h3", "h4", "h5", "h6", 
            "ul", "ol", "li", "section", "article", "header", 
            "footer", "main", "aside", "blockquote", "pre"
        };
        
        return blockElements.Contains(tagName.ToLowerInvariant());
    }

    private static string CleanupWhitespace(string text)
    {
        // Replace multiple consecutive line breaks with max 2
        text = MultipleLineBreaksRegex().Replace(text, "\n\n");
        
        // Replace multiple spaces with single space
        text = MultipleSpacesRegex().Replace(text, " ");
        
        // Trim and ensure we don't have leading/trailing whitespace
        return text.Trim();
    }
}