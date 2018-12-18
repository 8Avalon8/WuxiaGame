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
    [XmlAttribute("skillCost")]
    public string skillCost;
    [XmlAttribute("costMp")]
    public int costMp;
    [XmlAttribute("startCoolDown")]
    public int startCoolDown;
    [XmlAttribute("coolDown")]
    public int coolDown;
    [XmlAttribute("type")]
    public string type;
    [XmlAttribute("damageRatio")]
    public string damageRatio;
    [XmlAttribute("enemyEffect")]
    public string enemyEffect;
    [XmlAttribute("ownSideEffect")]
    public string ownSideEffect;
    [XmlAttribute("ballAddingEffect")]
    public string ballAddingEffect;

    public override void InitBind()
    {
        base.InitBind();
    }

}
