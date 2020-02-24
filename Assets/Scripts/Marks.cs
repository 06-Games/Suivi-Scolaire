using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Account
{
    public enum Type { Local, EcoleDirecte }
    public Type type;
    public string id;
    public string password;
    public string child;
}

public class Marks : MonoBehaviour
{
    public GameObject EcoleDirecte;
    public GameObject Loading;

    void Awake()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        UnityThread.initUnityThread();
        Logging.Initialise();
    }

    void Start()
    {
        EcoleDirecte.SetActive(true);
        for (int i = 0; i < EcoleDirecte.transform.childCount; i++) EcoleDirecte.transform.GetChild(i).gameObject.SetActive(false);
        try
        {
            var account = FileFormat.XML.Utils.XMLtoClass<Account>(Security.Encrypting.Decrypt(PlayerPrefs.GetString("Connection"), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG"));
            if (account.type == Account.Type.EcoleDirecte) StartCoroutine(SyncED(account, false));
        }
        catch { EcoleDirecte.transform.Find("Connect").gameObject.SetActive(true); }
    }

    public void ConnectToED()
    {
        var connect = EcoleDirecte.transform.Find("Connect").Find("Content");
        var account = new Account()
        {
            type = Account.Type.EcoleDirecte,
            id = connect.Find("ID").GetComponent<InputField>().text,
            password = connect.Find("MDP").GetComponent<InputField>().text
        };

        StartCoroutine(SyncED(account, connect.Find("RememberMe").GetComponent<Toggle>().isOn));
    }
    IEnumerator SyncED(Account account, bool save)
    {
        Log("Establishing the connection with EcoleDirecte");

        //Get Token
        var accountRequest = UnityEngine.Networking.UnityWebRequest.Post("https://api.ecoledirecte.com/v3/login.awp", $"data={{\"identifiant\": \"{account.id}\", \"motdepasse\": \"{account.password}\"}}");
        yield return accountRequest.SendWebRequest();
        var accountInfos = new FileFormat.JSON(accountRequest.downloadHandler.text);
        if (accountInfos.Value<int>("code") != 200)
        {
            EcoleDirecte.transform.Find("Connect").Find("Content").Find("Error").GetComponent<Text>().text = accountInfos.Value<string>("message");
            Loading.SetActive(false);
            yield break;
        }

        if (account.child == null)
        {
            //Get eleves
            var eleves = accountInfos.jToken.SelectToken("data.accounts").FirstOrDefault().SelectToken("profile").Value<JArray>("eleves");
            var childs = EcoleDirecte.transform.Find("Childs");
            var btns = childs.Find("btns");
            for (int i = 1; i < btns.childCount; i++) Destroy(btns.GetChild(i).gameObject);
            foreach (var eleve in eleves)
            {
                var btn = Instantiate(btns.GetChild(0).gameObject, btns);
                btn.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(SyncChild(eleve)));
                btn.transform.GetChild(0).GetComponent<Text>().text = eleve.Value<string>("prenom") + "\n" + eleve.Value<string>("nom");

                //Get picture
                var profileRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture("https:" + eleve.Value<string>("photo"));
                profileRequest.SetRequestHeader("referer", $"https://www.ecoledirecte.com/Eleves/{eleve.Value<string>("id")}/Notes");
                yield return profileRequest.SendWebRequest();
                if (!profileRequest.isHttpError)
                {
                    var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(profileRequest);
                    btn.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                else { Logging.Log("Error getting profile picture, server returned " + profileRequest.error + "\n" + profileRequest.url, LogType.Warning); }


                btn.SetActive(true);
            }

            EcoleDirecte.transform.Find("Connect").gameObject.SetActive(false);
            childs.gameObject.SetActive(true);
            Loading.SetActive(false);
        }
        else
        {
            var eleve = accountInfos.jToken.SelectToken("data.accounts").FirstOrDefault().SelectToken("profile").Value<JArray>("eleves").FirstOrDefault(e => e.Value<string>("id") == account.child);
            StartCoroutine(SyncChild(eleve));
        }

        IEnumerator SyncChild(JToken eleve)
        {
            Logging.Log(eleve.Value<string>("prenom") + " has been selected");
            account.child = eleve.Value<string>("id");
            if (save) PlayerPrefs.SetString("Connection", Security.Encrypting.Encrypt(FileFormat.XML.Utils.ClassToXML(account), "W#F4iwr@tr~_6yRpnn8W1m~G6eQWi3IDTnf(i5x7bcRmsa~pyG"));

            //Get Marks
            Log("Getting marks");
            var markRequest = UnityEngine.Networking.UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/eleves/{eleve.Value<string>("id")}/notes.awp?verbe=get&", $"data={{\"token\": \"{accountInfos.Value<string>("token")}\"}}");
            yield return markRequest.SendWebRequest();
            var marks = new FileFormat.JSON(markRequest.downloadHandler.text).jToken.SelectToken("data.notes").Values<JObject>().Select(obj => new Note()
            {
                nom = obj.Value<string>("devoir"),
                codePeriode = obj.Value<string>("codePeriode"),
                codeMatiere = obj.Value<string>("codeMatiere"),
                libelleMatiere = obj.Value<string>("libelleMatiere"),
                codeSousMatiere = obj.Value<string>("codeSousMatiere"),
                typeDevoir = obj.Value<string>("typeDevoir"),
                enLettre = obj.Value<bool>("enLettre"),
                coef = float.TryParse(obj.Value<string>("coef"), out var coef) ? coef : 1,
                noteSur = obj.Value<float>("noteSur"),
                valeur = float.TryParse(obj.Value<string>("valeur"), out var value) ? value : (float?)null,
                nonSignificatif = obj.Value<bool>("nonSignificatif"),
                date = obj.Value<System.DateTime>("date"),
                dateSaisie = obj.Value<System.DateTime>("dateSaisie"),
                valeurisee = obj.Value<bool>("valeurisee"),
                moyenneClasse = float.TryParse(obj.Value<string>("moyenneClasse"), out var m) ? m : (float?)null,

                //WIP
                competences = obj.Value<JArray>("elementsProgramme").Select(c => new Competence()
                {
                    nom = c.Value<string>("descriptif"),
                    id = uint.TryParse(c.Value<string>("idElemProg"), out var idComp) ? idComp : (uint?)null,
                    valeur = c.Value<string>("valeur"),
                    cdt = c.Value<bool>("cdt"),
                    idCat = c.Value<uint>("idCompetence"),
                    libelleCat = c.Value<string>("libelleCompetence")
                }).ToArray(),
            }).ToArray();

            Loading.SetActive(false);
            Debug.Log(FileFormat.XML.Utils.ClassToXML(marks, false));
        }
    }

    void Log(string txt)
    {
        Logging.Log(txt);
        Loading.transform.GetChild(1).GetComponent<Text>().text = txt;
        Loading.SetActive(true);
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