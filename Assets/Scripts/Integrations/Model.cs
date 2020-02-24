using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        public static GameObject Loading;
        public void Log(string txt)
        {
            Logging.Log(txt);
            Loading.transform.GetChild(1).GetComponent<Text>().text = txt;
            Loading.SetActive(true);
        }

        public static Transform Childs;
        public void SelectChilds(List<(System.Action, string, Sprite)> childs)
        {
            for (int i = 1; i < Childs.childCount; i++) Object.Destroy(Childs.GetChild(i).gameObject);
            foreach (var child in childs)
            {
                var btn = Object.Instantiate(Childs.GetChild(0).gameObject, Childs);
                btn.GetComponent<Button>().onClick.AddListener(() => { Childs.parent.gameObject.SetActive(false); child.Item1(); });
                btn.transform.GetChild(0).GetComponent<Text>().text = child.Item2;
                btn.GetComponent<Image>().sprite = child.Item3;
                btn.SetActive(true);
            }
            Childs.parent.gameObject.SetActive(true);
        }
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
