using Home;
using Homeworks;
using Marks;
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
        bool NeedAuth { get; }

        IEnumerator GetMarks(System.Action<List<Period>, List<Subject>, List<Mark>> onComplete);
        IEnumerator GetHomeworks(System.Action<List<Homework>> onComplete);
        IEnumerator GetHolidays(System.Action<List<Holiday>> onComplete);
    }
    public static class ProviderExtension
    {
        public static T GetModule<T>(this Provider provider)
        {
            try { return (T)provider; }
            catch { return default; }
        }
    }

    public interface Auth { IEnumerator Connect(Account account, System.Action<Account> onComplete, System.Action<string> onError); }
}
