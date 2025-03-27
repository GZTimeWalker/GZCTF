using GZCTF.Models.Request.Game;
using Microsoft.Extensions.Localization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GZCTF.Utils;

public class ExcelHelper(IStringLocalizer<Program> localizer)
{
    const string Empty = "<empty>";
    const string Split = " / ";

    readonly string[] _commonScoreboardHeader =
    [
        localizer[nameof(Resources.Program.Header_Ranking)],
        localizer[nameof(Resources.Program.Header_Team)],
        localizer[nameof(Resources.Program.Header_Captain)],
        localizer[nameof(Resources.Program.Header_Member)],
        localizer[nameof(Resources.Program.Header_RealName)],
        localizer[nameof(Resources.Program.Header_Email)],
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
        var boardSheet = workbook.CreateSheet(localizer[nameof(Resources.Program.Scoreboard_Title)]);
        var headerStyle = GetHeaderStyle(workbook);
        var challIds = WriteBoardHeader(boardSheet, headerStyle, scoreboard, game);
        WriteBoardContent(boardSheet, scoreboard, challIds, game);

        var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream;
    }

    public MemoryStream GetSubmissionExcel(IEnumerable<Submission> submissions)
    {
        var workbook = new XSSFWorkbook();
        var subSheet = workbook.CreateSheet(localizer[nameof(Resources.Program.Scoreboard_AllSubmissions)]);
        var headerStyle = GetHeaderStyle(workbook);
        WriteSubmissionHeader(subSheet, headerStyle);
        WriteSubmissionContent(subSheet, submissions);

        var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream;
    }

    static ICellStyle GetHeaderStyle(XSSFWorkbook workbook)
    {
        var style = workbook.CreateCellStyle();
        var boldFontStyle = workbook.CreateFont();

        boldFontStyle.IsBold = true;
        style.SetFont(boldFontStyle);
        style.BorderBottom = BorderStyle.Medium;
        style.VerticalAlignment = VerticalAlignment.Center;
        style.Alignment = HorizontalAlignment.Center;

        return style;
    }

    void WriteSubmissionHeader(ISheet sheet, ICellStyle style)
    {
        var row = sheet.CreateRow(0);
        var colIndex = 0;

        foreach (var col in _commonSubmissionHeader)
        {
            var cell = row.CreateCell(colIndex++);
            cell.SetCellValue(col);
            cell.CellStyle = style;
        }
    }

    void WriteSubmissionContent(ISheet sheet, IEnumerable<Submission> submissions)
    {
        var rowIndex = 1;

        foreach (var item in submissions)
        {
            var colIndex = 0;
            var row = sheet.CreateRow(rowIndex);
            row.CreateCell(colIndex++).SetCellValue(item.Status.ToShortString(localizer));
            row.CreateCell(colIndex++).SetCellValue(item.SubmitTimeUtc.ToString("u"));
            row.CreateCell(colIndex++).SetCellValue(item.TeamName);
            row.CreateCell(colIndex++).SetCellValue(item.UserName);
            row.CreateCell(colIndex++).SetCellValue(item.ChallengeName);
            row.CreateCell(colIndex++).SetCellValue(item.Answer);
            row.CreateCell(colIndex).SetCellValue(item.User?.Email ?? string.Empty);

            rowIndex++;
        }
    }

    int[] WriteBoardHeader(ISheet sheet, ICellStyle style, ScoreboardModel scoreboard, Game game)
    {
        var row = sheet.CreateRow(0);
        var colIndex = 0;
        var challIds = new List<int>();
        var withOrg = game.Divisions is not null && game.Divisions.Count > 0;

        foreach (var col in _commonScoreboardHeader)
        {
            var cell = row.CreateCell(colIndex++);
            cell.SetCellValue(col);
            cell.CellStyle = style;

            if (!withOrg || colIndex != 2)
                continue;

            cell = row.CreateCell(colIndex++);
            cell.SetCellValue(localizer[nameof(Resources.Program.Scoreboard_BelongingDivision)]);
            cell.CellStyle = style;
        }

        foreach (var type in scoreboard.Challenges)
            foreach (var chall in type.Value)
            {
                var cell = row.CreateCell(colIndex++);
                cell.SetCellValue(chall.Title);
                cell.CellStyle = style;
                challIds.Add(chall.Id);
            }

        return challIds.ToArray();
    }

    static void WriteBoardContent(ISheet sheet, ScoreboardModel scoreboard, int[] challIds, Game game)
    {
        var rowIndex = 1;
        var withOrg = game.Divisions is not null && game.Divisions.Count > 0;

        foreach (var item in scoreboard.Items.Values)
        {
            var colIndex = 0;
            var row = sheet.CreateRow(rowIndex);
            row.CreateCell(colIndex++).SetCellValue(item.Rank);
            row.CreateCell(colIndex++).SetCellValue(item.Name);

            if (withOrg)
                row.CreateCell(colIndex++).SetCellValue(item.Division);

            row.CreateCell(colIndex++).SetCellValue(TakeIfNotEmpty(item.TeamInfo?.Captain?.UserName));

            var members = item.Participants ?? [];

            row.CreateCell(colIndex++)
                .SetCellValue(string.Join(Split, members.Select(m => TakeIfNotEmpty(m.UserName))));
            row.CreateCell(colIndex++)
                .SetCellValue(string.Join(Split, members.Select(m => TakeIfNotEmpty(m.RealName))));
            row.CreateCell(colIndex++)
                .SetCellValue(string.Join(Split, members.Select(m => TakeIfNotEmpty(m.Email))));
            row.CreateCell(colIndex++)
                .SetCellValue(string.Join(Split, members.Select(m => TakeIfNotEmpty(m.StdNumber))));
            row.CreateCell(colIndex++)
                .SetCellValue(string.Join(Split, members.Select(m => TakeIfNotEmpty(m.PhoneNumber))));

            row.CreateCell(colIndex++).SetCellValue(item.SolvedCount);
            row.CreateCell(colIndex++).SetCellValue(item.LastSubmissionTime.ToString("u"));
            row.CreateCell(colIndex++).SetCellValue(item.Score);

            foreach (var challId in challIds)
            {
                var chall = item.SolvedChallenges.SingleOrDefault(c => c.Id == challId);
                row.CreateCell(colIndex++).SetCellValue(chall?.Score ?? 0);
            }

            rowIndex++;
        }
    }

    static string TakeIfNotEmpty(string? str) => string.IsNullOrWhiteSpace(str) ? Empty : str;
}
