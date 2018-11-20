using HSFrameWork.Common;
using HSFrameWork.Common.Inner;

namespace HSFrameWork.Common.Editor
{
    /// <summary>
    /// HSFrameWork在编辑器下的唯一初始化入口
    /// </summary>
    public class HSBootEditor
    {
        /// <summary>
        /// 在编辑器下初始化HSFrameWork的唯一入口。
        /// </summary>
        public static void ColdBind(string desKey, ConfigTable.IInitHelper helper, string nLogConfigFile)
        {
            _OTRH.TryRunOnce(() => 
            {
                HSBooterShared.ColdBind(desKey);

                NLogHelper.AutoRefreshNLogConfig(nLogConfigFile);

                ConfigTable.Editor.Impl.RunTimeConfiger.ColdBind();
                ConfigTable.Editor.Impl.UITaskScheduler.ColdBind();
                ConfigTable.Editor.HSCTC.ColdBind();

                if (helper != null)
                {
                    ConfigTable.Inner.CTFacadeUnity.ColdBind(helper);
                    ConfigTable.Editor.Trans.Impl.TransFacade.RegisterTextFindersForward(() => helper.TextFinders);
                }

                SPojo.Editor.Inner.SPojoLogger.ColdBind();

                HSUtils.Log("▬▬▬▬▬▬▬▬▬▬▬▬ 项目重新加载完成 ▬▬▬▬▬▬▬▬▬▬▬▬");
            });
        }

        private static OneTimeRunHelper _OTRH = new OneTimeRunHelper();
    }
}
