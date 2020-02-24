using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Integrations
{
    public class EcoleDirecte : ModelClass, Model
    {
        public string Name => "EcoleDirecte";
        public bool NeedAuth => true;

        public IEnumerator Connect(Account account, bool save)
        {
            Log("Establishing the connection with EcoleDirecte");

            //Get Token
            var accountRequest = UnityEngine.Networking.UnityWebRequest.Post("https://api.ecoledirecte.com/v3/login.awp", $"data={{\"identifiant\": \"{account.id}\", \"motdepasse\": \"{account.password}\"}}");
            yield return accountRequest.SendWebRequest();
            var accountInfos = new FileFormat.JSON(accountRequest.downloadHandler.text);
            if (accountInfos.Value<int>("code") != 200)
            {
                OnError?.Invoke(accountInfos.Value<string>("message"));
                Loading.SetActive(false);
                yield break;
            }

            var Account = accountInfos.jToken.SelectToken("data.accounts").FirstOrDefault();
            if (Account.Value<string>("typeCompte") == "E") UnityThread.executeCoroutine(SyncChild(Account));
            else if (Account.Value<string>("typeCompte") == "2")
            {
                if (account.child == null)
                {
                    //Get eleves
                    var eleves = Account.SelectToken("profile").Value<JArray>("eleves");
                    var childs = new List<(System.Action, string, Sprite)>();
                    foreach (var eleve in eleves)
                    {
                        System.Action action = () => UnityThread.executeCoroutine(SyncChild(eleve));
                        var name = eleve.Value<string>("prenom") + "\n" + eleve.Value<string>("nom");
                        Sprite picture = null;

                        //Get picture
                        var profileRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture("https:" + eleve.Value<string>("photo"));
                        profileRequest.SetRequestHeader("referer", $"https://www.ecoledirecte.com/Eleves/{eleve.Value<string>("id")}/Notes");
                        yield return profileRequest.SendWebRequest();
                        if (!profileRequest.isHttpError)
                        {
                            var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(profileRequest);
                            picture = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                        else { Logging.Log("Error getting profile picture, server returned " + profileRequest.error + "\n" + profileRequest.url, LogType.Warning); }

                        childs.Add((action, name, picture));
                    }
                    Loading.SetActive(false);
                    SelectChilds(childs);
                }
                else
                {
                    var eleve = accountInfos.jToken.SelectToken("data.accounts").FirstOrDefault().SelectToken("profile").Value<JArray>("eleves").FirstOrDefault(e => e.Value<string>("id") == account.child);
                    UnityThread.executeCoroutine(SyncChild(eleve));
                }
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
                var markR = new FileFormat.JSON(markRequest.downloadHandler.text);
                if (markR.Value<int>("code") != 200)
                {
                    Logging.Log("Error getting marks, server returned \"" + markR.Value<string>("message") + "\"", LogType.Error);
                    yield break;
                }
                OnComplete?.Invoke(markR.jToken.SelectToken("data.notes")?.Values<JObject>().Select(obj => new Note()
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
                }).ToArray());

                Loading.SetActive(false);
            }
        }
    }
}
