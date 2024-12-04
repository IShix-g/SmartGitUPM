
using System;
using System.Collections.Generic;

namespace SmartGitUPM.Editor
{
    public sealed class SGUPackageManager : IDisposable
    {
        public PackageInstaller Installer { get; }
        public PackageCollection Collection { get; }
        public PackageCollectionSetting Setting { get; }
        
        bool _isDisposed;
        readonly IReadOnlyList<IPackageInfoFetcher> _infoFetchers;

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