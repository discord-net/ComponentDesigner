using Discord.CX.Nodes;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public sealed class UnfurledMediaItemTests(ITestOutputHelper output) : BaseRendererTest(output)
{
    [Fact]
    public void BasicUnfurledMediaItem()
    {
        AssertRenders(
            """
            'https://example.com'
            """,
            CXValueGenerator.UnfurledMediaItem,
            """
            new global::Discord.UnfurledMediaItemProperties("https://example.com")
            """
        );
    }
}