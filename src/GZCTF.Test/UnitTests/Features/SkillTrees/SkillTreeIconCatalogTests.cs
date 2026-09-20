using GZCTF.Features.SkillTrees.Domain;
using Xunit;

namespace GZCTF.Test.UnitTests.Features.SkillTrees;

public sealed class SkillTreeIconCatalogTests
{
    [Theory]
    [InlineData("flag")]
    [InlineData("web")]
    [InlineData("crypto")]
    [InlineData("pwn")]
    [InlineData("brain")]
    [InlineData("ai")]
    public void Preset_icons_are_accepted(string key) =>
        Assert.True(SkillTreeIconCatalog.IsSupported(key));

    [Theory]
    [InlineData("")]
    [InlineData("🚩")]
    [InlineData("custom")]
    public void Arbitrary_icons_are_rejected(string key) =>
        Assert.False(SkillTreeIconCatalog.IsSupported(key));
}
