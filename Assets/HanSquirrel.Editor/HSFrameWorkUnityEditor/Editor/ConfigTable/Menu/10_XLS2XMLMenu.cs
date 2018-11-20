using System.Threading;
using SysTask = System.Threading.Tasks.Task;
using System;

using UnityEditor;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Editor.Impl;

namespace HSFrameWork.ConfigTable.Editor
{
    /// <summary>
    /// XLS转换为XML的菜单
    /// </summary>
    public class XLS2XMLMenu
    {
        /// <summary>
        /// 菜单 Tools♥/HSConfigTable/XML_全部强制
        /// </summary>
        [MenuItem("Tools♥/HSConfigTable/XML_全部强制", false)]
        public static void GenerateAllXML()
        {
            using (HSUtils.ExeTimer("菜单: [HSConfigTable/XML_全部强制]"))
                MenuHelper.SafeWrapMenuAction("XML_全部强制", title =>
                            Xls2XMLHelperWin.SafeRunBlocked(true));
        }

        /// <summary>
        /// 菜单  Tools♥/HSConfigTable/XML_普通更新
        /// </summary>
        [MenuItem("Tools♥/HSConfigTable/XML_普通更新", false)]
        public static void GenerateXML()
        {
            using (HSUtils.ExeTimer("菜单: [HSConfigTable/XML_普通更新]"))
                MenuHelper.SafeWrapMenuAction("XML_普通更新", title =>
                            Xls2XMLHelperWin.SafeRunBlocked(false));
        }

        /// <summary>
        /// 菜单 Tools♥/HSConfigTable/XML_检查孤儿
        /// </summary>
        [MenuItem("Tools♥/HSConfigTable/XML_检查孤儿", false)]
        public static void CheckOrphanXML()
        {
            using (HSUtils.ExeTimer("菜单: [HSConfigTable/XML_检查孤儿]"))
                MenuHelper.SafeWrapMenuAction("检查孤儿XML", CheckOrphanXMLInner);
        }

        private static void CheckOrphanXMLInner(string title)
        {
            MenuHelper.SafeDisplayProgressBar(title, "正在检查", 0.1f);
            Xls2XMLHelperWin.CheckOrphanXML();
            MenuHelper.SafeShow100Progress(title);
        }
    }

}
