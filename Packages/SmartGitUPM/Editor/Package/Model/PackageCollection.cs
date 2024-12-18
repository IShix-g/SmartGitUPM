
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    public sealed class PackageCollection : IDisposable
    {
        public bool IsDisposed{ get; private set; }
        public bool HasMetas => Metas is {Count: > 0};
        public IReadOnlyList<PackageMetaData> Metas { get; private set; }
        public List<PackageInfoDetails> Details { get; private set; } = new ();
        
        readonly IReadOnlyList<IPackageInfoFetcher> _infoFetchers;
        CancellationTokenSource _tokenSource;

        public PackageCollection(
            IReadOnlyList<IPackageInfoFetcher> infoFetchers,
            IReadOnlyList<PackageMetaData> metas)
        {
            _infoFetchers = infoFetchers;
            Set(metas);
        }
        
        public void Set(IReadOnlyList<PackageMetaData> metas) => Metas = metas;

        public Task FetchPackages(bool superReload, CancellationToken token = default)
        {
            if (!HasMetas)
            {
                throw new InvalidOperationException("Package meta data is not set.");
            }
            return FetchPackages(Details, Metas, superReload, token);
        }
        
        public async Task FetchPackages(List<PackageInfoDetails> details, IEnumerable<PackageMetaData> metas, bool superReload, CancellationToken token = default)
        {
            _tokenSource?.SafeCancelAndDispose();
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            details.Clear();
            foreach (var meta in metas)
            {
                if (_tokenSource.IsCancellationRequested)
                {
                    break;
                }
                try
                {
                    if (string.IsNullOrEmpty(meta.InstallUrl))
                    {
                        throw new InvalidOperationException("Package install URL is not set.");
                    }
                    var fetcher = GetInfoFetcher(meta.InstallUrl);
                    if (fetcher == default)
                    {
                        throw new InvalidOperationException("Package info fetcher is not supported. Support Protocols: " + GetSupportProtocolsString());
                    }
                    var installUrl = meta.InstallUrl.Trim().TrimEnd('/');
                    var branch = meta.Branch.Trim();
                    var detail = await fetcher.FetchPackageInfo(installUrl, branch, superReload, _tokenSource.Token);
                    details.Add(detail);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            _tokenSource.Dispose();
            _tokenSource = default;
        }

        public string[] GetSupportProtocols()
        {
            var protocols = new string[_infoFetchers.Count];
            for (var i = 0; i < _infoFetchers.Count; i++)
            {
                protocols[i] = _infoFetchers[i].SupportProtocol;
            }
            return protocols;
        }
        
        public string GetSupportProtocolsString()
        {
            var protocols = string.Empty;
            foreach (var infoFetcher in _infoFetchers)
            {
                protocols += infoFetcher.SupportProtocol + ",";
            }
            return protocols;
        }
        
        IPackageInfoFetcher GetInfoFetcher(string packageInstallUrl)
        {
            foreach (var infoFetcher in _infoFetchers)
            {
                if (infoFetcher.IsSupported(packageInstallUrl))
                {
                    return infoFetcher;
                }
            }
            return default;
        }

        public string[] GetInstallPackageUrls() => GetInstallPackageUrls(Metas);
        
        string[] GetInstallPackageUrls(IReadOnlyList<PackageMetaData> metas)
        {
            var result = new string[metas.Count];
            for (var i = 0; i < metas.Count; i++)
            {
                result[i] = metas[i].InstallUrl;
            }
            return result;
        }
        
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            foreach (var infoFetcher in _infoFetchers)
            {
                infoFetcher?.Dispose();
            }
            _tokenSource?.SafeCancelAndDispose();
            _tokenSource = default;
        }
    }
}