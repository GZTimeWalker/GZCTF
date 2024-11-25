using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FluentStorage;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(GameInstanceId))]
[Index(nameof(ExerciseInstanceId))]
public class Container
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Image name
    /// </summary>
    [Required]
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Container ID
    /// </summary>
    [Required]
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// Container status
    /// </summary>
    [Required]
    public ContainerStatus Status { get; set; } = ContainerStatus.Pending;

    /// <summary>
    /// Container creation time
    /// </summary>
    [Required]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Expected container stop time
    /// </summary>
    /// <remarks>
    /// Set to 2 hours to avoid immediate destruction after creation, actual destruction time is determined by the container manager
    /// </remarks>
    [Required]
    public DateTimeOffset ExpectStopAt { get; set; } = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);

    /// <summary>
    /// Whether the container has a reverse proxy
    /// </summary>
    [Required]
    public bool IsProxy { get; set; }

    /// <summary>
    /// Local IP
    /// </summary>
    [Required]
    public string IP { get; set; } = string.Empty;

    /// <summary>
    /// Local port
    /// </summary>
    [Required]
    public int Port { get; set; }

    /// <summary>
    /// Public IP
    /// </summary>
    public string? PublicIP { get; set; }

    /// <summary>
    /// Public port
    /// </summary>
    public int? PublicPort { get; set; }

    /// <summary>
    /// Container instance access method
    /// </summary>
    [NotMapped]
    public string Entry => IsProxy ? Id.ToString() : $"{PublicIP ?? IP}:{PublicPort ?? Port}";

    /// <summary>
    /// Whether traffic capture is enabled
    /// </summary>
    [NotMapped]
    public bool EnableTrafficCapture => GameInstance?.Challenge.EnableTrafficCapture ?? false;

    /// <summary>
    /// Container instance traffic capture storage path
    /// </summary>
    public string TrafficPath(string conn)
    {
        if (GameInstance is null)
            return string.Empty;

        var shortId = Id.ToString("N")[..8];

        return StoragePath.Combine(PathHelper.Capture,
            GameInstance.ChallengeId.ToString(),
            GameInstance.ParticipationId.ToString(),
            $"{shortId}-{conn}.pcap");
    }

    /// <summary>
    /// Generate metadata for the container
    /// </summary>
    /// <returns></returns>
    public byte[]? GenerateMetadata(JsonSerializerOptions options)
    {
        if (GameInstance is not null)
            return JsonSerializer.SerializeToUtf8Bytes(
                new GameMetadata(
                    GameInstance.Challenge.Title,
                    GameInstance.ChallengeId,
                    GameInstance.Participation.Team.Name,
                    GameInstance.Participation.TeamId,
                    ContainerId,
                    GameInstance.FlagContext?.Flag
                ), options);

        if (ExerciseInstance is not null)
            return JsonSerializer.SerializeToUtf8Bytes(
                new ExerciseMetadata(
                    ExerciseInstance.Exercise.Title,
                    ExerciseInstance.ExerciseId,
                    ExerciseInstance.User.UserName,
                    ExerciseInstance.UserId,
                    ContainerId,
                    ExerciseInstance.FlagContext?.Flag
                ), options);

        return null;
    }

    #region Db Relationship

    /// <summary>
    /// Game challenge instance object
    /// </summary>
    public GameInstance? GameInstance { get; set; }

    /// <summary>
    /// Game challenge instance object ID
    /// </summary>
    public int? GameInstanceId { get; set; }

    /// <summary>
    /// Exercise challenge instance object
    /// </summary>
    public ExerciseInstance? ExerciseInstance { get; set; }

    /// <summary>
    /// Exercise challenge instance object ID
    /// </summary>
    public int? ExerciseInstanceId { get; set; }

    #endregion Db Relationship
}

internal record GameMetadata(
    string Challenge,
    int ChallengeId,
    string Team,
    int TeamId,
    string ContainerId,
    string? Flag);

internal record ExerciseMetadata(
    string Challenge,
    int ExerciseId,
    string? UserName,
    Guid UserId,
    string ContainerId,
    string? Flag);
