using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class PullDownToRefresh : MonoBehaviour, IEndDragHandler, IDragHandler
{
    //Parameters
    [Range(0, 300)] float PullPower = 150; //Minimum y position to call the event
    [Range(0, 300)] float MinYPos = 50; //Minimum y position for indicator display
    public UnityEngine.Events.UnityEvent Pulled;

    //Private variables
    ScrollRect ScrollRect;
    Image Indicator;

    void Start()
    {
        ScrollRect = GetComponent<ScrollRect>();
        Indicator = Instantiate(Resources.Load<GameObject>("Prefabs/Pull Indicator"), ScrollRect.viewport).GetComponent<RectTransform>().GetChild(0).GetComponent<Image>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        var topPos = ScrollRect.content.anchoredPosition.y * -1;
        if (topPos < MinYPos) { SetA(Indicator, 0); return; }

        var state = topPos / PullPower;
        if (state > 1) state = 1;
        Indicator.fillAmount = state;

        var a = (topPos - MinYPos) / 20;
        SetA(Indicator, a > 1 ? 1 : (a < 0 ? 0 : a));
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        var topPos = ScrollRect.content.anchoredPosition.y * -1;
        if (topPos > PullPower) Pulled?.Invoke();
        SetA(Indicator, 0);
    }
    void SetA(Image img, float a) { var color = img.color; color.a = a; img.color = color; }
}
