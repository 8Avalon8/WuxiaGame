using GLib;
using System;
using System.IO;

namespace HSFrameWork.Common
{
    /// <summary>
    /// 不带文件头的压缩和加密工具类。
    /// </summary>
    public class HSPackToolRaw
    {
        /// <summary> 纯粹的压缩，不带文件头 </summary>
        public static void LZMARaw(string srcFile, string dstFile)
        {
            HSPackTool.ConvertFileBy(srcFile, dstFile, LZMARaw);
        }

        /// <summary> 纯粹的压缩，不带文件头 </summary>
        public static byte[] LZMARaw(byte[] inputData)
        {
            return HSPackTool.ConvertBytesBy(inputData, LZMARaw);
        }

        /// <summary> 纯粹的压缩，不带文件头 </summary>
        public static void LZMARaw(Stream input, Stream output)
        {
            SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();
            coder.WriteCoderProperties(output);
            output.Write(BitConverter.GetBytes(input.Length), 0, 8);
            coder.Code(input, output, input.Length, -1, null);
            output.Flush();
        }

        /// <summary> 纯粹的解压，不带文件头 </summary>
        public static void DeLZMARaw(string srcFile, string dstFile)
        {
            HSPackTool.ConvertFileBy(srcFile, dstFile, DeLZMARaw);
        }

        /// <summary> 纯粹的解压，不带文件头 </summary>
        public static byte[] DeLZMARaw(byte[] data)
        {
            return HSPackTool.ConvertBytesBy(data, DeLZMARaw);
        }

        /// <summary> 纯粹的解压，不带文件头 </summary>
        public static void DeLZMARaw(Stream input, Stream output)
        {
            // Read the decoder properties
            byte[] properties = new byte[5];
            input.Read(properties, 0, 5);

            // Read in the decompress file size.
            byte[] fileLengthBytes = new byte[8];
            input.Read(fileLengthBytes, 0, 8);
            long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

            // Decompress the file.
            SevenZip.Compression.LZMA.Decoder coder = new SevenZip.Compression.LZMA.Decoder();
            coder.SetDecoderProperties(properties);
            coder.Code(input, output, input.Length, fileLength, null);
            output.Flush();
        }

        /// <summary>
        /// LZMA>TDES，不带文件头
        /// </summary>
        public static void LZMA_3DES(string srcFile, string dstFile)
        {
            dstFile.WriteAllBytes(TDES.LocalInstance.Encrypt(LZMARaw(srcFile.ReadAllBytes())));
        }

        /// <summary>
        /// 不带文件头：解密解压 LZMA>TDES，线程池安全
        /// </summary>
        public static byte[] DE3DES_DELZMA_RAW(byte[] data)
        {
            return DeLZMARaw(TDES.LocalInstance.Decrypt(data));
        }
    }
}
