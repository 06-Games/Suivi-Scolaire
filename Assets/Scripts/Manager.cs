using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject Loading;

    [Header("Modules")]
    public FirstStart FirstStart;
    public Home.Home Home;
    public GameObject[] modules;

    public static Integrations.Provider provider;
    void Start()
    {
        FirstStart.onComplete += (Provider) =>
        {
            provider = Provider;
            HideLoadingPanel();
            OpenModule(Home.gameObject);
        };
        FirstStart.Initialise();
    }

    internal static Manager instance;
    void Awake()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        UnityThread.initUnityThread();
        Logging.Initialise();
        instance = this;
        for (int i = 0; i < instance.transform.childCount; i++) instance.transform.GetChild(i).gameObject.SetActive(false); //Close all modules
    }

    public void OpenModuleEditor(GameObject module) => OpenModule(module);
    public static void OpenModule(GameObject module) { foreach (var obj in instance.modules) obj.SetActive(obj == module); }

    public static void UpdateLoadingStatus(string txt)
    {
        Logging.Log(txt);
        instance.Loading.transform.GetChild(1).GetComponent<UnityEngine.UI.Text>().text = txt;
        instance.Loading.SetActive(true);
    }
    public static void HideLoadingPanel() => instance.Loading.SetActive(false);

    public static bool isReady => provider != null;
}
