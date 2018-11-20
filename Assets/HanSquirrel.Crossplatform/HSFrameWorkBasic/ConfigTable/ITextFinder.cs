using System.Collections.Generic;
using GLib;

namespace HSFrameWork.ConfigTable.Trans
{
    /// <summary>
    /// 语言包自动替换；只在Editor里面用；处于方便放在这里。
    /// </summary>
    public interface ITextFinder
    {
        /// <summary>
        /// arg是REG文件里面该TextFinder的参数；
        /// obj是当前处理的实例；str是要处理的该实例的某个属性值；
        /// dict是该实例的这个属性对应的翻译字典；可能为null或者不全；
        /// newSttr是翻译结果；如果没有翻译，则是null。
        /// 返回值是需要翻译的字符块；如果不需要翻译，则为null或者为string[0]。
        /// </summary>
        string[] DoWork(object obj, string arg, string str, Mini.IReadOnlyEasyDict<string, string> dict, ref int transedTextCount, out string newStr);

    }
}
