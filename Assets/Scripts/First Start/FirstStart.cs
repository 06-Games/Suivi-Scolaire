using Integrations;
using UnityEngine;
using UnityEngine.UI;

public class FirstStart : MonoBehaviour
{
    public System.Action<Note[]> OnComplete;

    public void Initialise()
    {
        ModelClass.OnError += (error) =>
        {
            var auth = transform.Find("Auth").Find("Content");
            auth.parent.gameObject.SetActive(true);
            auth.Find("Error").GetComponent<Text>().text = error;
        };
        ModelClass.OnComplete += OnComplete;
        ModelClass.FirstStart = this;

        try
        {
            var account = FileFormat.XML.Utils.XMLtoClass<Account>(Security.Encrypting.Decrypt(PlayerPrefs.GetString("Connection"), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG"));
            UnityThread.executeCoroutine(Account.Types[account.type].Connect(account, false));
        }
        catch
        {
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
            transform.Find("Welcome").gameObject.SetActive(true);
            Manager.OpenModule(gameObject);
        }
    }

    public void ConnectWith(string provider)
    {
        var Provider = Account.Types[provider];
        if (Provider.NeedAuth)
        {
            var auth = transform.Find("Auth").Find("Content");
            auth.Find("Connexion").GetComponent<Button>().onClick.RemoveAllListeners();
            auth.Find("Connexion").GetComponent<Button>().onClick.AddListener(() =>
            {
                auth.parent.gameObject.SetActive(false);
                var account = new Account()
                {
                    type = provider,
                    id = auth.Find("ID").GetComponent<InputField>().text,
                    password = auth.Find("PASSWORD").GetComponent<InputField>().text
                };

                StartCoroutine(Provider.Connect(account, auth.Find("RememberMe").GetComponent<Toggle>().isOn));
            });
            auth.parent.gameObject.SetActive(true);
        }
        else StartCoroutine(Provider.Connect(new Account() { type = provider }, false));
    }

    public void SelectChilds(System.Collections.Generic.List<(System.Action, string, Sprite)> childs)
    {
        var Childs = transform.Find("Childs").Find("Content");
        for (int i = 1; i < Childs.childCount; i++) Destroy(Childs.GetChild(i).gameObject);
        foreach (var child in childs)
        {
            var btn = Instantiate(Childs.GetChild(0).gameObject, Childs);
            btn.GetComponent<Button>().onClick.AddListener(() => { Childs.parent.gameObject.SetActive(false); child.Item1(); });
            btn.transform.GetChild(0).GetComponent<Text>().text = child.Item2;
            if(child.Item3 != null) btn.GetComponent<Image>().sprite = child.Item3;
            btn.SetActive(true);
        }
        Childs.parent.gameObject.SetActive(true);
    }
}
