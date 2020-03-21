using Homeworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace Integrations
{
    public class CambridgeKids : Provider, Auth, Homeworks
    {
        public string Name => "Cambridge Kids";


        static string sessionId = "";
        static string childId = "";
        public IEnumerator Connect(Account account, Action<Account> onComplete, Action<string> onError)
        {
            Manager.UpdateLoadingStatus("provider.connecting", "Establishing the connection with [0]", true, Name);
            sessionId = $"PHPSESSID={RandomString(26)}";

            //Get Token
            var data = new WWWForm();
            data.AddField("signin", account.id);
            data.AddField("password", account.password);
            var accountRequest = UnityWebRequest.Post("https://cambridgekids.sophiacloud.com/console/sophiacloud/login_validate.php?skins=cambridgekids", data);
            accountRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            accountRequest.SetRequestHeader("Cookie", sessionId);
            accountRequest.redirectLimit = 0;
            yield return accountRequest.SendWebRequest();

            if (account.child == null)
            {
                accountRequest = UnityWebRequest.Get(new Uri(accountRequest.uri, accountRequest.GetResponseHeader("Location")));
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
                    Action action = () =>
                    {
                        Logging.Log(enfant.Value<string>("prenom") + " has been selected");
                        account.child = childId = enfant.Value<string>("user_id");
                        account.username = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase($"{enfant.Value<string>("prenom")}{enfant.Value<string>("nom")}".ToLower());
                        onComplete?.Invoke(account);
                    };
                    var name = enfant.Value<string>("prenom") + enfant.Value<string>("nom");
                    return (action, name, (Sprite)null);
                }).ToList();
                Manager.HideLoadingPanel();
                FirstStart.SelectChilds(enfants);
            }
            else
            {
                childId = account.child;
                Manager.HideLoadingPanel();
                onComplete?.Invoke(account);
            }
        }
        public IEnumerator<global::Homeworks.Period> DiaryPeriods()
        {
            var request = UnityWebRequest.Get($"https://cambridgekids.sophiacloud.com/console/sophiacloud/data_mgr.php?s=feed&q=service_search&beneficiaire_user_id={childId}&interactive_worksheet=1&scl_version=v46-697-gb3c6cf80&mode_debutant=1");
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            request.SetRequestHeader("Cookie", sessionId);
            request.SendWebRequest();
            while (!request.isDone) yield return null;
            var result = new FileFormat.JSON(request.downloadHandler.text);
            var periods = result.jToken.SelectToken("service_search").Where(obj => !string.IsNullOrEmpty(obj.Value<string>("date_debut")));
            foreach (var period in periods)
            {
                yield return new global::Homeworks.Period()
                {
                    name = period.Value<string>("description"),
                    id = period.Value<string>("service_id")
                };
            }
        }
        public IEnumerator GetHomeworks(global::Homeworks.Period period, Action<List<Homework>> onComplete)
        {
            Manager.UpdateLoadingStatus("provider.homeworks", "Getting homeworks");

            var request = UnityWebRequest.Get($"https://cambridgekids.sophiacloud.com/console/sophiacloud/data_mgr.php?s=interactive_worksheet&timestamp=0&service_id={period.id}");
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 Firefox/74.0");
            request.SetRequestHeader("Cookie", sessionId);
            yield return request.SendWebRequest();
            var result = new FileFormat.JSON($"{{\"list\":{request.downloadHandler.text}}}");
            var homeworks = result.jToken.SelectToken("list").SelectMany(obj =>
            {
                var data = obj.SelectToken("page_section");
                var docs = obj.SelectToken("link_file").Select(doc => new Request()
                {
                    docName = doc.Value<string>("file_name"),
                    url = $"https://cambridgekids.sophiacloud.com/console/sophiacloud/file_mgr.php?up_file_id={doc.Value<string>("up_file_id")}",
                    headers = new Dictionary<string, string>() { { "User-Agent", "Mozilla/5.0 Firefox/74.0" }, { "Cookie", sessionId } },
                    method = Request.Method.Get
                });
                return data.Select(d => new Homework()
                {
                    subject = new Subject() { name = d.Value<string>("sec_title") },
                    forThe = UnixTimeStampToDateTime(double.TryParse(obj.Value<string>("date_evenement"), out var date) ? date : 0),
                    addedBy = data.First.Value<string>("prenom") + " " + data.First.Value<string>("nom"),
                    addedThe = UnixTimeStampToDateTime(double.TryParse(obj.Value<string>("date_creation"), out var _d) ? _d : 0),
                    content = ProviderExtension.RemoveEmptyLines(ProviderExtension.HtmlToRichText(d.Value<string>("text"))),
                    documents = docs
                });
            }).ToList();

            onComplete?.Invoke(homeworks);
            Manager.HideLoadingPanel();
        }

        private static Random random = new Random();
        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
