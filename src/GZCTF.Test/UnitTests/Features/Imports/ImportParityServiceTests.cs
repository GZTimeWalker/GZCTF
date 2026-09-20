using GZCTF.Features.Imports.Application;
using GZCTF.Utils;
using System.Collections.Generic;
using Xunit;

namespace GZCTF.Test.UnitTests.Features.Imports;

public sealed class ImportParityServiceTests
{
    [Fact]
    public void Report_is_blocking_only_when_behavior_fields_differ()
    {
        var report = new ImportParityReport(1, 1, 1, 1,
            new Dictionary<string, int> { [nameof(ChallengeType.StaticAttachment)] = 1 },
            1, 1, 0, 1, 0,
            [new ImportParityMismatch("challenge-1", "flag", "expected", "actual")]);
        Assert.False(report.IsComplete);
        Assert.Single(report.Mismatches);
    }

    [Fact]
    public void A_warning_without_a_behavior_mismatch_is_complete()
    {
        var report = new ImportParityReport(1, 1, 1, 1, new Dictionary<string, int>(),
            0, 0, 0, 1, 1, []);
        Assert.True(report.IsComplete);
    }
}
