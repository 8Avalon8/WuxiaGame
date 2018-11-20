using UnityEditor;
using System.IO;
using GLib;
using System.Text;
using AiUnity.NLog.Core.Common;
using AiUnity.NLog.Core.LayoutRenderers;
using HSFrameWork.ConfigTable.Editor.Impl;
using HSFrameWork.ConfigTable.Editor;

namespace HSFrameWork.Common.Editor
{
    /// <summary>
    /// HSFrameWork基础的一些菜单实现
    /// </summary>
    public class HSMenu
    {
        /// <summary>
        /// 菜单 Tools♥/[帮助_更新配置]
        /// </summary>
        [MenuItem("Tools♥/[帮助_更新配置]", false, 1)]
        public static void Help()
        {
            RunTimeConfiger.ReloadConfig();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("当前多线程模式: [{0}]".Eat(TE.NoThreadExtention ? "禁用" : "启用"));
            sb.AppendLine("当前Value包压缩格式: [{0}]".Eat(HSCTC.ActiveCompressMode));
            sb.AppendLine("当前打包语言: [{0}]".Eat(HSCTC.ActiveLanguage == null ? "简体中文（原始）" : HSCTC.ActiveLanguage));
            sb.AppendLine("强制测试HotFix: [{0}]".f(HSCTC.ForceHotPatchTestInAppTagFile.Exists() ? "启用" : "禁用"));

            sb.AppendLine();
            try
            {
                sb.AppendLine(File.ReadAllText(HSCTC.HelpFile));
            }
            catch { }
            EditorUtility.DisplayDialog("关于打包", sb.ToString(), "关闭");
        }

        /// <summary>
        ///  菜单 Tools♥/[打开配置目录]
        /// </summary>
        [MenuItem("Tools♥/[打开配置目录]", false)]
        public static void OpenConfigDir()
        {
            EditorUtility.RevealInFinder(HSCTC.ConfigPath.StandardSub("Readme.txt"));
        }

        /// <summary>
        /// 菜单 Tools♥/[打开Persistent目录]
        /// </summary>
        [MenuItem("Tools♥/[打开Persistent目录]", false)]
        public static void OpenPersistentDir()
        {
            var dummy = HSUnityEnv.InPersistentDataFolder(".Dummy");
            dummy.WriteAllText("");
            EditorUtility.RevealInFinder(dummy);
        }

        /// <summary>
        /// 菜单 Tools♥/打开NLog日志目录
        /// </summary>
        [MenuItem("Tools♥/打开NLog日志目录")]
        public static void OpenNlogFolder()
        {
            if (NLogConfigFile.ConfigFile.ExistsAsFile())
                EditorUtility.RevealInFinder(NLogConfigFile.ConfigFile);
            else
                EditorUtility.RevealInFinder(LogPathLayoutRendererGG.LogPath);
        }
    }
}
