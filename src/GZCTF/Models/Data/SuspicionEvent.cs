using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ParticipationId))]
public class SuspicionEvent
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    
    [InverseProperty(nameof(Models.Data.Participation.SuspicionEvents))]
    [JsonIgnore]
    public Participation Participation { get; set; } = null!;

    public string Type { get; set; } = string.Empty; // e.g., "SharedIP", "BurstSolve"
    
    public int ScoreDelta { get; set; }
    
    public string Details { get; set; } = string.Empty;
    
    public DateTimeOffset TimeUtc { get; set; } = DateTimeOffset.UtcNow;

    public int GameId { get; set; }
    
    [JsonIgnore]
    public Game Game { get; set; } = null!;

    public int? RelatedParticipationId { get; set; }

    [JsonIgnore]
    public Participation? RelatedParticipation { get; set; }
}
