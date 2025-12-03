using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AAXClean;
using AudibleDownloader.Enums;
using AudibleDownloader.Models;

namespace AudibleDownloader;

internal class AudibleMerger
{
    #region Fields

    private static readonly Regex _ffmpegTimeMatch = new("time=(\\d{2}):(\\d{2}):(\\d{2}\\.\\d{2})", RegexOptions.Compiled);
    private static readonly Regex _partFileMatch = new(@"Part (\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly FFmpegManager _ffmpegManager;
    private ConcurrentDictionary<string, MergeProgressResult> _mergeProgress = new();
    private ConcurrentDictionary<string, ConcurrentDictionary<int, double>> _partProgress = new();

    #endregion

    #region Constructor

    public AudibleMerger(FFmpegManager ffmpegManager)
    {
        _ffmpegManager = ffmpegManager;
    }

    #endregion

    #region Public Methods

    public async Task<string> AutoMergeAsync(string asin, string directory)
    {
        UpdateProgress(
            asin,
            0.0001,
            "Starting merge...",
            MergeStatus.NotStarted
        );

        string ffmpegPath = await _ffmpegManager.EnsureFFmpegAsync(progress =>
        {
            UpdateProgress(
                asin,
                progress,
                "Downloading FFmpeg...",
                MergeStatus.DownloadingFFmpeg
            );
        });

        List<string> parts = GetPartsInOrder(directory);
        string bookName = new DirectoryInfo(directory).Name;
        string mergedFilePath = Path.Combine(directory, $"{bookName}.m4a");

        string tempDir = Path.Combine(directory, "_temp_merge");
        Directory.CreateDirectory(tempDir);

        //List<string> trimmedFiles = new List<string>();

        //for (int i = 0; i < parts.Count; i++)
        //{
        //    string partPath = parts[i];
        //    string trimmedPath = Path.Combine(tempDir, $"trimmed_{i}.m4a");

        //    await TrimPartAsync(ffmpegPath, directory, partPath, trimmedPath, i, parts.Count);
        //    trimmedFiles.Add(trimmedPath);
        //}

        List<Task<string>> trimTasks = new List<Task<string>>();

        for (int i = 0; i < parts.Count; i++)
        {
            string partPath = parts[i];
            string trimmedPath = Path.Combine(tempDir, $"trimmed_{i}.m4a");

            int capturedIndex = i;

            trimTasks.Add(Task.Run(async () =>
            {
                await TrimPartAsync(ffmpegPath, asin, partPath, trimmedPath, capturedIndex, parts.Count);
                return trimmedPath;
            }));
        }

        string[] trimmedFiles = await Task.WhenAll(trimTasks);

        await MergePartsFinalAsync(ffmpegPath, asin, trimmedFiles, mergedFilePath);

        Directory.Delete(tempDir, recursive: true);

        return mergedFilePath;
    }

    public MergeProgressResult GetMergeProgress(string directory)
    {
        _mergeProgress.TryGetValue(directory, out MergeProgressResult value);
        return value;
    }

    public void ClearMergeProgress(string directory)
    {
        _mergeProgress.TryRemove(directory, out _);
        _partProgress.TryRemove(directory, out _);
    }

    #endregion

    #region Private Methods

    private async Task TrimPartAsync(
        string ffmpegPath,
        string asin,
        string inputPath,
        string outputPath,
        int partIndex,
        int totalParts)
    {
        using FileStream inputStream = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Mp4File mp4File = new Mp4File(inputStream);
        ChapterInfo chapters = await mp4File.GetChapterInfoAsync();

        List<string> args = new List<string> { "-i", inputPath };

        bool isFirst = partIndex == 0;
        bool isLast = partIndex == totalParts - 1;
        TimeSpan totalLength = chapters.Chapters[^1].EndOffset;

        if (isFirst && !isLast)
        {
            TimeSpan endTime = chapters.Chapters[^2].EndOffset;

            totalLength = endTime;

            args.Add("-t");
            args.Add(endTime.TotalSeconds.ToString("F3"));
        }
        else if (isLast && !isFirst)
        {
            TimeSpan startTime = chapters.Chapters[1].StartOffset;

            totalLength = chapters.Chapters[^1].EndOffset;

            args.Add("-ss");
            args.Add(startTime.TotalSeconds.ToString("F3"));
        }
        else if (!isFirst && !isLast)
        {
            TimeSpan startTime = chapters.Chapters[1].StartOffset;
            TimeSpan endTime = chapters.Chapters[^2].EndOffset;
            TimeSpan duration = endTime - startTime;

            totalLength = duration;

            args.Add("-ss");
            args.Add(startTime.TotalSeconds.ToString("F3"));
            args.Add("-t");
            args.Add(duration.TotalSeconds.ToString("F3"));
        }

        args.Add("-map"); args.Add("0:a");          // Audio stream only
        args.Add("-map"); args.Add("0:v?");         // Optional cover (video stream)
        args.Add("-map"); args.Add("-0:d?");
        args.Add("-map_chapters"); args.Add("0");   // Copy all chapters (global metadata)
        args.Add("-map_metadata"); args.Add("0");   // Copy all global metadata (artist, album, etc.)

        args.Add("-c");
        args.Add("copy");
        args.Add(outputPath);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = string.Join(" ", args.Select(a => $"\"{a}\"")),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };

        using Process process = Process.Start(startInfo);

        List<string> percentLines = new();

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                MatchCollection matches = _ffmpegTimeMatch.Matches(e.Data);

                if (matches.Count > 0)
                {
                    Match match = matches[0];
                    int hours = int.Parse(match.Groups[1].Value);
                    int minutes = int.Parse(match.Groups[2].Value);
                    double seconds = double.Parse(match.Groups[3].Value);
                    TimeSpan currentTime = TimeSpan.FromHours(hours) +
                                           TimeSpan.FromMinutes(minutes) +
                                           TimeSpan.FromSeconds(seconds);

                    double partProgress = Math.Min(1.0, currentTime.TotalMilliseconds / totalLength.TotalMilliseconds);

                    UpdatePartProgress(asin, partIndex, totalParts, partProgress);
                }
            }
        };

        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            UpdateProgress(asin, null, $"Failed to trim part {partIndex + 1}", MergeStatus.Failed);
            throw new Exception($"FFmpeg failed with exit code {process.ExitCode}");
        }

        UpdatePartProgress(asin, partIndex, totalParts, 1.0);
    }

    private async Task MergePartsFinalAsync(string ffmpegPath, string asin, string[] trimmedFiles, string outputPath)
    {
        string title = Path.GetFileNameWithoutExtension(outputPath);

        // 1. Build concat list
        string concatFilePath = Path.Combine(Path.GetTempPath(), "audible_concat.txt");
        await File.WriteAllLinesAsync(concatFilePath,
            trimmedFiles.Select(f => $"file '{f.Replace("'", "\\'")}'"));

        // 2. Build perfect ffmetadata file
        string metaPath = Path.Combine(Path.GetTempPath(), "audible_metadata.txt");
        await using StreamWriter sw = new(metaPath);

        await sw.WriteLineAsync(";FFMETADATA1");
        await sw.WriteLineAsync($"title={title}");

        // Get all metadata from first file
        await using FileStream firstStream = File.OpenRead(trimmedFiles[0]);
        using Mp4File firstMp4 = new(firstStream);

        if (!string.IsNullOrEmpty(firstMp4.AppleTags.Artist)) await sw.WriteLineAsync($"artist={firstMp4.AppleTags.Artist}");
        if (!string.IsNullOrEmpty(firstMp4.AppleTags.Album)) await sw.WriteLineAsync($"album={firstMp4.AppleTags.Album}");
        if (!string.IsNullOrEmpty(firstMp4.AppleTags.Generes)) await sw.WriteLineAsync($"genre={firstMp4.AppleTags.Generes}");
        if (!string.IsNullOrEmpty(firstMp4.AppleTags.Narrator)) await sw.WriteLineAsync($"narrator={firstMp4.AppleTags.Narrator}");
        if (!string.IsNullOrEmpty(firstMp4.AppleTags.Publisher)) await sw.WriteLineAsync($"publisher={firstMp4.AppleTags.Publisher}");
        if (!string.IsNullOrEmpty(firstMp4.AppleTags.Copyright)) await sw.WriteLineAsync($"copyright={firstMp4.AppleTags.Copyright}");
        if (!string.IsNullOrEmpty(firstMp4.AppleTags.Asin)) await sw.WriteLineAsync($"asin={firstMp4.AppleTags.Asin}");

        // 3. Build chapters with correct cumulative timing
        long offsetMs = 0;

        int chapterCount = 1;
        foreach (string file in trimmedFiles)
        {
            List<(long startMs, long endMs)> chapters = await GetChaptersFromFFmpeg(ffmpegPath, file);

            if (chapters?.Count > 0 == true)
            {
                foreach ((long startMs, long endMs) ch in chapters)
                {
                    long startMs = offsetMs + ch.startMs;
                    long endMs = offsetMs + ch.endMs;

                    await sw.WriteLineAsync("");
                    await sw.WriteLineAsync("[CHAPTER]");
                    await sw.WriteLineAsync("TIMEBASE=1/1000");
                    await sw.WriteLineAsync($"START={startMs}");
                    await sw.WriteLineAsync($"END={endMs}");
                    await sw.WriteLineAsync($"title=Chapter {chapterCount++}");
                }

                offsetMs += chapters[^1].endMs;
            }
        }

        TimeSpan totalLength = TimeSpan.FromMilliseconds(offsetMs);

        sw.Close();

        // 4. Merge, including metadata and chapters
        string args = $"-f concat -safe 0 -i \"{concatFilePath}\" " +
                      $"-i \"{metaPath}\" " +
                      $"-map 0:a -map 0:v? " +
                      $"-map_metadata 1 " +
                      $"-map_chapters -1 " +
                      $"-map_chapters 1 " +
                      $"-c copy " +
                      $"-f mp4 " +
                      $"-movflags +faststart " +
                      $"\"{outputPath}\"";

        ProcessStartInfo psi = new(ffmpegPath, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };

        using Process process = Process.Start(psi)!;

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                MatchCollection matches = _ffmpegTimeMatch.Matches(e.Data);

                if (matches.Count > 0)
                {
                    Match match = matches[0];
                    int hours = int.Parse(match.Groups[1].Value);
                    int minutes = int.Parse(match.Groups[2].Value);
                    double seconds = double.Parse(match.Groups[3].Value);
                    TimeSpan currentTime = TimeSpan.FromHours(hours) +
                                           TimeSpan.FromMinutes(minutes) +
                                           TimeSpan.FromSeconds(seconds);

                    double mergeProgress = Math.Min(1.0, currentTime.TotalMilliseconds / totalLength.TotalMilliseconds);

                    UpdateMergeProgress(asin, trimmedFiles.Length, mergeProgress);
                }
            }
        };

        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            UpdateProgress(asin, null, "Failed to merge", MergeStatus.Failed);
            throw new Exception($"FFmpeg failed with exit code {process.ExitCode}");
        }

        UpdateMergeProgress(asin, trimmedFiles.Length, 1.0, MergeStatus.Completed);

        // Cleanup
        File.Delete(concatFilePath);
        File.Delete(metaPath);
    }

    private static List<string> GetPartsInOrder(string directory)
    {
        List<string> partFiles = Directory.GetFiles(directory, "*.m4a")
            .Where(f => _partFileMatch.IsMatch(Path.GetFileName(f)))
            .ToList();

        partFiles.Sort((a, b) =>
        {
            Match matchA = _partFileMatch.Match(Path.GetFileName(a));
            Match matchB = _partFileMatch.Match(Path.GetFileName(b));

            int numA = int.Parse(matchA.Groups[1].Value);
            int numB = int.Parse(matchB.Groups[1].Value);

            return numA.CompareTo(numB);
        });

        return partFiles;
    }

    private static async Task<List<(long startMs, long endMs)>> GetChaptersFromFFmpeg(string ffmpegPath, string filePath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-i \"{filePath}\" -f ffmetadata -",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        Process process = Process.Start(startInfo);
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        List<(long, long)> chapters = new List<(long, long)>();
        string[] lines = output.Split('\n');

        long? start = null;
        long? end = null;
        double timebaseDivisor = 1000.0; // Default to milliseconds

        foreach (string line in lines)
        {
            if (line.StartsWith("TIMEBASE="))
            {
                string timebaseStr = line.Substring(9).Trim();
                string[] parts = timebaseStr.Split('/');
                if (parts.Length == 2)
                {
                    double numerator = double.Parse(parts[0]);
                    double denominator = double.Parse(parts[1]);
                    timebaseDivisor = denominator / numerator;
                }
            }
            else if (line.StartsWith("START="))
            {
                long rawStart = long.Parse(line.Substring(6).Trim());
                start = (long)(rawStart / timebaseDivisor * 1000);
            }
            else if (line.StartsWith("END="))
            {
                long rawEnd = long.Parse(line.Substring(4).Trim());
                end = (long)(rawEnd / timebaseDivisor * 1000);
            }

            if (start.HasValue && end.HasValue)
            {
                chapters.Add((start.Value, end.Value));
                start = null;
                end = null;
                timebaseDivisor = 1000.0;
            }
        }

        return chapters;
    }

    private void UpdatePartProgress(string directory, int partIndex, int totalParts, double partProgress)
    {
        ConcurrentDictionary<int, double> partsDict = _partProgress.GetOrAdd(directory, _ => new ConcurrentDictionary<int, double>());

        partsDict[partIndex] = partProgress;

        // Calculate how many parts are complete (progress = 1.0) and aggregate in-progress
        int completedParts = partsDict.Values.Count(p => p >= 1.0);
        double inProgressSum = partsDict.Values.Where(p => p < 1.0).Sum();

        // Total steps = all parts + final merge
        int totalSteps = totalParts + 1;

        // Overall progress = (completed parts + sum of in-progress parts) / total steps
        double overallProgress = (completedParts + inProgressSum) / totalSteps;

        UpdateProgress(
            directory,
            overallProgress,
            $"Trimming parts...",
            MergeStatus.Trimming
        );
    }

    private void UpdateProgress(string asin, double? progress = null, string message = null, MergeStatus? status = null)
    {
        _mergeProgress.AddOrUpdate(
            asin,
            new MergeProgressResult
            {
                Progress = progress ?? 0.0,
                Message = message ?? string.Empty,
                Status = (int)(status ?? MergeStatus.NotStarted)
            },
            (key, existing) =>
            {
                if (progress.HasValue)
                    existing.Progress = progress.Value;

                if (message != null)
                    existing.Message = message;

                if (status.HasValue)
                    existing.Status = (int)status.Value;

                return existing;
            }
        );
    }

    private void UpdateMergeProgress(string asin, int totalParts, double mergeProgress, MergeStatus status = MergeStatus.Merging)
    {
        int totalSteps = totalParts + 1;
        double overallProgress = (totalParts + mergeProgress) / totalSteps;

        UpdateProgress(
            asin,
            overallProgress,
            "Merging final file...",
            status
        );
    }

    #endregion
}
