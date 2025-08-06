using System.Diagnostics.CodeAnalysis;
using GZCTF.Models.Internal;
using ConfigModel = GZCTF.Models.Data.Config;

namespace GZCTF.Services.Config;

public interface IConfigService
{
    /// <summary>
    /// Saves a configuration object of the specified type.
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="config">Object value</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task SaveConfig<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T config,
        CancellationToken token = default) where T : class;

    /// <summary>
    /// Saves a configuration object of the specified type.
    /// </summary>
    /// <param name="type">Object type</param>
    /// <param name="value">Object value</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task SaveConfig([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        object? value, CancellationToken token = default);

    /// <summary>
    /// Saves a set of configuration items.
    /// </summary>
    /// <param name="configs">Configuration items</param>
    /// <param name="token"></param>
    public Task SaveConfigSet(HashSet<ConfigModel> configs, CancellationToken token = default);

    /// <summary>
    /// Updates the API encryption key.
    /// </summary>
    public Task UpdateApiEncryptionKey(CancellationToken token = default);

    /// <summary>
    /// Decrypts the given API data.
    /// </summary>
    /// <param name="cipherText">Encrypted data</param>
    /// <returns>Decrypted data, or null if decryption fails</returns>
    public string? DecryptApiData(string cipherText);

    /// <summary>
    /// Get the ApiToken signature context.
    /// </summary>
    public Task<SignatureContext> GetApiTokenContext(CancellationToken token = default);

    /// <summary>
    /// Get the XOR key from configuration.
    /// </summary>
    public byte[] GetXorKey();

    /// <summary>
    /// Reloads the configuration from the source.
    /// </summary>
    public void ReloadConfig();
}
