
using System.Linq;
using UnityEditor;
using SmartGitUPM.Editor;
using UnityEngine;

namespace Editor
{
    public class TestWindow
    {
        [MenuItem("Tests/Create Path")]
        public static void CreatePath()
        {
            AssetDatabaseSupport.CreateDirectories("Assets/Test/Test2");
        }
        
        [MenuItem("Tests/Install Package")]
        public static void InstallPackage()
        {
            var installer = new PackageInstaller();

            installer.Install(new[]
                {
                    "https://github.com/IShix-g/CMSuniVortex.git?path=Packages/CMSuniVortex",
                    "https://github.com/IShix-g/SMCP-Configurator.git?path=Packages/SMCPConfigurator#1.0.9"
                })
                .Handled(task =>
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }

                    var details = task.Result;
                    Debug.Log(details.Select(x => x.name + " " + x.version).Aggregate((a, b) => a + "\n" + b));
                });
        }
        
        [MenuItem("Tests/Execute Git Clone")]
        public static void ExecuteGitClone()
        {
            var installer = new PackageInstaller();
            var fetcher = new SshPackageInfoFetcher(installer);
            var packageSsh = "git@IShix-g-GitHub:IShix-g/UnityJenkinsBuilder.git?path=Assets/Plugins/Jenkins";
            var branch = "main";
            
            fetcher.FetchPackageInfo(packageSsh, branch, true)
                .Handled(task =>
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }
                    
                    var details = task.Result;
                    Debug.Log("Server: \n" + JsonUtility.ToJson(details.Server, true) + "\nLocal: \n" + JsonUtility.ToJson(details.Local, true));
                });
        }

        [MenuItem("Tests/Update Check")]
        static void UpdateCheck()
        {
            var updater = new PackageUpdateChecker();
            updater.CheckUpdate()
                .Handled();
        }

        [MenuItem("Tests/Open Dialog")]
        static void OpenDialog()
        {
            var contents = new CustomDialogContents(
                    "System Update Required",
                    "Your system requires an important update to improve performance and security.",
                    () => Debug.Log("Clicked Yes"),
                    "Web",
                    "Close",
                    isClickedYes => Debug.Log("Closed. Clicked Yes: " + isClickedYes)
                );
            CustomDialog.Open(contents, "Notice");
        }
    }
}