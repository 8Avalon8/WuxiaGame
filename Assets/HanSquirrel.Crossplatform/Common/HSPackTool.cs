using System;
using System.IO;
using System.Linq;
using GLib;
using System.Text;
using Ionic.Zlib;

namespace HSFrameWork.Common.Inner
{
    public enum HSFileFormat
    {
        LEGENCY,
        EASYDES,
        IONICZIP,
        HSLZMA
    }
}

namespace HSFrameWork.Common
{
    using Inner;

    /// <summary>
    /// 带文件头的数据压缩加密工具类，仅仅支持单个算法。
    /// </summary>
    public static class HSPackTool
    {
        #region 快捷函数
        public static void EasyDes(string input, string output)
        {
            ConvertFileBy(input, output, EasyDes);
        }

        public static byte[] EasyDes(byte[] src)
        {
            return ConvertBytesBy(src, EasyDes);
        }

        public static void DeEasyDes(string input, string output)
        {
            ConvertFileBy(input, output, DeEasyDes);
        }

        public static byte[] DeEasyDes(byte[] src)
        {
            return ConvertBytesBy(src, DeEasyDes);
        }

        public static byte[] IonicZip(byte[] src)
        {
            return ConvertBytesBy(src, IonicZIP);
        }

        public static void IonicZip(string src, string dst)
        {
            ConvertFileBy(src, dst, IonicZIP);
        }

        public static void DeIonicZip(string src, string dst)
        {
            ConvertFileBy(src, dst, DeIonicZip);
        }

        public static byte[] DeIonicZip(byte[] zipped)
        {
            return ConvertBytesBy(zipped, DeIonicZip);
        }


        public static byte[] LZMA(byte[] src)
        {
            return ConvertBytesBy(src, LZMA);
        }

        public static void LZMA(string src, string dst)
        {
            ConvertFileBy(src, dst, LZMA);
        }

        public static void DeLZMA(string src, string dst)
        {
            ConvertFileBy(src, dst, DeLZMA);
        }

        public static byte[] DeLZMA(byte[] data)
        {
            return ConvertBytesBy(data, DeLZMA);
        }
        #endregion

        #region 文件头
        public static HSFileFormat TryReadFileFormat(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var br = new BinaryReader(input))
                return TryReadFileFormat(input, br);
        }

        public static HSFileFormat TryReadFileFormat(Stream input, BinaryReader br)
        {
            long orgPos = input.Position;

            byte[] head = new byte[HS_FILE_HEAD.Length];
            if (HS_FILE_HEAD.Length != br.Read(head, 0, HS_FILE_HEAD.Length) || !head.SequenceEqual(HS_FILE_HEAD))
            {
                input.Position = orgPos;
                return HSFileFormat.LEGENCY;
            }

            string format = br.ReadString();
            if (format == HS_EASYDES_HEAD)
            {
                return HSFileFormat.EASYDES;
            }
            if (format == HS_IonicZIP_HEAD)
            {
                return HSFileFormat.IONICZIP;
            }
            if (format == HS_LZMA_HEAD)
            {
                return HSFileFormat.HSLZMA;
            }

            input.Position = orgPos;
            throw new Exception("文件损坏或者程序编写错误，无法识别文件头。");
        }

        private static void WriteFileFormat(BinaryWriter bw, HSFileFormat format)
        {
            if (format == HSFileFormat.LEGENCY)
                return;

            bw.Write(HS_FILE_HEAD, 0, HS_FILE_HEAD.Length);

            switch (format)
            {
                case HSFileFormat.EASYDES:
                    bw.Write(HS_EASYDES_HEAD);
                    break;
                case HSFileFormat.IONICZIP:
                    bw.Write(HS_IonicZIP_HEAD);
                    break;
                case HSFileFormat.HSLZMA:
                    bw.Write(HS_LZMA_HEAD);
                    break;
            }
        }



