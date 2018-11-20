using HSFrameWork.Common;
using System;
using HSFrameWork.Scut.RPC;
using HSFrameWork.Common.Inner;
using System.Threading;

namespace HSFrameWork.Scut.Inner
{
    public partial class ActionClient : IActionClient
    {
        public ActionClient(string url, IFrameUpdater updater, IActionClientSettings actionClientSetting)
        {
            URL = url;
            _Settings = actionClientSetting;
            if (updater != null)
            {
                _Updater = updater;
                _Updater.OnUpdate += Update;
                _Updater.OnAppQuit += OnApplicationQuit;
            }
        }

        public const int ACTION_MAX_RESEND_TIME = 10;
        public const float ACTION_RESEND_WAIT_TIME = 0.5f;
        public const int SOCKET_TIMEOUT = 2500;

        public IActionClientSettings Settings { get { return _Settings; } }

        public bool EnableRequireReconnectUI
        {
            get { return _EnableRequireReconnectUI; }
            set { _EnableRequireReconnectUI = value; }
        }

        public bool IsConnected
        {
            get
            {
                return mSocket != null && mSocket.IsConnected();
            }
        }

        public readonly string URL;

        /// <summary>
        /// 直接发送一条服务器协议，不进行排队
        /// </summary>
        public void Send(int actionId, Action<ActionResult> callback, ActionParam actionParam, bool bShowLoading = true)
        {
            Send(ActionFactory.Create(actionId, callback, null, Settings), actionParam, bShowLoading);
        }

        public void SendEx(int actionId, Action<ActionResult> callback, Action<ErrorCode, string> errorCallback, ActionParam actionParam = null, bool bShowLoading = true)
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1)
            {
                errorCallback(ErrorCode.UserCanceled, "用户关闭ActionClient");
                return;
            }

            Send(ActionFactory.Create(actionId, callback, errorCallback, Settings), actionParam, bShowLoading);
        }

        private void Send(GameAction gameAction, ActionParam actionParam, bool bShowLoading)
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1)
            {
                var str = "上层应用程序编写错误：ActionClinet已经关闭。请重建ActionClient";
                _Logger.Fatal(str);
                throw new Exception(str);
            }

            if (mSocket != null && mSocket.Closed)
            {
                mSocket.SayFinal();
                mSocket = null;
            }

            if (mSocket == null)
            {
                string[] arr = URL.Split(new char[] { ':' });
                int nPort = int.Parse(arr[1]);
                mSocket = new SocketConnect(arr[0], nPort, Settings);
            }

            byte[] data = gameAction.SetParam_GetDataToBeSent(actionParam);
            mSocket.Send(data, new ActionExecInfo()
            {
                Action = gameAction,
                ShowLoading = bShowLoading,
                SendTimeUTC = DateTime.UtcNow
            });
        }

        public void CallEx<TI, TO>(TI input, Action<TO> callback,
            Action<ErrorCode, string> errorCallback, string method = "any", bool bShowLoading = true)
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1)
            {
                errorCallback(ErrorCode.UserCanceled, "用户关闭ActionClient");
                return;
            }

            var action = new Action3<TI, TO>(method, input);
            action.Callback = x => callback((TO)x["ret"]);
            action.ErrorCallback = errorCallback;
            action.Settings = Settings;
            Send(action, null, bShowLoading);
        }

        public virtual void Destroy(bool noCallback = true)
        {
            Logger.Debug("{0} Destroy()", URL);
            if (_Updater != null)
            {
                _Updater.OnAppQuit -= OnApplicationQuit;
                _Updater.OnUpdate -= Update;
                _Updater = null;
            }

            ShutDownActionQueue(noCallback);
            SafeCloseSocket(noCallback);
            Interlocked.Exchange(ref _Closed, 1);
        }

        public void ResetSocket()
        {
            SafeCloseSocket(false);
        }

        public void HideLoading()
        {
            Container.Resolve<IActionClientUI>().ShowLoading = false;
        }

        #region 私有接收相关
        protected void Update()
        {
            if (mSocket != null)
            {
                mSocket.Update();

                ActionExecInfo info;
                while (mSocket != null && (info = mSocket.Dequeue()) != null)
                    OnSocketRespond(info);

                while (mSocket != null && (info = mSocket.DequeuePush()) != null)
                    OnSocketRespond(info);

                //在回调过程中mSocket可能会被修改。
                if (mSocket != null && mSocket.Closed)
                {
                    mSocket.SayFinal();
                    mSocket = null;
                }
            }
        }

        private void OnSocketRespond(ActionExecInfo execInfo)
        {
            if (execInfo.ErrorCode != 0)
            {
                OnActionError(execInfo.Action, execInfo.ErrorCode, execInfo.ErrorMsg); //网络错误
                return;
            }

            if (!TryPreProcessInQueue(execInfo))
                return;

            if (execInfo.Action.TryDecodePackageAndCallBack(execInfo.Reader))
            {
                LoggerDown.Trace("Action#{0} NetTime[{1}ms] 成功回调。", execInfo.Action.ActionId,
                    (int)execInfo.NetTime.TotalMilliseconds);
                if (execInfo.Action.ActionQueueSlot != null)
                    OnCurrentActionSuccess();
            }
            else
            {
                LoggerDown.Error("Action#{0} NetTime[{1}ms] 数据解码错误，关闭当前Socket.",
                    execInfo.Action.ActionId, (int)execInfo.NetTime.TotalMilliseconds);
                SafeCloseSocket(false);
                OnActionError(execInfo.Action, ErrorCode.DecodeError, "decode package fail.");
            }
        }

        private void OnActionError(GameAction action, ErrorCode errorCode, string errorMsg)
        {
            if (action.ActionQueueSlot != null)
                OnCurrentActionFailed(action, errorCode, errorMsg);
            else if (action.ErrorCallback != null)
                action.OnErrorCallback(errorCode, errorMsg);
            else
                TryShowRequireReconnect(); //网络出错，需要重连
        }

        private void TryShowRequireReconnect()
        {
            if (EnableRequireReconnectUI)
                _UI.ShowRequireReconnectUI(DoCurrentAction);
        }

        private void SafeCloseSocket(bool noCallback = true)
        {
            if (mSocket != null)
            {
                mSocket.Close(!noCallback);
                Update();
            }
        }
        #endregion

        #region 其他私有
        internal void OnApplicationQuit()
        {
            Logger.Info("退出游戏。");
            Destroy();
        }

        protected void ThrowIfClosed()
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1)
                throw new Exception("编程错误。ActionClient已经销毁。");
        }

        private IFrameUpdater _Updater;
        private SocketConnect mSocket;
        private bool _EnableRequireReconnectUI = true;
        private readonly IActionClientUI _UI = Container.Resolve<IActionClientUI>();
        protected int _Closed = 0;
        private readonly IActionClientSettings _Settings;

        internal static readonly IHSLogger Logger = HSLogManager.GetLogger("GSOCK");
        internal static readonly IHSLogger LoggerUp = HSLogManager.GetLogger("GSOCKU");
        internal static readonly IHSLogger LoggerDown = HSLogManager.GetLogger("GSOCKD");
        #endregion
    }
}