using Integrations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Modules
{
    public class Accounts : MonoBehaviour
    {
        public Sprite defaultChildImage;

        public System.Action<Provider> onComplete;

        HashSet<Account> accounts;
        public static Account selectedAccount { get; private set; }
        public void Initialise()
        {
            try
            {
                accounts = FileFormat.XML.Utils.XMLtoClass<HashSet<Account>>(Security.Encrypting.Decrypt(PlayerPrefs.GetString("Accounts"), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG")) ?? new HashSet<Account>();
                if (accounts?.Count == 1) ConnectTo(accounts.FirstOrDefault());
            }
            catch { accounts = new HashSet<Account>(); }
            Refresh();
        }
        public void Refresh()
        {
            transform.Find("Top").Find("Return").gameObject.SetActive(false);
            var welcome = transform.Find("Content").Find("Welcome");
            foreach (Transform gO in welcome.parent) gO.gameObject.SetActive(false);
            welcome.gameObject.SetActive(true);

            var accountList = welcome.GetChild(0).GetComponent<ScrollRect>().content;
            for (int i = 1; i < accountList.childCount; i++) Destroy(accountList.GetChild(i).gameObject);
            foreach (var account in accounts)
            {
                var go = Instantiate(accountList.GetChild(0).gameObject, accountList).transform;
                go.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Providers/" + account.provider);
                go.GetChild(1).GetChild(0).GetComponent<Text>().text = account.GetProvider?.Name ?? account.provider;
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
            UnityThread.executeInLateUpdate(() => UnityThread.executeInLateUpdate(addList.GetComponent<SimpleSideMenu>().Setup));

            Manager.OpenModule(gameObject);
        }
        void ConnectTo(Account account)
        {
            Manager.provider = null;
            var Provider = account.GetProvider;
            selectedAccount = account;
            accounts = new HashSet<Account>(accounts.OrderBy(ac => ac.provider));
            onComplete?.Invoke(Provider);
            return;

            UnityThread.executeCoroutine(Provider.Connect(account,
                (data) =>
                {
                    selectedAccount = account;
                    Manager.Data = data;
                    accounts = new HashSet<Account>(accounts.OrderBy(ac => ac.provider));
                    Save();
                    onComplete?.Invoke(Provider);
                },
                (error) =>
                {
                    if (Provider.TryGetModule<Auth>(out var authModule))
                    {
                        transform.Find("Content").Find("Auth").Find("Error").GetComponent<Text>().text = error;
                        ConnectWith(account.provider);
                    }
                }
            ));
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
                    var account = new Account
                    {
                        provider = provider,
                        id = auth.Find("ID").GetComponent<InputField>().text,
                        password = auth.Find("PASSWORD").GetComponent<InputField>().text
                    };
                    accounts.Add(account);
                    ConnectTo(account);
                });
                auth.gameObject.SetActive(true);
            }
            else ConnectTo(new Account { provider = provider });
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

        public static void SelectChild() { SelectChild(Manager.Child); }
        public static void SelectChild(Integrations.Data.Child selectedChild)
        {
            var childSelection = Manager.instance.Menu.transform.Find("Panel").Find("Child").Find("Slide");

            var instance = Manager.instance.FirstStart;
            selectedAccount.child = selectedChild.id;
            instance.Save();

            var list = childSelection.Find("List");
            for (int i = 1; i < list.childCount; i++) Destroy(list.GetChild(i).gameObject);
            foreach (var child in Manager.Data.Children)
            {
                Transform go = null;
                if (child == selectedChild) go = childSelection.Find("Selected");
                else
                {
                    go = Instantiate(list.GetChild(0).gameObject, list).transform;
                    go.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        instance.ResetData();
                        SelectChild(child);

                        foreach (Transform module in Manager.instance.transform)
                            if (module.gameObject.activeInHierarchy && module.TryGetComponent<Module>(out var m)) m.OnEnable();
                        Manager.instance.Menu.UpdateModules();
                    });
                }
                go.Find("ImageBG").Find("Image").GetComponent<Image>().sprite = child.sprite ?? instance.defaultChildImage;
                go.Find("Text").GetComponent<Text>().text = child.name;
                go.gameObject.SetActive(true);
            }
            var rect = childSelection.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 40 * (Manager.Data.Children.Length - 1));
            childSelection.GetComponent<SimpleSideMenu>().Setup();
            rect.pivot = new Vector2(0.5F, 0);
        }

        public void Logout()
        {
            accounts.Remove(selectedAccount);
            selectedAccount = null;
            Save();
            Manager.OpenModule(gameObject);
            ResetData();
            Initialise();

            transform.Find("Top").Find("Return").gameObject.SetActive(false);
            var auth = transform.Find("Content").Find("Auth");
            auth.Find("ID").GetComponent<InputField>().text = "";
            auth.Find("PASSWORD").GetComponent<InputField>().text = "";
        }
        public void ResetData() { foreach (var d in Manager.instance.modules) d?.Reset(); }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var returnBtn = transform.Find("Top").Find("Return").GetComponent<Button>();
                if (returnBtn.gameObject.activeInHierarchy) returnBtn.onClick.Invoke();
            }
        }
    }
}
