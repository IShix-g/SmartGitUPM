
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Pool;

namespace SmartGitUPM.Editor
{
    internal sealed class PackageUpdateChecker : IDisposable
    {
        static readonly List<PackageInfoDetails> s_latestFetchedDetails = new();
        public static event Action<PackageInfoDetails> OnPackageInfoFetched = delegate { };
        public static IReadOnlyList<PackageInfoDetails> LatestFetchedDetails => s_latestFetchedDetails;

        public sealed class LocalizedStrings
        {
            public string Title;
            public string Button;
            public string Installed;
            public string UpdateAvailable;
        }

        readonly LocalizedStrings _localizedStrings;
        bool _disposed;
        CancellationTokenSource _tokenSource;

        public PackageUpdateChecker(LocalizedStrings localizedStrings)
            => _localizedStrings = localizedStrings;

        public async Task CheckUpdate(bool promptForUpdate = true, CancellationToken token = default)
        {
            using var manager = SGUPackageManagerFactory.Create();
            if (!manager.Collection.HasMetas)
            {
                return;
            }

            var metas = HashSetPool<PackageMetaData>.Get();
            try
            {
                foreach (var meta in manager.Collection.Metas)
                {
                    if (meta.UpdateNotify)
                    {
                        metas.Add(meta);
                    }
                }

                if (metas.Count == 0)
                {
                    return;
                }

                await FetchPackages(metas, manager, token, promptForUpdate);
            }
            finally
            {
                HashSetPool<PackageMetaData>.Release(metas);
            }
        }

        async Task FetchPackages(HashSet<PackageMetaData> metas, SGUPackageManager manager, CancellationToken token, bool promptForUpdate)
        {
            var details = ListPool<PackageInfoDetails>.Get();
            var latestDetails = new List<PackageInfoDetails>();
            void OnLatestDetailsUpdated(PackageInfoDetails detail)
            {
                var index = latestDetails.FindIndex(item => string.Equals(item.PackageInstallUrl.ToPackageKey(), detail.PackageInstallUrl.ToPackageKey(), StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    latestDetails[index] = detail;
                }
                else
                {
                    latestDetails.Add(detail);
                }
                PublishFetchedDetail(detail);
            }

            try
            {
                details.Clear();
                manager.Collection.OnLatestDetailsUpdated += OnLatestDetailsUpdated;
                await manager.Collection.FetchPackages(details, metas, true, token);
                details.Clear();
                details.AddRange(latestDetails);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                for (var index = 0; index < details.Count;)
                {
                    var detail = details[index];
                    if (detail.IsInstalled
                        && (!detail.HasUpdate
                            || detail.IsFixedVersion))
                    {
                        details.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                if (details.Count == 0)
                {
                    return;
                }
                if (promptForUpdate)
                {
                    ShowUpdatePrompt(details);
                }
            }
            finally
            {
                manager.Collection.OnLatestDetailsUpdated -= OnLatestDetailsUpdated;
                ListPool<PackageInfoDetails>.Release(details);
            }
        }

        static void PublishFetchedDetail(PackageInfoDetails detail)
        {
            var index = s_latestFetchedDetails.FindIndex(item => string.Equals(item.PackageInstallUrl.ToPackageKey(), detail.PackageInstallUrl.ToPackageKey(), StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                s_latestFetchedDetails[index] = detail;
            }
            else
            {
                s_latestFetchedDetails.Add(detail);
            }
            OnPackageInfoFetched(detail);
        }

        void ShowUpdatePrompt(List<PackageInfoDetails> details)
        {
            var contents = new CustomDialogContents(
                _localizedStrings.Title,
                ToPackageDetailString(details),
                PackageCollectionWindow.Open,
                _localizedStrings.Button,
                "Close"
            );
            var logo = PackageCollectionWindow.GetLogo();
            CustomDialog.Open(contents, logo, "Package Update Alert");
        }

        string ToPackageDetailString(List<PackageInfoDetails> details)
        {
            var msg = string.Empty;
            foreach (var detail in details)
            {
                msg += "- " + detail.Remote.displayName;
                if (detail.Local == default)
                {
                    msg += " v" + detail.Remote.version + _localizedStrings.Installed + "\n";
                }
                else if (detail.Remote.version != detail.Local.version)
                {
                    msg += " v" + detail.Local.version + " \u2192 v" + detail.Remote.version + _localizedStrings.UpdateAvailable + "\n";
                }
            }
            return msg;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _tokenSource?.SafeCancelAndDispose();
            _tokenSource = default;
        }
    }
}
