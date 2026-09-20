using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Features.Imports.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;
using DashboardConfig = GZCTF.Features.Dashboard.Domain.Dashboard;

namespace GZCTF.Features.LearningPaths.Infrastructure;

internal static class LearningModelConfiguration
{
    internal static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CanonicalChallenge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Type).HasConversion<byte>();
            entity.Property(e => e.Difficulty).HasConversion<byte>();
            entity.Property(e => e.PublicationState).HasConversion<byte>();
            entity.Property(e => e.RuntimeConfigurationJson).HasColumnType("jsonb");
            entity.Property(e => e.SourceType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.SourceId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.SourceName).HasMaxLength(256);
            entity.Property(e => e.SourceMetadataJson).HasColumnType("jsonb");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();

            entity.HasMany(e => e.Localizations)
                .WithOne(e => e.Challenge)
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Flags)
                .WithOne(e => e.Challenge)
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Hints)
                .WithOne(e => e.Challenge)
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Writeups)
                .WithOne(e => e.Challenge)
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChallengeLocalization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => new { e.ChallengeId, e.Locale }).IsUnique();
        });

        modelBuilder.Entity<ChallengeFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Kind).HasConversion<byte>();
            entity.Property(e => e.Value).HasMaxLength(512);
            entity.Property(e => e.Template).HasMaxLength(Limits.MaxFlagTemplateLength);
            entity.Property(e => e.AttachmentPoolKey).HasMaxLength(256);
            entity.Property(e => e.MetadataJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<ChallengeHint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => new { e.ChallengeId, e.Locale, e.SortOrder }).IsUnique();
        });

        modelBuilder.Entity<ChallengeWriteup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => new { e.ChallengeId, e.Locale }).IsUnique();
        });

        modelBuilder.Entity<LearningPath>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Slug).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();

            entity.HasOne(e => e.CurrentPublishedRevision)
                .WithMany()
                .HasForeignKey(e => e.CurrentPublishedRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Localizations)
                .WithOne(e => e.Path)
                .HasForeignKey(e => e.PathId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Revisions)
                .WithOne(e => e.Path)
                .HasForeignKey(e => e.PathId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Enrollments)
                .WithOne(e => e.Path)
                .HasForeignKey(e => e.PathId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LearningPathLocalization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => new { e.PathId, e.Locale }).IsUnique();
        });

        modelBuilder.Entity<LearningPathRevision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasIndex(e => e.PathId)
                .IsUnique()
                .HasFilter($"\"Status\" = {(byte)LearningPathRevisionStatus.Draft}");
            entity.HasMany(e => e.Modules)
                .WithOne(e => e.Revision)
                .HasForeignKey(e => e.RevisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningModule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => new { e.RevisionId, e.SortOrder }).IsUnique();
            entity.HasMany(e => e.Localizations)
                .WithOne(e => e.Module)
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Module)
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningModuleLocalization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => new { e.ModuleId, e.Locale }).IsUnique();
        });

        modelBuilder.Entity<ModuleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => new { e.ModuleId, e.SortOrder }).IsUnique();
            entity.HasOne(e => e.Lesson)
                .WithMany(e => e.ModuleItems)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Challenge)
                .WithMany()
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ModuleItems_ExactlyOneContent",
                "(\"LessonId\" IS NOT NULL AND \"ChallengeId\" IS NULL) OR (\"LessonId\" IS NULL AND \"ChallengeId\" IS NOT NULL)"));
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasMany(e => e.Localizations)
                .WithOne(e => e.Lesson)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Progress)
                .WithOne(e => e.Lesson)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonLocalization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => new { e.LessonId, e.Locale }).IsUnique();
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => new { e.UserId, e.PathId }).IsUnique();
            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasFilter("\"IsCurrent\" = TRUE");
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => new { e.UserId, e.LessonId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChallengeProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SolveMode).HasConversion<byte>();
            entity.Property(e => e.AttributionMetadataJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.UserId, e.ChallengeId }).IsUnique();
            entity.HasIndex(e => e.SolvedAtUtc);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Challenge)
                .WithMany()
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cohort>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<DashboardConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.DisplaySettingsJson).HasColumnType("jsonb");
            entity.HasMany(e => e.Tokens)
                .WithOne(e => e.Dashboard)
                .HasForeignKey(e => e.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DashboardToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
        });

        modelBuilder.Entity<LearnerDailySolveStat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Date).HasColumnType("date");
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MigrationBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SourceType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.PackageFingerprintSha256).HasMaxLength(64).IsRequired();
            entity.Property(e => e.State).HasConversion<byte>();
            entity.Property(e => e.WarningsJson).HasColumnType("jsonb");
            entity.Property(e => e.ErrorsJson).HasColumnType("jsonb");
            entity.HasIndex(e => e.PackageFingerprintSha256).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_MigrationBatches_State",
                $"\"State\" BETWEEN 0 AND {(byte)MigrationBatchState.Failed}"));
        });

        modelBuilder.Entity<LegacyChallengeMap>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SourceType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.SourceId).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => new { e.SourceType, e.SourceId }).IsUnique();
            entity.HasOne(e => e.Challenge)
                .WithMany()
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.MigrationBatch)
                .WithMany(e => e.ChallengeMappings)
                .HasForeignKey(e => e.MigrationBatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LegacyPathMap>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SourceType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.SourceId).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => new { e.SourceType, e.SourceId }).IsUnique();
            entity.HasOne(e => e.Path)
                .WithMany()
                .HasForeignKey(e => e.PathId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.MigrationBatch)
                .WithMany(e => e.PathMappings)
                .HasForeignKey(e => e.MigrationBatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
