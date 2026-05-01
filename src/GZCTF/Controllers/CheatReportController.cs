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
        string TeamRef(int teamId)
        {
            var teamName = teamMap.TryGetValue(teamId, out var teamEntry) ? teamEntry.Name : "Unknown";
            return $"team '{teamName}'";
        }

        static string BuildDetail(params (string Key, string? Value)[] fields) =>
            string.Join('\n', fields
                .Where(f => !string.IsNullOrWhiteSpace(f.Value))
                .Select(f => $"{f.Key}: {f.Value!.Trim()}"));

        var analysisEndTime = game.PracticeMode
            ? DateTimeOffset.UtcNow
            : game.EndTimeUtc;

        // Fetch Logs for IP Analysis
        var logs = await dbContext.Logs
            .AsNoTracking()
            .Where(l => l.TimeUtc >= game.StartTimeUtc &&
                        l.TimeUtc <= analysisEndTime &&
                        l.Logger.Contains("AccountController") && 
                        l.RemoteIP != null && 
                        l.UserName != null)
            .Select(l => new { l.TimeUtc, l.UserName, l.RemoteIP, l.BrowserFingerprint })
            .ToListAsync(token);

        var logIdentities = logs
            .Where(l => !string.IsNullOrEmpty(l.UserName) && userTeamMap.ContainsKey(l.UserName!))
            .Select(l =>
            {
                var teamId = userTeamMap[l.UserName!];
                return new
                {
                    TeamId = teamId,
                    TeamName = teamMap[teamId].Name,
                    UserName = l.UserName!,
                    Ip = l.RemoteIP?.ToString(),
                    Fingerprint = l.BrowserFingerprint,
                    Time = l.TimeUtc
                };
            })
            .ToList();

        var ipUserUsage = logIdentities
            .Where(x => !string.IsNullOrWhiteSpace(x.Ip))
            .GroupBy(x => x.Ip!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var fingerprintUserUsage = logIdentities
            .Where(x => !string.IsNullOrWhiteSpace(x.Fingerprint))
            .GroupBy(x => x.Fingerprint!)
            .ToDictionary(g => g.Key, g => g.ToList());

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

        var dynamicAttachmentInstances = await dbContext.GameInstances
            .AsNoTracking()
            .Where(i => i.Challenge.GameId == id && i.FlagContext != null)
            .Select(i => new
            {
                TeamId = i.Participation.TeamId,
                i.ChallengeId,
                AttachmentType = i.FlagContext!.Attachment != null ? (FileType?)i.FlagContext.Attachment.Type : null,
                LocalFileId = i.FlagContext!.Attachment != null ? i.FlagContext.Attachment.LocalFileId : null
            })
            .ToListAsync(token);

        var dynamicLocalDownloadRequirement = dynamicAttachmentInstances
            .GroupBy(i => (i.TeamId, i.ChallengeId))
            .ToDictionary(
                g => g.Key,
                g => g.Any(i => i.AttachmentType == FileType.Local && i.LocalFileId != null));

        var dynamicChallengeHasLocalAttachment = await dbContext.FlagContexts
            .AsNoTracking()
            .Where(f => f.ChallengeId != null && f.Challenge != null && f.Challenge.GameId == id && f.Attachment != null)
            .Select(f => new
            {
                ChallengeId = f.ChallengeId!.Value,
                AttachmentType = f.Attachment!.Type,
                f.Attachment.LocalFileId
            })
            .ToListAsync(token);
        var dynamicChallengeLocalRequirement = dynamicChallengeHasLocalAttachment
            .GroupBy(f => f.ChallengeId)
            .ToDictionary(
                g => g.Key,
                g => g.Any(f => f.AttachmentType == FileType.Local && f.LocalFileId != null));

        // Extra datasets for new detection checks
        var wrongSubmissions = await dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.GameId == id && s.Status == AnswerResult.WrongAnswer)
            .Select(s => new { s.TeamId, s.ChallengeId, s.SubmitTimeUtc, s.Answer, s.ParticipationId })
            .ToListAsync(token);

        var allFlagContexts = await dbContext.GameInstances
            .AsNoTracking()
            .Where(i => i.Challenge.GameId == id && i.FlagContext != null && i.FlagContext.Flag != string.Empty)
            .Select(i => new { TeamId = i.Participation.TeamId, i.ChallengeId, Flag = i.FlagContext!.Flag })
            .ToListAsync(token);

        var firstSolvesData = await dbContext.FirstSolves
            .AsNoTracking()
            .Where(fs => fs.Participation.GameId == id)
            .Select(fs => new {
                fs.ChallengeId,
                SubmitTimeUtc = fs.Submission.SubmitTimeUtc,
                TeamId = fs.Submission.TeamId
            })
            .ToListAsync(token);

        // Build registration-IP lookup from each user's first-ever AccountController log entry
        // (more accurate than UserInfo.IP which is the most-recent login IP)
        var allGameUserNames = teams
            .SelectMany(t => t.Members.Select(m => m.UserName))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet();

        var allTimeUserLogs = await dbContext.Logs
            .AsNoTracking()
            .Where(l => l.Logger.Contains("AccountController") &&
                        l.RemoteIP != null &&
                        l.UserName != null &&
                        allGameUserNames.Contains(l.UserName!))
            .Select(l => new { l.UserName, l.RemoteIP, l.TimeUtc })
            .ToListAsync(token);

        var firstLogIpPerUser = allTimeUserLogs
            .GroupBy(l => l.UserName!)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.TimeUtc).First().RemoteIP?.ToString());

        bool RequiresLocalDownload(int teamId, GameChallenge challenge)
        {
            if (challenge.Type == ChallengeType.DynamicAttachment)
            {
                if (dynamicLocalDownloadRequirement.TryGetValue((teamId, challenge.Id), out var requiresDownload))
                    return requiresDownload;

                return dynamicChallengeLocalRequirement.GetValueOrDefault(challenge.Id);
            }

            return challenge.Attachment?.Type == FileType.Local && challenge.Attachment.LocalFileId != null;
        }

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

        // Check 2c: Fingerprint Churn (Per User)
        // Detects if a single user uses too many distinct fingerprints during a game.
        // This helps prevent evasion via frequent fingerprint switching.
        const int FingerprintChurnThreshold = 4;
        const int FingerprintChurnSampleCount = 3;
        var fingerprintChurnUsers = logs
            .Where(l => !string.IsNullOrEmpty(l.UserName) && !string.IsNullOrEmpty(l.BrowserFingerprint))
            .GroupBy(l => l.UserName!)
            .Select(g => new
            {
                UserName = g.Key,
                Fingerprints = g.Select(x => x.BrowserFingerprint!).Distinct().ToList(),
                FirstSeen = g.Min(x => x.TimeUtc),
                LastSeen = g.Max(x => x.TimeUtc),
            })
            .Where(x => x.Fingerprints.Count >= FingerprintChurnThreshold)
            .ToList();

        foreach (var item in fingerprintChurnUsers)
        {
            if (!userTeamMap.TryGetValue(item.UserName, out var teamId))
                continue;

            if (!teamMap.TryGetValue(teamId, out var team))
                continue;

            var samples = item.Fingerprints.Take(FingerprintChurnSampleCount).ToList();
            var sampleStr = string.Join(", ", samples);
            if (item.Fingerprints.Count > FingerprintChurnSampleCount)
                sampleStr += $", ... (+{item.Fingerprints.Count - FingerprintChurnSampleCount} more)";

            report.IpAnalysis.Add(new IpAnalysisResult
            {
                TeamId = teamId,
                TeamName = team.Name,
                Type = SuspicionType.FingerprintChurn,
                // Reuse the existing "ip" column for the subject (similar to SharedFingerprint using it for fingerprints)
                Ip = item.UserName,
                Time = item.LastSeen,
                UserNames = [item.UserName],
                Details = BuildDetail(
                    ("Summary", "Single user rotated many browser fingerprints"),
                    ("Target", TeamRef(teamId)),
                    ("User", item.UserName),
                    ("Distinct fingerprints", item.Fingerprints.Count.ToString()),
                    ("First seen", item.FirstSeen.ToString("MM/dd HH:mm:ss")),
                    ("Last seen", item.LastSeen.ToString("MM/dd HH:mm:ss")),
                    ("Samples", sampleStr)),
            });
        }

        // Check 2d: IP Churn (Per User)
        // Detects if a single user uses too many distinct IPs during a game.
        const int IpChurnThreshold = 4;
        const int IpChurnSampleCount = 3;
        var ipChurnUsers = logs
            .Where(l =>
                !string.IsNullOrEmpty(l.UserName) &&
                l.RemoteIP != null &&
                !IPAddress.Any.Equals(l.RemoteIP) &&
                !IPAddress.IPv6Any.Equals(l.RemoteIP))
            .GroupBy(l => l.UserName!)
            .Select(g => new
            {
                UserName = g.Key,
                Ips = g.Select(x => x.RemoteIP!.ToString()).Distinct().ToList(),
                FirstSeen = g.Min(x => x.TimeUtc),
                LastSeen = g.Max(x => x.TimeUtc),
            })
            .Where(x => x.Ips.Count >= IpChurnThreshold)
            .ToList();

        foreach (var item in ipChurnUsers)
        {
            if (!userTeamMap.TryGetValue(item.UserName, out var teamId))
                continue;

            if (!teamMap.TryGetValue(teamId, out var team))
                continue;

            var samples = item.Ips.Take(IpChurnSampleCount).ToList();
            var sampleStr = string.Join(", ", samples);
            if (item.Ips.Count > IpChurnSampleCount)
                sampleStr += $", ... (+{item.Ips.Count - IpChurnSampleCount} more)";

            report.IpAnalysis.Add(new IpAnalysisResult
            {
                TeamId = teamId,
                TeamName = team.Name,
                Type = SuspicionType.IpChurn,
                // Reuse the existing "ip" column for the subject (username)
                Ip = item.UserName,
                Time = item.LastSeen,
                UserNames = [item.UserName],
                Details = BuildDetail(
                    ("Summary", "Single user rotated many IP addresses"),
                    ("Target", TeamRef(teamId)),
                    ("User", item.UserName),
                    ("Distinct IPs", item.Ips.Count.ToString()),
                    ("First seen", item.FirstSeen.ToString("MM/dd HH:mm:ss")),
                    ("Last seen", item.LastSeen.ToString("MM/dd HH:mm:ss")),
                    ("Samples", sampleStr)),
            });
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
                    if (evt.Values.Count >= 1 && int.TryParse(evt.Values[0], out int dlCid))
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
            var structuredDownload = ParseDownloadMetadata(evt.Values);
            var challengeTitle = structuredDownload?.ChallengeTitle;
            if (string.IsNullOrWhiteSpace(challengeTitle))
            {
                var challengeMatch = Regex.Match(description, @"for challenge (.+?)\.");
                challengeTitle = challengeMatch.Success ? challengeMatch.Groups[1].Value : "Unknown";
            }
            challengeTitle ??= "Unknown";

            // Token Abuse Detection (Independent of IP Validity)
            var hasLegacyTokenTag = description.Contains("[Token Source:", StringComparison.Ordinal);
            if (structuredDownload?.TokenAbuse == true || hasLegacyTokenTag) 
            {
                var detailedMsg = BuildDetail(
                    ("Summary", "A submission/download token was used by a different actor"),
                    ("Target", TeamRef(evt.TeamId)));
                var tokenAbuseUsers = new List<string>();

                if (structuredDownload is not null)
                {
                    if (!string.IsNullOrWhiteSpace(structuredDownload.ActorUserName))
                        tokenAbuseUsers.Add(structuredDownload.ActorUserName);

                    detailedMsg = BuildDetail(
                        ("Summary", "Token abuse detected"),
                        ("Target", TeamRef(evt.TeamId)),
                        ("Actor", structuredDownload.ActorUserName ?? "Unknown"),
                        ("Actor declared team", structuredDownload.ActorTeamName),
                        ("Token type", structuredDownload.TokenType),
                        ("Token source user", structuredDownload.TokenSourceUserName),
                        ("Token source team", structuredDownload.TokenSourceTeamName),
                        ("Challenge", challengeTitle));
                }
                else
                {
                    // Legacy fallback for older logs stored without structured payload.
                    var attackerMatch = Regex.Match(description, @"^User (.+?) from team (.+?) downloaded");
                    var sourceMatch = Regex.Match(description, @"\[Token Source: (.+?)\]");

                    if (attackerMatch.Success && sourceMatch.Success)
                    {
                        var attackerName = attackerMatch.Groups[1].Value;
                        var attackerTeam = attackerMatch.Groups[2].Value;
                        var sourceInfo = sourceMatch.Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(attackerName))
                            tokenAbuseUsers.Add(attackerName);

                        detailedMsg = BuildDetail(
                            ("Summary", "Token abuse detected"),
                            ("Target", TeamRef(evt.TeamId)),
                            ("Actor", attackerName),
                            ("Actor declared team", attackerTeam),
                            ("Token source", sourceInfo));
                    }
                    else
                    {
                        detailedMsg = BuildDetail(
                            ("Summary", "Token abuse detected"),
                            ("Target", TeamRef(evt.TeamId)),
                            ("Raw event", description));
                    }
                }

                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = evt.TeamId,
                    TeamName = evt.TeamName,
                    Type = SuspicionType.TokenAbuse,
                    Ip = dlIp,
                    Details = detailedMsg,
                    Time = evt.PublishTimeUtc,
                    UserNames = tokenAbuseUsers
                });
            }
            
            if (dlIp != "Unknown" && IPAddress.TryParse(dlIp, out var ipAddr)) 
            {
                ipStr = ipAddr.ToString();

                // Check if this IP belongs to another team
                if (ipToTeams.TryGetValue(ipStr, out var teamsWithThisIp))
                {
                    var otherTeams = teamsWithThisIp.Where(tid => tid != evt.TeamId).ToList();
                    if (otherTeams.Any())
                    {
                        var otherTeamNames = otherTeams.Select(tid => teamMap[tid].Name).ToList();
                        var targetUsers = ipUserUsage.TryGetValue(ipStr, out var targetIpUsers)
                            ? targetIpUsers.Where(u => u.TeamId == evt.TeamId).Select(u => u.UserName).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Take(6).ToList()
                            : [];
                        var relatedUsers = ipUserUsage.TryGetValue(ipStr, out var sourceIpUsers)
                            ? sourceIpUsers.Where(u => u.TeamId != evt.TeamId).Select(u => u.UserName).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Take(8).ToList()
                            : [];

                        report.IpAnalysis.Add(new IpAnalysisResult
                        {
                            TeamId = evt.TeamId,
                            TeamName = evt.TeamName,
                            Type = SuspicionType.CrossTeamIP,
                            Ip = ipStr,
                            Details = BuildDetail(
                                ("Summary", "Download came from an IP used by other teams"),
                                ("Target", TeamRef(evt.TeamId)),
                                ("IP", ipStr),
                                ("Challenge", challengeTitle),
                                ("Source teams", string.Join(", ", otherTeams.Select(TeamRef))),
                                ("Source users", ipUserUsage.TryGetValue(ipStr, out var usersOnIp)
                                    ? string.Join(", ", usersOnIp.Where(u => u.TeamId != evt.TeamId).Select(u => $"{u.UserName} ({TeamRef(u.TeamId)})").Distinct().Take(6))
                                    : "none")),
                            RelatedTeams = otherTeamNames,
                            UserNames = targetUsers,
                            RelatedUsers = relatedUsers,
                            Time = evt.PublishTimeUtc
                        });
                    }
                }
                // Also check if IP is not in team's login history at all
                else if (teamIps.TryGetValue(evt.TeamId, out var teamKnownIps) && !teamKnownIps.Contains(ipStr))
                {
                    var actor = structuredDownload?.ActorUserName;
                    if (string.IsNullOrWhiteSpace(actor))
                    {
                        var actorMatch = Regex.Match(description, @"^User (.+?) from team (.+?) downloaded");
                        actor = actorMatch.Success ? actorMatch.Groups[1].Value : "Unknown";
                    }

                    report.IpAnalysis.Add(new IpAnalysisResult
                    {
                        TeamId = evt.TeamId,
                        TeamName = evt.TeamName,
                        Type = SuspicionType.UnknownIP,
                        Ip = ipStr,
                        Details = BuildDetail(
                            ("Summary", "Download came from an unmapped/unknown IP"),
                            ("Target", TeamRef(evt.TeamId)),
                            ("Actor", actor),
                            ("Challenge", challengeTitle),
                            ("IP", ipStr)),
                        UserNames = string.IsNullOrWhiteSpace(actor) || actor == "Unknown" ? [] : [actor],
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
                var targetUsers = ipUserUsage.TryGetValue(group.Key, out var targetIpUsers)
                    ? targetIpUsers.Where(u => u.TeamId == tid).Select(u => u.UserName).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Take(6).ToList()
                    : [];
                var relatedUsers = ipUserUsage.TryGetValue(group.Key, out var sourceIpUsers)
                    ? sourceIpUsers.Where(u => u.TeamId != tid).Select(u => u.UserName).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Take(8).ToList()
                    : [];

                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = tid,
                    TeamName = teamMap[tid].Name,
                    Type = SuspicionType.SharedIP,
                    Ip = group.Key,
                    Details = BuildDetail(
                        ("Summary", "Same IP observed across multiple teams"),
                        ("Target", TeamRef(tid)),
                        ("IP", group.Key),
                        ("Source teams", string.Join(", ", teamsSharing.Where(otherTid => otherTid != tid).Select(TeamRef))),
                        ("Target users on IP", ipUserUsage.TryGetValue(group.Key, out var targetIpUsersForDetail)
                            ? string.Join(", ", targetIpUsersForDetail.Where(u => u.TeamId == tid).Select(u => u.UserName).Distinct().Take(6))
                            : "none"),
                        ("Source users on IP", ipUserUsage.TryGetValue(group.Key, out var sourceIpUsersForDetail)
                            ? string.Join(", ", sourceIpUsersForDetail.Where(u => u.TeamId != tid).Select(u => $"{u.UserName} ({TeamRef(u.TeamId)})").Distinct().Take(8))
                            : "none")),
                    RelatedTeams = teamNames,
                    UserNames = targetUsers,
                    RelatedUsers = relatedUsers
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
                var targetUsers = fingerprintUserUsage.TryGetValue(group.Key, out var targetFpUsers)
                    ? targetFpUsers.Where(u => u.TeamId == tid).Select(u => u.UserName).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Take(6).ToList()
                    : [];
                var relatedUsers = fingerprintUserUsage.TryGetValue(group.Key, out var sourceFpUsers)
                    ? sourceFpUsers.Where(u => u.TeamId != tid).Select(u => u.UserName).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Take(8).ToList()
                    : [];

                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = tid,
                    TeamName = teamMap[tid].Name,
                    Type = SuspicionType.SharedFingerprint,
                    Ip = group.Key,
                    Details = BuildDetail(
                        ("Summary", "Same browser fingerprint observed across multiple teams"),
                        ("Target", TeamRef(tid)),
                        ("Fingerprint", group.Key),
                        ("Source teams", string.Join(", ", teamsSharing.Where(otherTid => otherTid != tid).Select(TeamRef))),
                        ("Target users on fingerprint", fingerprintUserUsage.TryGetValue(group.Key, out var targetFpUsersForDetail)
                            ? string.Join(", ", targetFpUsersForDetail.Where(u => u.TeamId == tid).Select(u => u.UserName).Distinct().Take(6))
                            : "none"),
                        ("Source users on fingerprint", fingerprintUserUsage.TryGetValue(group.Key, out var sourceFpUsersForDetail)
                            ? string.Join(", ", sourceFpUsersForDetail.Where(u => u.TeamId != tid).Select(u => $"{u.UserName} ({TeamRef(u.TeamId)})").Distinct().Take(8))
                            : "none")),
                    RelatedTeams = teamNames,
                    UserNames = targetUsers,
                    RelatedUsers = relatedUsers
                });
            }
        }
        
        // Pre-compute wrong attempt index per (team, challenge) for Check A
        var wrongByTeamChallenge = wrongSubmissions
            .GroupBy(s => (s.TeamId, s.ChallengeId))
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SubmitTimeUtc).ToList());

        // Pre-compute community solve time statistics per challenge for Check D
        var challengeFirstSolveTime = submissions
            .GroupBy(s => s.ChallengeId)
            .ToDictionary(g => g.Key, g => g.Min(s => s.SubmitTimeUtc));

        var challengeMedianSolveOffset = submissions
            .GroupBy(s => s.ChallengeId)
            .ToDictionary(g => g.Key, g =>
            {
                var offsets = g
                    .Select(s => (s.SubmitTimeUtc - game.StartTimeUtc).TotalMinutes)
                    .OrderBy(x => x).ToList();
                var mid = offsets.Count / 2;
                return offsets.Count % 2 == 0
                    ? (offsets[mid - 1] + offsets[mid]) / 2.0
                    : offsets[mid];
            });

        var challengeSolveCount = submissions
            .GroupBy(s => s.ChallengeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var teamParticipatingCount = teams.Count;

        // Per-challenge: fraction of solvers who had zero wrong attempts before their solve
        var zeroAttemptRatePerChallenge = submissions
            .GroupBy(s => s.ChallengeId)
            .ToDictionary(g => g.Key, g =>
            {
                var solvers = g.ToList();
                if (solvers.Count == 0) return 0.0;
                var zeroAttemptSolvers = solvers.Count(s =>
                    !wrongByTeamChallenge.TryGetValue((s.TeamId, s.ChallengeId), out var wrongs) ||
                    !wrongs.Any(w => w.SubmitTimeUtc < s.SubmitTimeUtc));
                return (double)zeroAttemptSolvers / solvers.Count;
            });

        // Easy challenge: solve-rate > 40% of participating teams, OR zero-attempt-rate > 30%
        // Suppress most per-challenge signals for easy challenges (FP-heavy).
        bool IsChallengeEasy(int challengeId) =>
            (teamParticipatingCount > 0 &&
             challengeSolveCount.GetValueOrDefault(challengeId, 0) / (double)teamParticipatingCount > 0.40) ||
            zeroAttemptRatePerChallenge.GetValueOrDefault(challengeId, 0.0) > 0.30;

        // Check F: Registration Clustering
        // Detects accounts from multiple teams whose first-ever login IP is shared,
        // registered within 48 hours — strong indicator of sockpuppet/multi-accounting.
        // Uses log-derived registration IP (not UserInfo.IP which is last-login IP).
        // Suppressed if >4 teams share the IP (large shared NAT like a university).
        var memberRegGroups = teams
            .SelectMany(t => t.Members
                .Where(m => !string.IsNullOrEmpty(m.UserName) &&
                            firstLogIpPerUser.TryGetValue(m.UserName!, out var regIp) &&
                            !string.IsNullOrEmpty(regIp))
                .Select(m => new
                {
                    TeamId = t.Id,
                    m.UserName,
                    Ip = firstLogIpPerUser[m.UserName!]!,
                    m.RegisterTimeUtc
                }))
            .GroupBy(x => x.Ip)
            .Where(g =>
            {
                var distinctTeams = g.Select(x => x.TeamId).Distinct().Count();
                return distinctTeams > 1 && distinctTeams <= 4; // suppress large shared networks
            })
            .ToList();

        foreach (var group in memberRegGroups)
        {
            var memberList = group.ToList();
            var registrationSpan = memberList.Max(m => m.RegisterTimeUtc) - memberList.Min(m => m.RegisterTimeUtc);
            if (registrationSpan > TimeSpan.FromHours(48)) continue;

            var teamsInvolved = memberList.Select(m => m.TeamId).Distinct().ToList();
            foreach (var teamId in teamsInvolved)
            {
                if (!teamMap.TryGetValue(teamId, out var regTeam)) continue;
                var teamMembers = memberList.Where(m => m.TeamId == teamId).ToList();
                var otherMembers = memberList.Where(m => m.TeamId != teamId).ToList();

                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = teamId,
                    TeamName = regTeam.Name,
                    Type = SuspicionType.ClusteredRegistration,
                    Ip = group.Key,
                    Time = memberList.Max(m => m.RegisterTimeUtc),
                    UserNames = teamMembers.Select(m => m.UserName ?? "").Where(n => n.Length > 0).ToList(),
                    RelatedUsers = otherMembers.Select(m => m.UserName ?? "").Where(n => n.Length > 0).ToList(),
                    RelatedTeams = teamsInvolved.Where(t => t != teamId).Select(TeamRef).ToList(),
                    Details = BuildDetail(
                        ("Summary", "Multiple team accounts share same first-login IP and registered within 48 hours"),
                        ("Target", TeamRef(teamId)),
                        ("IP", group.Key),
                        ("Registration span", $"{registrationSpan.TotalHours:F0}h"),
                        ("Target users", string.Join(", ", teamMembers.Select(m => m.UserName ?? "?"))),
                        ("Related teams", string.Join(", ", teamsInvolved.Where(t => t != teamId).Select(TeamRef))),
                        ("Related users", string.Join(", ", otherMembers.Select(m => $"{m.UserName} ({TeamRef(m.TeamId)})"))))
                });
            }
        }

        // Check G: Subnet Overlap (/24)
        // Soft signal: teams accessing from the same /24 subnet.
        // Low weight alone, but amplifies other corroborating signals.
        var teamSubnets28 = teamIps.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                .Select(GetSubnet28)
                .Where(s => s != null)
                .Cast<string>()
                .ToHashSet());

        var subnetGroups = teamSubnets28
            .SelectMany(kvp => kvp.Value.Select(s => new { TeamId = kvp.Key, Subnet = s }))
            .GroupBy(x => x.Subnet)
            .Where(g =>
            {
                var distinctTeams = g.Select(x => x.TeamId).Distinct().Count();
                return distinctTeams > 1 && distinctTeams <= 4; // suppress large shared networks
            })
            .ToList();

        foreach (var group in subnetGroups)
        {
            var teamIds = group.Select(x => x.TeamId).Distinct().ToList();
            foreach (var teamId in teamIds)
            {
                if (!teamMap.TryGetValue(teamId, out var snTeam)) continue;
                var otherTeams = teamIds.Where(t => t != teamId).ToList();

                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = teamId,
                    TeamName = snTeam.Name,
                    Type = SuspicionType.SubnetOverlap,
                    Ip = group.Key,
                    RelatedTeams = otherTeams.Select(t => teamMap[t].Name).ToList(),
                    Details = BuildDetail(
                        ("Summary", "Team shares /24 subnet with other teams"),
                        ("Target", TeamRef(teamId)),
                        ("Subnet", group.Key),
                        ("Related teams", string.Join(", ", otherTeams.Select(TeamRef))))
                });
            }
        }

        // Check I: Session Concurrency
        // Detects repeated occurrences of the same user account appearing from two IPs that are
        // in different /20 subnets within a 10-minute window (≥3 occurrences required).
        // Filters out mobile IP churn (same ISP /20 = same pool) and one-off VPN switches.
        {
            const int SessionWindowMinutes = 10;
            const int SessionConcurrencyMinOccurrences = 3;
            var userConcurrencyEvents = new Dictionary<string, List<DateTimeOffset>>();
            var userConcurrencyIps = new Dictionary<string, HashSet<string>>();

            var userSessions = logIdentities
                .GroupBy(l => l.UserName)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Time).ToList());

            foreach (var (userName, sessions) in userSessions)
            {
                if (sessions.Count < 2) continue;

                for (int si = 0; si < sessions.Count; si++)
                {
                    for (int sj = si + 1; sj < sessions.Count; sj++)
                    {
                        var timeDiff = sessions[sj].Time - sessions[si].Time;
                        if (timeDiff > TimeSpan.FromMinutes(SessionWindowMinutes)) break;

                        var ip1 = sessions[si].Ip ?? "";
                        var ip2 = sessions[sj].Ip ?? "";
                        if (string.IsNullOrEmpty(ip1) || string.IsNullOrEmpty(ip2) || ip1 == ip2) continue;

                        // Suppress: IPs in same /20 are likely the same ISP pool (mobile, DHCP rotation)
                        if (SameSubnet20(ip1, ip2)) continue;

                        if (!userConcurrencyEvents.ContainsKey(userName))
                        {
                            userConcurrencyEvents[userName] = [];
                            userConcurrencyIps[userName] = [];
                        }
                        userConcurrencyEvents[userName].Add(sessions[sj].Time);
                        userConcurrencyIps[userName].Add(ip1);
                        userConcurrencyIps[userName].Add(ip2);
                    }
                }
            }

            // Only fire if ≥3 distinct concurrent-session events (pattern, not one-off)
            foreach (var (userName, concurrencyOccurrences) in userConcurrencyEvents)
            {
                if (concurrencyOccurrences.Count < SessionConcurrencyMinOccurrences) continue;
                if (!userTeamMap.TryGetValue(userName, out var teamId)) continue;
                if (!teamMap.TryGetValue(teamId, out var scTeam)) continue;

                var distinctIps = userConcurrencyIps[userName];
                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = teamId,
                    TeamName = scTeam.Name,
                    Type = SuspicionType.SessionConcurrency,
                    Ip = string.Join(" / ", distinctIps.Take(3)),
                    Time = concurrencyOccurrences.Max(),
                    UserNames = [userName],
                    Details = BuildDetail(
                        ("Summary", $"User active from distinct /20 subnets {concurrencyOccurrences.Count}× within 10-minute windows"),
                        ("Target", TeamRef(teamId)),
                        ("User", userName),
                        ("Distinct IPs", string.Join(", ", distinctIps.Take(6))),
                        ("Occurrences", concurrencyOccurrences.Count.ToString()))
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
            if (RequiresLocalDownload(sub.TeamId, chal))
            {
                var key = (sub.TeamId, sub.ChallengeId);
                var hasDownload = teamDownloads.TryGetValue(key, out var dls) && dls.Any(d => d <= sub.SubmitTimeUtc);
                
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
                         Details = $"Target {TeamRef(sub.TeamId)} solved challenge '{sub.ChallengeName}' at {sub.SubmitTimeUtc:MM/dd HH:mm:ss} without any prior attachment download log."
                    });
                }
            }
            
            // Check 5: Solve Before Container Start
            // Flags attempts to solve a dynamic container challenge without starting a container instance.
            // Also detects if the solve happened BEFORE the container was started (impossible logic).
            if (chal.Type.IsContainer())
            {
                var key = (sub.TeamId, sub.ChallengeId);
                var hasStart = teamContainerStarts.TryGetValue(key, out var starts) && starts.Any(d => d <= sub.SubmitTimeUtc);
                
                if (!hasStart)
                {
                    string details;
                    if (starts != null && starts.Count != 0)
                    {
                        // Found starts, but all are later than solve time
                        var firstStart = starts.Min();
                        var delay = firstStart - sub.SubmitTimeUtc;
                        details = $"Target {TeamRef(sub.TeamId)} solved challenge '{sub.ChallengeName}' {delay.TotalSeconds:F0}s before container start (start at {firstStart:MM/dd HH:mm:ss}, solve at {sub.SubmitTimeUtc:MM/dd HH:mm:ss}).";
                    }
                    else
                    {
                        details = $"Target {TeamRef(sub.TeamId)} solved challenge '{sub.ChallengeName}' at {sub.SubmitTimeUtc:MM/dd HH:mm:ss} without any prior container start log.";
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
                                         Details = $"Target {TeamRef(sub.TeamId)} solved challenge '{sub.ChallengeName}' {diff.TotalMinutes:F0}m after the last container destroy (destroyed at {lastDestroy:MM/dd HH:mm}, solved at {sub.SubmitTimeUtc:MM/dd HH:mm})."
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
                if (teamChallengeOpens.TryGetValue(interactionKey, out var opTimesCheck))
                {
                    var opensBeforeSolve = opTimesCheck.Where(t => t <= sub.SubmitTimeUtc).ToList();
                    if (opensBeforeSolve.Count > 0)
                    {
                        var firstOpen = opensBeforeSolve.Min();
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
                                 Details = $"Target {TeamRef(sub.TeamId)} solved challenge '{sub.ChallengeName}' in {durationOpen.TotalSeconds:F1}s after opening it (opened at {firstOpen:MM/dd HH:mm:ss}, solved at {sub.SubmitTimeUtc:MM/dd HH:mm:ss})."
                             }); 
                        }
                    }
                }

                // 7b. Fast Solve (Download): Solved immediately after downloading attachment
                // Applies only if challenge has an attachment.
                if (RequiresLocalDownload(sub.TeamId, chal) &&
                    teamDownloads.TryGetValue(interactionKey, out var dlTimesCheck))
                {
                     var downloadsBeforeSolve = dlTimesCheck.Where(t => t <= sub.SubmitTimeUtc).ToList();
                     if (downloadsBeforeSolve.Count > 0)
                     {
                         var firstDl = downloadsBeforeSolve.Min();
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
                                  Details = $"Target {TeamRef(sub.TeamId)} solved challenge '{sub.ChallengeName}' in {durationDl.TotalSeconds:F1}s after downloading the attachment (downloaded at {firstDl:MM/dd HH:mm:ss}, solved at {sub.SubmitTimeUtc:MM/dd HH:mm:ss})."
                              });
                         }
                     }
                }

                // 7c. Fast Solve (Container): Solved immediately after container start
                // Applies if challenge is a container type AND has NO attachment (Blackbox).
                // If it has both, we generally prioritize Download check, but checking container for blackbox is key.
                if (chal.Type.IsContainer() && !RequiresLocalDownload(sub.TeamId, chal) &&
                    teamContainerStarts.TryGetValue(interactionKey, out var stTimesCheck))
                {
                     var startsBeforeSolve = stTimesCheck.Where(t => t <= sub.SubmitTimeUtc).ToList();
                     if (startsBeforeSolve.Count > 0)
                     {
                         var firstStart = startsBeforeSolve.Min();
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
                                  Details = $"Target {TeamRef(sub.TeamId)} solved challenge '{sub.ChallengeName}' in {durationStart.TotalSeconds:F1}s after starting the container (started at {firstStart:MM/dd HH:mm:ss}, solved at {sub.SubmitTimeUtc:MM/dd HH:mm:ss})."
                              });
                         }
                     }
                }
            }

            // Check A: Zero Wrong Attempts
            // A legitimate solver typically submits several wrong flags before the correct one.
            // Solving a dynamic-flag challenge on the very first attempt indicates the flag was received externally.
            // Suppressed for easy challenges (high zero-attempt rate) and challenges with few solvers.
            if (chal.Type.IsDynamic() &&
                !IsChallengeEasy(sub.ChallengeId) &&
                challengeSolveCount.GetValueOrDefault(sub.ChallengeId, 0) >= 5)
            {
                var waKey = (sub.TeamId, sub.ChallengeId);
                var wrongsBefore = wrongByTeamChallenge.TryGetValue(waKey, out var wrongs)
                    ? wrongs.Count(w => w.SubmitTimeUtc < sub.SubmitTimeUtc)
                    : 0;

                if (wrongsBefore == 0)
                {
                    report.AbnormalSolves.Add(new AbnormalSolveResult
                    {
                        TeamId = sub.TeamId,
                        TeamName = sub.TeamName,
                        ChallengeId = sub.ChallengeId,
                        ChallengeName = sub.ChallengeName,
                        Type = SuspicionType.ZeroWrongAttempts,
                        SolveTime = sub.SubmitTimeUtc,
                        Details = $"Target {TeamRef(sub.TeamId)} solved dynamic challenge '{sub.ChallengeName}' correctly on the first attempt with zero wrong submissions prior to {sub.SubmitTimeUtc:MM/dd HH:mm:ss}."
                    });
                }
            }

            // Check D: Adaptive Fast Solve (community-relative threshold)
            // Fires when solve time < 5% of community median AND median > 60 min (genuinely hard challenge).
            // Suppressed for easy challenges, challenges with few data points, and when a
            // specialist cohort (≥3 teams) solved it equally quickly (bimodal skill distribution).
            if (!IsChallengeEasy(sub.ChallengeId) &&
                challengeSolveCount.TryGetValue(sub.ChallengeId, out var commSolveCount) && commSolveCount >= 8)
            {
                var teamOffset = (sub.SubmitTimeUtc - game.StartTimeUtc).TotalMinutes;
                var medianOffset = challengeMedianSolveOffset.GetValueOrDefault(sub.ChallengeId);
                var firstSolveTime = challengeFirstSolveTime.GetValueOrDefault(sub.ChallengeId);

                if (medianOffset > 60 && teamOffset > 0 && teamOffset < medianOffset * 0.05)
                {
                    // Cohort suppression: if ≥3 other teams also solved it this quickly,
                    // it's a legitimate specialist cluster — not a single outlier cheater.
                    var fastCohortCount = submissions.Count(s =>
                        s.ChallengeId == sub.ChallengeId &&
                        s.TeamId != sub.TeamId &&
                        (s.SubmitTimeUtc - game.StartTimeUtc).TotalMinutes < medianOffset * 0.15);

                    if (fastCohortCount < 3)
                    {
                        var isFirstBlood = sub.SubmitTimeUtc <= firstSolveTime.AddSeconds(1);
                        report.AbnormalSolves.Add(new AbnormalSolveResult
                        {
                            TeamId = sub.TeamId,
                            TeamName = sub.TeamName,
                            ChallengeId = sub.ChallengeId,
                            ChallengeName = sub.ChallengeName,
                            Type = SuspicionType.AdaptiveFastSolve,
                            SolveTime = sub.SubmitTimeUtc,
                            Details = $"Target {TeamRef(sub.TeamId)} solved '{sub.ChallengeName}' in {teamOffset:F0}m " +
                                      $"while community median was {medianOffset:F0}m ({teamOffset / medianOffset:P0} of median). " +
                                      (isFirstBlood
                                          ? "This was the first blood."
                                          : $"First blood was at {firstSolveTime:MM/dd HH:mm}.")
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

        const int sequenceCommonThreshold = 3;
        const double sequenceRsiThreshold = 0.85;
        for (var i = 0; i < topTeams.Count; i++)
        {
            for (var j = i + 1; j < topTeams.Count; j++)
            {
                var teamA = topTeams[i];
                var teamB = topTeams[j];

                var sharedChallenges = teamA.Sequence.Intersect(teamB.Sequence).Distinct().ToList();
                if (sharedChallenges.Count < sequenceCommonThreshold)
                    continue;

                var unionCount = teamA.Sequence.Union(teamB.Sequence).Count();
                if (unionCount == 0)
                    continue;

                var jaccard = (double)sharedChallenges.Count / unionCount;
                var lcs = GetLongestCommonSubsequence(teamA.Sequence, teamB.Sequence);
                var minLen = Math.Min(teamA.Sequence.Count, teamB.Sequence.Count);
                var lcsScore = minLen == 0 ? 0 : (double)lcs.Count / minLen;
                var rsi = (jaccard * 0.7) + (lcsScore * 0.3);

                if (rsi < sequenceRsiThreshold)
                    continue;

                var teamAUsers = teamMap[teamA.TeamId].Members
                    .Select(m => m.UserName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .Distinct()
                    .Take(6)
                    .ToList();
                var teamBUsers = teamMap[teamB.TeamId].Members
                    .Select(m => m.UserName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .Distinct()
                    .Take(6)
                    .ToList();

                var pairTime = teamA.Raw.Last().SubmitTimeUtc > teamB.Raw.Last().SubmitTimeUtc
                    ? teamA.Raw.Last().SubmitTimeUtc
                    : teamB.Raw.Last().SubmitTimeUtc;
                var lcsTitles = lcs
                    .Select(cid => challengeMap.TryGetValue(cid, out var c) ? c.Title : cid.ToString())
                    .Distinct()
                    .Take(8);
                var lcsSummary = string.Join(", ", lcsTitles);

                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = teamA.TeamId,
                    TeamName = teamMap[teamA.TeamId].Name,
                    Type = SuspicionType.SequenceSimilarity,
                    Ip = $"RSI {rsi:P1}",
                    Time = pairTime,
                    Details = BuildDetail(
                        ("Summary", "Solve order is highly similar to another team"),
                        ("Target", TeamRef(teamA.TeamId)),
                        ("Source team", TeamRef(teamB.TeamId)),
                        ("RSI", rsi.ToString("P1")),
                        ("Jaccard", jaccard.ToString("P1")),
                        ("LCS", $"{lcs.Count}/{minLen}"),
                        ("Common solved challenges", sharedChallenges.Count.ToString()),
                        ("Shared sequence sample", lcsSummary)),
                    RelatedTeams = [teamMap[teamB.TeamId].Name],
                    UserNames = teamAUsers,
                    RelatedUsers = teamBUsers
                });

                report.IpAnalysis.Add(new IpAnalysisResult
                {
                    TeamId = teamB.TeamId,
                    TeamName = teamMap[teamB.TeamId].Name,
                    Type = SuspicionType.SequenceSimilarity,
                    Ip = $"RSI {rsi:P1}",
                    Time = pairTime,
                    Details = BuildDetail(
                        ("Summary", "Solve order is highly similar to another team"),
                        ("Target", TeamRef(teamB.TeamId)),
                        ("Source team", TeamRef(teamA.TeamId)),
                        ("RSI", rsi.ToString("P1")),
                        ("Jaccard", jaccard.ToString("P1")),
                        ("LCS", $"{lcs.Count}/{minLen}"),
                        ("Common solved challenges", sharedChallenges.Count.ToString()),
                        ("Shared sequence sample", lcsSummary)),
                    RelatedTeams = [teamMap[teamA.TeamId].Name],
                    UserNames = teamBUsers,
                    RelatedUsers = teamAUsers
                });

                // Check C: Temporal Cascade Correlation (Solution Relay)
                // Beyond sequence order, check if one team consistently solves SHORTLY AFTER the other
                // with a constant lag and low variance — a pattern consistent with flag sharing via messaging.
                // Only run this when RSI > 0.5 (teams are meaningfully similar).
                if (rsi >= 0.7 && sharedChallenges.Count >= 6)
                {
                    // Collect lags: B.solveTime - A.solveTime for each common challenge
                    var solveTimesA = teamA.Raw.ToDictionary(s => s.ChallengeId, s => s.SubmitTimeUtc);
                    var solveTimesB = teamB.Raw.ToDictionary(s => s.ChallengeId, s => s.SubmitTimeUtc);

                    // Direction A→B: positive lag means B solved after A
                    var lagsAtoB = sharedChallenges
                        .Where(cid => solveTimesA.ContainsKey(cid) && solveTimesB.ContainsKey(cid))
                        .Select(cid => (solveTimesB[cid] - solveTimesA[cid]).TotalMinutes)
                        .Where(lag => lag > 1.0 && lag <= 60.0)
                        .ToList();

                    // Direction B→A: positive lag means A solved after B
                    var lagsBtoA = sharedChallenges
                        .Where(cid => solveTimesA.ContainsKey(cid) && solveTimesB.ContainsKey(cid))
                        .Select(cid => (solveTimesA[cid] - solveTimesB[cid]).TotalMinutes)
                        .Where(lag => lag > 1.0 && lag <= 60.0)
                        .ToList();

                    void ReportRelay(List<double> lags, int sourceTeamId, int receiverTeamId)
                    {
                        if (lags.Count < 6) return;

                        // Coverage: relay pattern must appear in ≥60% of common challenges.
                        // Coincidental timing clusters locally; relay sharing spans all challenges.
                        var coverageRatio = (double)lags.Count / sharedChallenges.Count;
                        if (coverageRatio < 0.60) return;

                        var mean = lags.Average();
                        var stddev = Math.Sqrt(lags.Select(l => (l - mean) * (l - mean)).Average());

                        // Constant-lag relay: mean 2–30 min, stddev < 5 min
                        if (mean < 2 || mean > 30 || stddev >= 5) return;

                        var relayDetail = BuildDetail(
                            ("Summary", "Team consistently solves challenges minutes after another team (constant-lag relay)"),
                            ("Receiver", TeamRef(receiverTeamId)),
                            ("Source", TeamRef(sourceTeamId)),
                            ("Mean lag", $"{mean:F1} min"),
                            ("Std deviation", $"{stddev:F2} min"),
                            ("Matching challenges", lags.Count.ToString()),
                            ("Pattern", $"Receiver solves ~{mean:F1}m after source across {lags.Count} challenges"));

                        report.IpAnalysis.Add(new IpAnalysisResult
                        {
                            TeamId = receiverTeamId,
                            TeamName = teamMap[receiverTeamId].Name,
                            Type = SuspicionType.SolutionRelay,
                            Ip = $"~{mean:F1}m lag",
                            Time = teamMap[receiverTeamId].Participations.FirstOrDefault()?.SuspicionScore > 0
                                ? DateTimeOffset.UtcNow
                                : pairTime,
                            RelatedTeams = [teamMap[sourceTeamId].Name],
                            Details = relayDetail
                        });
                    }

                    ReportRelay(lagsAtoB, teamA.TeamId, teamB.TeamId);
                    ReportRelay(lagsBtoA, teamB.TeamId, teamA.TeamId);
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
                         Type = SuspicionType.Burst,
                         SolveTime = subs[i].SubmitTimeUtc,
                         Details = $"Target {TeamRef(teamSeq.TeamId)} burst solve pattern: solved {burstCount} challenges in {totalSeconds:F0}s starting at {subs[i].SubmitTimeUtc:MM/dd HH:mm:ss}. Sequence: {string.Join(", ", burstNames)}."
                     });
                     
                     // Skip the challenges we just grouped into this burst
                     i += burstCount - 1;
                 }
             }
        }

        // Check B: Wrong Flag Leakage
        // A team submitted another team's valid dynamic flag as a wrong answer.
        // This catches near-misses where the stolen flag was expired, already destroyed, or typed incorrectly.
        {
            var flagToOwners = allFlagContexts
                .Where(f => !string.IsNullOrEmpty(f.Flag))
                .GroupBy(f => f.Flag)
                .ToDictionary(g => g.Key, g => g.Select(x => x.TeamId).Distinct().ToList());

            var leakageReported = new HashSet<string>();
            foreach (var wrong in wrongSubmissions)
            {
                if (!flagToOwners.TryGetValue(wrong.Answer, out var ownerTeams)) continue;
                var otherOwners = ownerTeams.Where(tid => tid != wrong.TeamId).ToList();
                if (otherOwners.Count == 0) continue;

                var dedupeKey = $"{wrong.TeamId}:{wrong.ChallengeId}:{wrong.Answer}";
                if (!leakageReported.Add(dedupeKey)) continue;

                if (!teamMap.TryGetValue(wrong.TeamId, out var leakTeam)) continue;
                challengeMap.TryGetValue(wrong.ChallengeId, out var leakChal);

                report.AbnormalSolves.Add(new AbnormalSolveResult
                {
                    TeamId = wrong.TeamId,
                    TeamName = leakTeam.Name,
                    ChallengeId = wrong.ChallengeId,
                    ChallengeName = leakChal?.Title ?? "Unknown",
                    Type = SuspicionType.WrongFlagLeakage,
                    SolveTime = wrong.SubmitTimeUtc,
                    Details = BuildDetail(
                        ("Summary", "Team submitted another team's valid dynamic flag as a wrong answer"),
                        ("Target", TeamRef(wrong.TeamId)),
                        ("Challenge", leakChal?.Title ?? "Unknown"),
                        ("Flag owner teams", string.Join(", ", otherOwners.Select(TeamRef))),
                        ("Attempted at", wrong.SubmitTimeUtc.ToString("MM/dd HH:mm:ss")))
                });
            }
        }

        // Check E: Directed Solving
        // A team that opens almost exactly the challenges they solve (minimal exploratory browsing)
        // is receiving external guidance about which challenges to attempt.
        // Requires ≥8 solves, ratio <1.05, and the community median ratio must be ≥1.5
        // (if the whole game is focused, suppress — not suspicious in that context).
        {
            var teamUniqueOpens = teamChallengeOpens.Keys
                .GroupBy(k => k.TeamId)
                .ToDictionary(g => g.Key, g => g.Select(k => k.ChallengeId).ToHashSet());

            var teamSolvedSets = submissions
                .GroupBy(s => s.TeamId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.ChallengeId).ToHashSet());

            // Community median open/solve ratio — if teams generally browse very little, suppress
            var communityRatios = teamSolvedSets
                .Select(kvp =>
                {
                    var teamOpened = teamUniqueOpens.TryGetValue(kvp.Key, out var op) ? op.Count : 0;
                    return (kvp.Value.Count >= 4 && teamOpened > 0)
                        ? (double?)((double)teamOpened / kvp.Value.Count)
                        : null;
                })
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .OrderBy(r => r)
                .ToList();

            var communityMedianRatio = communityRatios.Count > 0
                ? communityRatios[communityRatios.Count / 2]
                : 2.0;

            // If the whole game is focused (short sprint, practice round), don't flag
            if (communityMedianRatio >= 1.5)
            {
                foreach (var (teamId, solved) in teamSolvedSets)
                {
                    if (solved.Count < 8) continue;
                    var opened = teamUniqueOpens.TryGetValue(teamId, out var op) ? op.Count : 0;
                    if (opened == 0) continue;

                    var explorationRatio = (double)opened / solved.Count;
                    if (explorationRatio >= 1.05) continue;

                    if (!teamMap.TryGetValue(teamId, out var dsTeam)) continue;
                    var lastSolve = submissions
                        .Where(s => s.TeamId == teamId)
                        .Max(s => s.SubmitTimeUtc);

                    report.AbnormalSolves.Add(new AbnormalSolveResult
                    {
                        TeamId = teamId,
                        TeamName = dsTeam.Name,
                        ChallengeId = 0,
                        ChallengeName = string.Empty,
                        Type = SuspicionType.DirectedSolving,
                        SolveTime = lastSolve,
                        Details = BuildDetail(
                            ("Summary", "Team opened almost only the challenges they solved — no exploratory browsing"),
                            ("Target", TeamRef(teamId)),
                            ("Challenges solved", solved.Count.ToString()),
                            ("Challenges opened", opened.ToString()),
                            ("Open/solve ratio", explorationRatio.ToString("F2")),
                            ("Community median ratio", communityMedianRatio.ToString("F2")))
                    });
                }
            }
        }

        // Check H: High Wrong Rate / Automated Pattern
        // Detects brute-force flag submission (many wrong answers per challenge in a short window)
        // and machine-speed submission intervals (< 2s between attempts — likely scripted).
        {
            const int BurstWrongThreshold = 40;
            const int AutoSpeedCount = 10;

            var wrongByTeamChallengeForH = wrongSubmissions
                .GroupBy(s => (s.TeamId, s.ChallengeId))
                .Where(g => g.Count() >= 5)
                .ToList();

            foreach (var group in wrongByTeamChallengeForH)
            {
                var wrongs = group.OrderBy(w => w.SubmitTimeUtc).ToList();
                if (!teamMap.TryGetValue(group.Key.TeamId, out var hwTeam)) continue;
                challengeMap.TryGetValue(group.Key.ChallengeId, out var hwChal);
                var reportedTypes = new HashSet<string>();

                // Burst wrong rate: >= BurstWrongThreshold wrong submissions within 60 seconds
                if (wrongs.Count >= BurstWrongThreshold)
                {
                    for (int wi = 0; wi < wrongs.Count; wi++)
                    {
                        var windowEnd = wrongs[wi].SubmitTimeUtc.AddSeconds(60);
                        var burst = wrongs.Count(w =>
                            w.SubmitTimeUtc >= wrongs[wi].SubmitTimeUtc &&
                            w.SubmitTimeUtc <= windowEnd);

                        if (burst >= BurstWrongThreshold && reportedTypes.Add(SuspicionType.HighWrongRate))
                        {
                            // Suppress if they solved the challenge within 5 minutes of the burst —
                            // that suggests productive flag-format exploration, not blind probing.
                            var solvedAfterBurst = submissions.Any(s =>
                                s.TeamId == group.Key.TeamId &&
                                s.ChallengeId == group.Key.ChallengeId &&
                                s.SubmitTimeUtc >= wrongs[wi].SubmitTimeUtc &&
                                s.SubmitTimeUtc <= wrongs[wi].SubmitTimeUtc.AddMinutes(5));

                            if (!solvedAfterBurst)
                            {
                                report.AbnormalSolves.Add(new AbnormalSolveResult
                                {
                                    TeamId = group.Key.TeamId,
                                    TeamName = hwTeam.Name,
                                    ChallengeId = group.Key.ChallengeId,
                                    ChallengeName = hwChal?.Title ?? "Unknown",
                                    Type = SuspicionType.HighWrongRate,
                                    SolveTime = wrongs[wi].SubmitTimeUtc,
                                    Details = $"Target {TeamRef(group.Key.TeamId)} submitted {burst} wrong answers for '{hwChal?.Title ?? "Unknown"}' " +
                                              $"within 60 seconds starting at {wrongs[wi].SubmitTimeUtc:MM/dd HH:mm:ss}. No solve followed — likely blind probing."
                                });
                            }
                            break;
                        }
                    }
                }

                // Machine-speed pattern: >= AutoSpeedCount consecutive intervals < 2 seconds
                if (wrongs.Count >= AutoSpeedCount + 1)
                {
                    var intervals = wrongs
                        .Zip(wrongs.Skip(1))
                        .Select(pair => (pair.Second.SubmitTimeUtc - pair.First.SubmitTimeUtc).TotalSeconds)
                        .ToList();

                    int machineCount = 0;
                    foreach (var iv in intervals)
                    {
                        if (iv >= 0 && iv < 2.0) machineCount++;
                        else machineCount = 0;
                        if (machineCount >= AutoSpeedCount && reportedTypes.Add(SuspicionType.AutomatedPattern))
                        {
                            report.AbnormalSolves.Add(new AbnormalSolveResult
                            {
                                TeamId = group.Key.TeamId,
                                TeamName = hwTeam.Name,
                                ChallengeId = group.Key.ChallengeId,
                                ChallengeName = hwChal?.Title ?? "Unknown",
                                Type = SuspicionType.AutomatedPattern,
                                SolveTime = wrongs.First().SubmitTimeUtc,
                                Details = $"Target {TeamRef(group.Key.TeamId)} submitted flags for '{hwChal?.Title ?? "Unknown"}' at machine speed " +
                                          $"(< 2s intervals sustained over {machineCount} submissions). Likely scripted."
                            });
                            break;
                        }
                    }
                }
            }
        }

        // Check J: First Blood Anomaly
        // A team that gets first blood on a hard challenge (nobody else solves for 2+ hours)
        // is flagged when that challenge's gap to second solve is very large.
        // This is a soft amplifier — it compounds other suspicion signals.
        {
            var firstBloodMap = firstSolvesData
                .GroupBy(fs => fs.ChallengeId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SubmitTimeUtc).First());

            var challSolvesByTime = submissions
                .GroupBy(s => s.ChallengeId)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SubmitTimeUtc).ToList());

            foreach (var (chalId, firstBlood) in firstBloodMap)
            {
                if (!challengeMap.TryGetValue(chalId, out var fbChal)) continue;
                var allSolves = challSolvesByTime.GetValueOrDefault(chalId);
                if (allSolves == null || allSolves.Count < 2) continue;

                var secondSolveTime = allSolves.Skip(1).First().SubmitTimeUtc;
                var solveGap = secondSolveTime - firstBlood.SubmitTimeUtc;
                if (solveGap < TimeSpan.FromHours(2)) continue;

                if (!teamMap.TryGetValue(firstBlood.TeamId, out var fbTeam)) continue;
                var fbOffset = (firstBlood.SubmitTimeUtc - game.StartTimeUtc).TotalMinutes;

                report.AbnormalSolves.Add(new AbnormalSolveResult
                {
                    TeamId = firstBlood.TeamId,
                    TeamName = fbTeam.Name,
                    ChallengeId = chalId,
                    ChallengeName = fbChal.Title,
                    Type = SuspicionType.FirstBloodAnomaly,
                    SolveTime = firstBlood.SubmitTimeUtc,
                    Details = BuildDetail(
                        ("Summary", "First blood on a hard challenge — second solve was 2+ hours later"),
                        ("Target", TeamRef(firstBlood.TeamId)),
                        ("Challenge", fbChal.Title),
                        ("First blood at", firstBlood.SubmitTimeUtc.ToString("MM/dd HH:mm")),
                        ("Second solve gap", $"{solveGap.TotalHours:F1}h"),
                        ("Team solve offset", $"{fbOffset:F0}m from game start"))
                });
            }
        }

        // Tier Gating: Soft signals only score if the team already has at least one Hard/Strong signal.
        // Prevents soft-signal stacking (SubnetOverlap + DirectedSolving + AdaptiveFastSolve = false Red).
        {
            var teamsWithStrongSignal = new HashSet<int>();

            foreach (var item in report.IpAnalysis)
                if (!SuspicionType.IsSoft(item.Type))
                    teamsWithStrongSignal.Add(item.TeamId);

            foreach (var item in report.AbnormalSolves)
                if (!SuspicionType.IsSoft(item.Type))
                    teamsWithStrongSignal.Add(item.TeamId);

            report.IpAnalysis = report.IpAnalysis
                .Where(i => !SuspicionType.IsSoft(i.Type) || teamsWithStrongSignal.Contains(i.TeamId))
                .ToList();

            report.AbnormalSolves = report.AbnormalSolves
                .Where(a => !SuspicionType.IsSoft(a.Type) || teamsWithStrongSignal.Contains(a.TeamId))
                .ToList();
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
                         var details = $"Target {TeamRef(team.Id)} collusion-group signal. {group.Details}";
                         await suspicionService.AddSuspicion(part, SuspicionType.CollusionGroup, details, token: token);
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

    private static DownloadEventLogMetadata? ParseDownloadMetadata(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count <= DownloadEventLogMetadata.ValuesIndex)
            return null;

        return DownloadEventLogMetadata.TryParse(values[DownloadEventLogMetadata.ValuesIndex], out var metadata)
            ? metadata
            : null;
    }

    private static string? GetSubnet28(string ip)
    {
        if (!IPAddress.TryParse(ip, out var addr)) return null;
        var bytes = addr.GetAddressBytes();
        if (bytes.Length != 4) return null; // IPv4 only
        // /28: mask 255.255.255.240 — zero the last 4 bits of the final octet
        var lastByte = (byte)(bytes[3] & 0xF0);
        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{lastByte}/28";
    }

    private static bool SameSubnet20(string ip1, string ip2)
    {
        if (!IPAddress.TryParse(ip1, out var addr1) || !IPAddress.TryParse(ip2, out var addr2)) return false;
        var b1 = addr1.GetAddressBytes();
        var b2 = addr2.GetAddressBytes();
        if (b1.Length != 4 || b2.Length != 4) return false;
        // /20: mask 255.255.240.0 — first 20 bits = b[0], b[1], upper nibble of b[2]
        return b1[0] == b2[0] && b1[1] == b2[1] && (b1[2] & 0xF0) == (b2[2] & 0xF0);
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
