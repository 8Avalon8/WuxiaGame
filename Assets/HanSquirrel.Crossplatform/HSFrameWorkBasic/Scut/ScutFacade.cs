using System;
using System.Reflection;

namespace HSFrameWork.Scut
{
    using Common;
    using Inner;
    #region 对外公开接口
    //GG 对应服务器上 Lanague.cs
    public enum ErrorCode
    {
        /// <summary> 成功 </summary>
        Success = 0,
        /// <summary> 连接错误（socket出错） </summary>
        ConnectError = -1,
        /// <summary> 超时（在给定时间内Action没有返回，然而Socket没有返回错误） </summary>
        TimedoutError = -2,
        /// <summary> 被应用强制终止 </summary>
        UserCanceled = -3,
        /// <summary> 客户端发现服务端代码实现有BUG </summary>
        Client_FoundServerLogicError = -4,
        /// <summary> 客户端和服务端不匹配 </summary>
        ClientServerMismatch = -5,
        SendError = -6,
        RecvError = -7,
        SendTimedout = -8,
        DecodeError = -9,
        /// <summary> 服务器内部错误 </summary>
        Server_GeneralError = 10000,
        /// <summary> 没有登录 </summary>
        Server_NeedLogIn = 10001,
        /// <summary> 被踢出，（我们目前没有使用） </summary>
        KServer_KickedOut = 10002,
        /// <summary> URL语法错误（服务器内部错误或者客户端编程错误） </summary>
        Server_URLParamError = 10003,
        /// <summary> 用户从给其他地方登录，该Session被踢出。 </summary>
        Server_ReplacedByNewLogin = 10004,
        /// <summary> 服务器正在维护 </summary>
        Server_Maintain = 10005,
        /// <summary> 服务器内部操作超时 </summary>
        Server_OpTimedout = 10006,
    }

    /// <summary>
    /// 与服务器交互的唯一接口，原名为Net
    /// </summary>
    public interface IActionClient
    {
        IActionClientSettings Settings { get; }

        /// <summary>
        /// 缺省都是Enable，否则如果出现需要重连的情况，就无法显示了。
        /// 但是目前HotPatchingUI中自行启动一个延时启动的重连对话框中使用了这个。
        /// 等待这个逻辑删除后，就可以删除这个接口了。
        /// </summary>
        bool EnableRequireReconnectUI { get; set; }

        /// <summary>
        /// 会清除所有Action（队列中的待发送+已发送+已收到的)，并关闭Socket。
        /// Close之后这个ActionClient就不能再次使用了。
        /// </summary>
        void Destroy(bool noCallback = true);

        /// <summary>
        /// 将Socket主动关闭，当前正在Send的会收到错误回调。
        /// </summary>
        void ResetSocket();

        /// <summary>
        /// 关闭ActionQueue。清除所有当前未发送的Action。
        /// 此后当前正在发送的Action如果收到服务端返回数据，则会被忽略。
        /// ShutDown不可恢复。如果在ShutDown之后还去SendInActionQueue，则会异常。
        /// ShutDown之后只可以用Send()函数来发送数据了。
        /// </summary>
        void ShutDownActionQueue(bool noCallback = true);

        /// <summary>
        /// 放入队列依次发送。如果发送中间出现错误，则会ShowRequireReconnectUI提示用户是否重连。
        /// 在重连结束后会自动重发。
        /// </summary>
        void SendInActionQueue(int actionId, Action<ActionResult> callback, ActionParam actionParam, bool bShowLoading = true);

        /// <summary>
        /// 放入队列依次发送。如果发送中间出现错误，则会ShowRequireReconnectUI提示用户是否重连。
        /// 在重连结束后会自动重发。
        /// 在用户Close、Dispose、ShutDownActionQueue的时候如果指定要CallBack，则会受到错误回调。
        /// </summary>
        void SendInActionQueueEx(int actionId, Action<ActionResult> callback, Action<ErrorCode, string> errorCallback, ActionParam actionParam, bool showLoading = true);

        /// <summary>
        /// 立刻发送。如果发送失败，会调用 IActionClientUI.ShowRequireReconnectUI。然而不会自动重发。
        /// </summary>
        void Send(int actionId, Action<ActionResult> callback, ActionParam actionParam, bool bShowLoading = true);

        /// <summary>
        /// 立刻发送，如果发送失败，则会调用 errorCallback()，不会调用 IActionClientUI.ShowRequireReconnectUI。
        /// timedOutMS是本条发送的超时时间（如果超出这个时间没有收到回复，则认为网络出错，断开Socket。）
        /// </summary>
        void SendEx(int actionId, Action<ActionResult> callback, Action<ErrorCode, string> errorCallback,
            ActionParam actionParam, bool bShowLoading = true);

        void CallEx<TI, TO>(TI input, Action<TO> callback, Action<ErrorCode, string> errorCallback, string method = "any", bool bShowLoading = true);

        /// <summary>
        /// 隐藏Loading窗口
        /// </summary>
        void HideLoading();
        bool IsConnected { get; }
    }

    public partial class ActionClientFactory
    {
        public static IActionClient Create(string url)
        {
            return new ActionClient(url, Container.Resolve<Common.Inner.IFrameUpdater>(), Container.Resolve<IActionClientSettings>());
        }
    }
    #endregion

    #region 需要外部注入在Container的接口
    public interface IActionClientSettings
    {
        void Sync(DateTime dt);
        bool IsSynced { get; }
        DateTime Now { get; }
        string GameVersion { get; }
        int ZipLength { get; }
        int DefaultSocketTimeoutMS { get; }
        int HeartBeatIntervalMS { get; }
        int HeartBeatTimeoutMS { get; }
        Assembly ActionAssembly { get; }
        string ActionTypeFormat { get; }
        string ApplicationPlatform { get; }
    }

    public interface IScutSocketOptions
    {
        uint DevFlagToServer { get; }
        /// <summary>
        /// 如果设置为TRUE，则如果服务器看本实例很久都没有通讯也不会将该玩家踢出。
        /// 应用场景：如果客户端在调试状态下，很容易很久都没有通讯。
        /// </summary>
        bool LetServerKeepIdleConnection { get; }
        /// <summary>
        /// 不发送心跳包
        /// </summary>
        bool NoHeartBeat { get; }
        /// <summary>
        /// 无限超时（也用于调试程序时不至于被服务端断开连接）
        /// </summary>
        bool NoSocketTimeOut { get; }
        string SocketOptionsDesc { get; }

        bool InternetReachable { get; }
    }

    public interface IActionClientUI
    {
        /// <summary>
        /// 是否显示loading用于阻塞界面
        /// </summary>
        bool ShowLoading { get; set; }

        /// <summary>
        /// 在Action发送失败后，框架需要调用显示提示用户重连，
        /// 在用户确定，并且对连接做了一些基础数据交换后，则回调callback。
        /// callback保证不是null。会恢复发送队列。
        /// </summary>
        void ShowRequireReconnectUI(Action callback);
    }
    #endregion
}