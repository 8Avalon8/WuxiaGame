using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;
using HSFrameWork.Common;
using GLib;
using System.Linq;  

namespace HanSquirrel.ResourceManager
{
    /// <summary>
    /// 热更包管理。
    /// </summary>
    public class HotPatch
    {
        /// <summary>
        /// 初始化，仅仅设置内部状态，不会做任何其他事情。
        /// </summary>
        public static void ColdInit(GameVersionInfo gv)
        {
            _patches = gv.patches;
        }

        /// <summary>
        /// 如果有热更资源，则返回热更AB包地址；否则返回原始资源地址。
        /// </summary>
        public static string GetABFilePath(string abName)
        {
            string patchFilePath = TryGetFilePathInPatch(abName);
            return patchFilePath != null ? patchFilePath : HSUnityEnv.InStreamingAssetsFolder(abName);
        }

        /// <summary>
        /// 如果有热更资源，则返回热更AB包地址；否则返回null。
        /// </summary>
        public static string TryGetFilePathInPatch(string abName)
        {
            string patchFilePath = HSUnityEnv.InHotPatchFolder(abName);
            if (ExistInPatchConfig(abName) && File.Exists(patchFilePath))
                return patchFilePath;
            else
                return null;
        }

        /// <summary>
        /// 删除所有的本地热更文件
        /// </summary>
        public static void ClearAllHotPatchFiles()
        {
            HSUnityEnv.HotPatchFolder.SafeClearDirectory();
        }

        /// <summary>
        /// 删除热更中的文件：abName
        /// </summary>
        public static void DelThisPatch(string abName)
        {
            HSUnityEnv.InHotPatchFolder(abName).Delete();
            HSUnityEnv.InHotPatchFolder(abName+".md5").Delete();
        }

        /// <summary>
        /// 删除所有垃圾热更包。
        /// </summary>
        public static void DeleteAllWastePatches(Patches patches)
        {
            if (!HSUnityEnv.HotPatchFolder.ExistsAsFolder())
                return;

            if (_patches == null || _patches.files == null || _patches.files.Length == 0)
            {
                HSUtils.LogWarning("因为服务器下发的HotPatch命令为空，因此删除所有的HotPatch");
                HotPatch.ClearAllHotPatchFiles();
                return;
            }

            HashSet<string> activePatchs = new HashSet<string>(
            patches.files.Select(x => x.name.ToLower())
                   .SelectMany(x => new string[] { x, x + ".md5" }));

            new DirectoryInfo(HSUnityEnv.HotPatchFolder)
                .GetFiles()
                .Where(x => !activePatchs.Contains(x.Name.ToLower()))
                .ForEachG(x =>
                {
                    HSUtils.LogWarning("删除无效的HotPatch包：{0}", x.Name);
                    x.Delete();
                });
        }

        /// <summary>
        /// 取得本地所有HotPatch文件的实际MD5
        /// </summary>
        public static string GetHotPatchFileMd5DictJson()
        {
            if (!HSUnityEnv.HotPatchFolder.ExistsAsFolder())
                return new Dictionary<string, string>().toJson();

            string keySuf;
            if (Application.platform == RuntimePlatform.IPhonePlayer)
                keySuf = "_ios";
            else if (Application.platform == RuntimePlatform.Android)
                keySuf = "_android";
            else
                return new Dictionary<string, string>().toJson();

            return new DirectoryInfo(HSUnityEnv.HotPatchFolder)
                .GetFiles()
                .Where(x => x.Extension.ToLower() != ".md5")
                .Select(x => Tuple.Create(x.Name + keySuf, MD5Utils.Encrypt(x.ReadAllBytes())))
                .ToDictionary(x => x.Item1, x => x.Item2)
                .toJson();
        }

#if HSFRAMEWORK_DEV_TEST
        /// <summary>
        /// 仅仅去检查热更目录是否有对应文件。
        /// </summary>
        public static bool UnsafeHotPatch = false;
#else
        /// <summary>
        /// 开发者内部使用。
        /// </summary>
        public const bool UnsafeHotPatch = false;
#endif

        private static bool ExistInPatchConfig(string abName)
        {
            return UnsafeHotPatch || (_patches != null && _patches.ContainsFile(abName));
        }

        private static Patches _patches;
    }

    /// <summary>
    /// 游戏启动的时候服务端返回的当前服务器支持的版本
    /// </summary>
    [XmlType("gameversions")]
    public class GameVersions
    {
        [XmlElement("gameversion")]
        public GameVersionInfo[] gameVersions;
    }

    /// <summary>
    /// 服务端支持的游戏版本和该版本的热更包
    /// </summary>
    [XmlType]
    public class GameVersionInfo
    {
        [XmlAttribute("v")]
        public string version;
        [XmlElement]
        public Patches patches;
    }

    /// <summary>
    /// 热更包列表
    /// </summary>
    [XmlType]
    public class Patches
    {
        [XmlAttribute("v")]
        public int version;

        [XmlAttribute]
        public string size;

        [XmlAttribute("content")]
        public string content;

        [XmlElement("patch")]
        public Patch[] files;

        public bool ContainsFile(string name)
        {
            if (files == null)
                return false;
            for (int i = 0; i < files.Length; ++i)
            {
                if (files[i].name.Equals(name))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 单个热更包
    /// </summary>
    [XmlType]
    public class Patch
    {
        [XmlAttribute]
        public string name;

        [XmlAttribute]
        public string url;

        [XmlAttribute]
        public string platform;

        public string getUrl()
        {
            //带http:// 或https://、ftp://的，直接用
            if (url.Contains("://") || url.Contains("://"))
            {
                return url;
            }
            return "";
        }

        [XmlAttribute]
        public string md5;
        [XmlAttribute]
        public float size;
    }
}

