using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    public interface Provider { string Name { get; } }
    public static class ProviderExtension
    {
        public static bool TryGetModule<T>(this Provider provider, out T module)
        {
            module = GetModule<T>(provider);
            return !module?.Equals(null) ?? false;
        }
        public static T GetModule<T>(this Provider provider)
        {
            try { return (T)provider; }
            catch { return default; }
        }

        public static IEnumerable<string> Modules(this Provider provider) => provider.GetType().GetInterfaces().Where(i => i.Namespace == "Integrations").Select(i => i.ToString().Substring("Integrations.".Length));
    }

    public interface Auth { IEnumerator Connect(Account account, Action<Account> onComplete, Action<string> onError); }
    public interface Home { IEnumerator GetHolidays(Action<List<global::Home.Holiday>> onComplete); }
    public interface Schedule { IEnumerator GetSchedule(TimeRange period, Action<List<global::Schedule.Event>> onComplete); }
    public interface Homeworks { IEnumerator GetHomeworks(TimeRange period, Action<List<global::Homeworks.Homework>> onComplete); }
    public interface Marks { IEnumerator GetMarks(Action<List<global::Marks.Period>, List<Subject>, List<global::Marks.Mark>> onComplete); }
}
