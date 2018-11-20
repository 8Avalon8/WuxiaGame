using GLib;
using HSFrameWork.Common;
using HSFrameWork.Net;
using ProtoBuf;
using System;

namespace HSFrameWork.RoomService
{
    [ProtoContract]
    public class RoomEndPoint
    {
        ///<summary> 高位是KEY，低位是从0开始的ID。</summary>
        [ProtoMember(1)]
        public uint RoomKey;
        [ProtoMember(2)]
        public byte[] HSNetServerEndPoint;

        public RoomEndPoint() { }
        public RoomEndPoint(uint roomKey, byte[] serverEndPoint)
        {
            HSNetServerEndPoint = serverEndPoint;
            RoomKey = roomKey;
        }

        public static byte[] GetBytes(uint roomKey, byte[] serverEndPoint)
        {
            return DirectProtoBufTools.Serialize(new RoomEndPoint(roomKey, serverEndPoint));
        }

        public static byte[] GetBytes<T>(uint roomKey, T serverEndPoint) where T : class
        {
            return DirectProtoBufTools.Serialize(new RoomEndPoint(roomKey, DirectProtoBufTools.Serialize(serverEndPoint)));
        }

        public static byte[] GetBytes(string ip, ushort port, uint roomKey)
        {
            return GetBytes<SimpleServerEndPoint>(roomKey, new SimpleServerEndPoint(ip, port));
        }

        public static byte[] GetBytes(string connectStr)
        {
            string ip;
            ushort port;
            uint roomKey;
            Parse(connectStr, out ip, out port, out roomKey);
            return GetBytes(ip, port, roomKey);
        }

        public static void Parse(string connectStr, out string ip, out ushort port, out uint roomKey)
        {
            try
            {
                var args = connectStr.Split(':');
                ip = args[0];
                port = Convert.ToUInt16(args[1]);
                roomKey = Convert.ToUInt32(args[2]);
            }
            catch (Exception e)
            {
                throw new ArgumentException("connectStr 格式必须是 192.168.1.33:1001:121", e);
            }
        }

        public string ConnectString
        {
            get
            {
                SimpleServerEndPoint se = HSNetServerEndPoint.Deserialize<SimpleServerEndPoint>();
                return "{0}:{1}:{2}".f(se.InternetIP,se.InternetPort,RoomKey);
            }
            set
            {
                string ip;
                ushort port;
                Parse(value, out ip, out port, out RoomKey);
                HSNetServerEndPoint = new SimpleServerEndPoint(ip, port).Serialize();
            }
        }

        public static string ConvertToConnectString(byte[] data)
        {
            return data.Deserialize<RoomEndPoint>().ConnectString;
        }
    }

    public class RoomClientHandShakeData
    {
#if false
        public static SmartBuffer GetBytes(uint roomKey, byte[] data)
        {
            var sb = ArrayPool<byte>.Shared.CreateSBEx(4, data, 0, data.Length, false);
            BinOp.EncodeUInt(roomKey, sb.Data, 0);
            return sb;
        }
#endif
        private static byte[] EmptyBytes = new byte[0];
        public static byte[] GetBytes(uint roomKey, byte[] data)
        {
            if (data == null) data = EmptyBytes;

            byte[] ret = new byte[data.Length + 4];
            BinOp.EncodeUInt(roomKey, ret, 0);
            Array.Copy(data, 0, ret, 4, data.Length);
            return ret;
        }
    }
}
