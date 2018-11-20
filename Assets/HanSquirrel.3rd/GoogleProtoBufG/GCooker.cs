using System.Reflection;
using Google.ProtocolBuffers.FieldAccess;
using System.Collections.Generic;

namespace Google.ProtocolBuffers.George
{
    /// <summary>
    /// GG 20181021 因为在IOS下运行不了，调试了半天逐步发现是ReflectionUtil中 MakeGenericMethod
    /// 的四个地方的的调用在IL2CPP中并没有生成代码，原因在于只是在运行才会调用。因此通过在代码中添加Debug
    /// 信息在Unity中运行后，拷贝这些实际需要的Generic参数并放在这里。
    /// 因为仅仅是阿里云日志需要GoogleProtobuf，因此在阿里云日志的适配代码中也有类似的实现。
    /// 这些代码的目的仅仅是为了让IL2CPP生成这些Generic参数的函数源码而已。这样在运行时
    /// MakeGenericMethod就会找到这些C++的函数了。
    /// </summary>
    public class GCooker
    {
        public static bool _Dummy = true;
        public static void ColdBind()
        {
            if (_Dummy)
                return;

            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorSet, IList<DescriptorProtos.FileDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.FileDescriptorSet.Builder, DescriptorProtos.FileDescriptorProto, DescriptorProtos.FileDescriptorSet.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorSet.Builder, Collections.IPopsicleList<DescriptorProtos.FileDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, IList<System.String>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.FileDescriptorProto.Builder, System.String, DescriptorProtos.FileDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, Collections.IPopsicleList<System.String>>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, IList<DescriptorProtos.DescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.FileDescriptorProto.Builder, DescriptorProtos.DescriptorProto, DescriptorProtos.FileDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.DescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, IList<DescriptorProtos.EnumDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.FileDescriptorProto.Builder, DescriptorProtos.EnumDescriptorProto, DescriptorProtos.FileDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.EnumDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.EnumDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, IList<DescriptorProtos.ServiceDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.FileDescriptorProto.Builder, DescriptorProtos.ServiceDescriptorProto, DescriptorProtos.FileDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.ServiceDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.ServiceDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, IList<DescriptorProtos.FieldDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.FileDescriptorProto.Builder, DescriptorProtos.FieldDescriptorProto, DescriptorProtos.FileDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.FieldDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, DescriptorProtos.FileOptions>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, DescriptorProtos.FileOptions>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.FileOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileDescriptorProto, DescriptorProtos.SourceCodeInfo>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileDescriptorProto.Builder, DescriptorProtos.SourceCodeInfo>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.SourceCodeInfo.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.DescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto, IList<DescriptorProtos.FieldDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.DescriptorProto.Builder, DescriptorProtos.FieldDescriptorProto, DescriptorProtos.DescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.FieldDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto, IList<DescriptorProtos.FieldDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.DescriptorProto.Builder, DescriptorProtos.FieldDescriptorProto, DescriptorProtos.DescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.FieldDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto, IList<DescriptorProtos.DescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.DescriptorProto.Builder, DescriptorProtos.DescriptorProto, DescriptorProtos.DescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.DescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto, IList<DescriptorProtos.EnumDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.DescriptorProto.Builder, DescriptorProtos.EnumDescriptorProto, DescriptorProtos.DescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.EnumDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.EnumDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto, IList<DescriptorProtos.DescriptorProto.Types.ExtensionRange>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.DescriptorProto.Builder, DescriptorProtos.DescriptorProto.Types.ExtensionRange, DescriptorProtos.DescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.DescriptorProto.Types.ExtensionRange>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Types.ExtensionRange.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto, DescriptorProtos.MessageOptions>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.DescriptorProto.Builder, DescriptorProtos.MessageOptions>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.MessageOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Types.ExtensionRange, System.Int32>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.DescriptorProto.Types.ExtensionRange.Builder, System.Int32>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.DescriptorProto.Types.ExtensionRange, System.Int32>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.DescriptorProto.Types.ExtensionRange.Builder, System.Int32>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto, System.Int32>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder, System.Int32>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto, DescriptorProtos.FieldDescriptorProto.Types.Label>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder, DescriptorProtos.FieldDescriptorProto.Types.Label>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto, DescriptorProtos.FieldDescriptorProto.Types.Type>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder, DescriptorProtos.FieldDescriptorProto.Types.Type>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldDescriptorProto, DescriptorProtos.FieldOptions>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldDescriptorProto.Builder, DescriptorProtos.FieldOptions>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.FieldOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.EnumDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumDescriptorProto, IList<DescriptorProtos.EnumValueDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.EnumDescriptorProto.Builder, DescriptorProtos.EnumValueDescriptorProto, DescriptorProtos.EnumDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumDescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.EnumValueDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.EnumValueDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumDescriptorProto, DescriptorProtos.EnumOptions>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.EnumDescriptorProto.Builder, DescriptorProtos.EnumOptions>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.EnumOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumValueDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.EnumValueDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumValueDescriptorProto, System.Int32>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.EnumValueDescriptorProto.Builder, System.Int32>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumValueDescriptorProto, DescriptorProtos.EnumValueOptions>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.EnumValueDescriptorProto.Builder, DescriptorProtos.EnumValueOptions>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.EnumValueOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.ServiceDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.ServiceDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.ServiceDescriptorProto, IList<DescriptorProtos.MethodDescriptorProto>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.ServiceDescriptorProto.Builder, DescriptorProtos.MethodDescriptorProto, DescriptorProtos.ServiceDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.ServiceDescriptorProto.Builder, Collections.IPopsicleList<DescriptorProtos.MethodDescriptorProto>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.MethodDescriptorProto.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.ServiceDescriptorProto, DescriptorProtos.ServiceOptions>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.ServiceDescriptorProto.Builder, DescriptorProtos.ServiceOptions>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.ServiceOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MethodDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.MethodDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MethodDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.MethodDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MethodDescriptorProto, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.MethodDescriptorProto.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MethodDescriptorProto, DescriptorProtos.MethodOptions>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.MethodDescriptorProto.Builder, DescriptorProtos.MethodOptions>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.MethodOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, DescriptorProtos.FileOptions.Types.OptimizeMode>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileOptions.Builder, DescriptorProtos.FileOptions.Types.OptimizeMode>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions, IList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.FileOptions.Builder, DescriptorProtos.UninterpretedOption, DescriptorProtos.FileOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FileOptions.Builder, Collections.IPopsicleList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MessageOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.MessageOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MessageOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.MessageOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MessageOptions, IList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.MessageOptions.Builder, DescriptorProtos.UninterpretedOption, DescriptorProtos.MessageOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MessageOptions.Builder, Collections.IPopsicleList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldOptions, DescriptorProtos.FieldOptions.Types.CType>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldOptions.Builder, DescriptorProtos.FieldOptions.Types.CType>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.FieldOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldOptions, IList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.FieldOptions.Builder, DescriptorProtos.UninterpretedOption, DescriptorProtos.FieldOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.FieldOptions.Builder, Collections.IPopsicleList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumOptions, IList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.EnumOptions.Builder, DescriptorProtos.UninterpretedOption, DescriptorProtos.EnumOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumOptions.Builder, Collections.IPopsicleList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumValueOptions, IList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.EnumValueOptions.Builder, DescriptorProtos.UninterpretedOption, DescriptorProtos.EnumValueOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.EnumValueOptions.Builder, Collections.IPopsicleList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.ServiceOptions, IList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.ServiceOptions.Builder, DescriptorProtos.UninterpretedOption, DescriptorProtos.ServiceOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.ServiceOptions.Builder, Collections.IPopsicleList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MethodOptions, IList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.MethodOptions.Builder, DescriptorProtos.UninterpretedOption, DescriptorProtos.MethodOptions.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.MethodOptions.Builder, Collections.IPopsicleList<DescriptorProtos.UninterpretedOption>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption, IList<DescriptorProtos.UninterpretedOption.Types.NamePart>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.UninterpretedOption.Builder, DescriptorProtos.UninterpretedOption.Types.NamePart, DescriptorProtos.UninterpretedOption.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder, Collections.IPopsicleList<DescriptorProtos.UninterpretedOption.Types.NamePart>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Types.NamePart.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption, System.UInt64>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder, System.UInt64>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption, System.Int64>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder, System.Int64>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption, System.Double>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder, System.Double>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption, ByteString>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder, ByteString>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.UninterpretedOption.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Types.NamePart, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.UninterpretedOption.Types.NamePart.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.UninterpretedOption.Types.NamePart, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.UninterpretedOption.Types.NamePart.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.SourceCodeInfo, IList<DescriptorProtos.SourceCodeInfo.Types.Location>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.SourceCodeInfo.Builder, DescriptorProtos.SourceCodeInfo.Types.Location, DescriptorProtos.SourceCodeInfo.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.SourceCodeInfo.Builder, Collections.IPopsicleList<DescriptorProtos.SourceCodeInfo.Types.Location>>(null);
            ReflectionUtil.CreateStaticUpcastDelegateImpl<DescriptorProtos.SourceCodeInfo.Types.Location.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.SourceCodeInfo.Types.Location, IList<System.Int32>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.SourceCodeInfo.Types.Location.Builder, System.Int32, DescriptorProtos.SourceCodeInfo.Types.Location.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.SourceCodeInfo.Types.Location.Builder, Collections.IPopsicleList<System.Int32>>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.SourceCodeInfo.Types.Location, IList<System.Int32>>(null);
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<DescriptorProtos.SourceCodeInfo.Types.Location.Builder, System.Int32, DescriptorProtos.SourceCodeInfo.Types.Location.Builder>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.SourceCodeInfo.Types.Location.Builder, Collections.IPopsicleList<System.Int32>>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, DescriptorProtos.CSharpServiceType>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, DescriptorProtos.CSharpServiceType>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFileOptions, System.Boolean>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFileOptions.Builder, System.Boolean>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpFieldOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpFieldOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpServiceOptions, System.String>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpServiceOptions.Builder, System.String>(null);
            ReflectionUtil.CreateUpcastDelegateImpl<DescriptorProtos.CSharpMethodOptions, System.Int32>(null);
            ReflectionUtil.CreateDowncastDelegateImpl<DescriptorProtos.CSharpMethodOptions.Builder, System.Int32>(null);
        }
        public static void CreateUpcastDelegateImpl<TSource, TResult>(MethodInfo method)
        {
            ReflectionUtil.CreateUpcastDelegateImpl<TSource, TResult>(method);
        }

        public static void CreateDowncastDelegateImpl<TSource, TParam>(MethodInfo method)
        {
            ReflectionUtil.CreateDowncastDelegateImpl<TSource, TParam>(method);
        }
        public static void CreateStaticUpcastDelegateImpl<T>(MethodInfo method)
        {
            ReflectionUtil.CreateStaticUpcastDelegateImpl<T>(method);

        }
        public static void CreateDowncastDelegateIgnoringReturnImpl<TSource, TParam, TReturn>(
            MethodInfo method)
        {
            ReflectionUtil.CreateDowncastDelegateIgnoringReturnImpl<TSource, TParam, TReturn>(method);
        }
    }
}
