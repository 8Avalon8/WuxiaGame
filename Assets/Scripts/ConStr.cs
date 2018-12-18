
using HanSquirrel.ResourceManager;
using System.Collections.Generic;

public class ConStr  {
    public const string NLogConfigAssetPath = "Assets/BuildSource/Configs/NLogDefault.xml";
    public const string GLOBAL_DESKEY = "eoidfjwhguireufcbdadr8232dsf";
    public static readonly HSLeanPoolConfig[] PrefabPoolConfig =
    {
        new HSLeanPoolConfig(ActionBall,0,0,NotificationTypeLP.None),
    };

    #region 预知路径

    public const string ActionBall = "Assets/Prefabs/ActionBall.prefab";

    #endregion

    #region AB打包配置
    public static readonly string[] PrefabSearchPaths = new string[]
    {

    };

    private static Dictionary<string, string> _ABFolderDict;
    public static Dictionary<string, string> ABFolderDict
    {
        get
        {
            if (_ABFolderDict == null)
            {
                _ABFolderDict = new Dictionary<string, string>();
              
            }
            return _ABFolderDict;
        }
    }

    /// <summary>
    /// 除了在上面 PrefabSearchPaths 和 _ABFolderDict 中定义的AB包之外，还有哪些AB包是有效的。
    /// 如果StreamingAsset目录下面还存在其他的AB包，在打包过程可能会被删除。
    ///（框架内部会自动添加：Android、lua、filter、values、以及values_*）
    /// </summary>
    public static readonly string[] AdditionalABs = new string[]
    {
        "bigmapnewlocationeffect", "bullythepeople", "headavatabody.allbodys",
        "pulloutsword", "redshowtime", "runintomisstao", "steponthejourney",
        "threefakeheros","values.zip","tilemaps.zip"
    };
    #endregion
}
