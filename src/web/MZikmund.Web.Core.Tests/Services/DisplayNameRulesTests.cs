using MZikmund.Web.Core.Services.Comments;

namespace MZikmund.Web.Core.Tests.Services;

public class DisplayNameRulesTests
{
	[Theory]
	[InlineData("  Jane   Doe  ", "Jane Doe")]
	[InlineData("Jane​Doe", "JaneDoe")]
	[InlineData("Jane‮Doe⁦", "JaneDoe")]
	[InlineData("Ja\u0000ne\tDoe\n", "Jane Doe")]
	[InlineData("Café", "Café")]
	[InlineData("A  B", "A B")]
	[InlineData(null, "")]
	public void Normalize_CleansTheName(string? input, string expected) =>
		Assert.Equal(expected, DisplayNameRules.Normalize(input));

	[Theory]
	[InlineData("J", DisplayNameProblem.TooShort)]
	[InlineData(" ​ ", DisplayNameProblem.TooShort)]
	[InlineData("!!", DisplayNameProblem.NoLettersOrDigits)]
	[InlineData("😀😀", DisplayNameProblem.NoLettersOrDigits)]
	[InlineData("Jo", DisplayNameProblem.None)]
	[InlineData("名前", DisplayNameProblem.None)]
	[InlineData("12", DisplayNameProblem.None)]
	public void Check_AppliesLengthAndContentRules(string input, DisplayNameProblem expected) =>
		Assert.Equal(expected, DisplayNameRules.Check(input, isAdmin: false).Problem);

	[Fact]
	public void Check_CountsTextElementsNotChars()
	{
		var forty = string.Concat(Enumerable.Repeat("😀", 39)) + "a";
		var fortyOne = forty + "b";

		Assert.True(DisplayNameRules.Check(forty, isAdmin: false).IsValid);
		Assert.Equal(DisplayNameProblem.TooLong, DisplayNameRules.Check(fortyOne, isAdmin: false).Problem);
	}

	[Theory]
	[InlineData("Martin Zikmund")]
	[InlineData("martin.zikmund")]
	[InlineData("M-Zikmund")]
	[InlineData("mzikmund")]
	[InlineData("Fake MZikmund Fan")]
	[InlineData("ＭＺＩＫＭＵＮＤ")]
	[InlineData("M​Zikmund")]
	[InlineData("Admin")]
	[InlineData("  A.d.m.i.n  ")]
	[InlineData("MODERATOR")]
	[InlineData("mod")]
	[InlineData("Support")]
	[InlineData("Deleted")]
	[InlineData("anonymous")]
	public void Check_RejectsReservedNamesForNonAdmins(string input)
	{
		Assert.Equal(DisplayNameProblem.Reserved, DisplayNameRules.Check(input, isAdmin: false).Problem);
		Assert.True(DisplayNameRules.Check(input, isAdmin: true).IsValid);
	}

	[Theory]
	[InlineData("Admiral")]
	[InlineData("Modest Mouse")]
	[InlineData("Martin")]
	[InlineData("Supporter")]
	public void Check_AllowsNamesThatOnlyLookSimilar(string input) =>
		Assert.True(DisplayNameRules.Check(input, isAdmin: false).IsValid);

	[Fact]
	public void FallbackName_IsStablePerUserAndShapedLikeReaderHex()
	{
		var id = Guid.Parse("8f14e45f-ceea-467a-9575-1b0e02f0b8f6");

		var name = DisplayNameRules.FallbackName(id);

		Assert.Equal(name, DisplayNameRules.FallbackName(id));
		Assert.Matches("^Reader [0-9A-F]{4}$", name);
		Assert.NotEqual(name, DisplayNameRules.FallbackName(Guid.NewGuid()));
	}

	[Theory]
	[InlineData("Martin Zikmund", false, "Reader")]
	[InlineData("Martin Zikmund", true, "Martin Zikmund")]
	[InlineData(null, false, "Reader")]
	[InlineData("x", false, "Reader")]
	[InlineData("Jane Doe", false, "Jane Doe")]
	public void FromToken_UsesTheNameOnlyWhenItPassesTheRules(string? token, bool isAdmin, string expectedPrefix) =>
		Assert.StartsWith(expectedPrefix, DisplayNameRules.FromToken(token, Guid.NewGuid(), isAdmin));

	[Theory]
	[InlineData("Reader 4F2A")]
	[InlineData("reader 4f2a")]
	[InlineData("Reader4F2A")]
	[InlineData("ＲＥＡＤＥＲ ４Ｆ２Ａ")]
	public void Check_RejectsAnotherReadersPlaceholderName(string input)
	{
		Guid someoneElse = Guid.Parse("11111111-1111-1111-1111-111111111111");

		Assert.Equal(DisplayNameProblem.Reserved, DisplayNameRules.Check(input, isAdmin: false, someoneElse).Problem);
	}

	[Fact]
	public void Check_AllowsKeepingYourOwnPlaceholderName()
	{
		Guid userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
		var own = DisplayNameRules.FallbackName(userId);

		Assert.True(DisplayNameRules.Check(own, isAdmin: false, userId).IsValid);
		Assert.True(DisplayNameRules.Check(own.ToLowerInvariant(), isAdmin: false, userId).IsValid);
	}

	[Theory]
	[InlineData("Reader")]
	[InlineData("Reader Joe")]
	[InlineData("Reader 4F2A5")]
	public void Check_AllowsNamesThatAreNotPlaceholderShaped(string input) =>
		Assert.True(DisplayNameRules.Check(input, isAdmin: false, Guid.NewGuid()).IsValid);
}

