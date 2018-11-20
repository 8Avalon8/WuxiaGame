using GLib;
using System.IO;
using UnityEngine;
using HSFrameWork.Common;
using HSFrameWork.Common.Editor;
using HSFrameWork.ConfigTable.Editor;
using HSFrameWork.SPojo.TestCase;

namespace HSFrameWork.SPojo.Editor.Inner
{
    /// <summary>
    /// 如果游戏在Editor里面运行，则会初始化SaveablePojo调试日志的相关信息。
    /// </summary>
    public static class SPojoLogger
    {
        public static void ColdBind()
        {
            EditorPlayMode.PlayModeChanged += OnPlayModeChanged;
            RunTimeDataDetailLog.Init(Application.dataPath.StandardSub("../data/Debug"), File.Exists(RuntimeDataLogTagFile));
        }

        private static void OnPlayModeChanged(PlayModeState currentState, PlayModeState changedState)
        {
            if (currentState == PlayModeState.Stopped && changedState == PlayModeState.Playing)
            {
                RunTimeDataDetailLog.ResetFileNames();
            }
        }

        public static readonly string RuntimeDataLogTagFile = HSCTC.ConfigPath.StandardSub("runtimedatalog");
    }
}
