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
    AppDbContext dbContext,
    GZCTF.Services.ISuspicionService suspicionService) : ControllerBase
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
            .Where(t => t.Participations.Any(p => p.GameId == id))
            .Include(t => t.Members)
            .Include(t => t.Participations.Where(p => p.GameId == id))
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
            .Select(l => new { l.UserName, l.RemoteIP, l.BrowserFingerprint })
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
        var teamFingerprints = new Dictionary<int, HashSet<string>>();
        
        foreach (var log in logs)
        {
            if (log.UserName != null && userTeamMap.TryGetValue(log.UserName, out var teamId))
            {
                if (!teamIps.ContainsKey(teamId))
                    teamIps[teamId] = [];
                
                if (log.RemoteIP != null)
                    teamIps[teamId].Add(log.RemoteIP.ToString());

                if (!string.IsNullOrEmpty(log.BrowserFingerprint))
                {
                    if (!teamFingerprints.ContainsKey(teamId))
                        teamFingerprints[teamId] = [];
                    teamFingerprints[teamId].Add(log.BrowserFingerprint);
                }
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
            var description = evt.Values[2];
            var ipStr = dlIp; // Default to raw string

            // Token Abuse Detection (Independent of IP Validity)
            if (description.Contains("[Token Source:")) 
            {
                 // Parse readable details
                 // Log format: "User {Attacker} from team {AttackerTeam} downloaded ... [Token Source: {Source}]"
                 var attackerMatch = Regex.Match(description, @"^User (.+?) from team (.+?) downloaded");
                 var sourceMatch = Regex.Match(description, @"\[Token Source: (.+?)\]");
                 
                 var detailedMsg = "Used stolen token.";
                 if (attackerMatch.Success && sourceMatch.Success) 
                 {
                     var attackerName = attackerMatch.Groups[1].Value;
                     var attackerTeam = attackerMatch.Groups[2].Value;
                     var sourceInfo = sourceMatch.Groups[1].Value;
                     detailedMsg = $"{attackerName} (Team: {attackerTeam}) used a token belonging to {sourceInfo}.";
                 }
                 else
                 {
                     detailedMsg = $"Token Abuse Detected. {description}";
                 }

                     report.IpAnalysis.Add(new IpAnalysisResult
                     {
                         TeamId = evt.TeamId,
                         TeamName = evt.TeamName,
                         Type = SuspicionType.TokenAbuse,
                         Ip = dlIp,
                         Details = detailedMsg,
                         Time = evt.PublishTimeUtc
                     });
            }
            
            if (dlIp != "Unknown" && IPAddress.TryParse(dlIp, out var ipAddr)) 
            {
                ipStr = ipAddr.ToString();
                
                // Extract challenge title from description for reporting (Values[2])
                // description already extracted above
                var match = Regex.Match(description, @"for challenge (.+?)\.");
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
                            Type = SuspicionType.CrossTeamIP,
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
                        Type = SuspicionType.UnknownIP,
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
                    Type = SuspicionType.SharedIP,
                    Ip = group.Key,
                    Details = $"IP {group.Key} is shared with teams: {string.Join(", ", teamNames.Where(n => n != teamMap[tid].Name))}",
                    RelatedTeams = teamNames
                });
            }
        }

        // Check 2b: Team Fingerprint Overlap
        var allFingerprints = teamFingerprints.SelectMany(x => x.Value.Select(fp => new { TeamId = x.Key, Fingerprint = fp })).ToList();
        var sharedFingerprints = allFingerprints.GroupBy(x => x.Fingerprint)
            .Where(g => g.Select(x => x.TeamId).Distinct().Count() > 1)
            .ToList();

        foreach (var group in sharedFingerprints)
        {
            var teamsSharing = group.Select(x => x.TeamId).Distinct().ToList();
            var teamNames = teamsSharing.Select(tid => teamMap[tid].Name).ToList();
            
            foreach (var tid in teamsSharing)
            {
                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = tid,
                    TeamName = teamMap[tid].Name,
                    Type = SuspicionType.SharedFingerprint,
                    Ip = group.Key,
                    Details = $"Fingerprint {group.Key} is shared with teams: {string.Join(", ", teamNames.Where(n => n != teamMap[tid].Name))}",
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
            if (chal.AttachmentId != null)
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
                         Type = SuspicionType.NoDownload,
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
                         Type = SuspicionType.NoContainer,
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
                        var lastStart = starts.Where(s => s < sub.SubmitTimeUtc).DefaultIfEmpty(DateTimeOffset.MinValue).Max();
                        
                        var relevantDestroys = destroys.Where(d => d < sub.SubmitTimeUtc).ToList();
                        
                        if (relevantDestroys.Any())
                        {
                            var lastDestroy = relevantDestroys.Max();
                            
                            // If the last action was a Destroy (no start after it)
                            if (lastDestroy > lastStart)
                            {
                                var diff = sub.SubmitTimeUtc - lastDestroy;
                                // Threshold: 60 minutes.
                                if (diff > TimeSpan.FromMinutes(60))
                                {
                                     report.AbnormalSolves.Add(new AbnormalSolveResult
                                     {
                                         TeamId = sub.TeamId,
                                         TeamName = sub.TeamName,
                                         ChallengeId = sub.ChallengeId,
                                         ChallengeName = sub.ChallengeName,
                                         Type = SuspicionType.Hoarding,
                                         SolveTime = sub.SubmitTimeUtc,
                                         Details = $"Solved {diff.TotalMinutes:F0}m after container destroy (Destroyed at {lastDestroy:MM/dd HH:mm})."
                                     });                                    
                                }
                            }
                        }
                    }
                }
                // REMOVED: Old "Long Duration" hoarding check.
                
                // Check 7: Fast Solve - Refined Logic
                
                // 7a. Fast Solve (Open): Solved immediately after opening challenge
                // This applies to ALL challenges.
                if (teamChallengeOpens.TryGetValue(interactionKey, out var opTimesCheck) && opTimesCheck.Any())
                {
                    var firstOpen = opTimesCheck.Min();
                    var durationOpen = sub.SubmitTimeUtc - firstOpen;
                    if (durationOpen < TimeSpan.FromMinutes(2))
                    {
                         report.AbnormalSolves.Add(new AbnormalSolveResult
                         {
                             TeamId = sub.TeamId,
                             TeamName = sub.TeamName,
                             ChallengeId = sub.ChallengeId,
                             ChallengeName = sub.ChallengeName,
                             Type = SuspicionType.FastSolveOpen,
                             SolveTime = sub.SubmitTimeUtc,
                             Details = $"Solved in {durationOpen.TotalSeconds:F1}s after opening challenge (Opened at {firstOpen:MM/dd HH:mm:ss})."
                         }); 
                    }
                }

                // 7b. Fast Solve (Download): Solved immediately after downloading attachment
                // Applies only if challenge has an attachment.
                if ((chal.Type.IsAttachment() || chal.AttachmentId != null) && 
                    teamDownloads.TryGetValue(interactionKey, out var dlTimesCheck) && dlTimesCheck.Any())
                {
                     var firstDl = dlTimesCheck.Min();
                     var durationDl = sub.SubmitTimeUtc - firstDl;
                     if (durationDl < TimeSpan.FromMinutes(2))
                     {
                          report.AbnormalSolves.Add(new AbnormalSolveResult
                          {
                              TeamId = sub.TeamId,
                              TeamName = sub.TeamName,
                              ChallengeId = sub.ChallengeId,
                              ChallengeName = sub.ChallengeName,
                              Type = SuspicionType.FastSolveDownload,
                              SolveTime = sub.SubmitTimeUtc,
                              Details = $"Solved in {durationDl.TotalSeconds:F1}s after downloading attachment (Downloaded at {firstDl:MM/dd HH:mm:ss})."
                          });
                     }
                }

                // 7c. Fast Solve (Container): Solved immediately after container start
                // Applies if challenge is a container type AND has NO attachment (Blackbox).
                // If it has both, we generally prioritize Download check, but checking container for blackbox is key.
                if (chal.Type.IsContainer() && chal.AttachmentId == null &&
                    teamContainerStarts.TryGetValue(interactionKey, out var stTimesCheck) && stTimesCheck.Any())
                {
                     var firstStart = stTimesCheck.Min();
                     var durationStart = sub.SubmitTimeUtc - firstStart;
                     if (durationStart < TimeSpan.FromMinutes(2))
                     {
                          report.AbnormalSolves.Add(new AbnormalSolveResult
                          {
                              TeamId = sub.TeamId,
                              TeamName = sub.TeamName,
                              ChallengeId = sub.ChallengeId,
                              ChallengeName = sub.ChallengeName,
                              Type = SuspicionType.FastSolveContainer,
                              SolveTime = sub.SubmitTimeUtc,
                              Details = $"Solved in {durationStart.TotalSeconds:F1}s after starting container (Started at {firstStart:MM/dd HH:mm:ss})."
                          });
                     }
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
                         Type = SuspicionType.Burst,
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

        // Check 9: Collusion Group Analysis (Hybrid CSD)
        var teamDataList = teamSequences.Select(ts => new TeamData
        {
            TeamId = ts.TeamId,
            TeamName = teamMap[ts.TeamId].Name,
            Solves = ts.Raw.Select(x => x.ChallengeId).ToHashSet(),
            Sequence = ts.Sequence,
            Raw = ts.Raw,
            ParticipationId = ts.Raw.FirstOrDefault()?.ParticipationId ?? 0
        }).ToList(); // Used the existing teamSequences which has Sequence and Raw solves

        AnalyzeCollusionGroups(report, teamDataList, challengeMap);

        // Process Suspicion Scores
        foreach (var item in report.IpAnalysis)
        {
            if (teamMap.TryGetValue(item.TeamId, out var team))
            {
                var part = team.Participations.FirstOrDefault(p => p.GameId == id);
                if (part != null)
                {
                    await suspicionService.AddSuspicion(part, item.Type, item.Details, token: token);
                }
            }
        }

        foreach (var item in report.AbnormalSolves)
        {
            if (teamMap.TryGetValue(item.TeamId, out var team))
            {
                var part = team.Participations.FirstOrDefault(p => p.GameId == id);
                if (part != null)
                {
                    await suspicionService.AddSuspicion(part, item.Type, item.Details, token: token);
                }
            }
        }
        

        
        foreach (var group in report.CollusionGroups)
        {
            foreach (var teamInfo in group.Teams)
            {
                var team = teams.FirstOrDefault(t => t.Id == teamInfo.Id);
                if (team != null)
                {
                     var part = team.Participations.FirstOrDefault(p => p.GameId == id);
                     if (part != null) 
                     {
                         await suspicionService.AddSuspicion(part, SuspicionType.CollusionGroup, group.Details, token: token);
                     }
                }
            }
        }

        // Populate Suspicion List in Report
        var participations = await dbContext.Participations
            .AsNoTracking()
            .Where(p => p.GameId == id && p.SuspicionScore > 0)
            .Include(p => p.Team)
            .Include(p => p.SuspicionEvents)
            .OrderByDescending(p => p.SuspicionScore)
            .ToListAsync(token);

        foreach (var p in participations)
        {
            report.SuspicionList.Add(new SuspicionRecordResult
            {
                TeamId = p.TeamId,
                ParticipationId = p.Id,
                Status = p.Status,
                TeamName = p.Team?.Name ?? "Unknown",
                Score = p.SuspicionScore,
                Events = p.SuspicionEvents.Select(e => new SuspicionEventResult
                {
                    Type = e.Type,
                    ScoreDelta = e.ScoreDelta,
                    Details = e.Details,
                    Time = e.TimeUtc
                }).OrderByDescending(e => e.Time).ToList()
            });
        }

        return Ok(report);
    }

    [HttpGet("compare")]
    [RequireMonitor]
    [ProducesResponseType(typeof(CollusionCompareResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compare(int id, int participationA, int participationB, CancellationToken token)
    {
        var game = await dbContext.Games.FindAsync([id], token);
        if (game == null) return NotFound();

        var participations = await dbContext.Participations
            .Where(p => p.Id == participationA || p.Id == participationB)
            .Include(p => p.Team)
            .ToListAsync(token);

        if (participations.Count != 2)
        {
            if (participationA == participationB)
                return BadRequest("Cannot compare a participation with itself.");

            var foundIds = participations.Select(p => p.Id).ToHashSet();
            if (!foundIds.Contains(participationA))
                return BadRequest($"Participation {participationA} not found.");

            return BadRequest($"Participation {participationB} not found.");
        }

        var pA = participations.First(p => p.Id == participationA);
        var pB = participations.First(p => p.Id == participationB);

        if (pA.GameId != id || pB.GameId != id)
            return BadRequest($"Participations must belong to Game {id}. P{participationA} is in Game {pA.GameId}, P{participationB} is in Game {pB.GameId}.");

        var subA = await dbContext.Submissions
            .Where(s => s.ParticipationId == participationA && s.GameId == id && s.Status == AnswerResult.Accepted)
            .OrderBy(s => s.SubmitTimeUtc)
            .ToListAsync(token);

        var subB = await dbContext.Submissions
            .Where(s => s.ParticipationId == participationB && s.GameId == id && s.Status == AnswerResult.Accepted)
            .OrderBy(s => s.SubmitTimeUtc)
            .ToListAsync(token);

        var challenges = await dbContext.GameChallenges
            .AsNoTracking()
            .Where(c => c.GameId == id)
            .ToListAsync(token);
        var challengeMap = challenges.ToDictionary(c => c.Id);

        var commonChallenges = subA.Select(s => s.ChallengeId)
            .Intersect(subB.Select(s => s.ChallengeId))
            .ToList();

        var detailedSolves = new List<SequenceSuspectDetail>();

        foreach (var cid in commonChallenges)
        {
            var sA = subA.First(s => s.ChallengeId == cid);
            var sB = subB.First(s => s.ChallengeId == cid);
            var chTitle = challengeMap.TryGetValue(cid, out var ch) ? ch.Title : "Unknown";
            var diff = Math.Abs((sA.SubmitTimeUtc - sB.SubmitTimeUtc).TotalSeconds);

            detailedSolves.Add(new SequenceSuspectDetail
            {
                ChallengeName = chTitle,
                TimeA = sA.SubmitTimeUtc,
                TimeB = sB.SubmitTimeUtc,
                TimeDiff = diff
            });
        }

        // Calculate RSI
        var solvesA = subA.Select(s => s.ChallengeId).ToHashSet();
        var solvesB = subB.Select(s => s.ChallengeId).ToHashSet();
        var intersection = solvesA.Intersect(solvesB).Count();
        var union = solvesA.Union(solvesB).Count();
        var jaccard = union == 0 ? 0 : (double)intersection / union;

        var seqA = subA.Select(s => s.ChallengeId).ToList();
        var seqB = subB.Select(s => s.ChallengeId).ToList();
        var lcs = GetLongestCommonSubsequence(seqA, seqB);
        var minLen = Math.Min(seqA.Count, seqB.Count);
        var lcsScore = minLen == 0 ? 0 : (double)lcs.Count / minLen;

        var rsi = (jaccard * 0.7) + (lcsScore * 0.3);

        return Ok(new CollusionCompareResult
        {
            RSI = rsi,
            Details = detailedSolves.OrderBy(d => d.TimeDiff).Take(50).OrderBy(d => d.TimeA).ToList()
        });
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

    private class TeamData
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public HashSet<int> Solves { get; set; } = new();
        public List<int> Sequence { get; set; } = new();
        public List<GZCTF.Models.Data.Submission> Raw { get; set; } = new();
        public int ParticipationId { get; set; }
    }

    private void AnalyzeCollusionGroups(
        CheatReport report,
        List<TeamData> teamDataList,
        Dictionary<int, GameChallenge> challengeMap)
    {
        var activeTeams = teamDataList.Where(t => t.Solves.Count > 0).ToList();
        var n = activeTeams.Count;
        if (n < 2) return;

        // 1. Calculate Hybrid RSI Matrix
        // RSI[i, j] stored in a dictionary? or just iterate?
        // We need to sort by RSI, so let's compute all pulses.
        var rsiList = new List<(int IndexA, int IndexB, double Score)>();

        var rsiMatrix = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                var tA = activeTeams[i];
                var tB = activeTeams[j];

                // Jaccard Similarity (Set)
                var intersection = tA.Solves.Intersect(tB.Solves).Count();
                var union = tA.Solves.Union(tB.Solves).Count();
                var jaccard = union == 0 ? 0 : (double)intersection / union;

                // LCS Score (Sequence)
                // Use existing helper
                var lcs = GetLongestCommonSubsequence(tA.Sequence, tB.Sequence);
                var minLen = Math.Min(tA.Sequence.Count, tB.Sequence.Count);
                var lcsScore = minLen == 0 ? 0 : (double)lcs.Count / minLen;

                // Hybrid RSI
                // Weight: 70% Set, 30% Sequence
                var rsi = (jaccard * 0.7) + (lcsScore * 0.3);

                rsiMatrix[i, j] = rsi;
                rsiMatrix[j, i] = rsi; // Symmetric

                if (rsi > 0.5) // Filter out low noise
                {
                    rsiList.Add((i, j, rsi));
                }
            }
        }

        // Sort pairs by RSI descending
        rsiList.Sort((a, b) => b.Score.CompareTo(a.Score));

        var visited = new bool[n];
        var groups = new List<CollusionGroupResult>();

        // 2. CSD Screening Algorithm
        foreach (var pair in rsiList)
        {
            if (visited[pair.IndexA] || visited[pair.IndexB]) continue;

            // Start a new candidate group S with this pair
            var groupIndices = new List<int> { pair.IndexA, pair.IndexB };
            
            // "Doubt" tracking (Average RSI of the group)
            // Ideally, we want to maintain High Internal Consistency.
            
            bool added;
            do
            {
                added = false;
                int bestCandidate = -1;
                double bestMeanRsi = -1;

                // Find best candidate k outside S
                for (int k = 0; k < n; k++)
                {
                    if (visited[k] || groupIndices.Contains(k)) continue;

                    // Calculate mean RSI with current group members
                    double sumRsi = 0;
                    foreach (var memberIdx in groupIndices)
                    {
                        sumRsi += rsiMatrix[k, memberIdx];
                    }
                    double meanRsi = sumRsi / groupIndices.Count;

                    if (meanRsi > bestMeanRsi)
                    {
                        bestMeanRsi = meanRsi;
                        bestCandidate = k;
                    }
                }

                // Check Cutoff / Mutation Point
                // Threshold: e.g., 0.8 or within 10% of the initial pair's score?
                // Or "Doubet" logic: If adding k drops the group average significantly.
                // Simple heuristic: If bestMeanRsi > 0.8 (High confidence they belong)
                
                if (bestCandidate != -1 && bestMeanRsi > 0.75) 
                {
                    groupIndices.Add(bestCandidate);
                    added = true;
                }

            } while (added);

            // Final Hypothesis Check
            // A group must have at least 2 members (we started with 2)
            // But usually collusion groups are interesting if > 2, or just the pair.
            // Let's verify the group average RSI is still high.
            
            if (groupIndices.Count >= 2)
            {
                // Calculate final metrics
                var groupTeams = new List<CollusionTeamInfo>();
                var solvesList = new List<HashSet<int>>();

                foreach (var idx in groupIndices)
                {
                    visited[idx] = true;
                    var team = activeTeams[idx];
                    groupTeams.Add(new CollusionTeamInfo { Id = team.TeamId, Name = team.TeamName, ParticipationId = team.ParticipationId });
                    solvesList.Add(team.Solves);
                }

                // Finds common solves across ALL members
                IEnumerable<int> intersection = solvesList[0];
                for(int i=1; i<solvesList.Count; i++)
                {
                    intersection = intersection.Intersect(solvesList[i]);
                }
                var commonSolves = intersection.ToList();
                
                // Calculate exact group Average RSI
                double totalRsi = 0;
                int count = 0;
                for(int i=0; i<groupIndices.Count; i++)
                {
                    for(int j=i+1; j<groupIndices.Count; j++)
                    {
                        totalRsi += rsiMatrix[groupIndices[i], groupIndices[j]];
                        count++;
                    }
                }
                var avgRsi = count == 0 ? 0 : totalRsi / count;

                // Generate result
                // Fetch challenge titles
                var commonTitles = commonSolves
                    .Select(cid => challengeMap.TryGetValue(cid, out var c) ? c.Title : cid.ToString())
                    .Take(10)
                    .ToList();
                
                if (commonSolves.Count > 10) commonTitles.Add("...");

                var detailedSolves = new List<SequenceSuspectDetail>();
                var detailsBody = $"Group of {groupTeams.Count} teams with {avgRsi:P1} similarity on {commonSolves.Count} common challenges.";

                // If it's a group of 2+, find the most suspicious pair for detailed view
                if (groupIndices.Count >= 2)
                {
                    var bestPair = (0, 1);
                    double maxRsi = -1;
                    
                    // Find pair with highest RSI in this group
                    for (int i = 0; i < groupIndices.Count; i++)
                    {
                        for (int j = i + 1; j < groupIndices.Count; j++)
                        {
                            if (rsiMatrix[groupIndices[i], groupIndices[j]] > maxRsi)
                            {
                                maxRsi = rsiMatrix[groupIndices[i], groupIndices[j]];
                                bestPair = (i, j);
                            }
                        }
                    }

                    var t1 = activeTeams[groupIndices[bestPair.Item1]];
                    var t2 = activeTeams[groupIndices[bestPair.Item2]];
                    
                    foreach (var cid in commonSolves)
                    {
                        var subA = t1.Raw.FirstOrDefault(x => x.ChallengeId == cid);
                        var subB = t2.Raw.FirstOrDefault(x => x.ChallengeId == cid);
                        
                        if (subA != null && subB != null)
                        {
                            var chTitle = challengeMap.TryGetValue(cid, out var ch) ? ch.Title : "Unknown";
                            var diff = Math.Abs((subA.SubmitTimeUtc - subB.SubmitTimeUtc).TotalSeconds);
                            
                            detailedSolves.Add(new SequenceSuspectDetail
                            {
                                ChallengeName = chTitle,
                                TimeA = subA.SubmitTimeUtc,
                                TimeB = subB.SubmitTimeUtc,
                                TimeDiff = diff
                            });
                        }
                    }
                    
                    // Sort by suspicion (low diff) and take top 25 to save bandwidth, then sort by time
                    detailedSolves = detailedSolves.OrderBy(d => d.TimeDiff).Take(25).OrderBy(d => d.TimeA).ToList();
                    
                    if (groupIndices.Count > 2)
                    {
                         detailsBody = $"Group of {groupTeams.Count} teams. Timeline compares {t1.TeamName} and {t2.TeamName} (most similar pair).";
                    }
                }

                report.CollusionGroups.Add(new CollusionGroupResult
                {
                    Teams = groupTeams,
                    AverageRSI = avgRsi,
                    CommonSolves = commonTitles,
                    Details = detailsBody,
                    DetailedSolves = detailedSolves
                });
            }
        }
    }
}
