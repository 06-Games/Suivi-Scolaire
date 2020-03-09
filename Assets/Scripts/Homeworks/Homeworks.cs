using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Homeworks
{
    public class Homeworks : MonoBehaviour
    {
        internal static Dictionary<string, List<Homework>> periodHomeworks = new Dictionary<string, List<Homework>>();
        DateTime? periodStart = null;

        public void OnEnable()
        {
            StartCoroutine(CheckOriantation());
            if (!Manager.isReady) { gameObject.SetActive(false); return; }
            if (periodStart == null) Initialise(null);
            else Manager.OpenModule(gameObject);
        }
        public void Initialise(DateTime? start)
        {
            periodStart = start;
            var period = !start.HasValue ? null : new TimeRange(start.Value, start.Value + new TimeSpan(6, 0, 0, 0));
            Action<List<Homework>> action = (homeworks) =>
            {
                Refresh(homeworks.OrderBy(h => h.forThe), period);
                Manager.OpenModule(gameObject);
            };
            if (periodHomeworks.TryGetValue(period?.ToString("yyyy-MM-dd") ?? "Upcomming", out var _homeworks)) action(_homeworks);
            else StartCoroutine(Manager.provider.GetHomeworks(period, (h) => { periodHomeworks.Add(period?.ToString("yyyy-MM-dd") ?? "Upcomming", h); action(h); }));
        }
        void Refresh(IEnumerable<Homework> homeworks, TimeRange period)
        {
            var WeekSwitcher = transform.Find("Top").Find("Week");
            WeekSwitcher.Find("Text").GetComponent<Text>().text = period == null ? "A Venir" : $"du {period.Start.ToString("dd/MM")} au {period.End.ToString("dd/MM")}";
            WeekSwitcher.Find("Next").GetComponent<Button>().interactable = period != null;

            var Content = transform.Find("Content").GetComponent<ScrollRect>().content;
            for (int i = 1; i < Content.childCount; i++) Destroy(Content.GetChild(i).gameObject);
            var language = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.EnglishName.Contains(Application.systemLanguage.ToString()));
            foreach (var Homeworks in homeworks.GroupBy(h => h.forThe))
            {
                var datePanel = Instantiate(Content.GetChild(0).gameObject, Content).transform;
                datePanel.Find("Head").Find("Date").GetComponent<Text>().text = Homeworks.Key.ToString("D", language);

                var panel = datePanel.Find("Panel");
                for (int i = 1; i < panel.childCount; i++) Destroy(panel.GetChild(i).gameObject);
                foreach (var homework in Homeworks)
                {
                    var go = Instantiate(panel.GetChild(0).gameObject, panel).transform;
                    go.GetComponent<LayoutSwitcher>().Switch(Screen.width > Screen.height ? LayoutSwitcher.Mode.Horizontal : LayoutSwitcher.Mode.Vertical);

                    var infos = go.Find("Infos");
                    infos.Find("Subject").GetComponent<Text>().text = homework.subject?.name;
                    infos.Find("Extra").GetComponent<Text>().text = $"Ajouté le {homework.addedThe.ToString("dd/MM")} par {homework.addedBy}";
                    var docs = infos.Find("Docs");
                    foreach (var doc in homework.documents)
                    {
                        var docGo = Instantiate(docs.GetChild(0).gameObject, docs).transform;
                        docGo.GetComponent<Text>().text = $"• {doc.Item1}";
                        docGo.GetComponent<Button>().onClick.AddListener(() => {
                            UnityThread.executeCoroutine(GetDoc());
                            IEnumerator GetDoc()
                            {
                                var request = UnityEngine.Networking.UnityWebRequest.Post(doc.Item2, doc.Item3);
                                request.SendWebRequest();
                                while (!request.isDone)
                                {
                                    Manager.UpdateLoadingStatus($"Downloading: {(request.downloadProgress * 100).ToString("0")}%");
                                    yield return new WaitForEndOfFrame();
                                }
                                Manager.HideLoadingPanel();
                                var path = Application.temporaryCachePath + "/Homeworks___" + doc.Item1;
                                System.IO.File.WriteAllBytes(path, request.downloadHandler.data);
                                NativeShare.SharePC(path, fileName: doc.Item1);
                            }
                        });
                        docGo.gameObject.SetActive(true);
                    }

                    go.Find("Content").GetComponent<TMPro.TextMeshProUGUI>().text = homework.content;
                    go.gameObject.SetActive(true);
                }
                panel.gameObject.SetActive(false);
                datePanel.gameObject.SetActive(true);
            }
        }

        public void ChangePeriod(bool next)
        {
            var start = periodStart ?? DateTime.Now;
            int delta = DayOfWeek.Monday - start.DayOfWeek;
            DateTime monday = start.AddDays(delta > 0 ? delta - 7 : delta);
            var week = periodStart == null && !next ? monday : monday + new TimeSpan(next ? 7 : -7, 0, 0, 0);
            Initialise(week > DateTime.Now ? null : (DateTime?)week);
        }


        IEnumerator CheckOriantation()
        {
            var Top = transform.Find("Top").GetComponent<LayoutSwitcher>();
            var Content = transform.Find("Content").GetComponent<ScrollRect>().content;
            while (true)
            {
                bool paysage = Screen.width > Screen.height;
                Top.Switch(paysage ? LayoutSwitcher.Mode.Horizontal : LayoutSwitcher.Mode.Vertical);
                for (int i = 1; i < Content.childCount; i++)
                {
                    foreach (var switcher in Content.GetChild(i).Find("Panel").GetComponentsInChildren<LayoutSwitcher>())
                        switcher.Switch(paysage ? LayoutSwitcher.Mode.Horizontal : LayoutSwitcher.Mode.Vertical);
                }
                yield return new WaitWhile(() => paysage == Screen.width > Screen.height);
            }
        }

        public void ShowHide(Transform go)
        {
            var panel = go.Find("Panel").gameObject;
            panel.SetActive(!panel.activeInHierarchy);
            go.Find("Head").Find("Arrow").transform.rotation = Quaternion.Euler(0, 0, panel.activeInHierarchy ? 0 : 180);
        }
    }
}
