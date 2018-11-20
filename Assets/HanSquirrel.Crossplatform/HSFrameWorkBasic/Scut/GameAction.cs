using ICSharpCode.SharpZipLib.GZip;
using System;
using System.IO;
using System.Text;
using GLib;
using HSFrameWork.Common;

namespace HSFrameWork.Scut
{
    using Inner;

    /// <summary>
    /// 游戏Action接口
    /// </summary>
    public abstract class GameAction
    {
        public string DebugString
        {
            get
            {
                return "Action#{0}[MsgId#{1}]".f(ActionId, MsgId);
            }
        }

        public IActionClientSettings Settings;

        protected GameAction(int actionId)
        {
            ActionId = actionId;
            MsgId = -1;
        }

        public const int TIMEOUTMS_NOT_SET = -1;
        public virtual int TimeoutMS
        {
            get
            {
                return TIMEOUTMS_NOT_SET;
            }
        }

        public readonly int ActionId;

        public int MsgId { get; private set; }

        public Action<ActionResult> Callback;
        public Action<ErrorCode, string> ErrorCallback;

        protected virtual bool EncryptStatus()
        {
            return true;
        }

        public virtual bool NeedResend()
        {
            return true;
        }

        //需要在submitqueque中重发而不是底层重发
        public virtual bool MustbeResendInSubmitqueue()
        {
            return false;
        }

        private static byte[] Compression(byte[] buf)
        {
            using (MemoryStream ms = new MemoryStream(buf))
            using (MemoryStream resultMS = new MemoryStream())
            {

                resultMS.WriteByte(0x1f);
                resultMS.WriteByte(0x8b);
                resultMS.WriteByte(0x08);
                resultMS.WriteByte(0x00);

                using (GZipOutputStream zip = new GZipOutputStream(resultMS))
                {
                    byte[] data = new byte[256];

                    int count = 0;
                    while ((count = ms.Read(data, 0, data.Length)) != 0)
                    {
                        zip.Write(data, 0, count);
                    }
                    zip.Finish();
                    zip.Close();
                }

                resultMS.Flush();
                return resultMS.ToArray();
            }
        }

        #region Debug接口，不影响效率 GG
        private static StringBuilder DataSentSB;
        public static Action<int, string> OnSendAction;

        /// <summary>
        /// 标记这个Action是否有发出过数据；有些Action是服务端主动发送的。
        /// </summary>
        private bool _Sended = false;
        public byte[] SetParam_GetDataToBeSent(ActionParam actionParam)
        {
            if (actionParam == null)
                actionParam = ActionParam.Empty;

            _Sended = true;
            if (OnSendAction == null)
                return GetDataOld(actionParam);

            DataSentSB = new StringBuilder();
            NetWriter.OnWriteAction = (key, obj) => DataSentSB.AppendFormat("{0}:{1} ", key, obj);
            using (DisposeHelper.Create(() => { NetWriter.OnWriteAction = null; DataSentSB = null; }))
            {
                byte[] data = GetDataOld(actionParam);
                OnSendAction(ActionId, DataSentSB.ToString());
                return data;
            }
        }
        #endregion

        private byte[] GetDataOld(ActionParam actionParam)
        {
            NetWriter writer = new NetWriter();
            MsgId = writer.MsgId;
            writer.writeInt32("actionId", ActionId);

            if (Settings.IsSynced)
            {
                var timeStamp =
                    Settings.Now.ToString("yyyy-MM-dd HH:mm:ss");

                writer.writeString("__sync_timestamp", timeStamp);
            }
            writer.writeString("__application_platform", Settings.ApplicationPlatform);
            writer.writeString("__application_version", Settings.GameVersion);

            SendParameter(writer, actionParam);

            if (!EncryptStatus())
            {
                return writer.PostData();
            }
            else
            {
                byte[] data = writer.PostData(false);

                if (data.Length > Settings.ZipLength)
                {
                    data = Compression(data);
                }

                byte[] buff = TDES.ScutInstance.Encrypt(data);
                byte[] result = new byte[buff.Length + 6];
                byte[] lenBuff = BitConverter.GetBytes(2 + buff.Length);

                Buffer.BlockCopy(lenBuff, 0, result, 0, 4);
                result[4] = Convert.ToByte('j');
                result[5] = Convert.ToByte('m');
                Buffer.BlockCopy(buff, 0, result, 6, buff.Length);
                return result;
            }
        }

        /// <summary>
        /// 尝试解Body包，然后调用CallBack
        /// </summary>
        public bool TryDecodePackageAndCallBack(NetReader reader)
        {
            try
            {
                DecodePackage(reader);
            }
            catch (Exception ex)
            {
                ActionClient.Logger.Error(ex, "Action {0} DecodePackage exception.", ActionId);
                return false;
            }

            ActionResult result;
            try
            {
                result = GetResponseData();
            }
            catch (Exception ex)
            {
                ActionClient.Logger.Error(ex, "Action {0} GetResponseData exception.", ActionId);
                return false;
            }

            if (OnReceiveAction != null)
                OnReceiveAction(_Sended, ActionId, result);
            try
            {
                if (Callback != null)
                    Callback(result);
                return true;
            }
            catch (Exception ex)
            {
                ActionClient.Logger.Error(ex, "Action {0} callback process exception.", ActionId);
                return false;
            }
        }

        /// <summary>
        /// GG 仅仅开始测试中使用。
        /// </summary>
        public static Action<bool, int, ActionResult> OnReceiveAction;

        public void OnErrorCallback(ErrorCode errorCode, string errorMsg)
        {
            try
            {
                if (ErrorCallback != null)
                    ErrorCallback(errorCode, errorMsg);
            }
            catch (Exception ex)
            {
                ActionClient.Logger.Error(ex, "Action {0} ErrorCallback process exception.", ActionId);
            }
        }

        protected abstract void SendParameter(NetWriter writer, ActionParam actionParam);

        protected abstract void DecodePackage(NetReader reader);

        protected abstract ActionResult GetResponseData();

        /// <summary>
        /// add by cg:action队列发送的代理句柄
        /// 如果为空，说明是直接通过Send发送的；如果不为空，说明是通过SendActionQueue发送出去的。
        /// </summary>
        public ActionQueueSlot ActionQueueSlot;
        protected IHSLogger _Logger = HSLogManager.GetLogger("Action");
    }
}
