using System;
using GLib;
using UnityEngine;
using UnityEditor;

namespace HSFrameWork.ConfigTable.Editor
{
    /// <summary>
    /// 无状态工具类
    /// </summary>
    public static class UtilsUnity
    {
        public static string GetUnityUpTime()
        {
            return TimeSpan.FromSeconds(EditorApplication.timeSinceStartup).FormatTimeSpanShort();
        }

        public static void ClearMemVerbose()
        {
            Mini.ClearMemVerbose(Debug.Log);
        }
    }
}
