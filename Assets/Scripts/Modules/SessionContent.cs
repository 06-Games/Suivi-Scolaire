using Integrations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Modules
{
    public class SessionContent : MonoBehaviour
    {
        TimeRange week;
        public void Reset() => week = null;

        private void OnEnable()
        {
            if (!Manager.isReady) { gameObject.SetActive(false); return; }
            LoadWeek();
        }
        public void Reload()
        {
            if (!Manager.provider.TryGetModule(out Integrations.SessionContent module)) { gameObject.SetActive(false); return; }
            StartCoroutine(module.GetSessionContent(week, () => Refresh(Manager.Data.ActiveChild.SessionsContents.Where(h => week.Contains(h.date)))));
        }

        public void ChangeOfWeek(bool next)
        {
            week += new TimeSpan(next ? 7 : -7, 0, 0, 0);
            LoadWeek();
        }
        void LoadWeek()
        {
            if (week == null) week = GetWeek(DateTime.Now);
            var sessionsContents = Manager.Data.ActiveChild.SessionsContents.Where(h => week.Contains(h.date));
            if (sessionsContents?.Count() > 0) Refresh(sessionsContents);
            else Reload();
        }

        public void Refresh(IEnumerable<Integrations.Data.SessionContent> sessionContents)
        {
            var top = transform.Find("Top").Find("Week");
            top.GetComponentInChildren<Text>().text = LangueAPI.Get("homeworks.period", "from [0] to [1]", week.Start.ToString("dd/MM"), week.End.ToString("dd/MM"));
            top.Find("Next").GetComponent<Button>().interactable = week.End < DateTime.Now;
            var culture = LangueAPI.Culture;

            var sessionsContents = sessionContents.GroupBy(h => h.date);
            var content = transform.Find("Content").GetComponent<ScrollRect>().content;
            for (int i = 1; i < content.childCount; i++) Destroy(content.GetChild(i).gameObject);
            foreach (var day in sessionsContents)
            {
                var dayPanel = Instantiate(content.GetChild(0).gameObject, content).transform;
                dayPanel.Find("Head").Find("Date").GetComponent<Text>().text = day.Key.ToString("D", culture);
                var panel = dayPanel.Find("Panel");

                foreach (var sessionContent in day)
                {
                    var go = Instantiate(panel.GetChild(0).gameObject, panel).transform;

                    // Set to subect color
                    Homeworks.SetColor(go.GetComponent<Image>(), sessionContent.subject?.color ?? new Color());
                    Homeworks.SetColor(go.Find("Tint").GetComponentInChildren<Image>(), sessionContent.subject?.color ?? new Color());
                    var goContent = go.Find("Content");

                    // Infos
                    var infos = goContent.Find("Infos");
                    infos.Find("Subject").GetComponent<Text>().text = sessionContent.subject?.name;
                    infos.Find("AddedBy").GetComponent<Text>().text = sessionContent.addedBy;
                    var docs = infos.Find("Docs");
                    foreach (var doc in sessionContent.documents)
                    {
                        var docGo = Instantiate(docs.GetChild(0).gameObject, docs).transform;
                        docGo.GetComponent<Text>().text = $"• {doc.name}";
                        docGo.GetComponent<Button>().onClick.AddListener(() => UnityThread.executeCoroutine(Manager.provider.GetModule<Integrations.Homeworks>().OpenHomeworkAttachment(doc)));
                        docGo.gameObject.SetActive(true);
                    }

                    // Content
                    goContent.Find("Content").GetComponent<TMPro.TMP_InputField>().text = sessionContent.content;

                    go.gameObject.SetActive(true);
                }

                dayPanel.gameObject.SetActive(true);
            }
        }

        public void ShowHide(Transform go)
        {
            var panel = go.Find("Panel").gameObject;
            panel.SetActive(!panel.activeInHierarchy);
            go.Find("Head").Find("Arrow").transform.rotation = Quaternion.Euler(0, 0, panel.activeInHierarchy ? 0 : 180);
        }

        public static TimeRange GetWeek(DateTime date)
        {
            int delta = DayOfWeek.Monday - date.DayOfWeek;
            var monday = date.AddDays(delta > 0 ? delta - 7 : delta);
            return new TimeRange(monday, monday + new TimeSpan(6, 0, 0, 0));
        }
    }
}
