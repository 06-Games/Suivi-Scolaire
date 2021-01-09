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
            transform.Find("Top").GetComponent<LayoutSwitcher>().switched += (mode) => Initialise(periodStart, defaultAction);
        }
        public void OnEnable()
        {
            language = LangueAPI.Culture;
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
            var _schedule = Manager.Data.ActiveChild.Schedule?.Where(h => dayList.Contains(h.start.Date)).ToList();
            if (_schedule?.Count > 0) action(period, _schedule);
            else UnityThread.executeCoroutine(module.GetSchedule(period, (schedule) => action(period, schedule.ToList())));
            return true;
        }
        TimeSpan min;
        void Refresh(IEnumerable<ScheduledEvent> schedule, TimeRange period)
        {
            ProviderExtension.GenerateSubjectColors();
            var WeekSwitcher = transform.Find("Top").Find("Week");
            WeekSwitcher.Find("Text").GetComponent<Text>().text = Screen.width > Screen.height ? LangueAPI.Get("schedule.period", "from [0] to [1]", period.Start.ToString("dd/MM"), period.End.ToString("dd/MM")) : period.Start.ToString("dd/MM");

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

                    var go = Instantiate(dateContent.GetChild(0).gameObject, dateContent).transform;
                    var goColor = Event.subject?.color ?? Color.gray;
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
