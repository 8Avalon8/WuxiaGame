using GLib;
using HSFrameWork.Common;

namespace HSFrameWork.ConfigTable.Inner
{
    /// <summary>
    /// 配置表功能的初始化类。编辑器和运行时都需要。
    /// </summary>
    public class CTFacadeUnity : FacadeAbstract
    {
        /// <summary>
        /// 初始化配置表功能
        /// </summary>
        public static new void ColdBind(IInitHelper helper)
        {
            Mini.ThrowIfFalse(HSUtils.Inited, "FacadeUnity.ColdBind之前必须初始化HSUtils");
            FacadeAbstract.ColdBind(helper);
        }
    }
}
