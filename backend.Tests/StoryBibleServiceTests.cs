using System.Reflection;
using CanonGuard.Api.Models;
using CanonGuard.Api.Services;
using CanonGuard.Api.Services.AI;

namespace CanonGuard.Tests;

public class StoryBibleServiceTests
{
    private static readonly Type ServiceType = typeof(StoryBibleService);

    private static object? InvokePrivateStatic(string methodName, params object?[] args)
    {
        var method = ServiceType.GetMethod(methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        return method!.Invoke(null, args);
    }

    // ── CleanDisplayName ──

    [Fact]
    public void CleanDisplayName_Null_ReturnsEmpty()
    {
        var result = InvokePrivateStatic("CleanDisplayName", (string?)null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void CleanDisplayName_Whitespace_ReturnsEmpty()
    {
        var result = InvokePrivateStatic("CleanDisplayName", "   ");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void CleanDisplayName_CollapsesMultipleSpaces()
    {
        var result = InvokePrivateStatic("CleanDisplayName", "John    Smith");
        Assert.Equal("John Smith", result);
    }

    [Fact]
    public void CleanDisplayName_TrimsPunctuation()
    {
        var result = InvokePrivateStatic("CleanDisplayName", "\"John Smith,\"");
        Assert.Equal("John Smith", result);
    }

    [Fact]
    public void CleanDisplayName_TrimsLeadingAndTrailingWhitespace()
    {
        var result = InvokePrivateStatic("CleanDisplayName", "  Alice  ");
        Assert.Equal("Alice", result);
    }

    [Fact]
    public void CleanDisplayName_NormalText_Unchanged()
    {
        var result = InvokePrivateStatic("CleanDisplayName", "Bob");
        Assert.Equal("Bob", result);
    }

    // ── TokenizeName ──

    [Fact]
    public void TokenizeName_SingleName_ReturnsSingleToken()
    {
        var result = (List<string>)InvokePrivateStatic("TokenizeName", "Alice")!;
        Assert.Single(result);
        Assert.Equal("alice", result[0]);
    }

    [Fact]
    public void TokenizeName_FullName_ReturnsMultipleTokens()
    {
        var result = (List<string>)InvokePrivateStatic("TokenizeName", "John Smith")!;
        Assert.Equal(2, result.Count);
        Assert.Equal("john", result[0]);
        Assert.Equal("smith", result[1]);
    }

    [Fact]
    public void TokenizeName_WithPunctuation_StripsIt()
    {
        var result = (List<string>)InvokePrivateStatic("TokenizeName", "Mr. Smith!")!;
        Assert.Contains("mr", result);
        Assert.Contains("smith", result);
    }

    // ── NormalizeToken ──

    [Fact]
    public void NormalizeToken_UpperCase_ReturnsLowerCase()
    {
        var result = InvokePrivateStatic("NormalizeToken", "ALICE");
        Assert.Equal("alice", result);
    }

    [Fact]
    public void NormalizeToken_WithPeriod_StripsIt()
    {
        var result = InvokePrivateStatic("NormalizeToken", "Mr.");
        Assert.Equal("mr", result);
    }

    [Fact]
    public void NormalizeToken_WithHyphen_PreservesIt()
    {
        var result = InvokePrivateStatic("NormalizeToken", "Anne-Marie");
        Assert.Equal("anne-marie", result);
    }

    // ── NormalizeFirstName (nicknames) ──

    [Fact]
    public void NormalizeFirstName_Bob_ReturnsRobert()
    {
        var result = InvokePrivateStatic("NormalizeFirstName", "Bob");
        Assert.Equal("robert", result);
    }

    [Fact]
    public void NormalizeFirstName_Bobby_ReturnsRobert()
    {
        var result = InvokePrivateStatic("NormalizeFirstName", "Bobby");
        Assert.Equal("robert", result);
    }

    [Fact]
    public void NormalizeFirstName_Rob_ReturnsRobert()
    {
        var result = InvokePrivateStatic("NormalizeFirstName", "Rob");
        Assert.Equal("robert", result);
    }

    [Fact]
    public void NormalizeFirstName_Liz_ReturnsElizabeth()
    {
        var result = InvokePrivateStatic("NormalizeFirstName", "Liz");
        Assert.Equal("elizabeth", result);
    }

    [Fact]
    public void NormalizeFirstName_Mike_ReturnsMichael()
    {
        var result = InvokePrivateStatic("NormalizeFirstName", "Mike");
        Assert.Equal("michael", result);
    }

    [Fact]
    public void NormalizeFirstName_UnknownName_ReturnsSelf()
    {
        var result = InvokePrivateStatic("NormalizeFirstName", "Gandalf");
        Assert.Equal("gandalf", result);
    }

    [Fact]
    public void NormalizeFirstName_Bill_ReturnsWilliam()
    {
        var result = InvokePrivateStatic("NormalizeFirstName", "Bill");
        Assert.Equal("william", result);
    }

    [Fact]
    public void NormalizeFirstName_Jim_ReturnsJames()
    {
        var result = InvokePrivateStatic("NormalizeFirstName", "Jim");
        Assert.Equal("james", result);
    }

    // ── ContainsHonorific ──

    [Fact]
    public void ContainsHonorific_MrSmith_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("ContainsHonorific", "Mr. Smith")!;
        Assert.True(result);
    }

    [Fact]
    public void ContainsHonorific_DrJones_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("ContainsHonorific", "Dr. Jones")!;
        Assert.True(result);
    }

    [Fact]
    public void ContainsHonorific_PlainName_ReturnsFalse()
    {
        var result = (bool)InvokePrivateStatic("ContainsHonorific", "Alice Johnson")!;
        Assert.False(result);
    }

    [Fact]
    public void ContainsHonorific_Captain_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("ContainsHonorific", "Captain Hook")!;
        Assert.True(result);
    }

