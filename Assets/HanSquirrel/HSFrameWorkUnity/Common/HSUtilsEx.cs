using UnityEngine;

using GLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Threading;

namespace HSFrameWork.Common
{
    /// <summary>
    /// 一些Unity相关的工具函数
    /// </summary>
    public static class HSUtilsEx
    {
        private static int _CurrentFrameCount = -1;
        private static Stopwatch _Stopwatch = new Stopwatch();
        /// <summary>
        /// 当前Frame已经执行的时间（MS）
        /// </summary>
        public static int ElaspsedThisFrame
        {
            get
            {
                return TriggerNewFrame();
            }
        }

        /// <summary>
        /// 即使 time<=0，也会到下一帧执行。
        /// </summary>
        public static void CallWithDelay(MonoBehaviour mb, Action callback, float time)
        {
            if (callback == null)
                return;
            mb.StartCoroutine(__callWithDelay(callback, time));
        }

        private static IEnumerator __callWithDelay(Action callback, float time)
        {
            yield return time <= 0 ? null : new WaitForSeconds(time);
            callback();
        }

        /// <summary>
        /// 激活新Frame时间
        /// </summary>
        public static int TriggerNewFrame()
        {
            if (_CurrentFrameCount != Time.frameCount)
            {
                _CurrentFrameCount = Time.frameCount;
                _Stopwatch.Reset();
                _Stopwatch.Start();
                return 0;
            }
            else
            {
                return (int)_Stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Application.Quit。如果在Unity下，则停止PlayMode。
        /// </summary>
        public static void ExitApp()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private static int _AlwaysABSeq = 1;

        /// <summary>
        /// 创建一个 DontDestroyOnLoad的T
        /// </summary>
        public static T CreateAlwayMB<T>() where T : MonoBehaviour
        {
            GameObject go = new GameObject();
            var ret = go.AddComponent<T>();
            go.name = "{0}#{1}".f(typeof(T).FullName, Interlocked.Increment(ref _AlwaysABSeq));
            GameObject.DontDestroyOnLoad(go);
            return ret;
        }
    }
}
