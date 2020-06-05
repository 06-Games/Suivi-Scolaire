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
    public class EcoleDirecte : Provider, Auth, Periods, Homeworks, Marks, Schedule, Messanging
    {
        public string Name => "EcoleDirecte";

        string token;
        public IEnumerator Connect(Account account, Action<Account, List<ChildAccount>> onComplete, Action<string> onError)
        {
            Manager.UpdateLoadingStatus("provider.connecting", "Establishing the connection with [0]", true, Name);

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
            var childs = new List<ChildAccount>();
            yield return GetChild(Account, Account.Value<string>("typeCompte") == "E" ? "eleves" : "familles", (c) => { childs.Add(c); account.username = c.name; });

            if (Account.Value<string>("typeCompte") != "E")
            {
                //Get eleves
                var eleves = Account.SelectToken("profile").Value<JArray>("eleves");
                foreach (var eleve in eleves) yield return GetChild(eleve, "eleves", (c) => childs.Add(c));
            }
            account.child = childs.FirstOrDefault(c => c.id == account.child?.id) ?? childs.FirstOrDefault();
            Manager.HideLoadingPanel();
            onComplete?.Invoke(account, childs);

            IEnumerator GetChild(JToken profile, string type, Action<ChildAccount> child)
            {
                //Get picture
                Sprite picture = null;
                var url = profile.Value<string>("photo");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var profileRequest = UnityWebRequestTexture.GetTexture("https:" + url);
                    profileRequest.SetRequestHeader("referer", $"https://www.ecoledirecte.com/Famille");
                    yield return profileRequest.SendWebRequest();
                    if (!profileRequest.isHttpError)
                    {
                        var tex = DownloadHandlerTexture.GetContent(profileRequest);
                        picture = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                    else { Logging.Log("Error getting profile picture, server returned " + profileRequest.error + "\n" + profileRequest.url, LogType.Warning); }
                }

                var moduleCores = new Dictionary<string, string>
                {
                    { "NOTES", "Marks" },
                    { "MESSAGERIE", "Messanging" },
                    { "EDT", "Schedule" },
                    { "CAHIER_DE_TEXTES", "Homeworks" }
                };
                child?.Invoke(new ChildAccount
                {
                    name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase($"{profile.Value<string>("prenom")} {profile.Value<string>("nom")}".ToLower()),
                    id = profile.Value<string>("id"),
                    modules = profile.SelectToken("modules")
                        .Where(m => m.Value<bool>("enable") && moduleCores.ContainsKey(m.Value<string>("code")))
                        .Select(m => moduleCores[m.Value<string>("code")]).Append("Periods").ToList(),
                    image = picture,
                    extraData = new Dictionary<string, string> { { "type", type } }
                });
            }
        }
        public IEnumerator GetMarks(Action<List<global::Marks.Period>, List<Subject>, List<Mark>> onComplete)
        {
            Manager.UpdateLoadingStatus("provider.marks", "Getting marks");
            var markRequest = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/eleves/{FirstStart.selectedAccount.child.id}/notes.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
            yield return markRequest.SendWebRequest();
            var result = new FileFormat.JSON(markRequest.downloadHandler.text);
            if (result.Value<int>("code") != 200)
            {
                if (result.Value<string>("message") == "Session expirée !")
                {
                    yield return Connect(FirstStart.selectedAccount, null, null);
                    yield return GetMarks(onComplete);
                }
                else Manager.FatalErrorDuringLoading(result.Value<string>("message"), "Error getting marks, server returned \"" + result.Value<string>("message") + "\"");
                yield break;
            }

            var periods = result.jToken.SelectToken("data.periodes")?.Values<JObject>().Where(obj => !obj.Value<bool>("annuel")).Select(obj => new global::Marks.Period()
            {
                id = obj.Value<string>("idPeriode"),
                name = obj.Value<string>("periode"),
                start = obj.Value<DateTime>("dateDebut"),
                end = obj.Value<DateTime>("dateFin")
            }).ToList();

            var subjects = result.jToken.SelectToken("data.periodes[0].ensembleMatieres.disciplines")
                .Where(obj => !obj.SelectToken("groupeMatiere").Value<bool>())
                .Select(obj => new Subject
                {
                    id = obj.SelectToken("codeMatiere").Value<string>(),
                    name = obj.SelectToken("discipline").Value<string>(),
                    coef = float.TryParse(obj.SelectToken("coef").Value<string>().Replace(",", "."), out var coef) ? coef : 1,
                    teachers = obj.SelectToken("professeurs").Select(o => o.SelectToken("nom").Value<string>()).ToArray()
                }).ToList();

            var marks = result.jToken.SelectToken("data.notes")?.Values<JObject>().Select(obj => new Mark
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
                skills = obj.Value<JArray>("elementsProgramme").Select(c => new Skill
                {
                    id = uint.TryParse(c.Value<string>("idElemProg"), out var idComp) ? idComp : (uint?)null,
                    name = c.Value<string>("descriptif"),
                    value = uint.TryParse(c.Value<string>("valeur"), out var v) ? v - 1 : (uint?)null,
                    categoryID = c.Value<uint>("idCompetence"),
                    categoryName = c.Value<string>("libelleCompetence")
                }).ToArray(),
                classAverage = float.TryParse(obj.Value<string>("moyenneClasse").Replace(",", "."), out var m) ? m : (float?)null,
                notSignificant = obj.Value<bool>("nonSignificatif")
            }).ToList();

            Manager.HideLoadingPanel();
            onComplete?.Invoke(periods, subjects, marks);
        }
        public IEnumerator GetHomeworks(global::Homeworks.Period period, Action<List<Homework>> onComplete)
        {
            Manager.UpdateLoadingStatus("provider.homeworks", "Getting homeworks");

            IEnumerable<string> dates = null;
            if (period.timeRange.End == DateTime.MaxValue)
            {
                var homeworksRequest = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/Eleves/{FirstStart.selectedAccount.child.id}/cahierdetexte.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
                yield return homeworksRequest.SendWebRequest();
                var result = new FileFormat.JSON(homeworksRequest.downloadHandler.text);
                if (result.Value<int>("code") != 200)
                {
                    if (result.Value<string>("message") == "Session expirée !")
                    {
                        yield return Connect(FirstStart.selectedAccount, null, null);
                        yield return GetHomeworks(period, onComplete);
                    }
                    else Manager.FatalErrorDuringLoading(result.Value<string>("message"), "Error getting homeworks, server returned \"" + result.Value<string>("message") + "\"");
                    yield break;
                }
                dates = result.jToken.SelectToken("data").Select(v => v.Path.Split('.').LastOrDefault()).Distinct();
            }
            else dates = period.timeRange.DayList().Select(d => d.ToString("yyyy-MM-dd"));

            var homeworks = new List<Homework>();
            foreach (var date in dates)
            {
                var request = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/Eleves/{FirstStart.selectedAccount.child.id}/cahierdetexte/{date}.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
                yield return request.SendWebRequest();
                var result = new FileFormat.JSON(request.downloadHandler.text);
                if (result.Value<int>("code") != 200)
                {
                    Logging.Log($"Error getting homeworks for {date}, server returned \"" + result.Value<string>("message") + "\"", LogType.Error);
                    continue;
                }

                homeworks.AddRange(result.jToken.SelectToken("data.matieres")?.Where(v => v.SelectToken("aFaire") != null).Select(v => new Homework
                {
                    subject = new Subject { id = v.Value<string>("codeMatiere"), name = v.Value<string>("matiere") },
                    forThe = DateTime.Parse(date),
                    addedThe = v.SelectToken("aFaire").Value<DateTime>("donneLe"),
                    addedBy = v.Value<string>("nomProf").Replace(" par ", ""),
                    content = ProviderExtension.RemoveEmptyLines(ProviderExtension.HtmlToRichText(FromBase64(v.SelectToken("aFaire").Value<string>("contenu")))),
                    done = v.SelectToken("aFaire").Value<bool>("effectue"),
                    exam = v.Value<bool>("interrogation"),
                    documents = v.SelectToken("aFaire.documents").Select(doc =>
                    {
                        WWWForm form = new WWWForm();
                        form.AddField("token", token);
                        form.AddField("leTypeDeFichier", doc.Value<string>("type"));
                        form.AddField("fichierId", doc.Value<string>("id"));
                        return new Request
                        {
                            docName = doc.Value<string>("libelle"),
                            url = "https://api.ecoledirecte.com/v3/telechargement.awp?verbe=get",
                            method = Request.Method.Post,
                            postData = form
                        };
                    })
                }));
            }

            Manager.HideLoadingPanel();
            onComplete?.Invoke(homeworks);
        }
        public IEnumerator<global::Homeworks.Period> DiaryPeriods()
        {
            DateTime start = DateTime.Now;
            yield return ToPeriod(start, DateTime.MaxValue);

            int delta = DayOfWeek.Monday - start.DayOfWeek;
            start = start.AddDays(delta > 0 ? delta - 7 : delta);
            while (true)
            {
                var end = start + new TimeSpan(6, 0, 0, 0);
                yield return ToPeriod(start, end);
                start += new TimeSpan(-7, 0, 0, 0);
            }

            global::Homeworks.Period ToPeriod(DateTime pStart, DateTime pEnd) => new global::Homeworks.Period
            {
                timeRange = new TimeRange(pStart, pEnd),
                id = new TimeRange(pStart, pEnd).ToString("yyyy-MM-dd") ?? "Upcomming",
                name = pEnd == DateTime.MaxValue ? LangueAPI.Get("homeworks.upcomming", "Upcomming") : LangueAPI.Get("homeworks.period", "from [0] to [1]", pStart.ToString("dd/MM"), pEnd.ToString("dd/MM"))
            };
        }
        public IEnumerator GetPeriods(Action<List<global::Periods.Period>> onComplete)
        {
            Manager.UpdateLoadingStatus("provider.holidays", "Getting holidays");
            var establishmentRequest = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/contactetablissement.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
            yield return establishmentRequest.SendWebRequest();
            var establishmentResult = new FileFormat.JSON(establishmentRequest.downloadHandler.text);
            if (establishmentResult.Value<int>("code") != 200)
            {
                if (establishmentResult.Value<string>("message") == "Session expirée !")
                {
                    yield return Connect(FirstStart.selectedAccount, null, null);
                    yield return GetPeriods(onComplete);
                }
                else Manager.FatalErrorDuringLoading(establishmentResult.Value<string>("message"), "Error getting establishment, server returned \"" + establishmentResult.Value<string>("message") + "\"");
                yield break;
            }
            var adress = FromBase64(establishmentResult.jToken.SelectToken("data[0]")?.Value<string>("adresse")).Replace("\r", "").Replace("\n", " ");
            Logging.Log("The address of the establishment is " + adress);

            var adressRequest = UnityWebRequest.Get($"https://api-adresse.data.gouv.fr/search/?q={adress}&limit=1");
            yield return adressRequest.SendWebRequest();
            var adressResult = new FileFormat.JSON(adressRequest.downloadHandler.text);
            var location = adressResult.jToken.SelectToken("features[0].properties");

            var gouvRequest = UnityWebRequest.Get($"https://data.education.gouv.fr/api/records/1.0/search/?dataset=fr-en-annuaire-education&q={location?.Value<string>("postcode")} {location?.Value<string>("city")}&rows=1");
            yield return gouvRequest.SendWebRequest();
            var gouvResult = new FileFormat.JSON(gouvRequest.downloadHandler.text);
            var academy = gouvResult.jToken.SelectToken("records[0].fields")?.Value<string>("libelle_academie");
            Logging.Log("It depends on the academy of " + academy);

            var holidaysRequest = UnityWebRequest.Get($"https://data.education.gouv.fr/api/records/1.0/search/?dataset=fr-en-calendrier-scolaire&q={academy}&sort=end_date");
            yield return holidaysRequest.SendWebRequest();
            var holidaysResult = new FileFormat.JSON(holidaysRequest.downloadHandler.text);
            DateTime lastPeriod = DateTime.MinValue;
            var holidays = holidaysResult.jToken.SelectToken("records").Reverse().SelectMany(v =>
            {
                var list = new List<global::Periods.Period>();
                var obj = v.SelectToken("fields");
                list.Add(new global::Periods.Period
                {
                    name = "Periode scolaire",
                    start = lastPeriod.AddSeconds(1),
                    end = obj.Value<DateTime>("start_date").AddSeconds(-1),
                    holiday = false
                });
                list.Add(new global::Periods.Period
                {
                    name = obj.Value<string>("description"),
                    start = obj.Value<DateTime>("start_date"),
                    end = obj.Value<DateTime>("end_date"),
                    holiday = true
                });
                lastPeriod = list.Last().end;
                return list;
            }).ToList();

            onComplete?.Invoke(holidays);
            Manager.HideLoadingPanel();
        }
        public IEnumerator GetSchedule(TimeRange period, Action<List<global::Schedule.Event>> onComplete)
        {
            Manager.UpdateLoadingStatus("provider.schedule", "Getting schedule");

            var request = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/E/{FirstStart.selectedAccount.child.id}/emploidutemps.awp?verbe=get&", $"data={{\"token\": \"{token}\", \"dateDebut\": \"{period.Start.ToString("yyyy-MM-dd")}\", \"dateFin\": \"{period.End.ToString("yyyy-MM-dd")}\", \"avecTrous\": false }}");
            yield return request.SendWebRequest();
            var result = new FileFormat.JSON(request.downloadHandler.text);
            if (result.Value<int>("code") != 200)
            {
                if (result.Value<string>("message") == "Session expirée !")
                {
                    yield return Connect(FirstStart.selectedAccount, null, null);
                    yield return GetSchedule(period, onComplete);
                }
                else Manager.FatalErrorDuringLoading(result.Value<string>("message"), $"Error getting schedule for {period}, server returned \"" + result.Value<string>("message") + "\"");
                yield break;
            }

            var events = result.jToken.SelectToken("data")?.Where(v => !string.IsNullOrWhiteSpace(v.Value<string>("codeMatiere")))?.Select(v => new global::Schedule.Event
            {
                subject = new Subject { id = v.Value<string>("codeMatiere"), name = v.Value<string>("matiere") },
                start = v.Value<DateTime>("start_date"),
                end = v.Value<DateTime>("end_date"),
                room = v.Value<string>("salle"),
                canceled = v.Value<bool>("isAnnule")
            }).ToList();

            Manager.HideLoadingPanel();
            onComplete?.Invoke(events ?? new List<global::Schedule.Event>());
        }
        public IEnumerator GetMessages(Action<List<global::Messanging.Message>> onComplete)
        {
            Manager.UpdateLoadingStatus("provider.messages", "Getting messages");
            var request = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/{FirstStart.selectedAccount.child.extraData["type"]}/{FirstStart.selectedAccount.child.id}/messages.awp?verbe=getall&idClasseur=0", $"data={{\"token\": \"{token}\"}}");
            yield return request.SendWebRequest();
            var result = new FileFormat.JSON(request.downloadHandler.text);
            if (result.Value<int>("code") != 200)
            {
                if (result.Value<string>("message") == "Session expirée !")
                {
                    yield return Connect(FirstStart.selectedAccount, null, null);
                    yield return GetMessages(onComplete);
                }
                else Manager.FatalErrorDuringLoading(result.Value<string>("message"), "Error getting messages, server returned \"" + result.Value<string>("message") + "\"");
                yield break;
            }

            var msg = result.jToken.SelectTokens("data.messages.*").SelectMany(t =>
            {
                return t.Select(m =>
                {
                    var type = (m.Value<string>("mtype") == "received") ? "from" : "to";
                    return new global::Messanging.Message
                    {
                        id = m.Value<uint>("id"),
                        read = m.Value<bool>("read"),
                        date = m.Value<DateTime>("date"),
                        subject = m.Value<string>("subject"),
                        correspondents = (type == "from" ? Enumerable.Repeat(m[type], 1) : m[type]).Select(c => c.Value<string>("name")).ToList(),
                        type = m.Value<string>("mtype") == "send" ? global::Messanging.Message.Type.sent : global::Messanging.Message.Type.received
                    };
                });
            }).ToList();

            Manager.HideLoadingPanel();
            onComplete?.Invoke(msg);
        }
        public IEnumerator LoadExtraMessageData(global::Messanging.Message message, Action<global::Messanging.Message> onComplete)
        {
            Manager.UpdateLoadingStatus("provider.messages.content", "Getting message content");
            var request = UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/{FirstStart.selectedAccount.child.extraData["type"]}/{FirstStart.selectedAccount.child.id}/messages/{message.id}.awp?verbe=get&mode={(message.type == global::Messanging.Message.Type.received ? "destinataire" : "expediteur")}", $"data={{\"token\": \"{token}\"}}");
            yield return request.SendWebRequest();
            var result = new FileFormat.JSON(request.downloadHandler.text);
            if (result.Value<int>("code") != 200)
            {
                if (result.Value<string>("message") == "Session expirée !")
                {
                    yield return Connect(FirstStart.selectedAccount, null, null);
                    yield return LoadExtraMessageData(message, onComplete);
                }
                else Manager.FatalErrorDuringLoading(result.Value<string>("message"), $"Error getting extra message data, server returned \"{result.Value<string>("message")}\"\nRequest URL: {request.url}\n\nFull server response: {result}");
                yield break;
            }

            var data = result.jToken.SelectToken("data");
            message.extra = new global::Messanging.Message.Extra
            {
                content = ProviderExtension.RemoveEmptyLines(ProviderExtension.HtmlToRichText(FromBase64(data.Value<string>("content")))),
                documents = data.SelectToken("files").Select(doc =>
                {
                    WWWForm form = new WWWForm();
                    form.AddField("token", token);
                    form.AddField("leTypeDeFichier", doc.Value<string>("type"));
                    form.AddField("fichierId", doc.Value<string>("id"));
                    return new Request
                    {
                        docName = doc.Value<string>("libelle"),
                        url = "https://api.ecoledirecte.com/v3/telechargement.awp?verbe=get",
                        method = Request.Method.Post,
                        postData = form
                    };
                })
            };

            Manager.HideLoadingPanel();
            onComplete.Invoke(message);
        }

        string FromBase64(string b64) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
    }
}
