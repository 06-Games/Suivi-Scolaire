using System.Collections.Generic;

namespace Messanging
{
    public class Message
    {
        public uint id;
        public string subject;
        public System.DateTime date;
        public bool read;
        public enum Type { received, sent }
        public Type type;
        public List<string> correspondents;

        public Extra extra;
        public class Extra
        {
            public string content;
            public IEnumerable<Request> documents = new List<Request>();
        }
    }
}
