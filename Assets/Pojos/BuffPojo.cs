using JianghuX;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public enum BuffType
{
    BUFF = 0,
    DEBUFF = 1,
    SKILLEFEECT = 2,
}

public class BuffPojo : BasePojo
{
    public override string PK { get { return Key; } }
    [XmlAttribute("key")]
    public string Key;
    [XmlAttribute("showname")]
    public string showname;
    [XmlAttribute("type")]
    public string type;
    [XmlAttribute("desc")]
    public string desc;

    public bool IsBuff()
    {
        return type == "0";
    }

}
