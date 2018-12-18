using System.Xml.Serialization;


namespace JianghuX
{
    /// <summary> 跨平台 </summary>
    [XmlType("role")]
    
    
    public class Role : BasePojo
    {
        public override string PK { get { return Key; } }

        [XmlAttribute("key")]
        public string Key;

        [XmlAttribute("showname")]
        public string ShowName;

        [XmlAttribute("baseclass")]
        public string BaseClass;

        [XmlAttribute("talents")]
        public string Talents;

        [XmlAttribute("defaultskills")]
        public string DefaultSkills;

        [XmlAttribute("equipments")]
        public string Equipments;

        [XmlAttribute("strength")]
        public int Strength;

        [XmlAttribute("intelligence")]
        public int Intelligence;

        [XmlAttribute("agility")]
        public int Agility;

        [XmlAttribute("perception")]
        public int Perception;

        [XmlAttribute("charisma")]
        public int Charisma;

        [XmlAttribute("luck")]
        public int Luck;
    }
}
