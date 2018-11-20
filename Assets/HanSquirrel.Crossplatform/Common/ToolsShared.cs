using System;
using System.Collections.Generic;

namespace HSFrameWork.Common
{
    /// <summary>
    /// 跨平台共享的一些小函数
    /// </summary>
    public partial class ToolsShared
    {
        public static float Min(float f1, params float[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (f1 > values[i])
                    f1 = values[i];
            return f1;
        }

        public static int Min(int f1, params int[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (f1 > values[i])
                    f1 = values[i];
            return f1;
        }

        /// <summary>
        /// 战场角色间的实际距离
        /// 使用角色的战场坐标计算
        /// </summary>
        /// <param name="sourceX">源x坐标</param>
        /// <param name="sourceY">源y坐标</param>
        /// <param name="targetX">目标x坐标</param>
        /// <param name="targetY">目标y坐标</param>
        /// <returns></returns>
        public static int GetDistance(int sourceX, int sourceY, int targetX, int targetY)
        {
            var X = sourceX;
            var Y = sourceY;
            var spX = targetX;
            var spY = targetY;

            int s2 = spX % 2 == 1 ? 1 : 0;
            int s1 = X % 2 == 1 ? 1 : 0;
            int T = Math.Abs((spY - Y) * 2 + (s2 - s1));
            int TXright = X + T;
            int TXleft = X - T;

            int distance = 0;

            if (spX >= TXright)
                distance = Math.Abs(TXright - X) + Math.Abs((int)(spX - TXright) / 2);
            else if (spX < TXleft)
                distance = Math.Abs(X - TXleft) + Math.Abs((int)(spX - TXleft) / 2);
            else
                distance = T;
            return distance;
        }
    }

    public static class ToolSharedExtensions
    {
        /// <summary>
        /// 向上取整
        /// </summary>
        public static int ToCeilingInt(this double value)
        {
            return (int)Math.Ceiling(value);
        }

        /// <summary>
        /// 乱序
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            System.Random rnd = new System.Random();
            while (n > 1)
            {
                int k = (rnd.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

}
