namespace GZCTF.Models.Internal;

public class ContainerInfo
{
    /// <summary>
    /// Container ID
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Container name
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Container image
    /// </summary>
    public string Image { get; set; } = default!;

    /// <summary>
    /// Container state
    /// </summary>
    public string State { get; set; } = default!;
}
