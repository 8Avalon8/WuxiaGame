using GLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using HSFrameWork.Common;
using System.Reflection;
using HSFrameWork.ConfigTable.Trans;

namespace HSFrameWork.ConfigTable.Editor.Trans.Impl
{
    public class ParamSplitException : Exception
    {
        public ParamSplitException(string msg) : base(msg) { }
    }

    public class DefaultTextFinder : ITextFinder
    {
        /// <summary>
        /// 缺省的实现：ARG的格式为：第一个字符为分隔符；后面的数字为需要翻译的，从0开始。
        /// ARG从reg文件里面取得。如  need_translate="true" textfinder="Default#2,4"
        /// 例如arg="#2,4" 表示用#来分割该字段，然后翻译第2和第4个子串。
        /// </summary>
        public virtual string[] DoWork(object obj, string arg, string str, Mini.IReadOnlyEasyDict<string, string> dict, ref int transedTextCount, out string newStr)
        {
            if (!str.Visible())
            {
                newStr = null;
                return null;
            }

            string trimmed = str.Trim();

            if (arg == null || arg.Length == 0)
            {
                string result;
                if (dict.SafeTryGetValue(trimmed, out result))
                {
                    transedTextCount++;
                    newStr = str.Replace(trimmed, result);
                    return null;
                }
                else
                {
                    newStr = null;
                    return new string[] { trimmed };
                }
            }

            try
            {
                return DoWorkInner(str, arg[0], dict, ref transedTextCount, out newStr,
                    arg.Substring(1).Split(',').Where(s => s.Visible()).Select(s => Convert.ToInt32(s)).ToArray());
            }
            catch (FormatException e)
            {
                HSUtils.LogError("arg参数不合规范 {0}。".Eat(arg));
                throw e;
            }
        }

        /// <summary>
        /// 在str中存在一个小的表格：行用rowSplitTag来分割，列用colSplitTag来分割。需要翻译的列编号在textCols里面。
        /// </summary>
        protected string[] DoWorkInner(string str, Mini.IReadOnlyEasyDict<string, string> dict, ref int transedTextCount, out string newStr, char rowSplitTag, char colSplitTag, params int[] textCols)
        {
            string[] rows = str.Split(rowSplitTag);
            string[][] cells = new string[rows.Length][];
            int colCount = -1;
            for (int i = 0; i < rows.Length; i++)
            {
                cells[i] = rows[i].Split(colSplitTag);
                if (colCount == -1)
                    colCount = cells[i].Length;
                else if (colCount != cells[i].Length)
                    throw new ParamSplitException("策划BUG：字符串每行的列数不同：" + str);
            }

            bool changed = false;
            List<string> task = new List<string>();

            for (int m = 0; m < rows.Length; m++)
            {
                for (int n = 0; n < textCols.Length; n++)
                {
                    string trimmed = cells[m][textCols[n]].Trim();
                    string another;
                    if (dict.SafeTryGetValue(trimmed, out another))
                    {
                        changed = true;
                        transedTextCount++;
                        cells[m][textCols[n]] = cells[m][textCols[n]].Replace(trimmed, another);
                    }
                    else
                    {
                        task.Add(trimmed);
                    }
                }
            }

            newStr = !changed ? null : string.Join(rowSplitTag.ToString(), cells.Select(row => string.Join(colSplitTag.ToString(), row)).ToArray());
            return task.UniformG();
        }

        protected void ProcessOneBlock(string block, Mini.IReadOnlyEasyDict<string, string> dict, ref int transedTextCount, ref bool changed, StringBuilder sb, List<string> task)
        {
            string trimmed = block.Trim();
            string another;
            if (dict.SafeTryGetValue(trimmed, out another))
            {
                changed = true;
                transedTextCount++;
                sb.Append(block.Replace(trimmed, another));
            }
            else
            {
                task.Add(trimmed);
                sb.Append(block);
            }
        }

