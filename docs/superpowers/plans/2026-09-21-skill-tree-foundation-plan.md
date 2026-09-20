# Skill Tree Foundation and Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the skill tree and globally shared category schema, then idempotently backfill the currently implemented LearningPath data without changing current APIs or deleting any existing table.

**Architecture:** Add a parallel `Features/SkillTrees` domain and an additive EF migration. A startup backfill copies current paths into skill trees using stable source IDs, maps every existing module to an independent category, copies ordered content references and enrollments, and records old slug redirects. Current Learning APIs stay active through this packet so schema and migration can ship independently.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL, xUnit, Testcontainers, Docker.

---

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/Features/SkillTrees/Domain/SkillTreeModels.cs` | Skill tree, revision, category, content, enrollment, and redirect entities |
| `src/GZCTF/Features/SkillTrees/Domain/SkillTreeIconCatalog.cs` | Fixed CTF icon keys and validation |
| `src/GZCTF/Features/SkillTrees/Infrastructure/SkillTreeModelConfiguration.cs` | EF keys, relations, checks, indexes, and concurrency tokens |
| `src/GZCTF/Features/SkillTrees/Migration/SkillTreeBackfillService.cs` | Idempotent current-Learning to SkillTree data copy |
| `src/GZCTF/Models/AppDbContext.cs` | DbSets and model configuration entry point |
| `src/GZCTF/Features/LearningPaths/Domain/LearningPathModels.cs` | Add lesson publication state while retaining current Learning entities |
| `src/GZCTF/Extensions/Startup/ServicesExtension.cs` | Register backfill service |
| `src/GZCTF/Utils/PrelaunchHelper.cs` | Run backfill after EF migration and before readiness |
| `src/GZCTF/Migrations/20260921000100_AddSkillTrees.cs` | Additive tables, constraints, indexes, and Lesson publication column |
| `src/GZCTF/Migrations/20260921000100_AddSkillTrees.Designer.cs` | Generated EF migration metadata |
| `src/GZCTF/Migrations/AppDbContextModelSnapshot.cs` | Updated EF snapshot |
| `src/GZCTF.Test/UnitTests/Features/SkillTrees/SkillTreeIconCatalogTests.cs` | Icon allow-list behavior |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeSchemaTests.cs` | PostgreSQL constraint tests |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeBackfillTests.cs` | Count, order, enrollment, redirect, and idempotency tests |

## Task 1: ST01 — Freeze icon and schema invariants

**Files:**
- Create: `src/GZCTF.Test/UnitTests/Features/SkillTrees/SkillTreeIconCatalogTests.cs`
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeSchemaTests.cs`

- [ ] **Step 1: Write the failing icon catalog tests**

```csharp
using GZCTF.Features.SkillTrees.Domain;
using Xunit;

namespace GZCTF.Test.UnitTests.Features.SkillTrees;

public sealed class SkillTreeIconCatalogTests
{
    [Theory]
    [InlineData("flag")]
    [InlineData("web")]
    [InlineData("crypto")]
    [InlineData("pwn")]
    [InlineData("brain")]
    [InlineData("ai")]
    public void Preset_icons_are_accepted(string key) =>
        Assert.True(SkillTreeIconCatalog.IsSupported(key));

    [Theory]
    [InlineData("")]
    [InlineData("🚩")]
    [InlineData("custom")]
    public void Arbitrary_icons_are_rejected(string key) =>
        Assert.False(SkillTreeIconCatalog.IsSupported(key));
}
```

- [ ] **Step 2: Write the failing PostgreSQL schema tests**

Create tests that use `GZCTFApplicationFactory` and `AssertConstraintViolationAsync` with these exact cases:

