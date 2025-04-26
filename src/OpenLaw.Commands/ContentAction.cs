namespace Clarius.OpenLaw;

/// <summary>
/// The action performed by <see cref="IContentRepository.SetContentAsync(string, long, Stream)"/>.
/// </summary>
public enum ContentAction
{
    /// <summary>
    /// The given content didn't exist and was created.
    /// </summary>
    Created,
    /// <summary>
    /// The given content existed and was updated since the timestamps didn't match.
    /// </summary>
    Updated,
    /// <summary>
    /// The change was only on timestamps, no actual content changed.
    /// </summary>
    Timestamps,
    /// <summary>
    /// The given content existed but the timestamps matched, so it wasn't updated.
    /// </summary>
    Skipped,
}