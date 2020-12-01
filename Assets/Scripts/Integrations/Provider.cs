using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public class Account : IEquatable<Account>
    {
        internal static readonly Dictionary<string, Provider> Providers = new Dictionary<string, Provider> {
            { "Local", new Local() },
            { "EcoleDirecte", new EcoleDirecte() },
            { "CambridgeKids", new CambridgeKids() }
        };

        public string provider;
        public Provider GetProvider => Providers.TryGetValue(provider, out var p) ? p : null;
        public string username;
        public string id;
        public string password;
        public string child;

        public bool Equals(Account other) => other is null ? false : provider == other.provider && username == other.username && id == other.id;
        public override bool Equals(object obj) => Equals(obj as Account);
        public override int GetHashCode() => string.Format("{0}-{1}-{2}", provider, username, id).GetHashCode();
    }

    public interface Provider
    {
        string Name { get; }
        IEnumerator Connect(Account account, Action<Data.Data> onComplete, Action<string> onError);
    }
    public interface Auth : Provider { }
    public interface Periods : Provider { IEnumerator GetPeriods(Action onComplete = null); }
    public interface Schedule : Provider { IEnumerator GetSchedule(TimeRange period, Action<IEnumerable<Data.ScheduledEvent>> onComplete = null); }
    public interface Homeworks : Provider
    {
        IEnumerator<Data.Homework.Period> DiaryPeriods();
        IEnumerator GetHomeworks(Data.Homework.Period period, Action onComplete);
        IEnumerator OpenHomeworkAttachment(Data.Document document);
        IEnumerator HomeworkDoneStatus(Data.Homework homework);
    }
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
