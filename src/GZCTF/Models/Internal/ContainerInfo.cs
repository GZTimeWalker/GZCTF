namespace GZCTF.Models.Internal;

public class ContainerInfo
{
    /// <summary>
    /// Container ID
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Container name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Container image
    /// </summary>
    public string Image { get; set; } = null!;

    /// <summary>
    /// Container state
    /// </summary>
    public string State { get; set; } = null!;
}
