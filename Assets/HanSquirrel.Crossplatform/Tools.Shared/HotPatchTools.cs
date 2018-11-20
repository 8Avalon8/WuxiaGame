using GLib;
using HSFrameWork.Common;
using System;
using System.IO;
using System.Linq;
using StrDict = System.Collections.Generic.Dictionary<string, string>;

namespace HanSquirrel.ResourceManager
{
    /// <summary>
    /// 读取热更内容的目录，并生成热更Patches对应的XML文件
    /// </summary>
    public class HotPatchTools
    {
        /// <summary>
        /// patchFolder：包含android和ios两个目录。两个目录下面分别是android和ios的热更内容。
        /// pubURLBase：热更的发布URL。
        /// 生成热更Patches对应的XML文件.
        /// </summary>
        public static void GenHotPatchIndexFile(string patchFolder, string pubURLBase)
        {
            new Impl().GenHotPatchIndexFile(patchFolder, pubURLBase);
        }

        private class Impl
        {
            private string _PatchFolder, _PubURLBase;
            public void GenHotPatchIndexFile(string patchFolder, string pubURLBase)
            {
                _PatchFolder = Path.GetFullPath(patchFolder);
                _PubURLBase = pubURLBase;
                HSUtils.Log("[{0}] 开始处理服务端HotPatch目录。发布根为 [{1}]", patchFolder, pubURLBase);

                try
                {
                    using (var csw = new StreamWriter(patchFolder.Sub("ios&android_patches.txt")))
                    {
                        csw.WriteLine(GetPatchesString(null));

                        PatchHandle("android", csw);
                        PatchHandle("ios", csw);

                        csw.Write("</patches>");
                    }
                    HSUtils.LogSuccess("[{0}] HotPatch目录处理完成。 ", patchFolder);
                }
                catch (Exception e)
                {
                    patchFolder.Sub("ios&android_patches.txt").Delete();
                    patchFolder.Sub("ios_patches.txt").Delete();
                    patchFolder.Sub("android_patches.txt").Delete();
                    HSUtils.LogException(e);
                    HSUtils.LogError("[{0}] 处理过程中出现异常，请截屏发给研发。", patchFolder);
                }
            }

            private string Version { get { return new DirectoryInfo(_PatchFolder).Name; } }
            private string GetPatchesString(double? size)
            {
                return "<patches v=\"{0}\" size=\"{1}\" kickofftype=\"optional\" content=\"\">"
                    .f(Version, size.HasValue ? size.Value.ToString() + "MB" : "");
            }

            private string PathRelativeToPubBase(FileInfo fileinfo)
            {
                var fullPath = Path.GetFullPath(fileinfo.FullName);
                int i = _PatchFolder.LastIndexOf("static\\");
                Mini.ThrowIfTrue(i == -1, "必须在static目录下面运行。");
                Mini.ThrowIfFalse(fullPath.Substring(0, i) == _PatchFolder.Substring(0, i), "目录结构不对");
                return fullPath.Substring(i + "static\\".Length).Split('\\').JoinC("/");
            }

            private string GetPatchString(string type, FileInfo file, StrDict md5Dict)
            {
                var md5 = MD5Utils.GetMD5WithFilePath(file.FullName).ToUpper();
                md5Dict.Add(file.Name.ToLower(), md5);
                var filesize = Math.Round(1.0 * file.Length / 1024, 2);
                var url = _PubURLBase + PathRelativeToPubBase(file);
                return "    <patch platform=\"{0}\" name=\"{1}\" url=\"{2}\" md5=\"{3}\" size=\"{4}\"/>"
                    .f(type, file.Name, url, md5, filesize);
            }

            private void PatchHandle(string type, StreamWriter combinedSW)
            {
                FileInfo[] files;
                var folder = new DirectoryInfo(_PatchFolder.Sub(type));
                if (!folder.Exists)
                {
                    files = new FileInfo[0];
                    HSUtils.LogWarning("没有 {0} 文件夹！".f(type));
                }
                else
                {
                    files = folder.GetFiles();
                    if (files.Length == 0)
                        HSUtils.LogWarning("{0} 文件夹没有文件！".f(type));
                }

                var md5Dict = new StrDict();
                using (var sw = new StreamWriter(_PatchFolder.Sub(type + "_patches.txt")))
                {
                    sw.WriteLine(GetPatchesString(Math.Round(1.0 * files.Sum(x => x.Length) / 1024 / 1024, 2)));

                    foreach (var file in files)
                    {
                        string patch = GetPatchString(type, file, md5Dict);
                        sw.WriteLine(patch);
                        combinedSW.WriteLine(patch);
                    }
                    sw.Write("</patches>");
                }

                _PatchFolder.Sub(type + "_patches.bin").WriteAllBytes(md5Dict.Serialize());
            }
        }
    }
}