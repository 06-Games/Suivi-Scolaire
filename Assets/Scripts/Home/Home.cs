using Integrations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Periods
{
    public class Home : MonoBehaviour, Module
    {
        public Sprite[] periodSprites;

        internal static List<Period> periods;
        static List<Schedule.Event> events;
        public void Reset() { periods = null; events = null; }
        public void OnEnable()
        {
            if (Manager.connectedToInternet) StartCoroutine(enumerator());
            else
            {
                periods = FirstStart.GetConfig<List<Period>>("periods");
                Refresh();
            }

            IEnumerator enumerator()
            {
                if (!Manager.isReady) { gameObject.SetActive(false); yield break; }
                if (periods == null && Manager.provider.TryGetModule(out Integrations.Periods hM)) yield return hM.GetPeriods((p) => periods = p.OrderBy(d => d.start).ToList());
                if (Marks.Marks.marks == null && Manager.provider.TryGetModule(out Integrations.Marks mM)) yield return mM.GetMarks(Marks.Marks.Initialise);
                if (events == null)
                {
                    bool ended = false;
                    if (!Schedule.Schedule.Initialise(DateTime.Now, (p, s) => { events = s.OrderBy(e => e.start).ToList(); ended = true; })) ended = true;
                    yield return new WaitUntil(() => ended);
                    Save();
                }
                Refresh();
            }
        }

        private void Awake() { transform.Find("Content").gameObject.SetActive(false); }
        static void Save() { FirstStart.SetConfig("periods", periods); }

        public void Refresh()
        {
            var Content = transform.Find("Content");
            var now = DateTime.Now;

            var period = Content.Find("Period");
            if (periods?.Count > 0)
            {
                var actualPeriod = periods.FirstOrDefault(p => p.start <= now && p.end >= now);
                period.Find("Img").Find("Image").GetComponent<Image>().sprite = periodSprites[actualPeriod.holiday ? 1 : 0];
                period.Find("Txt").Find("Period").GetComponent<Text>().text = actualPeriod.name;
                if (actualPeriod.holiday) period.Find("Txt").Find("Desc").GetComponent<Text>().text = LangueAPI.Get("", "Elles se termineront dans [0] jours", (actualPeriod.start - now).ToString("dd"));
                var nextPeriod = periods.FirstOrDefault(h => h.start > now);
                period.Find("Txt").Find("Desc").GetComponent<Text>().text = LangueAPI.Get(
                    actualPeriod.holiday ? "home.holidays.end" : "home.holidays.start",
                    actualPeriod.holiday ? "It ends in <b>[1]</b> days" : "<i>[0]</i>\nstart in <b>[1]</b> days",
                    nextPeriod?.name.ToLower(),
                    nextPeriod == null ? "0" : (nextPeriod.start - now).ToString("dd")
                );
                period.gameObject.SetActive(true);
            }
            else period.gameObject.SetActive(false);


            var lastMarks = Content.Find("Marks");
            if (Marks.Marks.marks?.Count >= 3)
            {
                for (int i = 2; i < lastMarks.childCount; i++) Destroy(lastMarks.GetChild(i).gameObject);
                for (int i = 1; i <= 3; i++)
                {
                    var m = Marks.Marks.marks.ElementAt(Marks.Marks.marks.Count - i);
                    var go = Instantiate(lastMarks.Find("Template").gameObject, lastMarks).transform;
                    go.Find("Date").GetComponent<Text>().text = m.date.ToString("dd/MM");
                    go.Find("Subject").GetComponent<Text>().text = m.subject.name;
                    go.Find("Value").GetComponent<Text>().text = m.mark == null ? $"<color=#aaa>{LangueAPI.Get("marks.absent", "Abs")}</color>" : (m.notSignificant ? $"<color=#aaa>({m.mark}<size=14>/{m.markOutOf}</size>)</color>" : $"{m.mark}<size=14>/{m.markOutOf}</size>");
                    go.Find("Coef").GetComponent<Text>().text = LangueAPI.Get("marks.coef", "coef [0]", m.coef);
                    go.gameObject.SetActive(true);
                }
                lastMarks.gameObject.SetActive(true);
            }
            else lastMarks.gameObject.SetActive(false);


            var schedule = Content.Find("Schedule");
            schedule.gameObject.SetActive(false);
            if (events?.Count > 0)
            {
                var actualIndex = events.FindIndex(e => e.start <= now && e.end >= now);
                schedule.Find("Bar").gameObject.SetActive(true);
                for (int i = 0; i < 2; i++)
                {
                    var go = schedule.Find(i == 0 ? "Currently" : "Next");
                    var E = i == 0 ? events.FirstOrDefault(e => e.start <= now && e.end >= now) : events.FirstOrDefault(e => e.start > now);
                    if (E == null || E.start.Day != now.Day) { go.gameObject.SetActive(false); schedule.Find("Bar").gameObject.SetActive(false); continue; }
                    else schedule.gameObject.SetActive(true);
                    go.Find("Subject").GetComponent<Text>().text = E.subject.name;
                    go.Find("Teacher").GetComponent<Text>().text = string.Join(", ", E.subject.teachers ?? Array.Empty<string>());
                    go.Find("Room").GetComponent<Text>().text = E.room;
                    go.Find("Hours").GetComponent<Text>().text = $"{E.start.ToString("HH:mm")} - {E.end.ToString("HH:mm")}";
                    go.gameObject.SetActive(true);
                }
            }

            Content.gameObject.SetActive(true);
        }
    }
}
