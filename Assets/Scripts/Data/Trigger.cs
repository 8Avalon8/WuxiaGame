using HSFrameWork.SPojo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using HSFrameWork;
using HSFrameWork.ConfigTable;
using JianghuX;

public class Trigger : SaveablePojo{
    public Trigger()
    {

    }
    public override string PK
    {
        get
        {
            string str = Name + "_" + ArgvsString;
            str += "_" + Level.ToString();
            return str;
        }
    }

    [XmlAttribute("name")]
    public string Name
    {
        get { return Get("Name"); }
        set { Save("Name", value); }
    }

    [XmlAttribute("argvs")]
    public string ArgvsString
    {
        get { return Get("ArgvsString"); }
        set
        {
            Save("ArgvsString", value);
            if (ArgvsString != null)
            {
                _argvs = ArgvsString.Split(',');
            }
        }
    }

    [XmlIgnore]
    public string[] Argvs
    {
        get
        {
            if (_argvs == null)
            {
                var s = ArgvsString;
                if (s == null)
                {
                    ArgvsString = "";
                }
                _argvs = ArgvsString.Split(',');
            }
            for (int i = 0; i < _argvs.Count(); i++)
            {
                if (_argvs[i].StartsWith("."))
                    _argvs[i] = "0" + _argvs[i];
            }
            return _argvs;
        }
    }

    [XmlIgnore]
    private string[] _argvs;

    public void changeArgvAt(string v, int index)
    {
        string[] vs = this.Argvs;

        if (index >= vs.Length)
            return;

        vs[index] = v;
        string rst = "";
        for (int i = 0; i < vs.Length - 1; i++)
        {
            rst += vs[i] + ",";
        }
        rst += vs[vs.Length - 1];
        ArgvsString = rst;
    }

    public string GetParamString(int index)
    {
        return Argvs[index];
    }

    public int GetParamInt(int index)
    {
        try
        {
            return Math.Max(0, int.Parse(Argvs[index]));
        }
        catch
        {
            return Math.Max(0, (int)(double.Parse(Argvs[index])));
        }
    }

    public double GetParamDouble(int index)
    {
        try
        {
            return double.Parse(Argvs[index]);
        }
        catch
        {
            Debug.LogError("某个trigger的参数个数配置错误:" + Name);
            return 0;
        }
    }

    [XmlAttribute("lv")]
    public int Level
    {
        get { return Get<int>("Level", -1); }
        set { Save("Level", value); }
    }

    [XmlIgnore]
    public string Tag
    {
        get { return Get("Tag"); }
        set { Save("Tag", value); }
    }

    //挂载到其他词条上，合并显示的Trigger（单独生效、合并显示，如：攻击强度1300(+50)) by PY 2017.7.20
    public bool IsPlugable()
    {
        if (Tag == "PLUG")
            return true;
        return false;
    }

    //获取对比的主键
    public string GetCompareWithIndexMainKey(int ignoreIndex)
    {
        string rst = Name;
        for (int i = 0; i < Argvs.Length; ++i)
        {
            if (i != ignoreIndex)
                rst += Argvs[i].ToString();
        }
        return rst;
    }

    public string GetCompareKey()
    {
        try
        {
            var res = ConfigTable.Get<ResourceDTO>("ItemTrigger." + Name);
            if (string.IsNullOrEmpty(res.Tag))
                return Name;
            if (res.Tag.StartsWith("C_WITH_INDEX"))
            {
                int index = int.Parse(res.Tag.Split(':')[1]);
                return GetCompareWithIndexMainKey(index);
            }
            if (res.Tag.StartsWith("C_WITH_KEY"))
            {
                string key = res.Tag.Split(':')[1];
                return key;
            }
            return Name;
        }
        catch
        {
            return Name;
        }
    }


}
