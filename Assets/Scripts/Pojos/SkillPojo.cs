using JianghuX;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[XmlType("skill")]
public class SkillPojo : BasePojo
{
    public override string PK { get { return Key; } }
    [XmlAttribute("key")]
    public string Key;
    [XmlAttribute("showname")]
    public string showname;
    [XmlAttribute("wuxue")]
    public string wuxue;
    [XmlAttribute("cost")]
    public string cost;
    [XmlAttribute("type")]
    public string type;
    [XmlAttribute("damageratio")]
    public string damageRatio;
    [XmlAttribute("effect")]
    public string effect;

    public override void InitBind()
    {
        base.InitBind();
    }

}
