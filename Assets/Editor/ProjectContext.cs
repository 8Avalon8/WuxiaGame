using GLib;
using HSFrameWork.Common.Editor;
using HSFrameWork.ConfigTable.Editor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GPDC
{

    public static readonly string LuaPathShort =  "/lua";
    public static readonly string LuaPath = HSCTC.AppDataPath.StandardSub("/../data" + LuaPathShort);
    public static readonly string LuaBytes = Application.dataPath.StandardSub("/../data/data/lua.bytes");

    public static readonly string FilterFile = Application.dataPath.StandardSub("/../data/data/filter.txt");

    public static readonly string CachePath = HSCTC.CachePath;
    public static readonly string FilterTSFile = CachePath.StandardSub("filter.ts");
    public static readonly string CEFilterTSFile = CachePath.StandardSub("cefilter.ts");
    public static readonly string LastLuaSummaryFile = CachePath.StandardSub("luasummary");
    /// <summary>
    /// 不能在用的时候才初始化，因为有可能第一次使用是在线程池中。
    /// </summary>
    [InitializeOnLoadMethod]
    public static void OnProjectLoadedInEditor()
    {
        HSBootEditor.ColdBind(ConStr.GLOBAL_DESKEY, HSConfigTableInitHelperEditor.Create(), ConStr.NLogConfigAssetPath);

        Directory.CreateDirectory(CachePath);
        //EditorPlayMode.PlayModeChanged += OnPlayModeChanged;
    }
}
