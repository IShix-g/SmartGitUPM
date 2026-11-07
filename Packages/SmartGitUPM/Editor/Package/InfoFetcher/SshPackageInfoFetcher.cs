// #define DEBUG_PROSESS

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace SmartGitUPM.Editor
{
    internal sealed class SshPackageInfoFetcher : IPackageInfoFetcher
    {
        public bool IsProcessing{ get; private set; }
        public string SupportProtocol { get; } = "SSH";

        bool _isDisposed;
        CancellationTokenSource _tokenSource;
        readonly PackageInstaller _installer;

        public SshPackageInfoFetcher(PackageInstaller installer) => _installer = installer;

        public bool IsSupported(string url) => url.StartsWith("git");

        public async Task<PackageInfoDetails> GetCachedPackageInfo(string packageInstallUrl, IEnumerable<PackageInfo> installedPackages, CancellationToken token = default)
        {
            PackageCacheManager.Initialize();
            var (installUrl, _, packagePath, revision) = ParseInstallUrl(packageInstallUrl);
            if (!string.IsNullOrEmpty(revision)
                || !PackageCacheManager.TryGetByInstallUrl(installUrl, out var cache))
            {
                return default;
            }

            var jsonPath = Application.dataPath + "/../" + HttpPackageInfoFetcher.SgUpmPackageCachePath + "/" + cache.Name + "/"
                           + (!string.IsNullOrEmpty(packagePath) ? packagePath + "/" : string.Empty)
                           + HttpPackageInfoFetcher.PackageJsonFileName;
            if (!File.Exists(jsonPath))
            {
                return default;
            }

            var json = await File.ReadAllTextAsync(jsonPath, token);
            var remote = JsonUtility.FromJson<PackageRemoteInfo>(json);
            if (remote == default)
            {
                return default;
            }

            var installed = _installer.GetInfoByPackageId(installedPackages, installUrl);
            var local = installed != default
                ? ToLocalInfo(installed)
                : default;
            return new PackageInfoDetails(local, remote, packageInstallUrl);
        }

        public async Task<PackageInfoDetails> FetchPackageInfo(string packageInstallUrl, string branch, bool supperReload, CancellationToken token = default)
        {
            var installedPackages = await _installer.GetInfos(token);
            return await FetchPackageInfo(packageInstallUrl, branch, supperReload, installedPackages, token);
        }

        public async Task<PackageInfoDetails> FetchPackageInfo(string packageInstallUrl, string branch, bool supperReload, IEnumerable<PackageInfo> installedPackages, CancellationToken token = default)
        {
            if (!IsSupported(packageInstallUrl))
            {
                throw new ArgumentException("Specify the URL of " + SupportProtocol + ". packageInstallUrl: " + packageInstallUrl, nameof(packageInstallUrl));
            }

            PackageCacheManager.Initialize();

            var (installUrl, remote, packagePath, revision) = ParseInstallUrl(packageInstallUrl);
            var useCachedCheckout = string.IsNullOrEmpty(revision);
            var rootPath = Application.dataPath + "/../" + HttpPackageInfoFetcher.SgUpmPackageCachePath;
            var tempPath = rootPath + "/.tmp_" + CreateUniqID();
            var packageJsonPath = !string.IsNullOrEmpty(packagePath)
                ? packagePath + "/" + HttpPackageInfoFetcher.PackageJsonFileName
                : HttpPackageInfoFetcher.PackageJsonFileName;
            var localInfo = default(PackageLocalInfo);
            var isCachePackage = false;
            var localPackagePath = default(string);
            var absoluteLocalPackageJsonPath = default(string);

            try
            {
                IsProcessing = true;
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                var info = _installer.GetInfoByPackageId(installedPackages, installUrl);
                localInfo = info != default
                    ? ToLocalInfo(info)
                    : default;

                if (localInfo == default
                    && useCachedCheckout
                    && PackageCacheManager.TryGetByInstallUrl(installUrl, out var cache))
                {
                    localInfo = ToLocalInfo(cache);
                    isCachePackage = true;
                }
            }
            finally
            {
                IsProcessing = false;
            }

            if (useCachedCheckout && localInfo != default)
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
                    return new PackageInfoDetails(!isCachePackage ? localInfo : default, serverInfo, packageInstallUrl);
                }
            }

            try
            {
                HttpPackageInfoFetcher.CreateDirectories(rootPath);

                if (!useCachedCheckout)
                {
                    // An explicit ref may be a tag or commit, so do not pull it into the shared branch cache.
                    await RunGitAsync(rootPath, token, "init", tempPath);
                    await RunGitAsync(tempPath, token, "fetch", "--depth", "1", "--no-tags", "--", remote, revision);
                    await RunGitAsync(tempPath, token, "checkout", "--detach", "FETCH_HEAD");
                }
                else if (string.IsNullOrEmpty(absoluteLocalPackageJsonPath))
                {
                    if (string.IsNullOrEmpty(branch))
                    {
                        await RunGitAsync(rootPath, token, "clone", "--single-branch", "--no-tags", remote, tempPath);
                    }
                    else
                    {
                        await RunGitAsync(rootPath, token, "clone", "--single-branch", "--no-tags", "-b", branch, remote, tempPath);
                    }
                }
                else if (string.IsNullOrEmpty(branch))
                {
                    await RunGitAsync(localPackagePath, token, "pull", "origin");
                }
                else
                {
                    await RunGitAsync(localPackagePath, token, "pull", "origin", branch);
                }

                var jsonPath = absoluteLocalPackageJsonPath ?? tempPath + "/" + packageJsonPath;
                if (!File.Exists(jsonPath))
                {
                    throw new InvalidOperationException("Failed to find the package.json.");
                }
                var jsonString = await File.ReadAllTextAsync(jsonPath, token);
                var serverInfo = JsonUtility.FromJson<PackageRemoteInfo>(jsonString);
                if (serverInfo == default)
                {
                    throw new InvalidOperationException("Failed to parse the package.json.");
                }

                if (!useCachedCheckout)
                {
                    Directory.Delete(tempPath, true);
                }
                else if (string.IsNullOrEmpty(absoluteLocalPackageJsonPath))
                {
                    localPackagePath = rootPath + "/" + serverInfo.name;
                    if (Directory.Exists(localPackagePath))
                    {
                        Directory.Delete(localPackagePath, true);
                    }
                    Directory.Move(tempPath, localPackagePath);

                    var cache = new PackageCacheInfo(installUrl, serverInfo.name, serverInfo.displayName, serverInfo.version);
                    PackageCacheManager.Add(cache);
                    PackageCacheManager.Save();
                }

                return new PackageInfoDetails(!isCachePackage ? localInfo : default, serverInfo, packageInstallUrl);
            }
            catch (Exception ex)
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }

                if (ex is OperationCanceledException)
                {
                    throw;
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

        static async Task RunGitAsync(string workingDirectory, CancellationToken token, params string[] arguments)
        {
            token.ThrowIfCancellationRequested();
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start the process.");
            }
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            await WaitForProcessExitAsync(process, token);
            await Task.WhenAll(standardOutput, standardError);
            token.ThrowIfCancellationRequested();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Git package information fetch failed with exit code " + process.ExitCode + ".");
            }