```csharp
[Fact]
public async Task Tree_revision_has_unique_category_and_sort_order()
{
    await AssertConstraintViolationAsync(async db =>
    {
        var category = new SkillCategory { Name = "Web", IconKey = "web" };
        var revision = new SkillTreeRevision
        {
            SkillTree = new SkillTree { Name = "Beginner", IconKey = "flag" },
            Status = SkillTreeRevisionStatus.Draft
        };
        revision.Categories.Add(new SkillTreeCategoryRef { Category = category, SortOrder = 0 });
        revision.Categories.Add(new SkillTreeCategoryRef { Category = category, SortOrder = 1 });
        db.SkillTreeRevisions.Add(revision);
        await db.SaveChangesAsync();
    });
}

[Fact]
public async Task Category_content_references_exactly_one_content_type()
{
    await AssertConstraintViolationAsync(async db =>
    {
        db.CategoryContents.Add(new CategoryContent
        {
            Category = new SkillCategory { Name = "Web", IconKey = "web" },
            Lesson = new Lesson(),
            Challenge = new Challenge { Type = ChallengeType.StaticAttachment },
            SortOrder = 0
        });
        await db.SaveChangesAsync();
    });
}

[Fact]
public async Task User_has_at_most_one_current_skill_tree()
{
    var user = await TestDataSeeder.CreateUserAsync(
        factory.Services, TestDataSeeder.RandomName(), "SkillTree!123");
    await AssertConstraintViolationAsync(async db =>
    {
        db.SkillTreeEnrollments.AddRange(
            new SkillTreeEnrollment
            {
                UserId = user.Id,
                SkillTree = new SkillTree { Name = "Web", IconKey = "web" },
                IsCurrent = true
            },
            new SkillTreeEnrollment
            {
                UserId = user.Id,
                SkillTree = new SkillTree { Name = "AI", IconKey = "ai" },
                IsCurrent = true
            });
        await db.SaveChangesAsync();
    });
}
```

Also cover duplicate category content, duplicate draft revision, duplicate old slug redirect, and invalid `IconKey` database checks.

Use these exact arrangements for the remaining constraints:

```csharp
[Fact]
public async Task Category_cannot_contain_the_same_challenge_twice()
{
    await AssertConstraintViolationAsync(async db =>
    {
        var category = new SkillCategory { Name = "Web", IconKey = "web" };
        var challenge = new Challenge { Type = ChallengeType.StaticAttachment };
        category.Contents.Add(new CategoryContent { Challenge = challenge, SortOrder = 0 });
        category.Contents.Add(new CategoryContent { Challenge = challenge, SortOrder = 1 });
        db.SkillCategories.Add(category);
        await db.SaveChangesAsync();
    });
}

[Fact]
public async Task Tree_has_at_most_one_draft_revision()
{
    await AssertConstraintViolationAsync(async db =>
    {
        var tree = new SkillTree { Name = "Web", IconKey = "web" };
        tree.Revisions.Add(new SkillTreeRevision { Status = SkillTreeRevisionStatus.Draft });
        tree.Revisions.Add(new SkillTreeRevision { Status = SkillTreeRevisionStatus.Draft });
        db.SkillTrees.Add(tree);
        await db.SaveChangesAsync();
    });
}

[Fact]
public async Task Old_slug_redirect_is_unique()
{
    await AssertConstraintViolationAsync(async db =>
    {
        var tree = new SkillTree { Name = "Web", IconKey = "web" };
        db.LearningPathRedirects.AddRange(
            new LearningPathRedirect { LearningPathId = Guid.CreateVersion7(), OldSlug = "old-web", SkillTree = tree },
            new LearningPathRedirect { LearningPathId = Guid.CreateVersion7(), OldSlug = "old-web", SkillTree = tree });
        await db.SaveChangesAsync();
    });
}

[Fact]
public async Task Unknown_icon_key_is_rejected_by_database()
{
    await AssertConstraintViolationAsync(async db =>
    {
        db.SkillTrees.Add(new SkillTree { Name = "Web", IconKey = "arbitrary" });
        await db.SaveChangesAsync();
    });
}
```

Add this helper inside `SkillTreeSchemaTests` so every expected constraint failure rolls back its transaction and leaves the shared integration database clean:

