using HSFrameWork.Scut.Inner;
namespace HSFrameWork.Common
{
    using AiUnity.NLog.GG;
    using System.Threading;
    using HanSquirrel.ResourceManager;
    using ConfigTable;
    using System.Collections.Generic;
    using Inner;
    using Scut;
    using UnityEngine;
    using System;

    namespace Inner
    {
        /// <summary>
        /// HSFrameWork在Untiy中的初始化工具类。编辑器菜单和运行时共享。
        /// </summary>
        public class HSBooterShared
        {
            public static void ColdBind(string desKey)
            {
                _OTRH.TryRunOnce(() =>
                {
                    if (desKey != null)
                        TDES.LocalInstance.Init(desKey);

                    HSUnityEnv.WarmUp();
                    ContainerByFunq.ColdBind();

#if HSFRAMEWORK_AIUNITY_NLOG_GG
                    Container.Register<HSLogManager>(c => HSLogManagerALogImpl.Instance, ReuseScope.Container);
#else
                    Container.Register<HSLogManager>(c => HSLogManagerUnityImpl.Instance, ReuseScope.Container);
#endif

#if HSFRAMEWORK_ALIYUN_LOG
                    UnityALiCloudLogSourceAgent.ColdBind();
                    Container.Register(x => UnityALiCloudLogSourceAgent.Instance, ReuseScope.Container);
#endif

#if HSFRAMEWORK_AIUNITY_NLOG_GG
                    HSUtilsHSLogImpl.ColdBind();
#else
                    HSUtilsUnityImpl.ColdBind();
#endif

                    BetterStreamingAssets.Initialize();
                });
            }
            private static OneTimeRunHelper _OTRH = new OneTimeRunHelper();
        }
    }

    /// <summary>
    /// HSFrameWork在APP中的唯一初始化工具类。
    /// </summary>
    public class HSBootApp
    {
        /// <summary>
        /// HSFrameWork在App中的唯一初始化函数。会初始化所有和HSFrameWork相关的功能。
        /// 越早调用越好。可重入。
        /// </summary>
        public static void ColdBind(string desKey, IInitHelper helper, IEnumerable<HSLeanPoolConfig> prefabPoolConfig)
        {
            _OTRH.TryRunOnce(() =>
            {
                HSBooterShared.ColdBind(desKey);
                Container.Register<IFrameUpdater>(x => HSUtilsEx.CreateAlwayMB<MonoFrameUpdater>(), ReuseScope.Container);
                Container.Register<IScutSocketOptions>(x => new ScutSocketOptUnityImpl(), ReuseScope.Container);
                Container.Register<Func<string, string>>(NetWriter.URLEncodeContainerKey, x => WWW.EscapeURL, ReuseScope.Container);
                Container.HideResolveException = false;

                if (helper != null)
                    HSFrameWork.ConfigTable.Inner.CTFacadeUnity.ColdBind(helper);
                SPojo.Inner.DebugFacade.ColdBind();

                if (prefabPoolConfig != null)
                    ResourceLoader.ResetPoolConfig(prefabPoolConfig);

#if HSFRAMEWORK_NET_ABOVE_4_5
                UTaskScheduler.ColdBind();
#endif
            });
        }

        private static OneTimeRunHelper _OTRH = new OneTimeRunHelper();
    }
}
