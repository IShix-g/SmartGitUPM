
using System;
using System.Collections.Generic;

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
            Collection = new PackageCollection(_infoFetchers, Setting.Packages);
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
