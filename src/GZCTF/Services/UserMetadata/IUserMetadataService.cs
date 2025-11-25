using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InternalUserMetadataField = GZCTF.Models.Internal.UserMetadataField;

namespace GZCTF.Services;

/// <summary>
/// Provides validation and metadata field discovery services for user-defined profile attributes.
/// </summary>
public interface IUserMetadataService
{
    /// <summary>
    /// Validates the provided metadata values against the configured schema and merges them with existing values.
    /// </summary>
    /// <param name="incoming">The raw metadata dictionary coming from the client. Keys are case-insensitive.</param>
    /// <param name="existing">The currently stored metadata values, if any.</param>
    /// <param name="allowLockedWrites">When <see langword="true" />, locked fields can be written by the caller.</param>
    /// <param name="enforceLockedRequirements">When <see langword="true" />, required locked fields must still be supplied.</param>
    /// <param name="token">Cancellation token propagated to downstream storage operations.</param>
    /// <returns>A validation result containing normalized metadata values or validation errors.</returns>
    Task<UserMetadataValidationResult> ValidateAsync(
        IDictionary<string, string?>? incoming,
        IDictionary<string, string>? existing,
        bool allowLockedWrites,
        bool enforceLockedRequirements,
        CancellationToken token = default);

    /// <summary>
    /// Retrieves the list of metadata fields defined in the system, including validation constraints.
    /// </summary>
    /// <param name="token">Cancellation token propagated to downstream storage operations.</param>
    /// <returns>The metadata field descriptor collection.</returns>
    Task<IReadOnlyList<InternalUserMetadataField>> GetFieldsAsync(CancellationToken token = default);
}
