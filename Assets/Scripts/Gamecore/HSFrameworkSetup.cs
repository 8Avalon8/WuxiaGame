using System;
using HSFrameWork.ConfigTable;
using System.Collections.Generic;
using HanSquirrel.ResourceManager;
using GLib;
using HSFrameWork.Common;
using JianghuX;

public class HSConfigTableInitHelperPhone : DefaultInitHelper
{
    /// <summary> desKey不放在InitHelper里面是因为担心会被忽略。 </summary>
    public static IInitHelper Create()
    {
        return new HSConfigTableInitHelperPhone();
    }
    public override Func<byte[]> LoadConfigTableData
    {
        get
        {
            return () => BinaryResourceLoader.LoadBinary(HSUnityEnv.CEValuesPath);
        }
    }
    protected override void BuildTypeNodes()
    {
        AddTypeNode<ResourceDTO>("resource");
        AddTypeNode<SkillPojo>("skill");
        AddTypeNode<ActionBallPojo>("actionball");
    }

    protected HSConfigTableInitHelperPhone() { }

    public override IEnumerable<Type> ProtoBufTypes { get { return _SharedTypes; } }

    protected static readonly List<Type> _SharedTypes = new List<Type>()
    {
        typeof(ResourceDTO),
        typeof(SkillPojo),
        typeof(ActionBallPojo),
    };

    private static List<Type> _ClientAllTypes;

    /// <summary>
    /// 客户端额外需要二进制序列化的类。
    /// </summary>
    private static readonly List<Type> _ClientExtraTypes = new List<Type>()
    {

    };
}

