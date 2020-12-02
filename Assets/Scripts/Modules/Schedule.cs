using Integrations;
using Integrations.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Modules
{
    public class Schedule : MonoBehaviour, Module
    {
        public float sizePerHour = 100F;

        Action<TimeRange, List<ScheduledEvent>> defaultAction;
        static DateTime periodStart { get; set; } = DateTime.Now;
        public void Reset() => periodStart = DateTime.Now;
        public void Reload()
        {
            if (!Manager.provider.TryGetModule(out Integrations.Schedule module)) { gameObject.SetActive(false); return; }
            var period = new TimeRange(periodStart, periodStart + new TimeSpan(6, 0, 0, 0));
            UnityThread.executeCoroutine(module.GetSchedule(period, (schedule) => defaultAction(period, schedule.ToList())));
        }

        Transform content;
        CultureInfo language;
        void Awake()
        {
            content = transform.Find("Content").GetComponent<ScrollRect>().content;
            defaultAction = (period, schedule) =>
            {
                Refresh(schedule.Where(e => Screen.width > Screen.height || e.start.Date == periodStart).OrderBy(e => e.start), Screen.width > Screen.height ? period : new TimeRange(periodStart, periodStart));
            };
        }
        public void OnEnable()
        {
            language = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.EnglishName.Contains(Application.systemLanguage.ToString()));
            StartCoroutine(CheckOriantation());
            if (!Manager.isReady) { gameObject.SetActive(false); return; }
            Initialise(periodStart, defaultAction);
            Manager.OpenModule(gameObject);
        }
        public static bool Initialise(DateTime start, Action<TimeRange, List<ScheduledEvent>> action)
        {
            if (Screen.width <= Screen.height) periodStart = start.Date;
            int delta = DayOfWeek.Monday - start.DayOfWeek;
            start = start.AddDays(delta > 0 ? delta - 7 : delta);
            if (Screen.width > Screen.height) periodStart = start.Date;
            var period = new TimeRange(start, start + new TimeSpan(6, 0, 0, 0));

            if (!Manager.provider.TryGetModule(out Integrations.Schedule module)) { Manager.instance.transform.Find("Schedule").gameObject.SetActive(false); return false; }
            var dayList = period.DayList();
            var _schedule = Manager.Child.Schedule?.Where(h => dayList.Contains(h.start.Date)).ToList();
            if (_schedule?.Count > 0) action(period, _schedule);
            else UnityThread.executeCoroutine(module.GetSchedule(period, (schedule) => action(period, schedule.ToList())));
            return true;
        }
        TimeSpan min;
        void Refresh(IEnumerable<ScheduledEvent> schedule, TimeRange period)
        {
            var WeekSwitcher = transform.Find("Top").Find("Week");
            WeekSwitcher.Find("Text").GetComponent<Text>().text = Screen.width > Screen.height ? LangueAPI.Get("schedule.period", "from [0] to [1]", period.Start.ToString("dd/MM"), period.End.ToString("dd/MM")) : period.Start.ToString("dd/MM");

            var rnd = new System.Random();
            var colorPalette = new List<Color32> {
                new Color32(100, 140, 200, 255),
                new Color32(165, 170, 190, 255),
                new Color32(112, 162, 136, 255),
                new Color32(218, 183, 133, 255),
                new Color32(213, 137, 111, 255),
                new Color32(255, 188, 010, 255),
                new Color32(174, 118, 166, 255),
                new Color32(204, 214, 235, 255),
                new Color32(163, 195, 217, 255),
                new Color32(170, 229, 153, 255),
                new Color32(223, 229, 192, 255),
                new Color32(202, 175, 234, 255),
                new Color32(249, 215, 194, 255),
                new Color32(217, 244, 146, 255),
                new Color32(241, 227, 243, 255)
            };

            transform.Find("Content").gameObject.SetActive(schedule?.Count() > 0);
            transform.Find("Empty").gameObject.SetActive(!(schedule?.Count() > 0));
            if (schedule == null || schedule.Count() == 0) return;

            for (int i = 1; i < content.childCount; i++) Destroy(content.GetChild(i).gameObject);
            min = schedule.Min(s => s.start.TimeOfDay);
            foreach (var Schedule in schedule.GroupBy(e => e.start.Date))
            {
                var datePanel = Instantiate(content.GetChild(0).gameObject, content).transform;
                datePanel.Find("Day").GetComponentInChildren<Text>().text = Schedule.Key.ToString("dddd", language);
                datePanel.name = Schedule.Key.ToString("yyyy-MM-dd");
                var dateContent = datePanel.Find("Content");
                var lastTime = min;
                foreach (var Event in Schedule)
                {
                    if (lastTime < Event.start.TimeOfDay)
                    {
                        var hole = new GameObject();
                        hole.transform.SetParent(dateContent);
                        hole.AddComponent<RectTransform>().sizeDelta = new Vector2(1, sizePerHour * (float)(Event.start.TimeOfDay - lastTime).TotalHours);
                    }
                    if (Event.subject.color == default)
                    {
                        var index = rnd.Next(colorPalette.Count);
                        Event.subject.color = index < colorPalette.Count ? colorPalette[index] : new Color32(100, 140, 200, 255);
                        colorPalette.Remove(Event.subject.color);
                    }
                    else colorPalette.Remove(Event.subject.color);

                    var go = Instantiate(dateContent.GetChild(0).gameObject, dateContent).transform;
                    var goColor = Event.subject.color;
                    goColor.a = Event.canceled ? 0.4F : 1;
                    go.GetComponent<Image>().color = goColor;
                    var subject = Event.subject;
                    if (subject == null) Debug.LogError(Event.room + " " + Event.start + "\n" + Event.subjectID);
                    go.Find("Subject").GetComponent<Text>().text = subject?.name ?? Event.subjectID;
                    go.Find("Room").GetComponent<Text>().text = Event.canceled ? $"<color=#F56E6E>{LangueAPI.Get("schedule.canceled", "Canceled")}</color>" : Event.room;
                    go.Find("Teacher").GetComponent<Text>().text = Event.teacher ?? string.Join(" / ", subject.teachers);
                    go.Find("Hours").GetComponent<Text>().text = $"{Event.start.ToString("HH:mm")} - {Event.end.ToString("HH:mm")}";
                    ((RectTransform)go).sizeDelta = new Vector2(1, sizePerHour * (float)(Event.end - Event.start).TotalHours);
                    go.gameObject.SetActive(true);
                    lastTime = Event.end.TimeOfDay;
                }
                datePanel.gameObject.SetActive(true);
            }
        }

        public void ChangePeriod(bool next)
        {
            if (Screen.width > Screen.height)
            {
                int delta = DayOfWeek.Monday - periodStart.DayOfWeek;
                Initialise(periodStart.AddDays(delta > 0 ? delta - 7 : delta) + new TimeSpan(next ? 7 : -7, 0, 0, 0), defaultAction);
            }
            else Initialise(periodStart.AddDays(next ? 1 : -1), defaultAction);
        }


        IEnumerator CheckOriantation()
        {
            var Top = transform.Find("Top").GetComponent<LayoutSwitcher>();
            while (true)
            {
                bool paysage = Screen.width > Screen.height;
                yield return new WaitWhile(() => paysage == Screen.width > Screen.height);
                Top.Switch(paysage ? LayoutSwitcher.Mode.Vertical : LayoutSwitcher.Mode.Horizontal);
                Initialise(periodStart, defaultAction);
            }
        }

        public void ShowHide(Transform go)
        {
            var panel = go.Find("Panel").gameObject;
            panel.SetActive(!panel.activeInHierarchy);
            go.Find("Head").Find("Arrow").transform.rotation = Quaternion.Euler(0, 0, panel.activeInHierarchy ? 0 : 180);
        }

        void Update()
        {
            var now = DateTime.Now;
            var time = content.Find(now.ToString("yyyy-MM-dd"))?.Find("Actual time").GetComponent<RectTransform>();
            if (time == null) return;
            time.gameObject.SetActive(true);
            time.anchoredPosition = new Vector2(0, (float)(now.TimeOfDay - min).TotalHours * sizePerHour * -1 - 50);
            time.GetComponentInChildren<Text>().text = now.ToString("HH:mm");
        }
    }
}
