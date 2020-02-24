using Integrations;
using UnityEngine;
using UnityEngine.UI;

public class Marks : MonoBehaviour
{
    public GameObject FirstStart;
    public GameObject Loading;

    public static Note[] Notes;

    void Awake()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        UnityThread.initUnityThread();
        Logging.Initialise();

        ModelClass.OnError += (error) =>
        {
            var auth = FirstStart.transform.Find("Auth").Find("Content");
            auth.parent.gameObject.SetActive(true);
            auth.Find("Error").GetComponent<Text>().text = error;
        };
        ModelClass.OnComplete += (marks) => Debug.Log(FileFormat.XML.Utils.ClassToXML(marks, false));
        ModelClass.Childs = FirstStart.transform.Find("Childs").Find("Content");
        ModelClass.Loading = Loading;
    }

    void Start()
    {
        try
        {
            var account = FileFormat.XML.Utils.XMLtoClass<Account>(Security.Encrypting.Decrypt(PlayerPrefs.GetString("Connection"), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG"));
            StartCoroutine(Account.Types[account.type].Connect(account, false));
        }
        catch
        {
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
            FirstStart.transform.Find("Welcome").gameObject.SetActive(true);
            FirstStart.SetActive(true);
        }
    }

    public void ConnectWith(string provider)
    {
        var Provider = Account.Types[provider];
        if (Provider.NeedAuth)
        {
            var auth = FirstStart.transform.Find("Auth").Find("Content");
            auth.Find("Connexion").GetComponent<Button>().onClick.RemoveAllListeners();
            auth.Find("Connexion").GetComponent<Button>().onClick.AddListener(() =>
            {
                auth.parent.gameObject.SetActive(false);
                var account = new Account()
                {
                    type = provider,
                    id = auth.Find("ID").GetComponent<InputField>().text,
                    password = auth.Find("MDP").GetComponent<InputField>().text
                };

                StartCoroutine(Provider.Connect(account, auth.Find("RememberMe").GetComponent<Toggle>().isOn));
            });
            auth.parent.gameObject.SetActive(true);
        }
        else StartCoroutine(Provider.Connect(new Account() { type = provider }, false));
    }
}

public class Note
{
    public string nom;
    public string codePeriode;
    public string codeMatiere;
    public string libelleMatiere;
    public string codeSousMatiere;
    public string typeDevoir;
    public bool enLettre;
    public float coef;
    public float noteSur;
    public float? valeur;
    public bool nonSignificatif;
    public System.DateTime date;
    public System.DateTime dateSaisie;
    public bool valeurisee;
    public float? moyenneClasse;
    public Competence[] competences;
}

public class Competence
{
    public string nom;
    public uint? id;
    public string valeur;
    public bool cdt;
    public uint idCat;
    public string libelleCat;
}