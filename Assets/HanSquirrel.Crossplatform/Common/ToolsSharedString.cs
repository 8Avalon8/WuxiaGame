using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HSFrameWork.Common
{
    public partial class ToolsShared
    {
        public static string[] chineseNumber = new String[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十", "二十一", "二十二", "二十三", "二十四", "二十五", "二十六", "二十七", "二十八", "二十九", "三十", "三十一" };
        public static char[] chineseTime = new char[] { '子', '丑', '寅', '卯', '辰', '巳', '午', '未', '申', '酉', '戌', '亥' };
        public static bool IsChineseTime(System.DateTime t, char time)
        {
            var index = (int)(t.Hour / 2);
            if (index < 0 || index >= chineseTime.Length) return false;
            return chineseTime[index] == time;
        }

        /// <summary>
        /// 时间转中文
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string DateToString(DateTime date)
        {
            string str = chineseNumber[date.Year] + "年" +
                chineseNumber[date.Month] + "月" +
                chineseNumber[date.Day] + "日";
            return str;
        }

        public static string DateToString(String dateStr)
        {
            DateTime t = DateTime.MinValue;
            try
            {
                t = DateTime.Parse(dateStr);
            }
            catch
            {
                t = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
            }
            return DateToString(t);
        }

        #region 字符串操作
        /// <summary>
        /// 给字符串分成多行
        /// </summary>
        public static string StringToMultiLine(string content, int lineLength, string enterFlag = "\n")
        {
            string rst = "";
            string tmp = content;
            while (tmp.Length > 0)
            {
                if (tmp.Length > lineLength)
                {
                    string line = tmp.Substring(0, lineLength);
                    tmp = tmp.Substring(lineLength, tmp.Length - lineLength);
                    rst += line + "\n";
                }
                else
                {
                    rst += tmp;
                    tmp = "";
                }
            }

            return rst;
        }

        /// <summary>
        /// 将字符串变为一行一个字符的字符串
        /// </summary>
        public static string ToVertical(string value)
        {
            string result = "";
            foreach (var item in value)
            {
                result += item + "\n";
            }

            return result.TrimEnd('\n');
        }

        /// <summary>
        /// 将秒变为 X天X小时X分X秒
        /// </summary>
        public static string FromSecondsToTime(int seconds)
        {
            int sec = seconds % 60;
            int minutes = (int)(seconds / 60);
            int minute = minutes % 60;
            int hours = (int)(minutes / 60);
            int hour = hours % 24;
            int days = (int)(hours / 24);

            return days + "天" + hour + "小时" + minute + "分" + sec + "秒";
        }

        /// <summary>
        /// 实现有很大局限性。
        /// 将[[red:xxx]]变为《color='red'》xxx
        /// 将[[yellow:xxx]]变为《color='#DF6A00FF'》xxx
        /// 将]]变为《/color》
        /// </summary>
        public static string StringWithColorTag(string s)
        {
            return s.Replace("[[red:", "<color='red'>").Replace("[[yellow:", "<color=#DF6A00FF>")
                .Replace("]]", "</color>");
        }


        /// <summary>
        /// 将传入的字符串中间部分字符替换成特殊字符
        /// </summary>
        /// <param name="value">需要替换的字符串</param>
        /// <param name="startLen">前保留长度</param>
        /// <param name="endLen">尾保留长度</param>
        /// <param name="replaceChar">特殊字符</param>
        /// <returns>被特殊字符替换的字符串</returns>
        public static string ReplaceWithSpecialChar(string value, int startLen = 4, int endLen = 4,
            char specialChar = '*')
        {
            try
            {
                int lenth = value.Length - startLen - endLen;
                string replaceStr = value.Substring(startLen, lenth);
                string specialStr = string.Empty;
                for (int i = 0; i < replaceStr.Length; i++)
                {
                    specialStr += specialChar;
                }

                value = value.Replace(replaceStr, specialStr);
            }
            catch (Exception)
            {
                throw;
            }

            return value;
        }

        /// <summary>
        /// 按长度分割字符串，汉字按一个字符算
        /// </summary>
        public static List<string> SplitLength(string SourceString, int Length)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < SourceString.Trim().Length; i += Length)
            {
                if ((SourceString.Trim().Length - i) >= Length)
                    list.Add(SourceString.Trim().Substring(i, Length));
                else
                    list.Add(SourceString.Trim().Substring(i, SourceString.Trim().Length - i));
            }

            return list;
        }

        /// <summary>
        /// table中是否包含str
        /// </summary>
        public static bool TableContainsStr(string[] table, string str)
        {
            foreach (var item in table)
            {
                if (item.Equals(str))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///除去字符串中的空格
        /// </summary>
        public static string Trim(string str)
        {
            str = str.Replace(" ", "");
            return str;
        }

        #endregion
    }
}