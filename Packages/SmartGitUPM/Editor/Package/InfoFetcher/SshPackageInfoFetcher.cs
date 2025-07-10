// #define DEBUG_PROSESS

using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace SmartGitUPM.Editor
{
    public sealed class SshPackageInfoFetcher : IPackageInfoFetcher
    {
        public bool IsProcessing{ get; private set; }
        public string SupportProtocol { get; } = "SSH";

        bool _isDisposed;
        CancellationTokenSource _tokenSource;
        readonly PackageInstaller _installer;

        public SshPackageInfoFetcher(PackageInstaller installer) => _installer = installer;

        public bool IsSupported(string url) => url.StartsWith("git");

        public async Task<PackageInfoDetails> FetchPackageInfo(string packageInstallUrl, string branch, bool supperReload, CancellationToken token = default)
        {
            if (!IsSupported(packageInstallUrl))
            {
                throw new ArgumentException("Specify the URL of " + SupportProtocol + ". packageInstallUrl: " + packageInstallUrl, nameof(packageInstallUrl));
            }

            PackageCacheManager.Initialize();

            var currentVersion = packageInstallUrl.ToVersion();
            var installUrlWithoutVersion = string.IsNullOrEmpty(currentVersion)
                    ? packageInstallUrl
                    : packageInstallUrl.WithoutVersion();
            var (absolutePath, query) = ParsePath(installUrlWithoutVersion);
            var rootPath = Application.dataPath + "/../" + HttpPackageInfoFetcher.SgUpmPackageCachePath;
            var tempPath = rootPath + "/.tmp_" + CreateUniqID();
            var command = "git";
            var arguments = default(string);
            var packagePath = ExtractPathFromQuery(query);
            var packageJsonPath = !string.IsNullOrEmpty(packagePath)
                ? ExtractPathFromQuery(query) + "/" + HttpPackageInfoFetcher.PackageJsonFileName
                : HttpPackageInfoFetcher.PackageJsonFileName;
            var localInfo = default(PackageLocalInfo);
            var isCachePackage = false;
            var localPackagePath = default(string);
            var absoluteLocalPackageJsonPath = default(string);

            try
            {
                IsProcessing = true;
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                var info = await _installer.GetInfoByPackageId(installUrlWithoutVersion, _tokenSource.Token);
                localInfo = info != default
                    ? ToLocalInfo(info)
                    : default;

                if (localInfo == default
                    && PackageCacheManager.TryGetByInstallUrl(installUrlWithoutVersion, out var cache))
                {
                    localInfo = ToLocalInfo(cache);
                    isCachePackage = true;
                    Debug.Log("localInfo.version = " + localInfo.version);
                }
            }
            finally
            {
                IsProcessing = false;
            }

            if (localInfo != default)
            {
                localPackagePath = rootPath + "/" + localInfo.name;
                absoluteLocalPackageJsonPath = localPackagePath + "/" + packageJsonPath;
                if (!File.Exists(absoluteLocalPackageJsonPath))
                {
                    absoluteLocalPackageJsonPath = default;
                }
            }

            if (!supperReload
                && !string.IsNullOrEmpty(absoluteLocalPackageJsonPath))
            {
                var jsonString = await File.ReadAllTextAsync(absoluteLocalPackageJsonPath, token);
                var serverInfo = JsonUtility.FromJson<PackageRemoteInfo>(jsonString);

                if (serverInfo != default)
                {
                    return new PackageInfoDetails(!isCachePackage ? localInfo : default, serverInfo, installUrlWithoutVersion);
                }
            }

            if (!string.IsNullOrEmpty(branch))
            {
                branch = "-b " + branch;
            }

            arguments = string.IsNullOrEmpty(absoluteLocalPackageJsonPath)
                ? $"clone --single-branch --no-tags {branch} {absolutePath} {tempPath}"
                : $"pull origin {branch}";

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    arguments = $"/C {command} {arguments}";
                    command = "cmd.exe";
                    break;
                default:
                    arguments = $"-c \"{command} {arguments}\"";
                    command = "/bin/bash";
                    break;
            }

            var workingDirectory
                    = localInfo != default
                      && !string.IsNullOrEmpty(absoluteLocalPackageJsonPath)
                        ? localPackagePath
                        : rootPath;

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            try
            {
                HttpPackageInfoFetcher.CreateDirectories(rootPath);

                using var process = Process.Start(startInfo);
                if (process != null)
                {
#if DEBUG_PROSESS
                    var error = await process.StandardError.ReadToEndAsync();
#endif
                    process.WaitForExit();
                    await WaitForProcessExitAsync(process);
#if DEBUG_PROSESS
                    if (!string.IsNullOrEmpty(error))
                    {
                        UnityEngine.Debug.LogWarning(error);
                    }
#endif

                    var serverInfo = default(PackageRemoteInfo);
                    if (!string.IsNullOrEmpty(absoluteLocalPackageJsonPath))
                    {
                        var jsonString = await File.ReadAllTextAsync(absoluteLocalPackageJsonPath, token);
                        serverInfo = JsonUtility.FromJson<PackageRemoteInfo>(jsonString);

                        if (serverInfo == default)
                        {
                            throw new InvalidOperationException("Failed to parse the package.json.");
                        }
                    }
                    else
                    {
                        var absolutePackageJsonPath = tempPath + "/" + packageJsonPath;
                        if (!File.Exists(absolutePackageJsonPath))
                        {
                            throw new InvalidOperationException("Failed to find the package.json.");
                        }

                        var jsonString = await File.ReadAllTextAsync(absolutePackageJsonPath, token);
                        serverInfo = JsonUtility.FromJson<PackageRemoteInfo>(jsonString);

                        if (serverInfo == default)
                        {
                            throw new InvalidOperationException("Failed to parse the package.json.");
                        }

                        localPackagePath = rootPath + "/" + serverInfo.name;
                        if (Directory.Exists(localPackagePath))
                        {
                            Directory.Delete(localPackagePath, true);
                        }
                        Directory.Move(tempPath, localPackagePath);

                        var cache = new PackageCacheInfo(
                            installUrlWithoutVersion,
                            serverInfo.name,
                            serverInfo.displayName,
                            serverInfo.version);
                        PackageCacheManager.Add(cache);
                        PackageCacheManager.Save();
                    }

                    return new PackageInfoDetails(!isCachePackage ? localInfo : default, serverInfo, installUrlWithoutVersion);
                }
                else
                {
                    throw new InvalidOperationException("Failed to start the process.");
                }
            }
            catch (Exception ex)
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }

                var message = ex.Message + "\n";
                if (localInfo != default)
                {
                    message += "Package: " + localInfo.displayName + " (" + localInfo.name + ") \n";
                }
                message += "Install url: " + packageInstallUrl;

                throw new Exception(message, ex);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        static PackageLocalInfo ToLocalInfo(PackageInfo info)
            => new PackageLocalInfo
            {
                name = info.name,
                version = info.version,
                displayName = info.displayName
            };

        static PackageLocalInfo ToLocalInfo(PackageCacheInfo info)
            => new PackageLocalInfo
            {
                name = info.Name,
                version = info.Version,
                displayName = info.DisplayName
            };

        static Task WaitForProcessExitAsync(Process process)
        {
            var tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) =>
            {
                tcs.TrySetResult(true);
                process.Dispose();
            };
            return tcs.Task;
        }

        static string CreateUniqID()
        {
            var guid = Guid.NewGuid();
            var bytes = guid.ToByteArray();
            return Convert.ToBase64String(bytes)
                .Replace("=", "")
                .Replace("+", "")
                .Replace("/", "");
        }

        static (string AbsolutePath, string Query) ParsePath(string path)
        {
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var url = new Uri(path);
                return (url.AbsolutePath, url.Query);
            }

            var index = path.IndexOf('?');
            if (index == -1)
            {
                return (path, string.Empty);
            }

            var absolutePath = path.Substring(0, index);
            var query = path.Substring(index);

            return (absolutePath, query);
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
