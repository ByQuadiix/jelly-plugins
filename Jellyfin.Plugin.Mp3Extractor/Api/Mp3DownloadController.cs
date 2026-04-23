using System.Diagnostics;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Mp3Extractor.Api;

/// <summary>
/// Jellyfin API controller that streams the audio track of a video item as
/// an MP3 file by piping the output of the bundled FFmpeg binary.
/// </summary>
[ApiController]
[Route("Mp3Extractor")]
[Authorize(Policy = "DefaultAuthorization")]
public class Mp3DownloadController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ILogger<Mp3DownloadController> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="Mp3DownloadController"/>.
    /// </summary>
    public Mp3DownloadController(
        ILibraryManager libraryManager,
        IMediaEncoder mediaEncoder,
        ILogger<Mp3DownloadController> logger)
    {
        _libraryManager = libraryManager;
        _mediaEncoder = mediaEncoder;
        _logger = logger;
    }

    /// <summary>
    /// Streams the audio track of the specified item as an MP3 file.
    /// </summary>
    /// <param name="itemId">The Jellyfin item GUID.</param>
    /// <returns>An MP3 audio stream, or an appropriate HTTP error.</returns>
    [HttpGet("{itemId}/download")]
    [Produces("audio/mpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DownloadMp3([FromRoute] Guid itemId)
    {
        // ── 1. Resolve item ────────────────────────────────────────────────
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound("Item not found.");
        }

        if (string.IsNullOrWhiteSpace(item.Path) || !System.IO.File.Exists(item.Path))
        {
            return BadRequest("Item has no accessible file path.");
        }

        // ── 2. Check that the item's library is enabled ────────────────────
        var config = Plugin.Instance!.Configuration;
        if (!IsLibraryEnabled(item, config.EnabledLibraryIds))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                "MP3 download is not enabled for this library.");
        }

        // ── 3. Determine FFmpeg path ───────────────────────────────────────
        var ffmpegPath = _mediaEncoder.EncoderPath;
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {
            _logger.LogError("FFmpeg path could not be resolved.");
            return StatusCode(StatusCodes.Status500InternalServerError, "FFmpeg is not available.");
        }

        // ── 4. Build safe file name ────────────────────────────────────────
        var safeName = string.Concat(
            (item.Name ?? "audio")
                .Split(System.IO.Path.GetInvalidFileNameChars()));

        // ── 5. Stream FFmpeg output ────────────────────────────────────────
        _logger.LogInformation(
            "Starting MP3 extraction for item {ItemId} ({ItemName})", itemId, item.Name);

        var outputStream = CreateMp3Stream(ffmpegPath, item.Path);
        return File(outputStream, "audio/mpeg", $"{safeName}.mp3");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static bool IsLibraryEnabled(BaseItem item, string[] enabledIds)
    {
        if (enabledIds is null || enabledIds.Length == 0)
        {
            return false;
        }

        // Walk up the item hierarchy until we reach a top-level folder.
        BaseItem? current = item;
        while (current is not null)
        {
            var idN = current.Id.ToString("N");
            if (enabledIds.Contains(idN, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    /// <summary>
    /// Launches FFmpeg and returns a readable stream connected to its stdout.
    /// The process is killed when the stream is closed / disposed.
    /// </summary>
    private System.IO.Stream CreateMp3Stream(string ffmpegPath, string inputPath)
    {
        var args = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "-hide_banner -loglevel error -i {0} -vn -acodec libmp3lame -b:a 320k -f mp3 pipe:1",
            QuoteArgument(inputPath));

        var psi = new ProcessStartInfo(ffmpegPath, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.Start();

        // Return a wrapper that kills the process once the HTTP response is done.
        return new ProcessOutputStream(process);
    }

    private static string QuoteArgument(string arg)
        => $"\"{arg.Replace("\"", "\\\"")}\"";
}
