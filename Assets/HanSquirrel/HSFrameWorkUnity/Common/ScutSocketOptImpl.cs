using GLib;
using HSFrameWork.Common;

namespace HSFrameWork.Scut.Inner
{
    public class ScutSocketOptUnityImpl : IScutSocketOptions
    {
        public bool LetServerKeepIdleConnection { get { return _LetServerKeepIdleConnection; } }

        public bool NoHeartBeat { get { return _NoHeartBeat; } }

        public bool NoSocketTimeOut { get { return _NoSocketTimeOut; } }
        public string SocketOptionsDesc { get { return _SocketOptionsDesc; } }

        public uint DevFlagToServer
        {
            get
            {
                return LetServerKeepIdleConnection ? (uint)1 : (uint)0;
            }
        }

        public bool InternetReachable
        {
            get
            {
                return UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable;
            }
        }

        public ScutSocketOptUnityImpl()
        {
            _SocketOptionsDesc = "";
            if (LetServerKeepIdleConnection)
                _SocketOptionsDesc += "[LetServerKeepIdleConnection]";
            if (NoSocketTimeOut)
                _SocketOptionsDesc += "[NoSocketTimedOut]";
            if (NoHeartBeat)
                _SocketOptionsDesc += "[NoHeartBeat]";
        }

        private string _SocketOptionsDesc;
        private bool _LetServerKeepIdleConnection
#if UNITY_EDITOR
            = HSUnityEnv.ProjectPath.Sub("data/HSConfigTable/let_server_keep_idle_connection").ExistsAsFile();
#else
            = false;
#endif

        private bool _NoHeartBeat
#if UNITY_EDITOR
            = HSUnityEnv.ProjectPath.Sub("data/HSConfigTable/no_heartbeat").ExistsAsFile();
#else
            = false;
#endif

        private bool _NoSocketTimeOut
#if UNITY_EDITOR
            = HSUnityEnv.ProjectPath.Sub("data/HSConfigTable/no_socket_timedout").ExistsAsFile();
#else
            = false;
#endif
    }
}
