using Integrations;
using Integrations.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Credential = System.Collections.Generic.KeyValuePair<string, string>;

namespace Modules
{
    /// <remarks>
    /// Confidential information is safeguarded thanks to a second part of this class kept top secret ;)
    /// If for some reason you would like to know how it works to improve its reliability, 
    /// contact me and I will give you the outline of its operation.
    /// For any security problem, please contact me through my discord server: https://discord.gg/PaFbgFT
    /// </remarks>
    public partial class Accounts : MonoBehaviour
    {
        public Sprite defaultChildImage;

        static IEnumerable<FileInfo> accounts;
        static Dictionary<string, Credential> credentials;
        public static Credential GetCredential { get { Logging.Log("Credentials have been asked"); credentials.TryGetValue(Manager.Data.ID, out var c); return c; } }

        public void OnEnable()
        {
            accounts = Saving.dataPath.EnumerateFiles($"*{Saving.dataExtension}");
            credentials = SuperSecretAccountsInformationYouWontKnow;
            Refresh();
        }
        public void Refresh()
        {
            var selectPanel = transform.Find("Content").Find("Select");

            var list = selectPanel.Find("List").GetComponent<ScrollRect>().content;
            for (int i = 1; i < list.childCount; i++) Destroy(list.GetChild(i).gameObject);
            foreach (var dataFile in accounts)
            {
                var account = Saving.LoadData(dataFile);
                if (account == null) continue;
                if (account.ID == null) account.ID = dataFile.Name.Substring(0, dataFile.Name.Length - Saving.dataExtension.Length);
                var credential = credentials.TryGetValue(account.ID, out var c) ? c : (Credential?)null;

                var go = Instantiate(list.GetChild(0).gameObject, list).transform;
                go.name = $"{account.Provider}: {account.Label}";

                go.Find("Logo").GetComponent<Image>().sprite = Resources.Load<Sprite>("Providers/" + account.Provider);
                var infos = go.Find("Infos");
                infos.Find("Provider").GetComponent<Text>().text = account.GetProvider?.Name ?? account.Provider;
                infos.Find("Label").GetComponent<Text>().text = account.Label;
                infos.Find("GUID").GetComponent<Text>().text = account.ID;
                go.Find("Delete").GetComponent<Button>().onClick.AddListener(() => { SetupDeletePanel(dataFile, account); });
                go.GetComponent<Button>().onClick.AddListener(() => Connect(account?.GetProvider, account, credential));

                go.gameObject.SetActive(true);
            }

            var addList = selectPanel.Find("Add");
            for (int i = 2; i < addList.childCount; i++) Destroy(addList.GetChild(i).gameObject);
            var providers = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == "Integrations.Providers" && t.GetInterface("Provider") != null);
            foreach (var provider in providers)
            {
                Provider providerModule = (Provider)System.Activator.CreateInstance(provider);

                var go = Instantiate(addList.GetChild(1).gameObject, addList).transform;
                go.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Providers/" + provider.Name);
                go.GetChild(1).GetComponent<Text>().text = providerModule?.Name;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Connect(providerModule);
                    addList.GetComponent<SimpleSideMenu>().Close();
                });
                go.gameObject.SetActive(true);
            }
            UnityThread.executeInLateUpdate(() => UnityThread.executeInLateUpdate(addList.GetComponent<SimpleSideMenu>().Setup));
            OpenPanel(selectPanel.gameObject);
        }

        void Connect(Provider provider, Data data = null, Credential? credential = null)
        {
            if (credential == null && provider.TryGetModule<Auth>(out var auth))
            {
                var content = transform.Find("Content");
                var authPanel = content.Find("Auth");
                OpenPanel(authPanel.gameObject);

                var btn = authPanel.Find("Login").GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    credential = new Credential(authPanel.Find("ID").GetComponent<InputField>().text, authPanel.Find("Password").GetComponent<InputField>().text);
                    StartCoroutine(auth.Connect(credential.Value, () =>
                    {
                        bool dataIsNull = data == null;
                        if (dataIsNull) data = CreateData();

                        UpdateCredentials(data.ID, credential.Value);

                        if (dataIsNull)
                        {
                            Manager.Data = data;
                            StartCoroutine(provider.GetInfos(data, Complete));
                        }
                        else Complete(data);
                    }, (error) => authPanel.Find("Error").GetComponent<Text>().text = error));

                }); 
                OpenPanel(authPanel.gameObject);
            }
            else if (data == null) StartCoroutine(provider.GetInfos(CreateData(), Complete));
            else Complete(data);

            Data CreateData() => new Data { ID = System.Guid.NewGuid().ToString("D"), Provider = provider.GetType().Name };
            void Complete(Data d)
            {
                d.LastLogin = System.DateTime.UtcNow;
                Manager.Data = d;
                Manager.provider = provider;
                Manager.instance.Menu.sideMenu.handle.SetActive(true);
                Menu.SelectChild();
                Manager.OpenModule(Manager.instance.Home.gameObject);
            }
        }

        void UpdateCredentials(string key, KeyValuePair<string, string>? value = null)
        {
            if (credentials.ContainsKey(key)) credentials.Remove(key);
            if (value.HasValue) credentials.Add(key, value.Value);
            SuperSecretAccountsInformationYouWontKnow = credentials;
        }

        void SetupDeletePanel(FileInfo file, Data data)
        {
            var delete = transform.Find("Content").Find("Delete Confirm");
            delete.Find("Text").GetComponent<Text>().text = LangueAPI.Get("welcome.delete", "Are you sure you want to delete the \"<size=20><color=grey>[0]</color></size>\" account?", data.ID);
            var yesBtn = delete.Find("Buttons").Find("Yes").GetComponent<Button>();
            yesBtn.onClick.RemoveAllListeners();
            yesBtn.onClick.AddListener(() => {
                UpdateCredentials(data.ID);
                file.Delete();
                Refresh();
            });
            OpenPanel(delete.gameObject);
        }
        public void OpenPanel(GameObject panel)
        {
            foreach (Transform go in transform.Find("Content")) go.gameObject.SetActive(false);
            panel.SetActive(true);
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
    }
}
