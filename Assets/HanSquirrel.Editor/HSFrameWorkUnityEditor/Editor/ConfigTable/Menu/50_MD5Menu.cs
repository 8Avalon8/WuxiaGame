using UnityEditor;
using System.IO;
using GLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HSFrameWork.Common;

namespace HSFrameWork.ConfigTable.Editor
{
    /// <summary>
    /// MD5相关菜单
    /// </summary>
    public class MD5Menu
    {
        /// <summary>
        /// 生成StreamingAssetsPath目录下的所有（不包括.meta和.manifest）文件的MD5码，并存储在file里面。线程池安全.
        /// </summary>
        public static void GenerateStreamingAssetsMD5Summary()
        {
            //之前版本会根据MD5文件日期来判断是需要重新生成，然而可能会有错误。
            //因此今后每次必须强制生成MD5文件！因为MD5是命脉所在，必须保持最新。GG 20181009 
            using (HSUtils.ExeTimer("生成StreamingAsset的MD5文件"))
            using (var tr = File.CreateText(HSCTC.Md5File))
                new DirectoryInfo(HSCTC.StreamingAssetsPath)
                    .GetFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(x => !(x.Name.EndsWith(".meta") || x.Name.EndsWith(".manifest")))
                    .ForEachG(
                        x => tr.WriteLine(x.Name + "/" + MD5Utils.GetMD5WithFilePath(x.FullName)));

        }

        /// <summary>
        /// 将当前打包的MD5文件根据平台拷贝到 ABMD5/AndroidABMD5.txt 或者 IOSABMD5.txt 或者 WIN.txt
        /// 发布完整版本时调用
        /// </summary>
        [MenuItem("Build/拷贝当前MD5到ABMD5目录")]
        public static void CopyMD5ToPlatformByBuildTarget()
        {
            //每次必须强制生成MD5文件！因为MD5必须保持最新。GG 20181009
            GenerateStreamingAssetsMD5Summary();

            Debug.LogFormat("清空HotFix目录：{0}", HSCTC.HotFixFolder);
            HSCTC.HotFixFolder.ClearDirectory();
            File.Copy(HSCTC.Md5File, HSCTC.Md5FileByPlatform, true);
            Debug.LogFormat("Copy {0} > {1} 完成。", HSCTC.Md5File, HSCTC.Md5FileByPlatform);
        }

        /// <summary>
        /// 用当前MD5文件和ABMD5/*ABMD5比较，拷贝更新的AB包到HotFix目录。
        /// 发布热更时调用。
        /// </summary>
        [MenuItem("Build/拷贝新AB包到[ABMD5-HotFix]")]
        public static void CopyABToHotFixFolder()
        {
            //每次必须强制生成MD5文件！因为MD5必须保持最新。GG 20181009
            GenerateStreamingAssetsMD5Summary();

            Debug.LogFormat("清空HotFix目录：{0}", HSCTC.HotFixFolder);
            HSCTC.HotFixFolder.ClearDirectory();

            var baseMd5 = ReadMd5(HSCTC.Md5FileByPlatform);

            int c = 0;
            ReadMd5(HSCTC.Md5File)
                .Where(kv => !baseMd5.ContainsKey(kv.Key) || baseMd5[kv.Key] != kv.Value)
                .Select(kv => kv.Key)
                .ForEachG(x =>
                {
                    c++;
                    Debug.LogFormat("Copy {0}", x);
                    HSCTC.StreamingAssetsPath.Sub(x).CopyFileTo(HSCTC.HotFixFolder.Sub(x));
                });
            Debug.LogFormat("拷贝 [{0}] 个AB包到HotFix完成。", c);
        }

        /// <summary>
        /// 将MD5文件读取为字典
        /// </summary>
        public static Dictionary<string, string> ReadMd5(string path)
        {
            return path.ReadAllLines().Select(x => x.Split('/')).ToDictionary(x => x[0], x => x[1]);
        }
    }
}
