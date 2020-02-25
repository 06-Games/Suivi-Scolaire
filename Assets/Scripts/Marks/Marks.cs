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

        public void Initialise(List<Period> _periods, List<Subject> _subjects, List<Mark> _marks)
        {
            periods = _periods;
            subjects = _subjects;
            marks = _marks;

            period.ClearOptions();
            period.AddOptions(new List<string>() { "Tout" });
            period.AddOptions(periods.Select(p => p.name).ToList());
            period.value = periods.IndexOf(periods.FirstOrDefault(p => p.start <= System.DateTime.Now && p.end >= System.DateTime.Now)) + 1;

            Refresh();
            Manager.OpenModule(gameObject);
        }

        public Dropdown groupBy;
        public Dropdown period;

        public void Refresh()
        {
            var contentPanel = transform.Find("Content");
            for (int i = 0; i < contentPanel.childCount; i++) contentPanel.GetChild(i).gameObject.SetActive(groupBy.value == i);

            if (groupBy.value == 0)
            {
                var subjectsPanel = contentPanel.Find("By Nothing").GetComponent<ScrollRect>().content;
                for (int i = 1; i < subjectsPanel.childCount; i++) Destroy(subjectsPanel.GetChild(i).gameObject);

                var average = new Dictionary<string, float>();
                var coef = new Dictionary<string, float>();
                var subCoef = new Dictionary<string, float>();
                foreach (var subject in subjects) { average.Add(subject.id, 0); coef.Add(subject.id, 0); subCoef.Add(subject.id, subject.coef); }
                foreach (var m in marks.Where(m => period.value == 0 || m.period == periods[period.value - 1]))
                {
                    var btn = Instantiate(subjectsPanel.GetChild(0).gameObject, subjectsPanel).transform;
                    btn.GetComponent<Text>().text = m.mark == null ? "Abs" : (m.notSignificant ? $"<color=grey>({m.mark}<size=12>/{m.markOutOf}</size>)</color>" : $"{m.mark}<size=12>/{m.markOutOf}</size>");
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
                transform.Find("Bottom").Find("Average").GetComponent<Text>().text = generalCoef > 0 ? $"Ma moyenne générale: {(generalAverage / generalCoef).ToString("0.##")}<size=12>/20</size>" : "";
            }
            else if (groupBy.value == 1)
            {
                var bySubPanel = contentPanel.Find("By Subject");
                for (int i = 0; i < bySubPanel.childCount; i++) bySubPanel.GetChild(i).gameObject.SetActive(i == 0);
                var subjectsPanel = bySubPanel.Find("Subjects Grid").GetComponent<ScrollRect>().content;
                for (int i = 1; i < subjectsPanel.childCount; i++) Destroy(subjectsPanel.GetChild(i).gameObject);

                var generalAverage = 0F;
                var generalCoef = 0F;
                foreach (var subject in subjects)
                {
                    var marks = Marks.marks.Where(m => m.subject == subject && (period.value == 0 || m.period == periods[period.value - 1]));

                    var btn = Instantiate(subjectsPanel.GetChild(0).gameObject, subjectsPanel).transform;
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
}