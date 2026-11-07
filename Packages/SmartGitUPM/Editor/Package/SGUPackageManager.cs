
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartGitUPM.Editor
{
    internal sealed class SGUPackageManager : IDisposable
    {
        public PackageInstaller Installer { get; }
        public PackageCollection Collection { get; }
        public PackageCollectionSetting Setting { get; }

        readonly IReadOnlyList<IPackageInfoFetcher> _infoFetchers;
        bool _isDisposed;

        public SGUPackageManager(
            PackageInstaller installer,
            IReadOnlyList<IPackageInfoFetcher> infoFetchers,
            PackageCollectionSetting setting)
        {
            Installer = installer;
            _infoFetchers = infoFetchers;
            Setting = setting;
            Collection = new PackageCollection(Installer, _infoFetchers, Setting.Packages);
        }

        public async Task FetchPackagesAsync(bool superReload, CancellationToken token = default, bool throwOnError = false)
        {
            token.ThrowIfCancellationRequested();
            Collection.Set(Setting.Packages);
            if (Collection.HasMetas)
            {
                await Collection.FetchPackages(Collection.Details, Collection.Metas, superReload, token, throwOnError);
            }
        }

        public async Task<bool> HasUninstalledPackagesAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var urls = GetRegisteredPackageUrls();
            if (urls.Length == 0)
            {
                return false;
            }

            var installed = await Installer.GetInfosChecked(token);
            token.ThrowIfCancellationRequested();
            foreach (var url in urls)
            {
                if (PackageInstaller.FindRegisteredPackage(installed, url) == default)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> HasUpdatesAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            GetRegisteredPackageUrls();
            var hasUpdate = false;
            void CheckLatest(PackageInfoDetails detail)
            {
                hasUpdate |= detail.IsInstalled && detail.HasUpdate;
            }
            Collection.OnLatestDetailsUpdated += CheckLatest;
            try
            {
                await FetchPackagesAsync(true, token, true);
                token.ThrowIfCancellationRequested();
                return hasUpdate;
            }
            finally
            {
                Collection.OnLatestDetailsUpdated -= CheckLatest;
            }
        }

        public async Task UpdatePackageAsync(string packageInstallUrl, CancellationToken token = default, bool showProgressBar = false)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(packageInstallUrl))
            {
                throw new ArgumentException("Specify a complete registered package install URL.", nameof(packageInstallUrl));
            }

            for (var i = 0; i < Setting.Length; i++)
            {
                var meta = Setting.GetPackageAt(i);
                if (meta != default
                    && !string.IsNullOrWhiteSpace(meta.InstallUrl)
                    && string.Equals(meta.InstallUrl.Trim(), packageInstallUrl.Trim(), StringComparison.Ordinal))
                {
                    await Installer.InstallChecked(new[] { meta.InstallUrl.Trim() }, token, showProgressBar);
                    return;
                }
            }
            throw new ArgumentException("The package install URL is not registered in Smart Git UPM.", nameof(packageInstallUrl));
        }

        public async Task InstallAllAsync(CancellationToken token = default, bool showProgressBar = false)
        {
            token.ThrowIfCancellationRequested();
            var urls = GetRegisteredPackageUrls();
            if (urls.Length > 0)
            {
                await Installer.InstallChecked(urls, token, showProgressBar);
            }
        }

        string[] GetRegisteredPackageUrls()
        {
            var urls = new string[Setting.Length];
            for (var i = 0; i < urls.Length; i++)
            {
                var meta = Setting.GetPackageAt(i);
                if (meta == default || string.IsNullOrWhiteSpace(meta.InstallUrl))
                {
                    throw new InvalidOperationException("A registered package has no install URL.");
                }
                urls[i] = meta.InstallUrl.Trim();
            }
            return urls;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            Installer?.Dispose();
            Collection?.Dispose();
            foreach (var infoFetcher in _infoFetchers)
            {
                infoFetcher?.Dispose();
            }
        }
    }
}
