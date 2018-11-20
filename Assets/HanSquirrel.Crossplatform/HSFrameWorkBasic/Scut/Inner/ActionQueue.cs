using System;
using System.Collections.Generic;
using HSFrameWork.Common;
using System.Threading;

namespace HSFrameWork.Scut.Inner
{
    public class ActionQueueSlot
    {
        public ActionQueueSlot(GameAction gameAction, ActionParam p, bool showLoading)
        {
            gameAction.ActionQueueSlot = this;
            Action = gameAction;
            Param = p;
            ShowLoading = showLoading;
        }

        public readonly GameAction Action;
        public readonly ActionParam Param;
        public readonly bool ShowLoading = false;
    }

    public partial class ActionClient
    {
        public void SendInActionQueueEx(int actionId, Action<ActionResult> callback, Action<ErrorCode, string> errorCallback, ActionParam actionParam, bool showLoading = true)
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1)
            {
                errorCallback(ErrorCode.UserCanceled, "用户关闭ActionClient");
                return;
            }

            if (_Shutdown)
            {
                errorCallback(ErrorCode.UserCanceled, "用户关闭ActionQueue");
                return;
            }

            _Logger.Trace("AddActionToQueue(Action#{0}, bShowLoading={1})", actionId, showLoading);
            _queue.Enqueue(new ActionQueueSlot(ActionFactory.Create(actionId, callback, errorCallback, Settings), actionParam, showLoading));
            _ShowLoadingCount += showLoading ? 1 : 0;

            TryDoNextAction();
        }

        /// <summary>
        /// 将一个发送请求压入处理队列中，会抛出异常。GG 原始设计如此。
        /// </summary>
        public void SendInActionQueue(int actionId, Action<ActionResult> callback, ActionParam actionParam, bool showLoading = true)
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1 || _Shutdown)
            {
                var str = "上层应用程序编写错误：ActionQueue已经被关闭，如需SendInActionQueue，请重建ActionClient";
                _Logger.Fatal(str);
                throw new Exception(str);
            }

            SendInActionQueueEx(actionId, callback, null, actionParam, showLoading);
        }

        public void ShutDownActionQueue(bool noCallback = true)
        {
            _Logger.Debug("{0} ShutDownActionQueue(), 删除 [{1}] 个Action。", URL, _queue.Count);

            if (noCallback)
            {
                _queue.Clear();
            }
            else
            {
                while (_queue.Count > 0)
                {
                    var slot = _queue.Dequeue();
                    slot.Action.OnErrorCallback(ErrorCode.UserCanceled, "ActionQueue被用户关闭");
                }
            }

            _UI.ShowLoading = false;
            _ShowLoadingCount = 0;
            _Shutdown = true;
        }

        private void TryDoNextAction()
        {
            if (_currentQueuedAction != null)
                return;

            if (_queue.Count == 0)
            {
                _UI.ShowLoading = false;
                _Logger.Trace("所有指令都已经发送完成。");
                return;
            }

            _currentQueuedAction = _queue.Dequeue();

            DoCurrentAction();
        }

        private void DoCurrentAction()
        {
            if (_currentQueuedAction == null)
            {
                _UI.ShowLoading = false;
                return;
            }

            _UI.ShowLoading = _ShowLoadingCount > 0;
            _Logger.Trace("发送Action#{0} ", _currentQueuedAction.Action.ActionId);
            Send(_currentQueuedAction.Action, _currentQueuedAction.Param, _currentQueuedAction.ShowLoading);
        }

        private bool TryPreProcessInQueue(ActionExecInfo exeInfo)
        {
            if (exeInfo.Action.ActionQueueSlot == null)
                return true;

            if (_Shutdown)
            {
                _Logger.Warn("当前ActionQueue已经关闭。被忽略，回调不会被调用。");
                return false;
            }

            if (_currentQueuedAction != exeInfo.Action.ActionQueueSlot)
            {
                _Logger.Fatal("GG程序编写错误。OnCurrentActionSuccess({0})，然而当前是 {1}。被忽略，回调不会被调用。",
                    exeInfo.Action.ActionQueueSlot.Action.DebugString, _currentQueuedAction.Action.DebugString);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 发送成功的回调
        /// </summary>
        private void OnCurrentActionSuccess()
        {
            _ShowLoadingCount -= _currentQueuedAction.ShowLoading ? 1 : 0;
            _currentQueuedAction = null;

            TryDoNextAction();
        }

        /// <summary>
        /// 发送失败的回调
        /// </summary>
        private void OnCurrentActionFailed(GameAction action, ErrorCode errorCode, string errorMsg)
        {
            if (_Shutdown)
            {
                _Logger.Warn("OnCurrentActionFailed()，然而当前ActionQueue已经关闭。被忽略，回调不会被调用。");
                return;
            }

            _Logger.Error("Net error: Action#{0} [{1}]-[{2}]", action.ActionId, errorCode, errorMsg);

            if (action.ActionQueueSlot != _currentQueuedAction)
            { //回调的和当前发的不匹配
                _Logger.Fatal("GG程序编写错误：not matched current Action!");
                return;
            }

            TryShowRequireReconnect();
        }

        //当前执行的action
        private int _ShowLoadingCount = 0;
        private bool _Shutdown = false;
        private ActionQueueSlot _currentQueuedAction = null;
        private Queue<ActionQueueSlot> _queue = new Queue<ActionQueueSlot>();
        private IHSLogger _Logger = HSLogManager.GetLogger("ActionQ");
    }
}
