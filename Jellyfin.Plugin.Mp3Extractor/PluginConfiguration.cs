using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Mp3Extractor;

/// <summary>
/// Plugin configuration – persisted as XML by Jellyfin.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the set of library (top-level folder) IDs for which
    /// the MP3-download feature is enabled.
    /// </summary>
    /// <remarks>
    /// IDs are stored as plain GUIDs without dashes (32 hex characters).
    /// An empty array means the feature is disabled for every library.
    /// </remarks>
    public string[] EnabledLibraryIds { get; set; } = [];
}
