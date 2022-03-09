using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Models;

public class AppDbContext : IdentityDbContext<UserInfo>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LogModel> Logs { get; set; } = default!;
    public DbSet<Submission> Submissions { get; set; } = default!;
    public DbSet<Challenge> Challenges { get; set; } = default!;
    public DbSet<Notice> Notices { get; set; } = default!;
    public DbSet<GameNotice> GameNotices { get; set; } = default!;
    public DbSet<LocalFile> Files { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserInfo>(entity =>
        {
            entity.Property(e => e.Privilege)
                .HasConversion<int>();

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
