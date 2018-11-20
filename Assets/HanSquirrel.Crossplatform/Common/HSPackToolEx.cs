using System;
using System.IO;
using GLib;
using HSFrameWork.Common.Inner;

namespace HSFrameWork.Common
{
    /// <summary>
    /// 带文件头的数据压缩加密工具。先压缩后加密。
    /// </summary>
    public static class HSPackToolEx
    {
        public static byte[] AutoDeFile(byte[] data)
        {
            switch (HSPackTool.TryReadFileFormat(data))
            {
                case HSFileFormat.LEGENCY:
                    using (HSUtils.ExeTimerEnd("加载Values-AutoDeFile-传统格式"))
                        return HSPackToolRaw.DE3DES_DELZMA_RAW(data);
                case HSFileFormat.EASYDES:
                    byte[] newData;
                    using (HSUtils.ExeTimerEnd("加载Values-AutoDeFile-文件头格式-EasyDes"))
                        newData = HSPackTool.DeEasyDes(data);
                    switch (HSPackTool.TryReadFileFormat(newData))
                    {
                        case HSFileFormat.IONICZIP:
                            using (HSUtils.ExeTimerEnd("加载Values-AutoDeFile-文件头格式-HSIZip"))
                                return HSPackTool.DeIonicZip(newData);
                        case HSFileFormat.HSLZMA:
                            using (HSUtils.ExeTimerEnd("加载Values-AutoDeFile-文件头格式-HSLZMA"))
                                return HSPackTool.DeLZMA(newData);
                    }
                    break;
            }

            HSUtils.LogError("values文件格式错误");
            return null;
        }

        /// <summary>
        /// 带文件头的：IonicZip > Easy Des，线程池安全
        /// </summary>
        public static void IonicZip_EasyDes(string src, string dst)
        {
            using (HSUtils.ExeTimerEnd("IonicZip_EasyDes: [{0}]=>[{1}]".Eat(src, dst)))
            {
                string tmpFile = Path.GetTempFileName();
                HSPackTool.IonicZip(src, tmpFile);
                HSPackTool.EasyDes(tmpFile, dst);
                File.Delete(tmpFile);
            }
        }

        /// <summary>
        /// 带文件头的：IonicZip > Easy Des，线程池安全
        /// </summary>
        public static void DeEasyDes_DeIonicZip(string src, string dst)
        {
            using (HSUtils.ExeTimerEnd("DeEasyDes_DeIonicZip: [{0}]=>[{1}]".Eat(src, dst)))
            {
                string tmpFile = Path.GetTempFileName();
                HSPackTool.DeEasyDes(src, tmpFile);
                HSPackTool.DeIonicZip(tmpFile, dst);
                File.Delete(tmpFile);
            }
        }

        /// <summary>
        /// 带文件头的：IonicZip > Easy Des，线程池安全
        /// </summary>
        public static void HSLZMA_EasyDes(string src, string dst)
        {
            using (HSUtils.ExeTimerEnd("HSLZMA_EasyDes: [{0}]=>[{1}]".Eat(src, dst)))
            {
                string tmpFile = Path.GetTempFileName();
                HSPackTool.LZMA(src, tmpFile);
                HSPackTool.EasyDes(tmpFile, dst);
                File.Delete(tmpFile);
            }
        }

        /// <summary>
        /// 带文件头的：IonicZip > Easy Des，线程池安全
        /// </summary>
        public static void DeHSLZMA_DeEasyDes(string src, string dst)
        {
            using (HSUtils.ExeTimerEnd("DeEasyDes_DeIonicZip: [{0}]=>[{1}]".Eat(src, dst)))
            {
                string tmpFile = Path.GetTempFileName();
                HSPackTool.DeEasyDes(src, tmpFile);
                HSPackTool.DeLZMA(tmpFile, dst);
                File.Delete(tmpFile);
            }
        }
    }
}
