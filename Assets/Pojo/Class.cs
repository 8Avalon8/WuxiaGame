using System.Xml.Serialization;


namespace JianghuX
{
    /// <summary> 跨平台 </summary>
    [XmlType("class")]
    
    
    public class Class : BasePojo
    {
        public override string PK { get { return Key; } }

        [XmlAttribute("key")]
        public string Key;

        [XmlAttribute("showname")]
        public string ShowName;

        [XmlAttribute("levelimit")]
        public int LeveLimit;

        [XmlAttribute("strgrowth")]
        public int StrGrowth;

        [XmlAttribute("intgrowth")]
        public int IntGrowth;

        [XmlAttribute("agigrowth")]
        public int AgiGrowth;

        [XmlAttribute("pergrowth")]
        public int PerGrowth;

        [XmlAttribute("lucgrowth")]
        public int LucGrowth;

        [XmlAttribute("chagrowth")]
        public int ChaGrowth;

        [XmlAttribute("skilllist")]
        public string SkillList;

        [XmlAttribute("talentlist")]
        public string TalentList;

    }
}
