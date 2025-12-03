using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AudibleDownloader.Models;
using AudibleDownloader.Models.Audible;
using Galdr.Native;
using SharpWebview.Content;

namespace AudibleDownloader;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Config config = LoadConfig();

        if (File.Exists(config.AuthFilePath))
        {
            config.IsLoggedIn = true;
        }

#if !DEBUG
        EmbeddedContent embeddedContent = new(embeddedNamespace: "AudibleDownloader", contentDir: "FrontEnd.dist");
        Uri embeddedUri = new(embeddedContent.ToWebviewUrl());
#endif

        GaldrBuilder builder = new GaldrBuilder()
            .SetTitle("Audible Downloader")
            .SetSize(1024, 768)
            .SetMinSize(640, 480)
#if DEBUG
            .SetDebug(true)
            .SetPort(5173)
            .SetInitScript(GetUrlMonitorScript(5173))
            .SetContentProvider(new UrlContent("http://localhost:5173/"))
#else
            .SetPort(embeddedUri.Port)
            .SetInitScript(GetUrlMonitorScript(embeddedUri.Port))
            .SetContentProvider(embeddedContent)
#endif
            .AddSingleton(config)
            .AddSingleton<AudibleClient>()
            .AddSingleton<AudibleRegister>()
            .AddSingleton<FFmpegManager>()
            .AddSingleton<AudibleMerger>();

        builder.AddFunction("getLoginStatus", (Config Config) =>
        {
            return new LoginStatusResult()
            {
                IsLoggedIn = config.IsLoggedIn
            };
        });

        builder.AddFunction("getOAuthUrl", async (AudibleRegister register) =>
        {
            return new GetOAuthUrlResult()
            {
                Url = register.GenerateAudibleSignInUrl()
            };
        });

        builder.AddAction("onAuthorizationCodeFound", async (string url, AudibleRegister register, Config config) =>
        {
            if (url.Contains("openid.oa2.authorization_code="))
            {
                register.ExtractAuthorizationCode(url);
                await register.CreateAuthFileAsync();
                config.IsLoggedIn = File.Exists(config.AuthFilePath);
            }
        });

        builder.AddFunction("getLibrary", async (Config config, AudibleClient client) =>
        {
            List <LibraryItem> libraryItems = await client.GetAllLibraryItemsAsync();

            return new GetLibraryResult()
            {
                Items = libraryItems.Select(x => 
                {
                    string author = x.Authors.FirstOrDefault()?.Name;
                    string safeAuthor = client.RemoveInvalidNameChars(author);

                    string bookTitle = String.IsNullOrWhiteSpace(x.Subtitle) ? x.Title : $"{x.Title} {x.Subtitle}";
                    string safeBookTitle = client.RemoveInvalidNameChars(bookTitle);

                    string bookDirectory = Path.Combine(
                        config.LibraryPath,
                        safeAuthor,
                        safeBookTitle
                    );

                    bool isDownloaded = false;
                    bool isMerged = false;

                    if (Directory.Exists(bookDirectory))
                    {
                        string mergedTitle = new DirectoryInfo(bookDirectory).Name;
                        string[] m4aFiles = Directory.GetFiles(bookDirectory, "*.m4a");
                        isDownloaded = m4aFiles.Length > 0;
                        isMerged = m4aFiles.Any(x => Path.GetFileNameWithoutExtension(x) == mergedTitle);
                    }

                    return new LibraryResult()
                    {
                        Asin = x.Asin,
                        Title = x.Title,
                        Subtitle = x.Subtitle,
                        Authors = x.Authors.Select(y => y.Name).ToList(),
                        Narrators = x.Narrators.Select(y => y.Name).ToList(),
                        RuntimeMinutes = x.RuntimeLengthMin,
                        PurchaseDate = DateTime.Parse(x.PurchaseDate),
                        ReleaseDate = DateTime.Parse(x.ReleaseDate),
                        ImageUrl = x.ProductImages.FirstOrDefault().Value,
                        AvailableCodecs = x.AvailableCodecs.Select(y => y.EnhancedCodec).ToList(),
                        Description = x.MerchandisingSummary,
                        IsPlayable = x.IsPlayable,
                        IsDownloaded = isDownloaded,
                        IsMerged = isMerged,
                        Directory = isDownloaded ? bookDirectory : null,
                        Relationships = x.Relationships != null ? x.Relationships
                            .Where(y => !String.IsNullOrEmpty(y.Sort) && y.RelationshipToProduct == "child" && y.RelationshipType == "component")
                            .Select(y =>
                            {
                                return new LibraryRelationship()
                                {
                                    Asin = y.Asin,
                                    RelationshipToProduct = y.RelationshipToProduct,
                                    RelationshipType = y.RelationshipType,
                                    Sort = Int32.Parse(y.Sort),
                                };
                            })
                            .OrderBy(y => y.Sort)
                            .ToList() : null,
                        Series = x.Series != null ? x.Series.Select(y =>
                        {
                            int sequence = -1;
                            if (Int32.TryParse(y.Sequence, out int seqNum))
                            {
                                sequence = seqNum;
                            }

                            return new SeriesResult()
                            {
                                Asin = y.Asin,
                                Title = y.Title,
                                Sequence = sequence,
                            };
                        })
                        .OrderBy(y => y.Title)
                            .ThenBy(y => y.Sequence)
                        .ToList() : null,
                    };
                }).ToList()
            };
        });

        builder.AddFunction("downloadBook", async (LibraryResult libraryEntry, string codec, Config config, AudibleClient client) =>
        {
            List<byte[]> encryptedBookDataParts = await client.DownloadBookAsync(libraryEntry, codec);

            string safeAuthorName = client.RemoveInvalidNameChars(libraryEntry.Authors.FirstOrDefault() ?? "Unknown Author");

            string bookTitle = String.IsNullOrWhiteSpace(libraryEntry.Subtitle) ?
                libraryEntry.Title :
                $"{libraryEntry.Title} {libraryEntry.Subtitle}";
            string safeBookTitle = client.RemoveInvalidNameChars(bookTitle);

            string saveFileFolder = Path.Combine(
                config.LibraryPath,
                safeAuthorName,
                safeBookTitle
            );

            if (!Directory.Exists(saveFileFolder))
            {
                Directory.CreateDirectory(saveFileFolder);
            }

            for (int i = 0; i < encryptedBookDataParts.Count; i++)
            {
                DecryptedBookResult decryptedBook = await client.DecryptBook(encryptedBookDataParts[i]);

                File.WriteAllBytes(Path.Combine(saveFileFolder, decryptedBook.SafeFileName), decryptedBook.FileData);
            }

            return new DownloadBookResult()
            {
                Success = true,
                Directory = saveFileFolder,
                IsMerged = encryptedBookDataParts.Count == 1,
            };
        });

        builder.AddFunction("getDownloadProgress", (string asin, AudibleClient client) =>
        {
            return client.GetDownloadProgress(asin);
        });

        builder.AddAction("clearDownloadProgress", (string asin, AudibleClient client) =>
        {
            client.ClearDownloadProgress(asin);
        });

        builder.AddAction("openDirectory", (string directory) =>
        {
            if (Directory.Exists(directory))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = directory,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        });

        builder.AddFunction("getClientInfo", (AudibleClient client) =>
        {
            AuthFile auth = client.GetAuthInfo();

            return new GetClientInfoResult()
            {
                DeviceName = auth.DeviceInfo.DeviceName
            };
        });

        builder.AddFunction("mergeBook", async (LibraryResult libraryEntry, bool trimParts, bool deleteParts, AudibleMerger audibleMerger) =>
        {
            bool success = false;

            try
            {
                string path = await audibleMerger.MergeBookAsync(libraryEntry.Asin, libraryEntry.Directory, trimParts, deleteParts);
                success = !String.IsNullOrEmpty(path) && File.Exists(path);
            }
            catch { }

            return new MergeBookResult()
            {
                Success = success
            };
        });

        builder.AddFunction("getMergeProgress", (string asin, AudibleMerger audibleMerger) =>
        {
            return audibleMerger.GetMergeProgress(asin);
        });

        builder.AddAction("clearMergeProgress", (string asin, AudibleMerger audibleMerger) =>
        {
            audibleMerger.ClearMergeProgress(asin);
        });

        builder.AddFunction("getSettings", (Config config) =>
        {
            return config;
        });

        builder.AddFunction("updateLibraryPath", (string newPath, Config config) =>
        {
            config.LibraryPath = newPath;
            string json = JsonSerializer.Serialize(config, AudibleJsonContext.Default.Config);
            File.WriteAllText(config.SettingsPath, json);
            return config;
        });

        builder.AddFunction("browseDirectory", async (string defaultPath, Galdr.Native.Galdr galdr, IDialogService dialogService) =>
        {
            string directory = "waiting...";

            galdr.Dispatch(() =>
            {
                string cleanDirectory = String.IsNullOrEmpty(defaultPath) ? null : Path.GetFullPath(defaultPath);
                directory = dialogService.OpenDirectoryDialog(cleanDirectory);
            });

            while (directory == "waiting...")
            {
                await Task.Delay(250);
            }

            return new BrowseDirectoryResult()
            {
                SelectedDirectory = directory
            };
        });

        builder.AddFunction("logoutAndUnregister", async (Config config, AudibleClient client) =>
        {
            bool success = false;

            try
            {
                DeregisterResponse response = await client.DeregisterAsync();
                success = response?.Response?.Success != null;

                if (success && File.Exists(config.AuthFilePath))
                {
                    File.Delete(config.AuthFilePath);
                    config.IsLoggedIn = false;
                }
            }
            catch (Exception ex)
            {

            }

            return new LogoutResult()
            {
                Success = success
            };
        });

        using Galdr.Native.Galdr galdr = builder
            .Build()
            .Run();
    }

    static string GetUrlMonitorScript(int port)
    {
        return @"
                (function() {
                    let lastUrl = window.location.href;
                    
                    // Check URL immediately
                    checkUrl();
                    
                    // Set up interval to check for URL changes
                    setInterval(function() {
                        const currentUrl = window.location.href;
                        if (currentUrl !== lastUrl) {
                            lastUrl = currentUrl;
                            checkUrl();
                        }
                    }, 100);
                    
                    function checkUrl() {
                        const url = window.location.href;
                        
                        // Check if URL contains authorization code
                        if (url.includes('openid.oa2.authorization_code=')) {
                            // Call C# callback
                            galdrInvoke('onAuthorizationCodeFound', { url: url });
                            window.location.href = 'http://localhost:{{PORT}}/loading';
                        }
                    }
                })();
            ".Replace("{{PORT}}", port.ToString());
    }

    static Config LoadConfig()
    {
        Config config = new()
        {
            AuthFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AudibleDownloader",
                "auth.json"),
            LibraryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AudibleDownloader",
                "Library"),
            SettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AudibleDownloader",
                "config.json"),
        };

        if (File.Exists(config.SettingsPath))
        {
            string json = File.ReadAllText(config.SettingsPath);
            return JsonSerializer.Deserialize<Config>(json, AudibleJsonContext.Default.Config);
        }
        else
        {
            string json = JsonSerializer.Serialize(config, AudibleJsonContext.Default.Config);
            File.WriteAllText(config.SettingsPath, json);
        }

        return config;
    }
}

[JsonSerializable(typeof(AuthFile))]
[JsonSerializable(typeof(RegistrationRequest))]
[JsonSerializable(typeof(RegistrationResponse))]
[JsonSerializable(typeof(RefreshTokenResponse))]
[JsonSerializable(typeof(DeregisterRequest))]
[JsonSerializable(typeof(DeregisterResponse))]
[JsonSerializable(typeof(LibraryResponse))]
[JsonSerializable(typeof(LibraryItemResponse))]
[JsonSerializable(typeof(Config))]
internal partial class AudibleJsonContext : JsonSerializerContext
{
}