```csharp
private async Task AssertConstraintViolationAsync(Func<AppDbContext, Task> arrangeAndSave)
{
    using var scope = factory.Services.CreateScope();
    await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await using var transaction = await db.Database.BeginTransactionAsync();
    await Assert.ThrowsAnyAsync<DbUpdateException>(() => arrangeAndSave(db));
    await transaction.RollbackAsync();
}
```

- [ ] **Step 3: Run tests and verify the intended red state**

```bash
docker run --rm \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet test GZCTF.Test/GZCTF.Test.csproj -c Debug \
  --filter FullyQualifiedName~SkillTreeIconCatalogTests
```

Expected: compilation fails because `SkillTreeIconCatalog` does not exist.

Build the integration project after adding the entity types in Task 2, then run `SkillTreeSchemaTests` before Task 3. Expected: database operations fail because the new tables do not exist.

- [ ] **Step 4: Keep the red tests uncommitted until Task 2**

Do not commit the non-compiling red state. Task 2 supplies the smallest domain implementation and commits the icon tests with the domain types. Keep `SkillTreeSchemaTests.cs` uncommitted until Task 3 adds DbSets, mappings, and tables.

## Task 2: ST02 — Add domain entities and icon catalog

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Domain/SkillTreeModels.cs`
- Create: `src/GZCTF/Features/SkillTrees/Domain/SkillTreeIconCatalog.cs`
- Modify: `src/GZCTF/Features/LearningPaths/Domain/LearningPathModels.cs`

- [ ] **Step 1: Add the fixed icon catalog**

```csharp
namespace GZCTF.Features.SkillTrees.Domain;

public static class SkillTreeIconCatalog
{
    public const string Default = "flag";

    public static IReadOnlyDictionary<string, string> Values { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["flag"] = "🚩",
            ["web"] = "🕸️",
            ["crypto"] = "🔐",
            ["pwn"] = "💣",
            ["brain"] = "🧠",
            ["ai"] = "🤖"
        };

    public static bool IsSupported(string? key) =>
        key is not null && Values.ContainsKey(key);
}
```

- [ ] **Step 2: Add the complete foundation entities**

Create `SkillTreeModels.cs` with these public types and properties:

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Models.Data;

namespace GZCTF.Features.SkillTrees.Domain;

public enum SkillTreeRevisionStatus : byte
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public sealed class SkillTree
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string IconKey { get; set; } = SkillTreeIconCatalog.Default;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? CurrentPublishedRevisionId { get; set; }
    public SkillTreeRevision? CurrentPublishedRevision { get; set; }
    [JsonIgnore, Timestamp]
    public uint RowVersion { get; set; }
    public List<SkillTreeRevision> Revisions { get; set; } = [];
    public List<SkillTreeEnrollment> Enrollments { get; set; } = [];
}

public sealed class SkillTreeRevision
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SkillTreeId { get; set; }
    public SkillTree SkillTree { get; set; } = null!;
    public SkillTreeRevisionStatus Status { get; set; } = SkillTreeRevisionStatus.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    [JsonIgnore, Timestamp]
    public uint RowVersion { get; set; }
    public List<SkillTreeCategoryRef> Categories { get; set; } = [];
}

public sealed class SkillCategory
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string IconKey { get; set; } = SkillTreeIconCatalog.Default;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? MergedIntoCategoryId { get; set; }
    public SkillCategory? MergedIntoCategory { get; set; }
    [JsonIgnore, Timestamp]
    public uint RowVersion { get; set; }
    public List<SkillTreeCategoryRef> SkillTrees { get; set; } = [];
    public List<CategoryContent> Contents { get; set; } = [];
}

public sealed class SkillTreeCategoryRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RevisionId { get; set; }
    public SkillTreeRevision Revision { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public SkillCategory Category { get; set; } = null!;
    public int SortOrder { get; set; }
}

public sealed class CategoryContent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CategoryId { get; set; }
    public SkillCategory Category { get; set; } = null!;
    public int SortOrder { get; set; }
    public Guid? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public Guid? ChallengeId { get; set; }
    public Challenge? Challenge { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SkillTreeEnrollment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public UserInfo User { get; set; } = null!;
    public Guid SkillTreeId { get; set; }
    public SkillTree SkillTree { get; set; } = null!;
    public bool IsCurrent { get; set; }
    public DateTimeOffset EnrolledAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LearningPathRedirect
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid LearningPathId { get; set; }
    public string OldSlug { get; set; } = string.Empty;
    public Guid SkillTreeId { get; set; }
    public SkillTree SkillTree { get; set; } = null!;
}
```

