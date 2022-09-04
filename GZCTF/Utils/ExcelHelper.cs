using CTFServer.Models.Request.Game;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace CTFServer.Utils;

public static class ExcelHelper
{
    private static readonly string[] CommonScoreboardHeader = { "排名", "战队", "解题数量", "得分时间", "总分" };
    private static readonly string[] CommonTeamHeader = { "排名", "战队", "总分", "队伍人数", "队长" };

    public static MemoryStream GetExcel(ScoreboardModel scoreboard, Game game)
    {
        var workbook = new XSSFWorkbook();
        var boardSheet = workbook.CreateSheet("排行榜");
        var headerStyle = GetHeaderStyle(workbook);
        var challIds = WriteBoardHeader(boardSheet, headerStyle, scoreboard, game);
        WriteBoardContent(boardSheet, scoreboard, challIds, game);

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
        style.BorderBottom = BorderStyle.Medium;
        style.VerticalAlignment = VerticalAlignment.Center;
        style.Alignment = HorizontalAlignment.Center;

        return style;
    }

    private static int[] WriteBoardHeader(ISheet sheet, ICellStyle style, ScoreboardModel scoreboard, Game game)
    {
        var row = sheet.CreateRow(0);
        var colIndex = 0;
        var challIds = new List<int>();

        foreach (var col in CommonScoreboardHeader)
        {
            var cell = row.CreateCell(colIndex++);
            cell.SetCellValue(col);
            cell.CellStyle = style;
        }

        if (game.Organizations is not null && game.Organizations.Count > 0)
        {
            var cell = row.CreateCell(colIndex++);
            cell.SetCellValue("所属组织");
            cell.CellStyle = style;
        }

        foreach (var type in scoreboard.Challenges)
        {
            foreach (var chall in type.Value)
            {
                var cell = row.CreateCell(colIndex++);
                cell.SetCellValue(chall.Title);
                cell.CellStyle = style;
                challIds.Add(chall.Id);
            }
        }

        return challIds.ToArray();
    }

    private static void WriteBoardContent(ISheet sheet, ScoreboardModel scoreboard, int[] challIds, Game game)
    {
        var rowIndex = 1;
        var withOrg = game.Organizations is not null && game.Organizations.Count > 0;
        foreach (var item in scoreboard.Items)
        {
            var row = sheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue(item.Rank);
            row.CreateCell(1).SetCellValue(item.Name);
            row.CreateCell(2).SetCellValue(item.SolvedCount);
            row.CreateCell(3).SetCellValue(item.LastSubmissionTime.ToString("u"));
            row.CreateCell(4).SetCellValue(item.Score);

            var colIndex = 5;
            if (withOrg)
                row.CreateCell(colIndex++).SetCellValue(item.Organization);

            foreach (var challId in challIds)
            {
                var chall = item.Challenges.Single(c => c.Id == challId);
                row.CreateCell(colIndex++).SetCellValue(chall.Score);
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

        if (teamMemberCount == 1)
            return;

        var titleCell = row.CreateCell(colIndex);
        titleCell.SetCellValue("队员信息");
        titleCell.CellStyle = style;

        var teamMemberStart = colIndex;
        colIndex++;

        for (var i = 0; i < teamMemberCount - 2; ++i)
        {
            var cell = row.CreateCell(colIndex);
            cell.CellStyle = style;
            colIndex++;
        }

        if (teamMemberCount > 2)
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, teamMemberStart, teamMemberStart + teamMemberCount - 2));
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
            row.CreateCell(4).SetCellValue($"{captain.UserName}({captain.RealName})[{captain.StudentNumber}]");

            var others = team.Members.Where(u => u.Id != captain.Id);

            var colIndex = 5;
            foreach (var u in others)
            {
                row.CreateCell(colIndex).SetCellValue($"{u.UserName}({u.RealName})[{captain.StudentNumber}]");
                colIndex++;
            }

            rowIndex++;
        }
    }
}