using GZCTF.Models.Request.Game;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GZCTF.Utils;

public static class ExcelHelper
{
    static readonly string[] CommonScoreboardHeader = { "排名", "战队", "队长", "队员", "学号", "手机号", "解题数量", "得分时间", "总分" };
    static readonly string[] CommonSubmissionHeader = { "提交状态", "提交时间", "战队", "用户", "题目", "提交内容", "用户邮箱" };

    public static MemoryStream GetScoreboardExcel(ScoreboardModel scoreboard, Game game)
    {
        if (scoreboard.Items.FirstOrDefault()?.TeamInfo is null)
            throw new ArgumentException("Team is not loaded");

        var workbook = new XSSFWorkbook();
        ISheet? boardSheet = workbook.CreateSheet("排行榜");
        ICellStyle headerStyle = GetHeaderStyle(workbook);
        var challIds = WriteBoardHeader(boardSheet, headerStyle, scoreboard, game);
        WriteBoardContent(boardSheet, scoreboard, challIds, game);

        var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream;
    }

    public static MemoryStream GetSubmissionExcel(IEnumerable<Submission> submissions)
    {
        var workbook = new XSSFWorkbook();
        ISheet? subSheet = workbook.CreateSheet("全部提交");
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

    static void WriteSubmissionHeader(ISheet sheet, ICellStyle style)
    {
        IRow? row = sheet.CreateRow(0);
        var colIndex = 0;

        foreach (var col in CommonSubmissionHeader)
        {
            ICell? cell = row.CreateCell(colIndex++);
            cell.SetCellValue(col);
            cell.CellStyle = style;
        }
    }

    static void WriteSubmissionContent(ISheet sheet, IEnumerable<Submission> submissions)
    {
        var rowIndex = 1;

        foreach (Submission item in submissions)
        {
            IRow? row = sheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue(item.Status.ToShortString());
            row.CreateCell(1).SetCellValue(item.SubmitTimeUTC.ToString("u"));
            row.CreateCell(2).SetCellValue(item.TeamName);
            row.CreateCell(3).SetCellValue(item.UserName);
            row.CreateCell(4).SetCellValue(item.ChallengeName);
            row.CreateCell(5).SetCellValue(item.Answer);
            row.CreateCell(6).SetCellValue(item.User.Email);

            rowIndex++;
        }
    }

    static int[] WriteBoardHeader(ISheet sheet, ICellStyle style, ScoreboardModel scoreboard, Game game)
    {
        IRow? row = sheet.CreateRow(0);
        var colIndex = 0;
        var challIds = new List<int>();
        var withOrg = game.Organizations is not null && game.Organizations.Count > 0;

        foreach (var col in CommonScoreboardHeader)
        {
            ICell? cell = row.CreateCell(colIndex++);
            cell.SetCellValue(col);
            cell.CellStyle = style;

            if (withOrg && colIndex == 2)
            {
                cell = row.CreateCell(colIndex++);
                cell.SetCellValue("所属组织");
                cell.CellStyle = style;
            }
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

        foreach (ScoreboardItem item in scoreboard.Items)
        {
            var colIndex = 0;
            IRow? row = sheet.CreateRow(rowIndex);
            row.CreateCell(colIndex++).SetCellValue(item.Rank);
            row.CreateCell(colIndex++).SetCellValue(item.Name);

            if (withOrg)
                row.CreateCell(colIndex++).SetCellValue(item.Organization);

            row.CreateCell(colIndex++).SetCellValue(item.TeamInfo!.Captain!.RealName);
            row.CreateCell(colIndex++).SetCellValue(string.Join("/", item.TeamInfo!.Members.Select(m => m.RealName)));
            row.CreateCell(colIndex++).SetCellValue(string.Join("/", item.TeamInfo!.Members.Select(m => m.StdNumber)));
            row.CreateCell(colIndex++)
                .SetCellValue(string.Join("/", item.TeamInfo!.Members.Select(m => m.PhoneNumber)));

            row.CreateCell(colIndex++).SetCellValue(item.SolvedCount);
            row.CreateCell(colIndex++).SetCellValue(item.LastSubmissionTime.ToString("u"));
            row.CreateCell(colIndex++).SetCellValue(item.Score);

            foreach (var challId in challIds)
            {
                ChallengeItem chall = item.Challenges.Single(c => c.Id == challId);
                row.CreateCell(colIndex++).SetCellValue(chall.Score);
            }

            rowIndex++;
        }
    }
}