- [ ] **Step 3: Add lesson publication state**

Add to `LearningPathModels.cs`:

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<LessonPublicationState>))]
public enum LessonPublicationState : byte
{
    Draft = 0,
    Published = 1,
    Retired = 2
}
```

Also add `using GZCTF.Features.SkillTrees.Domain;` because `Lesson` exposes `CategoryContent` navigation.

Add to `Lesson`:

```csharp
public LessonPublicationState PublicationState { get; set; } = LessonPublicationState.Draft;
[JsonIgnore, Timestamp]
public uint RowVersion { get; set; }
public List<CategoryContent> CategoryContents { get; set; } = [];
```

- [ ] **Step 4: Run icon tests**

Run the Task 1 unit command. Expected: all icon cases pass.

- [ ] **Step 5: Commit green icon tests and domain types**

```bash
git add src/GZCTF/Features/SkillTrees/Domain \
  src/GZCTF/Features/LearningPaths/Domain/LearningPathModels.cs \
  src/GZCTF.Test/UnitTests/Features/SkillTrees
git commit -m "feat: add skill tree domain model"
```

## Task 3: ST03 — Configure EF Core and generate the additive migration

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Infrastructure/SkillTreeModelConfiguration.cs`
- Modify: `src/GZCTF/Models/AppDbContext.cs`
- Modify: `src/GZCTF/Features/LearningPaths/Infrastructure/LearningModelConfiguration.cs`
- Create: `src/GZCTF/Migrations/20260921000100_AddSkillTrees.cs`
- Create: `src/GZCTF/Migrations/20260921000100_AddSkillTrees.Designer.cs`
- Modify: `src/GZCTF/Migrations/AppDbContextModelSnapshot.cs`

- [ ] **Step 1: Add DbSets and configuration entry point**

Add these DbSets to `AppDbContext`:

```csharp
public DbSet<SkillTree> SkillTrees { get; set; } = null!;
public DbSet<SkillTreeRevision> SkillTreeRevisions { get; set; } = null!;
public DbSet<SkillCategory> SkillCategories { get; set; } = null!;
public DbSet<SkillTreeCategoryRef> SkillTreeCategoryRefs { get; set; } = null!;
public DbSet<CategoryContent> CategoryContents { get; set; } = null!;
public DbSet<SkillTreeEnrollment> SkillTreeEnrollments { get; set; } = null!;
public DbSet<LearningPathRedirect> LearningPathRedirects { get; set; } = null!;
```

Add `using GZCTF.Features.SkillTrees.Domain;`, `using GZCTF.Features.SkillTrees.Infrastructure;`, and call `SkillTreeModelConfiguration.Configure(builder)` from `OnModelCreating` beside the current Learning configuration.

- [ ] **Step 2: Add exact relational rules**

`SkillTreeModelConfiguration.Configure` must configure:

```csharp
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
```

Do not add global query filters; application queries must choose whether deleted rows are included.

- [ ] **Step 3: Configure lesson publication state**

In `LearningModelConfiguration` add:

```csharp
entity.Property(e => e.PublicationState).HasConversion<byte>();
entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
```

- [ ] **Step 4: Generate the migration**

```bash
docker run --rm \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet ef migrations add AddSkillTrees \
  --project GZCTF/GZCTF.csproj \
  --startup-project GZCTF/GZCTF.csproj
```

