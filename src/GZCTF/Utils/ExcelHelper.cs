using GZCTF.Models.Request.Game;
using Microsoft.Extensions.Localization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GZCTF.Utils;

public class ExcelHelper(IStringLocalizer<Program> localizer)
{
    readonly string[] _commonScoreboardHeader =
    [
        localizer[nameof(Resources.Program.Header_Ranking)],
        localizer[nameof(Resources.Program.Header_Team)],
        localizer[nameof(Resources.Program.Header_Captain)],
        localizer[nameof(Resources.Program.Header_Member)],
        localizer[nameof(Resources.Program.Header_StdNumber)],
        localizer[nameof(Resources.Program.Header_PhoneNumber)],
        localizer[nameof(Resources.Program.Header_SolvedNumber)],
        localizer[nameof(Resources.Program.Header_ScoringTime)],
        localizer[nameof(Resources.Program.Header_TotalScore)]
    ];

    readonly string[] _commonSubmissionHeader =
    [
        localizer[nameof(Resources.Program.Header_SubmitStatus)],
        localizer[nameof(Resources.Program.Header_SubmitTime)],
        localizer[nameof(Resources.Program.Header_Team)],
        localizer[nameof(Resources.Program.Header_User)],
        localizer[nameof(Resources.Program.Header_Challenge)],
        localizer[nameof(Resources.Program.Header_SubmitContent)],
        localizer[nameof(Resources.Program.Header_Email)]
    ];

    public MemoryStream GetScoreboardExcel(ScoreboardModel scoreboard, Game game)
    {
        if (scoreboard.Items.Values.FirstOrDefault()?.TeamInfo is null)
            throw new ArgumentException(localizer[nameof(Resources.Program.Scoreboard_TeamNotLoaded)]);

        var workbook = new XSSFWorkbook();
        ISheet? boardSheet = workbook.CreateSheet(localizer[nameof(Resources.Program.Scoreboard_Title)]);
        ICellStyle headerStyle = GetHeaderStyle(workbook);
        var challIds = WriteBoardHeader(boardSheet, headerStyle, scoreboard, game);
        WriteBoardContent(boardSheet, scoreboard, challIds, game);

        var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream;
    }

    public MemoryStream GetSubmissionExcel(IEnumerable<Submission> submissions)
    {
        var workbook = new XSSFWorkbook();
        ISheet? subSheet = workbook.CreateSheet(localizer[nameof(Resources.Program.Scoreboard_AllSubmissions)]);
        ICellStyle headerStyle = GetHeaderStyle(workbook);
        WriteSubmissionHeader(subSheet, headerStyle);
        WriteSubmissionContent(subSheet, submissions);

        var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream;
    }

    static ICellStyle GetHeaderStyle(XSSFWorkbook workbook)
    {
        ICellStyle? style = workbook.CreateCellStyle();
        IFont? boldFontStyle = workbook.CreateFont();

        boldFontStyle.IsBold = true;
        style.SetFont(boldFontStyle);
        style.BorderBottom = BorderStyle.Medium;
        style.VerticalAlignment = VerticalAlignment.Center;
        style.Alignment = HorizontalAlignment.Center;

        return style;
    }

    void WriteSubmissionHeader(ISheet sheet, ICellStyle style)
    {
        IRow? row = sheet.CreateRow(0);
        var colIndex = 0;

        foreach (var col in _commonSubmissionHeader)
        {
            ICell? cell = row.CreateCell(colIndex++);
            cell.SetCellValue(col);
            cell.CellStyle = style;
        }
    }

    void WriteSubmissionContent(ISheet sheet, IEnumerable<Submission> submissions)
    {
        var rowIndex = 1;

        foreach (Submission item in submissions)
        {
            IRow? row = sheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue(item.Status.ToShortString(localizer));
            row.CreateCell(1).SetCellValue(item.SubmitTimeUtc.ToString("u"));
            row.CreateCell(2).SetCellValue(item.TeamName);
            row.CreateCell(3).SetCellValue(item.UserName);
            row.CreateCell(4).SetCellValue(item.ChallengeName);
            row.CreateCell(5).SetCellValue(item.Answer);
            row.CreateCell(6).SetCellValue(item.User.Email);

            rowIndex++;
        }
    }

    int[] WriteBoardHeader(ISheet sheet, ICellStyle style, ScoreboardModel scoreboard, Game game)
    {
        IRow? row = sheet.CreateRow(0);
        var colIndex = 0;
        var challIds = new List<int>();
        var withOrg = game.Organizations is not null && game.Organizations.Count > 0;

        foreach (var col in _commonScoreboardHeader)
        {
            ICell? cell = row.CreateCell(colIndex++);
            cell.SetCellValue(col);
            cell.CellStyle = style;

            if (!withOrg || colIndex != 2)
                continue;

            cell = row.CreateCell(colIndex++);
            cell.SetCellValue(localizer[nameof(Resources.Program.Scoreboard_BelongingOrganization)]);
            cell.CellStyle = style;
        }

        foreach (KeyValuePair<ChallengeTag, IEnumerable<ChallengeInfo>> type in scoreboard.Challenges)
            foreach (ChallengeInfo chall in type.Value)
            {
                ICell? cell = row.CreateCell(colIndex++);
                cell.SetCellValue(chall.Title);
                cell.CellStyle = style;
                challIds.Add(chall.Id);
            }

        return challIds.ToArray();
    }

    static void WriteBoardContent(ISheet sheet, ScoreboardModel scoreboard, int[] challIds, Game game)
    {
        var rowIndex = 1;
        var withOrg = game.Organizations is not null && game.Organizations.Count > 0;

        foreach (ScoreboardItem item in scoreboard.Items.Values)
        {
            var colIndex = 0;
            IRow? row = sheet.CreateRow(rowIndex);
            row.CreateCell(colIndex++).SetCellValue(item.Rank);
            row.CreateCell(colIndex++).SetCellValue(item.Name);

            if (withOrg)
                row.CreateCell(colIndex++).SetCellValue(item.Organization);

            row.CreateCell(colIndex++).SetCellValue(item.TeamInfo?.Captain?.RealName ?? string.Empty);

            var members = item.TeamInfo?.Members ?? [];

            row.CreateCell(colIndex++)
                .SetCellValue(string.Join(Split, members.Select(m => TakeIfNotEmpty(m.RealName))));
            row.CreateCell(colIndex++)
                .SetCellValue(string.Join(Split, members.Select(m => TakeIfNotEmpty(m.StdNumber))));
            row.CreateCell(colIndex++)
                .SetCellValue(string.Join(Split, members.Select(m => TakeIfNotEmpty(m.PhoneNumber))));

            row.CreateCell(colIndex++).SetCellValue(item.SolvedCount);
            row.CreateCell(colIndex++).SetCellValue(item.LastSubmissionTime.ToString("u"));
            row.CreateCell(colIndex++).SetCellValue(item.Score);

            foreach (var challId in challIds)
            {
                ChallengeItem? chall = item.SolvedChallenges.SingleOrDefault(c => c.Id == challId);
                row.CreateCell(colIndex++).SetCellValue(chall?.Score ?? 0);
            }

            rowIndex++;
        }
    }

    const string Empty = "<empty>";
    const string Split = " / ";

    static string TakeIfNotEmpty(string? str) => string.IsNullOrWhiteSpace(str) ? Empty : str;
}
