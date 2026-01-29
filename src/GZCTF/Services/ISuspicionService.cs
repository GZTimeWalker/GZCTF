using GZCTF.Models.Data;

namespace GZCTF.Services;

public interface ISuspicionService
{
    Task AddSuspicion(Participation participation, string ruleCode, string details, int? relatedParticipationId = null, CancellationToken token = default);
    Task<int> GetScore(Participation participation, CancellationToken token = default);
}