Rename the generated migration pair to `20260921000100_AddSkillTrees.cs` and `20260921000100_AddSkillTrees.Designer.cs`. In the designer file, set `[Migration("20260921000100_AddSkillTrees")]`; keep the migration class name `AddSkillTrees` and regenerate the snapshot from the final model.

The migration must only create new tables/indexes/FKs and add `Lessons.PublicationState`. For existing lessons, add the column with `Published` value `1`, then alter the default to `Draft` value `0` for future rows. It must not rename or drop `LearningPaths`, `LearningModules`, `ModuleItems`, `Enrollments`, or legacy competition tables.

- [ ] **Step 5: Run schema tests**

```bash
docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src \
  -e GZCTF_INTEGRATION_TEST_MODE=local \
  -e TESTCONTAINERS_RYUK_DISABLED=true \
  -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -c Debug \
  --filter FullyQualifiedName~SkillTreeSchemaTests
```

Expected: all uniqueness, check constraint, concurrency mapping, and current-enrollment cases pass.

- [ ] **Step 6: Commit schema and migration**

```bash
git add src/GZCTF/Features/SkillTrees/Infrastructure \
  src/GZCTF/Models/AppDbContext.cs \
  src/GZCTF/Features/LearningPaths/Infrastructure/LearningModelConfiguration.cs \
  src/GZCTF/Migrations \
  src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeSchemaTests.cs
git commit -m "feat: add skill tree persistence schema"
```

## Task 4: ST04 — Add failing backfill parity tests

**Files:**
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeBackfillTests.cs`

- [ ] **Step 1: Seed a deterministic current Learning graph**

The fixture must contain two paths, including duplicate module names that must remain separate, one published and one draft revision, one shared challenge, one lesson, ordered items, two enrollments, and one current enrollment. Use unique source IDs and slugs per test.

Build the central graph with this pattern, then add a second path and module using new IDs but the same module title `Web Basics`:

```csharp
var user = await TestDataSeeder.CreateUserAsync(
    factory.Services, TestDataSeeder.RandomName(), "Backfill!123");
var challenge = new Challenge
{
    Id = Guid.CreateVersion7(),
    Type = ChallengeType.StaticAttachment,
    PublicationState = ChallengePublicationState.Published,
    SourceType = "skill-tree-backfill-test",
    SourceId = Guid.NewGuid().ToString("N")
};
var lesson = new Lesson
{
    Id = Guid.CreateVersion7(),
    PublicationState = LessonPublicationState.Published,
    Localizations =
    [
        new LessonLocalization { Locale = "zh-CN", Title = "基础课节", Body = "正文" },
        new LessonLocalization { Locale = "en", Title = "Foundation lesson", Body = "Body" }
    ]
};
var path = new LearningPath
{
    Id = Guid.CreateVersion7(),
    Slug = $"backfill-{Guid.NewGuid():N}",
    Localizations =
    [
        new LearningPathLocalization { Locale = "zh-CN", Title = "初学者训练营", Summary = "入门" },
        new LearningPathLocalization { Locale = "en", Title = "Beginner camp", Summary = "Start here" }
    ]
};
var published = new LearningPathRevision
{
    Id = Guid.CreateVersion7(),
    Path = path,
    Status = LearningPathRevisionStatus.Published,
    PublishedAtUtc = DateTimeOffset.UtcNow
};
var module = new LearningModule
{
    Id = Guid.CreateVersion7(),
    Revision = published,
    SortOrder = 0,
    ExpectedMinutes = 60,
    Localizations =
    [
        new LearningModuleLocalization { Locale = "zh-CN", Title = "Web Basics", Summary = "Web" }
    ]
};
module.Items.Add(new ModuleItem { Id = Guid.CreateVersion7(), SortOrder = 0, Lesson = lesson });
module.Items.Add(new ModuleItem { Id = Guid.CreateVersion7(), SortOrder = 1, Challenge = challenge });
published.Modules.Add(module);
path.Revisions.Add(published);
path.Revisions.Add(new LearningPathRevision
{
    Id = Guid.CreateVersion7(),
    Path = path,
    Status = LearningPathRevisionStatus.Draft
});
path.CurrentPublishedRevision = published;
path.CurrentPublishedRevisionId = published.Id;
path.Enrollments.Add(new Enrollment
{
    Id = Guid.CreateVersion7(),
    UserId = user.Id,
    IsCurrent = true,
    EnrolledAtUtc = DateTimeOffset.UtcNow
});
db.LearningPaths.Add(path);
await db.SaveChangesAsync();
```

- [ ] **Step 2: Assert the exact mapping**

After invoking `SkillTreeBackfillService.RunAsync`, assert:

```csharp
Assert.Equal(oldPathCount, await db.SkillTrees.CountAsync(x => sourcePathIds.Contains(x.Id)));
Assert.Equal(oldModuleCount, await db.SkillCategories.CountAsync(x => sourceModuleIds.Contains(x.Id)));
Assert.Equal(oldItemCount, await db.CategoryContents.CountAsync(x => sourceModuleIds.Contains(x.CategoryId)));
Assert.Equal(oldEnrollmentCount,
    await db.SkillTreeEnrollments.CountAsync(x => sourcePathIds.Contains(x.SkillTreeId)));
