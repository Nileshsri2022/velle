// OpenInstaller — original tests for the original parser, MIT licensed.
using OpenInstaller.Services;
using Xunit;

namespace OpenInstaller.Tests;

public sealed class WingetOutputParserTests
{
    [Fact]
    public void ParseSearch_ReadsColumnAlignedTable()
    {
        const string output = """
            Name            Id                  Version  Source
            ------------------------------------------------------
            VLC media player VideoLAN.VLC       3.0.21   winget
            Git             Git.Git             2.47.1   winget
            """;

        var results = WingetOutputParser.ParseSearch(output);

        Assert.Equal(2, results.Count);
        Assert.Equal("VLC media player", results[0].Name);
        Assert.Equal("VideoLAN.VLC", results[0].Id);
        Assert.Equal("3.0.21", results[0].Version);
        Assert.Equal("winget", results[0].Source);
        Assert.Equal("Git.Git", results[1].Id);
    }

    [Fact]
    public void ParseSearch_ReturnsEmpty_WhenHeaderIsMissing()
    {
        var results = WingetOutputParser.ParseSearch("No package found matching input criteria.");
        Assert.Empty(results);
    }

    [Fact]
    public void IsNoPackageFound_DetectsWingetMessage()
    {
        Assert.True(WingetOutputParser.IsNoPackageFound("No package found matching input criteria."));
        Assert.False(WingetOutputParser.IsNoPackageFound("VLC media player"));
    }

    [Fact]
    public void ParseList_ReadsInstalledRow_WithAvailableVersion()
    {
        const string output = """
            Name        Id              Version   Available  Source
            -------------------------------------------------------
            VLC         VideoLAN.VLC    3.0.20    3.0.21     winget
            """;

        var rows = WingetOutputParser.ParseList(output);

        Assert.Single(rows);
        Assert.Equal("VideoLAN.VLC", rows[0].Id);
        Assert.Equal("3.0.20", rows[0].Version);
        Assert.Equal("3.0.21", rows[0].Available);
    }

    [Fact]
    public void ParseShow_SplitsOnFirstColonOnly()
    {
        const string output = """
            Publisher: VideoLAN
            Description: A free and open source cross-platform multimedia player: plays almost anything.
            Version: 3.0.21
            """;

        var details = WingetOutputParser.ParseShow(output);

        Assert.Equal("VideoLAN", details["Publisher"]);
        Assert.Equal("A free and open source cross-platform multimedia player: plays almost anything.", details["Description"]);
    }

    [Theory]
    [InlineData("1.2.3", "1.10.0", -1)]
    [InlineData("1.10.0", "1.2.3", 1)]
    [InlineData("3.0.21", "3.0.21", 0)]
    [InlineData("2.0", "2.0.1", -1)]
    public void CompareVersions_OrdersNumerically(string left, string right, int expectedSign)
    {
        var comparison = WingetOutputParser.CompareVersions(left, right);
        Assert.Equal(expectedSign, Math.Sign(comparison));
    }
}
