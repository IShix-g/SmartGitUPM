
using System;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DynamicDropdownAttribute : PropertyAttribute
    {
        public string MethodName { get; }

        public DynamicDropdownAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}