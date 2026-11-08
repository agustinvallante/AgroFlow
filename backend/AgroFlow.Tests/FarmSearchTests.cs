using AgroFlow.Application.MasterData;
using Xunit;

namespace AgroFlow.Tests;

/// <summary>Resolución del nombre con que una persona nombra una finca. Datos ficticios.</summary>
public sealed class FarmSearchTests
{
    private static readonly MasterDataItem[] Farms =
    [
        Farm("FINCA-NORTE", "Finca Norte"),
        Farm("FINCA-SUR", "Finca Sur"),
        Farm("F-SJ", "Finca San José"),
        Farm("F-SP", "Finca San Pedro")
    ];

    [Theory]
    [InlineData("FINCA-NORTE", "FINCA-NORTE")]
    [InlineData("finca-sur", "FINCA-SUR")]
    [InlineData("Finca Norte", "FINCA-NORTE")]
    [InlineData("norte", "FINCA-NORTE")]
    [InlineData("  NORTE ", "FINCA-NORTE")]
    [InlineData("finca sur", "FINCA-SUR")]
    [InlineData("la finca sur", "FINCA-SUR")]
    [InlineData("la del norte", "FINCA-NORTE")]
    [InlineData("finca norteña", null)]
    [InlineData("san jose", "F-SJ")]
    [InlineData("San José", "F-SJ")]
    [InlineData("F-SP", "F-SP")]
    public void Exact_and_natural_names_resolve_to_the_canonical_code(string search, string? expectedCode)
    {
        var result = FarmSearch.Filter(Farms, search);

        if (expectedCode is null)
            Assert.Empty(result);
        else
            Assert.Equal(expectedCode, Assert.Single(result).Code);
    }

    [Theory]
    [InlineData("Finca Oeste")]
    [InlineData("nor")]
    [InlineData("norte sur")]
    [InlineData("12")]
    public void Unknown_or_partial_names_match_no_farm(string search) =>
        Assert.Empty(FarmSearch.Filter(Farms, search));

    [Fact]
    public void A_name_shared_by_several_farms_returns_all_of_them_to_ask_for_clarification() =>
        Assert.Equal(["F-SJ", "F-SP"], FarmSearch.Filter(Farms, "san").Select(f => f.Code));

    [Theory]
    [InlineData("finca")]
    [InlineData("--")]
    public void A_search_without_distinctive_words_does_not_choose_a_farm(string search) =>
        Assert.Equal(Farms.Length, FarmSearch.Filter(Farms, search).Count);

    [Fact]
    public void No_search_returns_the_list_unchanged() =>
        Assert.Same(Farms, FarmSearch.Filter(Farms, null));

    private static MasterDataItem Farm(string code, string name) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, code, "Referencia ficticia", null, true, DateTime.UtcNow, null);
}
