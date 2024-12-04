
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Pool;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace SmartGitUPM.Editor
{
    public sealed class PackageInstaller : IDisposable
    {
        public bool IsProcessing{ get; private set; }

        bool _isDisposed;
        readonly ObjectPool<EditorAsync> _pool;
        CancellationTokenSource _tokenSource;
        
        public PackageInstaller()
        {
            _pool = new ObjectPool<EditorAsync>(
                createFunc: () => new EditorAsync(),
                actionOnGet: _ => {},
                actionOnRelease: obj => obj.Reset(),
                actionOnDestroy: obj => obj.Dispose(),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 100
            );
        }

        public async Task<IEnumerable<PackageInfo>> GetInfos(CancellationToken token = default)
        {
            var op = _pool.Get();
            try
            {
                IsProcessing = true;
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                var request = Client.List();
                await op.StartAsync(() => request.IsCompleted, _tokenSource.Token);
                return request.Result;
            }
            finally
            {
                _pool.Release(op);
                if (_tokenSource != default)
                {
                    _tokenSource.Dispose();
                    _tokenSource = default;
                }
                IsProcessing = false;
            }
        }
        
        public async Task<PackageInfo> GetInfoByPackageId(string packageIdOrPackageInstallUrl, CancellationToken token = default)
        {
            var infos = await GetInfos(token);
            foreach (var info in infos)
            {
                if (info.packageId.Contains(packageIdOrPackageInstallUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return info;
                }
            }
            return default;
        }
        
        public async Task Install(string packageId, CancellationToken token = default, bool showProgressBar = true)
        {
            var op = _pool.Get();
            try
            {
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Package operations", "Please wait...", 0.2f);
                }
                
                var info = await GetInfoByPackageId(packageId, token);
                var specifiedVersion = info != default
                        ? GetVersionFromPackageID(info.packageId)
                        : string.Empty;
                
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Package operations", info != default ? "Update package..." : "Install Package...", 0.5f);
                }
                
                IsProcessing = true;
                var request = Client.Add(info != default ? info.name : packageId);
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                await op.StartAsync(() => request.IsCompleted, _tokenSource.Token);

                switch (request.Status)
                {
                    case StatusCode.Success:
                        var msg = default(string);
                        if (info != default)
                        {
                            if (request.Result.version == info.version)
                            {
                                msg = request.Result.displayName + " ( " + request.Result.name + " ), You have the latest version.";
                            }
                            else
                            {
                                msg =  request.Result.displayName + " ( " + request.Result.name + " ) Version: " + info.version + " \u2192 " + request.Result.version + ", Updated.";
                            }
                        }
                        else
                        {
                            msg = request.Result.displayName + " ( " + request.Result.name + ") Version: " + request.Result.version + ", Installed.";
                        }
                        Debug.Log(msg + "\nYou can check the details in the Package Manager.");
                        break;
                    case StatusCode.Failure:
                        // NotFound error if package cannot be retrieved or updated
                        if(request.Error.errorCode == ErrorCode.NotFound)
                        {
                            if (!string.IsNullOrEmpty(specifiedVersion))
                            {
                                Debug.LogWarning("Cannot be updated because a version is specified. Version: " + specifiedVersion + ". Package ID: " + packageId);
                            }
                        }
                        else
                        {
                            Debug.LogError("ErrorCode: " + request.Error.errorCode + " " + request.Error.message + ". Package ID: " + packageId);
                        }
                        break;
                }
            }
            finally
            {
                _pool.Release(op);
                if (_tokenSource != default)
                {
                    _tokenSource.Dispose();
                    _tokenSource = default;
                }
                if (showProgressBar)
                {
                    EditorUtility.ClearProgressBar();
                }
                IsProcessing = false;
            }
        }
        
        public async Task<UnityEditor.PackageManager.PackageCollection> Install(string[] packageIds, CancellationToken token = default, bool showProgressBar = true)
        {
            var op = _pool.Get();
            try
            {
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Package operations", "Please wait...", 0.2f);
                }
                
                var infos = new PackageInfo[packageIds.Length];
                var specifiedVersions = new string[packageIds.Length];
                for (var i = 0; i < packageIds.Length; i++)
                {
                    var info = await GetInfoByPackageId(packageIds[i], token);
                    var specifiedVersion = info != default
                            ? GetVersionFromPackageID(info.packageId)
                            : string.Empty;
                    infos[i] = info;
                    specifiedVersions[i] = specifiedVersion;
                }
                
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Package operations", "Resolving Dependence Packages.", 0.5f);
                }
                
                IsProcessing = true;
                var request = Client.AddAndRemove(packageIds);
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                await op.StartAsync(() => request.IsCompleted, _tokenSource.Token);

                switch (request.Status)
                {
                    case StatusCode.Success:
                        var sb = new StringBuilder();
                        sb.Append("Package operations success: \n");
                        
                        for (var i = 0; i < packageIds.Length; i++)
                        {
                            var packageId = packageIds[i];
                            var info = infos[i];
                            var specifiedVersion = specifiedVersions[i];
                            
                            foreach (var obj in request.Result)
                            {
                                if (!obj.packageId.Contains(packageId, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                
                                if (info != default)
                                {
                                    if(obj.version != info.version)
                                    {
                                        sb.Append("- ");
                                        sb.Append(obj.displayName);
                                        sb.Append(" (");
                                        sb.Append(obj.name);
                                        sb.Append(") Version: ");
                                        sb.Append(info.version);
                                        sb.Append(" \u2192 ");
                                        sb.Append(obj.version);
                                        sb.Append(", Updated.");
                                        sb.Append("\n");
                                    }
                                    else if (!string.IsNullOrEmpty(specifiedVersion))
                                    {
                                        sb.Append("- ");
                                        sb.Append(obj.displayName);
                                        sb.Append(" (");
                                        sb.Append(obj.name);
                                        sb.Append(") Specified version: ");
                                        sb.Append(obj.version);
                                        sb.Append(", Cannot be updated because a version is specified.");
                                        sb.Append("\n");
                                    }
                                    else
                                    {
                                        sb.Append("- ");
                                        sb.Append(obj.displayName);
                                        sb.Append(" (");
                                        sb.Append(obj.name);
                                        sb.Append(") Version: ");
                                        sb.Append(obj.version);
                                        sb.Append(", You have the latest version.");
                                        sb.Append("\n");
                                    }
                                }
                                else
                                {
                                    sb.Append("- ");
                                    sb.Append(obj.displayName);
                                    sb.Append(" (");
                                    sb.Append(obj.name);
                                    sb.Append(") Version: ");
                                    sb.Append(obj.version);
                                    sb.Append(", Installed.");
                                    sb.Append("\n");
                                }
                            }
                        }

                        sb.Append(".\nYou can check the details in the Package Manager.");
                        
                        Debug.Log(sb.ToString());
                        return request.Result;
                    case StatusCode.Failure:
                        Debug.LogError("ErrorCode: " + request.Error.errorCode + " " + request.Error.message);
                        break;
                }
                return default;
            }
            finally
            {
                _pool.Release(op);
                if (_tokenSource != default)
                {
                    _tokenSource.Dispose();
                    _tokenSource = default;
                }
                if (showProgressBar)
                {
                    EditorUtility.ClearProgressBar();
                }
                IsProcessing = false;
            }
        }
        
        public async Task UnInstall(string packageId, CancellationToken token = default, bool showProgressBar = true)
        {
            var op = _pool.Get();
            try
            {
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Package operations", "Please wait...", 0.2f);
                }
                
                var info = await GetInfoByPackageId(packageId, token);
                if (info == default)
                {
                    Debug.LogWarning("Did not exist. ID: " + packageId);
                    return;
                }
                
                IsProcessing = true;
                var request = Client.Remove(info.name);
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                await op.StartAsync(() => request.IsCompleted, _tokenSource.Token);

                switch (request.Status)
                {
                    case StatusCode.Success:
                        Debug.Log("Removed. Name: " + info.displayName + " ( " + info.name + " )");
                        break;
                    case StatusCode.Failure:
                        Debug.LogError("ErrorCode: " + request.Error.errorCode + " " + request.Error.message);
                        break;
                }
            }
            finally
            {
                _pool.Release(op);
                if (_tokenSource != default)
                {
                    _tokenSource.Dispose();
                    _tokenSource = default;
                }
                if (showProgressBar)
                {
                    EditorUtility.ClearProgressBar();
                }
                IsProcessing = false;
            }
        }
        
        public async Task UnInstall(string[] packageIds, CancellationToken token = default, bool showProgressBar = true)
        {
            var op = _pool.Get();
            try
            {
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Package operations", "Please wait...", 0.2f);
                }
                var infos = new HashSet<PackageInfo>();
                foreach (var packageId in packageIds)
                {
                    var info = await GetInfoByPackageId(packageId, token);
                    if (info != default)
                    {
                        infos.Add(info);
                    }
                    else
                    {
                        Debug.LogWarning("Did not exist. ID: " + packageId);
                    }
                }

                if (infos.Count == 0)
                {
                    
                    return;
                }
                
                IsProcessing = true;
                var ids = ToIds(infos);
                var request = Client.AddAndRemove(default, ids);
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                await op.StartAsync(() => request.IsCompleted, _tokenSource.Token);

                switch (request.Status)
                {
                    case StatusCode.Success:
                        var sb = new StringBuilder();
                        sb.Append("Package removed success: \n");
                        foreach (var info in infos)
                        {
                            sb.Append("- Removed. Name: ");
                            sb.Append(info.displayName);
                            sb.Append(" (");
                            sb.Append(info.name);
                            sb.Append(").");
                        }
                        sb.Append(".\nYou can check the details in the Package Manager.");
                        Debug.Log(sb.ToString());
                        break;
                    case StatusCode.Failure:
                        Debug.LogError("ErrorCode: " + request.Error.errorCode + " " + request.Error.message);
                        break;
                }
            }
            finally
            {
                _pool.Release(op);
                if (_tokenSource != default)
                {
                    _tokenSource.Dispose();
                    _tokenSource = default;
                }
                if (showProgressBar)
                {
                    EditorUtility.ClearProgressBar();
                }
                IsProcessing = false;
            }
        }
        
        public void Cancel()
        {
            if (!IsProcessing
                || _tokenSource == default)
            {
                return;
            }
            
            if (!_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();
            }
            _tokenSource.Dispose();
            _tokenSource = default;
        }
        
        string[] ToIds(in HashSet<PackageInfo> infos)
        {
            var ids = new string[infos.Count];
            var index = 0;
            foreach (var info in infos)
            {
                ids[index] = info.packageId;
                index++;
            }
            return ids;
        }
        
        string GetVersionFromPackageID(string url)
        {
            var match = Regex.Match(url, @"#([\d.]+)$");
            return match.Success
                ? match.Groups[1].Value
                : string.Empty;
        }
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            if (IsProcessing)
            {
                Cancel();
            }
            
            if (_tokenSource != default)
            {
                _tokenSource.Dispose();
                _tokenSource = default;
            }
            _pool.Dispose();
        }
        
        public static void OpenPackageManager()
            => EditorApplication.ExecuteMenuItem("Window/Package Manager");
    }
}