        /// <summary>
        /// 以split分割后，取indexes的子串翻译。
        /// </summary>
        /// <returns></returns>
        protected string[] DoWorkInner(string str, char split, Mini.IReadOnlyEasyDict<string, string> dict, ref int transedTextCount, out string newStr, params int[] indexes)
        {
            if (!str.Visible())
            {
                newStr = null;
                return null;
            }

            bool changed = false;
            List<string> task = new List<string>();
            StringBuilder sb = new StringBuilder();

            int currentBlock = 0;
            int i = 0, j = 0;
            while (j <= str.Length)
            {
                if (j == str.Length || str[j] == split)
                {
                    if (j - i > 0)
                    {//找到一个非空BLOCK
                        string block = str.Substring(i, j - i);
                        if (indexes == null || indexes.Length == 0 || Array.IndexOf(indexes, currentBlock) != -1)
                        {
                            //需要翻译
                            ProcessOneBlock(block, dict, ref transedTextCount, ref changed, sb, task);
                        }
                        else
                        {
                            sb.Append(block);
                        }

                    }
                    currentBlock++;

                    if (j < str.Length)
                        sb.Append(split);
                    j++;
                    i = j;
                }
                else
                {
                    j++;
                }
            }

            newStr = changed ? sb.ToString() : null;
            return task.UniformG();
        }

        /// <summary>
        /// 将obj转换为类型T，如果obj是null或者不是T，则抛出异常。
        /// </summary>
        protected T CheckParams<T>(object obj) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException("obj是null");

            T action = obj as T;
            if (action == null)
                throw new ParamSplitException("程序编写错误或者REG文件书写错误：期望类型{0}：实际类型{1}".Eat(typeof(T), obj.GetType()));
            return action;
        }

        /// <summary>
        /// 在str里面的子串个数不确定的情况下，使用该函数。
        /// minCount是最小字符快个数，如果是0表示不检查；
        /// omitted是要忽略的字符块索引，从0开始。
        /// </summary>
        protected int[] PrepareIndexes(char split, string str, int minCount, params int[] omitted)
        {
            int count = str.Split(split).Length;
            if (minCount >= 0 && count < minCount)
                throw new ParamSplitException("EXCEL文件编写错误 {0} ：参数个数不足{1}个。".Eat(str, minCount));

            List<int> ret = new List<int>();
            for (int i = 0; i < count; i++)
                ret.Add(i);

            if (omitted != null)
                foreach (int j in omitted)
                    ret.Remove(j);

            return ret.ToArray();
        }

        /// <summary>
        /// 将需要翻译的子串的索引传入即可。
        /// </summary>
        protected string[] DoWorkInner(string str, Mini.IReadOnlyEasyDict<string, string> dict, ref int transedTextCount, out string newStr, params SubStringIndex[] subs)
        {
            if (subs == null || subs.Length == 0)
            {
                newStr = null;
                return null;
            }

            bool changed = false;
            List<string> task = new List<string>();
            StringBuilder sb = new StringBuilder();
            int lastIndex = 0; //上一个不翻译的字段的开始，Inclusive
            foreach (var sub in subs)
            {
                if (sub.StartIndex > lastIndex)
                    sb.Append(str.Substring(lastIndex, sub.StartIndex - lastIndex)); //将不翻译的部分放进去

                ProcessOneBlock(sub.Take(str), dict, ref transedTextCount, ref changed, sb, task);
                lastIndex = sub.EndIndex;
            }

            if (lastIndex != str.Length)
                sb.Append(str.Substring(lastIndex)); //将尾巴不翻译的部分放进去

            newStr = changed ? sb.ToString() : null;
            return task.UniformG();
        }


    }

    public class TextFinderTeam
    {
        private Dictionary<string, ITextFinder> _finders = new Dictionary<string, ITextFinder>();

        public void RegisterTextFinder(string key, ITextFinder finder)
        {
            _finders[key] = finder;
        }

        public ITextFinder GetTextFinder(string keyORname, Assembly assembly = null)
        {
            ITextFinder st;
            if (_finders.TryGetValue(keyORname, out st))
                return st;

            Type t = assembly == null ? Type.GetType(keyORname) : assembly.GetType(keyORname);
            if (t != null)
            {
                st = Activator.CreateInstance(t) as ITextFinder;
                if (st != null)
                {
                    _finders.Add(keyORname, st);
                    return st;
                }
            }

            throw new KeyNotFoundException("程序或者REG文件书写错误：无法生成有效的TextFinder[{0}] 。".Eat(keyORname));
        }
    }
}
