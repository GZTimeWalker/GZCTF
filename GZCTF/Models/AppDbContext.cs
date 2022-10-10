using System.Text.Json;
using CTFServer.Extensions;
using CTFServer.Models.Data;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CTFServer.Models;

public class AppDbContext : IdentityDbContext<UserInfo>, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    private static ValueConverter<T?, string> GetJsonConverter<T>() where T : class, new()
    {
        var options = new JsonSerializerOptions() { WriteIndented = false };
        return new ValueConverter<T?, string>(
                    v => JsonSerializer.Serialize(v ?? new(), options),
                    v => JsonSerializer.Deserialize<T>(v, options)
               );
    }

    private static ValueComparer<TList> GetEnumerableComparer<TList, T>()
        where T : notnull
        where TList : IEnumerable<T>, new()
        => new ValueComparer<TList>(
            (c1, c2) => (c1 == null && c2 == null) || (c2 != null && c1 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())));

    public DbSet<LogModel> Logs { get; set; } = default!;
    public DbSet<Submission> Submissions { get; set; } = default!;
    public DbSet<Challenge> Challenges { get; set; } = default!;
    public DbSet<Post> Posts { get; set; } = default!;
    public DbSet<GameNotice> GameNotices { get; set; } = default!;
    public DbSet<GameEvent> GameEvents { get; set; } = default!;
    public DbSet<LocalFile> Files { get; set; } = default!;
    public DbSet<Game> Games { get; set; } = default!;
    public DbSet<Instance> Instances { get; set; } = default!;
    public DbSet<Participation> Participations { get; set; } = default!;
    public DbSet<Team> Teams { get; set; } = default!;
    public DbSet<FlagContext> FlagContexts { get; set; } = default!;
    public DbSet<Container> Containers { get; set; } = default!;
    public DbSet<Attachment> Attachments { get; set; } = default!;
    public DbSet<Config> Configs { get; set; } = default!;
    public DbSet<UserParticipation> UserParticipations { get; set; } = default!;
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var listConverter = GetJsonConverter<List<string>>();
        var setConverter = GetJsonConverter<HashSet<string>>();
        var listComparer = GetEnumerableComparer<List<string>, string>();
        var setComparer = GetEnumerableComparer<HashSet<string>, string>();

        builder.Entity<UserInfo>(entity =>
        {
            entity.Property(e => e.Role)
                .HasConversion<int>();

            entity.Property(e => e.UserName)
                .HasMaxLength(16);

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Game>(entity =>
        {
            entity.Property(e => e.Organizations)
                .HasConversion(setConverter)
                .Metadata
                .SetValueComparer(setComparer);

            entity.HasMany(e => e.GameEvents)
                .WithOne(e => e.Game)
                .HasForeignKey(e => e.GameId);

            entity.HasMany(e => e.GameNotices)
                .WithOne(e => e.Game)
                .HasForeignKey(e => e.GameId);

            entity.HasMany(e => e.Challenges)
                .WithOne(e => e.Game)
                .HasForeignKey(e => e.GameId);

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.Game)
                .HasForeignKey(e => e.GameId);

            entity.HasMany(e => e.Teams)
                .WithMany(e => e.Games)
                .UsingEntity<Participation>(
                    e => e.HasOne(e => e.Team)
                        .WithMany(e => e.Participations)
                        .HasForeignKey(e => e.TeamId),
                    e => e.HasOne(e => e.Game)
                        .WithMany(e => e.Participations)
                        .HasForeignKey(e => e.GameId),
                    e => e.HasIndex(e => new { e.TeamId, e.GameId })
                );
        });

        builder.Entity<Post>(entity =>
        {
            entity.HasOne(e => e.Auther)
                .WithMany()
                .HasForeignKey(e => e.AutherId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Tags)
                .HasConversion(listConverter)
                .Metadata
                .SetValueComparer(listComparer);

            entity.Navigation(e => e.Auther).AutoInclude();
        });

        builder.Entity<Team>(entity =>
        {
            entity.HasMany(e => e.Members)
                .WithMany(e => e.Teams);

            entity.HasOne(e => e.Captain)
                .WithMany()
                .HasForeignKey(e => e.CaptainId);

            entity.HasOne(e => e.Captain)
                .WithMany()
                .HasForeignKey(e => e.CaptainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Participation>(entity =>
        {
            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.HasMany(e => e.Instances).WithOne();

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.Participation)
                .HasForeignKey(e => e.ParticipationId);

            entity.HasMany(e => e.Members)
                .WithOne(e => e.Participation)
                .HasForeignKey(e => e.ParticipationId);

            entity.Navigation(e => e.Game).AutoInclude();
            entity.Navigation(e => e.Team).AutoInclude();
            entity.Navigation(e => e.Members).AutoInclude();

            entity.HasMany(e => e.Challenges)
                .WithMany(e => e.Teams)
                .UsingEntity<Instance>(
                    e => e.HasOne(e => e.Challenge)
                        .WithMany(c => c.Instances)
                        .HasForeignKey(e => e.ChallengeId),
                    e => e.HasOne(e => e.Participation)
                        .WithMany(e => e.Instances)
                        .HasForeignKey(e => e.ParticipationId)
                        .OnDelete(DeleteBehavior.Cascade),
                    e => e.HasKey(e => new { e.ChallengeId, e.ParticipationId })
                );
        });

        builder.Entity<Instance>(entity =>
        {
            entity.HasOne(e => e.FlagContext)
                .WithMany()
                .HasForeignKey(e => e.FlagId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Container)
                .WithOne(e => e.Instance)
                .HasForeignKey<Container>(e => e.InstanceId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Navigation(e => e.Container).AutoInclude();
            entity.Navigation(e => e.Challenge).AutoInclude();

            entity.HasIndex(e => e.FlagId).IsUnique();
        });

        builder.Entity<UserParticipation>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);

            entity.HasOne(e => e.Team)
                .WithMany()
                .HasForeignKey(e => e.TeamId);

            entity.HasOne(e => e.Game)
                .WithMany()
                .HasForeignKey(e => e.GameId);

            entity.HasKey(e => new { e.GameId, e.TeamId, e.UserId });

            entity.HasIndex(e => new { e.UserId, e.GameId }).IsUnique();
        });

        builder.Entity<Container>(entity =>
        {
            entity.HasOne(e => e.Instance)
                .WithOne(e => e.Container)
                .HasForeignKey<Instance>(e => e.ContainerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.InstanceId);
        });

        builder.Entity<Challenge>(entity =>
        {
            entity.Property(e => e.Hints)
                .HasConversion(listConverter)
                .Metadata
                .SetValueComparer(listComparer);

            entity.HasMany(e => e.Flags)
               .WithOne(e => e.Challenge)
               .HasForeignKey(e => e.ChallengeId);

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.Challenge)
                .HasForeignKey(e => e.ChallengeId);

            entity.HasOne(e => e.Attachment)
                .WithMany()
                .HasForeignKey(e => e.AttachmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TestContainer)
                .WithMany()
                .HasForeignKey(e => e.TestContainerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Navigation(e => e.Attachment).AutoInclude();
            entity.Navigation(e => e.TestContainer).AutoInclude();

            entity.HasIndex(e => e.GameId);
        });

        builder.Entity<Submission>(entity =>
        {
            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Navigation(e => e.Team).AutoInclude();
            entity.Navigation(e => e.User).AutoInclude();
            entity.Navigation(e => e.Challenge).AutoInclude();

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TeamId, e.ChallengeId, e.GameId });
        });

        builder.Entity<FlagContext>(entity =>
        {
            entity.HasOne(e => e.Attachment)
                .WithMany()
                .HasForeignKey(e => e.AttachmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Navigation(e => e.Attachment).AutoInclude();

            entity.HasIndex(e => e.ChallengeId);
        });

        builder.Entity<Attachment>(entity =>
        {
            entity.HasOne(e => e.LocalFile)
                .WithMany()
                .HasForeignKey(e => e.LocalFileId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Navigation(e => e.LocalFile).AutoInclude();
        });

        builder.Entity<GameEvent>(entity =>
        {
            entity.HasOne(e => e.Team)
                .WithMany()
                .HasForeignKey(e => e.TeamId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);

            entity.Navigation(e => e.Team).AutoInclude();
            entity.Navigation(e => e.User).AutoInclude();
        });
    }
}