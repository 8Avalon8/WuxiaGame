using HSFrameWork.Common;
using System;
using UnityEditor;
using AiUnity.NLog.Core.Targets;
using UnityEngine;
using GLib;

namespace HSFrameWork.Editor.Test
{
    public class ALiLogNLogTest
    {
        private const string _NLogConfigFile = @"Assets/HanSquirrel.Editor/HSFrameWorkUnityEditor/Editor/Test/ALiLogNLogTest.xml";

        [MenuItem("Tools♥/码农专用/显示AliLog输出日志", false, 1)]
        public static void ShowWriteLogs()
        {
            ALiCloudLogTarget.DumpWriteLog(Debug.Log);
        }

        //[MenuItem("码农专用/测试AliLog", false, 1)]
        public static void DoWork()
        {
            NLogHelper.ForceUsingConfigFile(_NLogConfigFile);
            HSLogManager.GetLogger("Default").Info("Hello from Unity");
        }
    }
}
