using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    ScrollRect m_TargetScrollRect;
    void Start() => m_TargetScrollRect = transform.GetComponentInParent<ScrollRect>();

    public void OnBeginDrag(PointerEventData eventData) => m_TargetScrollRect.SendMessage("OnBeginDrag", eventData);
    public void OnDrag(PointerEventData eventData) => m_TargetScrollRect.SendMessage("OnDrag", eventData);
    public void OnEndDrag(PointerEventData eventData) => m_TargetScrollRect.SendMessage("OnEndDrag", eventData);
    public void OnScroll(PointerEventData data) => m_TargetScrollRect.SendMessage("OnScroll", data);
}
