using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using GLib;

namespace HSFrameWork.Common
{
    /// <summary>
    /// MD5工具类
    /// </summary>
    public abstract class MD5Utils
    {
        /// <summary>
        /// 计算出buffer的MD5，小写。
        /// </summary>
        public static string Encrypt(byte[] buffer)
        {
            Mini.ThrowNullIf(buffer, "Encrypt(buffer)，buffer不应该为null");
            using (MD5 alg = new MD5CryptoServiceProvider())
            {
                byte[] data = alg.ComputeHash(buffer);
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }

        /// <summary>
        /// 将str变为UTF8的byte数组，然后计算出其MD5，小写。
        /// </summary>
        public static string Encrypt(string str)
        {
            Mini.ThrowNullIf(str, "Encrypt(str)，str不应该为null");
            return Encrypt(System.Text.Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// 计算文件的MD5，小写
        /// </summary>
        public static string GetMD5WithFilePath(string filePath)
        {
            try
            {
                using (FileStream file = new FileStream(filePath, FileMode.Open))
                using (MD5 md5 = new MD5CryptoServiceProvider())
                {
                    byte[] retVal = md5.ComputeHash(file);

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < retVal.Length; i++)
                    {
                        sb.Append(retVal[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail.", ex);
            }
        }
    }
}
