using System.Linq;
using System.Xml.Serialization;
using DateTime = System.DateTime;

public class Subject
{
    public string id;
    public string name;
    public float coef;
    public string[] teachers;
}

namespace Marks
{
    public class Period
    {
        public string id;
        public string name;
        public DateTime start;
        public DateTime end;
    }

    public class Mark
    {
        //Date
        [XmlIgnore] public Period period => Marks.periods.FirstOrDefault(p => p.id == periodID);
        public string periodID;
        public DateTime date;
        public DateTime dateAdded;

        //Infos
        [XmlIgnore] public Subject subject => Marks.subjects.FirstOrDefault(s => s.id == subjectID);
        public string subjectID;
        public string name;
        public float coef;
        public float? mark;
        public float markOutOf;
        public Skill[] skills;
        public float? classAverage;
        public bool notSignificant;
    }

    public class Skill
    {
        public uint? id;
        public string name;
        public string value;
        public uint categoryID;
        public string categoryName;
    }
}
