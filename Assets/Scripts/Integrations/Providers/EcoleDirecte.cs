using Integrations.Data;
using Modules;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Integrations
{
    public class EcoleDirecte : Provider, Auth, Periods, Homeworks, Marks, Schedule, Messanging, Books, Documents
    {
        public string Name => "EcoleDirecte";

        string token;
        public IEnumerator Connect(Account account, Action<Data.Data> onComplete, Action<string> onError)
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
            var childs = new List<Child>();
            yield return GetChild(Account, Account.Value<string>("typeCompte") == "E" ? "eleves" : "familles", (c) => { childs.Add(c); account.username = c.name; });

            if (Account.Value<string>("typeCompte") != "E")
            {
                //Get eleves
                var eleves = Account.SelectToken("profile").Value<JArray>("eleves");
                foreach (var eleve in eleves) yield return GetChild(eleve, "eleves", (c) => childs.Add(c));
            }
            Manager.HideLoadingPanel();
            onComplete?.Invoke(new Data.Data { Provider = "EcoleDirecte", Label = account.username, Children = childs.ToArray() });

            IEnumerator GetChild(JToken profile, string type, Action<Child> child)
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

                var moduleCores = new Dictionary<string, string[]>
                {
                    { "NOTES", new []{ "Marks" } },
                    { "MESSAGERIE", new []{ "Messanging" } },
                    { "EDT", new []{ "Schedule" } },
                    { "CAHIER_DE_TEXTES", new []{ "Homeworks", "Books" } },
                    { "CLOUD", new[]{ "Documents" } },
                    { "VIE_DE_LA_CLASSE", new[]{ "Documents" } },
                    { "DOCUMENTS_ELEVE", new[]{ "Documents" } },
                    { "DOCUMENTS", new[]{ "Documents" } }
                };
                child?.Invoke(new Child
                {
                    name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase($"{profile.Value<string>("prenom")} {profile.Value<string>("nom")}".ToLower()),
                    id = profile.Value<string>("id"),
                    modules = profile.SelectToken("modules")
                        .Where(m => m.Value<bool>("enable") && moduleCores.ContainsKey(m.Value<string>("code")))
                        .SelectMany(m => moduleCores[m.Value<string>("code")]).Append("Periods").ToList(),
                    sprite = picture,
                    extraData = new Dictionary<string, string> {
                        { "type", type },
                        { "edModules", Newtonsoft.Json.JsonConvert.SerializeObject(profile.SelectToken("modules").Where(m => m.Value<bool>("enable")).Select(m => m.Value<string>("code")).ToArray()) }
                    }
                });
            }
        }

        public IEnumerator GetMarks(Action onComplete)
        {
            FileFormat.JSON result = null;
            Func<UnityWebRequest> request = () => UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/eleves/{Accounts.selectedAccount.child}/notes.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
            yield return TryConnection(request, "marks", (o) => result = o);
            if (result == null) yield break;

            Manager.Child.Trimesters = result.jToken.SelectToken("data.periodes")?.Values<JObject>().Where(obj => !obj.Value<bool>("annuel")).Select(obj => new Trimester
            {
                id = obj.Value<string>("idPeriode"),
                name = obj.Value<string>("periode"),
                start = obj.Value<DateTime>("dateDebut"),
                end = obj.Value<DateTime>("dateFin")
            }).ToList();

            Manager.Child.Subjects = result.jToken.SelectToken("data.periodes[0].ensembleMatieres.disciplines")
                .Where(obj => !obj.SelectToken("groupeMatiere").Value<bool>())
                .Select(obj => new Subject
                {
                    id = obj.SelectToken("codeMatiere").Value<string>(),
                    name = obj.SelectToken("discipline").Value<string>(),
                    coef = float.TryParse(obj.SelectToken("coef").Value<string>().Replace(",", "."), out var coef) ? coef : 1,
                    teachers = obj.SelectToken("professeurs").Select(o => o.SelectToken("nom").Value<string>()).ToArray()
                }).ToList();

            Manager.Child.Marks = result.jToken.SelectToken("data.notes")?.Values<JObject>().Select(obj => new Mark
            {
                //Date
                trimesterID = obj.Value<string>("codePeriode"),
                date = obj.Value<DateTime>("date"),
                dateAdded = obj.Value<DateTime>("dateSaisie"),

                //Infos
                subjectID = obj.Value<string>("codeMatiere"),
                name = obj.Value<string>("devoir"),
                coef = float.TryParse(obj.Value<string>("coef").Replace(",", "."), out var coef) ? coef : 1,
                mark = float.TryParse(obj.Value<string>("valeur").Replace(",", "."), out var value) ? value : (float?)null,
                markOutOf = float.Parse(obj.Value<string>("noteSur").Replace(",", ".")),
                skills = obj.Value<JArray>("elementsProgramme").Select(c => new Mark.Skill
                {
                    id = uint.TryParse(c.Value<string>("idElemProg"), out var idComp) ? idComp : 0,
                    name = c.Value<string>("descriptif"),
                    value = uint.TryParse(c.Value<string>("valeur"), out var v) ? v - 1 : (uint?)null,
                    categoryID = c.Value<uint>("idCompetence"),
                    categoryName = c.Value<string>("libelleCompetence")
                }).ToArray(),
                classAverage = float.TryParse(obj.Value<string>("moyenneClasse").Replace(",", "."), out var m) ? m : -1,
                notSignificant = obj.Value<bool>("nonSignificatif")
            }).ToList();

            Manager.HideLoadingPanel();
            onComplete?.Invoke();
        }

        public IEnumerator GetHomeworks(Homework.Period period, Action onComplete)
        {

            IEnumerable<string> dates = null;
            if (period.timeRange.End == DateTime.MaxValue)
            {
                FileFormat.JSON result = null;
                Func<UnityWebRequest> homeworksRequest = () => UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/Eleves/{Accounts.selectedAccount.child}/cahierdetexte.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
                yield return TryConnection(homeworksRequest, "homeworks", (o) => result = o);
                if (result == null) yield break;
                dates = result.jToken.SelectToken("data").Select(v => v.Path.Split('.').LastOrDefault()).Distinct();
            }
            else dates = period.timeRange.DayList().Select(d => d.ToString("yyyy-MM-dd"));

            var homeworks = Manager.Child.Homeworks ?? new List<Homework>();
            foreach (var date in dates)
            {
                FileFormat.JSON result = null;
                Func<UnityWebRequest> request = () => UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/Eleves/{Accounts.selectedAccount.child}/cahierdetexte/{date}.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
                yield return TryConnection(request, "homeworks", (o) => result = o, false);
                if (result == null) yield break;
                if (result.Value<int>("code") != 200)
                {
                    Logging.Log($"Error getting homeworks for {date}, server returned \"" + result.Value<string>("message") + "\"", LogType.Error);
                    continue;
                }
                DateTime.TryParse(date, out var dateTime);
                homeworks.RemoveAll(s => s.forThe == dateTime.Date);

                homeworks.AddRange(result.jToken.SelectToken("data.matieres")?.Where(v => v.SelectToken("aFaire") != null).Select(v => new Homework
                {
                    subjectID = v.Value<string>("codeMatiere"),
                    periodID = period.id,
                    forThe = dateTime,
                    addedThe = v.SelectToken("aFaire").Value<DateTime>("donneLe"),
                    addedBy = v.Value<string>("nomProf").Replace(" par ", ""),
                    content = ProviderExtension.RemoveEmptyLines(ProviderExtension.HtmlToRichText(FromBase64(v.SelectToken("aFaire").Value<string>("contenu")))),
                    done = v.SelectToken("aFaire").Value<bool>("effectue"),
                    exam = v.Value<bool>("interrogation"),
                    documents = v.SelectToken("aFaire.documents").Select(doc =>
                    {
                        return new Document
                        {
                            name = doc.Value<string>("libelle"),
                            id = doc.Value<string>("id"),
                            type = doc.Value<string>("type")
                        };
                    }).ToList()
                }));
            }
            Manager.Child.Homeworks = homeworks;

            Manager.HideLoadingPanel();
            onComplete?.Invoke();
        }
        public IEnumerator OpenHomeworkAttachment(Document doc) => OpenDocument(doc);

        public IEnumerator<Homework.Period> DiaryPeriods()
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

            Homework.Period ToPeriod(DateTime pStart, DateTime pEnd) => new Homework.Period
            {
                timeRange = new TimeRange(pStart, pEnd),
                id = new TimeRange(pStart, pEnd).ToString("yyyy-MM-dd") ?? "Upcomming",
                name = pEnd == DateTime.MaxValue ? LangueAPI.Get("homeworks.upcomming", "Upcomming") : LangueAPI.Get("homeworks.period", "from [0] to [1]", pStart.ToString("dd/MM"), pEnd.ToString("dd/MM"))
            };
        }
        public IEnumerator GetPeriods(Action onComplete)
        {
            FileFormat.JSON establishmentResult = null;
            Func<UnityWebRequest> establishmentRequest = () => UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/contactetablissement.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
            yield return TryConnection(establishmentRequest, "holidays", (o) => establishmentResult = o);
            if (establishmentResult == null) yield break;
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
            Manager.Child.Periods = holidaysResult.jToken.SelectToken("records").Reverse().SelectMany(v =>
            {
                var list = new List<Period>();
                var obj = v.SelectToken("fields");
                list.Add(new Period
                {
                    name = "Periode scolaire",
                    start = lastPeriod.AddSeconds(1),
                    end = obj.Value<DateTime>("start_date").AddSeconds(-1),
                    holiday = false
                });
                list.Add(new Period
                {
                    name = obj.Value<string>("description"),
                    start = obj.Value<DateTime>("start_date"),
                    end = obj.Value<DateTime>("end_date"),
                    holiday = true
                });
                lastPeriod = list.Last().end;
                return list;
            }).ToList();

            onComplete?.Invoke();
            Manager.HideLoadingPanel();
        }

        public IEnumerator GetSchedule(TimeRange period, Action<IEnumerable<ScheduledEvent>> onComplete)
        {
            FileFormat.JSON result = null;
            Func<UnityWebRequest> request = () => UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/E/{Accounts.selectedAccount.child}/emploidutemps.awp?verbe=get&", $"data={{\"token\": \"{token}\", \"dateDebut\": \"{period.Start.ToString("yyyy-MM-dd")}\", \"dateFin\": \"{period.End.ToString("yyyy-MM-dd")}\", \"avecTrous\": false }}");
            yield return TryConnection(request, "schedule", (o) => result = o);
            if (result == null) yield break;

            if (Manager.Child.Schedule == null) Manager.Child.Schedule = new List<ScheduledEvent>();
            var events = result.jToken.SelectToken("data")?.Where(v => !string.IsNullOrWhiteSpace(v.Value<string>("codeMatiere")))?.Select(v =>
            {
                if (Manager.Child.Subjects == null) Manager.Child.Subjects = new List<Subject>();
                if (!Manager.Child.Subjects.Any(s => s.id == v.Value<string>("codeMatiere")))
                {
                    Manager.Child.Subjects.Add(new Subject
                    {
                        id = v.Value<string>("codeMatiere"),
                        name = v.Value<string>("matiere"),
                        teachers = new[] { v.Value<string>("prof") }
                    });
                }
                return new ScheduledEvent
                {
                    subjectID = v.Value<string>("codeMatiere"),
                    teacher = v.Value<string>("prof"),
                    start = v.Value<DateTime>("start_date"),
                    end = v.Value<DateTime>("end_date"),
                    room = v.Value<string>("salle"),
                    canceled = v.Value<bool>("isAnnule")
                };
            });
            Manager.Child.Schedule.RemoveAll(s => s.start >= period.Start && s.end <= period.End);
            Manager.Child.Schedule.AddRange(events);

            Manager.HideLoadingPanel();
            onComplete?.Invoke(events);
        }
        
        public IEnumerator GetMessages(Action onComplete)
        {
            FileFormat.JSON result = null;
            Func<UnityWebRequest> request = () => UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/{Manager.Child.GetExtraData("type")}/{Accounts.selectedAccount.child}/messages.awp?verbe=getall&idClasseur=0", $"data={{\"token\": \"{token}\"}}");
            yield return TryConnection(request, "messages", (o) => result = o);
            if (result == null) yield break;
            Manager.Child.Messages = result.jToken.SelectTokens("data.messages.*").SelectMany(t =>
            {
                return t.Select(m =>
                {
                    var type = (m.Value<string>("mtype") == "received") ? "from" : "to";
                    return new Message
                    {
                        id = m.Value<uint>("id"),
                        read = m.Value<bool>("read"),
                        date = m.Value<DateTime>("date"),
                        subject = m.Value<string>("subject"),
                        correspondents = (type == "from" ? Enumerable.Repeat(m[type], 1) : m[type]).Select(c => c.Value<string>("name")).ToList(),
                        type = m.Value<string>("mtype") == "send" ? Message.Type.sent : Message.Type.received
                    };
                });
            }).OrderByDescending(m => m.date).ToList();

            Manager.HideLoadingPanel();
            onComplete?.Invoke();
        }
        public IEnumerator LoadExtraMessageData(uint messageID, Action onComplete)
        {
            var message = Manager.Child.Messages.FirstOrDefault(m => m.id == messageID);

            FileFormat.JSON result = null;
            Func<UnityWebRequest> request = () => UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/{ Manager.Child.GetExtraData("type") }/{Accounts.selectedAccount.child}/messages/{message.id}.awp?verbe=get&mode={(message.type == Message.Type.received ? "destinataire" : "expediteur")}", $"data={{\"token\": \"{token}\"}}");
            yield return TryConnection(request, "messages.content", (o) => result = o);
            if (result == null) yield break;

            var data = result.jToken.SelectToken("data");
            message.content = ProviderExtension.RemoveEmptyLines(ProviderExtension.HtmlToRichText(FromBase64(data.Value<string>("content"))));
            message.documents = data.SelectToken("files").Select(doc =>
                 {
                     return new Document
                     {
                         id = doc.Value<string>("id"),
                         name = doc.Value<string>("libelle"),
                         type = doc.Value<string>("type")
                     };
                 }).ToList();

            Manager.HideLoadingPanel();
            onComplete.Invoke();
        }
        public IEnumerator OpenMessageAttachment(Document doc) => OpenDocument(doc);

        public IEnumerator GetBooks(Action onComplete)
        {
            FileFormat.JSON result = null;
            Func<UnityWebRequest> request = () => UnityWebRequest.Post($"https://api.ecoledirecte.com/v3/Eleves/{Accounts.selectedAccount.child}/manuelsNumeriques.awp?verbe=get&", $"data={{\"token\": \"{token}\"}}");
            yield return TryConnection(request, "books", (o) => result = o);
            if (result == null) yield break;

            var books = new List<Book>();
            foreach (var obj in result.jToken.SelectToken("data")?.Values<JObject>().Where(obj => obj.SelectToken("disciplines").HasValues))
            {

                var cover = UnityWebRequestTexture.GetTexture(obj.Value<string>("urlCouverture"));
                yield return cover.SendWebRequest();
                var tex = DownloadHandlerTexture.GetContent(cover);

                books.Add(new Book
                {
                    id = obj.Value<string>("idRessource"),
                    subjectsID = obj.Value<JArray>("disciplines").Select(s => s.Value<string>()).ToArray(),
                    name = obj.Value<string>("libelle"),
                    editor = obj.Value<string>("editeur"),
                    url = obj.Value<string>("url"),
                    cover = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f))
                });
            }
            Manager.Child.Books = books;

            Manager.HideLoadingPanel();
            onComplete?.Invoke();
        }
        public IEnumerator OpenBook(Book book)
        {
            if (token == null) yield return Connect(Accounts.selectedAccount, null, null);
            Manager.UpdateLoadingStatus("provider.books.opening", "School book being opened");
            var bookRequest = UnityWebRequest.Post(book.url, new Dictionary<string, string> { { "token", token } });
            yield return bookRequest.SendWebRequest();
            Manager.HideLoadingPanel();

            var url = bookRequest.url;
            try
            {
                if (bookRequest.uri.Host == "api.ecoledirecte.com")
                    url = bookRequest.downloadHandler.text.Split(new[] { "<meta http-equiv=\"refresh\" content=\"1;url=" }, StringSplitOptions.None)[1].Split('"')[0];
            }
            catch
            {
                url = bookRequest.url;
                Debug.LogError("Unespected content at " + url + "\n\n" + bookRequest.downloadHandler.text);
            }
            Application.OpenURL(url);
        }

        public IEnumerator GetDocuments(Action onComplete)
        {
            Manager.UpdateLoadingStatus("provider.documents", "Getting documents");
            var root = new Folder { id = "root", name = LangueAPI.Get("documents.root", "Root") };

            var type = Manager.Child.GetExtraData("type");
            var modules = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(Manager.Child.GetExtraData("edModules") ?? "");

            if (type == "eleves")
            {
                if (modules.Contains("VIE_DE_LA_CLASSE")) //Ressources
                {
                    yield return Request($"https://api.ecoledirecte.com/v3/R/189/viedelaclasse.awp?verbe=get&", Result);
                    IEnumerator Result(JObject response)
                    {
                        root.folders.Add(new Folder
                        {
                            name = "Ressources",
                            id = "ressources",
                            folders = response.SelectToken("data.matieres").Select(m => new Folder
                            {
                                id = m.Value<string>("id"),
                                name = m.Value<string>("libelle"),
                                documents = m.SelectToken("fichiers").Select(f => new Document
                                {
                                    id = f.Value<string>("id"),
                                    name = f.Value<string>("libelle"),
                                    size = f.Value<uint>("taille"),
                                    type = f.Value<string>("type")
                                }).ToList()
                            }).ToList()
                        });
                        yield break;
                    }
                }

                if (modules.Contains("CLOUD")) //Cloud
                {
                    yield return Request($"https://api.ecoledirecte.com/v3/cloud/E/{Accounts.selectedAccount.child}.awp?verbe=get&", Result);
                    IEnumerator Result(JObject response)
                    {
                        Folder rootFolder = null;
                        yield return Analyse(response.SelectToken("data").First, (dir) => rootFolder = dir);
                        rootFolder.name = "Cloud";
                        root.folders.Add(rootFolder);

                        IEnumerator Analyse(JToken jToken, Action<Folder> output)
                        {
                            if (!jToken.Value<bool>("isLoaded"))
                            {
                                var id = jToken.Value<string>("id");
                                var search = $"\\E\\{Accounts.selectedAccount.child}";
                                var startIndex = id.IndexOf(search);
                                var folderID = id.Substring(startIndex == -1 ? 0 : (startIndex + search.Length));
                                yield return Request($"https://api.ecoledirecte.com/v3/cloud/E/{Accounts.selectedAccount.child}.awp?verbe=get&idFolder={folderID}", SetJToken);
                                IEnumerator SetJToken(JObject token) { jToken = token.SelectToken("data").First; yield break; }
                            }

                            var jFolders = jToken.SelectToken("children")?.Where(c => c.Value<string>("type") == "folder");
                            List<Folder> folders = new List<Folder>();
                            foreach (var folder in jFolders) yield return Analyse(folder, (dir) => folders.Add(dir));

                            output(new Folder
                            {
                                id = jToken.Value<string>("id"),
                                name = jToken.Value<string>("libelle"),
                                folders = folders,
                                documents = jToken.SelectToken("children").Where(c => c.Value<string>("type") == "file").Select(f => new Document
                                {
                                    id = f.Value<string>("id"),
                                    name = f.Value<string>("libelle"),
                                    added = f.Value<DateTime>("date"),
                                    size = f.Value<uint>("taille"),
                                    type = "CLOUD"
                                }).ToList()
                            });
                        }
                    }
                }

                if (modules.Contains("DOCUMENTS_ELEVE"))
                {
                    yield return Request($"https://api.ecoledirecte.com/v3/elevesDocuments.awp?verbe=get&", Result);
                    IEnumerator Result(JObject response) => Docs(response, (folder) => root.folders.Add(new Folder
                    {
                        name = "Documents",
                        id = "documents",
                        folders = folder
                    }));
                }
            }
            else if (type == "familles" && modules.Contains("DOCUMENTS")) //Documents
            {
                yield return Request($"https://api.ecoledirecte.com/v3/familledocuments.awp?verbe=get&", Result);
                IEnumerator Result(JObject response) => Docs(response, (folder) => root.folders = folder);
            }
            IEnumerator Docs(JObject result, Action<List<Folder>> callback)
            {
                callback(result.Value<JObject>("data").Properties().Select(f => new Folder
                {
                    id = f.Name,
                    name = f.Name,
                    documents = f.Where(d => d.Type != JTokenType.Object).SelectMany(d => d.Value<JToken>().Values<JToken>()).Select(d =>
                    {
                        return new Document
                        {
                            id = d.Value<string>("id"),
                            name = d.Value<string>("libelle") + ".pdf",
                            added = d.Value<DateTime>("date"),
                            type = d.Value<string>("type")
                        };
                    }).ToList()
                }).ToList());
                yield break;
            }

            Manager.Child.Documents = root;

            Manager.HideLoadingPanel();
            onComplete?.Invoke();


            IEnumerator Request(string url, Func<JObject, IEnumerator> output)
            {
                FileFormat.JSON result = null;
                yield return TryConnection(() => UnityWebRequest.Post(url, $"data={{\"token\": \"{token}\"}}"), "documents", (o) => result = o);
                if (result == null) yield break;
                yield return output(result.jToken);
            }
        }
        public IEnumerator OpenDocument(Document doc)
        {
            if (token == null) yield return Connect(Accounts.selectedAccount, null, null);
            UnityWebRequest webRequest = UnityWebRequest.Post("https://api.ecoledirecte.com/v3/telechargement.awp?verbe=get", new Dictionary<string, string> {
                { "token", token },
                { "leTypeDeFichier", doc.type },
                { "fichierId", doc.id }
            });
            yield return ProviderExtension.DownloadDoc(webRequest, doc);
        }

        string FromBase64(string b64) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        IEnumerator TryConnection(Func<UnityWebRequest> request, string getting, Action<FileFormat.JSON> output, bool manageErrors = true)
        {
            if (token == null) yield return Connect(Accounts.selectedAccount, null, null);

            Manager.UpdateLoadingStatus($"provider.{getting}", $"Getting {getting}");
            var _request = request();
            yield return _request.SendWebRequest();
            var result = new FileFormat.JSON(_request.downloadHandler.text);
            if (result.Value<int>("code") == 200)
            {
                output?.Invoke(result);
                yield break;
            }

            //There is an error
            if (!manageErrors) yield break;
            if (new[] { "Session expirée !", "Token invalide !" }.Contains(result.Value<string>("message")))
            {
                yield return Connect(Accounts.selectedAccount, null, null);
                yield return TryConnection(request, getting, output); //Retry connection
            }
            else Manager.FatalErrorDuringLoading(result.Value<string>("message"),
                $"Error getting {getting}, server returned \"{result?.Value<string>("message")}\"\nRequest URL: {_request.url}\n\nFull server response: {_request.downloadHandler.text}");
        }
    }
}
