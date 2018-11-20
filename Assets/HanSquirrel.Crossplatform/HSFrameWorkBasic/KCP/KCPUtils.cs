using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using HSFrameWork.Common;
using HSFrameWork.Common.Inner;
using GLib;
using Ionic.Zlib;
using System.IO;
using System.Collections.Generic;
using HSFrameWork.Net;
#if HSFRAMEWORK_NET_ABOVE_4_5
using System.Threading.Tasks;
#endif

namespace HSFrameWork.KCP.Common
{
    public enum KCPDataDirection
    {
        SEND,
        RECEIVE,
        SIGNAL,
    }

    public static class KCPSigals
    {
        public static SmartBufferEx<KCPDataDirection> SuspendFlushSignal = new SmartBufferEx<KCPDataDirection>(KCPDataDirection.SIGNAL);
        public static SmartBufferEx<KCPDataDirection> ResumeFlushSignal = new SmartBufferEx<KCPDataDirection>(KCPDataDirection.SIGNAL);
        public static SmartBufferEx<KCPDataDirection> ReceiveFailSignal = new SmartBufferEx<KCPDataDirection>(KCPDataDirection.SIGNAL);
        public static SmartBufferEx<KCPDataDirection> PoliteCloseSignal = new SmartBufferEx<KCPDataDirection>(KCPDataDirection.SIGNAL);
        public static SmartBufferEx<KCPDataDirection> RuteCloseSignal = new SmartBufferEx<KCPDataDirection>(KCPDataDirection.SIGNAL);
    }

    public enum UDPDataTypeTag : byte
    {
        NONE,
        KCP,
    }

    public enum KCPUpDataTypeTag : byte
    {
        NONE,
        HANDSHAKE1, //C>S
        HANDSHAKE2, //C<S
        HANDSHAKE3, //C>S
        CLOSE, //C<>S
        APP, //C<>S
        APPZIPPED, //C<>S
        MAX,
    }

    /// <summary>
    ///  LEVEL3 APPDATA       [APPDATA] 
    ///                       ------PlayerLink--------
    ///  LEVEL2 KCPUPDATA     [KCPUPDATATYPE][APPSEQ][APPDATA]   
    ///                       ------KCPLib------------
    ///  LEVEL1 KCPDOWNDATA： [KCPHEAD][KCPUPDATA]
    ///                       ------PlayerLinkImpl--------
    ///  LEVEL0 UDPDATA：     [VERSION][KEY][CRC][UDPDATATYPE][REMOTESESSIONID][LOCALSESSIONID][KCPDATA]
    ///                       ------UDPClient-------------
    /// APP《APPDATA》Upper 《APPDATAPacked》KCP《KCPDATA》KCPPlayerLinkDown《UDPDATA》UDPClient 
    /// </summary>
    public static class DataPackUtils
    {
        public static bool TryUnpackHandShakeData(byte[] data, int offset, int size,
            out uint displayName, out bool traceMe, out int sendlogMaxSize,
            out int appHSOffset, out int appHSSize)
        {
            if (size < 9)
            {
                displayName = 0;
                traceMe = false;
                sendlogMaxSize = 0;
                appHSOffset = 0;
                appHSSize = 0;
                return false;
            }

            displayName = BinOp.DecodeUInt(data, offset);
            sendlogMaxSize = (int)BinOp.DecodeUInt(data, offset + 4);
            traceMe = data[offset + 8] == 1;
            appHSOffset = offset + 9;
            appHSSize = size - 9;
            return true;
        }

        public static byte[] PackHandShakeData(byte[] appHandShakeData, uint displayName, bool traceMe, int sendlogMaxSize)
        {
            appHandShakeData = appHandShakeData == null ? Mini.EmptyBytes : appHandShakeData;
            //[DispalyName4+sendlogsize4+traceMe1]
            var ret = new byte[9 + appHandShakeData.Length];
            BinOp.EncodeUInt(displayName, ret, 0);
            BinOp.EncodeUInt((uint)sendlogMaxSize, ret, 4);
            ret[8] = traceMe ? (byte)1 : (byte)0;
            Array.Copy(appHandShakeData, 0, ret, 9, appHandShakeData.Length);
            return ret;
        }
        public static SmartBufferEx<KCPDataDirection> GetCloseSB(ushort seq)
        {
            return KCPLibEx.SharedBytePool.ConvertAppData2KCPUpData(KCPUpDataTypeTag.CLOSE, DataPackUtils.DummyBuff, 0, DataPackUtils.DummyBuff.Length, false, null, seq);
        }

