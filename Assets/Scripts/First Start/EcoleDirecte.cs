using Marks;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Integrations
{
    public class EcoleDirecte : Provider, Auth
    {
        public string Name => "EcoleDirecte";
        public bool NeedAuth => true;

        static string token;
        static string childID;
        public IEnumerator Connect(Account account, System.Action<Account> onComplete, System.Action<string> onError)
        {
            Manager.UpdateLoadingStatus("Establishing the connection with EcoleDirecte");

            //Get Token
            var accountRequest = UnityEngine.Networking.UnityWebRequest.Post("https://api.ecoledirecte.com/v3/login.awp", $"data={{\"identifiant\": \"{account.id}\", \"motdepasse\": \"{account.password}\"}}");
            yield return accountRequest.SendWebRequest();
            var accountInfos = new FileFormat.JSON(accountRequest.downloadHandler.text);
            if (accountInfos.Value<int>("code") != 200)
            {
                onError?.Invoke(accountInfos.Value<string>("message"));
                Manager.HideLoadingPanel();
                yield break;
            }
            token = accountInfos.Value<string>("token");

            var Account = accountInfos.jToken.SelectToken("data.accounts").FirstOrDefault();
            if (Account.Value<string>("typeCompte") == "E")
            {
                account.child = childID = Account.Value<string>("id");
                onComplete.Invoke(account);
            }
            else if (Account.Value<string>("typeCompte") == "2")
            {
                if (account.child == null)
                {
                    //Get eleves
                    var eleves = Account.SelectToken("profile").Value<JArray>("eleves");
                    var childs = new List<(System.Action, string, Sprite)>();
                    foreach (var eleve in eleves)
                    {
                        System.Action action = () =>
                        {
                            Logging.Log(eleve.Value<string>("prenom") + " has been selected");
                            account.child = childID = eleve.Value<string>("id");
                            onComplete.Invoke(account);
                        };
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
                    Manager.HideLoadingPanel();
                    FirstStart.SelectChilds(childs);
                }
                else
                {
                    var eleve = accountInfos.jToken.SelectToken("data.accounts").FirstOrDefault().SelectToken("profile").Value<JArray>("eleves").FirstOrDefault(e => e.Value<string>("id") == account.child);
                    Logging.Log(eleve.Value<string>("prenom") + " has been selected");
                    childID = eleve.Value<string>("id");
                    onComplete.Invoke(account);
                }
            }
        }

        public IEnumerator GetMarks(System.Action<List<Period>, List<Subject>, List<Mark>> onComplete)
        {
            Manager.UpdateLoadingStatus("Getting marks");
            var markRequest = UnityEngine.Networking.UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/eleves/{childID}/notes.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
            yield return markRequest.SendWebRequest();
            var markResult = new FileFormat.JSON(markRequest.downloadHandler.text);
            if (markResult.Value<int>("code") != 200)
            {
                Logging.Log("Error getting marks, server returned \"" + markResult.Value<string>("message") + "\"", LogType.Error);
                yield break;
            }

            var periods = markResult.jToken.SelectToken("data.periodes")?.Values<JObject>().Where(obj => !obj.Value<bool>("annuel")).Select(obj => new Period()
            {
                id = obj.Value<string>("idPeriode"),
                name = obj.Value<string>("periode"),
                start = obj.Value<System.DateTime>("dateDebut"),
                end = obj.Value<System.DateTime>("dateFin")
            }).ToList();

            var subjects = markResult.jToken.SelectToken("data.periodes[0].ensembleMatieres.disciplines")
                .Where(obj => !obj.SelectToken("groupeMatiere").Value<bool>())
                .Select(obj => new Subject()
                {
                    id = obj.SelectToken("codeMatiere").Value<string>(),
                    name = obj.SelectToken("discipline").Value<string>(),
                    coef = float.TryParse(obj.SelectToken("coef").Value<string>().Replace(",", "."), out var coef) ? coef : 1,
                    teachers = obj.SelectToken("professeurs").Select(o => o.SelectToken("nom").Value<string>()).ToArray()
                }).ToList();

            var marks = markResult.jToken.SelectToken("data.notes")?.Values<JObject>().Select(obj => new Mark()
            {
                //Date
                period = periods.FirstOrDefault(p => p.id == obj.Value<string>("codePeriode")),
                date = obj.Value<System.DateTime>("date"),
                dateAdded = obj.Value<System.DateTime>("dateSaisie"),

                //Infos
                subject = subjects.FirstOrDefault(s => s.id == obj.Value<string>("codeMatiere")),
                name = obj.Value<string>("devoir"),
                coef = float.TryParse(obj.Value<string>("coef").Replace(",", "."), out var coef) ? coef : 1,
                mark = float.TryParse(obj.Value<string>("valeur").Replace(",", "."), out var value) ? value : (float?)null,
                markOutOf = float.Parse(obj.Value<string>("noteSur").Replace(",", ".")),
                skills = obj.Value<JArray>("elementsProgramme").Select(c => new Skill()
                {
                    id = uint.TryParse(c.Value<string>("idElemProg"), out var idComp) ? idComp : (uint?)null,
                    name = c.Value<string>("descriptif"),
                    value = c.Value<string>("valeur"),
                    categoryID = c.Value<uint>("idCompetence"),
                    categoryName = c.Value<string>("libelleCompetence")
                }).ToArray(),
                classAverage = float.TryParse(obj.Value<string>("moyenneClasse").Replace(",", "."), out var m) ? m : (float?)null,
                notSignificant = obj.Value<bool>("nonSignificatif")
            }).ToList();

            onComplete.Invoke(periods, subjects, marks);

            Manager.HideLoadingPanel();
        }
    }
}
