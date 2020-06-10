using Integrations;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Periods
{
    public class Home : MonoBehaviour, Module
    {
        public Sprite[] periodSprites;

        public void Reset() {}
        public void OnEnable()
        {
            StartCoroutine(enumerator());

            IEnumerator enumerator()
            {
                if (!Manager.isReady) { gameObject.SetActive(false); yield break; }
                if (Manager.Data.Periods == null && Manager.provider.TryGetModule(out Integrations.Periods hM)) yield return hM.GetPeriods();
                if (Manager.Data.Marks == null && Manager.provider.TryGetModule(out Integrations.Marks mM)) yield return mM.GetMarks();
                if (Manager.Data.Schedule == null)
                {
                    bool ended = false;
                    if (!Schedule.Schedule.Initialise(DateTime.Now, (p, s) => ended = true)) ended = true;
                    yield return new WaitUntil(() => ended);
                }
                Refresh();
            }
        }

        private void Awake() { transform.Find("Content").gameObject.SetActive(false); }

        public void Refresh()
        {
            var Content = transform.Find("Content");
            var now = DateTime.Now;

            var period = Content.Find("Period");
            if (Manager.Data.Periods?.Count > 0)
            {
                var actualPeriod = Manager.Data.Periods.FirstOrDefault(p => p.start <= now && p.end >= now);
                period.Find("Img").Find("Image").GetComponent<Image>().sprite = periodSprites[actualPeriod.holiday ? 1 : 0];
                period.Find("Txt").Find("Period").GetComponent<Text>().text = actualPeriod.name;

                var nextPeriod = Manager.Data.Periods.FirstOrDefault(h => h.start > now);
                var remaining = nextPeriod == null ? new TimeSpan() : (nextPeriod.start - now);
                var unit = remaining.TotalDays > 1 ? new[] { "days", "dd" } : (remaining.TotalHours > 1 ? new[] { "hours", "hh" } : new[] { "minutes", "mm" });
                period.Find("Txt").Find("Desc").GetComponent<Text>().text = LangueAPI.Get(
                    actualPeriod.holiday ? "home.holidays.end" : "home.holidays.start",
                    actualPeriod.holiday ? "It ends in [1]" : "<i>[0]</i>\nstart in [1]",
                    nextPeriod?.name.ToLower(),
                    LangueAPI.Get(
                        "home.holidays." + unit[0],
                        "<b>[0]</b> " + unit[0],
                        remaining.ToString(unit[1])
                    )
                );
                period.gameObject.SetActive(true);
            }
            else period.gameObject.SetActive(false);


            var lastMarks = Content.Find("Marks");
            if (Manager.Data.Marks?.Count >= 3)
            {
                for (int i = 2; i < lastMarks.childCount; i++) Destroy(lastMarks.GetChild(i).gameObject);
                for (int i = 1; i <= 3; i++)
                {
                    var m = Manager.Data.Marks.ElementAt(Manager.Data.Marks.Count - i);
                    var go = Instantiate(lastMarks.Find("Template").gameObject, lastMarks).transform;
                    go.Find("Date").GetComponent<Text>().text = m.date.ToString("dd/MM");
                    go.Find("Subject").GetComponent<Text>().text = m.subject.name;
                    go.Find("Value").GetComponent<TMPro.TextMeshProUGUI>().text = m.mark != null ? m.notSignificant ? $"<color=#aaa>({m.mark}<size=17>/{m.markOutOf}</size>)</color>"
                        : $"{m.mark}<size=17>/{m.markOutOf}</size>"
                        : (m.skills.Length == 0 || m.skills.Any(s => !s.value.HasValue) ? $"<color=#aaa>{LangueAPI.Get("marks.absent", "Abs")}</color>"
                        : string.Join(" ", m.skills.Select(s => $"<sprite index={s.value}>")));
                    go.Find("Coef").GetComponent<Text>().text = m.coef == 0 ? "" : LangueAPI.Get("marks.coef", "coef [0]", m.coef);
                    go.gameObject.SetActive(true);
                }
                lastMarks.gameObject.SetActive(true);
            }
            else lastMarks.gameObject.SetActive(false);


            var schedule = Content.Find("Schedule");
            schedule.gameObject.SetActive(false);
            if (Manager.Data.Schedule?.Count > 0)
            {
                schedule.Find("Bar").gameObject.SetActive(true);
                for (int i = 0; i < 2; i++)
                {
                    var go = schedule.Find(i == 0 ? "Currently" : "Next");
                    var E = i == 0 ? Manager.Data.Schedule.FirstOrDefault(e => e.start <= now && e.end >= now) : Manager.Data.Schedule.FirstOrDefault(e => e.start > now);
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
