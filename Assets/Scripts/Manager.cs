using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject Loading;
    public Sprite LoadingError;

    [Header("Modules")]
    public FirstStart FirstStart;
    public Home.Home Home;

    public static Integrations.Provider provider;
    void Start()
    {
        FirstStart.onComplete += (Provider) =>
        {
            provider = Provider;
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
    public static void OpenModule(GameObject module)
    {
        foreach (Transform obj in instance.transform) obj.gameObject.SetActive(obj.gameObject == module);
    }

    public static void UpdateLoadingStatus(string id, string fallback, bool log = true, params string[] args)
    {
        if (log) Logging.Log(LangueAPI.Get(null, fallback, args));
        instance.Loading.SetActive(true);
        var img = instance.Loading.transform.GetChild(0);
        img.GetComponent<UnityEngine.UI.Image>().color = Color.white;
        img.GetComponent<SpriteAnimator>().Play();
        instance.Loading.transform.GetChild(1).GetComponent<UnityEngine.UI.Text>().text = LangueAPI.Get(id, fallback, args);
    }
    public static void FatalErrorDuringLoading(string txt, string log)
    {
        Logging.Log(log, LogType.Error);
        var img = instance.Loading.transform.GetChild(0);
        img.GetComponent<SpriteAnimator>().Stop();
        img.GetComponent<UnityEngine.UI.Image>().sprite = instance.LoadingError;
        img.GetComponent<UnityEngine.UI.Image>().color = Color.red;
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

    public static bool isReady => provider != null;
}
