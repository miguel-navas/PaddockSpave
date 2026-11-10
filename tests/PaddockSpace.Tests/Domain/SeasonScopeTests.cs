using PaddockSpace.Domain.ValueObjects;

namespace PaddockSpace.Tests.Domain;

public class SeasonScopeTests
{
    [Fact]
    public void Parse_ShouldReturnCurrentSeason_WhenInputIs2026OrNullOrCurrent()
    {
        var fromYear = SeasonScope.Parse("2026");
        var fromNull = SeasonScope.Parse(null);
        var fromCurrent = SeasonScope.Parse("current");

        Assert.True(fromYear.IsCurrent);
        Assert.Equal(2026, fromYear.Year);
        Assert.True(fromYear.HasRichTelemetry);
        Assert.True(fromYear.HasQualifyingSplits);
        Assert.True(fromYear.HasSprintRaces);
        Assert.True(fromYear.HasConstructorChampionship);

        Assert.Equal(fromYear, fromNull);
        Assert.Equal(fromYear, fromCurrent);
    }

    [Theory]
    [InlineData(1950, false, false, false, false)]
    [InlineData(1958, false, false, false, true)]
    [InlineData(1991, false, false, false, true)]
    [InlineData(1994, false, true, false, true)]
    [InlineData(2021, false, true, true, true)]
    [InlineData(2025, false, true, true, true)]
    public void Parse_ShouldReturnHistoricalSeasonWithExpectedCapabilities(
        int year,
        bool expectedTelemetry,
        bool expectedQualifying,
        bool expectedSprint,
        bool expectedConstructors)
    {
        var scope = SeasonScope.Parse(year.ToString());

        Assert.True(scope.IsHistorical);
        Assert.Equal(year, scope.Year);
        Assert.Equal(expectedTelemetry, scope.HasRichTelemetry);
        Assert.Equal(expectedQualifying, scope.HasQualifyingSplits);
        Assert.Equal(expectedSprint, scope.HasSprintRaces);
        Assert.Equal(expectedConstructors, scope.HasConstructorChampionship);
    }

    [Theory]
    [InlineData("all")]
    [InlineData("todas")]
    [InlineData("all-time")]
    [InlineData("*")]
    public void Parse_ShouldReturnAllTime_WhenInputIsAllTimeAlias(string input)
    {
        var scope = SeasonScope.Parse(input);

        Assert.True(scope.IsAllTime);
        Assert.Null(scope.Year);
        Assert.Equal("all", scope.ToKey());
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2027)]
    public void ForYear_ShouldThrowArgumentOutOfRangeException_WhenYearIsOutOfBounds(int invalidYear)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SeasonScope.ForYear(invalidYear));
    }

    [Fact]
    public void CategoryCode_ShouldSupportMultiCategoryRoadmap()
    {
        var f1 = CategoryCode.Parse("f1");
        Assert.True(f1.IsFormula1);
        Assert.True(f1.IsAvailable);

        var all = CategoryCode.ListAll();
        Assert.Equal(7, all.Count);
        Assert.Contains(all, c => c.Code == "indy");
        Assert.Contains(all, c => c.Code == "nascar");
        Assert.Contains(all, c => c.Code == "stockcar");
        Assert.Contains(all, c => c.Code == "rally");
        Assert.Contains(all, c => c.Code == "motogp");
    }
}

