using Integrations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Home
{
    public class Home : MonoBehaviour, Module
    {
        internal static List<Holiday> holidays;
        public void Reset() => holidays = null;
        public void OnEnable()
        {
            StartCoroutine(enumerator());

            IEnumerator enumerator()
            {
                if (!Manager.isReady) { gameObject.SetActive(false); yield break; }
                if (holidays == null && Manager.provider.TryGetModule(out Integrations.Home hM)) yield return hM.GetHolidays((h) => holidays = h);
                if (Marks.Marks.marks == null && Manager.provider.TryGetModule(out Integrations.Marks mM)) yield return mM.GetMarks(Marks.Marks.Initialise);
                Refresh();
            }
        }

        private void Awake() { transform.Find("Content").gameObject.SetActive(false); }

        public void Refresh()
        {
            var Content = transform.Find("Content");

            var nextHoliday = holidays.LastOrDefault(h => h.start >= System.DateTime.Now);
            Content.Find("Holidays").GetComponent<Text>().text = holidays != null && holidays.Count > 0 ? LangueAPI.Get("home.holidays", "Your next vacation is <i>[0]</i>, it will start in <b>[1]</b> days", nextHoliday?.name, nextHoliday == null ? "0" : (nextHoliday.start - System.DateTime.Now).ToString("dd")) : "";
            var lastMarks = Content.Find("Marks");
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

            Content.gameObject.SetActive(true);
        }
    }
}
