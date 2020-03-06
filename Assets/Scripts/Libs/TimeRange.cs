using System;
public class TimeRange
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }

    public TimeRange(DateTime start, DateTime end) { Start = start; End = end; }
    public override string ToString() => $"{Start} - {End}";
    public string ToString(string arg) => $"{Start.ToString(arg)} - {End.ToString(arg)}";
}
