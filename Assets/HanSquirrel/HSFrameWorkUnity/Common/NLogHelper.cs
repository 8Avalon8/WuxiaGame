using AiUnity.NLog.Core.Common;
using UnityEngine;
using GLib;
using System.IO;
using AiUnity.NLog.Core;
using HanSquirrel.ResourceManager;

namespace HSFrameWork.Common
{
    /// <summary>
    /// NLog工具类
    /// </summary>
    public class NLogHelper
    {
        /// <summary>
        /// 删除本设备特定的NLog配置文件
        /// </summary>
        public static void DeleteNLogConfigLocal()
        {
            LocalConfig.Delete();
        }

        /// <summary>
        /// 存储本设备特定的NLog配置文件
        /// </summary>
        public static void SaveNLogConfigLocal(string context)
        {
            if (context != null)
                LocalConfig.WriteAllText(context);
            else
                LocalConfig.Delete();
        }

        /// <summary>
        /// 只读。缺省的NLOG配置文件缓存路径
        /// </summary>
        public static readonly string DefaultConfig = HSUnityEnv.InPersistentDataFolder("NLog/NLogDefault.xml");
        /// <summary>
        /// 只读。本设备特定的NLog配置文件存储路径
        /// </summary>
        public static readonly string LocalConfig = HSUnityEnv.InPersistentDataFolder("NLog/NLogLocal.xml");
        /// <summary>
        /// 只读。本机开发人员的NLOG配置文件路径
        /// </summary>
        public const string DevConfig = "data/NLogDevThisPC.xml";

        /// <summary>
        /// 按照优先级由高到低选择有效的配置：
        /// 1）DevConfig（开发者PC上存储的配置）
        /// 2）LocalConfig（作用是对某个玩家或者某个手机由服务端指定NLog配置来取得详细运行信息）
        /// 3）DefaultConfig （在AB包中的配置文件。如nlogConfigAssetPath是null则忽略）
        /// 4）内置配置
        /// </summary>
        /// <param name="nlogConfigAssetPath">缺省的NLog配置文件的资源路径</param>
        public static void AutoRefreshNLogConfig(string nlogConfigAssetPath)
        {
            DefaultConfig.Delete();
            NLogConfigFile.ConfigFile.Delete();

            if (nlogConfigAssetPath.Visible())
            {
                var ta = ResourceLoader.LoadAsset<TextAsset>(nlogConfigAssetPath);
                if (ta != null)
                    DefaultConfig.WriteAllText(ta.text);
            }

            string source, sourceFile;
            if (DevConfig.ExistsAsFile())
            {
                source = "当前主机开发者的配置";
                sourceFile = DevConfig;
            }
            else if (LocalConfig.ExistsAsFile())
            {
                source = "当前实例指定的配置";
                sourceFile = LocalConfig;
            }
            else if (DefaultConfig.ExistsAsFile())
            {
                source = "资源中的配置:{0}".f(nlogConfigAssetPath);
                sourceFile = DefaultConfig;
            }
            else
            {
                source = "程序内置的配置";
                sourceFile = null;
            }

            if (sourceFile != null)
            {
                //Debug.LogFormat("NLog使用了[{0}] ：[{1}] 复制到 [{2}]。", source, sourceFile, NLogConfigFile.ConfigFile);
                File.Copy(sourceFile, NLogConfigFile.ConfigFile, true);
            }
            else
            {
                NLogConfigFile.ConfigFile.WriteAllText(NLogConfigFile.DefaultNLogConfig);
                Debug.LogFormat("NLog使用了 [{0}]。", source);
            }

            if (NLogConfigFile.ConfigFile.ReadAllText() != NLogConfigFile.CurrentConfigText)
            {
                NLogManager.Instance.ReloadConfig();
                HSLogManager.GetLogger("Default").Info("Nlog日志系统重新加载，使用了[{0}]。", source);
            }
        }

        /// <summary>
        /// 强制使用此配置文件
        /// </summary>
        public static void ForceUsingConfigFile(string configFile)
        {
            File.Copy(configFile, NLogConfigFile.ConfigFile, true);
            NLogManager.Instance.ReloadConfig();
            HSLogManager.GetLogger("Default").Info("Nlog日志系统重新加载，使用了[{0}]。", configFile);
        }
    }
}
