
using System;
using System.Net.Http;
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
        public const string GitHubRawUrl = "https://raw.githubusercontent.com";

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
            return FetchPackageInfoByPackageJsonUrl(packageInstallUrl, gitPackageJsonUrl, token);
        }
        
        public async Task<PackageInfoDetails> FetchPackageInfoByPackageJsonUrl(string packageInstallUrl, string gitPackageJsonUrl, CancellationToken token = default)
        {
            var info = default(PackageInfo);
            try
            {
                IsProcessing = true;
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                
                info = await _installer.GetInfoByPackageId(packageInstallUrl, _tokenSource.Token);
                var local = info != default
                    ? new PackageLocalInfo
                    {
                        name = info.name,
                        version = info.version,
                        displayName = info.displayName
                    }
                    : default;
                var server = await FetchPackageInfo(gitPackageJsonUrl);
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
        
        public async Task<PackageServerInfo> FetchPackageInfo(string packageJsonUrl)
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
                    return JsonUtility.FromJson<PackageServerInfo>(op.downloadHandler.text);
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
            var resultUrl = packageInstallUrl.Contains("github.com") ? GitHubRawUrl + pathWithoutFileName : uri.GetLeftPart(UriPartial.Authority) + pathWithoutFileName;
            var raw = packageInstallUrl.Contains("github.com") ? $"refs/heads/{branch}" : $"raw/{branch}";
            return $"{resultUrl}/{raw}/{path}";
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