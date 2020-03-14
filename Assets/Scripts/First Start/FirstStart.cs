using Integrations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FirstStart : MonoBehaviour
{
    public System.Action<Provider> onComplete;

    List<Account> accounts = null;
    Account selectedAccount = null;
    public void Initialise()
    {
        try
        {
            accounts = FileFormat.XML.Utils.XMLtoClass<List<Account>>(Security.Encrypting.Decrypt(PlayerPrefs.GetString("Accounts"), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG")) ?? new List<Account>();
            if (accounts?.Count == 1) selectedAccount = accounts[0];
        }
        catch { accounts = new List<Account>(); }
        if (selectedAccount != null) ConnectTo(selectedAccount);
        else Refresh();
    }
    public void Refresh()
    {
        var welcome = transform.Find("Content").Find("Welcome");
        foreach (Transform gO in welcome.parent) gO.gameObject.SetActive(false);
        welcome.gameObject.SetActive(true);
        var accountList = welcome.GetChild(0).GetComponent<ScrollRect>().content;
        for (int i = 1; i < accountList.childCount; i++) Destroy(accountList.GetChild(i).gameObject);
        foreach (var account in accounts)
        {
            var go = Instantiate(accountList.GetChild(0).gameObject, accountList).transform;
            go.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Providers/" + account.provider);
            go.GetChild(1).GetChild(0).GetComponent<Text>().text = account.provider;
            go.GetChild(1).GetChild(1).GetComponent<Text>().text = account.username;
            go.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { accounts.Remove(account); Save(); Refresh(); });
            go.GetComponent<Button>().onClick.AddListener(() => ConnectTo(account));
            go.gameObject.SetActive(true);
        }
        var addList = welcome.Find("Add");
        for (int i = 2; i < addList.childCount; i++) Destroy(addList.GetChild(i).gameObject);
        foreach (var provider in Account.Providers)
        {
            var go = Instantiate(addList.GetChild(1).gameObject, addList).transform;
            go.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Providers/" + provider.Key);
            go.GetChild(1).GetComponent<Text>().text = provider.Value.Name;
            go.GetComponent<Button>().onClick.AddListener(() => { ConnectWith(provider.Key); addList.GetComponent<SimpleSideMenu>().Close(); });
            go.gameObject.SetActive(true);
        }
        Manager.OpenModule(gameObject);
    }
    void ConnectTo(Account account)
    {
        selectedAccount = account;
        var Provider = Account.Providers[selectedAccount.provider];
        if (Provider.TryGetModule<Auth>(out var authModule))
        {
            UnityThread.executeCoroutine(authModule.Connect(selectedAccount,
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

    public void ConnectWith(string provider)
    {
        var Provider = Account.Providers[provider];
        foreach (Transform gO in transform.Find("Content")) gO.gameObject.SetActive(false);
        if (Provider.TryGetModule<Auth>(out var authModule))
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

                StartCoroutine(authModule.Connect(account, (acc) =>
                    {
                        if (auth.Find("RememberMe").GetComponent<Toggle>().isOn)
                        {
                            accounts.Add(acc);
                            Save();
                        }
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
    void Save() => PlayerPrefs.SetString("Accounts", Security.Encrypting.Encrypt(FileFormat.XML.Utils.ClassToXML(accounts), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG"));
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var returnBtn = transform.Find("Top").Find("Return").GetComponent<Button>();
            if (returnBtn.gameObject.activeInHierarchy) returnBtn.onClick.Invoke();
        }
    }
}
