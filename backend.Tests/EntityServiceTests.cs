using CanonGuard.Api.DTOs;
using CanonGuard.Api.Models;
using CanonGuard.Api.Services;
using CanonGuard.Tests.Helpers;

namespace CanonGuard.Tests;

public class EntityServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly EntityService _service;
    private readonly string _userId = "user-1";
    private readonly int _projectId;

    public EntityServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new EntityService(_db);

        var project = new Project { OwnerId = _userId, Title = "Test Project" };
        _db.Projects.Add(project);
        _db.SaveChanges();
        _projectId = project.Id;
    }

    public void Dispose() => _db.Dispose();

    // ── GetForProjectAsync ──

    [Fact]
    public async Task GetForProjectAsync_OwnerWithEntities_ReturnsList()
    {
        _db.Entities.Add(new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" });
        _db.Entities.Add(new Entity { ProjectId = _projectId, Type = EntityType.Location, Name = "Castle" });
        await _db.SaveChangesAsync();

        var result = await _service.GetForProjectAsync(_userId, _projectId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetForProjectAsync_WrongUser_ReturnsEmptyList()
    {
        _db.Entities.Add(new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" });
        await _db.SaveChangesAsync();

        var result = await _service.GetForProjectAsync("other-user", _projectId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetForProjectAsync_NoEntities_ReturnsEmptyList()
    {
        var result = await _service.GetForProjectAsync(_userId, _projectId);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetForProjectAsync_OrdersByTypeThenName()
    {
        _db.Entities.Add(new Entity { ProjectId = _projectId, Type = EntityType.Location, Name = "Zeta" });
        _db.Entities.Add(new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" });
        _db.Entities.Add(new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Bob" });
        await _db.SaveChangesAsync();

        var result = await _service.GetForProjectAsync(_userId, _projectId);

        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
        Assert.Equal("Zeta", result[2].Name);
    }

    // ── CreateAsync ──

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsEntity()
    {
        var request = new CreateEntityRequest { Type = "Character", Name = "Alice", Summary = "A brave hero" };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.Name);
        Assert.Equal("Character", result.Type);
        Assert.Contains("A brave hero", result.Summary);
    }

    [Fact]
    public async Task CreateAsync_WrongUser_ReturnsNull()
    {
        var request = new CreateEntityRequest { Type = "Character", Name = "Alice" };

        var result = await _service.CreateAsync("other-user", _projectId, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ReturnsNull()
    {
        var request = new CreateEntityRequest { Type = "Character", Name = "   " };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_InvalidType_ReturnsNull()
    {
        var request = new CreateEntityRequest { Type = "InvalidType", Name = "Alice" };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_NullName_ReturnsNull()
    {
        var request = new CreateEntityRequest { Type = "Character", Name = null! };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_TrimsName()
    {
        var request = new CreateEntityRequest { Type = "Character", Name = "  Alice  " };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.Name);
    }

    [Fact]
    public async Task CreateAsync_LocationType_Works()
    {
        var request = new CreateEntityRequest { Type = "Location", Name = "Castle Rock" };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.NotNull(result);
        Assert.Equal("Location", result!.Type);
    }

    // ── UpdateAsync ──

    [Fact]
    public async Task UpdateAsync_ValidUpdate_ReturnsUpdatedEntity()
    {
        var entity = new Entity
        {
            ProjectId = _projectId,
            Type = EntityType.Character,
            Name = "Alice",
            SummaryJson = "{\"summary\":\"old\"}"
        };
        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        var request = new UpdateEntityRequest
        {
            ProjectId = _projectId,
            Type = "Character",
            Name = "Alice Updated",
            Summary = "new summary"
        };

        var result = await _service.UpdateAsync(_userId, entity.Id, request);

        Assert.NotNull(result);
        Assert.Equal("Alice Updated", result!.Name);
        Assert.Contains("new summary", result.Summary);
    }

    [Fact]
    public async Task UpdateAsync_WrongUser_ReturnsNull()
    {
        var entity = new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" };
        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        var request = new UpdateEntityRequest { ProjectId = _projectId, Type = "Character", Name = "Alice" };

        var result = await _service.UpdateAsync("other-user", entity.Id, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_EmptyName_ReturnsNull()
    {
        var entity = new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" };
        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        var request = new UpdateEntityRequest { ProjectId = _projectId, Type = "Character", Name = "  " };

        var result = await _service.UpdateAsync(_userId, entity.Id, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_InvalidType_ReturnsNull()
    {
        var entity = new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" };
        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        var request = new UpdateEntityRequest { ProjectId = _projectId, Type = "BadType", Name = "Alice" };

        var result = await _service.UpdateAsync(_userId, entity.Id, request);

        Assert.Null(result);
    }

    // ── DeleteAsync ──

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsTrue()
    {
        var entity = new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" };
        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(_userId, entity.Id);

        Assert.True(result);
        Assert.Null(await _db.Entities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task DeleteAsync_WrongUser_ReturnsFalse()
    {
        var entity = new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" };
        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync("other-user", entity.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(_userId, 9999);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_CascadesDeleteToFacts()
    {
        var entity = new Entity { ProjectId = _projectId, Type = EntityType.Character, Name = "Alice" };
        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        var chapter = new Chapter { ProjectId = _projectId, Title = "Ch1", Content = "content" };
        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        _db.Facts.Add(new Fact
        {
            ProjectId = _projectId,
            EntityId = entity.Id,
            FactType = "mention",
            Value = "Alice",
            SourceChapterId = chapter.Id,
            SourceQuote = "quote",
            Confidence = 0.9
        });
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(_userId, entity.Id);

        Assert.True(result);
        Assert.Empty(_db.Facts.Where(f => f.EntityId == entity.Id));
    }

    // ── MapToResponse (tested through CreateAsync) ──

    [Fact]
    public async Task CreateAsync_MapsAllFields()
    {
        var request = new CreateEntityRequest { Type = "Location", Name = "Castle", Summary = "A dark castle" };

        var result = await _service.CreateAsync(_userId, _projectId, request);

        Assert.NotNull(result);
        Assert.True(result!.Id > 0);
        Assert.Equal(_projectId, result.ProjectId);
        Assert.Equal("Location", result.Type);
        Assert.Equal("Castle", result.Name);
        Assert.Contains("A dark castle", result.Summary);
        Assert.True(result.UpdatedAt <= DateTime.UtcNow);
    }
}
