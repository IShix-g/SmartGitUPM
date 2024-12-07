
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SmartGitUPM.Editor
{
    internal sealed class PackageVersionChecker : IDisposable
    {
        public readonly string GitInstallUrl;
        public readonly string BranchName;
        public readonly string PackageName;
        public bool IsProcessing => _isNowLoading || _packageInstaller.IsProcessing;
        public bool IsLoaded => ServerInfo != default && LocalInfo != default;
        public PackageJson ServerInfo { get; private set; }
        public PackageJson LocalInfo { get; private set; }

        readonly PackageInstaller _packageInstaller = new();
        bool _isDisposed;
        CancellationTokenSource _tokenSource;
        bool _isNowLoading;
        
        public sealed class PackageJson
        {
            public string name;
            public string version;
            public string VersionString => !string.IsNullOrEmpty(version) ? "v" + version : "v---";
        }
        
        public PackageVersionChecker(string gitInstallUrl, string branchName, string packageName)
        {
            GitInstallUrl = gitInstallUrl;
            BranchName = branchName;
            PackageName = packageName;
            LocalInfo = GetLocalInfo(packageName);
        }

        public bool HasNewVersion()
        {
            if (ServerInfo == default)
            {
                throw new InvalidOperationException("ServerInfo is not loaded.");
            }
            var current = new Version(ServerInfo.version);
            var server = new Version(LocalInfo.version);
            return current.CompareTo(server) >= 0;
        }
        
        public async Task Fetch()
        {
            _isNowLoading = true;
            var gitPackageJsonUrl = HttpPackageInfoFetcher.ToRawPackageJsonURL(GitInstallUrl, BranchName);
            var request = UnityWebRequest.Get(gitPackageJsonUrl);
            
            try
            {
                await request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    ServerInfo = JsonUtility.FromJson<PackageJson>(request.downloadHandler.text);
                }
                else
                {
                    throw new InvalidOperationException(request.error);
                }
            }
            finally
            {
                request.Dispose();
                _isNowLoading = false;
            }
        }

        public PackageJson GetLocalInfo(string packageName)
        {
            var path = "Packages/" + packageName + "/package.json";
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<PackageJson>(json);
        }

        public void CheckVersion(CancellationToken token = default)
        {
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            var localVersion = LocalInfo.VersionString;
            var serverVersion = ServerInfo.VersionString;
            if (HasNewVersion())
            {
                EditorUtility.DisplayDialog(
                    "You have the latest version.",
                    "Editor: " + localVersion + " | GitHub: " + serverVersion + "\nThe current version is the latest release.",
                    "Close");
            }
            else
            {
                var isOpen = EditorUtility.DisplayDialog(
                    localVersion + " -> " + serverVersion, "There is a newer version (" + serverVersion + ").",
                    "Update",
                    "Close");
                        
                if (isOpen)
                {
                    _packageInstaller.Install(GitInstallUrl, _tokenSource.Token).Handled();
                }
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _tokenSource?.SafeCancelAndDispose();
            _packageInstaller.Dispose();
        }
    }
}