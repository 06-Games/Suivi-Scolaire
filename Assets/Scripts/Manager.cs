using Modules;
using UnityEngine;

public class Manager : MonoBehaviour
{
    [Header("Modules")]
    public Accounts Account;
    public Home Home;
    public System.Collections.Generic.List<Module> modules = new System.Collections.Generic.List<Module>();

    [Header("Others")]
    public Menu Menu;
    public GameObject Loading;
    public Sprite LoadingError;

    public static Integrations.Provider provider { get; set; }
    public static Integrations.Data.Data Data { get; set; }
    public void Reset() { provider = null; Data = null; }
    private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) Integrations.Saving.SaveData(); }
    private void OnApplicationQuit() => Integrations.Saving.SaveData();
    public static bool isReady => provider != null && Data != null;

    internal static Manager instance { get; private set; }
    void Awake()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        UnityThread.initUnityThread();
        Logging.Initialise();

        instance = this;
        for (int i = 0; i < instance.transform.childCount; i++) instance.transform.GetChild(i).gameObject.SetActive(false); //Close all modules
    }


    void Start()
    {
        Menu.sideMenu.handle.SetActive(false);
        Account.gameObject.SetActive(true);
        foreach (Transform obj in transform) modules.Add(obj.GetComponent<Module>());
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
