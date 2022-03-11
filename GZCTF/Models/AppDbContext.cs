using Microsoft.AspNetCore.Identity;
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
    public DbSet<Game> Games { get; set; } = default!;
    public DbSet<Instance> Instances { get; set; } = default!;
    public DbSet<Participation> Participations { get; set; } = default!;
    public DbSet<Team> Teams { get; set; } = default!;
    public DbSet<FlagContext> FlagContexts { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserInfo>(entity =>
        {
            entity.Property(e => e.Role)
                .HasConversion<int>();

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Avatar)
                .WithMany()
                .HasForeignKey(e => e.AcatarId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ActiveTeam)
                .WithMany()
                .HasForeignKey(e => e.ActiveTeamId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Game>(entity =>
        {
            entity.HasMany(e => e.Notices)
                .WithOne();

            entity.HasMany(e => e.Challenges)
                .WithMany(e => e.Games);

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.Game)
                .HasForeignKey(e => e.GameId);

            entity.HasMany(e => e.Instances)
                .WithOne(e => e.Game)
                .HasForeignKey(e => e.GameId);

            entity.HasMany(e => e.Teams)
                .WithOne(e => e.Game)
                .HasForeignKey(e => e.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Team>(entity =>
        {
            entity.HasOne(e => e.Avatar);

            entity.HasMany(e => e.Games)
                .WithOne(e => e.Team)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Members)
                .WithMany(e => e.Teams);
        });

        builder.Entity<Participation>(entity =>
        {
            entity.HasMany(e => e.Instances).WithOne();

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.Team)
                .HasForeignKey(e => e.ParticipationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Instance>(entity =>
        {
            entity.HasOne(e => e.Challenge)
               .WithMany()
               .HasForeignKey(e => e.ChallengeId);

            entity.HasOne(e => e.Flag)
                .WithMany()
                .HasForeignKey(e => e.FlagId);
        });

        builder.Entity<Challenge>(entity =>
        {
            entity.HasMany(e => e.Flags)
               .WithOne(e => e.Challenge)
               .HasForeignKey(e => e.ChallengeId);
        });

        builder.Entity<Submission>(entity =>
        {
            entity.HasOne(e => e.User)
               .WithMany()
               .HasForeignKey(e => e.UserId);

            entity.Property(e => e.Status)
                .HasConversion<string>();
        });

        builder.Entity<FlagContext>(entity =>
        {
            entity.HasOne(e => e.Attachment)
               .WithMany()
               .HasForeignKey(e => e.FileId);
        });
    }
}
