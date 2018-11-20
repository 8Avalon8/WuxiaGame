using GLib;
using System.Linq;
using System.Collections.Generic;
using HSFrameWork.Common;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace HSFrameWork.ConfigTable.Editor.Trans.Impl
{
    public class InvalideCsvFileException : Exception
    {
        public InvalideCsvFileException(string msg) : base(msg) { }
    }

    /// <summary>
    /// 存储语言包字典
    /// </summary>
    public class ResultArchive
    {
        public Dictionary<string, string> SharedDict = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();
        public int ErrorEntryCount { get; private set; }
        public int WarnEntryCount { get; private set; }

        public Mini.IReadOnlyEasyDict<string, string> GetDict(string title)
        {
            Dictionary<string, string> d;
            Dict.TryGetValue(title, out d);
            if (d != null)
                return new Mini.ReadOnlyMultiDict<string, string>(SharedDict, d);
            else
                return new Mini.ReadOnlyMultiDict<string, string>(SharedDict);
        }

        public void GenSumFile(string sumFile)
        {
            Dictionary<string, string> globalDict = new Dictionary<string, string>();
            foreach (var dc in Dict.Values)
                foreach (var k in dc.Keys)
                    globalDict[k] = dc[k];

            Dictionary<string, int> textCount = new Dictionary<string, int>();
            int total = 0;
            int unqiue = Dict.Values.SelectMany(x => x.Keys).Select(x =>
            {
                int c;
                if (textCount.TryGetValue(x, out c))
                    textCount[x] = c + 1;
                else
                    textCount[x] = 1;

                total++;
                return x;
            }).Distinct().Count();

            HSUtils.LogSuccess("总共{0}, 不重复{1}，重复{2}", total, unqiue, total - unqiue);

            using (var sw = new CsvFileWriter(sumFile))
            {
                List<string> row = new List<string> { "", "", "" };
                textCount.Keys.ToList().SortC(new Comparison<string>((i1, i2) =>
                {
                    int cp = textCount[i2].CompareTo(textCount[i1]);
                    return cp != 0 ? cp : i1.Length.CompareTo(i2.Length);
                })).ForEach(k =>
                {
                    row[0] = k;
                    row[1] = globalDict[k];
                    row[2] = textCount[k].ToString();
                    sw.WriteRow(row);
                });
            }
            HSUtils.LogSuccess(sumFile);
        }

        public ResultArchive(IEnumerable<string> sharedFiles, IEnumerable<string> resultFiles, string warnFile)
        {
            using (var sw = File.AppendText(warnFile))
            {
                sharedFiles.OrderBy(fi => fi.LastWriteTime()).ToList().ForEach(fi => LoadSharedFile(fi, sw));
                resultFiles.OrderBy(fi => fi.LastWriteTime()).ToList().ForEach(fi => LoadFile(fi, sw));

                if (ErrorEntryCount != 0)
                    HSUtils.LogError("配置表语言包中存在错误，共 [{0}] 条。详细请看{1}。", ErrorEntryCount, warnFile);
                if (WarnEntryCount != 0)
                    HSUtils.LogWarning("配置表语言包中存在重复的句子，共 [{0}] 条。详细请看{1}。", WarnEntryCount, warnFile);
            }
        }

        private void LoadSharedFile(string sharedFile, StreamWriter sw)
        {
            List<string> cells = new List<string>();
            using (CsvFileReader reader = new CsvFileReader(sharedFile))
            {
                if (!reader.ReadRow(cells))
                {
                    sw.WriteLine("{0}里面没有数据。".Eat(sharedFile));
                    return;
                }
                Utils.RemoveEmptyEnds(cells);

                if (!cells.SequenceEqual(new string[]
                    { "勿动第一行", "全局字典", "こんにちは", "안녕하세요", "轻舟一叶下扬州", "輕舟一葉下揚州" }))
                    throw new InvalideCsvFileException("共享词库 [{0}] 第一行数据不对".Eat(sharedFile));

                int warnCount = 0;
                int currentRow = 1;
                while (reader.ReadRow(cells))
                {
                    currentRow++;
                    Utils.RemoveEmptyEnds(cells);
                    if (cells.Count == 0)
                        continue;
                    if (cells.Count == 1)
                        throw new InvalideCsvFileException("{0}的第{1}行只有一个数据。".Eat(sharedFile, currentRow));
                    if (cells.Count > 3)
                        throw new InvalideCsvFileException("{0}的第{1}行数据过多。".Eat(sharedFile, currentRow));

                    Action TryWriteTitle = delegate 
                    {
                        if (warnCount++ == 0)
                            sw.WriteLine("文件[{0}]中存在错误或者重复。", sharedFile);
                    };

                    string org = cells[0].Trim().Replace("\r\n", "\n");
                    string other = cells[1].Trim().Replace("\r\n", "\n");
                    if (!IsSpecialTextValid(org, other))
                    {
                        TryWriteTitle();
                        ErrorEntryCount++;
                        sw.WriteLine("\t\t★★★★控制字符错误：[{0}] >> [{1}]".Eat(org, other));
                    }
                    else
                    {
                        string old;
                        if (SharedDict.TryGetValue(org, out old) && !old.Equals(other))
                        {
                            TryWriteTitle();
                            sw.WriteLine("\tDuplicate：[{0}] >> [{1}] → [{2}]".Eat(org, old, other));
                        }
                        SharedDict[org] = other;
                    }
                }
                WarnEntryCount += warnCount;
            }
        }

        private void LoadFile(string resultFile, StreamWriter sw)
        {
            int warnCount = 0;

            string lastWarnTitle = null;
            string currentTitle = null;
            Action TryWriteWarnTitile = () =>
                {
                    if (warnCount++ == 0)
                        sw.WriteLine("文件[{0}]中存在错误或者重复。", resultFile);

                    if (!currentTitle.Equals(lastWarnTitle))
                    {
                        lastWarnTitle = currentTitle;
                        sw.Write("\t");
                        sw.WriteLine(lastWarnTitle);
                    }
                };

            int currentRow = 0;
            Dictionary<string, string> currentDict = null;
            List<string> cells = new List<string>();
            using (CsvFileReader reader = new CsvFileReader(resultFile))
            {
                while (reader.ReadRow(cells))
                {
                    currentRow++;
                    Utils.RemoveEmptyEnds(cells);
                    if (cells.Count == 0)
                        continue;
                    if (cells.Count == 1)
                    {
                        currentTitle = cells[0].Trim().Replace("\r\n", "\n");
                        if (!IsTitleValid(currentTitle))
                        {
                            throw new InvalideCsvFileException("[{0}] 不是有效的翻译文件，Title : [{1}]".Eat(resultFile, currentTitle));
                        }

                        if (Dict.TryGetValue(currentTitle, out currentDict))
                        {
                            //TryWriteWarnTitile(); //GG 20180606 忘记了原来这里为何要加一行
                        }
                        else
                        {
                            currentDict = new Dictionary<string, string>();
                            Dict.Add(currentTitle, currentDict);
                        }
                        continue;
                    }
                    else if (cells.Count == 2)
                    {
                        if (cells[0].Visible())
                        {
                            throw new InvalideCsvFileException("文件 [{0}] 格式有误：第{1}行的第一列非空: [{2}]。".Eat(resultFile, currentRow, cells[0]));
                        }
                    }
                    else if (cells.Count == 3)
                    {
                        if (currentDict == null)
                            throw new InvalideCsvFileException("文件 [{0}] 格式有误：memo之前有value [{1}].".Eat(resultFile, cells[1]));
                        if (cells[0].Visible())
                            throw new InvalideCsvFileException("文件 [{0}] 格式有误：词条第一列应该为空 [{1}]".Eat(resultFile, cells[0]));

                        string org = cells[1].Trim().Replace("\r\n","\n");
                        string other = cells[2].Trim().Replace("\r\n", "\n");
                        if (!IsSpecialTextValid(org, other))
                        {
                            TryWriteWarnTitile();
                            ErrorEntryCount++;
                            sw.WriteLine("\t\t★★★★控制字符错误：[{0}] >> [{1}]".Eat(org, other));
                        }
                        else
                        {
                            string old;
                            if (currentDict.TryGetValue(org, out old) && !old.Equals(other))
                            {
                                TryWriteWarnTitile();
                                sw.WriteLine("\t\tDuplicate：[{0}] >> [{1}] → [{2}]".Eat(org, old, other));
                            }
                            currentDict[org] = other;
                        }
                    }
                    else
                    {
                        throw new InvalideCsvFileException("[{0}] 不是有效的翻译文件，第[{1}]行存在过多数据。".Eat(resultFile, currentRow));
                    }
                }
            }
            WarnEntryCount += warnCount;
        }

        private bool IsTitleValid(string title)
        {
            int i = title.LastIndexOf('#') + 1;
            if (i == 0 || (title.Length - i != 32))
                return false;

            for (i++; i < title.Length; i++)
            {
                if (title[i] >= '0' && title[i] <= '9')
                    continue;
                if (title[i] >= 'A' && title[i] <= 'F')
                    continue;
                return false;
            }
            return true;
        }

        private bool MatchCollectionEqual(Regex rgx, string t1, string t2, params int[] groups)
        {
            var m1 = rgx.Matches(t1);
            var m2 = rgx.Matches(t2);

            if (m1.Count != m2.Count)
                return false;

            for (int i = 0; i < m1.Count; i++)
            {
                if (groups.Length == 0)
                {
                    if (m1[i].Value != m2[i].Value)
                        return false;
                }
                else
                {
                    for (int j = 0; j < groups.Length; j++)
                    {
                        if (groups[j] >= m1[i].Groups.Count)
                        {
                            throw new Exception("George编程错误。");
                        }

                        if (m1[i].Groups[groups[j]].Value != m2[i].Groups[groups[j]].Value)
                            return false;
                    }
                }
            }
            return true;
        }

        // Z_资源_词条
        //降低受到暴击概率{0:F1}%
        //每回合有{3:0}%的概率{0}(等级{1},持续{2}回合)
        private Regex _formatTagRgx = new Regex("{.+?}%?");
        //Z资源_小贴士.xlsx
        //胜利：{0}场\n失败：{1}场\n防御胜利：{2}场\n防御失败：{3}场\n

        private Regex _splitTagRgx = new Regex(@"\\n|\n|#|\|");
        // "\n" @"\n" "#" "|"

        private Regex _htmlTagRgx = new Regex("<.+?>");
        //下次解锁需要的侠客令碎片<color=red>数目翻倍</color>

        private Regex _labelTagRgx = new Regex(@"\$[A-Z_]+\$");
        //我们不能倒在这里呀！$MALE$，加油，加油！

        private Regex _unityColorTagRgx = new Regex(@"(\[\[[a-zA-Z]+:).+?\]\]");
        //等团队中有成员的[[yellow:身法]]好一点再来试吧。

        private bool IsSpecialTextValid(string t1, string t2)
        {
            return
                MatchCollectionEqual(_formatTagRgx, t1, t2) &&
                MatchCollectionEqual(_splitTagRgx, t1, t2) &&
                MatchCollectionEqual(_htmlTagRgx, t1, t2) &&
                MatchCollectionEqual(_labelTagRgx, t1, t2) &&
                MatchCollectionEqual(_unityColorTagRgx, t1, t2, 1); //仅仅匹配 [[yellow:
        }

#if HSFRAMEWORK_DEV_TEST
        private ResultArchive()
        {
        }

        public static void SelfTest()
        {
            var ra = new ResultArchive();

            HSUtils.Assert(ra.MatchCollectionEqual(ra._formatTagRgx, "对指定玩家造成{0}点伤害，己方遗失手牌{1}张", "對指定玩家造成{0}點傷害，己方遺失手牌{1}張"));
            HSUtils.Assert(ra.IsSpecialTextValid("对指定玩家造成{0}点伤害，己方遗失手牌{1}张",
                                                 "對指定玩家造成{0}點傷害，己方遺失手牌{1}張"));


            HSUtils.Assert(ra.MatchCollectionEqual(ra._formatTagRgx, "降低受到暴击概率{0:F1}%", "降低受到暴擊概率{0:F1}%"));
            HSUtils.Assert(!ra.MatchCollectionEqual(ra._formatTagRgx, "降低受到暴击概率{0:F1}%", "降低受到暴擊概率{0 :F1}%"));
            HSUtils.Assert(!ra.MatchCollectionEqual(ra._formatTagRgx, "降低受到暴击概率{0:F1}%", "降低受到暴擊概率{0:F1} %"));
            HSUtils.Assert(!ra.MatchCollectionEqual(ra._formatTagRgx, "降低受到暴击概率{0:F1}%", "降低受到暴擊概率{0:F1}"));

            HSUtils.Assert(ra.MatchCollectionEqual(ra._splitTagRgx, "胜利：{0}场\n失败：{1}场\n防御胜利：{2}场\n防御失败：{3}场\n",
                                                                    "勝利：{0}場\n失敗：{1}場\n防禦勝利：{2}場\n防禦失敗：{3}場\n"));
            HSUtils.Assert(!ra.MatchCollectionEqual(ra._splitTagRgx, "胜利：{0}场\n失败：{1}场\n防御胜利：{2}场\n防御失败：{3}场\n",
                                                                     "勝利：{0}場失敗：{1}場\n防禦勝利：{2}場\n防禦失敗：{3}場\n"));

            HSUtils.Assert(ra.MatchCollectionEqual(ra._splitTagRgx, @"胜利：{0}场\n失败：{1}场\n防御胜利：{2}场\n防御失败：{3}场\n",
                                                        @"勝利：{0}場\n失敗：{1}場\n防禦勝利：{2}場\n防禦失敗：{3}場\n"));
            HSUtils.Assert(!ra.MatchCollectionEqual(ra._splitTagRgx, @"胜利：{0}场\n失败：{1}场\n防御胜利：{2}场\n防御失败：{3}场\n",
                                                        @"勝利：{0}場\n失敗：{1}場\n防禦勝利：{2}場\ n防禦失敗：{3}場\n"));

            HSUtils.Assert(ra.MatchCollectionEqual(ra._splitTagRgx, "我的江湖|作曲：子尹#编曲：音尚月（YSY Music）#监制：cg|",
                                                                    "我的江湖|作曲：子尹#編曲：音尚月（YSY Music）#監制：cg|"));

            HSUtils.Assert(ra.MatchCollectionEqual(ra._splitTagRgx, "我的江湖|作曲：子尹#编曲：音尚月（YSY Music）#监制：cg|",
                                                                    "我 的 江湖|作 曲：子尹#編 曲：音尚月（YSY Music）#監 制：cg|"));

            HSUtils.Assert(!ra.MatchCollectionEqual(ra._splitTagRgx, "我的江湖|作曲：子尹#编曲：音尚月（YSY Music）#监制：cg|",
                                                                     "我的江湖作曲：子尹#編曲：音尚月（YSY Music）#監制：cg|"));

            HSUtils.Assert(!ra.MatchCollectionEqual(ra._splitTagRgx, "我的江湖|作曲：子尹#编曲：音尚月（YSY Music）#监制：cg|",
                                                                     "我的江湖|作曲：#子尹#編曲：音尚月（YSY Music）#監制：cg|"));

            HSUtils.Assert(ra.IsSpecialTextValid("", ""));
            HSUtils.Assert(ra.IsSpecialTextValid("<color=red>海岛冒险</color>模式已经开放！可以前往<color=yellow>营地</color>",
                                                 "<color=red>海島冒險</color>模式已經開放！可以前往<color=yellow>營地</color>"));
            HSUtils.Assert(!ra.IsSpecialTextValid("<color= red>海岛冒险</color>模式已经开放！可以前往<color=yellow>营地</color>",
                                                  "<color=red>海島冒險</color>模式已經開放！可以前往<color=yellow>營地</color>"));

            HSUtils.Assert(ra.IsSpecialTextValid("(语气森然，目光直瞪$MALE$)你说，将门都是垃圾？",
                                                 "(語氣森然，目光直瞪$MALE$)你說，將門都是垃圾？"));
            HSUtils.Assert(!ra.IsSpecialTextValid("(语气森然，目光直瞪$MALE$)你说，将门都是垃圾？",
                                                  "(語氣森然，目光直瞪$MALE $)你說，將門都是垃圾？"));

            HSUtils.Assert(ra.IsSpecialTextValid("就这么办，现在就[[yellow:去大将军府]]吧！",
                                                 "就這麽辦，現在就[[yellow:去大將軍府]]吧！"));
            HSUtils.Assert(ra.IsSpecialTextValid("就这么办，现在就[[yellow:去大将军府]]吧！",
                                                 "就這麽辦，現在就[[yellow:  去大將軍府]]吧！"));

            HSUtils.Assert(ra.IsSpecialTextValid("迎面战斗（推荐等级：[[yellow:43]]）",
                                                 "迎面戰鬥（推薦等級：[[yellow:43]]）"));
            HSUtils.Assert(ra.IsSpecialTextValid("迎面战斗（推荐等级：[[yellow:43]]）",
                                                 "迎面戰鬥（推薦等級：[[yellow: 43]]）"));


            HSUtils.Assert(!ra.IsSpecialTextValid("就这么办，现在就[[yellow:去大将军府]]吧！",
                                                 "就這麽辦，現在就[[ yellow:  去大將軍府]]吧！"));

            HSUtils.Assert(!ra.IsSpecialTextValid("就这么办，现在就[[yellow:去大将军府]]吧！",
                                                 "就這麽辦，現在就[[red:  去大將軍府]]吧！"));

            HSUtils.Assert(!ra.IsSpecialTextValid("就这么办，现在就[[yellow:去大将军府]]吧！",
                                                 "就這麽辦，現在就[[yellow:去大將軍府]]吧[[yellow:去大將軍府]]！"));

            HSUtils.ExceptException<Exception>("", () => 
            {
                ra.MatchCollectionEqual(ra._unityColorTagRgx, 
                                        "迎面战斗（推荐等级：[[yellow:43]]）",
                                        "迎面戰鬥（推薦等級：[[yellow:43]]）", 2);
            });
        }
#endif
    }
}