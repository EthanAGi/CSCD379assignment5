using CanonGuard.Api.DTOs;
using CanonGuard.Api.Models;
using CanonGuard.Api.Services;
using CanonGuard.Tests.Helpers;

namespace CanonGuard.Tests;

public class ProjectServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly ProjectService _service;
    private readonly string _userId = "user-1";

    public ProjectServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new ProjectService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── CreateAsync ──

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsProject()
    {
        var request = new CreateProjectRequest { Title = "My Novel", Description = "A great story" };

        var result = await _service.CreateAsync(_userId, request);

        Assert.True(result.Id > 0);
        Assert.Equal("My Novel", result.Title);
        Assert.Equal("A great story", result.Description);
    }

    [Fact]
    public async Task CreateAsync_NoDescription_ReturnsNullDescription()
    {
        var request = new CreateProjectRequest { Title = "My Novel" };

        var result = await _service.CreateAsync(_userId, request);

        Assert.Null(result.Description);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var request = new CreateProjectRequest { Title = "My Novel" };

        var result = await _service.CreateAsync(_userId, request);

        Assert.True(result.CreatedAt >= before);
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        var request = new CreateProjectRequest { Title = "Persisted" };

        var result = await _service.CreateAsync(_userId, request);

        var found = await _db.Projects.FindAsync(result.Id);
        Assert.NotNull(found);
        Assert.Equal("Persisted", found!.Title);
    }

    // ── GetForUserAsync ──

    [Fact]
    public async Task GetForUserAsync_UserWithProjects_ReturnsList()
    {
        _db.Projects.Add(new Project { OwnerId = _userId, Title = "Project 1" });
        _db.Projects.Add(new Project { OwnerId = _userId, Title = "Project 2" });
        await _db.SaveChangesAsync();

        var result = await _service.GetForUserAsync(_userId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetForUserAsync_DifferentUser_ReturnsEmpty()
    {
        _db.Projects.Add(new Project { OwnerId = _userId, Title = "Project 1" });
        await _db.SaveChangesAsync();

        var result = await _service.GetForUserAsync("other-user");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetForUserAsync_NoProjects_ReturnsEmpty()
    {
        var result = await _service.GetForUserAsync(_userId);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetForUserAsync_OrdersByCreatedAtDescending()
    {
        _db.Projects.Add(new Project { OwnerId = _userId, Title = "Old", CreatedAt = DateTime.UtcNow.AddDays(-2) });
        _db.Projects.Add(new Project { OwnerId = _userId, Title = "New", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _service.GetForUserAsync(_userId);

        Assert.Equal("New", result[0].Title);
        Assert.Equal("Old", result[1].Title);
    }

    [Fact]
    public async Task GetForUserAsync_MapsAllFields()
    {
        _db.Projects.Add(new Project
        {
            OwnerId = _userId,
            Title = "Test",
            Description = "Desc"
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetForUserAsync(_userId);

        Assert.Single(result);
        Assert.Equal("Test", result[0].Title);
        Assert.Equal("Desc", result[0].Description);
        Assert.True(result[0].Id > 0);
    }
}