        private static readonly byte[] HS_FILE_HEAD = Encoding.ASCII.GetBytes("HanFramework.HSConfigTable.FileFormat");
        private static readonly string HS_EASYDES_HEAD = "EasyDes";
        private static readonly string HS_IonicZIP_HEAD = "IonicZip";
        private static readonly string HS_LZMA_HEAD = "LZMA";
        private static readonly int EASY_DES_BLOCK_SIZE = 8 * 1024;
        #endregion

        #region EasyDes
        public static void EasyDes(Stream input, Stream output)
        {
            using (var br = new BinaryReader(input))
            using (var bw = new BinaryWriter(output))
            {
                WriteFileFormat(bw, HSFileFormat.EASYDES);
                byte[] buffer = new byte[EASY_DES_BLOCK_SIZE];

                int length = br.Read(buffer, 0, EASY_DES_BLOCK_SIZE); //文件可能比8K要小
                byte[] xbyte = TDES.LocalInstance.Encrypt(buffer, 0, length);

                bw.Write(xbyte.Length);
                bw.Write(xbyte, 0, xbyte.Length);

                br.DumpTo(bw);
            }
        }

        public static void DeEasyDes(Stream input, Stream output)
        {
            using (var br = new BinaryReader(input))
            {
                if (TryReadFileFormat(input, br) != HSFileFormat.EASYDES)
                    throw new Exception("程序编写错误：该文件不是EasyDes文件。");

                using (var bw = new BinaryWriter(output))
                {
                    byte[] deData = TDES.LocalInstance.Decrypt(br.ReadBytes(br.ReadInt32()));

                    bw.Write(deData, 0, deData.Length);
                    br.DumpTo(bw);
                }
            }
        }
        #endregion

        #region IonicZip
        public static void IonicZIP(Stream input, Stream output)
        {
            using (var bw = new BinaryWriter(output))
            {
                WriteFileFormat(bw, HSFileFormat.IONICZIP);
                bw.Flush();
                using (var zipstream = new ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestCompression))
                    input.DumpTo(zipstream);
            }
        }


        public static void DeIonicZip(Stream input, Stream output)
        {
            using (var br = new BinaryReader(input))
            {
                if (TryReadFileFormat(input, br) != HSFileFormat.IONICZIP)
                    throw new Exception("程序编写错误：该文件不是IONICZIP文件。");

                using (var zipstream = new ZlibStream(input, CompressionMode.Decompress))
                    zipstream.DumpTo(output);
            }
        }
        #endregion

        #region LZMA

        public static void LZMA(Stream input, Stream output)
        {
            using (var bw = new BinaryWriter(output))
            {
                WriteFileFormat(bw, HSFileFormat.HSLZMA);
                bw.Flush();

                SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();
                coder.WriteCoderProperties(output);
                output.Write(BitConverter.GetBytes(input.Length), 0, 8);
                coder.Code(input, output, input.Length, -1, null);
                output.Flush();
            }
        }

        public static void DeLZMA(Stream input, Stream output)
        {
            using (var br = new BinaryReader(input))
            {
                if (TryReadFileFormat(input, br) != HSFileFormat.HSLZMA)
                    throw new Exception("程序编写错误：该文件不是HSLZMA文件。");
                HSPackToolRaw.DeLZMARaw(input, output);
            }
        }

        #endregion

        #region 辅助函数
        public static void ConvertFileBy(string src, string dst, Action<Stream, Stream> converter)
        {
            using (var input = File.OpenRead(src))
            using (var output = File.Open(dst, FileMode.Create))
                converter(input, output);
        }

        public static byte[] ConvertBytesBy(byte[] data, Action<Stream, Stream> converter)
        {
            using (var input = new MemoryStream(data))
            using (var output = new MemoryStream())
            {
                converter(input, output);
                output.Close();
                return output.ToArray();
            }

        }

        #endregion
    }
}
