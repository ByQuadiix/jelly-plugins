using System.Diagnostics;

namespace Jellyfin.Plugin.Mp3Extractor.Api;

/// <summary>
/// A read-only stream that wraps the standard-output of an FFmpeg process.
/// When the stream is disposed the underlying process is cleaned up.
/// </summary>
internal sealed class ProcessOutputStream : Stream
{
    private readonly Process _process;
    private readonly Stream _inner;
    private bool _disposed;

    internal ProcessOutputStream(Process process)
    {
        _process = process;
        _inner = process.StandardOutput.BaseStream;
    }

    // ── Stream contract ────────────────────────────────────────────────────

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
        => _inner.Read(buffer, offset, count);

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
        => _inner.ReadAsync(buffer, offset, count, ct);

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer, CancellationToken ct = default)
        => _inner.ReadAsync(buffer, ct);

    public override void Flush() => _inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    // ── Cleanup ────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            _inner.Dispose();

            if (!_process.HasExited)
            {
                try
                {
                    // Kill the FFmpeg process; entireProcessTree: false because FFmpeg
                    // typically does not spawn persistent child processes and we do not
                    // want to accidentally kill unrelated processes in complex scenarios.
                    _process.Kill(entireProcessTree: false);
                }
                catch (Exception)
                {
                    // Process may have already exited between the check and the Kill call.
                }
            }

            _process.Dispose();
        }

        base.Dispose(disposing);
    }
}
