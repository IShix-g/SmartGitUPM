
using System.Linq;
using UnityEditor;
using SmartGitUPM.Editor;
using SmartGitUPM.Editor.Localization;
using UnityEngine;

namespace Tests
{
    public class Tests
    {
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
                    Debug.Log("Server: \n" + JsonUtility.ToJson(details.Remote, true) + "\nLocal: \n" + JsonUtility.ToJson(details.Local, true));
                });
        }

        [MenuItem("Tests/Update Check")]
        static void UpdateCheck()
        {
            var manager = LanguageManagerFactory.GetOrCreate();
            var strings = EditorInitializationExecuter.GetLocalizedStrings(manager);
            var updater = new PackageUpdateChecker(strings);
            updater.CheckUpdate().Handled();
        }

        [MenuItem("Tests/Open Test Dialog")]
        static void OpenTestDialog()
        {
            var contents = new CustomDialogContents(
                    "System Update Required",
                    "Your system requires an important update to improve performance and security.",
                    () => Debug.Log("Clicked Yes"),
                    "Web",
                    "Close",
                    isClickedYes => Debug.Log("Closed. Clicked Yes: " + isClickedYes)
                );
            CustomDialog.Open(contents, default, "Notice");
        }

        [MenuItem("Tests/Print Package Cache")]
        static void PrintPackageCache()
        {
            PackageCacheManager.Initialize();
            if (PackageCacheManager.HasCache())
            {
                Debug.Log(PackageCacheManager.Infos.Packages.Select(x => "- " + x.DisplayName + " (" + x.Name + ")").Aggregate((a,b) => a + "\n" + b));
            }
        }

        [MenuItem("Tests/Load Languages")]
        static void LoadLanguages()
        {
            var path = "Assets/Tests/LocalizationData.asset";
            var data = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
            var manager = new LanguageManager(data, SystemLanguage.Afrikaans);
            Debug.Log(manager.CurrentLanguage + " " + manager.GetEntry("test1").CurrentValue);
            Debug.Log(manager.CurrentLanguage + " " + manager.GetEntry("test2").CurrentValue);
        }
    }
}