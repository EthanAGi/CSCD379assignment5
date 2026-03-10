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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Project>()
            .Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Entity<Chapter>()
            .Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Entity<Chapter>()
            .Property(c => c.Content)
            .IsRequired();

        builder.Entity<Chapter>()
            .HasOne(c => c.Project)
            .WithMany(p => p.Chapters)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Entity>()
            .HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}