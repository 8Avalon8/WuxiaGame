using System;
using System.Threading.Tasks;

using UnityEngine;

using HSFrameWork.XLS2XML;
using GLib;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Editor.Impl;

namespace HSFrameWork.ConfigTable.Editor
{
    /// <summary>
    /// 将XLS转换为XML的工具函数
    /// </summary>
    public static class Xls2XMLHelperWin
    {
        /// <summary>
        /// 在RobotMode下，或者TE.DisableSysUI下，会自动删除。
        /// </summary>
        public static void CheckOrphanXML()
        {
            //这里面不需要TRYCatch是因为可以直接穿透给等待该Task结束的调用者
            List<string> orphans = HSCTC.RegV2Active ?
                OrphanXMLDelector.CheckV2(ConvertorHelper.V2IndexFile, ConvertorHelper.XMLPath) :
                OrphanXMLDelector.Check(ConvertorHelper.RegPath, ConvertorHelper.XMLPath);
            if (orphans.Count == 0)
            {
                HSUtils.Log("没有OrphanXML.");
            }
            else
            {
                if (MenuHelper.SafeDisplayDialog("发现{0}孤儿XML{1}".Eat(orphans.Count, Environment.NewLine),
                    "是否删除如下XML？" + string.Join(Environment.NewLine, orphans.ToArray()), "!!删除!!", "不删除"))
                {
                    orphans.Select(name => Path.Combine(ConvertorHelper.XMLPath, name)).ToList().ForEach(fullPath =>
                     {
                         Debug.LogWarning("删除孤儿XML：" + fullPath);
                         File.Delete(fullPath);
                     });
                }
                else
                {
                    Debug.LogWarning("发现了{0}个OrphanXML，用户没有选择删除：".Eat(orphans.Count));
                    orphans.ForEach(Debug.Log);
                }
            }
        }

        /// <summary>
        /// RobotMode可用，NOTHREADEXTENTION可用
        /// </summary>
        public static void SafeRunBlocked(bool force)
        {
            if (TE.NoThreadExtention)
            {
                ConvertorHelper.RunBlockedSingleThreaded(force,
                    () => MenuHelper.SafeDisplayProgressBar("XLS2XML",
                    ConvertorHelper.ProgressDesc, ConvertorHelper.Progress));
            }
            else
            {
                SafeCreateAndDisplay(force, TimeSpan.FromSeconds(0.5)).Wait();
            }
        }

        /// <summary>
        /// RobotMode可用，需要等待；NOTHREADEXTENTION不可用
        /// </summary>
        private static Task SafeCreateAndDisplay(bool force, TimeSpan delayClose)
        {
            TE.CheckIfFrozen();

            Task task = ConvertorHelper.SafeCreate(force, CancellationToken.None);
            return TE.RunInPool(() =>
            {
                try
                {
                    while (!task.Wait(100))
                    {
                        MenuHelper.SafeDisplayProgressBar("XLS2XML", ConvertorHelper.ProgressDesc, ConvertorHelper.Progress);
                    }
                }
                catch { } // Xls2XMLUTHelper里面有全部状态。

                MenuHelper.SafeDisplayProgressBar("XLS2XML", ConvertorHelper.ProgressDesc, ConvertorHelper.Progress);

                if (task.Status == TaskStatus.Faulted)
                    throw new Exception("XLS2XML出现异常");
                else if (task.Status == TaskStatus.Canceled)
                    throw new OperationCanceledException();
            });
        }
    }
}
