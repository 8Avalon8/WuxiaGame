using UnityEditor;
using HSFrameWork.ConfigTable.Editor.Impl;
using HSFrameWork.ConfigTable.Editor;
using HSFrameWork.Common;
using UnityEngine;
using GLib;
using HanSquirrel.ResourceManager;

namespace JianghuX.Editor
{
    ///所有生成数据的菜单命令的入口
    public class GenDataMenuCmd
    {
        [MenuItem("Tools♥/[GEN DATA]")]
        static void GenerateData()
        {
            using (HSUtils.ExeTimer("菜单：[GEN DATA]"))
                MenuHelper.SafeWrapMenuAction("GENERATE DATA", title => GenerateDataWithEnding(title, false));
        }

        [MenuItem("Tools♥/[GEN DATA (重载XLS)]")]
        static void GenerateDataForce()
        {
            using (HSUtils.ExeTimer("菜单：[GEN DATA (重载XLS)]"))
                MenuHelper.SafeWrapMenuAction("GENERATE DATA重新转换XLS", title => GenerateDataWithEnding(title, true));
        }

        private static void GenerateDataWithEnding(string title,  bool force)
        {
            GenPackDataAllTheWay(title, force, false);
            MenuHelper.SafeShow100Progress(title);
        }

        /// <summary>
        /// 生成Assets/StreamingAssets/目录下的[value,filter,lua]
        /// 清理无用AB包 →→→ XLS →→→ 加密压缩的 Value/filter/lua →→→ BuildCurrent
        /// </summary>
        public static void GenPackDataAllTheWay(string title, bool force, bool buildab = true)
        {
            using (HSUtils.ExeTimer("GenDataAllTheWay [{0}]".f(HSCTC.DisplayActiveLanguage)))
            {
                BeanDictMenu.GenBeanDictAllTheWay(title, force); //【Assets/StreamingAssets/value】

                using (HSUtils.ExeTimer("ZipEncFilter"))
                {
                    MenuHelper.SafeDisplayProgressBar(title, "FinalPackHelper.ZipAndEncryptFilter", 0.6f);
                    GenDataHelper.ZipAndEncryptFilter();  //【Assets/StreamingAssets/filter】
                }


                using (HSUtils.ExeTimer("AssetDatabase.Refresh"))
                {
                    MenuHelper.SafeDisplayProgressBar(title, "AssetDatabase.Refresh", 0.8f);
                    TE.RunInOrSendToUI(AssetDatabase.Refresh);
                }

                if (buildab)
                {
                    using (HSUtils.ExeTimer("AssetTool.BuildCurrent"))
                    {
                        MenuHelper.SafeDisplayProgressBar(title, "AssetTool.BuildCurrent", 0.9f);
                        TE.RunInOrSendToUI(AssetTool.BuildCurrent);
                        Debug.LogWarning("▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲ [ 成功完成BuildCurrent ] ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲".EatWithTID());
                    }
                }
                else if (ResourceLoader.LoadFromABAlways)
                {
                    HSUtils.LogWarning("当前设置永远从AB包加载。如果不打AB包，程序运行可能会异常。");
                }
            }
        }
    }
}
