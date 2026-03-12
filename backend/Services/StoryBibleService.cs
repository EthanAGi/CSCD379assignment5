using System.Text.Json;
using System.Text.RegularExpressions;
using CanonGuard.Api.Data;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using CanonGuard.Api.Models;
using CanonGuard.Api.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Services;

public class StoryBibleService : IStoryBibleService
{
    private readonly AppDbContext _db;
    private readonly AzureChapterExtractionService _azureChapterExtractionService;

    private static readonly HashSet<string> Honorifics = new(StringComparer.OrdinalIgnoreCase)
    {
        "mr", "mr.", "mrs", "mrs.", "ms", "ms.", "miss", "dr", "dr.",
        "prof", "prof.", "sir", "lady", "lord", "capt", "capt.", "captain"
    };

    private static readonly Dictionary<string, string> NicknameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ed"] = "edward",
        ["eddie"] = "edward",
        ["ben"] = "benjamin",
        ["benny"] = "benjamin",
        ["alex"] = "alexander",
        ["liz"] = "elizabeth",
        ["beth"] = "elizabeth",
        ["lizzy"] = "elizabeth",
        ["kate"] = "katherine",
        ["katie"] = "katherine",
        ["mike"] = "michael",
        ["mikey"] = "michael",
        ["sam"] = "samuel",
        ["dave"] = "david",
        ["charlie"] = "charles",
        ["joe"] = "joseph",
        ["joey"] = "joseph",
        ["tom"] = "thomas",
        ["will"] = "william",
        ["bill"] = "william",
        ["billy"] = "william",
        ["rob"] = "robert",
        ["bob"] = "robert",
        ["bobby"] = "robert",
        ["rick"] = "richard",
        ["ricky"] = "richard",
        ["rich"] = "richard",
        ["jim"] = "james",
        ["jimmy"] = "james",
        ["jack"] = "john",
        ["johnny"] = "john",
        ["lucy"] = "lucille"
    };

    public StoryBibleService(
        AppDbContext db,
        AzureChapterExtractionService azureChapterExtractionService)
    {
        _db = db;
        _azureChapterExtractionService = azureChapterExtractionService;
    }

    public async Task<ChapterEntityExtractionResponse?> ExtractFromChapterAsync(
        string userId,
        int chapterId,
        CancellationToken cancellationToken = default)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Project)
            .FirstOrDefaultAsync(
                c => c.Id == chapterId && c.Project.OwnerId == userId,
                cancellationToken);

        if (chapter == null)
        {
            return null;
        }

        var existingEntities = await _db.Entities
            .Where(e => e.ProjectId == chapter.ProjectId)
            .ToListAsync(cancellationToken);

        var existingCharacterProfiles = existingEntities
            .Where(e => e.Type == EntityType.Character)
            .ToDictionary(
                e => e.Name,
                e => ExtractSummary(e.SummaryJson),
                StringComparer.OrdinalIgnoreCase);

        var existingLocationProfiles = existingEntities
            .Where(e => e.Type == EntityType.Location)
            .ToDictionary(
                e => e.Name,
                e => ExtractSummary(e.SummaryJson),
                StringComparer.OrdinalIgnoreCase);

        var aiResult = await _azureChapterExtractionService.ExtractAsync(
            chapter.Title,
            chapter.Content,
            existingCharacterProfiles,
            existingLocationProfiles,
            cancellationToken);

        var normalizedCharacters = DeduplicateExtractedEntities(
            aiResult.Characters,
            EntityType.Character);

        var normalizedLocations = DeduplicateExtractedEntities(
            aiResult.Locations,
            EntityType.Location);

        foreach (var character in normalizedCharacters)
        {
            var entity = await UpsertEntityAsync(
                chapter.ProjectId,
                EntityType.Character,
                character.Name,
                character.Description,
                cancellationToken);

            await AddFactIfMissingAsync(
                chapter.ProjectId,
                entity.Id,
                "mention",
                character.Name,
                chapter.Id,
                character.SourceQuote,
                character.Confidence,
                cancellationToken);
        }

        foreach (var location in normalizedLocations)
        {
            var entity = await UpsertEntityAsync(
                chapter.ProjectId,
                EntityType.Location,
                location.Name,
                location.Description,
                cancellationToken);

            await AddFactIfMissingAsync(
                chapter.ProjectId,
                entity.Id,
                "mention",
                location.Name,
                chapter.Id,
                location.SourceQuote,
                location.Confidence,
                cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ChapterEntityExtractionResponse
        {
            ChapterId = chapter.Id,
            ProjectId = chapter.ProjectId,
            Characters = normalizedCharacters.Select(c => new ExtractedEntityDto
            {
                Name = c.Name,
                Description = c.Description,
                SourceQuote = c.SourceQuote,
                Confidence = c.Confidence
            }).ToList(),
            Locations = normalizedLocations.Select(l => new ExtractedEntityDto
            {
                Name = l.Name,
                Description = l.Description,
                SourceQuote = l.SourceQuote,
                Confidence = l.Confidence
            }).ToList()
        };
    }

    private async Task<Entity> UpsertEntityAsync(
        int projectId,
        EntityType type,
        string rawName,
        string? description,
        CancellationToken cancellationToken)
    {
        var name = CleanDisplayName(rawName);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Entity name cannot be empty.");
        }

        var existingEntities = await _db.Entities
            .Where(e => e.ProjectId == projectId && e.Type == type)
            .ToListAsync(cancellationToken);

        var existing = FindBestMatchingEntity(existingEntities, name, type);

        if (existing != null)
        {
            if (ShouldReplaceCanonicalName(existing.Name, name, type))
            {
                existing.Name = name;
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                existing.SummaryJson = BuildSummaryJson(description);
            }

            existing.UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        var entity = new Entity
        {
            ProjectId = projectId,
            Type = type,
            Name = name,
            SummaryJson = BuildSummaryJson(description),
            UpdatedAt = DateTime.UtcNow
        };

        _db.Entities.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static List<AiExtractedEntity> DeduplicateExtractedEntities(
        IEnumerable<AiExtractedEntity>? entities,
        EntityType type)
    {
        if (entities == null)
        {
            return new List<AiExtractedEntity>();
        }

        var result = new List<AiExtractedEntity>();

        foreach (var entity in entities)
        {
            var cleanedName = CleanDisplayName(entity.Name);
            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                continue;
            }

            var candidate = new AiExtractedEntity
            {
                Name = cleanedName,
                Description = entity.Description?.Trim() ?? string.Empty,
                SourceQuote = entity.SourceQuote?.Trim() ?? string.Empty,
                Confidence = ClampConfidence(entity.Confidence)
            };

            var existingIndex = result.FindIndex(e => AreEquivalentNames(e.Name, candidate.Name, type));
            if (existingIndex >= 0)
            {
                result[existingIndex] = MergeExtractedEntities(result[existingIndex], candidate, type);
            }
            else
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    private static AiExtractedEntity MergeExtractedEntities(
        AiExtractedEntity current,
        AiExtractedEntity incoming,
        EntityType type)
    {
        var betterName = PickBetterName(current.Name, incoming.Name, type);
        var betterDescription = PickBetterDescription(current.Description, incoming.Description);
        var betterQuote = PickBetterQuote(current.SourceQuote, incoming.SourceQuote);
        var betterConfidence = Math.Max(current.Confidence, incoming.Confidence);

        return new AiExtractedEntity
        {
            Name = betterName,
            Description = betterDescription,
            SourceQuote = betterQuote,
            Confidence = betterConfidence
        };
    }

    private static string PickBetterName(string left, string right, EntityType type)
    {
        if (type == EntityType.Location)
        {
            return right.Length > left.Length ? right : left;
        }

        var leftScore = GetCharacterNameSpecificityScore(left);
        var rightScore = GetCharacterNameSpecificityScore(right);

        if (rightScore > leftScore)
        {
            return right;
        }

        if (leftScore > rightScore)
        {
            return left;
        }

        return right.Length > left.Length ? right : left;
    }

    private static int GetCharacterNameSpecificityScore(string name)
    {
        var tokens = TokenizeName(name);
        var score = tokens.Count * 10;

        if (tokens.Count >= 2)
        {
            score += 20;
        }

        if (ContainsHonorific(name))
        {
            score -= 5;
        }

        return score;
    }

    private static string PickBetterDescription(string current, string incoming)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return incoming;
        }

        if (string.IsNullOrWhiteSpace(incoming))
        {
            return current;
        }

        return incoming.Length > current.Length ? incoming : current;
    }

    private static string PickBetterQuote(string current, string incoming)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return incoming;
        }

        if (string.IsNullOrWhiteSpace(incoming))
        {
            return current;
        }

        return incoming.Length > current.Length ? incoming : current;
    }

    private static string CleanDisplayName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        var value = rawName.Trim();
        value = Regex.Replace(value, @"\s+", " ");
        value = value.Trim(',', '.', ';', ':', '!', '?', '"', '\'');

        return value;
    }

    private static bool ContainsHonorific(string name)
    {
        var tokens = TokenizeName(name);
        return tokens.Count > 0 && Honorifics.Contains(tokens[0]);
    }

    private static List<string> TokenizeName(string name)
    {
        return Regex.Split(name.Trim(), @"\s+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(NormalizeToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private static string NormalizeToken(string token)
    {
        var cleaned = token.Trim().Trim('.', ',', ';', ':', '!', '?', '"', '\'');
        cleaned = Regex.Replace(cleaned, @"[^a-zA-Z\-]", "");
        return cleaned.ToLowerInvariant();
    }

    private static string NormalizeFirstName(string token)
    {
        var normalized = NormalizeToken(token);

        if (NicknameMap.TryGetValue(normalized, out var canonical))
        {
            return canonical;
        }

        return normalized;
    }

    private static bool AreEquivalentNames(string left, string right, EntityType type)
    {
        if (type == EntityType.Location)
        {
            return AreEquivalentLocationNames(left, right);
        }

        return AreEquivalentCharacterNames(left, right);
    }

    private static bool AreEquivalentLocationNames(string left, string right)
    {
        var l = NormalizeSimplePhrase(left);
        var r = NormalizeSimplePhrase(right);

        return l == r;
    }

    private static string NormalizeSimplePhrase(string value)
    {
        var cleaned = value.Trim().ToLowerInvariant();
        cleaned = Regex.Replace(cleaned, @"[^\w\s\-]", "");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return cleaned;
    }

    private static bool AreEquivalentCharacterNames(string left, string right)
    {
        var leftTokens = TokenizeName(left);
        var rightTokens = TokenizeName(right);

        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return false;
        }

        var leftTokensNoTitles = RemoveHonorifics(leftTokens);
        var rightTokensNoTitles = RemoveHonorifics(rightTokens);

        if (leftTokensNoTitles.SequenceEqual(rightTokensNoTitles))
        {
            return true;
        }

        if (leftTokensNoTitles.Count == 1 && rightTokensNoTitles.Count == 1)
        {
            return NormalizeFirstName(leftTokensNoTitles[0]) == NormalizeFirstName(rightTokensNoTitles[0]);
        }

        var leftLast = leftTokensNoTitles.Last();
        var rightLast = rightTokensNoTitles.Last();

        var leftFirst = NormalizeFirstName(leftTokensNoTitles.First());
        var rightFirst = NormalizeFirstName(rightTokensNoTitles.First());

        if (leftLast == rightLast)
        {
            if (leftTokensNoTitles.Count == 1 || rightTokensNoTitles.Count == 1)
            {
                return true;
            }

            if (leftFirst == rightFirst)
            {
                return true;
            }
        }

        if (leftTokensNoTitles.Count == 1 && rightTokensNoTitles.Count >= 2)
        {
            return NormalizeFirstName(leftTokensNoTitles[0]) == rightFirst;
        }

        if (rightTokensNoTitles.Count == 1 && leftTokensNoTitles.Count >= 2)
        {
            return NormalizeFirstName(rightTokensNoTitles[0]) == leftFirst;
        }

        return false;
    }

    private static List<string> RemoveHonorifics(List<string> tokens)
    {
        return tokens.Where(t => !Honorifics.Contains(t)).ToList();
    }

    private static Entity? FindBestMatchingEntity(
        List<Entity> existingEntities,
        string name,
        EntityType type)
    {
        return existingEntities.FirstOrDefault(e => AreEquivalentNames(e.Name, name, type));
    }

    private static bool ShouldReplaceCanonicalName(string existingName, string newName, EntityType type)
    {
        var preferred = PickBetterName(existingName, newName, type);
        return !string.Equals(preferred, existingName, StringComparison.Ordinal);
    }

    private static string BuildSummaryJson(string? description)
    {
        var payload = new
        {
            summary = description?.Trim() ?? string.Empty
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string ExtractSummary(string? summaryJson)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(summaryJson);

            if (doc.RootElement.TryGetProperty("summary", out var summaryElement))
            {
                return summaryElement.GetString() ?? string.Empty;
            }

            return summaryJson;
        }
        catch
        {
            return summaryJson;
        }
    }

    private async Task AddFactIfMissingAsync(
        int projectId,
        int entityId,
        string factType,
        string value,
        int sourceChapterId,
        string sourceQuote,
        double confidence,
        CancellationToken cancellationToken)
    {
        var normalizedValue = value?.Trim() ?? string.Empty;
        var normalizedQuote = sourceQuote?.Trim() ?? string.Empty;

        var exists = await _db.Facts.AnyAsync(
            f => f.ProjectId == projectId &&
                 f.EntityId == entityId &&
                 f.SourceChapterId == sourceChapterId &&
                 f.FactType == factType &&
                 f.Value == normalizedValue &&
                 f.SourceQuote == normalizedQuote,
            cancellationToken);

        if (exists)
        {
            return;
        }

        _db.Facts.Add(new Fact
        {
            ProjectId = projectId,
            EntityId = entityId,
            FactType = factType,
            Value = normalizedValue,
            SourceChapterId = sourceChapterId,
            SourceQuote = normalizedQuote,
            Confidence = ClampConfidence(confidence)
        });
    }

    private static double ClampConfidence(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
}