Assert.Equal(oldPathCount,
    await db.LearningPathRedirects.CountAsync(x => sourcePathIds.Contains(x.LearningPathId)));
```

Assert category IDs equal source module IDs, tree IDs equal source path IDs, category and item order match, duplicate names remain separate, the current enrollment remains current, and existing challenge/lesson progress row counts do not change.

- [ ] **Step 3: Assert idempotency**

Call `RunAsync` a second time in a new service scope and assert every count and association is unchanged.

- [ ] **Step 4: Run and verify red**

Run the integration command from Task 3 with filter `FullyQualifiedName~SkillTreeBackfillTests`.

Expected: compilation fails because `SkillTreeBackfillService` does not exist.

- [ ] **Step 5: Keep the red backfill test uncommitted until Task 5**

Do not commit the non-compiling red state. Commit it with `SkillTreeBackfillService` after the parity and idempotency assertions pass.

## Task 5: ST05 — Implement idempotent current-Learning backfill

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Migration/SkillTreeBackfillService.cs`
- Modify: `src/GZCTF/Extensions/Startup/ServicesExtension.cs`
- Modify: `src/GZCTF/Utils/PrelaunchHelper.cs`

- [ ] **Step 1: Implement deterministic text selection**

Use one private helper for path/module text:

```csharp
private static T? PickText<T>(IEnumerable<T> values, Func<T, string> locale) =>
    values.FirstOrDefault(x => locale(x).Equals("zh-CN", StringComparison.OrdinalIgnoreCase))
    ?? values.FirstOrDefault(x => locale(x).Equals("en", StringComparison.OrdinalIgnoreCase))
    ?? values.FirstOrDefault();
```

- [ ] **Step 2: Implement the transaction and stable-ID backfill**

`RunAsync` must:

1. Start a serializable transaction.
2. Load current paths with localizations, revisions, modules, module localizations, items, and enrollments using split queries.
3. Skip a path when `LearningPathRedirects` already contains its ID.
4. Create `SkillTree` with `Id = LearningPath.Id`, selected name/summary, icon `flag`, timestamps, and a null current published revision pointer for the first save.
5. Create every `SkillTreeRevision` with `Id = LearningPathRevision.Id` and equivalent status/timestamps.
6. Create every `SkillCategory` with `Id = LearningModule.Id`, selected name/summary, icon `flag`, without name-based merging.
7. Create `SkillTreeCategoryRef` with stable ID `LearningModule.Id`, source module order, revision ID, and category ID.
8. Create every `CategoryContent` with `Id = ModuleItem.Id`, source content IDs and order.
9. Create every `SkillTreeEnrollment` with `Id = Enrollment.Id`, the same user, tree, current flag, and timestamp.
10. Create a `LearningPathRedirect` with `Id = LearningPath.Id`, source path ID, old slug, and tree ID.
11. Save the new graph, set each tree's current published revision pointer to the copied revision ID, save again, commit, and return inserted counts.

