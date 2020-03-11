using Integrations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Home
{
    public class Home : MonoBehaviour
    {
        internal static List<Holiday> holidays;
        public void OnEnable()
        {
            if (!Manager.isReady || !Manager.provider.TryGetModule(out Integrations.Home module)) { gameObject.SetActive(false); return; }
            if (holidays == null) StartCoroutine(module.GetHolidays(Initialise));
            else Refresh();
        }
        public void Initialise(List<Holiday> _holidays)
        {
            holidays = _holidays;
            Refresh();
        }

        private void Awake() { transform.Find("Content").gameObject.SetActive(false); }

        public void Refresh()
        {
            var Content = transform.Find("Content");

            var nextHoliday = holidays.LastOrDefault(h => h.start >= System.DateTime.Now);
            Content.Find("Holidays").GetComponent<Text>().text = LangueAPI.Get("home.holidays", "Your next vacation is <i>[0]</i>, it will start in <b>[1]</b> days", nextHoliday?.name, nextHoliday == null ? "0" : (nextHoliday.start - System.DateTime.Now).ToString("dd"));

            Content.gameObject.SetActive(true);
        }
    }
}
