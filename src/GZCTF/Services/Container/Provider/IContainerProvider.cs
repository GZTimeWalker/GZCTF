namespace GZCTF.Services.Container.Provider;

public interface IContainerProvider<out TProvider, out TMetadata>
{
    /// <summary>
    /// Get container service provider client
    /// </summary>
    /// <returns></returns>
    public TProvider GetProvider();

    /// <summary>
    /// Get container service metadata
    /// </summary>
    /// <returns></returns>
    public TMetadata GetMetadata();
}
