
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace SmartGitUPM.Editor
{
    public sealed class SshPackageInfoFetcher : IPackageInfoFetcher
    {
        public const string PackageCachePath = "Library/PackageCache-SmartGitUPM";
        
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
            
            var (absolutePath, query) = ParsePath(packageInstallUrl);
            var rootPath = Application.dataPath + "/../" + PackageCachePath;
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

                var info = await _installer.GetInfoByPackageId(packageInstallUrl, _tokenSource.Token);
                localInfo = info != default ? ToLocalInfo(info) : default;

                if (localInfo == default
                    && PackageCacheManager.TryGetByInstallUrl(packageInstallUrl, out var cache))
                {
                    localInfo = ToLocalInfo(cache);
                    isCachePackage = true;
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
                var serverInfo = JsonUtility.FromJson<PackageServerInfo>(jsonString);
                if (serverInfo != default)
                {
                    return new PackageInfoDetails(!isCachePackage ? localInfo : default, serverInfo, packageInstallUrl);
                }
            }
            
            arguments = string.IsNullOrEmpty(absoluteLocalPackageJsonPath)
                ? $"clone --single-branch --no-tags -b {branch} {absolutePath} {tempPath}"
                : $"pull origin {branch}";

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    arguments = $"/C {command} {arguments}";
                    command = "cmd.exe";
                    break;
                case RuntimePlatform.OSXEditor:
                    arguments = $"-c \"{command} {arguments}\"";
                    command = "/bin/bash";
                    break;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = localInfo != default ? localPackagePath : rootPath
            };

            try
            {
                CreateDirectories(rootPath);
                
                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    // var error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                    await WaitForProcessExitAsync(process);
                    
                    // if (!string.IsNullOrEmpty(error))
                    // {
                    //     UnityEngine.Debug.LogWarning(error);
                    // }
                    
                    var serverInfo = default(PackageServerInfo);
                    if (!string.IsNullOrEmpty(absoluteLocalPackageJsonPath))
                    {
                        var jsonString = await File.ReadAllTextAsync(absoluteLocalPackageJsonPath, token);
                        serverInfo = JsonUtility.FromJson<PackageServerInfo>(jsonString);
                        
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
                        serverInfo = JsonUtility.FromJson<PackageServerInfo>(jsonString);
                        
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
                            packageInstallUrl,
                            serverInfo.name,
                            serverInfo.displayName,
                            serverInfo.version);
                        PackageCacheManager.Add(cache);
                        PackageCacheManager.Save();
                    }
                    
                    return new PackageInfoDetails(!isCachePackage ? localInfo : default, serverInfo, packageInstallUrl);
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

        static void CreateDirectories(string path)
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
        
        static (string AbsolutePath, string Query) ParsePath(string path)
        {
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var url = new Uri(path);
                return (url.AbsolutePath, url.Query);
            }
    
            int index = path.IndexOf('?');
            if (index == -1)
            {
                return (path, string.Empty);
            }
    
            string absolutePath = path.Substring(0, index);
            string query = path.Substring(index);

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