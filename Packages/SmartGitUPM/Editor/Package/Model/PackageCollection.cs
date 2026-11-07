
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace SmartGitUPM.Editor
{
    internal sealed class PackageCollection : IDisposable
    {
        public event Action<PackageInfoDetails> OnDetailsUpdated = delegate { };
        public event Action<PackageInfoDetails> OnLatestDetailsUpdated = delegate { };
        public bool IsDisposed{ get; private set; }
        public bool IsFetching => _tokenSource != default;
        public bool HasMetas => Metas is {Count: > 0};
        public IReadOnlyList<PackageMetaData> Metas { get; private set; }
        public List<PackageInfoDetails> Details { get; private set; } = new ();

        readonly IReadOnlyList<IPackageInfoFetcher> _infoFetchers;
        readonly PackageInstaller _installer;
        CancellationTokenSource _tokenSource;

        public PackageCollection(
            PackageInstaller installer,
            IReadOnlyList<IPackageInfoFetcher> infoFetchers,
            IReadOnlyList<PackageMetaData> metas)
        {
            _installer = installer;
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

        public async Task FetchPackages(List<PackageInfoDetails> details, IEnumerable<PackageMetaData> metas, bool superReload, CancellationToken token = default, bool throwOnError = false)
        {
            token.ThrowIfCancellationRequested();
            _tokenSource?.SafeCancelAndDispose();
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _tokenSource = tokenSource;
            var fetchToken = tokenSource.Token;
            try
            {
                var packageMetas = metas.ToArray();
                var installUrls = new HashSet<string>(packageMetas
                    .Where(meta => !string.IsNullOrEmpty(meta.InstallUrl))
                    .Select(meta => meta.InstallUrl.ToPackageKey()), StringComparer.OrdinalIgnoreCase);
                var existingUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                details.RemoveAll(detail =>
                {
                    var key = detail.PackageInstallUrl.ToPackageKey();
                    return !installUrls.Contains(key) || !existingUrls.Add(key);
                });

                var installedPackages = throwOnError
                    ? await _installer.GetInfosChecked(fetchToken)
                    : await _installer.GetInfos(fetchToken);
                var taskList = new List<Task>(packageMetas.Length);
                foreach (var meta in packageMetas)
                {
                    taskList.Add(FetchPackage(meta, details, installedPackages, superReload, fetchToken, throwOnError));
                }
                await Task.WhenAll(taskList);
                if (throwOnError)
                {
                    fetchToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                tokenSource.Dispose();
                if (ReferenceEquals(_tokenSource, tokenSource))
                {
                    _tokenSource = default;
                }
            }
        }

        async Task FetchPackage(PackageMetaData meta, List<PackageInfoDetails> details, IEnumerable<PackageInfo> installedPackages, bool superReload, CancellationToken token, bool throwOnError)
        {
            if (string.IsNullOrEmpty(meta.InstallUrl))
            {
                if (throwOnError)
                {
                    throw new InvalidOperationException("A registered package has no install URL.");
                }
                Debug.LogError("Package install URL is not set.");
                return;
            }
            var fetcher = GetInfoFetcher(meta.InstallUrl);
            if (fetcher == default)
            {
                if (throwOnError)
                {
                    throw new InvalidOperationException("No package information fetcher supports a registered package.");
                }
                Debug.LogError("Package info fetcher is not supported. Support Protocols: " + GetSupportProtocolsString());
                return;
            }

            var installUrl = meta.InstallUrl.Trim().TrimEnd('/');
            var branch = meta.Branch?.Trim() ?? string.Empty;
            try
            {
                var cached = await fetcher.GetCachedPackageInfo(installUrl, installedPackages, token);
                token.ThrowIfCancellationRequested();
                if (cached != default)
                {
                    UpsertDetails(details, cached);
                }
            }
            catch (OperationCanceledException)
            {
                if (throwOnError)
                {
                    throw;
                }
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to read package cache. Install url: " + installUrl + "\n" + exception.Message);
            }

            if (token.IsCancellationRequested)
            {
                if (throwOnError)
                {
                    token.ThrowIfCancellationRequested();
                }
                return;
            }

            try
            {
                var latest = await fetcher.FetchPackageInfo(installUrl, branch, superReload, installedPackages, token);
                token.ThrowIfCancellationRequested();
                if (throwOnError)
                {
                    if (latest == default || !latest.IsLoaded || string.IsNullOrEmpty(latest.Remote.version))
                    {
                        throw new InvalidOperationException("Failed to fetch current package information.");
                    }
                    var installed = PackageInstaller.FindRegisteredPackage(installedPackages, installUrl);
                    latest.Installed(installed != default
                        ? new PackageLocalInfo { name = installed.name, version = installed.version, displayName = installed.displayName }
                        : default);
                    latest.UpdatePackageUrl(meta.InstallUrl.Trim());
                }
                if (latest != default)
                {
                    UpsertDetails(details, latest, true);
                }
            }
            catch (OperationCanceledException)
            {
                if (throwOnError)
                {
                    throw;
                }
                // A newer refresh or window close superseded this request.
            }
            catch (Exception exception)
            {
                if (throwOnError)
                {
                    throw;
                }
                Debug.LogWarning("Failed to fetch package information. Install url: " + installUrl + "\n" + exception.Message);
            }
        }

        void UpsertDetails(List<PackageInfoDetails> details, PackageInfoDetails detail, bool isLatest = false)
        {
            var index = details.FindIndex(item => string.Equals(item.PackageInstallUrl.ToPackageKey(), detail.PackageInstallUrl.ToPackageKey(), StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                if (!isLatest)
                {
                    return;
                }
                details[index] = detail;
            }
            else
            {
                details.Add(detail);
            }
            OnDetailsUpdated(detail);
            if (isLatest)
            {
                OnLatestDetailsUpdated(detail);
            }
        }

        public void ApplyFetchedDetail(PackageInfoDetails detail)
        {
            if (detail != default && Metas.Any(meta => !string.IsNullOrEmpty(meta.InstallUrl)
                                                       && string.Equals(meta.InstallUrl.ToPackageKey(), detail.PackageInstallUrl.ToPackageKey(), StringComparison.OrdinalIgnoreCase)))
            {
                UpsertDetails(Details, detail, true);
            }
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
