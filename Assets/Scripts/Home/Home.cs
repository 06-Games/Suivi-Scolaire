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
            if (!Manager.isReady) { gameObject.SetActive(false); return; }
            if (holidays == null) StartCoroutine(Manager.provider.GetHolidays(Initialise));
            else Refresh();
        }
        public void Initialise(List<Holiday> _holidays)
        {
            holidays = _holidays;
            Refresh();
        }

        private void Awake()
        {
            transform.Find("Content").gameObject.SetActive(false);
        }

        public void Refresh()
        {
            var Content = transform.Find("Content");

            var nextHoliday = holidays.LastOrDefault(h => h.start >= System.DateTime.Now);
            Content.Find("Holidays").GetComponent<Text>().text = $"Vos prochaines vacances sont les <i>{nextHoliday.name}</i>, elles commenceront dans <b>{(nextHoliday.start - System.DateTime.Now).ToString("dd")}</b> jours";

            Content.gameObject.SetActive(true);
        }
    }
}
