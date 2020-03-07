using DateTime = System.DateTime;

namespace Schedule
{
    public class Event
    {
        public Subject subject;
        public DateTime start;
        public DateTime end;
        public string room;
        public bool canceled;
    }
}
