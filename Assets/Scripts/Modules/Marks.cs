using Integrations;
using Integrations.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Modules
{
    public class Marks : MonoBehaviour, Module
    {
        public void Reset() { /* There is nothing to reset */ }
        public void Reload()
        {
            if (!Manager.isReady || !Manager.provider.TryGetModule(out Integrations.Marks module)) { gameObject.SetActive(false); return; }
            StartCoroutine(module.GetMarks(() => Initialise()));
        }
        public void OnEnable()
        {
            if (!Manager.isReady) return;
            if (Manager.Data.ActiveChild.Marks == null || Manager.Data.ActiveChild.Marks.Count == 0) Reload();
            else Initialise();
            Manager.OpenModule(gameObject);
        }
        void Initialise()
        {
            ProviderExtension.GenerateSubjectColors();

            period.onValueChanged.RemoveAllListeners();
            period.ClearOptions();
            period.AddOptions(new List<string> { LangueAPI.Get("marks.displayedPeriod.all", "All") });
            period.AddOptions(Manager.Data.ActiveChild.Trimesters.Select(p => p.name).ToList());
            period.value = Manager.Data.ActiveChild.Trimesters.IndexOf(Manager.Data.ActiveChild.Trimesters.FirstOrDefault(p => p.start <= System.DateTime.Now && p.end >= System.DateTime.Now)) + 1;
            Refresh();
        }

        public Dropdown period;
        public void Refresh()
        {
            //Variables
            var trimester = period.value == 0 ? null : Manager.Data.ActiveChild.Trimesters[period.value - 1];
            var marks = Manager.Data.ActiveChild.Marks.Where(m => trimester == null || m.trimesterID == trimester.id);
            var subjects = Manager.Data.ActiveChild.Subjects.OrderBy(s => s.name);

            //Average variables
            var subjectSum = 0F;
            var subjectCoefSum = 0F;
            var subjectClassSum = 0F;
            var subjectClassCoefSum = 0F;

            var content = transform.Find("Content").GetComponent<ScrollRect>().content;
            for (int i = 1; i < content.childCount; i++) Destroy(content.GetChild(i).gameObject);
            foreach (var subject in subjects)
            {
                //Variables
                var subjectColor = subject?.color ?? new Color();
                var subjectGo = Instantiate(content.GetChild(0).gameObject, content).transform;
                var panel = subjectGo.Find("Panel");

                //Marks
                var subjectMarks = marks.Where(m => m.subjectID == subject.id);
                var markSum = 0F;
                var coefSum = 0F;
                var classSum = 0F;
                var classCoefSum = 0F;
                foreach (var mark in subjectMarks)
                {
                    //Variables to calculate the average
                    if (!mark.absent && !mark.notSignificant)
                    {
                        markSum += mark.mark.GetMark / mark.mark.markOutOf * mark.coef;
                        coefSum += mark.coef;
                    }
                    if (mark.classAverage != null && !mark.notSignificant)
                    {
                        classSum += mark.classAverage.GetMark / mark.mark.markOutOf * mark.coef;
                        classCoefSum += mark.coef;
                    }

                    var markGo = Instantiate(panel.GetChild(0).gameObject, panel).transform;

                    //Color
                    SetColor(markGo.GetComponent<Image>(), subjectColor);
                    SetColor(markGo.Find("Tint").GetComponentInChildren<Image>(), subjectColor);

                    //Texts
                    markGo.Find("Name").GetComponent<Text>().text = $"{mark.name} <size=12><color=grey>({mark.date.ToString("dd/MM/yyyy")})</color></size>";
                    markGo.Find("Class Value").GetComponent<TMPro.TextMeshProUGUI>().text = LangueAPI.Get("marks.class", "Classe: [0]", DisplayMark(mark.classAverage));
                    markGo.Find("Value").GetComponent<TMPro.TextMeshProUGUI>().text = DisplayMark(mark.mark, mark);
                    markGo.Find("Coef").GetComponent<TMPro.TextMeshProUGUI>().text = LangueAPI.Get("marks.coef", "Coef [0]", mark.coef);

                    markGo.gameObject.SetActive(true);
                }

                //Average
                var average = markSum / coefSum * 20F;
                var classAverage = classSum / coefSum * 20F;
                if (!float.IsNaN(average))
                {
                    subjectSum += average * subject.coef;
                    subjectCoefSum += subject.coef;
                }
                if (!float.IsNaN(classAverage))
                {
                    subjectClassSum += classAverage * subject.coef;
                    subjectClassCoefSum += subject.coef;
                }

                //Header
                var head = subjectGo.Find("Head");
                SetColor(head.GetComponent<Image>(), subjectColor);
                head.Find("Subject").GetComponent<Text>().text = $"{subject.name} <size=8><color=grey>{string.Join(", ", subject.teachers)} (coef {subject.coef})</color></size>";
                head.Find("Average").GetComponentInChildren<Text>().text = float.IsNaN(average) && float.IsNaN(classAverage) ? "" : $"{average.ToString("0.00")} <size=12><color=grey>{classAverage.ToString("0.00")}</color></size>";

                subjectGo.gameObject.SetActive(true);
            }

            //Footer
            var overallAverage = subjectSum / subjectCoefSum;
            var overallClassAverage = subjectClassSum / subjectClassCoefSum;

            var footer = transform.Find("Bottom");
            footer.Find("Average").GetComponent<Text>().text = $"Ma moyenne générale: {overallAverage.ToString("0.00")}<size=12>/20</size>";
            footer.Find("Class Average").GetComponent<Text>().text = $"Moyenne générale de la classe: {overallClassAverage.ToString("0.00")}<size=12>/20</size>";
        }
        void SetColor(Image img, Color color) => img.color = new Color(color.r, color.g, color.b, img.color.a);
        public static string DisplayMark(Mark m, Mark.MarkData value)
        {
            if (m.absent) return $"<color=#aaa>{LangueAPI.Get("marks.absent", "Abs")}</color>";
            else if (value == null) return null;

            var txt = "";
            if (value.mark != -1) txt += $"{value.mark.ToString("0.00")}<size=13>/{value.markOutOf}</size>";
            if (value.skills != null && value.skills.Length > 0)
            {
                if (txt != "") txt += " ";
                txt += string.Join(" ", value.skills.Select(s => $"<sprite index={s.value}>"));
            }
            return m.notSignificant ? $"<color=#aaa>({txt})</color>" : txt;
        }
    }
}