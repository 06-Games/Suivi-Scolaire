using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public interface Model
    {
        string Name { get; }
        bool NeedAuth { get; }

        IEnumerator Connect(Account account, bool save);
    }

    public class ModelClass
    {
        public static System.Action<string> OnError { get; set; }
        public static System.Action<Note[]> OnComplete { get; set; }
        public static FirstStart FirstStart { get; set; }
    }

    public class Account
    {
        public static Dictionary<string, Model> Types = new Dictionary<string, Model>() {
            { "Local", new Local() },
            { "EcoleDirecte", new EcoleDirecte() }
        };
        public string type;

        public string id;
        public string password;
        public string child;
    }
}
