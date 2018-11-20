using HSFrameWork.Common;
using System;

namespace HSFrameWork.Scut.RPC
{
    public class Action3<TI, TO> : GameAction
    {
        private readonly TI _Input;

        private string _MethodName;
        public Action3(string method, TI ti) : base(3)
        {
            _Input = ti;
            _MethodName = method;
        }

        protected override bool EncryptStatus()
        {
            return false;
        }

        protected override void SendParameter(NetWriter writer, ActionParam actionParam)
        {
            writer.writeString("method", _MethodName);
            writer.writeString("input", typeof(TI).ToString());
            writer.writeString("output", typeof(TO).ToString());
            if (_Input != null)
                writer.WriteBytes(_Input.Serialize()); //这样在服务端就会收到一个NULL的。
        } 

        protected override void DecodePackage(NetReader reader)
        {
            byte[] data = null;
            if (!reader.TryReadBytes(ref data))
            {
                _ActionResult["ret"] = null;
                return;
            }

            try
            {
                _ActionResult["ret"] = DirectProtoBufTools.Deserialize<TO>(data);
            }
            catch (Exception ex)
            {
                _Logger.Error(ex, "反序列化失败 [{0}]", typeof(TO).ToString());
                _ActionResult["ret"] = null;
            }
        }

        private ActionResult _ActionResult = new ActionResult();
        protected override ActionResult GetResponseData()
        {
            return _ActionResult;
        }

        public TO GetResult()
        {
            return (TO)_ActionResult["ret"];
        }
    }
}
