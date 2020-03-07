using Home;
using Homeworks;
using Marks;
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

        IEnumerator GetMarks(Action<List<Period>, List<Subject>, List<Mark>> onComplete);
        IEnumerator GetHomeworks(TimeRange period, Action<List<Homework>> onComplete);
        IEnumerator GetHolidays(Action<List<Holiday>> onComplete);
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
