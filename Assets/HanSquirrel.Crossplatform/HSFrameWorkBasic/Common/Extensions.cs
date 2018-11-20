using System;
using System.Collections.Generic;

namespace HSFrameWork.Common
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// 如果tuple为null，则会返回一个空字典
        /// </summary>
        public static Dictionary<TK,TV> SafeToDict<TK, TV>(IEnumerable<Tuple<TK,TV>> tuples)
        {
            Dictionary<TK, TV> ret = new Dictionary<TK, TV>();
            if (tuples == null)
                return ret;

            foreach (var t in tuples)
                ret.Add(t.Item1, t.Item2);
            return ret;
        }
    }
}