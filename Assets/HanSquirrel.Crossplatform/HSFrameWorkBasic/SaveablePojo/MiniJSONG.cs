using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Stopwatch=System.Diagnostics.Stopwatch;
using GLib;

namespace Hanjiasongshu
{
}

/* Based on the JSON parser from 
 * http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
 * 
 * I simplified it so that it doesn't throw exceptions
 * and can be used in Unity iPhone with maximum code stripping.
 */
/// <summary>
/// This class encodes and decodes JSON strings.
/// Spec. details, see http://www.json.org/
/// 
/// JSON uses Arrays and Objects. These correspond here to the datatypes ArrayList and Hashtable.
/// All numbers are parsed to doubles.
/// </summary>
namespace HSFrameWork.Common.Inner
{
    /// <summary>
    /// 为了测试方便，拷贝原始的MiniJSON并修改了一些代码。
    /// </summary>
    public class MiniJSONG
    {
        private const int TOKEN_NONE = 0;
        private const int TOKEN_CURLY_OPEN = 1;
        private const int TOKEN_CURLY_CLOSE = 2;
        private const int TOKEN_SQUARED_OPEN = 3;
        private const int TOKEN_SQUARED_CLOSE = 4;
        private const int TOKEN_COLON = 5;
        private const int TOKEN_COMMA = 6;
        private const int TOKEN_STRING = 7;
        private const int TOKEN_NUMBER = 8;
        private const int TOKEN_TRUE = 9;
        private const int TOKEN_FALSE = 10;
        private const int TOKEN_NULL = 11;
        private const int BUILDER_CAPACITY = 2000;

        /// <summary>
        /// On decoding, this value holds the position at which the parse failed (-1 = no error).
        /// </summary>
        protected static int _lastErrorIndex = -1;
        protected static string _lastDecode = "";

        public static bool encodeCharacteFlag = false;


        /// <summary>
        /// Parses the string json into a value
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
        public static object jsonDecode(string json)
        {
            // save the string for debug information
            MiniJSONG._lastDecode = json;

