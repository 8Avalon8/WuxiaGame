using System.Collections;
using System.Collections.Generic;
using StrDictDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace HSFrameWork.SPojo.Inner
{
    public class HashTableUtils
    {
        /// <summary>
        /// 仅仅只能在Saveable的HashTable判断中使用。
        /// </summary>
        public static bool EqualObject(object a, object b)
        {
            if (a == null)
                return b == null;

            if (a is Hashtable)
            {
                return EqualHashTable(a as Hashtable, b as Hashtable);
            }
            else if (a is string && !(b is string))
            {  //有时候会出现 111 和"111"比较。
                return a.Equals(b.ToString());
            }
            else if (!(a is string) && b is string)
            {
                return b.Equals(a.ToString());
            }
            else
            {
                return a.Equals(b);
            }
        }

        static bool EqualHashTableInner(Hashtable t1, Hashtable t2)
        {
            if (t1.Count != t2.Count)
                return false;

            foreach (var key1 in t1.Keys)
            {
                if (!EqualObject(t1[key1], t2[key1]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool EqualHashTable(Hashtable t1, Hashtable t2)
        {
            try
            {
                return EqualHashTableInner(t1, t2);
            }
            catch
            {
                return false;
            }
        }

        public static StrDictDict ConverToDict(Hashtable ht)
        {
            var ret = new StrDictDict();
            foreach (var key in ht.Keys)
            {
                var subHash = (ht[key] as Hashtable);
                var subDict = new Dictionary<string, string>();
                ret.Add(key as string, subDict);

                foreach(var subKey in subHash.Keys)
                {
                    subDict.Add(subKey as string, subHash[subKey].ToString());
                }
            }

            return ret;
        }

        public static Hashtable ConvertToHashtable(StrDictDict dict)
        {
            var ret = new Hashtable();
            foreach(var kv in dict)
            {
                var subHT = new Hashtable();
                ret.Add(kv.Key, subHT);
                foreach(var subKV in kv.Value)
                {
                    subHT.Add(subKV.Key, subKV.Value);
                }
            }
            return ret;
        }
    }
}