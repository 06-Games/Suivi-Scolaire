using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Marks
{
    public class Marks : MonoBehaviour
    {
        internal static List<Period> periods;
        internal static List<Subject> subjects;
        internal static List<Mark> marks;

        public void Initialise(IEnumerable<Period> _periods, IEnumerable<Subject> _subjects, IEnumerable<Mark> _marks)
        {
            periods = _periods.OrderBy(s => s.start).ToList();
            subjects = _subjects.OrderBy(s => s.name).ToList();
            marks = _marks.OrderByDescending(m => m.date).ToList();

            period.ClearOptions();
            period.AddOptions(new List<string>() { "Tout" });
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
        }

        public Dropdown groupBy;
        public Dropdown period;

        public void Refresh()
        {
            if (groupBy.value == 0) DisplayMarks(marks.Where(m => period.value == 0 || m.period == periods[period.value - 1]));
            else if (groupBy.value == 1) DisplaySubjects();
        }
        void DisplayMarks(IEnumerable<Mark> marks, Subject selectedSubject = null)
        {
            var top = topLayoutSwitcher.transform;
            if(selectedSubject != null) top.Find("Subject Btns").Find("Subject").GetComponent<Text>().text = selectedSubject.name;
            for (int i = 2; i < top.childCount; i++) top.GetChild(i).gameObject.SetActive(selectedSubject == null ? i == 2 : i == 3);
            var contentPanel = transform.Find("Content");
            for (int i = 0; i < contentPanel.childCount; i++) contentPanel.GetChild(i).gameObject.SetActive(i == 0);
            var subjectsPanel = contentPanel.Find("Marks").GetComponent<ScrollRect>().content;
            for (int i = 1; i < subjectsPanel.childCount; i++) Destroy(subjectsPanel.GetChild(i).gameObject);

            var average = new Dictionary<string, float>();
            var coef = new Dictionary<string, float>();
            var subCoef = new Dictionary<string, float>();
            foreach (var subject in subjects) { average.Add(subject.id, 0); coef.Add(subject.id, 0); subCoef.Add(subject.id, subject.coef); }
            foreach (var m in marks)
            {
                var btn = Instantiate(subjectsPanel.GetChild(0).gameObject, subjectsPanel).transform;
                btn.Find("Name").GetComponent<Text>().text = string.IsNullOrWhiteSpace(m.name) ? "<i>Nom non précisé</i>" : m.name;
                btn.Find("Value").GetComponent<Text>().text = m.mark == null ? "<color=#aaa>Abs</color>" : (m.notSignificant ? $"<color=#aaa>({m.mark}<size=12>/{m.markOutOf}</size>)</color>" : $"{m.mark}<size=12>/{m.markOutOf}</size>");
                btn.Find("Subject").GetComponent<Text>().text = selectedSubject == null ? m.subject.name : "";
                btn.Find("Date").GetComponent<Text>().text = m.date.ToString("dd/MM/yyyy");
                btn.Find("Coef").GetComponent<Text>().text = "coef " + m.coef;
                btn.gameObject.SetActive(true);

                if (m.mark != null && !m.notSignificant)
                {
                    average[m.subject.id] += (float)m.mark / m.markOutOf * 20F * m.coef;
                    coef[m.subject.id] += m.coef;
                }
            }

            var generalAverage = average.Sum(a => {
                var subAv = a.Value / coef[a.Key] * subCoef[a.Key];
                return float.IsNaN(subAv) ? 0 : subAv;
            });
            var generalCoef = subCoef.Sum(s => coef[s.Key] == 0 ? 0 : s.Value);
            transform.Find("Bottom").Find("Average").GetComponent<Text>().text = generalCoef > 0 ? $"Ma moyenne{(selectedSubject == null ? "générale" : "")}: {(generalAverage / generalCoef).ToString("0.##")}<size=12>/20</size>" : "";
        }
        public void DisplaySubjects()
        {
            var top = topLayoutSwitcher.transform;
            for (int i = 2; i < top.childCount; i++) top.GetChild(i).gameObject.SetActive(i == 2);
            var contentPanel = transform.Find("Content");
            for (int i = 0; i < contentPanel.childCount; i++) contentPanel.GetChild(i).gameObject.SetActive(i == 1);
            var subjectsPanel = contentPanel.Find("Subjects").GetComponent<ScrollRect>().content;
            for (int i = 1; i < subjectsPanel.childCount; i++) Destroy(subjectsPanel.GetChild(i).gameObject);

            var generalAverage = 0F;
            var generalCoef = 0F;
            foreach (var subject in subjects)
            {
                var marks = Marks.marks.Where(m => m.subject == subject && (period.value == 0 || m.period == periods[period.value - 1]));

                var btn = Instantiate(subjectsPanel.GetChild(0).gameObject, subjectsPanel).transform;
                if (marks.Count() > 0) btn.GetComponent<Button>().onClick.AddListener(() => DisplayMarks(marks, subject));
                btn.Find("Name").GetComponent<Text>().text = subject.name;
                btn.Find("Teacher").GetComponent<Text>().text = string.Join("\n", subject.teachers);

                var _marks = marks.Where(m => m.mark != null && !m.notSignificant);
                var average = _marks.Count() == 0 ? (float?)null : _marks.Sum(m => (float)m.mark / m.markOutOf * 20F * m.coef) / _marks.Sum(m => m.coef);
                if (average != null)
                {
                    generalAverage += (float)average * subject.coef;
                    generalCoef += subject.coef;
                }
                btn.Find("Average").GetComponent<Text>().text = average == null ? "" : ((float)average).ToString("0.##") + "<size=12>/20</size>";

                btn.gameObject.SetActive(true);
            }

            transform.Find("Bottom").Find("Average").GetComponent<Text>().text = generalCoef > 0 ? $"Ma moyenne générale: {(generalAverage / generalCoef).ToString("0.##")}<size=12>/20</size>" : "";
        }
    }
}