using Home;
using Homeworks;
using Marks;
using Schedule;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public class Local : Provider
    {
        public string Name => "Local";

        public IEnumerator GetMarks(Action<List<Period>, List<Subject>, List<Mark>> onComplete) { onComplete?.Invoke(new List<Period>(), new List<Subject>(), new List<Mark>()); yield break; }
        public IEnumerator GetHomeworks(TimeRange period, Action<List<Homework>> onComplete) { onComplete?.Invoke(new List<Homework>()); yield break; }
        public IEnumerator GetSchedule(TimeRange period, Action<List<Event>> onComplete) { onComplete?.Invoke(new List<Event>()); yield break; }
        public IEnumerator GetHolidays(Action<List<Holiday>> onComplete) { onComplete?.Invoke(new List<Holiday>()); yield break; }
    }
}
