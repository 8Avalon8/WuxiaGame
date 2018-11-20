using GLib;
using HSFrameWork.ConfigTable.Editor.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using HSFrameWork.ConfigTable;

namespace HSFrameWork.Common.Editor
{
    /// <summary>
    /// 目录打包工具类
    /// </summary>
    public static class FolderPackUtils
    {
        /// <summary>
        /// 将某个目录压缩加密。
        /// </summary>
        public static void PackZipAndEncryptFolder(bool updateOnly, string memo, string pattern, string folder, string dataFile, string lastSummaryFile, Func<string, string> fileKeyMapper, string ceDataFile)
        {
            var files = Directory.GetFiles(folder, pattern, SearchOption.AllDirectories);

            if (updateOnly && FileSummary.PackedFileValid(files, dataFile, lastSummaryFile))
            {
                HSUtils.Log("没有任何{0}文件更新，因此不用重新生成 [{1}]。".EatWithTID(memo, dataFile.ShortName()));
            }
            else
            {
                HSUtils.Log("{0}文件有更新，重新生成 [{1}]。".EatWithTID(memo, dataFile.ShortName()));
                Dictionary<string, byte[]> dataDict = new Dictionary<string, byte[]>();
                HSUtils.Log("Loading: [{0}] files，共[{1}]个...", memo, files.Length);
                foreach (var file in files)
                {
                    dataDict.Add(fileKeyMapper(file), File.ReadAllBytes(file));
                }

                ProtoBufTools.Serialize(dataDict, dataFile);
                FileSummary.WriteSummary(files, dataFile, lastSummaryFile);
                //dataFile=lastSummaryFile 都是当前时间
            }


            if (File.GetLastWriteTime(dataFile) == File.GetLastWriteTime(ceDataFile))
            {
                HSUtils.Log("[{0}] 没有更新，因此不用重新生成 [{1}]。".EatWithTID(dataFile.ShortName(), ceDataFile.ShortName()));
            }
            else
            {
                HSUtils.Log("[{0}] 有更新，因此重新生成 [{1}]。".EatWithTID(dataFile.ShortName(), ceDataFile.ShortName()));
                HSPackToolRaw.LZMA_3DES(dataFile, ceDataFile);
                dataFile.Touch(ceDataFile);
                //dataFile=LastSummaryFile=ceDataFile 都是当前时间
            }
        }
    }
}