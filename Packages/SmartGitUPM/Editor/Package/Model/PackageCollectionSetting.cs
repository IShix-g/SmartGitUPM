
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    [UniqueScriptableObjectPath("Assets/Editor/SmartGitUPM_PackageCollectionSetting.asset")]
    public sealed class PackageCollectionSetting : ScriptableObject
    {
        [SerializeField] PackageMetaData[] _packages;
        
        public int Length => _packages != default ? _packages.Length : 0;
        
        internal PackageMetaData[] Packages => _packages;
        
        public IReadOnlyList<PackageMetaData> GetPackages() => _packages;
        
        public PackageMetaData GetPackageAt(int index) => _packages[index];
        
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