        public delegate void LogDelegate(string format, params object[] args);

#if HSFRAMEWORK_NET_ABOVE_4_5
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        public static ushort TraceKCPDownBuffer(string dir, byte[] buf, int offset, int size, string left, string right, string stage, string pre, uint pk, string seqPre, LogDelegate logFunc)
        {
            KCPUpDataTypeTag kcpUpDataType;
            int appDataOffset;
            int appDataLength;

            if (TryUnPackKCPDownData2AppData(buf, offset, size, out kcpUpDataType, out appDataOffset, out appDataLength))
            {
                logFunc("{0} {1} {2}{3}{4}{5}[{6:D3}/{7:D3} app{8}{9}-{10} {11} {12}]{13}", left.Visible() ? left : "     ", dir, //0,1
                    right.Visible() ? (right + stage).PadRight(7) : "       ", //2
                    "", pre, pk,  //3,4,5
                    appDataLength, size, //6,7
                    seqPre,//8
                    buf.ToHex(appDataOffset - APP_DATA_SEQ_SIZE, APP_DATA_SEQ_SIZE),//9 [APPSEQ]
                    buf.ToHex(appDataOffset, Math.Min(256, appDataLength)), //[APPDATA] 10
                    buf.ToHex(appDataOffset - APP_DATA_SEQ_SIZE - KCPUPDATATYPE_SIZE, KCPUPDATATYPE_SIZE), //[KCPUPDATATYPE] 11
                    buf.ToHex(offset, DataPackUtils.KCPDOWNDATA_MIN_SIZE), //[KCPHEAD] 12
                    kcpUpDataType == KCPUpDataTypeTag.HANDSHAKE1 ||
                    kcpUpDataType == KCPUpDataTypeTag.HANDSHAKE2 ||
                    kcpUpDataType == KCPUpDataTypeTag.HANDSHAKE3 ? "HS" : ""); //13
                return BinOp.DecodeUShort(buf, appDataOffset - APP_DATA_SEQ_SIZE);
            }
            else
            {
                string display = size == 20 ? "UDPAK" : "UDPXX";
                logFunc("{0} {1} {2}{3}{4}{5}[{6:D3}     {7}]",
                        left.Visible() ? display : "     ", dir, //0,1
                        right.Visible() ? (display + stage).PadRight(7) : "       ", //2
                        "", pre, pk,//3,4,5 
                        size, buf.ToHex(offset, Math.Min(size, 256))); //6,7
                return 0;
            }
        }

        public static SmartBuffer UnzipAppData(this ArrayPool<byte> bytePool, byte[] buf, int offset, int zippedSize)
        {
            ushort length = BinOp.DecodeUShort(buf, offset);
            var sb = bytePool.CreateSB(length);
            try
            {
                using (var input = new MemoryStream(buf, offset + 2, zippedSize - 2, false))
                using (var zipstream = new ZlibStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream(sb.Data))
                using (var sbCache = bytePool.CreateSB(sb.Data.Length))
                {
                    zipstream.DumpTo(output, sbCache.Data);
                    zipstream.Close();
                    sb.Size = (int)output.Position;
                    return sb;
                }
            }
            catch
            {
                sb.Dispose();
                return null;
            }
        }

