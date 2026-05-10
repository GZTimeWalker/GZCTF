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

    // New signals
    public const string ZeroWrongAttempts = "ZeroWrongAttempts";
    public const string WrongFlagLeakage = "WrongFlagLeakage";
    public const string SolutionRelay = "SolutionRelay";
    public const string AdaptiveFastSolve = "AdaptiveFastSolve";
    public const string DirectedSolving = "DirectedSolving";
    public const string ClusteredRegistration = "ClusteredRegistration";
    public const string SubnetOverlap = "SubnetOverlap";
    public const string HighWrongRate = "HighWrongRate";
    public const string AutomatedPattern = "AutomatedPattern";
    public const string SessionConcurrency = "SessionConcurrency";
    public const string FirstBloodAnomaly = "FirstBloodAnomaly";

    // Inspector signals — automated-tool / scanner detection
    public const string HoneypotHit = "HoneypotHit";
    public const string HoneypotProtocolHit = "HoneypotProtocolHit";
    public const string HoneypotCanaryFlag = "HoneypotCanaryFlag";
    public const string HoneypotChain = "HoneypotChain";

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
        { CollusionGroup, (10, "Member of a detected collusion group") },
        { ZeroWrongAttempts, (50, "Solved dynamic challenge on first attempt with no wrong submissions") },
        { WrongFlagLeakage, (80, "Submitted another team's valid dynamic flag as a wrong answer") },
        { SolutionRelay, (60, "Consistently solves challenges shortly after another team with constant lag") },
        { AdaptiveFastSolve, (60, "Solved far faster than the community median solve time") },
        { DirectedSolving, (30, "Only opened challenges they solved — no exploratory browsing") },
        { ClusteredRegistration, (40, "Multiple team accounts registered from the same IP within 48h") },
        { SubnetOverlap, (5, "Teams share the same /24 subnet") },
        { HighWrongRate, (40, "Burst of wrong flag submissions — possible brute force") },
        { AutomatedPattern, (50, "Machine-speed flag submission intervals — likely scripted") },
        { SessionConcurrency, (30, "Same user account active from two different IPs within 10 minutes") },
        { FirstBloodAnomaly, (20, "First blood on a hard challenge not solved by others for 2+ hours") },
        { HoneypotHit, (70, "Hit a platform honeypot HTTP route — automated reconnaissance") },
        { HoneypotProtocolHit, (90, "Connected to a platform honeypot protocol service (SSH, Redis, etc.) — broad infra scan") },
        { HoneypotCanaryFlag, (100, "Submitted a canary flag exposed only via honeypot — automated scrape pipeline") },
        { HoneypotChain, (150, "Followed multiple cross-referenced honeypot baits — automated link-following scanner or agent") },
    };

    /// Hard evidence — always persisted regardless of other signals.
    public static readonly HashSet<string> HardSignals = [
        StolenFlag, WrongFlagLeakage, NoContainer, NoDownload, TokenAbuse,
        HoneypotProtocolHit, HoneypotCanaryFlag, HoneypotChain
    ];

    /// Strong evidence — filed always; Soft signals unlock only when a Strong/Hard signal exists.
    public static readonly HashSet<string> StrongSignals = [
        ZeroWrongAttempts, SolutionRelay, HighWrongRate, AutomatedPattern,
        Burst, FingerprintChurn, SharedFingerprint, CollusionGroup,
        CrossTeamIP, SequenceSimilarity,
        FastSolveOpen, FastSolveDownload, FastSolveContainer, Hoarding, SharedIP,
        HoneypotHit
    ];

    /// Returns true if the signal is "Soft" — low-confidence, suppressible without corroboration.
    public static bool IsSoft(string ruleCode) =>
        !HardSignals.Contains(ruleCode) && !StrongSignals.Contains(ruleCode);
}
