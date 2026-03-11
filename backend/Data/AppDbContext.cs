using CanonGuard.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<Fact> Facts => Set<Fact>();
    public DbSet<Embedding> Embeddings => Set<Embedding>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Description)
                .HasMaxLength(2000);

            entity.HasIndex(p => p.OwnerId);
            entity.HasIndex(p => new { p.OwnerId, p.CreatedAt });
        });

        builder.Entity<Chapter>(entity =>
        {
            entity.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Content)
                .IsRequired();

            entity.HasOne(c => c.Project)
                .WithMany(p => p.Chapters)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.ProjectId);
            entity.HasIndex(c => new { c.ProjectId, c.CreatedAt });
            entity.HasIndex(c => new { c.ProjectId, c.UpdatedAt });
        });

        builder.Entity<Entity>(entity =>
        {
            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.SummaryJson)
                .IsRequired();

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.ProjectId, e.Type, e.Name })
                .IsUnique();
        });

        builder.Entity<Fact>(entity =>
        {
            entity.Property(f => f.FactType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(f => f.Value)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(f => f.SourceQuote)
                .IsRequired();

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Entity)
                .WithMany()
                .HasForeignKey(f => f.EntityId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne<Chapter>()
                .WithMany()
                .HasForeignKey(f => f.SourceChapterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(f => f.ProjectId);
            entity.HasIndex(f => f.EntityId);
            entity.HasIndex(f => f.SourceChapterId);
            entity.HasIndex(f => new { f.ProjectId, f.FactType });
        });

        builder.Entity<Embedding>(entity =>
        {
            entity.Property(e => e.Text)
                .IsRequired();

            entity.Property(e => e.VectorJson)
                .IsRequired();

            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Chapter>()
                .WithMany()
                .HasForeignKey(e => e.ChapterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ChapterId);
            entity.HasIndex(e => new { e.ProjectId, e.CreatedAt });
        });
    }
}