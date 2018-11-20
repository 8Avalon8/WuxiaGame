using System;

namespace HSFrameWork.Scut.Inner
{
    ///Ô­Ãû SocketPackage
    public class ActionExecInfo
    {
        public GameAction Action { set; get; }
        public bool ShowLoading { set; get; }
        public DateTime SendTimeUTC { set; get; }
        public TimeSpan NetTime { get { return Reader.RecvTimeUTC - SendTimeUTC; } }

        public NetReader Reader { get; set; }
        public ErrorCode ErrorCode { set; get; }
        public string ErrorMsg { set; get; }
    }


}

