using System;
using HSFrameWork.SPojo.TestCase;
using UnityEngine;
using GLib;
using HSFrameWork.Common;

namespace HSFrameWork.SPojo.Inner
{
    /// <summary>
    /// SPojo的调试功能激活类
    /// </summary>
    public static class DebugFacade
    {
        /// <summary>
        /// 如果设置需要激活，则激活调试功能
        /// </summary>
        public static void ColdBind()
        {
            if (HSUnityEnv.RunInPureClient && Type.GetType("HSFrameWork.SPojo.EnableRunTimeDataDetailLog") != null)
            {
                HSUtils.Log("★★★★★★★★启动RuntimeDataLog★★★★★★★★ [{0}]", Application.persistentDataPath);
                RunTimeDataDetailLog.Init(Application.persistentDataPath.StandardSub("/Debug/"), true);
            }
        }
    }
}
