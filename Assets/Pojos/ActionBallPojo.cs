using JianghuX;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[XmlType("actionball")]
public class ActionBallPojo : BasePojo
{

    public override string PK { get { return Key; } }
    [XmlAttribute("key")]
    public string Key;
    [XmlAttribute("showname")]
    public string showname;
    [XmlAttribute("index")]
    public int index;
    [XmlAttribute("iconstr")]
    public string iconstr;
    [XmlAttribute("desc")]
    public string desc;

    public override void InitBind()
    {
        base.InitBind();
    }
}
