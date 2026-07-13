namespace kickflip.Tests;

public class UtilitiesTests
{
    [Theory]
    [InlineData("/public_html", "index.html", "/public_html/index.html")]
    [InlineData("/public_html/", "/index.html", "/public_html/index.html")]
    [InlineData("/", "index.html", "/index.html")]
    [InlineData("", "index.html", "index.html")]
    [InlineData("/public_html", "", "/public_html")]
    public void UrlCombine_CombinesPathsWithSingleSeparator(string url1, string url2, string expected)
    {
        var result = kickflip.Utilities.UrlCombine(url1, url2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void UrlCombine_NormalisesDirectorySeparatorsToForwardSlashes()
    {
        var nested = "sub" + Path.DirectorySeparatorChar + "file.txt";

        var result = kickflip.Utilities.UrlCombine("/public_html", nested);

        Assert.Equal("/public_html/sub/file.txt", result);
    }
}
