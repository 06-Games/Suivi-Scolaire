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
        public IEnumerable<(string, string, UnityEngine.WWWForm)> documents;
    }
}
