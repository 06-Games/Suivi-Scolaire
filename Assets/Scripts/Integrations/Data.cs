using System.Linq;
using System.Xml.Serialization;

namespace Integrations.Data
{
    public class Data
    {
        //Data file information
        [XmlAttribute] public string ID;
        [XmlAttribute] public string Label;
        [XmlAttribute] public string Provider;
        [XmlIgnore]
        public Provider GetProvider
        {
            get
            {
                var Type = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.FullName == "Integrations.Providers." + Provider);
                return Type == null ? null : (Provider)System.Activator.CreateInstance(Type);
            }
        }
        [XmlAttribute] public System.DateTime CreationDate = System.DateTime.UtcNow;
        [XmlAttribute] public System.DateTime LastLogin = System.DateTime.UtcNow;

        [XmlAttribute("ActiveChild")] public string activeChild;
        [XmlIgnore]
        public ref Child ActiveChild
        {
            get
            {
                if (Children == null || Children.Length == 0) throw new System.InvalidOperationException("Children must be defined");
                for (int i = 0; i < Children.Length; i++) { if (Children[i].id == activeChild) return ref Children[i]; }
                return ref Children[0];
            }
        }
        public Child[] Children;
    }
}
