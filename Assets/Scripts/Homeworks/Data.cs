using System.Collections.Generic;
using DateTime = System.DateTime;

namespace Homeworks
{
    public class Homework
    {
        public Subject subject;
        public DateTime forThe;
        public DateTime addedThe;
        public string addedBy;
        public string content;
        public bool done;
        public bool exam;
        public IEnumerable<(string, string, UnityEngine.WWWForm, (string, string)[], bool)> documents = new List<(string, string, UnityEngine.WWWForm, (string, string)[], bool)>();
    }
    public class Period
    {
        public string name;
        public string id;
        public TimeRange timeRange;
    }
}
