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
        public Period period;
        public DateTime date;
        public DateTime dateAdded;

        //Infos
        public Subject subject;
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
