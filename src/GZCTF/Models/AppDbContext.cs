using System.Text.Json;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GZCTF.Models;

public class AppDbContext(DbContextOptions<AppDbContext> options) :
    IdentityDbContext<UserInfo, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    public DbSet<Post> Posts { get; set; } = default!;
    public DbSet<Game> Games { get; set; } = default!;
    public DbSet<Team> Teams { get; set; } = default!;
    public DbSet<LogModel> Logs { get; set; } = default!;
    public DbSet<Config> Configs { get; set; } = default!;
    public DbSet<LocalFile> Files { get; set; } = default!;
    public DbSet<CheatInfo> CheatInfo { get; set; } = default!;
    public DbSet<Container> Containers { get; set; } = default!;
    public DbSet<GameEvent> GameEvents { get; set; } = default!;
    public DbSet<Submission> Submissions { get; set; } = default!;
    public DbSet<Attachment> Attachments { get; set; } = default!;
    public DbSet<GameNotice> GameNotices { get; set; } = default!;
    public DbSet<FlagContext> FlagContexts { get; set; } = default!;
    public DbSet<Participation> Participations { get; set; } = default!;
    public DbSet<GameInstance> GameInstances { get; set; } = default!;
    public DbSet<GameChallenge> GameChallenges { get; set; } = default!;
    public DbSet<ExerciseInstance> ExerciseInstances { get; set; } = default!;
    public DbSet<ExerciseChallenge> ExerciseChallenges { get; set; } = default!;
    public DbSet<UserParticipation> UserParticipations { get; set; } = default!;
    public DbSet<ExerciseDependency> ExerciseDependencies { get; set; } = default!;
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    static ValueConverter<T?, string> GetJsonConverter<T>() where T : class, new() =>
        new(
            v => JsonSerializer.Serialize(v ?? new(), JsonOptions),
            v => JsonSerializer.Deserialize<T>(v, JsonOptions)
        );

    static ValueComparer<TList> GetEnumerableComparer<TList, T>()
        where T : notnull
        where TList : IEnumerable<T>, new() =>
        new(
            (c1, c2) => (c1 == null && c2 == null) || (c2 != null && c1 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())));

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ValueConverter<List<string>?, string> listConverter = GetJsonConverter<List<string>>();
        ValueConverter<HashSet<string>?, string> setConverter = GetJsonConverter<HashSet<string>>();
        ValueComparer<List<string>> listComparer = GetEnumerableComparer<List<string>, string>();
        ValueComparer<HashSet<string>> setComparer = GetEnumerableComparer<HashSet<string>, string>();

        builder.Entity<UserInfo>(entity =>
        {
            entity.Property(e => e.Role)
                .HasConversion<int>();

            entity.Property(e => e.UserName)
                .HasMaxLength(16);

            entity.Property(e => e.ExerciseVisible)
                .HasDefaultValue(true);

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
                    e => e.HasOne(p => p.Team)
                        .WithMany(t => t.Participations)
                        .HasForeignKey(p => p.TeamId),
                    e => e.HasOne(p => p.Game)
                        .WithMany(g => g.Participations)
                        .HasForeignKey(p => p.GameId)
                );
        });

        builder.Entity<Post>(entity =>
        {
            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Tags)
                .HasConversion(listConverter)
                .Metadata
                .SetValueComparer(listComparer);

            entity.Navigation(e => e.Author).AutoInclude();
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
                .HasConversion<int>();

            entity.HasMany(e => e.Instances).WithOne();

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.Participation)
                .HasForeignKey(e => e.ParticipationId);

            entity.HasMany(e => e.Members)
                .WithOne(e => e.Participation)
                .HasForeignKey(e => e.ParticipationId);

            entity.HasOne(e => e.Writeup)
                .WithMany();

            entity.Navigation(e => e.Game).AutoInclude();
            entity.Navigation(e => e.Team).AutoInclude();
            entity.Navigation(e => e.Members).AutoInclude();
            entity.Navigation(e => e.Writeup).AutoInclude();

            entity.HasMany(e => e.Challenges)
                .WithMany(e => e.Teams)
                .UsingEntity<GameInstance>(
                    e => e.HasOne(i => i.Challenge)
                        .WithMany(c => c.Instances)
                        .HasForeignKey(i => i.ChallengeId),
                    e => e.HasOne(i => i.Participation)
                        .WithMany(p => p.Instances)
                        .HasForeignKey(i => i.ParticipationId)
                        .OnDelete(DeleteBehavior.Cascade),
                    e => e.HasKey(i => new { i.ChallengeId, i.ParticipationId })
                );
        });

        builder.Entity<GameInstance>(entity =>
        {
            entity.HasOne(e => e.FlagContext)
                .WithMany()
                .HasForeignKey(e => e.FlagId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Container)
                .WithOne(e => e.GameInstance)
                .HasForeignKey<Container>(e => e.GameInstanceId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Navigation(e => e.Container).AutoInclude();
            entity.Navigation(e => e.Challenge).AutoInclude();
        });

        builder.Entity<ExerciseInstance>(entity =>
        {
            entity.HasOne(e => e.Container)
                .WithOne(e => e.ExerciseInstance)
                .HasForeignKey<Container>(e => e.ExerciseInstanceId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.FlagContext)
                .WithMany()
                .HasForeignKey(e => e.FlagId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Navigation(e => e.Container).AutoInclude();
            entity.Navigation(e => e.Exercise).AutoInclude();
        });

        builder.Entity<Container>(entity =>
        {
            entity.HasOne(e => e.GameInstance)
                .WithOne(e => e.Container)
                .HasForeignKey<GameInstance>(e => e.ContainerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ExerciseInstance)
                .WithOne(e => e.Container)
                .HasForeignKey<ExerciseInstance>(e => e.ContainerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GameChallenge>(entity =>
        {
            entity.Property(e => e.Hints)
                .HasConversion(listConverter)
                .Metadata
                .SetValueComparer(listComparer);

            entity.HasMany(e => e.Flags)
                .WithOne(e => e.Challenge)
                .HasForeignKey(e => e.ChallengeId);

            entity.HasMany(e => e.Submissions)
                .WithOne(e => e.GameChallenge)
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

        builder.Entity<ExerciseChallenge>(entity =>
        {
            entity.Property(e => e.Hints)
                .HasConversion(listConverter)
                .Metadata
                .SetValueComparer(listComparer);

            entity.HasOne(e => e.Attachment)
                .WithMany()
                .HasForeignKey(e => e.AttachmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Tags)
                .HasConversion(listConverter)
                .Metadata
                .SetValueComparer(listComparer);

            entity.HasMany(e => e.Flags)
                .WithOne(e => e.Exercise)
                .HasForeignKey(e => e.ExerciseId);

            entity.HasOne(e => e.TestContainer)
                .WithMany()
                .HasForeignKey(e => e.TestContainerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Dependencies)
                .WithMany()
                .UsingEntity<ExerciseDependency>(
                    l => l.HasOne(e => e.Target).WithMany().HasForeignKey(e => e.TargetId),
                    r => r.HasOne(e => e.Source).WithMany().HasForeignKey(e => e.SourceId)
                );

            entity.Navigation(e => e.Attachment).AutoInclude();
            entity.Navigation(e => e.TestContainer).AutoInclude();
        });

        builder.Entity<Submission>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();

            entity.Navigation(e => e.Team).AutoInclude();
            entity.Navigation(e => e.User).AutoInclude();
            entity.Navigation(e => e.GameChallenge).AutoInclude();
        });

        builder.Entity<FlagContext>(entity =>
        {
            entity.HasOne(e => e.Attachment)
                .WithMany()
                .HasForeignKey(e => e.AttachmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Navigation(e => e.Attachment).AutoInclude();
        });

        builder.Entity<Attachment>(entity =>
        {
            entity.HasOne(e => e.LocalFile)
                .WithMany()
                .HasForeignKey(e => e.LocalFileId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Navigation(e => e.LocalFile).AutoInclude();
        });

        builder.Entity<GameNotice>(entity =>
        {
            entity.Property(e => e.Values)
                .HasConversion(listConverter)
                .Metadata
                .SetValueComparer(listComparer);
        });

        builder.Entity<GameEvent>(entity =>
        {
            entity.Property(e => e.Values)
                .HasConversion(listConverter)
                .Metadata
                .SetValueComparer(listComparer);

            entity.HasOne(e => e.Team)
                .WithMany()
                .HasForeignKey(e => e.TeamId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);

            entity.Navigation(e => e.Team).AutoInclude();
            entity.Navigation(e => e.User).AutoInclude();
        });

        builder.Entity<CheatInfo>(entity =>
        {
            entity.HasOne(e => e.Game)
                .WithMany()
                .HasForeignKey(e => e.GameId);

            entity.HasOne(e => e.SourceTeam)
                .WithMany()
                .HasForeignKey(e => e.SourceTeamId);

            entity.HasOne(e => e.SubmitTeam)
                .WithMany()
                .HasForeignKey(e => e.SubmitTeamId);

            entity.HasOne(e => e.Submission)
                .WithMany()
                .HasForeignKey(e => e.SubmissionId);

            entity.HasKey(e => e.SubmissionId);
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
        });
    }
}
