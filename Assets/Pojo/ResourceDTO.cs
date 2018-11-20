using System.Xml.Serialization;


namespace JianghuX
{
    /// <summary> 跨平台 </summary>
    [XmlType("resource")]
    
    
    public class ResourceDTO : BasePojo
    {
        public override string PK { get { return Key; } }

        [XmlAttribute("key")]
        public string Key;

        [XmlAttribute("value")]
        public string Value;

        [XmlAttribute("tag")]
        public string Tag;

        [XmlAttribute("icon")]
        public string Icon;

        public override void InitBind()
        {
            base.InitBind();
        }
    }
}
