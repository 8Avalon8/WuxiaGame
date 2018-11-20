using System;
using System.Text;
using HSFrameWork.Common;
namespace HSFrameWork.Scut.Inner
{
    /// <summary>
    /// 原名 PackageHead
    /// </summary>
    public class RecvPackageHead
    {
        public int ActionId { get; set; }
        public int MsgId { get; set; }

        public int ErrorCode { get; set; }
        public string Description { get; set; }
        public string StrTime { get; set; }
    }

    public interface IRecvHeadDecoder
    {
        bool TryParse(byte[] data, out RecvPackageHead head, out byte[] bodyBytes);
    }

    class DefaultRecvHeadDecoder : SingletonI<DefaultRecvHeadDecoder, IRecvHeadDecoder>, IRecvHeadDecoder
    {
        public bool TryParse(byte[] data, out RecvPackageHead head, out byte[] bodyBytes)
        {
            bodyBytes = null;
            head = null;
            int pos = 0;
            if (data == null || data.Length == 0)
            {
                return false;
            }

            int nStreamSize = GetInt(data, ref pos);

            if (nStreamSize != data.Length)
            {
                return false;
            }
            head = new RecvPackageHead();
            head.ErrorCode = GetInt(data, ref pos);
            head.MsgId = GetInt(data, ref pos);
            head.Description = GetString(data, ref pos);
            head.ActionId = GetInt(data, ref pos);
            head.StrTime = GetString(data, ref pos);
            int bodyLen = data.Length - pos;
            if (bodyLen > 0)
            {
                bodyBytes = new byte[bodyLen];
                Buffer.BlockCopy(data, pos, bodyBytes, 0, bodyLen);
            }
            else
            {
                bodyBytes = new byte[0];
            }
            return true;
        }

        private string GetString(byte[] data, ref int pos)
        {
            string val = string.Empty;
            int len = GetInt(data, ref pos);
            if (len > 0)
            {
                val = Encoding.UTF8.GetString(data, pos, len);
                pos += len;
            }
            return val;
        }

        private int GetInt(byte[] data, ref int pos)
        {
            int val = BitConverter.ToInt32(data, pos);
            pos += sizeof(int);
            return val;
        }
    }
}