
using System;
using System.Collections.Generic;
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
                if (meta.UpdateNotify)
                {
                    metas.Add(meta);
                }
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
                    if(detail.IsInstalled
                       && (!detail.HasUpdate 
                           || detail.IsFixedVersion))
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
                    var logo = PackageCollectionWindow.GetLogo();
                    CustomDialog.Open(contents, logo, "Package Update Alert");
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
                msg += "- " + detail.Remote.displayName;
                if (detail.Local == default)
                {
                    msg += " v" + detail.Remote.version + " is not installed.\n";
                }
                else if (detail.Remote.version != detail.Local.version)
                {
                    msg += " v" + detail.Local.version + " \u2192 v" + detail.Remote.version + " Update available.\n";
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