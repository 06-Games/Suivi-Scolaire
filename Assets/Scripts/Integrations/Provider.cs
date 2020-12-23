using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public interface Provider
    {
        string Name { get; }
        IEnumerator GetInfos(Data.Data data, Action<Data.Data> onComplete);
    }
    public interface Auth : Provider { IEnumerator Connect(KeyValuePair<string, string> account, Action onComplete, Action<string> onError); }
    public interface Periods : Provider { IEnumerator GetPeriods(Action onComplete = null); }
    public interface Schedule : Provider { IEnumerator GetSchedule(TimeRange period, Action<IEnumerable<Data.ScheduledEvent>> onComplete = null); }
    public interface Homeworks : Provider
    {
        IEnumerator<Data.Homework.Period> DiaryPeriods();
        IEnumerator GetHomeworks(Data.Homework.Period period, Action onComplete);
        IEnumerator OpenHomeworkAttachment(Data.Document document);
        IEnumerator HomeworkDoneStatus(Data.Homework homework);
    }
    public interface SessionContent : Provider { IEnumerator GetSessionContent(TimeRange period, Action onComplete); }
    public interface Marks : Provider { IEnumerator GetMarks(Action onComplete = null); }
    public interface Messanging : Provider
    {
        IEnumerator GetMessages(Action onComplete = null);
        IEnumerator LoadExtraMessageData(uint messageID, Action onComplete = null);
        IEnumerator OpenMessageAttachment(Data.Document document);
    }
    public interface Books : Provider { IEnumerator GetBooks(Action onComplete); IEnumerator OpenBook(Data.Book book); }
    public interface Documents : Provider { IEnumerator GetDocuments(Action onComplete); IEnumerator OpenDocument(Data.Document document); }
}
