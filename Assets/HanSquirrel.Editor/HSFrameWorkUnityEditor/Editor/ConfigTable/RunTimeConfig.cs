using UnityEngine;
using System;
using GLib;
using UnityEditor;
using System.IO;
using HSFrameWork.XLS2XML;
using HSFrameWork.Common;
using HanSquirrel.ResourceManager;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    /// <summary>
    /// 无状态独立工具类
    /// </summary>
    public static class RunTimeConfiger
    {
        public static void ColdBind()
        {
            EditorUtility.ClearProgressBar();
            //在苹果系统下，如果因为异常造成进度条没有被销掉，则Untiy无法操作。
            //在此种情况下，随意修改一个CS文件，就会让这个代码执行。

            ReloadConfig();
            //Debug.Log("运行时配置完成。");
        }

        public static void ReloadConfig()
        {   
            ResourceLoader.LoadFromABAlways = HSCTC.ConfigPath.Sub("force_load_from_streamingassets").ExistsAsFile();

            bool disable;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    disable = false;
                    break;
                default:
                    disable = true;
                    break;
            }

            if (File.Exists(HSCTC.NoThreadExtentionTagFile))
                disable = true;

            GlobalDisableThreadExtention(disable);

            if (HSCTC.ForceHotPatchTestInAppTagFile.ExistsAsFile())
            {
                if (!HSUnityEnv.ForceHotPatchTestInAppTagPath.ExistsAsFile())
                    HSCTC.ResourcesPath.Sub(HSUnityEnv.ForceHotPatchTestInAppTagPath).WriteAllText("DUMMY");
            }
            else
            {
                HSCTC.ResourcesPath.Sub(HSUnityEnv.ForceHotPatchTestInAppTagPath).Delete();
            }
        }

        /// <summary> 强制进入打包机模式 </summary>
        public static IDisposable EnterRobotMode
        {
            get
            {
                bool configBackup = HSCTC.IsRobotMode;
                return DisposeHelper.Create(delegate
                {
                    HSCTC.IsRobotMode = true;
                }, delegate
                {
                    HSCTC.IsRobotMode = configBackup;
                });
            }
        }

        private static void GlobalDisableThreadExtention(bool disabled)
        {
            TE.NoThreadExtention = disabled;
            ConvertorHelper.NoThreadExtention = disabled;
            //Debug.Log(disabled ? "××××××××××××全局禁用多线程扩展×××××××××××××××" : "√√√√√√√√√√全局启用多线程扩展√√√√√√√√√√√√√");
        }


        /// <summary>
        /// 临时禁用
        /// </summary>
        public static IDisposable EnterNoThreadExtentionMode
        {
            get
            {
                bool configBackup = TE.NoThreadExtention;
                GlobalDisableThreadExtention(true);

                return DisposeHelper.Create(null, delegate
                {
                    GlobalDisableThreadExtention(configBackup);
                });
            }
        }
    }
}
