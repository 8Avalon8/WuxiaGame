using GLib;

namespace HSFrameWork.ConfigTable.Inner
{
    public abstract class FacadeAbstract
    {
        protected static void ColdBind(IInitHelper helper)
        {
            ExeTimer.Disabled = !helper.ShowExeTimer;  //是否显示HSFrameWork内部的很多执行时间；

            if (helper.ProtoBufTypes != null)
                ProtoBufTools.Reset(helper.ProtoBufTypes);

            ConfigTable.DoLoadData = helper.LoadConfigTableData;
            ConfigTable.DoLoadDataV2 = helper.LoadConfigTableDataV2;
            BeanNodeMap.ColdBind(helper.XMLBeanMaps);
            ConfigTable.LoadDesignModeDelegate = helper.ResourceManagerLoadDesignModeDelegate;
            ConfigTable.LoadDesignModeDelegateV2 = helper.ResourceManagerLoadDesignModeDelegateV2;
        }
    }
}