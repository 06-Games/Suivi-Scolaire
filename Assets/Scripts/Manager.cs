using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject Loading;

    [Header("Modules")]
    public FirstStart FirstStart;
    public Marks.Marks Marks;


    void Start()
    {
        FirstStart.OnComplete += Marks.Initialise;
        FirstStart.Initialise();
    }

    static Manager instance;
    void Awake()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        UnityThread.initUnityThread();
        Logging.Initialise();
        instance = this;
        OpenModule(gameObject); //Close all modules
    }

    public static void OpenModule(GameObject module)
    {
        for (int i = 0; i < instance.transform.childCount; i++) instance.transform.GetChild(i).gameObject.SetActive(false);
        module.SetActive(true);
    }

    public static void UpdateLoadingStatus(string txt)
    {
        Logging.Log(txt);
        instance.Loading.transform.GetChild(1).GetComponent<UnityEngine.UI.Text>().text = txt;
        instance.Loading.SetActive(true);
    }
    public static void HideLoadingPanel() { instance.Loading.SetActive(false); }
}
