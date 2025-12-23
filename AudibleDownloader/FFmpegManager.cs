using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AudibleDownloader;

internal class FFmpegManager
{
    #region Fields

    private static readonly string _ffmpegDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudibleDownloader",
        "ffmpeg"
    );

    private static readonly Dictionary<string, string> _platformUrls = new()
    {
        ["win-x64"] = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-win-64.zip",
        ["linux-x64"] = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-linux-64.zip",
        ["osx-x64"] = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-macos-64.zip",
        ["osx-arm64"] = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-macos-64.zip"
    };

    private readonly SemaphoreSlim _downloadLock = new(1, 1);

    #endregion

    #region Public Methods

    public async Task<string> EnsureFFmpegAsync(Action<double> progressCallback = null)
    {
        string ffmpegPath = GetFFmpegPath();

        if (File.Exists(ffmpegPath))
            return ffmpegPath;

        await _downloadLock.WaitAsync();

        try
        {
            // double-check after acquiring lock
            if (File.Exists(ffmpegPath))
                return ffmpegPath;

            await DownloadFFmpegAsync(progressCallback);

            return ffmpegPath;
        }
        finally
        {
            _downloadLock.Release();
        }
    }

    #endregion

    #region Private Methods

    private static string GetFFmpegPath()
    {
        string extension = OperatingSystem.IsWindows() ? ".exe" : "";
        return Path.Combine(_ffmpegDirectory, $"ffmpeg{extension}");
    }

    private async Task DownloadFFmpegAsync(Action<double> progressCallback = null)
    {
        string rid = RuntimeInformation.RuntimeIdentifier;

        if (!_platformUrls.TryGetValue(rid, out string url))
        {
            if (OperatingSystem.IsLinux())
            {
                rid = "linux-x64";
            }
            else if (OperatingSystem.IsMacOS())
            {
                rid = "osx-x64";
            }

            if (!_platformUrls.TryGetValue(rid, out url))
            {
                throw new PlatformNotSupportedException($"Platform {rid} is not supported");
            }
        }

        Directory.CreateDirectory(_ffmpegDirectory);

        using HttpClient httpClient = new();

        progressCallback?.Invoke(0.0);

        using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? contentLength = response.Content.Headers.ContentLength;

        using Stream contentStream = await response.Content.ReadAsStreamAsync();
        using MemoryStream memoryStream = new MemoryStream();

        byte[] buffer = new byte[8192];
        long totalBytesRead = 0;

        while (true)
        {
            int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
                break;

            await memoryStream.WriteAsync(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;

            if (contentLength.HasValue && contentLength.Value > 0)
            {
                double progress = (double)totalBytesRead / contentLength.Value;
                progressCallback?.Invoke(progress);
            }
        }

        byte[] zipData = memoryStream.ToArray();
        string zipPath = Path.Combine(_ffmpegDirectory, "ffmpeg.zip");
        await File.WriteAllBytesAsync(zipPath, zipData);

        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, _ffmpegDirectory, overwriteFiles: true);

        File.Delete(zipPath);

        if (!OperatingSystem.IsWindows())
        {
            var ffmpegPath = GetFFmpegPath();
            File.SetUnixFileMode(ffmpegPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        progressCallback?.Invoke(1.0);
    }

    #endregion
}
