using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTFServer.Models;

[Index(nameof(AccessLevel))]
public class Puzzle : PuzzleBase
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 当前题目分值
    /// </summary>
    [NotMapped]
    public int CurrentScore
    {
        get
        {
            if (AcceptedUserCount <= AwardCount)
                return OriginalScore - AcceptedUserCount;
            if (AcceptedUserCount > ExpectMaxCount)
                return MinScore;
            var range = OriginalScore - AwardCount - MinScore;
            return (int)(OriginalScore - AwardCount
                - Math.Floor(range * (AcceptedUserCount - AwardCount) / (float)(ExpectMaxCount - AwardCount)));
        }
    }

    #region 数据库关系
    public List<Submission> Submissions { get; set; } = new();

    #endregion 数据库关系

    public void Update(PuzzleBase puzzle)
    {
        foreach (var item in typeof(PuzzleBase).GetProperties())
            item.SetValue(this, item.GetValue(puzzle));
    }

    public Puzzle() : base()
    {
    }

    public Puzzle(PuzzleBase puzzle) : base() => Update(puzzle);
}