    // ── RemoveHonorifics ──

    [Fact]
    public void RemoveHonorifics_WithTitle_RemovesIt()
    {
        var tokens = new List<string> { "mr", "smith" };
        var result = (List<string>)InvokePrivateStatic("RemoveHonorifics", tokens)!;
        Assert.Single(result);
        Assert.Equal("smith", result[0]);
    }

    [Fact]
    public void RemoveHonorifics_NoTitle_ReturnsAll()
    {
        var tokens = new List<string> { "john", "smith" };
        var result = (List<string>)InvokePrivateStatic("RemoveHonorifics", tokens)!;
        Assert.Equal(2, result.Count);
    }

    // ── AreEquivalentCharacterNames ──

    [Fact]
    public void AreEquivalentCharacterNames_ExactMatch_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "John Smith", "John Smith")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_CaseDifference_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "john smith", "JOHN SMITH")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_NicknameMatch_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "Bob", "Robert")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_NicknameWithLastName_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "Bob Smith", "Robert Smith")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_LastNameOnly_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "Smith", "John Smith")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_FirstNameOnly_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "John", "John Smith")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_HonorificIgnored_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "Mr. Smith", "Smith")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_CompletelyDifferent_ReturnsFalse()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "Alice Johnson", "Bob Smith")!;
        Assert.False(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_EmptyToken_ReturnsFalse()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "", "Bob")!;
        Assert.False(result);
    }

    [Fact]
    public void AreEquivalentCharacterNames_DrSmith_Smith_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentCharacterNames", "Dr. Smith", "Smith")!;
        Assert.True(result);
    }

    // ── AreEquivalentLocationNames ──

    [Fact]
    public void AreEquivalentLocationNames_ExactMatch_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentLocationNames", "Castle Rock", "Castle Rock")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentLocationNames_CaseDifference_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentLocationNames", "castle rock", "CASTLE ROCK")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentLocationNames_WithPunctuation_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentLocationNames", "Castle Rock!", "Castle Rock")!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentLocationNames_DifferentNames_ReturnsFalse()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentLocationNames", "Castle Rock", "Mountain View")!;
        Assert.False(result);
    }

    // ── AreEquivalentNames (dispatch) ──

    [Fact]
    public void AreEquivalentNames_Character_UsesCharacterLogic()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentNames", "Bob", "Robert", EntityType.Character)!;
        Assert.True(result);
    }

    [Fact]
    public void AreEquivalentNames_Location_UsesLocationLogic()
    {
        var result = (bool)InvokePrivateStatic("AreEquivalentNames", "castle rock", "CASTLE ROCK", EntityType.Location)!;
        Assert.True(result);
    }

    // ── ClampConfidence ──

    [Fact]
    public void ClampConfidence_NormalValue_ReturnsSame()
    {
        var result = (double)InvokePrivateStatic("ClampConfidence", 0.5)!;
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void ClampConfidence_Above1_Returns1()
    {
        var result = (double)InvokePrivateStatic("ClampConfidence", 1.5)!;
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ClampConfidence_Below0_Returns0()
    {
        var result = (double)InvokePrivateStatic("ClampConfidence", -0.5)!;
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void ClampConfidence_NaN_Returns0()
    {
        var result = (double)InvokePrivateStatic("ClampConfidence", double.NaN)!;
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void ClampConfidence_Infinity_Returns0()
    {
        // Infinity is treated same as NaN — returns 0
        var result = (double)InvokePrivateStatic("ClampConfidence", double.PositiveInfinity)!;
        Assert.Equal(0.0, result);
    }

    // ── GetCharacterNameSpecificityScore ──

    [Fact]
    public void GetCharacterNameSpecificityScore_SingleToken_Returns10()
    {
        var result = (int)InvokePrivateStatic("GetCharacterNameSpecificityScore", "Alice")!;
        Assert.Equal(10, result);
    }

    [Fact]
    public void GetCharacterNameSpecificityScore_TwoTokens_Returns40()
    {
        // 2 tokens * 10 + 20 bonus for 2+ tokens = 40
        var result = (int)InvokePrivateStatic("GetCharacterNameSpecificityScore", "Alice Johnson")!;
        Assert.Equal(40, result);
    }

    [Fact]
    public void GetCharacterNameSpecificityScore_WithHonorific_SubtractsFive()
    {
        // Mr. Smith = 2 tokens * 10 + 20 - 5 = 35
        var result = (int)InvokePrivateStatic("GetCharacterNameSpecificityScore", "Mr. Smith")!;
        Assert.Equal(35, result);
    }

    [Fact]
    public void GetCharacterNameSpecificityScore_ThreeTokens_Returns50()
    {
        // 3 tokens * 10 + 20 = 50
        var result = (int)InvokePrivateStatic("GetCharacterNameSpecificityScore", "John Jacob Smith")!;
        Assert.Equal(50, result);
    }

    // ── PickBetterName ──

    [Fact]
    public void PickBetterName_Location_PicksLonger()
    {
        var result = (string)InvokePrivateStatic("PickBetterName", "Castle", "Castle Rock", EntityType.Location)!;
        Assert.Equal("Castle Rock", result);
    }

    [Fact]
    public void PickBetterName_Character_PicksHigherSpecificity()
    {
        // "John Smith" = score 40, "John" = score 10
        var result = (string)InvokePrivateStatic("PickBetterName", "John", "John Smith", EntityType.Character)!;
        Assert.Equal("John Smith", result);
    }

    [Fact]
    public void PickBetterName_Character_EqualScore_EqualLength_PicksLeft()
    {
        // When scores and lengths are equal, PickBetterName returns left
        var result = (string)InvokePrivateStatic("PickBetterName", "Alice", "Bobby", EntityType.Character)!;
        Assert.Equal("Alice", result);
    }

    // ── PickBetterDescription ──

    [Fact]
    public void PickBetterDescription_CurrentEmpty_ReturnsIncoming()
    {
        var result = (string)InvokePrivateStatic("PickBetterDescription", "", "A brave warrior")!;
        Assert.Equal("A brave warrior", result);
    }

    [Fact]
    public void PickBetterDescription_IncomingEmpty_ReturnsCurrent()
    {
        var result = (string)InvokePrivateStatic("PickBetterDescription", "A brave warrior", "")!;
        Assert.Equal("A brave warrior", result);
    }

    [Fact]
    public void PickBetterDescription_BothPresent_PicksLonger()
    {
        var result = (string)InvokePrivateStatic("PickBetterDescription", "Short", "A longer description here")!;
        Assert.Equal("A longer description here", result);
    }

    // ── PickBetterQuote ──

    [Fact]
    public void PickBetterQuote_CurrentEmpty_ReturnsIncoming()
    {
        var result = (string)InvokePrivateStatic("PickBetterQuote", "", "He said hello")!;
        Assert.Equal("He said hello", result);
    }

    [Fact]
    public void PickBetterQuote_IncomingLonger_PicksIncoming()
    {
        var result = (string)InvokePrivateStatic("PickBetterQuote", "Hi", "He said hello")!;
        Assert.Equal("He said hello", result);
    }

    // ── BuildSummaryJson / ExtractSummary ──

    [Fact]
    public void BuildSummaryJson_WithDescription_ContainsSummaryKey()
    {
        var result = (string)InvokePrivateStatic("BuildSummaryJson", "A brave hero")!;
        Assert.Contains("\"summary\"", result);
        Assert.Contains("A brave hero", result);
    }

    [Fact]
    public void BuildSummaryJson_Null_ReturnsEmptySummary()
    {
        var result = (string)InvokePrivateStatic("BuildSummaryJson", (string?)null)!;
        Assert.Contains("\"summary\":\"\"", result);
    }

    [Fact]
    public void ExtractSummary_ValidJson_ReturnsSummary()
    {
        var json = "{\"summary\":\"A brave hero\"}";
        var result = (string)InvokePrivateStatic("ExtractSummary", json)!;
        Assert.Equal("A brave hero", result);
    }

    [Fact]
    public void ExtractSummary_Null_ReturnsEmpty()
    {
        var result = (string)InvokePrivateStatic("ExtractSummary", (string?)null)!;
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractSummary_InvalidJson_ReturnsRaw()
    {
        var result = (string)InvokePrivateStatic("ExtractSummary", "not json")!;
        Assert.Equal("not json", result);
    }

    [Fact]
    public void ExtractSummary_MissingSummaryKey_ReturnsRaw()
    {
        var json = "{\"other\":\"value\"}";
        var result = (string)InvokePrivateStatic("ExtractSummary", json)!;
        Assert.Equal(json, result);
    }

    // ── ShouldReplaceCanonicalName ──

    [Fact]
    public void ShouldReplaceCanonicalName_BetterNew_ReturnsTrue()
    {
        var result = (bool)InvokePrivateStatic("ShouldReplaceCanonicalName", "John", "John Smith", EntityType.Character)!;
        Assert.True(result);
    }

    [Fact]
    public void ShouldReplaceCanonicalName_ExistingBetter_ReturnsFalse()
    {
        var result = (bool)InvokePrivateStatic("ShouldReplaceCanonicalName", "John Smith", "John", EntityType.Character)!;
        Assert.False(result);
    }

    // ── DeduplicateExtractedEntities ──

    [Fact]
    public void DeduplicateExtractedEntities_Null_ReturnsEmptyList()
    {
        var method = ServiceType.GetMethod("DeduplicateExtractedEntities",
            BindingFlags.Static | BindingFlags.NonPublic);
        var result = (List<AiExtractedEntity>)method!.Invoke(null, new object?[] { null, EntityType.Character })!;
        Assert.Empty(result);
    }

    [Fact]
    public void DeduplicateExtractedEntities_EmptyNames_Filtered()
    {
        var entities = new List<AiExtractedEntity>
        {
            new() { Name = "", Description = "test", Confidence = 0.8 },
            new() { Name = "   ", Description = "test2", Confidence = 0.7 }
        };

        var method = ServiceType.GetMethod("DeduplicateExtractedEntities",
            BindingFlags.Static | BindingFlags.NonPublic);
        var result = (List<AiExtractedEntity>)method!.Invoke(null,
            new object[] { entities.AsEnumerable(), EntityType.Character })!;
        Assert.Empty(result);
    }

    [Fact]
    public void DeduplicateExtractedEntities_DuplicateNames_Merged()
    {
        var entities = new List<AiExtractedEntity>
        {
            new() { Name = "Bob Smith", Description = "A man", SourceQuote = "Hi", Confidence = 0.7 },
            new() { Name = "Robert Smith", Description = "A tall man who works hard", SourceQuote = "Hello there friend", Confidence = 0.9 }
        };

        var method = ServiceType.GetMethod("DeduplicateExtractedEntities",
            BindingFlags.Static | BindingFlags.NonPublic);
        var result = (List<AiExtractedEntity>)method!.Invoke(null,
            new object[] { entities.AsEnumerable(), EntityType.Character })!;

        Assert.Single(result);
        Assert.Equal(0.9, result[0].Confidence);
    }

    [Fact]
    public void DeduplicateExtractedEntities_UniqueNames_AllKept()
    {
        var entities = new List<AiExtractedEntity>
        {
            new() { Name = "Alice", Description = "Hero", Confidence = 0.8 },
            new() { Name = "Bob", Description = "Villain", Confidence = 0.9 }
        };

        var method = ServiceType.GetMethod("DeduplicateExtractedEntities",
            BindingFlags.Static | BindingFlags.NonPublic);
        var result = (List<AiExtractedEntity>)method!.Invoke(null,
            new object[] { entities.AsEnumerable(), EntityType.Character })!;

        Assert.Equal(2, result.Count);
    }

    // ── FindBestMatchingEntity ──

    [Fact]
    public void FindBestMatchingEntity_MatchExists_ReturnsIt()
    {
        var entities = new List<Entity>
        {
            new() { Id = 1, Name = "Robert Smith", Type = EntityType.Character },
            new() { Id = 2, Name = "Alice Johnson", Type = EntityType.Character }
        };

        var method = ServiceType.GetMethod("FindBestMatchingEntity",
            BindingFlags.Static | BindingFlags.NonPublic);
        var result = (Entity?)method!.Invoke(null,
            new object[] { entities, "Bob Smith", EntityType.Character });

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
    }

    [Fact]
    public void FindBestMatchingEntity_NoMatch_ReturnsNull()
    {
        var entities = new List<Entity>
        {
            new() { Id = 1, Name = "Alice Johnson", Type = EntityType.Character }
        };

        var method = ServiceType.GetMethod("FindBestMatchingEntity",
            BindingFlags.Static | BindingFlags.NonPublic);
        var result = (Entity?)method!.Invoke(null,
            new object[] { entities, "Bob Smith", EntityType.Character });

        Assert.Null(result);
    }

    // ── MergeExtractedEntities ──

    [Fact]
    public void MergeExtractedEntities_PicksHigherConfidence()
    {
        var current = new AiExtractedEntity { Name = "Bob", Description = "A man", SourceQuote = "Hi", Confidence = 0.7 };
        var incoming = new AiExtractedEntity { Name = "Robert", Description = "A tall man", SourceQuote = "Hello", Confidence = 0.9 };

        var method = ServiceType.GetMethod("MergeExtractedEntities",
            BindingFlags.Static | BindingFlags.NonPublic);
        var result = (AiExtractedEntity)method!.Invoke(null,
            new object[] { current, incoming, EntityType.Character })!;

        Assert.Equal(0.9, result.Confidence);
    }

    [Fact]
    public void MergeExtractedEntities_PicksLongerDescription()
    {
        var current = new AiExtractedEntity { Name = "Bob", Description = "Short", SourceQuote = "Hi", Confidence = 0.7 };
        var incoming = new AiExtractedEntity { Name = "Robert", Description = "A much longer description", SourceQuote = "Hello", Confidence = 0.5 };

        var method = ServiceType.GetMethod("MergeExtractedEntities",
            BindingFlags.Static | BindingFlags.NonPublic);
        var result = (AiExtractedEntity)method!.Invoke(null,
            new object[] { current, incoming, EntityType.Character })!;

        Assert.Equal("A much longer description", result.Description);
    }
}
