namespace CTFServer.Models.Internal;

public class ContainerConfig
{
    public string Image { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public int MemoryLimit { get; set; } = 256;
    public int CPUCount { get; set; } = 1;
}
