using System;
using System.Collections.Generic;
using System.Linq;

namespace HSFrameWork.Common
{
    public partial class ToolsShared
    {
        public static double FIXED_RANDOM_SEED = 0.5;
        public static bool FakeRandomFlag = false;

        /// <summary>
        /// BY CG:随机取list中的n个不重复元素
        /// 原来break的算法错了，这里做一下调整
        /// </summary>
        public static List<T> GenerateRandomListNotRepeat<T>(List<T> list, int n)
        {
            if (list == null)
                return null;
            if (n >= list.Count)
                return list;
            return (list.OrderBy(d => Guid.NewGuid()).Take(n)).ToList();
        }

        /// <summary>
        /// 生成a到b之间的随机数。如果rnd为null，则使用缺省的共享Random，则多线程不安全
        /// </summary>
        public static double GetRandom(double a, double b, System.Random rnd = null)
        {
            if (rnd == null)
                rnd = _Random;

            double k = 0;
            if (FakeRandomFlag)
            {
                k = FIXED_RANDOM_SEED;
            }
            else
            {
                k = _Random.NextDouble();
            }

            double tmp = 0;
            if (b > a)
            {
                tmp = a;
                a = b;
                b = tmp;
            }

            return b + (a - b) * k;
        }

        /// <summary>
        /// 生成一个随机的BOOL。如果rnd为null，则使用缺省的共享Random，则多线程不安全
        /// </summary>
        public static bool GetRandomBool(System.Random rnd = null)
        {
            var rst = GetRandomInt(0, 1, rnd);
            if (rst == 0)
                return false;
            return true;
        }

        /// <summary>
        /// 生成a到b之间的随机数。如果rnd为null，则使用缺省的共享Random，则多线程不安全
        /// </summary>
        public static int GetRandomInt(int a, int b, System.Random rnd = null)
        {
            int k = (int)GetRandom(a, b + 1, rnd);
            if (k >= b && b >= a)
                k = b;
            return k;
        }

        /// <summary>
        /// 从list中随机取出一个元素。如果rnd为null，则使用缺省的共享Random，则多线程不安全
        /// </summary>
        public static T GetRandomElement<T>(IEnumerable<T> list, System.Random rnd = null)
        {
            return GetRandomElementInList<T>(list.ToList(), rnd);
        }

        /// <summary>
        /// 从list中随机取出一个元素。如果rnd为null，则使用缺省的共享Random，则多线程不安全
        /// </summary>
        public static T GetRandomElementInList<T>(List<T> list, System.Random rnd = null)
        {
            if (list.Count == 0) return default(T);
            return list[GetRandomInt(0, list.Count - 1, rnd)];
        }


        /// <summary>
        /// 从list中随机取出一个元素。如果rnd为null，则使用缺省的共享Random，则多线程不安全
        /// </summary>
        public static string GetRandomElement(string[] list, System.Random rnd = null)
        {
            if (list.Length == 0) return "";
            return list[GetRandomInt(0, list.Length - 1, rnd)];
        }


        /// <summary>
        /// 测试概率。如果rnd为null，则使用缺省的共享Random，则多线程不安全
        /// </summary>
        /// <param name="p">小于1的</param>
        /// <returns></returns>
        public static bool ProbabilityTest(double p, System.Random rnd = null)
        {
            if (p < 0) return false;
            if (p >= 1) return true;
            return GetRandom(0, 1, rnd) < p;
        }

        private static System.Random _Random = new System.Random();
    }
}