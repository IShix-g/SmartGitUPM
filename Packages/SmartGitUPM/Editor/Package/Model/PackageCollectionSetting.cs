
using System;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    [UniqScriptableObjectPath("Assets/Editor/SmartGitUPM_PackageCollectionSetting.asset")]
    public sealed class PackageCollectionSetting : ScriptableObject
    {
        [SerializeField] PackageMetaData[] _packages;
        public PackageMetaData[] Packages => _packages;

        public PackageMetaData GetPackage(string installUrl)
        {
            foreach (var package in _packages)
            {
                if (package.InstallUrl.Contains(installUrl))
                {
                    return package;
                }
            }
            throw new ArgumentException("Package not found: " + installUrl, nameof(installUrl));
        }
    }
}