using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTFServer.Models;

public class Challenge : ChallengeBase
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
    public List<ChallengeFile> Files { get; set; } = new();

    #endregion 数据库关系

    public void Update(ChallengeBase Challenges)
    {
        foreach (var item in typeof(ChallengeBase).GetProperties())
            item.SetValue(this, item.GetValue(Challenges));
    }

    public Challenge() : base()
    {
    }

    public Challenge(ChallengeBase Challenges) : base() => Update(Challenges);
}
