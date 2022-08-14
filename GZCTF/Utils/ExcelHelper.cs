using CTFServer.Models.Request.Game;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace CTFServer.Utils;

public static class ExcelHelper
{
    private static readonly string[] CommonScoreboardHeader = { "排名", "战队", "解题数量", "得分时间", "总分" };
    private static readonly string[] CommonTeamHeader = { "排名", "战队", "总分", "队伍人数", "队员信息" };

    public static MemoryStream GetExcel(ScoreboardModel scoreboard, Game game)
    {
        var workbook = new XSSFWorkbook();
        var boardSheet = workbook.CreateSheet("排行榜");
        var headerStyle = GetHeaderStyle(workbook);
        var challIds = WriteBoardHeader(boardSheet, headerStyle, scoreboard);
        WriteBoardContent(boardSheet, scoreboard, challIds);

        var teamSheet = workbook.CreateSheet("战队信息");
        WriteTeamHeader(teamSheet, headerStyle, scoreboard, game);
        WriteTeamContent(teamSheet, scoreboard);

        var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream;
    }

    private static ICellStyle GetHeaderStyle(XSSFWorkbook workbook)
    {
        var style = workbook.CreateCellStyle();
        var boldFontStyle = workbook.CreateFont();

        boldFontStyle.IsBold = true;
        style.SetFont(boldFontStyle);
        style.BorderBottom = BorderStyle.Thick;
        style.VerticalAlignment = VerticalAlignment.Center;
        style.Alignment = HorizontalAlignment.Center;

        return style;
    }

    private static int[] WriteBoardHeader(ISheet sheet, ICellStyle style, ScoreboardModel scoreboard)
    {
        var row = sheet.CreateRow(0);
        var colIndex = 0;
        var challIds = new List<int>();

        foreach (var col in CommonScoreboardHeader)
        {
            var cell = row.CreateCell(colIndex);
            cell.SetCellValue(col);
            cell.CellStyle = style;
            colIndex++;
        }

        foreach (var type in scoreboard.Challenges)
        {
            foreach (var chall in type.Value)
            {
                var cell = row.CreateCell(colIndex);
                cell.SetCellValue(chall.Title);
                cell.CellStyle = style;
                challIds.Add(chall.Id);
                colIndex++;
            }
        }

        return challIds.ToArray();
    }

    private static void WriteBoardContent(ISheet sheet, ScoreboardModel scoreboard, int[] challIds)
    {
        var rowIndex = 1;
        foreach (var item in scoreboard.Items)
        {
            var row = sheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue(item.Rank);
            row.CreateCell(1).SetCellValue(item.Name);
            row.CreateCell(2).SetCellValue(item.SolvedCount);
            row.CreateCell(3).SetCellValue(item.LastSubmissionTime.ToString("u"));
            row.CreateCell(4).SetCellValue(item.Score);

            var colIndex = 5;
            foreach (var challId in challIds)
            {
                var chall = item.Challenges.Single(c => c.Id == challId);
                row.CreateCell(colIndex).SetCellValue(chall.Score);
                colIndex++;
            }

            rowIndex++;
        }
    }

    private static void WriteTeamHeader(ISheet sheet, ICellStyle style, ScoreboardModel scoreboard, Game game)
    {
        var teamMemberCount = game.TeamMemberCountLimit;
        if (teamMemberCount == 0)
            teamMemberCount = scoreboard.Items.Max(i => i.Team?.Members?.Count ?? 1);

        var row = sheet.CreateRow(0);
        var colIndex = 0;

        foreach (var col in CommonTeamHeader)
        {
            var cell = row.CreateCell(colIndex);
            cell.SetCellValue(col);
            cell.CellStyle = style;
            colIndex++;
        }

        sheet.AddMergedRegion(new CellRangeAddress(0, 0, colIndex - 1, colIndex - 1 + teamMemberCount));
    }

    private static void WriteTeamContent(ISheet sheet, ScoreboardModel scoreboard)
    {
        var rowIndex = 1;
        foreach (var item in scoreboard.Items)
        {
            var row = sheet.CreateRow(rowIndex);
            var team = item.Team!;

            row.CreateCell(0).SetCellValue(item.Rank);
            row.CreateCell(1).SetCellValue(item.Name);
            row.CreateCell(2).SetCellValue(item.Score);
            row.CreateCell(3).SetCellValue(team.Members!.Count);

            var captain = team.Members!.First(u => u.Captain);
            row.CreateCell(4).SetCellValue($"[{captain.StudentNumber}]{captain.UserName}({captain.RealName})");

            var others = team.Members.Where(u => u.Id != captain.Id);

            var colIndex = 5;
            foreach (var u in others)
            {
                row.CreateCell(colIndex).SetCellValue($"[{captain.StudentNumber}]{u.UserName}({u.RealName})");
                colIndex++;
            }

            rowIndex++;
        }
    }
}