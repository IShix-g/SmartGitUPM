
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace SmartGitUPM.Editor
{
    public sealed class HttpPackageInfoFetcher : IPackageInfoFetcher
    {
        public const string PackageJsonFileName = "package.json";
        public const string SgUpmPackageCachePath = "Library/PackageCache-SmartGitUPM";
        public bool IsProcessing{ get; private set; }
        public string SupportProtocol { get; } = "https";

        bool _isDisposed;
        CancellationTokenSource _tokenSource;
        readonly PackageInstaller _installer;

        public HttpPackageInfoFetcher(PackageInstaller installer) => _installer = installer;

        public bool IsSupported(string url) => url.StartsWith("http");

        public Task<PackageInfoDetails> FetchPackageInfo(string packageInstallUrl, string branch, bool supperReload, CancellationToken token = default)
        {
            if (!IsSupported(packageInstallUrl))
            {
                throw new ArgumentException("Specify the URL of " + SupportProtocol + ". packageInstallUrl: " + packageInstallUrl, nameof(packageInstallUrl));
            }
            var gitPackageJsonUrl = ToRawPackageJsonURL(packageInstallUrl, branch);
            return FetchPackageInfoByPackageJsonUrl(packageInstallUrl, gitPackageJsonUrl, supperReload, token);
        }

        public async Task<PackageInfoDetails> FetchPackageInfoByPackageJsonUrl(string packageInstallUrl, string gitPackageJsonUrl, bool supperReload, CancellationToken token = default)
        {
            var info = default(PackageInfo);
            try
            {
                IsProcessing = true;
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                var installUrlWithoutVersion = WithoutVersion(packageInstallUrl);
                info = await _installer.GetInfoByPackageId(installUrlWithoutVersion, _tokenSource.Token);

                var local = info != default
                    ? new PackageLocalInfo
                    {
                        name = info.name,
                        version = info.version,
                        displayName = info.displayName
                    }
                    : default;

                var server = default(PackageRemoteInfo);
                var fileNameFromUrl = GenerateFileNameFromUrl(packageInstallUrl);
                if (!supperReload)
                {
                    server = await GetPackageInfoFromCache(fileNameFromUrl, token);
                }

                if(server == default)
                {
                    server = await FetchPackageInfo(gitPackageJsonUrl);
                    if (server != default)
                    {
                        await SavePackageInfoToCache(fileNameFromUrl, server, token);
                    }
                }

                return new PackageInfoDetails(local, server, packageInstallUrl);
            }
            catch (Exception ex)
            {
                var message = ex.Message + "\n";
                if (info != default)
                {
                    message += "Package: " + info.displayName + " (" + info.name + ")\npackage.json url: " + gitPackageJsonUrl + "\nInstall url: " + packageInstallUrl;
                }
                else
                {
                    message += "Install url: " + packageInstallUrl + "\npackage.json url: " + gitPackageJsonUrl;
                }

                throw new PackageInstallException(message, ex);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public static string WithoutVersion(string packageInstallUrl)
            => Regex.Replace(packageInstallUrl, @"[#@]v?([\d.]+)$", string.Empty);

        public async Task<PackageRemoteInfo> FetchPackageInfo(string packageJsonUrl)
        {
            if (!packageJsonUrl.EndsWith(PackageJsonFileName))
            {
                throw new ArgumentException("Please specify the URL of " + PackageJsonFileName + ". : " + packageJsonUrl, nameof(packageJsonUrl));
            }
            IsProcessing = true;
            try
            {
                using var op = UnityWebRequest.Get(packageJsonUrl);
                await op.SendWebRequest();
                if (op.isDone)
                {
                    return JsonUtility.FromJson<PackageRemoteInfo>(op.downloadHandler.text);
                }

                throw new InvalidOperationException(op.error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public static string ToRawPackageJsonURL(string packageInstallUrl, string branch)
        {
            var rootUrl = ToRawPackageRootURL(packageInstallUrl, branch);
            return rootUrl + "/" + PackageJsonFileName;
        }

        public static string ToRawPackageRootURL(string packageInstallUrl, string branch)
        {
            if (!packageInstallUrl.StartsWith("https://github.com")
                && !packageInstallUrl.StartsWith("https://bitbucket.org")
                && !packageInstallUrl.StartsWith("https://gitlab.com"))
            {
                throw new ArgumentException("Specify the URL of GitHub, Bitbucket, or GitLab. : " + packageInstallUrl, nameof(packageInstallUrl));
            }

            var uri = new Uri(packageInstallUrl);
            var pathWithoutFileName = uri.AbsolutePath;
            if (pathWithoutFileName.EndsWith(".git"))
            {
                pathWithoutFileName = pathWithoutFileName.Replace(".git", string.Empty);
            }
            var query = uri.Query;
            var path = ExtractPathFromQuery(query);
            var resultUrl = uri.GetLeftPart(UriPartial.Authority) + pathWithoutFileName;
            if (string.IsNullOrEmpty(branch))
            {
                branch = "HEAD";
            }
            return $"{resultUrl}/raw/{branch}/{path}";
        }

        public static async Task<PackageRemoteInfo> GetPackageInfoFromCache(string packageName, CancellationToken token = default)
        {
            var filePath = Application.dataPath + "/../" + SgUpmPackageCachePath + "/" + packageName + ".json";
            if (!File.Exists(filePath))
            {
                return default;
            }
            var jsonString = await File.ReadAllTextAsync(filePath, token);
            return JsonUtility.FromJson<PackageRemoteInfo>(jsonString);
        }

        public static async Task<bool> SavePackageInfoToCache(string packageName, PackageRemoteInfo info, CancellationToken token = default)
        {
            var filePath = Application.dataPath + "/../" + SgUpmPackageCachePath + "/" + packageName + ".json";
            var jsonString = JsonUtility.ToJson(info);
            if (!string.IsNullOrEmpty(jsonString)
                && jsonString != "[]")
            {
                CreateDirectories(filePath);
                await File.WriteAllTextAsync(filePath, jsonString, token);
                return true;
            }
            return false;
        }

        public static string GenerateFileNameFromUrl(string packageInstallUrl)
        {
            try
            {
                var packageInstallUrlWithoutVersion = Regex.Replace(packageInstallUrl, @"[#@]v?([\d.]+)$", string.Empty);
                var uri = new Uri(packageInstallUrlWithoutVersion);
                var segments = uri.AbsolutePath.Split('/');
                var userName = segments.Length > 1 ? segments[1] : string.Empty;
                var repoName = segments.Length > 2 ? segments[^1] : string.Empty;

                if (repoName.EndsWith(".git"))
                {
                    repoName = repoName.Substring(0, repoName.Length - 4);
                }
                return $"{userName}@{repoName}";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error processing URL: {ex.Message}");
                return string.Empty;
            }
        }

        static string ExtractPathFromQuery(string query)
        {
            var parameters = query.TrimStart('?').Split('&');
            foreach (var param in parameters)
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2
                    && keyValue[0] == "path")
                {
                    return Uri.UnescapeDataString(keyValue[1]);
                }
            }
            return string.Empty;
        }

        public static void CreateDirectories(string path)
        {
            path = Path.HasExtension(path)
                ? Path.GetDirectoryName(path)
                : path;

            if (!string.IsNullOrEmpty(path)
                && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            if (_tokenSource != default)
            {
                _tokenSource.Dispose();
                _tokenSource = default;
            }
        }
    }
}
