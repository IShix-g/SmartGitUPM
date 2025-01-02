
using System;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class OnValueChangedAttribute : PropertyAttribute
    {
        public string MethodName { get; }

        public OnValueChangedAttribute(string methodName)
            => MethodName = methodName;
    }
}