using HSFrameWork.Common.Inner;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace HSFrameWork.Common
{
    /// <summary>
    /// 不需要手工注册即可使用的ProtoBufTool（类是内置或者有ProtoBuf标签）
    /// 如果同时和ConfigTable.ProtoBufTools一起使用，则需要首先将ConfigTable.ProtoBufTools初始化。
    /// </summary>
    public static class DirectProtoBufTools
    {
        /// <summary>
        /// 使用ProtoBuf序列化该对象。
        /// </summary>
        public static byte[] Serialize(this object obj)
        {
            using (var memory = new MemoryStream())
            {
                Serializer.Serialize(memory, obj);
                return memory.ToArray();
            }
        }

        /// <summary>
        /// 使用ProtoBuf序列化该对象。
        /// 调用者需要保证bufSize足够，否则会异常。
        /// </summary>
        public static SmartBuffer Serialize(this object obj, int bufSize)
        {
            var sb = ArrayPool<byte>.Shared.CreateSB(bufSize);
            using (var memory = new MemoryStream(sb.Data))
            {
                Serializer.Serialize(memory, obj);
                sb.Size = (int)memory.Position;
                return sb;
            }
        }

        /// <summary>
        /// 使用ProtoBuf序列化该对象。
        /// 调用者需要保证bufSize足够，否则会异常。
        /// </summary>
        public static int Serialize(this object obj, byte[] buffer)
        {
            using (var memory = new MemoryStream(buffer))
            {
                Serializer.Serialize(memory, obj);
                return (int)memory.Position;
            }
        }

        /// <summary>
        /// 使用ProtoBuf反序列化。
        /// </summary>
        public static object Deserialize(byte[] data, Type type)
        {
            using (var memory = new MemoryStream(data))
                return Serializer.Deserialize(type, memory);
        }

        /// <summary>
        /// 使用ProtoBuf反序列化。
        /// </summary>
        public static T Deserialize<T>(this byte[] data)
        {
            using (var memory = new MemoryStream(data))
                return Serializer.Deserialize<T>(memory);
        }

        /// <summary>
        /// 使用ProtoBuf反序列化。
        /// </summary>
        public static T Deserialize<T>(this byte[] data, int offset, int size)
        {
            using (var memory = new MemoryStream(data, offset, size))
                return Serializer.Deserialize<T>(memory);
        }

#if HSFRAMEWORK_NET_ABOVE_4_5
        public static T Deserialize<T>(this SmartBuffer sb)
        {
            return Deserialize<T>(sb.Data, sb.Offset, sb.Size);
        }
#endif
    }
}