using GZCTF.Features.SkillTrees.Domain;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.SkillTrees.Infrastructure;

internal static class SkillTreeModelConfiguration
{
    internal static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SkillTree>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.IconKey).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasIndex(x => x.DeletedAtUtc);
            entity.HasOne(x => x.CurrentPublishedRevision).WithMany()
                .HasForeignKey(x => x.CurrentPublishedRevisionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Revisions).WithOne(x => x.SkillTree)
                .HasForeignKey(x => x.SkillTreeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Enrollments).WithOne(x => x.SkillTree)
                .HasForeignKey(x => x.SkillTreeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SkillTrees_IconKey",
                "\"IconKey\" IN ('flag','web','crypto','pwn','brain','ai')"));
        });

        modelBuilder.Entity<SkillTreeRevision>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<byte>();
            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasIndex(x => x.SkillTreeId).IsUnique()
                .HasFilter($"\"Status\" = {(byte)SkillTreeRevisionStatus.Draft}");
        });

        modelBuilder.Entity<SkillCategory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.IconKey).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasIndex(x => x.DeletedAtUtc);
            entity.HasOne(x => x.MergedIntoCategory).WithMany()
                .HasForeignKey(x => x.MergedIntoCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SkillCategories_IconKey",
                "\"IconKey\" IN ('flag','web','crypto','pwn','brain','ai')"));
        });

        modelBuilder.Entity<SkillTreeCategoryRef>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => new { x.RevisionId, x.CategoryId }).IsUnique();
            entity.HasIndex(x => new { x.RevisionId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.Revision).WithMany(x => x.Categories)
                .HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category).WithMany(x => x.SkillTrees)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CategoryContent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => new { x.CategoryId, x.SortOrder }).IsUnique();
            entity.HasIndex(x => new { x.CategoryId, x.ChallengeId }).IsUnique()
                .HasFilter("\"ChallengeId\" IS NOT NULL");
            entity.HasIndex(x => new { x.CategoryId, x.LessonId }).IsUnique()
                .HasFilter("\"LessonId\" IS NOT NULL");
            entity.HasOne(x => x.Category).WithMany(x => x.Contents)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Challenge).WithMany()
                .HasForeignKey(x => x.ChallengeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Lesson).WithMany(x => x.CategoryContents)
                .HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_CategoryContents_ExactlyOneContent",
                "(\"LessonId\" IS NOT NULL AND \"ChallengeId\" IS NULL) OR " +
                "(\"LessonId\" IS NULL AND \"ChallengeId\" IS NOT NULL)"));
        });

        modelBuilder.Entity<SkillTreeEnrollment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => new { x.UserId, x.SkillTreeId }).IsUnique();
            entity.HasIndex(x => x.UserId).IsUnique().HasFilter("\"IsCurrent\" = TRUE");
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LearningPathRedirect>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OldSlug).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.LearningPathId).IsUnique();
            entity.HasIndex(x => x.OldSlug).IsUnique();
            entity.HasOne(x => x.SkillTree).WithMany().HasForeignKey(x => x.SkillTreeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
