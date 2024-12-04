
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Pool;

namespace SmartGitUPM.Editor
{
    public sealed class PackageUpdateChecker : IDisposable
    {
        bool _disposed;
        CancellationTokenSource _tokenSource;
        
        public async Task CheckUpdate(bool promptForUpdate = true, CancellationToken token = default)
        {
            var factory = new SGUPackageManagerFactory();
            var manager = factory.Create();
            if (!manager.Collection.HasMetas)
            {
                return;
            }
            
            var metas = ListPool<PackageMetaData>.Get();
            foreach (var meta in manager.Collection.Metas)
            {
                var version = GetVersionFromUrl(meta.InstallUrl);
                if (!meta.UpdateNotify
                    || !string.IsNullOrEmpty(version))
                {
                    continue;
                }
                metas.Add(meta);
            }

            if (metas.Count == 0)
            {
                ListPool<PackageMetaData>.Release(metas);
                return;
            }
            
            var details = ListPool<PackageInfoDetails>.Get();
            try
            {
                await  manager.Collection.FetchPackages(details, metas, true, token);

                var index = 0;
                while (details.Count > 0
                       && details.Count > index)
                {
                    var detail = details[index];
                    if(!detail.HasUpdate 
                       || detail.IsFixedVersion)
                    {
                        details.RemoveAt(index);
                        continue;
                    }
                    
                    index++;
                }

                if (details.Count == 0)
                {
                    return;
                }
                
                if (promptForUpdate)
                {
                    var contents = new CustomDialogContents(
                        "An available update has been found.",
                        ToPackageDetailString(details),
                        () => PackageCollectionWindow.Open(false),
                        "Manage Packages",
                        "Close"
                    );
                    CustomDialog.Open(contents, "Package Update Alert");
                }
            }
            finally
            {
                ListPool<PackageMetaData>.Release(metas);
                ListPool<PackageInfoDetails>.Release(details);
            }
        }

        string ToPackageDetailString(List<PackageInfoDetails> details)
        {
            var msg = string.Empty;
            foreach (var detail in details)
            {
                msg += "- " + detail.Server.displayName;
                if (detail.Local == default)
                {
                    msg += " v" + detail.Server.version + " is not installed.\n";
                }
                else if (detail.Server.version != detail.Local.version)
                {
                    msg += " v" + detail.Local.version + " \u2192 v" + detail.Server.version + " Update available.\n";
                }
            }
            return msg;
        }
        
        static string GetVersionFromUrl(string url)
        {
            var match = Regex.Match(url, @"#([\d.]+)$");
            return match.Success
                ? match.Groups[1].Value
                : string.Empty;
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