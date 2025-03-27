using System.ComponentModel.DataAnnotations.Schema;

namespace GZCTF.Models.Data;

public class Instance
{
    /// <summary>
    /// Whether the challenge is solved
    /// </summary>
    public bool IsSolved { get; set; }

    /// <summary>
    /// Whether the challenge is loaded
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Last container operation time to ensure operations are not too frequent
    /// </summary>
    public DateTimeOffset LastContainerOperation { get; set; } = DateTimeOffset.MinValue;

    [NotMapped]
    public bool IsContainerOperationTooFrequent => DateTimeOffset.UtcNow - LastContainerOperation < TimeSpan.FromSeconds(10);

    #region Db Relationship

    public int? FlagId { get; set; }

    /// <summary>
    /// Flag context object
    /// </summary>
    public FlagContext? FlagContext { get; set; }

    public Guid? ContainerId { get; set; }

    /// <summary>
    /// Container object
    /// </summary>
    public Container? Container { get; set; }

    #endregion
}
