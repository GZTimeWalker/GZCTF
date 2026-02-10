namespace GZCTF.Models.Internal;

public static class SuspicionType
{
    public const string StolenFlag = "StolenFlag";
    public const string SharedIP = "SharedIP";
    public const string SharedFingerprint = "SharedFingerprint";
    public const string FingerprintChurn = "FingerprintChurn";
    public const string IpChurn = "IpChurn";
    public const string UnknownIP = "UnknownIP";
    public const string CrossTeamIP = "CrossTeamIP";
    public const string TokenAbuse = "TokenAbuse";
    public const string Hoarding = "Hoarding";
    public const string Burst = "Burst";
    public const string NoDownload = "NoDownload";
    public const string NoContainer = "NoContainer";
    public const string FastSolveOpen = "FastSolve-Open";
    public const string FastSolveDownload = "FastSolve-Download";
    public const string FastSolveContainer = "FastSolve-Container";
    public const string SequenceSimilarity = "SequenceSimilarity";
    public const string CollusionGroup = "CollusionGroup";

    public static readonly Dictionary<string, (int Weight, string Description)> Defaults = new()
    {
        { StolenFlag, (100, "Flag stolen from another team") },
        { SharedIP, (10, "Multiple team members using same IP") },
        { SharedFingerprint, (60, "Multiple users with same browser fingerprint") },
        { FingerprintChurn, (30, "Single user using many different browser fingerprints") },
        { IpChurn, (20, "Single user using many different IP addresses") },
        { UnknownIP, (10, "Using IP not seen in game before") },
        { CrossTeamIP, (20, "IP used by members from multiple teams") },
        { TokenAbuse, (80, "Multiple people using same submission token") },
        { Hoarding, (30, "Solved challenge long after container destroy") },
        { Burst, (30, "Multiple challenges solved in a very short time") },
        { NoDownload, (80, "Solved without downloading attachment") },
        { NoContainer, (80, "Solved without starting container") },
        { FastSolveOpen, (50, "Solved very quickly after opening challenge") },
        { FastSolveDownload, (50, "Solved very quickly after downloading attachment") },
        { FastSolveContainer, (50, "Solved very quickly after starting container") },
        { SequenceSimilarity, (40, "High similarity in solve order and timing") },
        { CollusionGroup, (10, "Member of a detected collusion group") }
    };}