        /// <summary>
        /// APPDATA => [KCPUPDATATYPE][APPSEQ][APPDATA]
        /// APPDATA => [KCPUPDATATYPE][APPSEQ][length][APPDATAZipped]
        /// </summary>
        public static SmartBufferEx<KCPDataDirection> ConvertAppData2KCPUpData(
            this ArrayPool<byte> bytePool, KCPUpDataTypeTag dataType, 
            byte[] buf, int offset, int size, bool tryZip, 
            FixedCircularQueue<KCPLib.ZipLogItem> zipLog, ushort seq = 0)
        {
            if (size > 8 * 1024)
            {
                throw new Exception("KCP底层不建议发送超大数据: [{0}]".f(size));
            }

            var sb = new SmartBufferEx<KCPDataDirection>(bytePool,
                size * (tryZip ? 2 : 1) + KCPUPDATA_PACK_ADDED_SIZE, //如果是ZIP，则用两倍原始数据的大小。
                KCPDataDirection.SEND);

            if (tryZip && (dataType == KCPUpDataTypeTag.APP || dataType == KCPUpDataTypeTag.APPZIPPED))
            {
                dataType = KCPUpDataTypeTag.APP;
                try
                {
                    using (var input = new MemoryStream(buf, offset, size, false))
                    using (var output = new MemoryStream(sb.Data, APP_DATA_OFFSET + 2, sb.Data.Length - APP_DATA_OFFSET - 2, true))
                    using (var zipstream = new ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestCompression, true))
                    using (var sbCache = bytePool.CreateSB(1024))
                    {
                        input.DumpTo(zipstream, sbCache.Data);
                        zipstream.Close();
                        if (zipLog != null)
                            zipLog.Enqueue(new KCPLib.ZipLogItem { Org = (ushort)size, Zipped = (ushort)output.Position });

                        if (output.Position < size)
                        {//压缩有用
                            sb.Size = (int)output.Position + KCPUPDATA_PACK_ADDED_SIZE + 2;
                            dataType = KCPUpDataTypeTag.APPZIPPED;
                            BinOp.EncodeUShort((ushort)size, sb.Data, APP_DATA_OFFSET);
                        }
                    }
                }
                catch
                {
                    //压缩没用
                }
            }

            sb.Data[0] = (byte)dataType;
            BinOp.EncodeUShort(seq, sb.Data, APP_DATA_SEQ_OFFSET);
            if (dataType != KCPUpDataTypeTag.APPZIPPED)
            {
                sb.Size = size + KCPUPDATA_PACK_ADDED_SIZE;
                Array.Copy(buf, offset, sb.Data, APP_DATA_OFFSET, size);
            }
            return sb;
        }

        public const int KCPHEAD_MIN_SIZE = 20;
        public const int APP_DATA_SEQ_SIZE = 2;
        public const int APPDATA_MIN_SIZE = 1;
        public const int KCPUPDATATYPE_SIZE = 1;

        /// <summary> 3 </summary>
        public const int KCPUPDATA_PACK_ADDED_SIZE = KCPUPDATATYPE_SIZE + APP_DATA_SEQ_SIZE;

        /// <summary> 24 </summary>
        public const int KCPDOWNDATA_MIN_SIZE = KCPHEAD_MIN_SIZE + KCPUPDATA_PACK_ADDED_SIZE +
                                                APPDATA_MIN_SIZE;

        /// [KCPHEAD?][KCPUPDATATYPE][APPSEQ][APPDATA]
        /// <summary> 1 </summary>
        public const int APP_DATA_SEQ_OFFSET = KCPUPDATATYPE_SIZE;

        /// <summary> 3 </summary>
        public const int APP_DATA_OFFSET = APP_DATA_SEQ_OFFSET + APP_DATA_SEQ_SIZE;