#if DEBUG_PROSESS
            if (!string.IsNullOrEmpty(standardError.Result))
            {
                Debug.LogWarning(standardError.Result);
            }
#endif
        }

        static async Task WaitForProcessExitAsync(Process process, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnExited(object sender, EventArgs args) => tcs.TrySetResult(true);
            process.Exited += OnExited;
            using var registration = token.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (InvalidOperationException)
                {
                    // The process may exit between the check and Kill.
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Cancellation still stops waiting if the OS cannot terminate the process.
                }
                tcs.TrySetCanceled();
            });
            try
            {
                process.EnableRaisingEvents = true;
                if (process.HasExited)
                {
                    tcs.TrySetResult(true);
                }
                await tcs.Task;
            }
            finally
            {
                process.Exited -= OnExited;
            }
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

        static (string InstallUrl, string Remote, string PackagePath, string Revision) ParseInstallUrl(string packageInstallUrl)
        {
            var fragmentIndex = packageInstallUrl.IndexOf('#');
            var installUrl = fragmentIndex >= 0 ? packageInstallUrl.Substring(0, fragmentIndex) : packageInstallUrl;
            var revision = fragmentIndex >= 0 ? Uri.UnescapeDataString(packageInstallUrl.Substring(fragmentIndex + 1)) : string.Empty;
            var queryIndex = installUrl.IndexOf('?');
            return queryIndex >= 0
                ? (installUrl, installUrl.Substring(0, queryIndex), ExtractPathFromQuery(installUrl.Substring(queryIndex + 1)), revision)
                : (installUrl, installUrl, string.Empty, revision);
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
