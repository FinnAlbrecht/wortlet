using Wortlet.Domain.Common;
using Wortlet.Domain.Languages;
using Wortlet.UnitTests.TestDoubles;

namespace Wortlet.UnitTests.Domain;

public class LanguageTests
{
    [Fact]
    public void Language_can_only_be_deleted_when_empty()
    {
        var language = Language.Create(TestData.Owner, "Französisch", TestData.Now);
        language.EnsureCanBeDeleted(0);
        var ex = Assert.Throws<DomainException>(() => language.EnsureCanBeDeleted(2));
        Assert.Contains("leer", ex.Message);
    }

    [Fact]
    public void Name_is_trimmed_and_required()
    {
        Assert.Equal("Italienisch", Language.Create(TestData.Owner, "  Italienisch ", TestData.Now).Name);
        Assert.Throws<DomainException>(() => Language.Create(TestData.Owner, " ", TestData.Now));
    }
}
