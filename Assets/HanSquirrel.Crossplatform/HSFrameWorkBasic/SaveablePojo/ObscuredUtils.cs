#if false
using CodeStage.AntiCheat.ObscuredTypes;
using HSFrameWork.Common;
using System;

namespace HSFrameWork.SPojo
{
    public static class ObscuredUtils
    {
        public static object ConvertNumberToObscured(Type type, object value)
        {
            if (type == typeof(int))
                return (ObscuredInt)Convert.ToInt32(value);
            else if (type == typeof(float))
                return (ObscuredFloat)Convert.ToSingle(value);
            else if (type == typeof(long))
                return (ObscuredLong)Convert.ToInt64(value);
            else if (type == typeof(double))
                return (ObscuredDouble)Convert.ToDouble(value);
            else
                return null;
        }

        public static T ConvertObscuredToNumber<T>(object value) where T : struct
        {
            var typeT = typeof(T);
            if (typeT == typeof(int))
                return (T)(object)(int)(ObscuredInt)value;
            if (typeT == typeof(float))
                return (T)(object)(float)(ObscuredFloat)value;
            if (typeT == typeof(long))
                return (T)(object)(long)(ObscuredLong)value;
            if (typeT == typeof(double))
                return (T)(object)(double)(ObscuredDouble)value;
            HSUtils.LogError("不支持的类型 [{0}]", typeT.FullName);
            return default(T);
        }
    }
}
#endif

