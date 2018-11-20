using System;
using UnityEditor;
using GLib;
using HSFrameWork.XLS2XML;
using System.IO;
using UnityEngine;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using HSFrameWork.SPojo;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Editor.Impl;
#if HSFRAMEWORK_TAISAI
using HSFrameWork.RoomService.Test.TaiSai;
#endif

using HSFrameWork.KCP.Client;

namespace HSFrameWork.ConfigTable.Editor.Inner
{
    public class MaNongTestBag
    {
        int yyy = 100;

#if HSFRAMEWORK_TAISAI
        [MenuItem("Tools♥/停止TasiSaiClientTest", true)]
        static bool CanStopTaiSaiClientTest()
        {
            int x = 2;
            return clientsCore != null;
        }

        [MenuItem("Tools♥/停止TasiSaiClientTest", false, 1)]
        static void StopTaiSaiClientTest()
        {
            clientsCore.SignalStop();
        }

        [MenuItem("Tools♥/启动TasiSaiClientTest", true)]
        static bool CanTaiSaiClientTest()
        {
            return clientsCore == null;
        }

        static MaNongTestBag()
        {
#if true
            TaiSaiPlayerCore.NeedTraceThisUserFunc = userId => userId == 1;
            KCPClientFactory.NeedTraceFunc = link => link.DisplayName == 1;
#endif
        }


        private static TaiSaiAIClientsAbstract clientsCore;
        [MenuItem("Tools♥/启动TasiSaiClientTest", false, 1)]
        static void TaiSaiClientTest()
        {
            //TaiSaiAsyncFacadeU.RunClientsDirAsync<TaiSaiPlayerAsyncWrapper>(out clientsCore, 20, 20).ContinueWith(t => clientsCore = null);
            TaiSaiAsyncFacadeU.RunClientsWSAsync<TaiSaiPlayerAsyncWrapper>(out clientsCore, 20, 20).ContinueWith(t => clientsCore = null);
            //clientsCore = TaiSaiPhoneFacade.RunClientsWSSync("192.168.1.66:10009", 20, 20, () => clientsCore = null);
        }
#endif
        [MenuItem("Tools♥/码农专用/NLogTest", false)]
        static void NLogTest()
        {
            var logger = HSLogManager.GetLogger("MiscTest");
            logger.Info("这是一个INFO");
            logger.Debug("这是一个DEBUG");
            logger.Trace("这是一个TRACE");
            logger.Warn("这是一个WARN");
            logger.Error("这是一个ERROR");
            logger.Fatal("这是一个FATAL");
        }

        private static List<string>[] myLists = new List<string>[1024 * 1024];
        //[MenuItem("Tools♥/码农专用/分配巨量List", false)]
        static void ListMemTest()
        {
            using (Mini.MemoryDiff(m => HSUtils.LogWarning("List<string>占用{0}字节", m / (1024 * 1024.0))))
                for (int i = 0; i < 1024 * 1024; i++)
                    myLists[i] = new List<String>();
        }

        [MenuItem("Tools♥/码农专用/GetGlobalPojoInfo", false)]
        static void GetGlobalPojoInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("总共有 [{0}] 个SaveablePojo。".Eat(Saveable.InstanceCount));
            Saveable.InstanceDict.ToList().SortC((kv1, kv2) => kv1.Value.lived.CompareTo(kv2.Value.lived)).ForEach(kv =>
            {
                sb.AppendLine("{0} [{1}]个，创建 [{2}] 个，销毁 [{3}] 个。".Eat(kv.Key.FullName, kv.Value.lived, kv.Value.created, kv.Value.destoried));
            });

            MenuHelper.SafeDisplayDialog("全局Pojo字典信息", sb.ToString(), "关闭");
            HSUtils.Log(sb.ToString());
        }

        [MenuItem("Tools♥/码农专用/策划配置复位", false)]
        static void ResetDesignerToolPanel1()
        {
            XMLBDUpdater.Instance.Reset();
        }

        [MenuItem("Tools♥/码农专用/转换实际Values为TXT", false)]
        public static void ConvertCurrentValueBytes2Txt()
        {
            Directory.CreateDirectory(HSCTC.DebugPath);

            using (ProgressBarAutoHide.Get(500))
            using (HSUtils.ExeTimer("ConvertCurrentValueBytes2Txt"))
            {
                BeanDictEditor.LoadAndConvertVerbose("转换实际Values为TXT: [{0}]".f(HSCTC.ActiveValueFile.ShortName()), HSCTC.ActiveValueFile, "z_converted");
                foreach (var tag in HSCTC.ValueBundleTags)
                {
                    var valueFile = HSCTC.ActiveValueFileV2.f(tag);
                    BeanDictEditor.LoadAndConvertVerbose("转换实际Values为TXT: [{0}]".f(valueFile.ShortName()), valueFile, "z_{0}_converted".f(tag));
                }
            }

            EditorUtility.RevealInFinder(BeanDictEditor.GetXMLPath("z_converted"));
        }

        [MenuItem("Tools♥/码农专用/内存_清理", false)]
        static void ClearMem()
        {
            string old = Mini.GetUsedMem();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            EditorUtility.DisplayDialog("已经使用内存", "{0} => {1}".Eat(old, Mini.GetUsedMem()) + Environment.NewLine
                + "Unity启动时间：" + UtilsUnity.GetUnityUpTime(), "关闭");
        }

        [MenuItem("Tools♥/码农专用/删除所有XML", false)]
        static void DeleteAllXML()
        {
            if (!EditorUtility.DisplayDialog("请问", "你确定要删除所有XML吗？\r\n" + ConvertorHelper.XMLPath, "确定", "取消"))
                return;
            Mini.ClearDirectory(HSCTC.XmlPath);
            EditorUtility.DisplayDialog("成功", "已经删除所有XML", "关闭");
        }

        [MenuItem("Tools♥/码农专用/关闭进度条", false)]
        public static void CloseProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
