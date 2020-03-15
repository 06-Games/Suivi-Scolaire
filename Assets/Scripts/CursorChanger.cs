using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CursorChanger : MonoBehaviour
{
    public Texture2D hoverCursor;
    void Start()
    {
        foreach (var element in Resources.FindObjectsOfTypeAll<Selectable>())
        {
            EventTrigger trigger = element.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { Cursor.SetCursor(hoverCursor, new Vector2(hoverCursor.width / 2F, 0), CursorMode.Auto); });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); });
            trigger.triggers.Add(entryExit);
        }
    }
}
