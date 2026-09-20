using GZCTF.Utils;

namespace GZCTF.Integration.Test.Fixtures.Challenges;

public sealed record ChallengeAttachmentFixture(
    string FileName,
    string Sha256,
    string Flag);

public sealed record ChallengeModeFixture(
    string Key,
    string Title,
    ChallengeType Type,
    string? StaticFlag,
    string? FlagTemplate,
    IReadOnlyList<ChallengeAttachmentFixture> Attachments,
    string? ContainerImage,
    int ExposedPort,
    int Cpu,
    int MemoryMb,
    int StorageMb,
    string NetworkMode)
{
    public bool IsAttachment => Type.IsAttachment();
    public bool IsContainer => Type.IsContainer();
    public bool IsDynamic => Type.IsDynamic();

    public string ExpectedStaticFlag => StaticFlag
        ?? throw new InvalidOperationException($"Fixture {Key} does not have a static flag.");
}

public static class ChallengeModeFixtures
{
    public static IReadOnlyList<ChallengeModeFixture> All { get; } =
    [
        new(
            "static-attachment",
            "Static attachment challenge",
            ChallengeType.StaticAttachment,
            "flag{static-attachment}",
            null,
            [new("static.bin", "sha256:static-attachment", "flag{static-attachment}")],
            null,
            0,
            0,
            0,
            0,
            "none"),
        new(
            "dynamic-attachment",
            "Dynamic attachment challenge",
            ChallengeType.DynamicAttachment,
            null,
            null,
            [
                new("learner-a.bin", "sha256:dynamic-a", "flag{dynamic-a}"),
                new("learner-b.bin", "sha256:dynamic-b", "flag{dynamic-b}")
            ],
            null,
            0,
            0,
            0,
            0,
            "none"),
        new(
            "static-container",
            "Static container challenge",
            ChallengeType.StaticContainer,
            "flag{static-container}",
            null,
            [],
            "ghcr.io/gzctf/runtime-static:fixture",
            8080,
            2,
            256,
            512,
            "open"),
        new(
            "dynamic-container",
            "Dynamic container challenge",
            ChallengeType.DynamicContainer,
            null,
            "flag{dynamic-container-{userId}}",
            [],
            "ghcr.io/gzctf/runtime-dynamic:fixture",
            9000,
            1,
            128,
            256,
            "isolated")
    ];
}
