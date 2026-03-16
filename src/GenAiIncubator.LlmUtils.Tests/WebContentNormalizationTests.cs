using System.Net;
using GenAiIncubator.LlmUtils.Core.Extensions;
using GenAiIncubator.LlmUtils.Core.Services.WebContentNormalization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;

namespace GenAiIncubator.LlmUtils.Tests;

public class WebContentNormalizationTests
{
    [Fact]
    public async Task NormalizeForLlm_PlainText_ReturnsAsIs()
    {
        // Arrange
        var mockHttpClient = CreateMockHttpClient("Plain text content without HTML");
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert
        Assert.Equal("Plain text content without HTML", result);
    }

    [Fact]
    public async Task NormalizeForLlm_SimpleHtml_ExtractsTextContent()
    {
        // Arrange
        var html = """
                   <html>
                   <head><title>Test Page</title></head>
                   <body>
                       <h1>Main Heading</h1>
                       <p>This is a paragraph with some content.</p>
                       <p>Another paragraph here.</p>
                   </body>
                   </html>
                   """;
        var mockHttpClient = CreateMockHttpClient(html);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert
        Assert.Contains("## Main Heading", result);
        Assert.Contains("This is a paragraph with some content.", result);
        Assert.Contains("Another paragraph here.", result);
        Assert.DoesNotContain("<html>", result);
        Assert.DoesNotContain("<p>", result);
    }

    [Fact]
    public async Task NormalizeForLlm_RemovesScriptsAndStyles()
    {
        // Arrange
        var html = """
                   <html>
                   <head>
                       <style>body { color: red; }</style>
                   </head>
                   <body>
                       <h1>Content</h1>
                       <script>alert('hello');</script>
                       <p>Visible content</p>
                   </body>
                   </html>
                   """;
        var mockHttpClient = CreateMockHttpClient(html);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert
        Assert.Contains("## Content", result);
        Assert.Contains("Visible content", result);
        Assert.DoesNotContain("color: red", result);
        Assert.DoesNotContain("alert('hello')", result);
    }

    [Fact]
    public async Task NormalizeForLlm_RemovesNavigationElements()
    {
        // Arrange
        var html = """
                   <html>
                   <body>
                       <nav>Navigation menu</nav>
                       <header>Site header</header>
                       <main>
                           <h1>Article Title</h1>
                           <p>Main content here</p>
                       </main>
                       <aside>Sidebar content</aside>
                       <footer>Footer content</footer>
                   </body>
                   </html>
                   """;
        var mockHttpClient = CreateMockHttpClient(html);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert
        Assert.Contains("## Article Title", result);
        Assert.Contains("Main content here", result);
        Assert.DoesNotContain("Navigation menu", result);
        Assert.DoesNotContain("Site header", result);
        Assert.DoesNotContain("Sidebar content", result);
        Assert.DoesNotContain("Footer content", result);
    }

    [Fact]
    public async Task NormalizeForLlm_CleansUpWhitespace()
    {
        // Arrange
        var html = """
                   <html>
                   <body>
                       <p>First paragraph</p>
                       
                       
                       
                       <p>Second    paragraph    with    spaces</p>
                   </body>
                   </html>
                   """;
        var mockHttpClient = CreateMockHttpClient(html);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert
        Assert.Contains("First paragraph", result);
        Assert.Contains("Second paragraph with spaces", result);
        // Should not have more than 2 consecutive line breaks
        Assert.DoesNotContain("\n\n\n", result);
        // Should not have multiple consecutive spaces
        Assert.DoesNotContain("    ", result);
    }

    [Fact]
    public async Task NormalizeForLlm_PrefersMainContentArea()
    {
        // Arrange
        var html = """
                   <html>
                   <body>
                       <div>Random content</div>
                       <main>
                           <h1>Main Article</h1>
                           <p>This is the main content</p>
                       </main>
                       <div>More random content</div>
                   </body>
                   </html>
                   """;
        var mockHttpClient = CreateMockHttpClient(html);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert
        Assert.Contains("## Main Article", result);
        Assert.Contains("This is the main content", result);
        // Should prioritize main content over random divs
        Assert.DoesNotContain("Random content", result);
        Assert.DoesNotContain("More random content", result);
    }

