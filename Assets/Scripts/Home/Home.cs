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
        internal static List<Period> periods;
        public void Reset() => periods = null;
        public void OnEnable()
        {
            StartCoroutine(enumerator());

            IEnumerator enumerator()
            {
                if (!Manager.isReady) { gameObject.SetActive(false); yield break; }
                if (periods == null && Manager.provider.TryGetModule(out Integrations.Periods hM)) yield return hM.GetPeriods((p) => periods = p.OrderBy(d => d.start).ToList());
                if (Marks.Marks.marks == null && Manager.provider.TryGetModule(out Integrations.Marks mM)) yield return mM.GetMarks(Marks.Marks.Initialise);
                Refresh();
            }
        }

        private void Awake() { transform.Find("Content").gameObject.SetActive(false); }

        public void Refresh()
        {
            var Content = transform.Find("Content");

            var period = Content.Find("Period");
            if (periods?.Count > 0)
            {
                var actualPeriod = periods.FirstOrDefault(p => p.start <= DateTime.Now && p.end >= DateTime.Now);
                period.Find("Img").Find("Image").GetComponent<Image>().sprite = null;
                period.Find("Txt").Find("Period").GetComponent<Text>().text = actualPeriod.name;
                if (actualPeriod.holiday) period.Find("Txt").Find("Desc").GetComponent<Text>().text = LangueAPI.Get("", "Elles se termineront dans [0] jours", (actualPeriod.start - DateTime.Now).ToString("dd"));
                var nextPeriod = periods.FirstOrDefault(h => h.start > DateTime.Now);
                period.Find("Txt").Find("Desc").GetComponent<Text>().text = LangueAPI.Get(
                    actualPeriod.holiday ? "home.holidays.end" : "home.holidays.start",
                    actualPeriod.holiday ? "It ends in <b>[1]</b> days" : "<i>[0]</i>\nstart in <b>[1]</b> days",
                    nextPeriod?.name.ToLower(),
                    nextPeriod == null ? "0" : (nextPeriod.start - DateTime.Now).ToString("dd")
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

            Content.gameObject.SetActive(true);
        }
    }
}
