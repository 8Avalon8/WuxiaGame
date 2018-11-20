using System;
using HSFrameWork.Common;
using GLib;
using HSFrameWork.ConfigTable;

namespace HanSquirrel.ResourceManager
{
    /// <summary>
    /// 根据资源KEY来加载二进制，可以从StreamingAsset下加载纯粹文件。
    /// 这些加载会优先从HotPath中取。
    /// resourceKey必须以"Assets/"开头。
    /// 如"Assets/BuildSource/test.txt"
    /// 如"Assets/BuildSource/MyFolder/test.bin"
    /// 如"Assets/StreamingAssets/test.txt"
    /// 如"Assets/StreamingAssets/MyFolder/test.txt"
    /// </summary>
    public class BinaryResourceLoader
    {
        /// <summary>
        /// errorLog主要用于单元测试环节。在此环节只要调用了Unity.Debug.LogError，测试就会被Unity认为失败。
        /// </summary>
        public static byte[] LoadBinary(string resourceKey, bool errorLog = true)
        {
            if (IsRawDataEx(ref resourceKey))
            {
                var patchFile = HotPatch.TryGetFilePathInPatch(resourceKey);
                if (patchFile != null)
                {
                    _Logger.Trace("Loading [{0}] from HotPatch.", resourceKey);
                    return patchFile.ReadAllBytes();
                }
                else
                {
                    _Logger.Trace("Loading [{0}] from StreamingAssets.", resourceKey);
                    try
                    {
                        return BetterStreamingAssets.ReadAllBytes(resourceKey);
                    }
                    catch (Exception e)
                    {
                        if (errorLog)
                            _Logger.Error(e, "加载[{0}]失败", resourceKey);
                        else
                            _Logger.Trace("加载[{0}]失败 : {1}", resourceKey, e.ToString());
                        return null;
                    }
                }
            }
            else
            {
                return ResourceLoader.LoadBinary(resourceKey);
            }
        }

        /// <summary>（无文件头） 加载》解密（3Des）》解压（LZMA） </summary>
        public static byte[] LoadDeDESDeLZMA(string resourceKey)
        {
            return HSPackToolRaw.DeLZMARaw(TDES.LocalInstance.Decrypt(LoadBinary(resourceKey)));
        }

        /// <summary>（无文件头） 加载》解密（3Des）</summary>
        public static byte[] LoadDeDES(string resourceKey)
        {
            return TDES.LocalInstance.Decrypt(LoadBinary(resourceKey));
        }

        /// <summary> （无文件头）加载》解压（LZMA）</summary>
        public static byte[] LoadDeLZMA(string resourceKey)
        {
            var bytes = LoadBinary(resourceKey);
            if (bytes == null)
                return null;
            return HSPackToolRaw.DeLZMARaw(bytes);
        }

        /// <summary> 加载》解密》解压 (自动判断是否有文件头)</summary>
        public static byte[] LoadCEBinary(string resourceKey)
        {
            Mini.ThrowIfFalse(IsRawData(resourceKey), "LoadCEBinary: resourceKey必须以[Assets/StreamingAssets]开头");
            var ret = HSPackToolEx.AutoDeFile(LoadBinary(resourceKey));
            if (ret == null)
                throw new Exception("[{0}]解密加载失败".f(resourceKey));
            return ret;
        }


#if UNITY_EDITOR

        /// <summary> 压缩（LZMA）》加密（3DES）》存储 （无文件头）</summary>
        public static void LZMADESSave(string srcFile, string resourceKey)
        {
            resourceKey.WriteAllBytes(TDES.LocalInstance.Encrypt(HSPackToolRaw.LZMARaw(srcFile.ReadAllBytes())));
        }

        /// <summary> 压缩（LZMA）》加密（3DES）》存储 （无文件头）</summary>
        public static void LZMADESSave(byte[] data, string resourceKey)
        {
            resourceKey.WriteAllBytes(TDES.LocalInstance.Encrypt(HSPackToolRaw.LZMARaw(data)));
        }

        /// <summary> 加密（3DES）》存储 （无文件头）</summary>
        public static void DESSave(byte[] data, string resourceKey)
        {
            resourceKey.WriteAllBytes(TDES.LocalInstance.Encrypt(data));
        }

        /// <summary> 压缩（LZMA）》存储 （无文件头）</summary>
        public static void LZMASave(byte[] data, string resourceKey)
        {
            resourceKey.WriteAllBytes(HSPackToolRaw.LZMARaw(data));
        }

        /// <summary>
        /// 将二进制文件压缩加密(带HS私有文件头:ZIP-EasyDes)，存储到resourceKey对应的路径中。
        /// </summary>
        public static void SaveCEBinary(string srcFile, string resourceKey)
        {
            Mini.ThrowIfFalse(IsRawData(resourceKey), "ZipBinaryToStreamingAssets: resourceKey必须以[Assets/StreamingAssets]开头");
            HSPackToolEx.IonicZip_EasyDes(srcFile, resourceKey);
        }
#endif

        private const string _StreamingAssetsPath = "assets/streamingassets/";
        private static bool IsRawData(string resourceKey)
        {
            return resourceKey.ToLower().StartsWith(_StreamingAssetsPath);
        }

        private static bool IsRawDataEx(ref string resourceKey)
        {
            if (resourceKey.ToLower().StartsWith(_StreamingAssetsPath))
            {
                resourceKey = resourceKey.Substring(_StreamingAssetsPath.Length);
                return true;
            }
            else
                return false;
        }
        private static IHSLogger _Logger = HSLogManager.GetLogger("ABM");
    }
}