using System.Collections;
using UnityEngine;

public class Menu : MonoBehaviour
{
    RectTransform Panel;
    Vector2 InitialPos;
    float SecondsPerPixel;

    void Awake()
    {
        Panel = (RectTransform)transform.Find("Panel");
        InitialPos = Panel.anchoredPosition;
        Panel.anchoredPosition = new Vector2(-InitialPos.x, InitialPos.y);
        SecondsPerPixel = 0.25F / (InitialPos.x * 2);

        if (TryGetComponent<UnityEngine.EventSystems.EventTrigger>(out var eT)) Destroy(eT);
        if (Panel.TryGetComponent(out eT)) Destroy(eT);
    }

    IEnumerator Mvt(Vector2 objective, System.Action onComplete = null)
    {
        float dist = objective.x - Panel.anchoredPosition.x;
        float time = Mathf.Abs(dist) * SecondsPerPixel;
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        var initial = Panel.anchoredPosition.x;
        while (sw.Elapsed.TotalSeconds < time)
        {
            Panel.anchoredPosition = new Vector2(initial + (dist / time * (float)sw.Elapsed.TotalSeconds), Panel.anchoredPosition.y);
            yield return new WaitForEndOfFrame();
        }
        sw.Stop();
        Panel.anchoredPosition = objective;
        onComplete?.Invoke();
    }

    void OnEnable() { StartCoroutine(Mvt(InitialPos)); }
    public void Hide() { StartCoroutine(Mvt(new Vector2(-InitialPos.x, InitialPos.y), () => gameObject.SetActive(false))); }
}
