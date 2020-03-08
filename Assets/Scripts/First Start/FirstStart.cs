using Integrations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FirstStart : MonoBehaviour
{
    public System.Action<Provider> onComplete;
    public void Initialise()
    {
        Account account = null;
        try { account = FileFormat.XML.Utils.XMLtoClass<Account>(Security.Encrypting.Decrypt(PlayerPrefs.GetString("Connection"), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG")); } catch { }
        if (account != null)
        {
            var Provider = Account.Providers[account.provider];
            if (Provider.GetModule<Auth>() != null)
            {
                UnityThread.executeCoroutine(Account.Providers[account.provider].GetModule<Auth>().Connect(account,
                    (a) => { onComplete?.Invoke(Provider); },
                    (error) =>
                    {
                        gameObject.SetActive(true);
                        var auth = transform.Find("Content").Find("Auth");
                        auth.gameObject.SetActive(true);
                        auth.Find("Error").GetComponent<Text>().text = error;
                    }
                ));
            }
            else onComplete?.Invoke(Provider);
        }
        else
        {
            for (int i = 0; i < transform.Find("Content").childCount; i++) transform.Find("Content").GetChild(i).gameObject.SetActive(false);
            transform.Find("Content").Find("Welcome").gameObject.SetActive(true);
            Manager.OpenModule(gameObject);
        }
    }

    public void ConnectWith(string provider)
    {
        var Provider = Account.Providers[provider];
        if (Provider.GetModule<Auth>() != null)
        {
            var auth = transform.Find("Content").Find("Auth");

            var returnBtn = transform.Find("Top").Find("Return").GetComponent<Button>();
            returnBtn.onClick.RemoveAllListeners();
            returnBtn.onClick.AddListener(() => { auth.gameObject.SetActive(false); transform.Find("Content").Find("Welcome").gameObject.SetActive(true); returnBtn.gameObject.SetActive(false); });
            returnBtn.gameObject.SetActive(true);

            auth.Find("Connexion").GetComponent<Button>().onClick.RemoveAllListeners();
            auth.Find("Connexion").GetComponent<Button>().onClick.AddListener(() =>
            {
                auth.gameObject.SetActive(false);
                var account = new Account()
                {
                    provider = provider,
                    id = auth.Find("ID").GetComponent<InputField>().text,
                    password = auth.Find("PASSWORD").GetComponent<InputField>().text
                };

                StartCoroutine(Provider.GetModule<Auth>().Connect(account, (acc) =>
                    {
                        if (auth.Find("RememberMe").GetComponent<Toggle>().isOn)
                            PlayerPrefs.SetString("Connection", Security.Encrypting.Encrypt(FileFormat.XML.Utils.ClassToXML(acc), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG"));
                        onComplete?.Invoke(Provider);
                    },
                    (error) =>
                    {
                        auth.gameObject.SetActive(true);
                        auth.Find("Error").GetComponent<Text>().text = error;
                    }
                ));
            });
            auth.gameObject.SetActive(true);
        }
        else onComplete?.Invoke(Provider);
    }
    public void ShowPassword(InputField pass)
    {
        pass.contentType = pass.contentType == InputField.ContentType.Password ? InputField.ContentType.Standard : InputField.ContentType.Password;
        pass.Select();
        StartCoroutine(Deselect());
        System.Collections.IEnumerator Deselect()
        {
            yield return 0; // Skip the first frame in which this is called.
            pass.MoveTextEnd(false); // Do this during the next frame.
        }
    }

    public static void SelectChilds(List<(System.Action, string, Sprite)> childs)
    {
        var instance = Manager.instance.FirstStart.transform;
        var Childs = instance.Find("Content").Find("Childs");
        for (int i = 1; i < Childs.childCount; i++) Destroy(Childs.GetChild(i).gameObject);

        var returnBtn = instance.Find("Top").Find("Return").GetComponent<Button>();
        returnBtn.onClick.RemoveAllListeners();
        returnBtn.onClick.AddListener(() => { Childs.gameObject.SetActive(false); instance.Find("Content").Find("Auth").gameObject.SetActive(true); });
        returnBtn.gameObject.SetActive(true);

        foreach (var child in childs)
        {
            var btn = Instantiate(Childs.GetChild(0).gameObject, Childs);
            btn.GetComponent<Button>().onClick.AddListener(() => { Childs.gameObject.SetActive(false); child.Item1(); });
            btn.transform.GetChild(0).GetComponent<Text>().text = child.Item2;
            if (child.Item3 != null) btn.GetComponent<Image>().sprite = child.Item3;
            btn.SetActive(true);
        }
        Childs.gameObject.SetActive(true);
    }

    public void Logout()
    {
        PlayerPrefs.SetString("Connection", "");
        Manager.OpenModule(gameObject);
        Initialise();

        transform.Find("Top").Find("Return").gameObject.SetActive(false);
        var auth = transform.Find("Content").Find("Auth");
        auth.Find("ID").GetComponent<InputField>().text = "";
        auth.Find("PASSWORD").GetComponent<InputField>().text = "";
    }
}
