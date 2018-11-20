using GLib;
using HSFrameWork.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace HSFrameWork.Scut
{
    public class NetWriter
    {
        public const string URLEncodeContainerKey = "HSFrameWork.Scut.NetWriter.URLEncode";

        private string s_strSessionID = "";
        private string s_strSt = "";
        private string s_strPostData = "";
        private string s_strUserData = "";
        /// <summary>
        /// 缺省从1开始，因为0代表从服务端主动推送的MSG
        /// </summary>
        private static int s_Counter = 0;

        public readonly int MsgId;

        private static void CheckKey(string key)
        {
#if UNITY_EDITOR
            if (_ReservedKey.Contains(key.ToLower()))
            {
                throw new Exception("Action开发者编程错误：不能使用内置的名称 [{0}]".f(key));
            }
#endif
        }

        private static readonly HashSet<string> _ReservedKey = new HashSet<string>() { "msgid", "sid", "st", "devflag" };

        public NetWriter()
        {
            MsgId = Interlocked.Increment(ref s_Counter);
            s_strUserData = string.Format("MsgId={0}&Sid={1}&St={2}&devflag={3}", MsgId, s_strSessionID, s_strSt, _SocketOpts.DevFlagToServer);
        }

        public static Action<string, object> OnWriteAction;

        public void writeInt32(string szKey, int nValue)
        {
            CheckKey(szKey);
            if (OnWriteAction != null)
                OnWriteAction(szKey, nValue);
            s_strUserData += string.Format("&{0}={1}", szKey, nValue);
        }

        public void writeString(string szKey, string szValue)//
        {
            CheckKey(szKey);
            if (szValue == null)
            {
                return;
            }

            if (OnWriteAction != null)
                OnWriteAction(szKey, szValue);

            s_strUserData += string.Format("&{0}=", szKey);
            s_strUserData += URLEncode(szValue);
        }

        private byte[] _ProtobufBytes;
        public void WriteBytes(byte[] data)
        {
            if (_ProtobufBytes != null)
                throw new Exception("WriteBytes只能用一次");
            _ProtobufBytes = data;
        }


        public static string getMd5String(string input)
        {
            return MD5Utils.Encrypt(Encoding.Default.GetBytes(input));
        }

        public readonly static byte[] EnterChar = Encoding.UTF8.GetBytes("\r\n\r\n");

        public byte[] PostData(bool needLength = true)
        {
            byte[] data;
            s_strPostData = "?d=";
            string str = s_strUserData + "&sign=" + getMd5String(s_strUserData);
            s_strPostData += URLEncode(str);
            data = Encoding.ASCII.GetBytes(s_strPostData);

            if (_ProtobufBytes != null)
            {   //_ProtobufBytes可以是byte[0]
                var data1 = new byte[data.Length + EnterChar.Length + _ProtobufBytes.Length];
                Buffer.BlockCopy(data, 0, data1, 0, data.Length);
                Buffer.BlockCopy(EnterChar, 0, data1, data.Length, EnterChar.Length);
                Buffer.BlockCopy(_ProtobufBytes, 0, data1, data.Length + EnterChar.Length, _ProtobufBytes.Length);
                data = data1;
            }

            if (!needLength) return data;

            //加包长度，拆包时使用
            byte[] len = BitConverter.GetBytes(data.Length);
            byte[] sendBytes = new byte[data.Length + len.Length];
            Buffer.BlockCopy(len, 0, sendBytes, 0, len.Length);
            Buffer.BlockCopy(data, 0, sendBytes, len.Length, data.Length);
            return sendBytes;
        }

        public static byte[] BuildHearbeatPackage(out int msgId)
        {
            NetWriter writer = new NetWriter();
            msgId = writer.MsgId;
            writer.writeInt32("actionId", 1);
            return writer.PostData();
        }

        private static Func<string, string> URLEncode = Container.Resolve<Func<string, string>>(URLEncodeContainerKey);
        private static IScutSocketOptions _SocketOpts = Container.Resolve<IScutSocketOptions>();
    }
}
