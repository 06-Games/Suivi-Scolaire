using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Homeworks
{
    public class Homeworks : MonoBehaviour
    {
        internal static List<Homework> homeworks;
        public void OnEnable()
        {
            StartCoroutine(CheckOriantation());
            if (!Manager.isReady) { gameObject.SetActive(false); return; }
            if (homeworks == null) StartCoroutine(Manager.provider.GetHomeworks(Initialise));
            else Manager.OpenModule(gameObject);
        }
        public void Initialise(IEnumerable<Homework> _homeworks)
        {
            homeworks = _homeworks.OrderBy(h => h.forThe).ToList();

            var Content = transform.Find("Content").GetComponent<ScrollRect>().content;
            for (int i = 1; i < Content.childCount; i++) Destroy(Content.GetChild(i).gameObject);
            foreach (var Homeworks in homeworks.GroupBy(h => h.forThe))
            {
                var datePanel = Instantiate(Content.GetChild(0).gameObject, Content).transform;
                var language = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.EnglishName.Contains(Application.systemLanguage.ToString()));
                datePanel.Find("Head").Find("Date").GetComponent<Text>().text = Homeworks.Key.ToString("D", language);

                var panel = datePanel.Find("Panel");
                for (int i = 1; i < panel.childCount; i++) Destroy(panel.GetChild(i).gameObject);
                foreach (var homework in Homeworks)
                {
                    var go = Instantiate(panel.GetChild(0).gameObject, panel).transform;
                    go.GetComponent<LayoutSwitcher>().Switch(Screen.width > Screen.height ? LayoutSwitcher.Mode.Horizontal : LayoutSwitcher.Mode.Vertical);

                    var infos = go.Find("Infos");
                    infos.Find("Subject").GetComponent<Text>().text = homework.subject?.name;
                    infos.Find("Extra").GetComponent<Text>().text = $"Ajouté le {homework.addedThe.ToString("dd/MM")} par {homework.addedBy}";

                    go.Find("Content").GetComponent<TMPro.TextMeshProUGUI>().text = homework.content;
                    go.gameObject.SetActive(true);
                }
                panel.gameObject.SetActive(false);
                datePanel.gameObject.SetActive(true);
            }

            Manager.OpenModule(gameObject);
        }

        IEnumerator CheckOriantation()
        {
            var Content = transform.Find("Content").GetComponent<ScrollRect>().content;
            while (true)
            {
                bool paysage = Screen.width > Screen.height;
                for (int i = 1; i < Content.childCount; i++)
                {
                    foreach (var switcher in Content.GetChild(i).Find("Panel").GetComponentsInChildren<LayoutSwitcher>()) 
                        switcher.Switch(Screen.width > Screen.height ? LayoutSwitcher.Mode.Horizontal : LayoutSwitcher.Mode.Vertical);
                }
                yield return new WaitWhile(() => paysage == Screen.width > Screen.height);
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
