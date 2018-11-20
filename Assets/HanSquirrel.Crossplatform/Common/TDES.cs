using GLib;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HSFrameWork.Common
{
    /// <summary>
    /// 3DES工具类
    /// </summary>
    public class TDES
    {
        /// <summary>
        /// Scut使用的单例
        /// </summary>
        public static readonly TDES ScutInstance = new TDES();

        /// <summary>
        /// Local使用的单例
        /// </summary>
        public static readonly TDES LocalInstance = new TDES();

        /// <summary>
        /// 以KEY的MD5作为实际的KEY
        /// </summary>
        public void Init(string key)
        {
            using (MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider())
            {
                DESKey = provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
        }

        public byte[] Encrypt(byte[] data, int offset, int length)
        {
            Mini.ThrowNullIf(DESKey, "程序编写错误：TDESG has not been inited!");

            using (TripleDESCryptoServiceProvider provider = new TripleDESCryptoServiceProvider
            {
                Key = DESKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            })
            using (var x = provider.CreateEncryptor())
                return x.TransformFinalBlock(data, offset, length);
        }

        public byte[] Encrypt(byte[] data)
        {
            return Encrypt(data, 0, data.Length);
        }

        public byte[] Decrypt(byte[] Message)
        {
            return Decrypt(Message, 0, Message.Length);
        }

        public byte[] Decrypt(byte[] Message, int offset)
        {
            return Decrypt(Message, offset, Message.Length - offset);
        }

        public byte[] Decrypt(byte[] Message, int offset, int length)
        {
            Mini.ThrowNullIf(DESKey, "程序编写错误：TDESG has not been inited!");

            using (TripleDESCryptoServiceProvider provider = new TripleDESCryptoServiceProvider
            {
                Key = DESKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            })
            using (var x = provider.CreateDecryptor())
            {
                return x.TransformFinalBlock(Message, offset, length);
            }
        }

        /// <summary>
        /// 加密文件srcFilePath为dstFilePath
        /// </summary>
        public void EncryptRAW(string srcFilePath, string dstFilePath)
        {
            Mini.ThrowNullIf(DESKey, "程序编写错误：TDES has not been inited!");

            using (FileStream output = File.Open(dstFilePath, FileMode.Create))
            using (FileStream input = File.OpenRead(srcFilePath))
            using (TripleDESCryptoServiceProvider provider = new TripleDESCryptoServiceProvider
            {
                Key = DESKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            })
            using (var encryptor = provider.CreateEncryptor())
            using (CryptoStream cs = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                input.DumpTo(cs);
        }

        /// <summary>
        /// 解密文件srcFilePath为dstFilePath
        /// </summary>
        public void DecryptRAW(string srcFilePath, string dstFilePath)
        {
            Mini.ThrowNullIf(DESKey, "程序编写错误：TDES has not been inited!");

            using (FileStream output = File.Open(dstFilePath, FileMode.Create))
            using (FileStream input = File.OpenRead(srcFilePath))
            using (TripleDESCryptoServiceProvider provider = new TripleDESCryptoServiceProvider
            {
                Key = DESKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            })
            using (var encryptor = provider.CreateDecryptor())
            using (CryptoStream cs = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                input.DumpTo(cs);
        }

        private byte[] DESKey;
    }
}