        public static bool TryUnPackKCPDownData2AppData(byte[] buf, int offset, int size, out KCPUpDataTypeTag kcpUpDataType, out int appDataOffset, out int appDataLength)
        {
            return TryUnPackKCPUpData2AppData(buf, offset + KCPHEAD_MIN_SIZE, size - KCPHEAD_MIN_SIZE, out kcpUpDataType, out appDataOffset, out appDataLength);
        }

#if HSFRAMEWORK_NET_ABOVE_4_5
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        public static bool TryUnPackKCPUpData2AppData(byte[] buf, int offset, int size, out KCPUpDataTypeTag kcpUpDataType, out int appDataOffset, out int appDataLength)
        {
            kcpUpDataType = KCPUpDataTypeTag.NONE;
            appDataOffset = appDataLength = 0;
            if (size <= 0)
                return false;
            try
            {
                return TryUnPackKCPUpData2AppDataInner(buf, offset, size, ref kcpUpDataType, ref appDataOffset, ref appDataLength);
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// [KCPUPDATATYPE][APPSEQ][APPDATA]
        /// </summary>
        public static bool TryUnPackKCPUpData2AppDataInner(byte[] buf, int offset, int size, ref KCPUpDataTypeTag kcpUpDataType, ref int appDataOffset, ref int appDataLength)
        {
            if (offset + size <= buf.Length && size > KCPUPDATA_PACK_ADDED_SIZE)
            {//在APP数据大小为1的时候，现有byte[]足够大。
                kcpUpDataType = (KCPUpDataTypeTag)buf[offset];
                appDataOffset = offset + APP_DATA_OFFSET;
                appDataLength = size - KCPUPDATA_PACK_ADDED_SIZE;
                if (kcpUpDataType > KCPUpDataTypeTag.NONE && kcpUpDataType < KCPUpDataTypeTag.MAX)
                    return true;
            }
            return false;
        }

        public static byte[] DummyBuff = new Byte[1] { 0XFF };
        public static byte[] EmptyBuff = new Byte[0];
        #region 添加[DataTypeTag][localsessionid][remotesessionid]
#if HSFRAMEWORK_NET_ABOVE_4_5
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        public static SmartBufferEx<T> PackKCPDownData2UDPData<T>(this ArrayPool<byte> bytePool, Random random, T t,
                    ushort remoteSessionID, byte localSessionID, byte[] data, int dataOffset, int dataSize)
        {
            if (data == null)
                data = EmptyBuff; //为了后面代码好写

            var sb = bytePool.CreateSBEx<T>(dataSize + SESSION_HEAD_SIZE, t);
            sb.Data[DATA_TYPE_OFFSET] = (byte)UDPDataTypeTag.KCP;
            KCPLib.ikcp_encode16u(sb.Data, REMOTE_SESSION_OFFSET, remoteSessionID);
            sb.Data[LOCAL_SESSION_OFFSET] = localSessionID;
            Array.Copy(data, dataOffset, sb.Data, KCPDOWN_DATA_OFFSET, dataSize);
            Encrypt(random, sb.Data, dataSize + SESSION_HEAD_SIZE - HEAD_LENGTH);
            return sb;
        }

        public static bool UnPackUDPData2KCPDownData(byte[] data, out UDPDataTypeTag udpDataType,
            out ushort remoteSessionID, out byte localSessionID, out int kcpDownDataOffset, out int kcpDownDataSize)
        {
            udpDataType = UDPDataTypeTag.NONE;
            remoteSessionID = ushort.MaxValue;
            localSessionID = byte.MaxValue;
            kcpDownDataOffset = KCPDOWN_DATA_OFFSET;
            kcpDownDataSize = data.Length - SESSION_HEAD_SIZE;
            if (kcpDownDataSize > 0 && DecryptHeader(data))
            {
                udpDataType = (UDPDataTypeTag)data[DATA_TYPE_OFFSET];
                KCPLib.ikcp_decode16u(data, REMOTE_SESSION_OFFSET, ref remoteSessionID);
                localSessionID = data[LOCAL_SESSION_OFFSET];
                return true;
            }
            return false;
        }
        #endregion

        public const int VERSION_OFFSET = 0;    //[0]
        public const int TRANSKEY_OFFSET = 1;   //[1]
        public const int CRC_OFFSET = 2;        //[2]  
        public const int HEAD_LENGTH = 3;
        public const int UDP_DATA_OFFSET = HEAD_LENGTH;     //[3]

        public const int DATA_TYPE_OFFSET = UDP_DATA_OFFSET;    //[3]
        public const int REMOTE_SESSION_OFFSET = 4;         //[4,5]
        public const int LOCAL_SESSION_OFFSET = 6;          //[6]
        public const int SESSION_HEAD_SIZE = 7;

        public const int KCPDOWN_DATA_OFFSET = SESSION_HEAD_SIZE;   //[7,]


#if HSFRAMEWORK_NET_ABOVE_4_5
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        private static bool Encrypt(Random random, byte[] data, int payloadSize)
        {
            //[0]: VERSION
            //[1]: DATAKEY
            //[2]: CRC
            //[3,]: DATA

            if (data.Length < (HEAD_LENGTH + payloadSize) || payloadSize <= 0)
                return false;

            //[0]: VERSION
            data[VERSION_OFFSET] = 0;

            //[1]: DATAKEY
            data[TRANSKEY_OFFSET] = (byte)random.Next();

            //[2]: CRC
            data[CRC_OFFSET] = GCRC8.Calc(data, UDP_DATA_OFFSET, payloadSize);

            if (KCPGlobalOptions.EnableKCPEncrypt)
            {
                //用datakey加密 [2]，然后循环加密。 
                for (int i = CRC_OFFSET; i < HEAD_LENGTH + payloadSize; i++)
                {
                    data[i] ^= data[i - 1];
                }
            }
            return true;
        }

#if HSFRAMEWORK_NET_ABOVE_4_5
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        private static bool DecryptHeader(byte[] data)
        {
            //[0]: VERSION
            //[1]: DATAKEY
            //[2]: CRC
            //[3,]: DATA

            if (data.Length <= HEAD_LENGTH)
                return false;

            //VERSION==0

            if (data[VERSION_OFFSET] != 0)
                return false;

            if (KCPGlobalOptions.EnableKCPEncrypt)
            {
                //循环解密
                for (int i = data.Length - 1; i >= CRC_OFFSET; i--)
                {
                    data[i] ^= data[i - 1];
                }
            }

            //校验CRC
            return GCRC8.Calc(data, UDP_DATA_OFFSET, data.Length - UDP_DATA_OFFSET) == data[CRC_OFFSET];
        }
    }

    public static class Utils
    {
        public static string GetFullLocalKey(uint sid, IPEndPoint endpoint)
        {
            return endpoint.ToString() + sid;
        }

        public static int CalcTimeOut(uint wakeUpTime, uint now, int min, int max, PlayerLinkStatus status)
        {
            if (wakeUpTime == uint.MaxValue && status == PlayerLinkStatus.Closing2)
            {
                return KCPGlobalOptions.ExitWaitAfterClosing2;
                //GGTODO: 需要根据RTO来设置。
            }

            if (wakeUpTime <= now)
            {
                return min;
            }
            else if (wakeUpTime - now > (uint)max)
            {
                return max;
            }
            else
            {
                return (int)(wakeUpTime - now);
            }
        }

#if false
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static void SmartSend<T>(this UdpClient udpClient, IPEndPoint endpoint, byte[] buf, int size, T state, Action<T, Exception> onException)
        {
            byte[] newBuf;
            if (KCPGlobalOptions.SendAsync)
            {
                newBuf = KCPLibEx.SharedBytePool.Rent(size);
                Array.Copy(buf, newBuf, size);
            }
            else
            {
                newBuf = buf;
            }

            try
            {
                if (KCPGlobalOptions.SendAsync)
                {
                    var sendTask = endpoint == null ? udpClient.SendAsync(newBuf, size) : udpClient.SendAsync(newBuf, size, endpoint);
                    sendTask.ContinueWith(t =>
                    {
                        KCPLibEx.SharedBytePool.Return(newBuf);
                        if (t.IsFaulted)
                        {
                            onException(state, t.Exception);
                            Console.WriteLine(t.Exception.ToString() + "\r\n");
                        }
                    }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                }
                else
                {
                    if (endpoint == null)
                        udpClient.Send(newBuf, size);
                    else
                        udpClient.Send(newBuf, size, endpoint);
                }
            }
            catch (Exception e)
            {
                if (KCPGlobalOptions.SendAsync)
                    KCPLibEx.SharedBytePool.Return(newBuf);

                onException(state, e);
            }
        }
#else
#endif
    }
}