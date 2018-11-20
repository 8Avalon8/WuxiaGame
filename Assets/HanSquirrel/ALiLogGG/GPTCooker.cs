#if HSFRAMEWORK_RUN_IN_MS_CONSOLE || (AIUNITY_CODE && HSFRAMEWORK_ALIYUN_LOG)

using System;
using Google.ProtocolBuffers.George;

namespace HSFrameWork.Common.Inner
{
    /// <summary>
    /// 用于强制ILCPP在IOS下生成对应的泛型函数。
    /// 如果不如此，则在IOS下，使用Google.ProtoBuf的阿里云Log会失败。
    /// </summary>
    public class GoolgeProtobufCooker
    {
        /// <summary>
        /// 这个永远不能改
        /// </summary>
        public static bool _Dummy = true;
        /// <summary>
        /// 仅仅有编译意义，没有运行意义。
        /// </summary>
        public static void ColdBind()
        {
            GCooker.ColdBind();

            if (_Dummy)
                return;

            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.Log, System.UInt32>(null);
            GCooker.CreateDowncastDelegateImpl<Aliyun.Api.LOG.Log.Builder, System.UInt32>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.Log, System.Collections.Generic.IList<Aliyun.Api.LOG.Log.Types.Content>> (null);
            GCooker.CreateDowncastDelegateIgnoringReturnImpl<Aliyun.Api.LOG.Log.Builder, Aliyun.Api.LOG.Log.Types.Content, Aliyun.Api.LOG.Log.Builder>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.Log.Builder, Google.ProtocolBuffers.Collections.IPopsicleList<Aliyun.Api.LOG.Log.Types.Content>> (null);
            GCooker.CreateStaticUpcastDelegateImpl<Aliyun.Api.LOG.Log.Types.Content.Builder>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.Log.Types.Content, System.String>(null);
            GCooker.CreateDowncastDelegateImpl<Aliyun.Api.LOG.Log.Types.Content.Builder, System.String>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.Log.Types.Content, System.String>(null);
            GCooker.CreateDowncastDelegateImpl<Aliyun.Api.LOG.Log.Types.Content.Builder, System.String>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.LogGroup, System.Collections.Generic.IList<Aliyun.Api.LOG.Log>> (null);
            GCooker.CreateDowncastDelegateIgnoringReturnImpl<Aliyun.Api.LOG.LogGroup.Builder, Aliyun.Api.LOG.Log, Aliyun.Api.LOG.LogGroup.Builder>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.LogGroup.Builder, Google.ProtocolBuffers.Collections.IPopsicleList<Aliyun.Api.LOG.Log>> (null);
            GCooker.CreateStaticUpcastDelegateImpl<Aliyun.Api.LOG.Log.Builder>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.LogGroup, System.String>(null);
            GCooker.CreateDowncastDelegateImpl<Aliyun.Api.LOG.LogGroup.Builder, System.String>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.LogGroup, System.String>(null);
            GCooker.CreateDowncastDelegateImpl<Aliyun.Api.LOG.LogGroup.Builder, System.String>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.LogGroup, System.String>(null);
            GCooker.CreateDowncastDelegateImpl<Aliyun.Api.LOG.LogGroup.Builder, System.String>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.LogGroupList, System.Collections.Generic.IList<Aliyun.Api.LOG.LogGroup>> (null);
            GCooker.CreateDowncastDelegateIgnoringReturnImpl<Aliyun.Api.LOG.LogGroupList.Builder, Aliyun.Api.LOG.LogGroup, Aliyun.Api.LOG.LogGroupList.Builder>(null);
            GCooker.CreateUpcastDelegateImpl<Aliyun.Api.LOG.LogGroupList.Builder, Google.ProtocolBuffers.Collections.IPopsicleList<Aliyun.Api.LOG.LogGroup>> (null);
            GCooker.CreateStaticUpcastDelegateImpl<Aliyun.Api.LOG.LogGroup.Builder>(null);
        }
    }
}

#endif
