using System.IO;
using GLib;
using System.Text;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    /// <summary>
    /// 跨平台。放置了相关的压缩加密和MD5函数，无状态独立。
    /// </summary>
    public static class FileSummary
    {
        public static string GetSummary(string[] files)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var file in files.SortC())
            {
                sb.AppendLine(file);
                sb.AppendLine(File.GetLastWriteTime(file).Ticks.ToString());
                sb.AppendLine(new FileInfo(file).Length.ToString());
            }

            return sb.ToString();
        }

        public static bool UnChanged(string[] files, string sumFile)
        {
            try
            {
                return GetSummary(files) == File.ReadAllText(sumFile);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// sumFile日期和上次成功编译的packFile日期相同。sumFile内容记录了所有 [files] 的日期和大小。
        /// 因此如果files有任何变化，packFile有任何变化，都会失效。
        /// </summary>
        public static bool PackedFileValid(string[] files, string packFile, string sumFile)
        {
            return File.Exists(packFile) && //目标文件存在
                File.GetLastWriteTime(packFile) == File.GetLastWriteTime(sumFile) && //和上次编译的总结文件日期相同
                UnChanged(files,sumFile); //所有文件日期大小都没有改变
        }

        /// <summary>
        /// sumFile日期和packFile日期相同。sumFile内容记录了所有file的日期和大小。
        /// </summary>
        public static void WriteSummary(string[] files, string packFile, string sumFile)
        {
            File.WriteAllText(sumFile, GetSummary(files));
            packFile.Touch(sumFile);
        }
    }
}