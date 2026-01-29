using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ParticipationId))]
public class SuspicionEvent
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    
    [JsonIgnore]
    public Participation Participation { get; set; } = null!;

    public string Type { get; set; } = string.Empty; // e.g., "SharedIP", "BurstSolve"
    
    public int ScoreDelta { get; set; }
    
    public string Details { get; set; } = string.Empty;
    
    public DateTimeOffset TimeUtc { get; set; } = DateTimeOffset.UtcNow;
}
