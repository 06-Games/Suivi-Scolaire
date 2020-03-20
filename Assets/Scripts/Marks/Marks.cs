﻿using Integrations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Marks
{
    public class Marks : MonoBehaviour, Module
    {
        internal static List<Period> periods;
        internal static List<Subject> subjects;
        internal static List<Mark> marks;
        public void Reset() { periods = null; subjects = null; marks = null; }

        public void OnEnable()
        {
            if (!Manager.isReady || !Manager.provider.TryGetModule(out Integrations.Marks module)) { gameObject.SetActive(false); return; }
            if (marks == null) StartCoroutine(module.GetMarks(Initialise));
            else
            {
                Refresh();
                Manager.OpenModule(gameObject);
            }
        }
        public void Initialise(IEnumerable<Period> _periods, IEnumerable<Subject> _subjects, IEnumerable<Mark> _marks)
        {
            periods = _periods.OrderBy(s => s.start).ToList();
            subjects = _subjects.OrderBy(s => s.name).ToList();
            marks = _marks.ToList();

            period.ClearOptions();
            period.AddOptions(new List<string>() { LangueAPI.Get("marks.displayedPeriod.all", "All") });
            period.AddOptions(periods.Select(p => p.name).ToList());
            period.value = periods.IndexOf(periods.FirstOrDefault(p => p.start <= System.DateTime.Now && p.end >= System.DateTime.Now)) + 1;

            Refresh();
            Manager.OpenModule(gameObject);
        }

        public LayoutSwitcher topLayoutSwitcher;
        public HorizontalLayoutGroup subjectBtns;
        void Update()
        {
            topLayoutSwitcher.Switch(Screen.width > Screen.height ? LayoutSwitcher.Mode.Horizontal : LayoutSwitcher.Mode.Vertical);
            subjectBtns.childAlignment = topLayoutSwitcher.mode == LayoutSwitcher.Mode.Horizontal ? TextAnchor.UpperRight : TextAnchor.UpperLeft;

            if (groupBy.value == 1 && Input.GetKeyDown(KeyCode.Escape) && transform.Find("Content").Find("Marks").gameObject.activeInHierarchy) Refresh();
        }

        public Dropdown groupBy;
        public Dropdown sortBy;
        public Dropdown period;

        public void Refresh() { Refresh(null); }
        public void Refresh(Subject selectedSubject)
        {
            var marksByS = marks.GroupBy(m => m.subject).ToDictionary(m => m.Key, _m => _m.Where(m => period.value == 0 || m.period == periods[period.value - 1]).ToList());
            var average = marksByS.ToDictionary(s => s.Key, s =>
            {
                var _marks = s.Value.Where(m => m.mark != null && !m.notSignificant);
                return _marks.Count() == 0 ? (float?)null : _marks.Sum(m => (float)m.mark / m.markOutOf * 20F * m.coef) / _marks.Sum(m => m.coef);
            });
            var coef = marksByS.Keys.ToDictionary(s => s, s => average[s] == null ? 0 : s.coef);
            var classAverage = marksByS.ToDictionary(s => s.Key, s =>
            {
                var _marks = s.Value.Where(m => m.classAverage != null && !m.notSignificant);
                return _marks.Count() == 0 ? (float?)null : _marks.Sum(m => m.classAverage.Value / m.markOutOf * 20F * m.coef) / _marks.Sum(m => m.coef);
            });

            var top = topLayoutSwitcher.transform;
            if (selectedSubject != null) top.Find("Subject Btns").Find("Subject").GetComponent<Text>().text = selectedSubject.name;
            for (int i = 1; i < top.childCount; i++) top.GetChild(i).gameObject.SetActive(selectedSubject == null ? i == 1 : i == 2);
            var contentPanel = transform.Find("Content");
            for (int i = 0; i < contentPanel.childCount; i++) contentPanel.GetChild(i).gameObject.SetActive(i == (selectedSubject != null ? 0 : groupBy.value));
            var subjectsPanel = contentPanel.Find(groupBy.value == 0 || selectedSubject != null ? "Marks" : "Subjects").GetComponent<ScrollRect>().content;
            for (int i = 1; i < subjectsPanel.childCount; i++) Destroy(subjectsPanel.GetChild(i).gameObject);

            if (groupBy.value == 0) DisplayMarks(marksByS.SelectMany(s => s.Value));
            else if (groupBy.value == 1 && selectedSubject != null) DisplayMarks(marksByS[selectedSubject]);
            else if (groupBy.value == 1) DisplaySubjects();
            void DisplayMarks(IEnumerable<Mark> marks)
            {
                if (sortBy.value == 0) marks = marks.OrderBy(m => m.name);
                else if (sortBy.value == 1) marks = marks.OrderBy(m => m.mark / m.markOutOf);
                else if (sortBy.value == 2) marks = marks.OrderBy(m => m.date);

                foreach (var m in marks)
                {
                    var btn = Instantiate(subjectsPanel.GetChild(0).gameObject, subjectsPanel).transform;
                    btn.Find("Name").GetComponent<Text>().text = string.IsNullOrWhiteSpace(m.name) ? $"<i>{LangueAPI.Get("marks.noName", "Name not specified")}</i>" : m.name;
                    btn.Find("Value").GetComponent<Text>().text = m.mark == null ? $"<color=#aaa>{LangueAPI.Get("marks.absent", "Abs")}</color>" : (m.notSignificant ? $"<color=#aaa>({m.mark}<size=12>/{m.markOutOf}</size>)</color>" : $"{m.mark}<size=12>/{m.markOutOf}</size>");
                    btn.Find("Subject").GetComponent<Text>().text = selectedSubject == null ? m.subject.name : "";
                    btn.Find("Date").GetComponent<Text>().text = m.date.ToString("dd/MM/yyyy");
                    btn.Find("Coef").GetComponent<Text>().text = LangueAPI.Get("marks.coef", "coef [0]", m.coef);
                    btn.gameObject.SetActive(true);
                }
            }
            void DisplaySubjects()
            {
                if (sortBy.value == 0) marksByS = marksByS.OrderBy(m => m.Key.name).ToDictionary(k => k.Key, v => v.Value);
                else if (sortBy.value == 1) marksByS = marksByS.OrderBy(m => average[m.Key]).ToDictionary(k => k.Key, v => v.Value);
                else if (sortBy.value == 2) marksByS = marksByS.OrderBy(m => m.Value.OrderBy(M => M.date).FirstOrDefault().date).ToDictionary(k => k.Key, v => v.Value);
                foreach (var subject in marksByS)
                {
                    var btn = Instantiate(subjectsPanel.GetChild(0).gameObject, subjectsPanel).transform;
                    if (subject.Value.Count() > 0) btn.GetComponent<Button>().onClick.AddListener(() => Refresh(subject.Key));
                    btn.Find("Name").GetComponent<Text>().text = subject.Key.name;
                    btn.Find("Teacher").GetComponent<Text>().text = string.Join("\n", subject.Key.teachers);
                    btn.Find("Average").GetComponent<Text>().text = average[subject.Key] == null ? "" : average[subject.Key].Value.ToString("0.##") + "<size=12>/20</size>";
                    btn.Find("Class Average").GetComponent<Text>().text = classAverage[subject.Key] == null ? "" : classAverage[subject.Key].Value.ToString("0.##") + "<size=12>/20</size>";
                    btn.gameObject.SetActive(true);
                }
            }

            var bottom = transform.Find("Bottom");
            bottom.Find("Average").GetComponent<Text>().text = coef.Values.Sum() > 0 ? LangueAPI.Get(selectedSubject == null ? "marks.overallAverage" : "marks.average", selectedSubject == null ? "My overall average: [0]" : "My average: [0]", $"{(marksByS.Keys.Sum(s => (average[s] ?? 0) * coef[s]) / coef.Values.Sum()).ToString("0.##")}<size=12>/20</size>") : "";
            bottom.Find("Class Average").GetComponent<Text>().text = coef.Values.Sum() > 0 ? $@"Moyenne générale de la classe: {(marksByS.Keys.Sum(s => (classAverage[s] ?? 0) * s.coef) / marksByS.Keys.Sum(s => s.coef)).ToString("0.##")}<size=12>/20</size>" : "";
        }
    }
}