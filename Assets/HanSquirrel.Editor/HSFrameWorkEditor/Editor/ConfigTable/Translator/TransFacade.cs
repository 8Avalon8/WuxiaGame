using GLib;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using HSFrameWork.ConfigTable.Trans;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Editor.Impl;

namespace HSFrameWork.ConfigTable.Editor.Trans.Impl
{
    public static class TransFacade
    {
        private static Func<List<KeyValuePair<string, ITextFinder>>> _getTextFindersFun;

        /// <summary>
        /// 在用的时候才会去调用该函数
        /// </summary>
        public static void RegisterTextFindersForward(Func<List<KeyValuePair<string, ITextFinder>>> func)
        {
            _getTextFindersFun = func;
        }

        /// <summary>
        /// dstDataFile是目标的ProtoBuf文件；
        /// regFiles是REG定义XML文件；
        /// sharedFiles是共享的全局翻译结果文件。
        /// transFiles是翻译结果文件；
        /// taskFile是新发现尚未翻译的任务文件。
        /// warnFile 是处理过程中的警告信息文件；
        /// sumFile 是翻译结果文件加载的统计信息文件；
        /// </summary>
        public static void GenFile(Func<BeanDictEditor> getBDE, string dstDataFile, IEnumerable<string> regFiles,
            IEnumerable<string> sharedFiles, IEnumerable<string> transFiles, string taskFile,
            string warnFile, string sumFile, bool genValueFileXML, Action<string, float> onProgress, Action<TextFactory, ResultArchive, BeanDictEditor> afterSetup = null)
        {
            onProgress("正在加载翻译结果", 0.0f);
            ResultArchive ra;
            using (HSUtils.ExeTimer("加载翻译结果"))
                ra = new ResultArchive(sharedFiles, transFiles, warnFile);

            onProgress("正在建造工厂", 0.1f);
            TextFactory tf = TextFactoryBuilder.DoWork(regFiles, _getTextFindersFun, onProgress);


            ra.GenSumFile(sumFile);

            onProgress("正在加载原始BeanDict", 0.2f);
            BeanDictEditor bde = getBDE();


            if (afterSetup != null)
            {
                afterSetup(tf, ra, bde);
            }

            int transedTextCount = 0;
            int trasTaskCount = 0;
            List<string> csvBuf = new List<string>();
            csvBuf.Add("");
            csvBuf.Add("");
            float last = -100.0f;
            using (var csvw = new CsvFileWriter(taskFile, Encoding.UTF8))
                bde.Visit((bean, p, i, c) =>
                {
                    if (p - last >= 0.01f)
                    {
                        onProgress("正在处理{0} : {1}".Eat(bean.GetType(), bean.PK), p);
                        last = p;
                    }

                    tf.ProcessBean(bean, ra, ref transedTextCount, ref trasTaskCount, (string title, string[] blocks) =>
                    {
                        if (blocks == null || blocks.Length == 0)
                            return;

                        csvw.WriteRow(title); //story[2017圣诞快乐].action#value#AB95C81A830FA30F2F798DFBB07DDFF8

                        foreach (var text in blocks)
                        {
                            csvBuf[1] = text; //简体中文字符串
                            csvw.WriteRow(csvBuf);//,简体中文字符串
                        }
                    });
                    i++;
                });

            bde.Save5Verbose("翻译后的字典", HSCTC.ActiveLanguage, 0.0f, 1.0f, true, false, false, genValueFileXML);
            File.Copy(BeanDictEditor.GetBinPath(HSCTC.ActiveLanguage), HSCTC.ActiveValueFile, true);

            HSUtils.LogWarning("▲▲▲▲▲▲▲▲语言 [{0}]：翻译 [{1}] 条，尚未翻译 [{2}] 条。▲▲▲▲▲▲", HSCTC.ActiveLanguage, transedTextCount, trasTaskCount);
        }
    }
}
