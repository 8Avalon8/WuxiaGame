using System.Threading;
using SysTask = System.Threading.Tasks.Task;
using System;

using UnityEditor;
using System.IO;
using UnityEngine;
using GLib;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Editor.Trans.Impl;
using System.Collections.Generic;
using System.Text;
using HSFrameWork.ConfigTable.Editor.Impl;

namespace HSFrameWork.ConfigTable.Editor
{
    /// <summary>
    /// 配置表菜单实现类
    /// </summary>
    public class BeanDictMenu
    {
        ///备注：https://www.onlinedoctranslator.com/zh-CN/translationprocess

        /// <summary>
        /// 会生成所有语言的未翻译文本CSV文件。
        /// </summary>
        //[MenuItem("Tools♥/HSConfigTable/生成需翻译文本", false)]
        public static void GenerateTranslationList()
        {
            using (HSUtils.ExeTimer("菜单：[生成需翻译文本]"))
                MenuHelper.SafeWrapMenuAction("生成需翻译文本", (title) => GenerateTranslationListMenu(title, false));
        }

        private static void GenerateTranslationListMenu(string title, bool force)
        {
            GenDefaultValueBytes(title, force);
            var languageBK = HSCTC.ActiveLanguage;

            using (DisposeHelper.Create(() => { HSCTC.ActiveLanguage = languageBK; })) //异常的时候也会恢复
                HSCTC.AllLanguages.ForEach(lan =>
                {
                    HSCTC.ActiveLanguage = lan;
                    GenActiveLanguageValueFile(true);
                });
        }

        /// <summary>
        /// 会生成[CHT]未翻译文本CSV文件。
        /// </summary>
        //[MenuItem("Tools♥/HSConfigTable/生成需翻译文本CHT", false)]
        public static void GenerateTranslationList_CHT()
        {
            using (HSUtils.ExeTimer("菜单：[生成需翻译文本CHT]"))
                MenuHelper.SafeWrapMenuAction("生成需翻译文本CHT",
                (t) => GenerateTranslationListMenu_CHT(t, false));
        }

        private static void GenerateTranslationListMenu_CHT(string title, bool force)
        {
            GenDefaultValueBytes(title, force);
            var languageBK = HSCTC.ActiveLanguage;

            using (DisposeHelper.Create(() => { HSCTC.ActiveLanguage = languageBK; }))  //异常的时候也会恢复
            {
                HSCTC.ActiveLanguage = "CHT";
                GenActiveLanguageValueFile(true);
            }
        }

        public static void GenActiveLanguageValueFile(bool force)
        {
            if (HSCTC.ActiveValueFile == HSCTC.ValuesFile)
                return;

            if (HSCTC.RegV2Active)
                throw new Exception("REGV2还不支持语言包功能。");

            string title = "生成语言包 [{0}]".Eat(HSCTC.ActiveLanguage);
            MenuHelper.SafeDisplayProgressBar(title, "正在初始化", 0.0f);

            var transFiles = Directory.GetFiles(HSCTC.ActiveTranslateDir, "*.csv", SearchOption.TopDirectoryOnly);
            var sharedFiles = Directory.GetFiles(HSCTC.ActiveTranslateSharedDir, "*.csv", SearchOption.TopDirectoryOnly);
            var depFiles = new List<string>(transFiles).AddRangeG(sharedFiles).AddG(HSCTC.ValuesFile).ToArray();

            if ((!force) && FileSummary.PackedFileValid(depFiles, HSCTC.ActiveValueFile, HSCTC.ActiveValueSummaryFile))
            {
                HSUtils.LogWarning("★★★ values.bytes和语言包都没有更新，因此不需要更新{0}. ★★★★", HSCTC.ActiveValueFile.ShortName());
                MenuHelper.SafeShow100Progress(title);
                return;
            }

            using (var bde = BeanDictEditor.Create())
            {
                if (!HSCTC.KeepTransOldOutput)
                {
                    HSUtils.LogWarning("打包过程会删除掉旧的翻译任务输出文件。如需要保留，在配置目录添加文件 keeptransoldoutput");
                    HSCTC.ActiveTranslateOutputDir.ClearDirectory();
                }

                string now = DateTime.Now.ToString("MMdd-HHmm.ss-");
                string transTaskFile = HSCTC.ActiveTranslateOutputDir.StandardSub("{0}TransTask.csv".Eat(now));
                string transWarningFile = HSCTC.ActiveTranslateOutputDir.StandardSub("{0}Warning.txt".Eat(now));
                string transSumFile = HSCTC.ActiveTranslateOutputDir.StandardSub("{0}Summary.txt".Eat(now));
                TransFacade.GenFile(
                    delegate { bde.Load(HSCTC.ValuesFile); return bde; },
                    HSCTC.ActiveValueFile,
                    Directory.GetFiles(HSCTC.RegPath),
                    sharedFiles, transFiles,
                    transTaskFile, transWarningFile, transSumFile,
                    HSCTC.GenLanguagePackVerboseXML,
                    (memo, progress) => MenuHelper.SafeDisplayProgressBar(title, memo, progress));
            }

            FileSummary.WriteSummary(depFiles, HSCTC.ActiveValueFile, HSCTC.ActiveValueSummaryFile);
            MenuHelper.SafeShow100Progress(title);
        }

        [MenuItem("Tools♥/HSConfigTable/BeanDict_全部强制", false)]
        static void GenerateProtoBufForced()
        {
            using (HSUtils.ExeTimer("菜单：[BeanDict_全部强制]"))
                MenuHelper.SafeWrapMenuAction("强制更新：从XLS →→→→ 压缩加密的value",
                title => GenerateBeanDictMenu(title, true));
        }

