using System.Net;
using System.Text.RegularExpressions;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GZCTF.Middlewares;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/game/{id}/cheatreport")]
public class CheatReportController(
    AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [RequireMonitor]
    [ProducesResponseType(typeof(CheatReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int id, CancellationToken token)
    {
        var game = await dbContext.Games.FindAsync([id], token);
        if (game == null)
            return NotFound();

        var report = new CheatReport();

        // Data Gathering
        var teams = await dbContext.Teams
            .AsNoTracking()
            .Where(t => t.Participations.Any(p => p.GameId == id))
            .Include(t => t.Members)
            .OrderBy(t => t.Id)
            .ToListAsync(token);

        var teamMap = teams.ToDictionary(t => t.Id);
        var userTeamMap = teams.SelectMany(t => t.Members.Select(u => new { u.UserName, TeamId = t.Id }))
            .Where(x => !string.IsNullOrEmpty(x.UserName))
            .GroupBy(x => x.UserName!)
            .ToDictionary(g => g.Key, g => g.First().TeamId);

        // Fetch Logs for IP Analysis
        var logs = await dbContext.Logs
            .AsNoTracking()
            .Where(l => l.TimeUtc >= game.StartTimeUtc && 
                        l.Logger.Contains("AccountController") && 
                        l.RemoteIP != null && 
                        l.UserName != null)
            .Select(l => new { l.UserName, l.RemoteIP })
            .ToListAsync(token);

        // ... (IP Analysis Logic) ...

        // Fetch Game Events
        var events = await dbContext.GameEvents
            .AsNoTracking()
            .Where(e => e.GameId == id && (e.Type == EventType.Download || e.Type == EventType.ContainerStart || e.Type == EventType.ChallengeOpened || e.Type == EventType.ContainerDestroy))
            .ToListAsync(token);
        
        // Fetch Challenges
        var challenges = await dbContext.GameChallenges
            .AsNoTracking()
            .Where(c => c.GameId == id)
            .Include(c => c.Attachment)
            .ToListAsync(token);
        var challengeMap = challenges.ToDictionary(c => c.Id);

        // Fetch Submissions
        var submissions = await dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.GameId == id && s.Status == AnswerResult.Accepted)
            .ToListAsync(token);

        // Map Team -> Set<IP>
        var teamIps = new Dictionary<int, HashSet<string>>();
        
        foreach (var log in logs)
        {
            if (log.UserName != null && userTeamMap.TryGetValue(log.UserName, out var teamId))
            {
                if (!teamIps.ContainsKey(teamId))
                    teamIps[teamId] = [];
                
                if (log.RemoteIP != null)
                    teamIps[teamId].Add(log.RemoteIP.ToString());
            }
        }
        
        foreach (var team in teams)
        {
            foreach (var member in team.Members)
            {
                if (!teamIps.ContainsKey(team.Id))
                    teamIps[team.Id] = [];
                
                 if (member.IP != null && !IPAddress.Any.Equals(member.IP) && !IPAddress.IPv6Any.Equals(member.IP))
                     teamIps[team.Id].Add(member.IP.ToString());
            }
        }

        // Create reverse mapping: IP -> List<TeamId>
        var ipToTeams = new Dictionary<string, List<int>>();
        foreach (var kvp in teamIps)
        {
            foreach (var ip in kvp.Value)
            {
                if (!ipToTeams.ContainsKey(ip))
                    ipToTeams[ip] = [];
                ipToTeams[ip].Add(kvp.Key);
            }
        }


        // Pre-process events into lookups
        var teamDownloads = new Dictionary<(int TeamId, int ChallengeId), List<DateTimeOffset>>();
        var teamContainerStarts = new Dictionary<(int TeamId, int ChallengeId), List<DateTimeOffset>>();
        var teamContainerDestroys = new Dictionary<(int TeamId, int ChallengeId), List<DateTimeOffset>>();
        var teamChallengeOpens = new Dictionary<(int TeamId, int ChallengeId), List<DateTimeOffset>>();
        
        // Use a single list for IP cross-check iteration logic to avoid re-looping full list
        var downloadEvents = new List<GameEvent>();

        foreach (var evt in events)
        {
            if (evt.Values == null) continue;

            switch (evt.Type)
            {
                case EventType.Download:
                    if (evt.Values.Count >= 4 && int.TryParse(evt.Values[0], out int dlCid))
                    {
                        var key = (evt.TeamId, dlCid);
                        if (!teamDownloads.ContainsKey(key)) teamDownloads[key] = [];
                        teamDownloads[key].Add(evt.PublishTimeUtc);
                        downloadEvents.Add(evt);
                    }
                    break;
                case EventType.ContainerStart:
                    if (evt.Values.Count >= 1 && int.TryParse(evt.Values[0], out int startCid))
                    {
                        var key = (evt.TeamId, startCid);
                        if (!teamContainerStarts.ContainsKey(key)) teamContainerStarts[key] = [];
                        teamContainerStarts[key].Add(evt.PublishTimeUtc);
                    }
                    break;
                case EventType.ContainerDestroy:
                    if (evt.Values.Count >= 1 && int.TryParse(evt.Values[0], out int destCid))
                    {
                        var key = (evt.TeamId, destCid);
                        if (!teamContainerDestroys.ContainsKey(key)) teamContainerDestroys[key] = [];
                        teamContainerDestroys[key].Add(evt.PublishTimeUtc);
                    }
                    break;
                case EventType.ChallengeOpened:
                    if (evt.Values.Count >= 1 && int.TryParse(evt.Values[0], out int openCid))
                    {
                        var key = (evt.TeamId, openCid);
                        if (!teamChallengeOpens.ContainsKey(key)) teamChallengeOpens[key] = [];
                        teamChallengeOpens[key].Add(evt.PublishTimeUtc);
                    }
                    break;
            }
        }

        // Check 1: Attachment IP Cross-Check
        // Detects if a team downloaded an attachment from an IP address associated with another team (Red Flag)
        // or from an IP address not seen in their own login history (Purple/Unknown Flag).
        foreach (var evt in downloadEvents)
        {
            if (evt.Values == null || evt.Values.Count < 4) continue;
            var dlIp = evt.Values[3];
            
            if (dlIp != "Unknown" && IPAddress.TryParse(dlIp, out var ipAddr)) 
            {
                var ipStr = ipAddr.ToString();
                
                // Extract challenge title from description for reporting (Values[2])
                var description = evt.Values[2];
                var match = Regex.Match(description, @"for challenge (.+?)\.$");
                var challengeTitle = match.Success ? match.Groups[1].Value : "Unknown";

                // Check if this IP belongs to another team
                if (ipToTeams.TryGetValue(ipStr, out var teamsWithThisIp))
                {
                    var otherTeams = teamsWithThisIp.Where(tid => tid != evt.TeamId).ToList();
                    if (otherTeams.Any())
                    {
                        var otherTeamNames = otherTeams.Select(tid => teamMap[tid].Name).ToList();
                        report.IpAnalysis.Add(new IpAnalysisResult
                        {
                            TeamId = evt.TeamId,
                            TeamName = evt.TeamName,
                            Type = "CrossTeamIP",
                            Ip = ipStr,
                            Details = $"Downloaded '{challengeTitle}' from IP {ipStr} which belongs to team(s): {string.Join(", ", otherTeamNames)}",
                            RelatedTeams = otherTeamNames,
                            Time = evt.PublishTimeUtc
                        });
                    }
                }
                // Also check if IP is not in team's login history at all
                else if (teamIps.TryGetValue(evt.TeamId, out var teamKnownIps) && !teamKnownIps.Contains(ipStr))
                {
                    report.IpAnalysis.Add(new IpAnalysisResult
                    {
                        TeamId = evt.TeamId,
                        TeamName = evt.TeamName,
                        Type = "UnknownIP",
                        Ip = ipStr,
                        Details = $"Downloaded '{challengeTitle}' from unknown IP {ipStr} (not in team's login history or any other team's)",
                        Time = evt.PublishTimeUtc
                    });
                }
            }
        }
 
        // Check 2: Team IP Overlap
        // Identifies IP addresses that have been used by multiple teams for login or interaction.
        // This suggests potential collusion or multi-accounting (Orange Flag).
        var allIps = teamIps.SelectMany(x => x.Value.Select(ip => new { TeamId = x.Key, Ip = ip })).ToList();
        var sharedIps = allIps.GroupBy(x => x.Ip)
            .Where(g => g.Select(x => x.TeamId).Distinct().Count() > 1)
            .ToList();

        foreach (var group in sharedIps)
        {
            var teamsSharing = group.Select(x => x.TeamId).Distinct().ToList();
            var teamNames = teamsSharing.Select(tid => teamMap[tid].Name).ToList();
            
            foreach (var tid in teamsSharing)
            {
                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = tid,
                    TeamName = teamMap[tid].Name,
                    Type = "SharedIP",
                    Ip = group.Key,
                    Details = $"IP {group.Key} is shared with teams: {string.Join(", ", teamNames.Where(n => n != teamMap[tid].Name))}",
                    RelatedTeams = teamNames
                });
            }
        }
        
        // Loop over submissions for Check 4, 5, 6
        foreach (var sub in submissions)
        {
            if (!challengeMap.TryGetValue(sub.ChallengeId, out var chal)) continue;

            // Check 4: Solve Before Download
            // Flags attempts to solve an attachment-based challenge without ever downloading the file.
            // This suggests the answer was shared or obtained externally.
            if (chal.Type.IsAttachment() && chal.AttachmentId != null)
            {
                var key = (sub.TeamId, sub.ChallengeId);
                var hasDownload = teamDownloads.TryGetValue(key, out var dls) && dls.Any(d => d < sub.SubmitTimeUtc);
                
                if (!hasDownload)
                {
                    report.AbnormalSolves.Add(new AbnormalSolveResult
                    {
                         TeamId = sub.TeamId,
                         TeamName = sub.TeamName,
                         ChallengeId = sub.ChallengeId,
                         ChallengeName = sub.ChallengeName,
                         Type = "NoDownload",
                         SolveTime = sub.SubmitTimeUtc,
                         Details = $"Solved at {sub.SubmitTimeUtc:MM/dd HH:mm:ss} without prior attachment download log."
                    });
                }
            }
            
            // Check 5: Solve Before Container Start
            // Flags attempts to solve a dynamic container challenge without starting a container instance.
            // Also detects if the solve happened BEFORE the container was started (impossible logic).
            if (chal.Type.IsContainer())
            {
                var key = (sub.TeamId, sub.ChallengeId);
                var hasStart = teamContainerStarts.TryGetValue(key, out var starts) && starts.Any(d => d < sub.SubmitTimeUtc);
                
                if (!hasStart)
                {
                    string details;
                    if (starts != null && starts.Count != 0)
                    {
                        // Found starts, but all are later than solve time
                        var firstStart = starts.Min();
                        var delay = firstStart - sub.SubmitTimeUtc;
                        details = $"Solved {delay.TotalSeconds:F0}s before container start (Start at {firstStart:MM/dd HH:mm:ss}).";
                    }
                    else
                    {
                        details = $"Solved at {sub.SubmitTimeUtc:MM/dd HH:mm:ss} without prior container start log.";
                    }

                    report.AbnormalSolves.Add(new AbnormalSolveResult
                    {
                         TeamId = sub.TeamId,
                         TeamName = sub.TeamName,
                         ChallengeId = sub.ChallengeId,
                         ChallengeName = sub.ChallengeName,
                         Type = "NoContainer",
                         SolveTime = sub.SubmitTimeUtc,
                         Details = details
                    });
                }
            }

            // Check 6: Flag Hoarding & Fast Solve (Time Analysis)
            // Identify if a team started/downloaded a challenge long before solving it (Hoarding)
            // or solved it almost instantly after opening (Fast Solve)
            var interactionKey = (sub.TeamId, sub.ChallengeId);
            var interactions = new List<DateTimeOffset>();
            
            if (teamDownloads.TryGetValue(interactionKey, out var dlTimes)) interactions.AddRange(dlTimes);
            if (teamContainerStarts.TryGetValue(interactionKey, out var stTimes)) interactions.AddRange(stTimes);
            if (teamChallengeOpens.TryGetValue(interactionKey, out var opTimes)) interactions.AddRange(opTimes);

            if (interactions.Any())
            {
                var firstInteraction = interactions.Min();
                var duration = sub.SubmitTimeUtc - firstInteraction;
                
                // Check 6: Flag Hoarding
                // Detects if a team held onto a flag and submitted it significantly later than when the environment was destroyed.
                // Specifically: Submission Time > Last Container Destroy Time + 3 Minutes.
                // This tracks "clean up then submit" behavior common in flag hoarding (Cyan Flag).
                // Only for Container challenges: If submission is AFTER container destroy by a margin.
                if (chal.Type.IsContainer())
                {
                    var key = (sub.TeamId, sub.ChallengeId);
                    if (teamContainerDestroys.TryGetValue(key, out var destroys))
                    {
                        // Get the latest destroy time that happened BEFORE the submission (or just take max?)
                        // User Logic: "apply this to last container destroy time"
                        // If I destroy, then submit 10 mins later -> Hoarding.
                        // Filter destroys relative to submission?
                        // If I destroy at 10:00, Submit at 10:10.
                        // If I destroy at 10:00, Start at 10:05, Submit at 10:10. Valid.
                        // So checking "Last Destroy < Submission" isn't enough if there's a subsequent Start.
                        
                        // Refined Logic:
                        // Find the LAST Start time before Submission.
                        // Find the LAST Destroy time before Submission.
                        // If LastDestroy > LastStart AND (Submission - LastDestroy) > Threshold -> Hoarding.
                        // Meaning: The current "session" ended with a destroy, and THEN they submitted.
                        // If LastStart > LastDestroy, the container is currently running (or was running at submit time), so it's fine.

                        var starts = teamContainerStarts.GetValueOrDefault(key) ?? new List<DateTimeOffset>();
                        var lastStart = starts.Where(s => s < sub.SubmitTimeUtc).MaxBy(s => s); // Default(DateTimeOffset) is MinValue
                        
                        var relevantDestroys = destroys.Where(d => d < sub.SubmitTimeUtc).ToList();
                        
                        if (relevantDestroys.Any())
                        {
                            var lastDestroy = relevantDestroys.Max();
                            
                            // If the last action was a Destroy (no start after it)
                            if (lastDestroy > lastStart)
                            {
                                var diff = sub.SubmitTimeUtc - lastDestroy;
                                // Threshold: 3 minutes.
                                if (diff > TimeSpan.FromMinutes(3))
                                {
                                     report.AbnormalSolves.Add(new AbnormalSolveResult
                                     {
                                         TeamId = sub.TeamId,
                                         TeamName = sub.TeamName,
                                         ChallengeId = sub.ChallengeId,
                                         ChallengeName = sub.ChallengeName,
                                         Type = "Hoarding",
                                         SolveTime = sub.SubmitTimeUtc,
                                         Details = $"Solved {diff.TotalMinutes:F0}m after container destroy (Destroyed at {lastDestroy:MM/dd HH:mm})."
                                     });                                    
                                }
                            }
                        }
                    }
                }
                // REMOVED: Old "Long Duration" hoarding check.
                
                // Check 7: Fast Solve
                // Flags submissions that occurred within 20 seconds of the very first interaction (Download/Start/Open).
                // Extremely unrealistic for most challenges (Cyan Flag).
                if (duration < TimeSpan.FromSeconds(20))
                {
                     report.AbnormalSolves.Add(new AbnormalSolveResult
                     {
                         TeamId = sub.TeamId,
                         TeamName = sub.TeamName,
                         ChallengeId = sub.ChallengeId,
                         ChallengeName = sub.ChallengeName,
                         Type = "FastSolve",
                         SolveTime = sub.SubmitTimeUtc,
                         Details = $"Solved in {duration.TotalSeconds:F1}s after first interaction (First touch at {firstInteraction:MM/dd HH:mm:ss})."
                     });
                }
            }
        }
        
        // Check 3: Sequence Similarity Analysis
        // Compares the order and timing of solves between teams to detect copying.
        // Uses Longest Common Subsequence (LCS) to find teams solving the same challenges in the same order.
        // Also calculates Time Correlation (Cosine Similarity of solve intervals) for high-confidence matches.
        var teamSequences = submissions
            .GroupBy(s => s.TeamId)
            .Select(g => new 
            { 
                TeamId = g.Key, 
                Sequence = g.OrderBy(x => x.SubmitTimeUtc).Select(x => x.ChallengeId).ToList(),
                Raw = g.OrderBy(x => x.SubmitTimeUtc).ToList()
            })
            .Where(x => x.Sequence.Count >= 3)
            .ToList();

        var topTeams = teamSequences.OrderByDescending(x => x.Sequence.Count).Take(50).ToList();
        
        for (int i = 0; i < topTeams.Count; i++)
        {
            for (int j = i + 1; j < topTeams.Count; j++)
            {
                var t1 = topTeams[i];
                var t2 = topTeams[j];
                
                // Get LCS (Common Sequence)
                var commonSeq = GetLongestCommonSubsequence(t1.Sequence, t2.Sequence);
                
                int minLen = Math.Min(t1.Sequence.Count, t2.Sequence.Count);
                double similarity = minLen == 0 ? 0 : (double)commonSeq.Count / minLen;
                
                if (similarity > 0.7)
                {
                   double timeCorrelation = 0;
                   if (commonSeq.Count >= 3)
                   {
                       var t1Times = t1.Raw.Where(x => commonSeq.Contains(x.ChallengeId))
                                       .OrderBy(x => x.SubmitTimeUtc)
                                       .Select(x => x.SubmitTimeUtc)
                                       .ToList();
                       var t2Times = t2.Raw.Where(x => commonSeq.Contains(x.ChallengeId))
                                       .OrderBy(x => x.SubmitTimeUtc)
                                       .Select(x => x.SubmitTimeUtc)
                                       .ToList();
                       timeCorrelation = CalculateTimeCorrelation(t1Times, t2Times);
                   }

                    var detailedSolves = new List<SequenceSuspectDetail>();
                    foreach (var cid in commonSeq)
                    {
                        var subA = t1.Raw.FirstOrDefault(x => x.ChallengeId == cid);
                        var subB = t2.Raw.FirstOrDefault(x => x.ChallengeId == cid);
                        
                        if (subA != null && subB != null)
                        {
                            var cName = challengeMap.TryGetValue(cid, out var ch) ? ch.Title : "Unknown";
                            var diff = Math.Abs((subA.SubmitTimeUtc - subB.SubmitTimeUtc).TotalSeconds);
                            
                            detailedSolves.Add(new SequenceSuspectDetail
                            {
                                ChallengeName = cName,
                                TimeA = subA.SubmitTimeUtc,
                                TimeB = subB.SubmitTimeUtc,
                                TimeDiff = diff
                            });
                        }
                    }

                    report.SequenceSuspects.Add(new SequenceSuspectResult
                    {
                        TeamA = teamMap[t1.TeamId].Name,
                        TeamB = teamMap[t2.TeamId].Name,
                        Similarity = similarity,
                        TimeCorrelation = timeCorrelation,
                        CommonSolves = commonSeq.Count,
                        Details = $"Common Solves: {string.Join(", ", commonSeq.Take(10))}{(commonSeq.Count > 10 ? "..." : "")}",
                        DetailedSolves = detailedSolves
                    });
                }
            }
        }

        // Check 8: Burst Solving
        // Detects if a team solves multiple challenges in an extremely short timeframe (e.g., >= 3 solves in < 60 seconds).
        // This indicates automated submission scripts or flag sharing entry.
        foreach (var teamSeq in teamSequences)
        {
             var subs = teamSeq.Raw;
             if (subs.Count < 3) continue;

             for (int i = 0; i < subs.Count; i++)
             {
                 int burstCount = 1;
                 double totalSeconds = 0;
                 var burstNames = new List<string> { subs[i].ChallengeName };
                 
                 // Look ahead
                 for (int j = i + 1; j < subs.Count; j++)
                 {
                     var diff = (subs[j].SubmitTimeUtc - subs[i].SubmitTimeUtc).TotalSeconds;
                     if (diff <= 60)
                     {
                         burstCount++;
                         totalSeconds = diff;
                         burstNames.Add(subs[j].ChallengeName);
                     }
                     else
                     {
                         break;
                     }
                 }
                 
                 if (burstCount >= 3)
                 {
                     report.AbnormalSolves.Add(new AbnormalSolveResult
                     {
                         TeamId = teamSeq.TeamId,
                         TeamName = teamMap[teamSeq.TeamId].Name,
                         ChallengeId = subs[i].ChallengeId, // Cite the first challenge in the burst
                         ChallengeName = subs[i].ChallengeName,
                         Type = "Burst",
                         SolveTime = subs[i].SubmitTimeUtc,
                         Details = $"Burst: Solved {burstCount} challenges in {totalSeconds:F0}s: {string.Join(", ", burstNames)}."
                     });
                     
                     // Skip the challenges we just grouped into this burst
                     i += burstCount - 1;
                 }
             }
        }

        report.IpAnalysis = report.IpAnalysis.OrderBy(x => x.TeamId).ThenBy(x => x.Time).ToList();
        report.AbnormalSolves = report.AbnormalSolves.OrderBy(x => x.TeamId).ThenBy(x => x.SolveTime).ToList();
        report.SequenceSuspects = report.SequenceSuspects.OrderByDescending(x => x.Similarity).ThenBy(x => x.TeamA).ToList();

        return Ok(report);
    }

    private static List<int> GetLongestCommonSubsequence(List<int> seq1, List<int> seq2)
    {
        int n = seq1.Count;
        int m = seq2.Count;
        int[,] dp = new int[n + 1, m + 1];

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                if (seq1[i - 1] == seq2[j - 1])
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        // Reconstruct LCS
        var lcs = new List<int>();
        int r = n, c = m;
        while (r > 0 && c > 0)
        {
            if (seq1[r - 1] == seq2[c - 1])
            {
                lcs.Add(seq1[r - 1]);
                r--;
                c--;
            }
            else if (dp[r - 1, c] > dp[r, c - 1])
                r--;
            else
                c--;
        }
        lcs.Reverse();
        return lcs;
    }

    private static double CalculateTimeCorrelation(List<DateTimeOffset> t1, List<DateTimeOffset> t2)
    {
        if (t1.Count < 3 || t2.Count < 3 || t1.Count != t2.Count) return 0;

        // Cosine Similarity of intervals
        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        for (int i = 1; i < t1.Count; i++)
        {
            var d1 = (t1[i] - t1[i-1]).TotalSeconds;
            var d2 = (t2[i] - t2[i-1]).TotalSeconds;
            
            dotProduct += d1 * d2;
            normA += d1 * d1;
            normB += d2 * d2;
        }

         if (normA == 0 || normB == 0) return 0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
