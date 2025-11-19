using GZCTF.Models.Internal;
using UserMetadataField = GZCTF.Models.Internal.UserMetadataField;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// User metadata fields configuration for client
/// </summary>
public class UserMetadataFieldsModel
{
    /// <summary>
    /// Available metadata fields
    /// </summary>
    public List<UserMetadataField> Fields { get; set; } = [];
}
