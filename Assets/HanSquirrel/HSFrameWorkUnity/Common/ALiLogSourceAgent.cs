#if HSFRAMEWORK_ALIYUN_LOG

using GLib;
using System;
using UnityEngine;

namespace HSFrameWork.Common
{
    /// <summary>
    /// 阿里云日志中的Source的获取接口。
    /// </summary>
    public class UnityALiCloudLogSourceAgent : SingletonI<UnityALiCloudLogSourceAgent, IALiCloudLogSourceAgent>, IALiCloudLogSourceAgent
    {
        /// <summary>
        /// 初始化Source
        /// </summary>
        public static void ColdBind()
        {
            Instance.GetSource();
        }

        /// <summary>
        /// 设置Source
        /// </summary>
        public static void SetSource(string source)
        {
            _Source = source;
            PlayerPrefs.SetString("ALiCloudLogSource", _Source);
            if (source.Visible())
                Debug.LogFormat("AliYun日志使用的Source为：{0}", _Source);
        }

        private static string _Source = null;
        string IALiCloudLogSourceAgent.GetSource()
        {
            if (_Source != null)
                return _Source;

            _Source = PlayerPrefs.GetString("ALiCloudLogSource");
            if (_Source.NullOrWhiteSpace())
            {
                _Source = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("ALiCloudLogSource", _Source);
            }

            Debug.LogFormat("AliYun日志使用的Source为：{0}", _Source);
            return _Source;
        }

        string IALiCloudLogSourceAgent.GetGameName()
        {
            return HSUnityEnv.ProductName;
        }
    }
}
#endif