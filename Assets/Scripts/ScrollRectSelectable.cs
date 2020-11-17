using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectSelectable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    ScrollRect m_TargetScrollRect;
    void Start() => m_TargetScrollRect = transform.GetComponentInParent<ScrollRect>();

    //Drag
    public bool Drag = true;
    public void OnBeginDrag(PointerEventData eventData) { if (Drag) m_TargetScrollRect.SendMessage("OnBeginDrag", eventData); }
    public void OnDrag(PointerEventData eventData) { if (Drag) m_TargetScrollRect.SendMessage("OnDrag", eventData); }
    public void OnEndDrag(PointerEventData eventData) { if (Drag) m_TargetScrollRect.SendMessage("OnEndDrag", eventData); }

    //Scroll
    public void OnScroll(PointerEventData data) => m_TargetScrollRect.SendMessage("OnScroll", data);
}
