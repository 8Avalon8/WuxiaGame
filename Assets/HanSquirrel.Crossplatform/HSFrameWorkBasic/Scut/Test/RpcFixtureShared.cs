
using ProtoBuf;

namespace HSFrameWork.Scut.RPC.Test
{
    [ProtoContract]
    public class EchoMsg
    {
        [ProtoMember(1)]
        public string Message;
    }
}
