using System;
using HSFrameWork.Common;
using System.Collections.Generic;

namespace HSFrameWork.Scut.Inner
{
    /// <summary>
    /// 游戏Action处理工厂
    /// </summary>
    public abstract class ActionFactory
    {
        private static readonly IHSLogger _Logger = HSLogManager.GetLogger("Scut");
        private static readonly Dictionary<int, Type> _ActionTypeDict = new Dictionary<int, Type>();

        /// <summary>
        /// 如果找不到这个Action，则抛出异常。
        /// </summary>
        public static GameAction Create(int actionId, Action<ActionResult> callBack, Action<ErrorCode, string> errorCallback, IActionClientSettings settings)
        {
            Type type = null;
            lock (_ActionTypeDict)
            {
                if (!_ActionTypeDict.TryGetValue(actionId, out type))
                {
                    var actionTypeName = string.Format(settings.ActionTypeFormat, actionId);
                    try
                    {
                        type = settings.ActionAssembly.GetType(actionTypeName);
                    }
                    catch (Exception ex)
                    {
                        _Logger.Error(ex, "无法找到类型 [{0}]", actionTypeName);
                    }
                    _ActionTypeDict.Add(actionId, type);
                }
            }

            if (type == null)
                throw new ArgumentException("无法创建实例。无法找到类型 [{0}]",
                    string.Format(settings.ActionTypeFormat, actionId));

            try
            {
                var ret = Activator.CreateInstance(type) as GameAction;
                if (callBack != null)
                    ret.Callback = callBack; //因为有些Action在构造函数里会设置缺省的callback
                if (errorCallback != null)
                    ret.ErrorCallback = errorCallback;
                ret.Settings = settings;
                return ret;
            }
            catch (Exception ex)
            {
                _Logger.Error(ex, "无法创建类型 [{0}]", type);
                throw new ArgumentException("无法创建实例。无法找到类型 [{0}]", type.FullName);
            }
        }
    }
}
