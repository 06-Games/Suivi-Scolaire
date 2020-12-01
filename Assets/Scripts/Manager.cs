using Modules;
using UnityEngine;

public class Manager : MonoBehaviour
{
    [Header("Modules")]
    public Accounts FirstStart;
    public Home Home;
    public System.Collections.Generic.List<Module> modules = new System.Collections.Generic.List<Module>();

    [Header("Others")]
    public Menu Menu;
    public GameObject Loading;
    public Sprite LoadingError;

    public static Integrations.Provider provider { get; set; }
    public static Integrations.Data.Data Data { get; set; }
    static Integrations.Data.Child nullChild;
    public static ref Integrations.Data.Child Child
    {
        get
        {
            if (Data?.Children == null) return ref nullChild;
            for (int i = 0; i < Data.Children.Length; i++) { if (Data.Children[i].id == Accounts.selectedAccount.child) return ref Data.Children[i]; }
            return ref Data.Children[0];
        }
    }
    static System.IO.FileInfo dataFile
    {
        get
        {
            var dir = new System.IO.DirectoryInfo($"{Application.persistentDataPath}/data/");
            if (!dir.Exists) dir.Create();
            return new System.IO.FileInfo($"{dir.FullName}/{Accounts.selectedAccount.provider}_{Accounts.selectedAccount.username}.xml"
#if !UNITY_EDITOR
            + ".gz"
#endif
            );
        }
    }
    public static void LoadData()
    {
        if (provider == null) { Debug.LogError("Provider is null"); return; }
        var file = dataFile;
        if (file.Exists)
        {
            string text;
#if UNITY_EDITOR
            text = System.IO.File.ReadAllText(file.FullName);
#else
            using (var msi = dataFile.OpenRead())
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            using (var mso = new System.IO.StreamReader(gs)) text = mso.ReadToEnd();
#endif
            Data = FileFormat.XML.Utils.XMLtoClass<Integrations.Data.Data>(text) ?? Data;
        }
        Logging.Log("Data loaded");
    }
    public static void SaveData()
    {
        if (!isReady || Data == null) return;

        var text = FileFormat.XML.Utils.ClassToXML(Data, false);
#if UNITY_EDITOR
        System.IO.File.WriteAllText(dataFile.FullName, text);
#else
        using (var msi = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)))
        using (var mso = new System.IO.MemoryStream())
        {
            using (var gs = new GZipStream(mso, CompressionMode.Compress)) msi.CopyTo(gs);
            System.IO.File.WriteAllBytes(dataFile.FullName, mso.ToArray());
        }
#endif

        Logging.Log("Data saved");
    }
    private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) SaveData(); }
    private void OnApplicationQuit() => SaveData();
    public static bool isReady => provider != null && Child != null;

    void Start()
    {
        FirstStart.onComplete += (Provider) =>
        {
            provider = Provider;
            Menu.sideMenu.handle.SetActive(true);
            Accounts.SelectChild();
            Data.LastLogin = System.DateTime.Now;
            OpenModule(Home.gameObject);
        };
        Menu.sideMenu.handle.SetActive(false);
        FirstStart.Initialise();
        foreach (Transform obj in transform) modules.Add(obj.GetComponent<Module>());
    }


    internal static Manager instance { get; private set; }
    void Awake()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        UnityThread.initUnityThread();
        Logging.Initialise();
        instance = this;
        for (int i = 0; i < instance.transform.childCount; i++) instance.transform.GetChild(i).gameObject.SetActive(false); //Close all modules
    }

    public void OpenModuleEditor(GameObject module) => OpenModule(module);
    public static void OpenModule(GameObject module)
    {
        foreach (Transform obj in instance.transform) obj.gameObject.SetActive(obj.gameObject == module);
    }

    public static void UpdateLoadingStatus(string id, string fallback, bool log = true, params string[] args)
    {
        if (log) Logging.Log(LangueAPI.Get(null, fallback, args));
        instance.Loading.SetActive(true);
        var img = instance.Loading.transform.GetChild(0);
        img.GetComponent<UnityEngine.UI.RawImage>().color = Color.white;
        img.GetComponent<VideoRenderer>().Play();
        instance.Loading.transform.GetChild(1).GetComponent<UnityEngine.UI.Text>().text = LangueAPI.Get(id, fallback, args);
    }
    public static void FatalErrorDuringLoading(string txt, string log)
    {
        Logging.Log(log, LogType.Error);
        var img = instance.Loading.transform.GetChild(0);
        img.GetComponent<VideoRenderer>().Stop();
        img.GetComponent<UnityEngine.UI.RawImage>().texture = instance.LoadingError.texture;
        img.GetComponent<UnityEngine.UI.RawImage>().color = Color.red;
        instance.Loading.transform.GetChild(1).GetComponent<UnityEngine.UI.Text>().text = $"<color=red>{txt}</color>";
        instance.Loading.SetActive(true);

        UnityThread.executeCoroutine(Wait());
        System.Collections.IEnumerator Wait()
        {
            yield return new WaitForSeconds(2);
            HideLoadingPanel();
        }
    }
    public static void HideLoadingPanel() => instance.Loading.SetActive(false);
}

public interface Module { void OnEnable(); void Reset(); void Reload(); }