        [MenuItem("Tools♥/HSConfigTable/BeanDict_普通更新", false)]
        static void GenerateProtoBuf()
        {
            using (HSUtils.ExeTimer("菜单：[BeanDict_普通更新]"))
                MenuHelper.SafeWrapMenuAction("一般更新：从XLS →→→→ 压缩加密的value",
                title => GenerateBeanDictMenu(title, false));
        }

        public static void GenerateProtoBufEasy(bool force)
        {
            using (HSUtils.ExeTimer("GenerateProtoBufEasy"))
                MenuHelper.SafeWrapMenuAction(force ? "强制" : "一般" + "更新：从XLS →→→→ 压缩加密的value",
                title => GenerateBeanDictMenu(title,force));
        }

        private static void GenerateBeanDictMenu(string title, bool force)
        {
            GenBeanDictAllTheWay(title, force);
            MenuHelper.SafeShow100Progress(title);
        }

        public static void GenDefaultValueBytes(string title, bool force)
        {
            using (HSUtils.ExeTimer("XlS >> Value*.bytes"))
            {
                MenuHelper.SafeDisplayProgressBar(title, "检查孤儿XML", 0.1f);
                Xls2XMLHelperWin.CheckOrphanXML();

                MenuHelper.SafeDisplayProgressBar(title, "XLS 》》XML", 0.2f);
                Xls2XMLHelperWin.SafeRunBlocked(force);
                HSUtils.Log("▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲  [ 成功完成XML生成 ] ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲".EatWithTID());

                //永远需要制作一个完整的数据包。"2"仅仅是为了在Debug目录生成临时文件，供框架开发者内部使用，下同。
                using (HSUtils.ExeTimer("XML >> {0}".f(HSCTC.ValuesFile.ShortName())))
                {
                    ValueBundleHelper.GenValueFileWithSummary(title, force,
                    new XMLBDLoader(), "2", HSCTC.ValuesFile, HSCTC.ValuesSummaryFile);
                }

                //制作子包。
                foreach (var tag in HSCTC.ValueBundleTags)
                    using (HSUtils.ExeTimer("XML >> {0}".f(HSCTC.ValuesFileV2.f(tag).ShortName())))
                    {
                        ValueBundleHelper.GenValueFileWithSummary(title, force,
                            new PartialXMLBDLoader(tag), "2_" + tag, HSCTC.ValuesFileV2.f(tag), HSCTC.ValuesSummaryFileV2.f(tag));
                    }
            }
        }

        /// <summary>
        /// 原始XLS → XML → values.bytes( values_Xml.txt + values_sum.txt ) → 最终的压缩加密文件：【Assets/StreamingAssets/value】
        /// 使用Language.txt 里面定义的当前语言
        /// </summary>
        public static void GenBeanDictAllTheWay(string title, bool force)
        {
            using (HSUtils.ExeTimer("GenBeanDictAllTheWay"))
            {
                GenDefaultValueBytes(title, force);

                if (HSCTC.ActiveLanguage != null)
                {
                    GenActiveLanguageValueFile(force);
                    File.Copy(HSCTC.LanguageCodeFile, HSCTC.OutputLanguageCodeFile, true);
                }
                else
                {
                    HSCTC.OutputLanguageCodeFile.DeleteWithMeta();
                }

                if (!force && IsCEValueFileValid() && IsCEValueV2FileValid())
                {
                    HSUtils.Log("★★★ {0} 没有更新，因此不需要更新values ★★★★", HSCTC.ActiveValueFile);
                }
                else
                {
                    File.Delete(HSCTC.LastCompressMode);
                    File.Delete(HSCTC.LastLanCode);

                    ValueBundleHelper.GenValueCEFile(title, HSCTC.ActiveValueFile, HSCTC.CEValues);

                    //加密压缩子包。
                    foreach (var tag in HSCTC.ValueBundleTags)
                        ValueBundleHelper.GenValueCEFile(title, 
                            HSCTC.ActiveValueFileV2.f(tag), HSCTC.CEValuesV2.f(tag));

                    if (HSCTC.ActiveLanguage != null)
                        File.WriteAllText(HSCTC.LastLanCode, HSCTC.ActiveLanguage);
                }
            }
        }

        private static bool IsLanguageSameAsLast()
        {
            if (HSCTC.ActiveLanguage == null)
                return !HSCTC.LastLanCode.ExistsAsFile();
            else
                return Mini.DefaultOnException(false, () => File.ReadAllText(HSCTC.LastLanCode) == HSCTC.ActiveLanguage);
        }

        private static bool IsCEValueFileValid()
        {
            try
            {
                if (HSCTC.ActiveCompressMode == ProtoBufTools.Deserialize<HSCTC.CompressMode>(File.ReadAllBytes(HSCTC.LastCompressMode)) &&
                    IsLanguageSameAsLast())
                {
                    return Mini.DefaultOnException(false, () => HSCTC.ActiveValueFile.LastWriteTime() < HSCTC.CEValues.LastWriteTime());
                }
            }
            catch
            {
            }
            return false;
        }

        private static bool IsCEValueV2FileValid()
        {
            foreach (var tag in HSCTC.ValueBundleTags)
            {
                if (!Mini.DefaultOnException(false, () => HSCTC.ActiveValueFileV2.f(tag).LastWriteTime() < HSCTC.CEValuesV2.f(tag).LastWriteTime()))
                    return false;
            }
            return true;
        }

        public static void ClearTarget()
        {
            File.Delete(HSCTC.CEValues);
            File.Delete(HSCTC.LastCompressMode);
        }
    }
}
