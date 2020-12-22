using Integrations.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace Integrations.Providers
{
    public class CambridgeKids : Provider, Auth, Homeworks
    {
        public string Name => "Cambridge Kids";

        string sessionId = "";
        public IEnumerator Connect(KeyValuePair<string, string> account) => Connect(account, null, null);
        public IEnumerator Connect(KeyValuePair<string, string> account, Action onComplete, Action<string> onError)
        {
            Manager.UpdateLoadingStatus("provider.connecting", "Establishing the connection with [0]", true, Name);
            sessionId = $"PHPSESSID={RandomString(26)}";

            //Get Token
            var data = new WWWForm();
            data.AddField("signin", account.Key);
            data.AddField("password", account.Value);
            var accountRequest = UnityWebRequest.Post("https://cambridgekids.sophiacloud.com/console/sophiacloud/login_validate.php?skins=cambridgekids", data);
            accountRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            accountRequest.SetRequestHeader("Cookie", sessionId);
            accountRequest.redirectLimit = 0;
            yield return accountRequest.SendWebRequest();

            var Location = accountRequest.GetResponseHeader("Location");
            if (Location == "../index.php?page=login&login_fail=1") onError?.Invoke("Un erreur est survenue");
            else onComplete?.Invoke();
            Manager.HideLoadingPanel();
        }

        public IEnumerator GetInfos(Data.Data data, Action<Data.Data> onComplete)
        {
            var accountRequest = UnityWebRequest.Get("https://cambridgekids.sophiacloud.com/console/index.php?page=MyDaily");
            accountRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            accountRequest.SetRequestHeader("Cookie", sessionId);
            yield return accountRequest.SendWebRequest();
            var response = "";
            foreach (var line in accountRequest.downloadHandler.text.Split('\n'))
                if (line.TrimStart('\t').StartsWith("window.page_data = "))
                {
                    response = line.TrimStart('\t').Substring("window.page_data = ".Length);
                    response = response.Remove(response.Length - 2);
                    break;
                }
            if (string.IsNullOrEmpty(response)) { Logging.Log("Un erreur est survenue", LogType.Error); Manager.HideLoadingPanel(); yield break; }
            var json = new FileFormat.JSON(response);
            var userID = json.GetCategory("user_session").GetCategory("settings").Value<string>("user_id");

            accountRequest = UnityWebRequest.Get($"https://cambridgekids.sophiacloud.com/console/sophiacloud/data_mgr.php?s=user&user_id={userID}&verbose=page");
            accountRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            accountRequest.SetRequestHeader("Cookie", sessionId);
            yield return accountRequest.SendWebRequest();
            json = new FileFormat.JSON(accountRequest.downloadHandler.text);
            var accountId = json.jToken.SelectToken("account_user").First.Value<string>("account_id");
            accountRequest = UnityWebRequest.Get($"https://cambridgekids.sophiacloud.com/console/sophiacloud/data_mgr.php?s=account&account_id={accountId}&verbose=page");
            accountRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            accountRequest.SetRequestHeader("Cookie", sessionId);
            yield return accountRequest.SendWebRequest();
            json = new FileFormat.JSON(accountRequest.downloadHandler.text);
            var enfants = json.jToken.SelectToken("account_user").Where(obj => obj.Value<string>("type") == "1").Select(enfant =>
            {
                var name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase($"{enfant.Value<string>("prenom")}{enfant.Value<string>("nom")}".ToLower());
                return new Child { name = name, id = enfant.Value<string>("user_id"), modules = new List<string> { "Homeworks" } };
            }).ToArray();

            Manager.HideLoadingPanel();
            var label = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(json.Value<string>("account_name").ToLower());

            data.Children = enfants;
            data.Label = label;
            onComplete?.Invoke(data);
            Manager.HideLoadingPanel();
        }
        public IEnumerator<Homework.Period> DiaryPeriods()
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                bool done = false;
                string error = null;
                UnityThread.executeCoroutine(Connect(Modules.Accounts.GetCredential, () => done = true, (e) => error = e));
                while (!done && error == null) yield return null;
                if (error != null)
                {
                    Logging.Log(error);
                    yield break;
                }
            }

            var request = UnityWebRequest.Get($"https://cambridgekids.sophiacloud.com/console/sophiacloud/data_mgr.php?s=feed&q=service_search&beneficiaire_user_id={Manager.Data.ActiveChild.id}&interactive_worksheet=1&scl_version=v46-697-gb3c6cf80&mode_debutant=1");
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            request.SetRequestHeader("Cookie", sessionId);
            request.SendWebRequest();
            while (!request.isDone) yield return null;
            var result = new FileFormat.JSON(request.downloadHandler.text);
            var periods = result.jToken.SelectToken("service_search").Where(obj => !string.IsNullOrEmpty(obj.Value<string>("date_debut")));
            foreach (var period in periods)
            {
                yield return new Homework.Period
                {
                    name = period.Value<string>("description"),
                    id = period.Value<string>("service_id")
                };
            }
        }
        public IEnumerator GetHomeworks(Homework.Period period, Action onComplete)
        {
            if (string.IsNullOrEmpty(sessionId)) yield return Connect(Modules.Accounts.GetCredential);
            Manager.UpdateLoadingStatus("provider.homeworks", "Getting homeworks");

            var request = UnityWebRequest.Get($"https://cambridgekids.sophiacloud.com/console/sophiacloud/data_mgr.php?s=interactive_worksheet&timestamp=0&service_id={period.id}");
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            request.SetRequestHeader("Cookie", sessionId);
            yield return request.SendWebRequest();
            var result = new FileFormat.JSON($"{{\"list\":{request.downloadHandler.text}}}");
            if (Manager.Data.ActiveChild.Subjects == null) Manager.Data.ActiveChild.Subjects = new List<Subject>();
            if (Manager.Data.ActiveChild.Homeworks == null) Manager.Data.ActiveChild.Homeworks = new List<Homework>();
            Manager.Data.ActiveChild.Homeworks.AddRange(result.jToken.SelectToken("list").SelectMany(obj =>
            {
                var data = obj.SelectToken("page_section");
                var docs = obj.SelectToken("link_file").Select(doc => new Document
                {
                    id = doc.Value<string>("up_file_id"),
                    name = doc.Value<string>("file_name")
                });
                return data.Select(d =>
                {
                    if (!Manager.Data.ActiveChild.Subjects.Any(s => s.id == d.Value<string>("page_section_id")))
                        Manager.Data.ActiveChild.Subjects.Add(new Subject { id = d.Value<string>("page_section_id"), name = d.Value<string>("sec_title"), color = new Color(0.3F, 0.3F, 0.3F) });
                    return new Homework
                    {
                        periodID = period.id,
                        subjectID = d.Value<string>("page_section_id"),
                        forThe = (double.TryParse(obj.Value<string>("date_evenement"), out var date) ? date : 0).UnixTimeStampToDateTime(),
                        addedBy = data.First.Value<string>("prenom") + " " + data.First.Value<string>("nom"),
                        addedThe = (double.TryParse(obj.Value<string>("date_creation"), out var _d) ? _d : 0).UnixTimeStampToDateTime(),
                        content = Renderer.HTML.ToRichText(d.Value<string>("text")).RemoveEmptyLines(),
                        documents = docs.ToList()
                    };
                });
            }));

            onComplete?.Invoke();
            Manager.HideLoadingPanel();
        }
        public IEnumerator OpenHomeworkAttachment(Document doc)
        {
            if (string.IsNullOrEmpty(sessionId)) yield return Connect(Modules.Accounts.GetCredential);
            UnityWebRequest webRequest = UnityWebRequest.Post($"https://cambridgekids.sophiacloud.com/console/sophiacloud/file_mgr.php?up_file_id={doc.id}",
                new Dictionary<string, string> { { "User-Agent", "Mozilla/5.0 Firefox/74.0" }, { "Cookie", sessionId } });
            yield return ProviderExtension.DownloadDoc(webRequest, doc);
        }
        public IEnumerator HomeworkDoneStatus(Homework homework) { yield break; }

        // Utils
        private static Random random = new Random();
        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
