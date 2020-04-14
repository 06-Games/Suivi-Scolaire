using System;
using System.Collections.Generic;

public class TimeRange
{
    public DateTime Start;
    public DateTime End;

    public TimeRange() {}
    public TimeRange(DateTime start, DateTime end) { Start = start; End = end; }

    public override string ToString() => $"{Start} - {End}";
    public string ToString(string arg) => $"{Start.ToString(arg)} - {End.ToString(arg)}";

    public IEnumerable<DateTime> DayList()
    {
        for (var day = Start.Date; day.Date <= End.Date; day = day.AddDays(1)) yield return day;
    }
}
