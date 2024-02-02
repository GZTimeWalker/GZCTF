using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class Initialize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IP = table.Column<string>(type: "text", nullable: false),
                    LastSignedInUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastVisitedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegisterTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Bio = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    RealName = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    StdNumber = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: false),
                    ExerciseVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AvatarHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserName = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    ConfigKey = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.ConfigKey);
                });

            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Image = table.Column<string>(type: "text", nullable: false),
                    ContainerId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpectStopAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsProxy = table.Column<bool>(type: "boolean", nullable: false),
                    IP = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    PublicIP = table.Column<string>(type: "text", nullable: true),
                    PublicPort = table.Column<int>(type: "integer", nullable: true),
                    GameInstanceId = table.Column<int>(type: "integer", nullable: true),
                    ExerciseInstanceId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UploadTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ReferenceCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    PrivateKey = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    PosterHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AcceptWithoutReview = table.Column<bool>(type: "boolean", nullable: false),
                    WriteupRequired = table.Column<bool>(type: "boolean", nullable: false),
                    InviteCode = table.Column<string>(type: "text", nullable: true),
                    Organizations = table.Column<string>(type: "text", nullable: true),
                    TeamMemberCountLimit = table.Column<int>(type: "integer", nullable: false),
                    ContainerCountLimit = table.Column<int>(type: "integer", nullable: false),
                    StartTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WriteupDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WriteupNote = table.Column<string>(type: "text", nullable: false),
                    BloodBonus = table.Column<long>(type: "bigint", nullable: false),
                    PracticeMode = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Logger = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    RemoteIP = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    UserName = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Exception = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Bio = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AvatarHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    InviteToken = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CaptainId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_AspNetUsers_CaptainId",
                        column: x => x.CaptainId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    RemoteUrl = table.Column<string>(type: "text", nullable: true),
                    LocalFileId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Files_LocalFileId",
                        column: x => x.LocalFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GameNotices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    PublishTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameNotices_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    PublishTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GameEvents_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameEvents_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Organization = table.Column<string>(type: "text", nullable: true),
                    WriteupId = table.Column<int>(type: "integer", nullable: true),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participations_Files_WriteupId",
                        column: x => x.WriteupId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Participations_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamUserInfo",
                columns: table => new
                {
                    MembersId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamUserInfo", x => new { x.MembersId, x.TeamsId });
                    table.ForeignKey(
                        name: "FK_TeamUserInfo_AspNetUsers_MembersId",
                        column: x => x.MembersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamUserInfo_Teams_TeamsId",
                        column: x => x.TeamsId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Credit = table.Column<bool>(type: "boolean", nullable: false),
                    Difficulty = table.Column<byte>(type: "smallint", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Tag = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Hints = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedCount = table.Column<int>(type: "integer", nullable: false),
                    SubmissionCount = table.Column<int>(type: "integer", nullable: false),
                    ContainerImage = table.Column<string>(type: "text", nullable: true),
                    MemoryLimit = table.Column<int>(type: "integer", nullable: true),
                    StorageLimit = table.Column<int>(type: "integer", nullable: true),
                    CPUCount = table.Column<int>(type: "integer", nullable: true),
                    ContainerExposePort = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    FlagTemplate = table.Column<string>(type: "text", nullable: true),
                    AttachmentId = table.Column<int>(type: "integer", nullable: true),
                    TestContainerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseChallenges_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExerciseChallenges_Containers_TestContainerId",
                        column: x => x.TestContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GameChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnableTrafficCapture = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalScore = table.Column<int>(type: "integer", nullable: false),
                    MinScoreRate = table.Column<double>(type: "double precision", nullable: false),
                    Difficulty = table.Column<double>(type: "double precision", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Tag = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Hints = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedCount = table.Column<int>(type: "integer", nullable: false),
                    SubmissionCount = table.Column<int>(type: "integer", nullable: false),
                    ContainerImage = table.Column<string>(type: "text", nullable: true),
                    MemoryLimit = table.Column<int>(type: "integer", nullable: true),
                    StorageLimit = table.Column<int>(type: "integer", nullable: true),
                    CPUCount = table.Column<int>(type: "integer", nullable: true),
                    ContainerExposePort = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    FlagTemplate = table.Column<string>(type: "text", nullable: true),
                    AttachmentId = table.Column<int>(type: "integer", nullable: true),
                    TestContainerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameChallenges_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameChallenges_Containers_TestContainerId",
                        column: x => x.TestContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameChallenges_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserParticipations",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserParticipations", x => new { x.GameId, x.TeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserParticipations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserParticipations_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserParticipations_Participations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserParticipations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseDependencies",
                columns: table => new
                {
                    SourceId = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseDependencies", x => new { x.SourceId, x.TargetId });
                    table.ForeignKey(
                        name: "FK_ExerciseDependencies_ExerciseChallenges_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ExerciseChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseDependencies_ExerciseChallenges_TargetId",
                        column: x => x.TargetId,
                        principalTable: "ExerciseChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlagContexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Flag = table.Column<string>(type: "text", nullable: false),
                    IsOccupied = table.Column<bool>(type: "boolean", nullable: false),
                    AttachmentId = table.Column<int>(type: "integer", nullable: true),
                    ChallengeId = table.Column<int>(type: "integer", nullable: true),
                    ExerciseId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlagContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlagContexts_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlagContexts_ExerciseChallenges_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "ExerciseChallenges",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FlagContexts_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Answer = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmitTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Submissions_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Participations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseInstances",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    SolveTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsSolved = table.Column<bool>(type: "boolean", nullable: false),
                    IsLoaded = table.Column<bool>(type: "boolean", nullable: false),
                    LastContainerOperation = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FlagId = table.Column<int>(type: "integer", nullable: true),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseInstances", x => new { x.UserId, x.ExerciseId });
                    table.ForeignKey(
                        name: "FK_ExerciseInstances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseInstances_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExerciseInstances_ExerciseChallenges_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "ExerciseChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseInstances_FlagContexts_FlagId",
                        column: x => x.FlagId,
                        principalTable: "FlagContexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GameInstances",
                columns: table => new
                {
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    IsSolved = table.Column<bool>(type: "boolean", nullable: false),
                    IsLoaded = table.Column<bool>(type: "boolean", nullable: false),
                    LastContainerOperation = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FlagId = table.Column<int>(type: "integer", nullable: true),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameInstances", x => new { x.ChallengeId, x.ParticipationId });
                    table.ForeignKey(
                        name: "FK_GameInstances_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameInstances_FlagContexts_FlagId",
                        column: x => x.FlagId,
                        principalTable: "FlagContexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameInstances_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameInstances_Participations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheatInfo",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    SubmitTeamId = table.Column<int>(type: "integer", nullable: false),
                    SourceTeamId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheatInfo", x => x.SubmissionId);
                    table.ForeignKey(
                        name: "FK_CheatInfo_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheatInfo_Participations_SourceTeamId",
                        column: x => x.SourceTeamId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheatInfo_Participations_SubmitTeamId",
                        column: x => x.SubmitTeamId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheatInfo_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_LocalFileId",
                table: "Attachments",
                column: "LocalFileId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatInfo_GameId",
                table: "CheatInfo",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatInfo_SourceTeamId",
                table: "CheatInfo",
                column: "SourceTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatInfo_SubmissionId",
                table: "CheatInfo",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheatInfo_SubmitTeamId",
                table: "CheatInfo",
                column: "SubmitTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_ExerciseInstanceId",
                table: "Containers",
                column: "ExerciseInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Containers_GameInstanceId",
                table: "Containers",
                column: "GameInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseChallenges_AttachmentId",
                table: "ExerciseChallenges",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseChallenges_TestContainerId",
                table: "ExerciseChallenges",
                column: "TestContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDependencies_SourceId",
                table: "ExerciseDependencies",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDependencies_TargetId",
                table: "ExerciseDependencies",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseInstances_ContainerId",
                table: "ExerciseInstances",
                column: "ContainerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseInstances_ExerciseId",
                table: "ExerciseInstances",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseInstances_FlagId",
                table: "ExerciseInstances",
                column: "FlagId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseInstances_UserId",
                table: "ExerciseInstances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Hash",
                table: "Files",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_FlagContexts_AttachmentId",
                table: "FlagContexts",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagContexts_ChallengeId",
                table: "FlagContexts",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagContexts_ExerciseId",
                table: "FlagContexts",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_GameChallenges_AttachmentId",
                table: "GameChallenges",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GameChallenges_GameId",
                table: "GameChallenges",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameChallenges_TestContainerId",
                table: "GameChallenges",
                column: "TestContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_GameId",
                table: "GameEvents",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_TeamId",
                table: "GameEvents",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_UserId",
                table: "GameEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameInstances_ContainerId",
                table: "GameInstances",
                column: "ContainerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameInstances_FlagId",
                table: "GameInstances",
                column: "FlagId");

            migrationBuilder.CreateIndex(
                name: "IX_GameInstances_ParticipationId",
                table: "GameInstances",
                column: "ParticipationId");

            migrationBuilder.CreateIndex(
                name: "IX_GameNotices_GameId",
                table: "GameNotices",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_GameId",
                table: "Participations",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_TeamId",
                table: "Participations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_TeamId_GameId",
                table: "Participations",
                columns: new[] { "TeamId", "GameId" });

            migrationBuilder.CreateIndex(
                name: "IX_Participations_WriteupId",
                table: "Participations",
                column: "WriteupId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ChallengeId",
                table: "Submissions",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_GameId",
                table: "Submissions",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ParticipationId",
                table: "Submissions",
                column: "ParticipationId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TeamId_ChallengeId_GameId",
                table: "Submissions",
                columns: new[] { "TeamId", "ChallengeId", "GameId" });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_UserId",
                table: "Submissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams",
                column: "CaptainId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUserInfo_TeamsId",
                table: "TeamUserInfo",
                column: "TeamsId");

            migrationBuilder.CreateIndex(
                name: "IX_UserParticipations_ParticipationId",
                table: "UserParticipations",
                column: "ParticipationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserParticipations_TeamId",
                table: "UserParticipations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_UserParticipations_UserId_GameId",
                table: "UserParticipations",
                columns: new[] { "UserId", "GameId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CheatInfo");

            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "ExerciseDependencies");

            migrationBuilder.DropTable(
                name: "ExerciseInstances");

            migrationBuilder.DropTable(
                name: "GameEvents");

            migrationBuilder.DropTable(
                name: "GameInstances");

            migrationBuilder.DropTable(
                name: "GameNotices");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "TeamUserInfo");

            migrationBuilder.DropTable(
                name: "UserParticipations");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "FlagContexts");

            migrationBuilder.DropTable(
                name: "Participations");

            migrationBuilder.DropTable(
                name: "ExerciseChallenges");

            migrationBuilder.DropTable(
                name: "GameChallenges");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Files");
        }
    }
}
