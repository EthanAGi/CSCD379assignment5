using CanonGuard.Api.DTOs;
using CanonGuard.Api.Models;
using CanonGuard.Api.Models.Configuration;
using CanonGuard.Api.Services;
using CanonGuard.Api.Services.AI;
using CanonGuard.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CanonGuard.Tests;

public class ChapterServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly ChapterService _service;
    private readonly string _userId = "user-1";
    private readonly int _projectId;

    public ChapterServiceTests()
    {
        _db = TestDbContextFactory.Create();

        // Build a real ChapterEmbeddingService with a stub EmbeddingService
        // that won't actually call Azure (endpoint is blank so it'll throw if called,
        // but for read/query tests it won't be invoked).
        var stubOptions = Options.Create(new AzureAiOptions());
        var embeddingService = new EmbeddingService(
            new HttpClient(), stubOptions, NullLogger<EmbeddingService>.Instance);
        var chapterEmbeddingService = new ChapterEmbeddingService(
            _db, embeddingService, NullLogger<ChapterEmbeddingService>.Instance);

        _service = new ChapterService(_db, chapterEmbeddingService);

        var project = new Project { OwnerId = _userId, Title = "Test Project" };
        _db.Projects.Add(project);
        _db.SaveChanges();
        _projectId = project.Id;
    }

    public void Dispose() => _db.Dispose();

    // ── GetForProjectAsync ──

    [Fact]
    public async Task GetForProjectAsync_OwnerWithChapters_ReturnsList()
    {
        _db.Chapters.Add(new Chapter { ProjectId = _projectId, Title = "Ch1", Content = "text" });
        _db.Chapters.Add(new Chapter { ProjectId = _projectId, Title = "Ch2", Content = "text" });
        await _db.SaveChangesAsync();

        var result = await _service.GetForProjectAsync(_userId, _projectId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetForProjectAsync_WrongUser_ReturnsEmpty()
    {
        _db.Chapters.Add(new Chapter { ProjectId = _projectId, Title = "Ch1", Content = "text" });
        await _db.SaveChangesAsync();

        var result = await _service.GetForProjectAsync("other-user", _projectId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetForProjectAsync_NoChapters_ReturnsEmpty()
    {
        var result = await _service.GetForProjectAsync(_userId, _projectId);
        Assert.Empty(result);
    }

    // ── GetByIdAsync ──

    [Fact]
    public async Task GetByIdAsync_ExistingChapter_ReturnsChapter()
    {
        var chapter = new Chapter { ProjectId = _projectId, Title = "Ch1", Content = "Hello" };
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(_userId, chapter.Id);

        Assert.NotNull(result);
        Assert.Equal("Ch1", result!.Title);
        Assert.Equal("Hello", result.Content);
    }

    [Fact]
    public async Task GetByIdAsync_WrongUser_ReturnsNull()
    {
        var chapter = new Chapter { ProjectId = _projectId, Title = "Ch1", Content = "Hello" };
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync("other-user", chapter.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(_userId, 9999);
        Assert.Null(result);
    }

    // ── CreateAsync ──

    [Fact]
    public async Task CreateAsync_WrongUser_ReturnsNull()
    {
        var request = new CreateChapterRequest { Title = "Chapter 1" };

        var result = await _service.CreateAsync("other-user", _projectId, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ReturnsNull()
    {
        var request = new CreateChapterRequest { Title = "   " };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_NullTitle_ReturnsNull()
    {
        var request = new CreateChapterRequest { Title = null! };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_NonExistentProject_ReturnsNull()
    {
        var request = new CreateChapterRequest { Title = "Chapter 1" };

        var result = await _service.CreateAsync(_userId, 9999, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsChapterBeforeEmbedding()
    {
        // The service saves the chapter then calls RegenerateChapterEmbeddingsAsync
        // which throws because Azure isn't configured, but the chapter is persisted.
        var request = new CreateChapterRequest { Title = "Chapter 1", Content = "Once upon a time" };

        try
        {
            await _service.CreateAsync(_userId, _projectId, request);
        }
        catch (InvalidOperationException)
        {
            // Expected: embedding service fails
        }

        var persisted = _db.Chapters.FirstOrDefault(c => c.Title == "Chapter 1");
        Assert.NotNull(persisted);
        Assert.Equal("Once upon a time", persisted!.Content);
    }

    // ── UpdateAsync ──

    [Fact]
    public async Task UpdateAsync_WrongUser_ReturnsNull()
    {
        var chapter = new Chapter { ProjectId = _projectId, Title = "Ch1", Content = "text" };
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        var request = new UpdateChapterRequest { Title = "Updated" };

        var result = await _service.UpdateAsync("other-user", chapter.Id, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_EmptyTitle_ReturnsNull()
    {
        var chapter = new Chapter { ProjectId = _projectId, Title = "Ch1", Content = "text" };
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        var request = new UpdateChapterRequest { Title = "  " };

        var result = await _service.UpdateAsync(_userId, chapter.Id, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_NonExistent_ReturnsNull()
    {
        var request = new UpdateChapterRequest { Title = "Updated" };

        var result = await _service.UpdateAsync(_userId, 9999, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ValidUpdate_PersistsBeforeEmbedding()
    {
        var chapter = new Chapter
        {
            ProjectId = _projectId,
            Title = "Ch1",
            Content = "text",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        var request = new UpdateChapterRequest { Title = "Updated Title", Content = "new content" };

        try
        {
            await _service.UpdateAsync(_userId, chapter.Id, request);
        }
        catch (InvalidOperationException)
        {
            // Expected: embedding service fails
        }

        var updated = await _db.Chapters.FindAsync(chapter.Id);
        Assert.Equal("Updated Title", updated!.Title);
        Assert.Equal("new content", updated.Content);
    }
}
