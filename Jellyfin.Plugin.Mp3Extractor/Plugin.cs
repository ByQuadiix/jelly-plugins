using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Mp3Extractor;

/// <summary>
/// Main plugin class for Mp3Extractor.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Singleton reference set during plugin initialisation.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Initialises a new instance of <see cref="Plugin"/>.
    /// </summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Mp3Extractor";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("a8b6e2f4-3c1d-4e7a-9f0b-2d5c8e1f4a7b");

    /// <inheritdoc />
    public override string Description =>
        "Extrahiert die Audiospur von Video-Items als MP3 und stellt sie zum Download bereit.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = "Mp3ExtractorConfigPage",
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.configurationpage.html",
                EnableInMainMenu = false,
                DisplayName = "Mp3 Extractor"
            },
            new PluginPageInfo
            {
                Name = "Mp3ExtractorScript",
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.mp3extractor.js"
            }
        ];
    }
}
