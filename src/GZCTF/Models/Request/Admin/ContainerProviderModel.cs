using GZCTF.Models.Internal;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Global Container Provider Settings
/// </summary>
public class ContainerProviderModel
{
    /// <summary>
    /// Container Provider Type
    /// </summary>
    public ContainerProviderType Type { get; set; }
}