Before inserting a category or content row, check by stable ID so a partially completed development database can resume safely. Never match by name.

- [ ] **Step 3: Register and run backfill at startup**

Register:

```csharp
builder.Services.AddScoped<SkillTreeBackfillService>();
```

In `RunPrelaunchWorkAsync`, after `Database.MigrateAsync()` and before readiness-dependent services, call:

```csharp
await serviceScope.ServiceProvider
    .GetRequiredService<SkillTreeBackfillService>()
    .RunAsync();
```

Do not remove or reorder `StartupLegacyMigrationService`; legacy Game/ZIP migration must continue to run.

- [ ] **Step 4: Run backfill tests twice**

Run `SkillTreeBackfillTests`, then immediately run the same command again. Expected: all tests pass on both invocations and no unique-key error occurs.

- [ ] **Step 5: Commit backfill**

```bash
git add src/GZCTF/Features/SkillTrees/Migration \
  src/GZCTF/Extensions/Startup/ServicesExtension.cs \
  src/GZCTF/Utils/PrelaunchHelper.cs \
  src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeBackfillTests.cs
git commit -m "feat: backfill learning paths into skill trees"
```

## Task 6: Prove migration safety and current behavior compatibility

**Files:**
- Modify: `src/GZCTF.Integration.Test/Tests/Decommission/LegacyTableRetentionTests.cs`
- Modify: `src/GZCTF.Integration.Test/Tests/Learning/LearningPathPublishingTests.cs`

- [ ] **Step 1: Extend retained-table assertions**

Record and compare row counts for `LearningPaths`, `LearningPathRevisions`, `LearningModules`, `ModuleItems`, `Enrollments`, legacy Game tables, challenges, lessons, and progress before and after the new migration/backfill. Expected: no source count decreases.

- [ ] **Step 2: Keep current Learning API tests green**

Run `LearningPathPublishingTests`, `EnrollmentTests`, and `LearningRecordTests`. They must pass unchanged in Wave ST1 because cutover belongs to later waves.

- [ ] **Step 3: Run the complete backend suite**

```bash
docker run --rm \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet build GZCTF.slnx -c Debug
```

Then run all 174 or more unit tests and all default local integration tests with the roadmap commands. Expected: zero failures.

- [ ] **Step 4: Inspect the migration script**

```bash
docker run --rm \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet ef migrations script 20260919000300_AddImportParityReport \
  20260921000100_AddSkillTrees \
  --project GZCTF/GZCTF.csproj \
  --startup-project GZCTF/GZCTF.csproj \
  --output /tmp/add-skill-trees.sql
```

Assert the script contains `CREATE TABLE "SkillTrees"`, `CREATE TABLE "SkillCategories"`, and no `DROP TABLE`, `DROP COLUMN`, or rename of a current Learning/legacy competition table.

- [ ] **Step 5: Commit the safety gate**

```bash
git add src/GZCTF.Integration.Test/Tests/Decommission/LegacyTableRetentionTests.cs \
  src/GZCTF.Integration.Test/Tests/Learning/LearningPathPublishingTests.cs
git commit -m "test: prove additive skill tree migration"
```

## Wave ST1 completion checklist

- [ ] Fresh database migration passes.
- [ ] Existing database upgrade passes.
- [ ] Backfill run twice is idempotent.
- [ ] Path, module, item, enrollment, current selection, and redirect counts match.
- [ ] Existing progress rows are unchanged.
- [ ] Current Learning APIs and UI remain operational.
- [ ] No old Learning or competition table is dropped or renamed.
- [ ] `dotnet build`, unit tests, integration tests, and `git diff --check` pass.

Wave ST1 ends here. Do not implement new SkillTree APIs or frontend pages in this packet; continue with `2026-09-21-skill-tree-api-plan.md` after migration evidence is reviewed.
