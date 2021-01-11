using Integrations;
using Integrations.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Modules
{
    public class Homeworks : MonoBehaviour, Module
    {
        readonly Color32 colorExam = new Color32(200, 000, 030, 255);
        readonly Color32 colorToDo = new Color32(175, 175, 175, 255);
        readonly Color32 colorDone = new Color32(050, 050, 050, 255);


        List<Homework.Period> periods = new List<Homework.Period>();
        IEnumerator<Homework.Period> periodsMethod;
        int periodIndex;
        public void Reset()
        {
            periods = new List<Homework.Period>();
            periodsMethod = null;
            periodIndex = 0;
        }
        public void Reload()
        {
            if (!Manager.provider.TryGetModule(out Integrations.Homeworks module)) { gameObject.SetActive(false); return; }
            var period = periods.ElementAtOrDefault(periodIndex);
            StartCoroutine(module.GetHomeworks(period, () => Refresh(Manager.Data.ActiveChild.Homeworks.Where(h => period.timeRange.Contains(h.forThe)).OrderBy(h => h.forThe), period)));
        }

        public void OnEnable()
        {
            if (!Manager.isReady) { gameObject.SetActive(false); return; }
            if (periodIndex == 0) Initialise();
            else Manager.OpenModule(gameObject);
        }
        public void Initialise()
        {
            if (!Manager.provider.TryGetModule(out Integrations.Homeworks module)) { gameObject.SetActive(false); return; }

            if (periodsMethod == null) periodsMethod = module.DiaryPeriods();
            var period = periods.ElementAtOrDefault(periodIndex);
            if (period != null) Void();
            else if (periodIndex >= 0) LoadNext((p) => { period = p; Void(); });


            void Void()
            {
                Action<List<Homework>> action = (homeworks) =>
                {
                    ProviderExtension.GenerateSubjectColors();
                    Refresh(homeworks.OrderBy(h => h.forThe), period);
                    Manager.OpenModule(gameObject);
                };
                List<Homework> GetHomeworks() => Manager.Data.ActiveChild.Homeworks?.Where(h => h.periodID != null ? h.periodID == period.id : period.timeRange.Contains(h.forThe)).ToList();

                var _homeworks = GetHomeworks();
                if (_homeworks?.Count > 0) action(_homeworks);
                else StartCoroutine(module.GetHomeworks(period, () => action(GetHomeworks())));


            }
        }
        bool LoadNext(Action<Homework.Period> onComplete = null)
        {
            if (!periodsMethod.MoveNext()) return false;
            UnityThread.executeCoroutine(enumerator());
            return true;
            IEnumerator enumerator()
            {
                var value = periodsMethod.Current;
                while (value == null)
                {
                    yield return new WaitForEndOfFrame();
                    if (!periodsMethod.MoveNext()) break;
                    value = periodsMethod.Current;
                }
                periods.Add(value);
                onComplete?.Invoke(value);
            }
        }
        void Refresh(IEnumerable<Homework> homeworks, Homework.Period period)
        {
            var WeekSwitcher = transform.Find("Top").Find("Week");
            WeekSwitcher.Find("Text").GetComponent<Text>().text = period.name;
            WeekSwitcher.Find("Previous").GetComponent<Button>().interactable = periods.Count > periodIndex + 1 || LoadNext();
            WeekSwitcher.Find("Next").GetComponent<Button>().interactable = periodIndex > 0;

            var Content = transform.Find("Content").GetComponent<ScrollRect>().content;
            for (int i = 1; i < Content.childCount; i++) Destroy(Content.GetChild(i).gameObject);

            transform.Find("Content").Find("Empty").gameObject.SetActive(!homeworks.Any());
            if (!homeworks.Any()) return;

            var language = LangueAPI.Culture;
            foreach (var Homeworks in homeworks.GroupBy(h => h.forThe))
            {
                var datePanel = Instantiate(Content.GetChild(0).gameObject, Content).transform;
                datePanel.Find("Head").Find("Date").GetComponent<Text>().text = Homeworks.Key.ToString("D", language);

                var panel = datePanel.Find("Panel");
                for (int i = 1; i < panel.childCount; i++) Destroy(panel.GetChild(i).gameObject);
                foreach (var homework in Homeworks)
                {
                    var go = Instantiate(panel.GetChild(0).gameObject, panel).transform;

                    // Set to subect color
                    SetColor(go.GetComponent<Image>(), homework.subject?.color ?? new Color());
                    SetColor(go.Find("Tint").GetComponentInChildren<Image>(), homework.subject?.color ?? new Color());
                    var goContent = go.Find("Content");

                    // Infos
                    var infos = goContent.Find("Infos");
                    infos.Find("Subject").GetComponent<Text>().text = homework.subject?.name;
                    infos.Find("Extra").GetComponent<Text>().text = LangueAPI.Get("homeworks.added", "Added on [0] by [1]", homework.addedThe.ToString("dd/MM"), homework.addedBy);
                    var docs = infos.Find("Docs");
                    foreach (var doc in homework.documents)
                    {
                        var docGo = Instantiate(docs.GetChild(0).gameObject, docs).transform;
                        docGo.GetComponent<Text>().text = $"• {doc.name}";
                        docGo.GetComponent<Button>().onClick.AddListener(() => UnityThread.executeCoroutine(Manager.provider.GetModule<Integrations.Homeworks>().OpenHomeworkAttachment(doc)));
                        docGo.gameObject.SetActive(true);
                    }

                    // Content
                    goContent.Find("Content").GetComponent<TMPro.TMP_InputField>().text = homework.content;

                    // Indicator
                    var indicator = go.Find("Indicator");
                    SetIndicator();
                    if (!homework.exam) indicator.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        homework.done = !homework.done;
                        UnityThread.executeCoroutine(Manager.provider.GetModule<Integrations.Homeworks>().HomeworkDoneStatus(homework));
                        SetIndicator();
                    });
                    void SetIndicator() => indicator.GetComponentInChildren<Image>().color = homework.exam ? colorExam : (homework.done ? colorDone : colorToDo);

                    go.gameObject.SetActive(true);
                }
                panel.gameObject.SetActive(false);
                datePanel.gameObject.SetActive(true);
            }
        }
        public static void SetColor(Image img, Color color) => img.color = new Color(color.r, color.g, color.b, img.color.a);

        public void ChangePeriod(bool next)
        {
            periodIndex += next ? -1 : 1;
            Initialise();
        }

        public void ShowHide(Transform go)
        {
            var panel = go.Find("Panel").gameObject;
            panel.SetActive(!panel.activeInHierarchy);
            go.Find("Head").Find("Arrow").transform.rotation = Quaternion.Euler(0, 0, panel.activeInHierarchy ? 0 : 180);
        }
    }
}
