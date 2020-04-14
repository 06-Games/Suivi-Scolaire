using Integrations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Schedule
{
    public class Schedule : MonoBehaviour, Module
    {
        public float sizePerHour = 100F;

        internal static Dictionary<string, List<Event>> periodSchedule = new Dictionary<string, List<Event>>();
        static DateTime periodStart = DateTime.Now;
        static Dictionary<string, Color> subjectColor = new Dictionary<string, Color>();
        public void Reset()
        {
            periodSchedule = new Dictionary<string, List<Event>>();
            periodStart = DateTime.Now;
            subjectColor = new Dictionary<string, Color>();
        }

        void Awake()
        {
            defaultAction = (period, schedule) =>
            {
                Refresh(schedule?.Where(e => Screen.width > Screen.height || e.start.Date == periodStart).OrderBy(e => e.start), Screen.width > Screen.height ? period : new TimeRange(periodStart, periodStart));
            };
        }
        public void OnEnable()
        {
            periodSchedule = FirstStart.GetConfig<List<(string, List<Event>)>>("schedule").ToDictionary() ?? new Dictionary<string, List<Event>>();
            subjectColor = FirstStart.GetConfig<List<(string, Color)>>("scheduleColors").ToDictionary() ?? new Dictionary<string, Color>();

            if (!Manager.isReady) { gameObject.SetActive(false); return; }
            StartCoroutine(CheckOriantation());
            Initialise(periodStart, defaultAction);
            Manager.OpenModule(gameObject);
        }
        static void Save()
        {
            FirstStart.SetConfig("schedule", periodSchedule.Serializable());
            FirstStart.SetConfig("scheduleColors", subjectColor.Serializable());
        }

        internal static Action<TimeRange, List<Event>> defaultAction;
        public static bool Initialise(DateTime start, Action<TimeRange, List<Event>> action)
        {
            if (Screen.width <= Screen.height) periodStart = start.Date;
            int delta = DayOfWeek.Monday - start.DayOfWeek;
            start = start.AddDays(delta > 0 ? delta - 7 : delta);
            if (Screen.width > Screen.height) periodStart = start.Date;
            var period = new TimeRange(start, start + new TimeSpan(6, 0, 0, 0));

            if (!Manager.provider.TryGetModule(out Integrations.Schedule module)) { Manager.instance.transform.Find("Schedule").gameObject.SetActive(false); return false; }
            if (periodSchedule.TryGetValue(period.ToString("yyyy-MM-dd"), out var _schedule)) action(period, _schedule);
            else if (Manager.connectedToInternet) UnityThread.executeCoroutine(module.GetSchedule(period, (s) => { periodSchedule.Add(period.ToString("yyyy-MM-dd"), s); action(period, s); }));
            else action(period, null);
            return true;
        }
        void Refresh(IEnumerable<Event> schedule, TimeRange period)
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
                new Color32(255, 188, 10, 255),
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
            if (!(schedule?.Count() > 0)) return;

            var Content = transform.Find("Content").GetComponent<ScrollRect>().content;
            for (int i = 1; i < Content.childCount; i++) Destroy(Content.GetChild(i).gameObject);
            var language = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.EnglishName.Contains(Application.systemLanguage.ToString()));
            var min = schedule.Min(s => s.start.TimeOfDay);
            foreach (var Schedule in schedule.GroupBy(e => e.start.Date))
            {
                var datePanel = Instantiate(Content.GetChild(0).gameObject, Content).transform;
                datePanel.Find("Day").GetComponentInChildren<Text>().text = Schedule.Key.ToString("dddd", language);
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
                    if (!subjectColor.ContainsKey(Event.subject?.id))
                    {
                        var index = rnd.Next(colorPalette.Count);
                        var color = index < colorPalette.Count ? colorPalette[index] : new Color32();
                        subjectColor.Add(Event.subject?.id, color);
                        colorPalette.Remove(color);
                    }

                    var go = Instantiate(dateContent.GetChild(0).gameObject, dateContent).transform;
                    var goColor = subjectColor[Event.subject?.id];
                    goColor.a = Event.canceled ? 0.4F : 1;
                    go.GetComponent<Image>().color = goColor;
                    go.Find("Subject").GetComponent<Text>().text = Event.subject?.name;
                    go.Find("Room").GetComponent<Text>().text = Event.canceled ? $"<color=#F56E6E>{LangueAPI.Get("schedule.canceled", "Canceled")}</color>" : Event.room;
                    go.Find("Hours").GetComponent<Text>().text = $"{Event.start.ToString("HH:mm")} - {Event.end.ToString("HH:mm")}";
                    ((RectTransform)go).sizeDelta = new Vector2(1, sizePerHour * (float)(Event.end - Event.start).TotalHours);
                    go.gameObject.SetActive(true);
                    lastTime = Event.end.TimeOfDay;
                }
                datePanel.gameObject.SetActive(true);
            }
            Save();
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
            var Content = transform.Find("Content").GetComponent<ScrollRect>().content;
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
    }
}
