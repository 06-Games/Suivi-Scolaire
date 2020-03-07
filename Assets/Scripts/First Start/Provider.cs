using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public class Account
    {
        public static Dictionary<string, Provider> Providers = new Dictionary<string, Provider>() {
            { "Local", new Local() },
            { "EcoleDirecte", new EcoleDirecte() }
        };
        public string provider;

        public string id;
        public string password;
        public string child;
    }

    public interface Provider
    {
        string Name { get; }

        IEnumerator GetHolidays(Action<List<Home.Holiday>> onComplete);
        IEnumerator GetSchedule(TimeRange period, Action<List<Schedule.Event>> onComplete);
        IEnumerator GetHomeworks(TimeRange period, Action<List<Homeworks.Homework>> onComplete);
        IEnumerator GetMarks(Action<List<Marks.Period>, List<Subject>, List<Marks.Mark>> onComplete);
    }
    public static class ProviderExtension
    {
        public static T GetModule<T>(this Provider provider)
        {
            try { return (T)provider; }
            catch { return default; }
        }
    }

    public interface Auth { IEnumerator Connect(Account account, Action<Account> onComplete, Action<string> onError); }
}
