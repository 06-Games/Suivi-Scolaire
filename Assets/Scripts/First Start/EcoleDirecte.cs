using Home;
using Homeworks;
using Marks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Integrations
{
    public class EcoleDirecte : Provider, Auth, Home, Homeworks, Marks, Schedule
    {
        public string Name => "EcoleDirecte";

        static string token;
        static string childID;

        public IEnumerator Connect(Account account, Action<Account> onComplete, Action<string> onError)
        {
            Manager.UpdateLoadingStatus("ecoleDirecte.connecting", "Establishing the connection with EcoleDirecte");

            //Get Token
            var accountRequest = UnityWebRequest.Post("https://api.ecoledirecte.com/v3/login.awp", $"data={{\"identifiant\": \"{account.id}\", \"motdepasse\": \"{account.password}\"}}");
            yield return accountRequest.SendWebRequest();
            var accountInfos = new FileFormat.JSON(accountRequest.downloadHandler.text);
            if (accountRequest.isNetworkError || accountInfos.Value<int>("code") != 200)
            {
                onError?.Invoke(accountRequest.isNetworkError ? accountRequest.error : accountInfos.Value<string>("message"));
                Manager.HideLoadingPanel();
                yield break;
            }
            token = accountInfos.Value<string>("token");

            var Account = accountInfos.jToken.SelectToken("data.accounts").FirstOrDefault();
            account.username = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase($"{Account.Value<string>("prenom")} {Account.Value<string>("nom")}".ToLower());
            if (Account.Value<string>("typeCompte") == "E")
            {
                account.child = childID = Account.Value<string>("id");
                Manager.HideLoadingPanel();
                onComplete?.Invoke(account);
            }
            else
            {
                if (account.child == null)
                {
                    //Get eleves
                    var eleves = Account.SelectToken("profile").Value<JArray>("eleves");
                    var childs = new List<(Action, string, Sprite)>();
                    foreach (var eleve in eleves)
                    {
                        Action action = () =>
                        {
                            Logging.Log(eleve.Value<string>("prenom") + " has been selected");
                            account.child = childID = eleve.Value<string>("id");
                            Manager.HideLoadingPanel();
                            onComplete?.Invoke(account);
                        };
                        var name = eleve.Value<string>("prenom") + "\n" + eleve.Value<string>("nom");
                        Sprite picture = null;

                        //Get picture
                        var profileRequest = UnityWebRequestTexture.GetTexture("https:" + eleve.Value<string>("photo"));
                        profileRequest.SetRequestHeader("referer", $"https://www.ecoledirecte.com/Eleves/{eleve.Value<string>("id")}/Notes");
                        yield return profileRequest.SendWebRequest();
                        if (!profileRequest.isHttpError)
                        {
                            var tex = DownloadHandlerTexture.GetContent(profileRequest);
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
                    Manager.HideLoadingPanel();
                    onComplete?.Invoke(account);
                }
            }
        }
        public IEnumerator GetMarks(Action<List<Period>, List<Subject>, List<Mark>> onComplete)
        {
            Manager.UpdateLoadingStatus("ecoleDirecte.marks", "Getting marks");
            var markRequest = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/eleves/{childID}/notes.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
            yield return markRequest.SendWebRequest();
            var result = new FileFormat.JSON(markRequest.downloadHandler.text);
            if (result.Value<int>("code") != 200)
            {
                if (result.Value<string>("message") == "Session expirée !")
                {
                    yield return Connect(Manager.instance.FirstStart.selectedAccount, null, null);
                    yield return GetMarks(onComplete);
                }
                else Manager.FatalErrorDuringLoading(result.Value<string>("message"), "Error getting marks, server returned \"" + result.Value<string>("message") + "\"");
                yield break;
            }

            var periods = result.jToken.SelectToken("data.periodes")?.Values<JObject>().Where(obj => !obj.Value<bool>("annuel")).Select(obj => new Period()
            {
                id = obj.Value<string>("idPeriode"),
                name = obj.Value<string>("periode"),
                start = obj.Value<DateTime>("dateDebut"),
                end = obj.Value<DateTime>("dateFin")
            }).ToList();

            var subjects = result.jToken.SelectToken("data.periodes[0].ensembleMatieres.disciplines")
                .Where(obj => !obj.SelectToken("groupeMatiere").Value<bool>())
                .Select(obj => new Subject()
                {
                    id = obj.SelectToken("codeMatiere").Value<string>(),
                    name = obj.SelectToken("discipline").Value<string>(),
                    coef = float.TryParse(obj.SelectToken("coef").Value<string>().Replace(",", "."), out var coef) ? coef : 1,
                    teachers = obj.SelectToken("professeurs").Select(o => o.SelectToken("nom").Value<string>()).ToArray()
                }).ToList();

            var marks = result.jToken.SelectToken("data.notes")?.Values<JObject>().Select(obj => new Mark()
            {
                //Date
                period = periods.FirstOrDefault(p => p.id == obj.Value<string>("codePeriode")),
                date = obj.Value<DateTime>("date"),
                dateAdded = obj.Value<DateTime>("dateSaisie"),

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

            Manager.HideLoadingPanel();
            onComplete?.Invoke(periods, subjects, marks);
        }
        public IEnumerator GetHomeworks(TimeRange period, Action<List<Homework>> onComplete)
        {
            Manager.UpdateLoadingStatus("ecoleDirecte.homeworks", "Getting homeworks");

            IEnumerable<string> dates = null;
            if (period == null)
            {
                var homeworksRequest = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/Eleves/{childID}/cahierdetexte.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
                yield return homeworksRequest.SendWebRequest();
                var result = new FileFormat.JSON(homeworksRequest.downloadHandler.text);
                if (result.Value<int>("code") != 200)
                {
                    if (result.Value<string>("message") == "Session expirée !")
                    {
                        yield return Connect(Manager.instance.FirstStart.selectedAccount, null, null);
                        yield return GetHomeworks(period, onComplete);
                    }
                    else Manager.FatalErrorDuringLoading(result.Value<string>("message"), "Error getting homeworks, server returned \"" + result.Value<string>("message") + "\"");
                    yield break;
                }
                dates = result.jToken.SelectToken("data").Select(v => v.Path.Split('.').LastOrDefault()).Distinct();
            }
            else dates = period.DayList().Select(d => d.ToString("yyyy-MM-dd"));

            var homeworks = new List<Homework>();
            foreach (var date in dates)
            {
                var request = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/Eleves/{childID}/cahierdetexte/{date}.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
                yield return request.SendWebRequest();
                var result = new FileFormat.JSON(request.downloadHandler.text);
                if (result.Value<int>("code") != 200)
                {
                    Logging.Log($"Error getting homeworks for {date}, server returned \"" + result.Value<string>("message") + "\"", LogType.Error);
                    continue;
                }

                homeworks.AddRange(result.jToken.SelectToken("data.matieres")?.Where(v => v.SelectToken("aFaire") != null).Select(v => new Homework()
                {
                    subject = new Subject() { id = v.Value<string>("codeMatiere"), name = v.Value<string>("matiere") },
                    forThe = DateTime.Parse(date),
                    addedThe = v.SelectToken("aFaire").Value<DateTime>("donneLe"),
                    addedBy = v.Value<string>("nomProf").Replace(" par ", ""),
                    content = RemoveEmptyLines(HtmlToRichText(FromBase64(v.SelectToken("aFaire").Value<string>("contenu")))),
                    done = v.SelectToken("aFaire").Value<bool>("effectue"),
                    exam = v.Value<bool>("interrogation"),
                    documents = v.SelectToken("aFaire.documents").Select(doc =>
                    {
                        WWWForm form = new WWWForm();
                        form.AddField("token", token);
                        form.AddField("leTypeDeFichier", doc.Value<string>("type"));
                        form.AddField("fichierId", doc.Value<string>("id"));
                        return (doc.Value<string>("libelle"), "https://api.ecoledirecte.com/v3/telechargement.awp?verbe=get", form);
                    })
                }));
            }

            Manager.HideLoadingPanel();
            onComplete?.Invoke(homeworks);
        }
        public IEnumerator GetHolidays(Action<List<Holiday>> onComplete)
        {
            Manager.UpdateLoadingStatus("ecoleDirecte.holidays", "Getting holidays");
            var establishmentRequest = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/contactetablissement.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
            yield return establishmentRequest.SendWebRequest();
            var establishmentResult = new FileFormat.JSON(establishmentRequest.downloadHandler.text);
            if (establishmentResult.Value<int>("code") != 200)
            {
                if (establishmentResult.Value<string>("message") == "Session expirée !")
                {
                    yield return Connect(Manager.instance.FirstStart.selectedAccount, null, null);
                    yield return GetHolidays(onComplete);
                }
                else Manager.FatalErrorDuringLoading(establishmentResult.Value<string>("message"), "Error getting establishment, server returned \"" + establishmentResult.Value<string>("message") + "\"");
                yield break;
            }
            var adress = FromBase64(establishmentResult.jToken.SelectToken("data[0]")?.Value<string>("adresse")).Replace("\r", "").Replace("\n", " ");
            Logging.Log("The address of the establishment is " + adress);

            var gouvRequest = UnityWebRequest.Get($"https://data.education.gouv.fr/api/records/1.0/search/?dataset=fr-en-annuaire-education&q={adress}&rows=1");
            yield return gouvRequest.SendWebRequest();
            var gouvResult = new FileFormat.JSON(gouvRequest.downloadHandler.text);
            var academy = gouvResult.jToken.SelectToken("records[0].fields")?.Value<string>("libelle_academie");
            Logging.Log("It depends on the academy of " + academy);

            var holidaysRequest = UnityWebRequest.Get($"https://data.education.gouv.fr/api/records/1.0/search/?dataset=fr-en-calendrier-scolaire&q={academy}&sort=end_date");
            yield return holidaysRequest.SendWebRequest();
            var holidaysResult = new FileFormat.JSON(holidaysRequest.downloadHandler.text);
            var holidays = holidaysResult.jToken.SelectToken("records").Select(v =>
            {
                var obj = v.SelectToken("fields");
                return new Holiday()
                {
                    name = obj.Value<string>("description"),
                    start = obj.Value<DateTime>("start_date"),
                    end = obj.Value<DateTime>("end_date")
                };
            }).ToList();

            onComplete?.Invoke(holidays);
            Manager.HideLoadingPanel();
        }
        public IEnumerator GetSchedule(TimeRange period, Action<List<global::Schedule.Event>> onComplete)
        {
            Manager.UpdateLoadingStatus("ecoleDirecte.schedule", "Getting schedule");

            var request = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/E/{childID}/emploidutemps.awp?verbe=get&", $"data={{\"token\": \"{token}\", \"dateDebut\": \"{period.Start.ToString("yyyy-MM-dd")}\", \"dateFin\": \"{period.End.ToString("yyyy-MM-dd")}\", \"avecTrous\": false }}");
            yield return request.SendWebRequest();
            var result = new FileFormat.JSON(request.downloadHandler.text);
            if (result.Value<int>("code") != 200)
            {
                if (result.Value<string>("message") == "Session expirée !")
                {
                    yield return Connect(Manager.instance.FirstStart.selectedAccount, null, null);
                    yield return GetSchedule(period, onComplete);
                }
                else Manager.FatalErrorDuringLoading(result.Value<string>("message"), $"Error getting schedule for {period}, server returned \"" + result.Value<string>("message") + "\"");
            }

            var events = result.jToken.SelectToken("data").Where(v => !string.IsNullOrWhiteSpace(v.Value<string>("codeMatiere"))).Select(v => new global::Schedule.Event()
            {
                subject = new Subject() { id = v.Value<string>("codeMatiere"), name = v.Value<string>("matiere") },
                start = v.Value<DateTime>("start_date"),
                end = v.Value<DateTime>("end_date"),
                room = v.Value<string>("salle"),
                canceled = v.Value<bool>("isAnnule")
            }).ToList();

            Manager.HideLoadingPanel();
            onComplete?.Invoke(events);
        }

        string FromBase64(string b64) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        string HtmlToRichText(string html)
        {
            return System.Net.WebUtility.HtmlDecode(html)
                .Replace("<p>", "").Replace("</p>", "")
                .Replace("<a href=", "<link=").Replace("</a>", "</link>")
                .Replace("<ul>", "").Replace("</ul>", "")
                .Replace("<li>", "• ").Replace("</li>", "")
                .Replace("\t", "");
        }
        string RemoveEmptyLines(string lines) => System.Text.RegularExpressions.Regex.Replace(lines, @"^\s*$\n|\r", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline).TrimEnd();
    }
}