    [Fact]
    public async Task FetchAndNormalizeAsync_EmptyContent_ThrowsException()
    {
        // Arrange
        var mockHttpClient = CreateMockHttpClient("");
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.FetchAndNormalizeAsync("http://example.com"));
    }

    [Fact]
    public async Task FetchAndNormalizeAsync_HttpError_ThrowsException()
    {
        // Arrange
        var mockHttpClient = CreateMockHttpClient("", HttpStatusCode.NotFound);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.FetchAndNormalizeAsync("http://example.com"));
    }

    [Fact]
    public async Task NormalizeForLlm_SampleHtmlFile_ExtractsCorrectContent()
    {
        // Arrange
        var htmlFilePath = Path.Combine("static", "test_content", "sample.html");
        var html = await File.ReadAllTextAsync(htmlFilePath);
        var mockHttpClient = CreateMockHttpClient(html);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert
        Assert.Contains("## Main Article Title", result);
        Assert.Contains("This is the main content that should be extracted.", result);
        Assert.Contains("important information", result);
        
        // Should remove unwanted elements
        Assert.DoesNotContain("Navigation menu", result);
        Assert.DoesNotContain("Page header", result);
        Assert.DoesNotContain("Sidebar content", result);
        Assert.DoesNotContain("Page footer", result);
        Assert.DoesNotContain("Ad content to be removed", result);
        Assert.DoesNotContain("background: blue", result);
        Assert.DoesNotContain("console.log", result);
    }

    [Fact]
    public async Task NormalizeForLlm_LinksInHtml_PreservesUrlsWithLinkText()
    {
        // Arrange
        var html = """
                   <html>
                   <body>
                       <h1>Article with Links</h1>
                       <p>Check out <a href="https://example.com">this website</a> for more info.</p>
                       <p>Email us at <a href="mailto:test@example.com">test@example.com</a></p>
                       <p>Visit our <a href="/about">About page</a> to learn more.</p>
                       <p>Link without text: <a href="https://google.com"></a></p>
                       <p>Text without link: <a>just text</a></p>
                   </body>
                   </html>
                   """;
        var mockHttpClient = CreateMockHttpClient(html);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert - URLs are preserved with link text
        Console.WriteLine(result);
        Assert.Contains("this website (https://example.com/)", result);
        Assert.Contains("test@example.com (mailto:test@example.com)", result);
        Assert.Contains("About page (http://example.com/about)", result);
        
        // Assert - Edge cases handled properly
        Assert.DoesNotContain("https://google.com", result); // Empty link text ignored
        Assert.Contains("just text", result); // Text without href preserved
        
        // Assert - Heading is still formatted correctly
        Assert.Contains("## Article with Links", result);
    }

    [Fact]
    public async Task NormalizeForLlm_ResolvesRelativeLinksToAbsolute_WithAndWithoutBase()
    {
        // Arrange
        var html = """
                   <html>
                   <head><title>Test</title></head>
                   <body>
                       <h1>Relative Links</h1>
                       <p>Root relative: <a href="/energie-besparen/energiezuinig-douchen/">Energiezuinig douchen</a></p>
                       <p>Path relative: <a href="docs/evidence">Evidence doc</a></p>
                       <p>Protocol relative: <a href="//cdn.example.com/asset">CDN asset</a></p>
                   </body>
                   </html>
                   """;
        var mockHttpClient = CreateMockHttpClient(html);
        var service = new WebContentNormalizationService(mockHttpClient);

        // Act
        var result = await service.FetchAndNormalizeAsync("http://example.com");

        // Assert: all links resolved to absolute http(s)
        Console.WriteLine(result);
        Assert.Contains("Energiezuinig douchen (http://example.com/energie-besparen/energiezuinig-douchen/)", result);
        Assert.Contains("Evidence doc (http://example.com/docs/evidence)", result);
        Assert.Contains("CDN asset (http://cdn.example.com/asset)", result);
    }

    private static HttpClient CreateMockHttpClient(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        return new HttpClient(mockHandler.Object);
    }
}
