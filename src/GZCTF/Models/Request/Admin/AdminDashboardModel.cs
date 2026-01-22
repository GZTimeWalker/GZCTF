using GZCTF.Models.Data;
using GZCTF.Models.Request.Info;
using GZCTF.Models.Request.Game;

namespace GZCTF.Models.Request.Admin;

public class AdminDashboardModel
{
    public SystemStatsModel SystemStats { get; set; } = new();
    public List<BasicGameInfoModel> TopGames { get; set; } = [];
}

public class SystemStatsModel
{
    public int UserCount { get; set; }
    public int TeamCount { get; set; }
    public int ActiveContainerCount { get; set; }
}

public class SubmissionTrendModel
{
    public DateTimeOffset Time { get; set; }
    public int Count { get; set; }
}
