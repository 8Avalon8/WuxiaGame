#if AIUNITY_CODE
using UnityEngine;
using AiUnity.Common.IO;
using System.IO;
using GLib;

namespace AiUnity.NLog.Core.Common
{
    /// <summary>
    /// NLog仅仅从 Application.persistentDataPath.Sub("NLog/NLog.xml") 中加载。
    /// 复杂的Nlog.xml管理在框架外实现。
    /// </summary>
    public class NLogConfigFile : UnityFileInfo<NLogConfigFile>
    {
        public static readonly string ConfigFile = Application.persistentDataPath.Sub("NLog").CreateDir().Sub("NLog.xml");

        public NLogConfigFile()
        {
            FileInfo = new FileInfo(ConfigFile);
            //Debug.LogFormat("NLog 配置文件 [{0}]，此文件会被程序自动覆盖。", ConfigFile);
        }

        public static string CurrentConfigText { get; private set; }

        public string GetConfigText()
        {
            CurrentConfigText = ConfigFile.ExistsAsFile() ? ConfigFile.ReadAllText() : DefaultNLogConfig;
            return CurrentConfigText;
        }

        public static string DefaultNLogConfig = 
            @"<?xml version = ""1.0"" encoding=""utf-8"" ?>
<nlog assertException = ""True"" >
<time type= ""AccurateLocal"" />
  <targets async =""false"" >
    <target layout=""${date:format=HH\:mm\:ss.fff} F${framecount_gg} @T${pad:padCharacter=0:padding=3:inner=${threadid}} ${pad:padding=-12:inner=${logger}} ${message} ${exception:format=toString}"" name=""UnityConsole"" type=""UnityConsole"" />
    <target layout=""${date:format=HH\:mm\:ss.fff} F${framecount_gg} @T${pad:padCharacter=0:padding=3:inner=${threadid}} ${pad:padding=-12:inner=${logger}} ${message} ${exception:format=toString}"" name=""File"" type=""File"" fileName=""${log_path_gg}/LOG_${appstarttime_gg}_G.log"" autoClear=""False"" />
  </targets>
  <rules>
    <logger name=""*"" target=""UnityConsole"" levels=""Assert, Fatal, Error, Warn, Info"" namespace=""*"" />
    <logger name=""*"" target=""File"" levels=""Assert, Fatal, Error, Warn, Info"" namespace=""*"" />
  </rules>
</nlog>
";
    }
}
#endif