            if (json != null)
            {
                char[] charArray = json.ToCharArray();
                int index = 0;
                bool success = true;
                object value = MiniJSONG.parseValue(charArray, ref index, ref success);

                if (success)
                    MiniJSONG._lastErrorIndex = -1;
                else
                    MiniJSONG._lastErrorIndex = index;

                return value;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Converts a Hashtable / ArrayList / Dictionary(string,string) object into a JSON string
        /// </summary>
        /// <param name="json">A Hashtable / ArrayList</param>
        /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
        public static string jsonEncode(object json, bool sort = true)
        {
            var builder = new StringBuilder(BUILDER_CAPACITY);
            var success = MiniJSONG.serializeValue(json, builder, sort);

            return (success ? builder.ToString() : null);
        }


        /// <summary>
        /// On decoding, this function returns the position at which the parse failed (-1 = no error).
        /// </summary>
        /// <returns></returns>
        public static bool lastDecodeSuccessful()
        {
            return (MiniJSONG._lastErrorIndex == -1);
        }


        /// <summary>
        /// On decoding, this function returns the position at which the parse failed (-1 = no error).
        /// </summary>
        /// <returns></returns>
        public static int getLastErrorIndex()
        {
            return MiniJSONG._lastErrorIndex;
        }


        /// <summary>
        /// If a decoding error occurred, this function returns a piece of the JSON string 
        /// at which the error took place. To ease debugging.
        /// </summary>
        /// <returns></returns>
        public static string getLastErrorSnippet()
        {
            if (MiniJSONG._lastErrorIndex == -1)
            {
                return "";
            }
            else
            {
                int startIndex = MiniJSONG._lastErrorIndex - 5;
                int endIndex = MiniJSONG._lastErrorIndex + 15;
                if (startIndex < 0)
                    startIndex = 0;

                if (endIndex >= MiniJSONG._lastDecode.Length)
                    endIndex = MiniJSONG._lastDecode.Length - 1;

                return MiniJSONG._lastDecode.Substring(startIndex, endIndex - startIndex + 1);
            }
        }


        #region Parsing

        protected static Hashtable parseObject(char[] json, ref int index)
        {
            Hashtable table = new Hashtable();
            int token;

            // {
            nextToken(json, ref index);

            bool done = false;
            while (!done)
            {
                token = lookAhead(json, index);
                if (token == MiniJSONG.TOKEN_NONE)
                {
                    return null;
                }
                else if (token == MiniJSONG.TOKEN_COMMA)
                {
                    nextToken(json, ref index);
                }
                else if (token == MiniJSONG.TOKEN_CURLY_CLOSE)
                {
                    nextToken(json, ref index);
                    return table;
                }
                else
                {
                    // name
                    string name = parseString(json, ref index);
                    if (name == null)
                    {
                        return null;
                    }

                    // :
                    token = nextToken(json, ref index);
                    if (token != MiniJSONG.TOKEN_COLON)
                        return null;

                    // value
                    bool success = true;
                    object value = parseValue(json, ref index, ref success);
                    if (!success)
                        return null;

                    table[name] = value;
                }
            }

            return table;
        }


        protected static ArrayList parseArray(char[] json, ref int index)
        {
            ArrayList array = new ArrayList();

            // [
            nextToken(json, ref index);

            bool done = false;
            while (!done)
            {
                int token = lookAhead(json, index);
                if (token == MiniJSONG.TOKEN_NONE)
                {
                    return null;
                }
                else if (token == MiniJSONG.TOKEN_COMMA)
                {
                    nextToken(json, ref index);
                }
                else if (token == MiniJSONG.TOKEN_SQUARED_CLOSE)
                {
                    nextToken(json, ref index);
                    break;
                }
                else
                {
                    bool success = true;
                    object value = parseValue(json, ref index, ref success);
                    if (!success)
                        return null;

                    array.Add(value);
                }
            }

            return array;
        }


        protected static object parseValue(char[] json, ref int index, ref bool success)
        {
            switch (lookAhead(json, index))
            {
                case MiniJSONG.TOKEN_STRING:
                    return parseString(json, ref index);
                case MiniJSONG.TOKEN_NUMBER:
                    return parseNumber(json, ref index);
                case MiniJSONG.TOKEN_CURLY_OPEN:
                    return parseObject(json, ref index);
                case MiniJSONG.TOKEN_SQUARED_OPEN:
                    return parseArray(json, ref index);
                case MiniJSONG.TOKEN_TRUE:
                    nextToken(json, ref index);
                    return Boolean.Parse("TRUE");
                case MiniJSONG.TOKEN_FALSE:
                    nextToken(json, ref index);
                    return Boolean.Parse("FALSE");
                case MiniJSONG.TOKEN_NULL:
                    nextToken(json, ref index);
                    return null;
                case MiniJSONG.TOKEN_NONE:
                    break;
            }

            success = false;
            return null;
        }


        public static string parseString(char[] json, ref int index)
        {
            string s = "";
            char c;

            eatWhitespace(json, ref index);

            // "
            c = json[index++];

            bool complete = false;
            while (!complete)
            {
                if (index == json.Length)
                    break;

                c = json[index++];
                if (c == '"')
                {
                    complete = true;
                    break;
                }
                else if (c == '\\')
                {
                    if (index == json.Length)
                        break;

                    c = json[index++];
                    if (c == '"')
                    {
                        s += '"';
                    }
                    else if (c == '\\')
                    {
                        s += '\\';
                    }
                    else if (c == '/')
                    {
                        s += '/';
                    }
                    else if (c == 'b')
                    {
                        s += '\b';
                    }
                    else if (c == 'f')
                    {
                        s += '\f';
                    }
                    else if (c == 'n')
                    {
                        s += '\n';
                    }
                    else if (c == 'r')
                    {
                        s += '\r';
                    }
                    else if (c == 't')
                    {
                        s += '\t';
                    }
                    else if (c == 'u')
                    {
                        int remainingLength = json.Length - index;
                        if (remainingLength >= 4)
                        {
                            char[] unicodeCharArray = new char[4];
                            Array.Copy(json, index, unicodeCharArray, 0, 4);

                            uint codePoint = UInt32.Parse(new string(unicodeCharArray), System.Globalization.NumberStyles.HexNumber);

                            // convert the integer codepoint to a unicode char and add to string
                            s += Char.ConvertFromUtf32((int)codePoint);

                            // skip 4 chars
                            index += 4;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    s += c;
                }

            }

            if (!complete)
                return null;

            return s;
        }


        protected static double parseNumber(char[] json, ref int index)
        {
            eatWhitespace(json, ref index);

            int lastIndex = getLastIndexOfNumber(json, index);
            int charLength = (lastIndex - index) + 1;
            char[] numberCharArray = new char[charLength];

            Array.Copy(json, index, numberCharArray, 0, charLength);
            index = lastIndex + 1;
            return Double.Parse(new string(numberCharArray)); // , CultureInfo.InvariantCulture);
        }


        protected static int getLastIndexOfNumber(char[] json, int index)
        {
            int lastIndex;
            for (lastIndex = index; lastIndex < json.Length; lastIndex++)
                if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
                {
                    break;
                }
            return lastIndex - 1;
        }


        protected static void eatWhitespace(char[] json, ref int index)
        {
            for (; index < json.Length; index++)
                if (" \t\n\r".IndexOf(json[index]) == -1)
                {
                    break;
                }
        }


        protected static int lookAhead(char[] json, int index)
        {
            int saveIndex = index;
            return nextToken(json, ref saveIndex);
        }


        protected static int nextToken(char[] json, ref int index)
        {
            eatWhitespace(json, ref index);

            if (index == json.Length)
            {
                return MiniJSONG.TOKEN_NONE;
            }

            char c = json[index];
            index++;
            switch (c)
            {
                case '{':
                    return MiniJSONG.TOKEN_CURLY_OPEN;
                case '}':
                    return MiniJSONG.TOKEN_CURLY_CLOSE;
                case '[':
                    return MiniJSONG.TOKEN_SQUARED_OPEN;
                case ']':
                    return MiniJSONG.TOKEN_SQUARED_CLOSE;
                case ',':
                    return MiniJSONG.TOKEN_COMMA;
                case '"':
                    return MiniJSONG.TOKEN_STRING;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    return MiniJSONG.TOKEN_NUMBER;
                case ':':
                    return MiniJSONG.TOKEN_COLON;
            }
            index--;

            int remainingLength = json.Length - index;

            // false
            if (remainingLength >= 5)
            {
                if (json[index] == 'f' &&
                    json[index + 1] == 'a' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 's' &&
                    json[index + 4] == 'e')
                {
                    index += 5;
                    return MiniJSONG.TOKEN_FALSE;
                }
            }

            // true
            if (remainingLength >= 4)
            {
                if (json[index] == 't' &&
                    json[index + 1] == 'r' &&
                    json[index + 2] == 'u' &&
                    json[index + 3] == 'e')
                {
                    index += 4;
                    return MiniJSONG.TOKEN_TRUE;
                }
            }

            // null
            if (remainingLength >= 4)
            {
                if (json[index] == 'n' &&
                    json[index + 1] == 'u' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 'l')
                {
                    index += 4;
                    return MiniJSONG.TOKEN_NULL;
                }
            }

            return MiniJSONG.TOKEN_NONE;
        }

        #endregion


        #region Serialization

        protected static bool serializeObjectOrArray(object objectOrArray, StringBuilder builder)
        {
            if (objectOrArray is IDictionary)
            {
                return serializeObject((IDictionary)objectOrArray, builder);
            }
            else if (objectOrArray is ArrayList)
            {
                return serializeArray((ArrayList)objectOrArray, builder);
            }
            else
            {
                return false;
            }
        }




        private static void sortArrayList(ArrayList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                StringBuilder sb = new StringBuilder();
                serializeString(list[i] as string, sb);
                list[i] = sb.ToString();
            }

            list.Sort();

            for (int i = 0; i < list.Count; i++)
            {
                int index = 0;
                list[i] = parseString((list[i] as string).ToCharArray(), ref index);
            }
        }


        protected static bool serializeObject(IDictionary anObject, StringBuilder builder, bool sort = true)
        {
            builder.Append("{");
            ICollection e = anObject.Keys;
            if (sort)
            {
                ArrayList a = new ArrayList(e);
                a.Sort();
                e = a;
            }

            bool first = true;
            foreach (string key in e)
            {
                object value = anObject[key];

                if (!first)
                {
                    builder.Append(", ");
                }

                serializeString(key, builder);
                builder.Append(":");
                if (!serializeValue(value, builder))
                {
                    return false;
                }

                first = false;
            }

            builder.Append("}");
            return true;
        }

#if false
        protected static bool serializeDictionary(IDictionary<string, string> dict, StringBuilder builder, bool sort = false)
        {
            builder.Append("{");

            bool first = true;

            ICollection<string> e = dict.Keys;
            if (sort)
            {
                List<string> a = new List<string>(e);
                a.Sort();
                e = a;
            }

            foreach (string key in e)
            {
                string value = dict[key];

                if (!first)
                    builder.Append(", ");

                serializeString(key, builder);
                builder.Append(":");
                serializeString(value, builder);

                first = false;
            }

            builder.Append("}");
            return true;
        }
#endif
        protected static bool serializeList(IList anArray, StringBuilder builder, bool sort = true)
        {
            builder.Append("[");

            bool first = true;

            if (sort)
            {
                if (anArray is List<int>)
                {
                    (anArray as List<int>).Sort();
                }
                else if (anArray is List<float>)
                {
                    (anArray as List<float>).Sort();
                }
                else if (anArray is List<long>)
                {
                    (anArray as List<long>).Sort();
                }
                else if (anArray is List<string>)
                {
                    (anArray as List<string>).Sort();
                }
            }

            for (int i = 0; i < anArray.Count; i++)
            {
                object value = anArray[i];

                if (!first)
                {
                    builder.Append(", ");
                }

                if (!serializeValue(value, builder))
                {
                    return false;
                }

                first = false;
            }

            builder.Append("]");
            return true;
        }

        protected static bool serializeArray(ArrayList anArray, StringBuilder builder, bool sort = true)
        {
            builder.Append("[");

            bool first = true;

            if (sort)
            {
                anArray.Sort();
            }

            for (int i = 0; i < anArray.Count; i++)
            {
                object value = anArray[i];

                if (!first)
                {
                    builder.Append(", ");
                }

                if (!serializeValue(value, builder))
                {
                    return false;
                }

                first = false;
            }

            builder.Append("]");
            return true;
        }


        protected static bool serializeValue(object value, StringBuilder builder, bool sort = true)
        {
            //Type t = value.GetType();
            //UnityEngine.Debug.Log("type: " + t.ToString() + " isArray: " + t.IsArray);

            if (value == null)
            {
                builder.Append("null");
            }
            else if (value.GetType().IsArray)
            {
                serializeArray(new ArrayList((ICollection)value), builder, sort);
            }
            else if (value is string)
            {
                serializeString((string)value, builder);
            }
            else if (value is Char)
            {
                serializeString(Convert.ToString((char)value), builder);
            }
            else if (value is decimal)
            {
                serializeString(Convert.ToString((decimal)value), builder);
            }
#if false
            else if (value is Dictionary<string, string>)
            {
                serializeDictionary((Dictionary<string, string>)value, builder, sort);
            }
#endif
            else if (value is IDictionary)
            {
                serializeObject((IDictionary)value, builder, sort);
            }
            else if (value is ArrayList)
            {
                serializeArray((ArrayList)value, builder, sort);
            }
            else if (value is IList && value.GetType().IsGenericType)
            {
                serializeList((IList)value, builder, sort);
            }
            else if ((value is Boolean) && ((Boolean)value == true))
            {
                builder.Append("true");
            }
            else if ((value is Boolean) && ((Boolean)value == false))
            {
                builder.Append("false");
            }
            else if (value.GetType().IsPrimitive)
            {
                serializeNumber(value, builder);
            }
            else
            {
                return false;
            }

            return true;
        }


        protected static void serializeString(string aString, StringBuilder builder, bool quote = false)
        {
            if (quote)
                builder.Append("'");

            builder.Append(aString);

            if (quote)
                builder.Append("'");
        }


        protected static void serializeNumber(object number, StringBuilder builder)
        {
            builder.Append(Convert.ToString(number)); // , CultureInfo.InvariantCulture));
        }

        #endregion

    }



    #region Extension methods

    public static class MiniJSONGExtensions
    {
        public static string toJsonG(this IDictionary obj, bool sort = true)
        {
            return MiniJSONG.jsonEncode(obj, sort);
        }


        public static string toJsonG(this IDictionary<string, string> obj, bool sort = true)
        {
            return MiniJSONG.jsonEncode(obj, sort);
        }

        public static string toJsonG(this IList obj)
        {
            return MiniJSONG.jsonEncode(obj);
        }


        public static Stopwatch ArrayListFromJsonExeTimer = new Stopwatch();
        public static ArrayList arrayListFromJsonG(this string json)
        {
            using (ExeTimerSum.Create(ArrayListFromJsonExeTimer))
                return MiniJSONG.jsonDecode(json) as ArrayList;
        }


        public static Hashtable hashtableFromJsonG(this string json)
        {
            return MiniJSONG.jsonDecode(json) as Hashtable;
        }

        public static string DecodeFromNiniJsonOutPutOneLine(this string str)
        {
            char[] charArray = str.ToCharArray();
            int index = 0;
            StringBuilder sb = new StringBuilder();
            while (index < charArray.Length)
            {
                if (charArray[index] != '\"')
                {
                    sb.Append(charArray[index++]);
                }
                else
                {
                    sb.Append(MiniJSONG.parseString(charArray, ref index));
                }
            }
            string oneLine = sb.ToString();
            if (oneLine.IndexOf(@"\u") != -1)
            {
                oneLine = DecodeFromNiniJsonOutPutOneLine(oneLine);
            }
            return oneLine;
        }

        private static bool PeekCharArray(char[] array, int index, string subStr)
        {
            char[] sub = subStr.ToCharArray();
            for (int i = 0; i < sub.Length; i++, index++)
            {
                if (index >= array.Length || sub[i] != array[index])
                    return false;
            }

            return true;
        }

        public static string DecodeFromMiniJsonOutput(this string str, bool includeOrg)
        {
            string oneLineS = DecodeFromNiniJsonOutPutOneLine(str);
            oneLineS = oneLineS.Replace('\n', ' '); //有时候字符串本身里面有回车，造成输出结果比较难看而已。
            char[] oneLine = oneLineS.ToCharArray();
            if (oneLine.Length < 150)
                return oneLineS;

            var sb = new StringBuilder();
            if (includeOrg)
            {
                sb.AppendLine(str);
                sb.AppendLine("------------------------------------------------------");
            }

            bool beginOfNewLine = false;
            bool endLine = false;
            sb.Append(oneLine[0]);
            int level = oneLine[0] == '{' ? 1 : 0;
            for (int i = 1; i < oneLine.Length; i++)
            {
                char c = oneLine[i];
                if (c == '{' && (i + 1) < oneLine.Length && char.IsDigit(oneLine[i + 1]) && (i + 2) < oneLine.Length && oneLine[i + 2] == '}')
                {
                    sb.Append(oneLine[i++]);
                    sb.Append(oneLine[i++]);
                    sb.Append(oneLine[i]);
                    beginOfNewLine = false;
                }
                else if (c == '{')
                {
                    level++;
                    if (beginOfNewLine)
                    {
                        sb.Append("\t");
                    }
                    else
                    {
                        sb.Append(Environment.NewLine);
                        sb.Append('\t', level);
                    }
                    sb.Append("{\t");
                    level++;
                    beginOfNewLine = false;
                }
                else if (c == '}')
                {
                    level -= 2;
                    sb.Append(c);
                    endLine = true;
                }
                else if (c == ']')
                {
                    sb.Append(c);
                }
                else if (c == ',' && endLine)
                {
                    i++; //去除空格
                    sb.Append(",");
                    sb.Append(Environment.NewLine);
                    sb.Append('\t', level);
                    beginOfNewLine = true;
                    endLine = false;
                }
                else
                {
                    sb.Append(c);
                    beginOfNewLine = false;
                    endLine = false;
                }
            }

            string str1 = _rgxNodeName.Replace(sb.ToString(), _rgxReplaced);
            string str2 = _rgxNodeName2.Replace(str1, _rgxReplaced2);
            return _rgxNodeName3.Replace(str2, _rgxReplaced3);
        }

        private static Regex _rgxNodeName = new Regex(@"([{\s])(\S+:\[?\r\n)(\t*)\t");
        //空格(一个或者多个非空格 冒号 一个或者0个[ 回车换行)(0个或者一个\t)一个\t
        private const string _rgxReplaced = "$1\r\n$3$2$3\t";

        //删除空行
        private static Regex _rgxNodeName2 = new Regex(@"\r\n\s*\r\n");
        private const string _rgxReplaced2 = "\r\n";

        //如果{}在同一行，则将{后面的\t删除。
        private static Regex _rgxNodeName3 = new Regex(@"(\r\n\t+){\t([^\r\n]+})");
        private const string _rgxReplaced3 = "$1{$2";
    }
    #endregion
}