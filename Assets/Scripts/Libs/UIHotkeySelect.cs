using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHotkeySelect : MonoBehaviour
{
    public Selectable previous;
    public Selectable next;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Navigate backward when holding shift, else navigate forward.
            HandleHotkeySelect(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EventSystem.current.SetSelectedGameObject(null, null);
        }
    }

    private void HandleHotkeySelect(bool isNavigateBackward)
    {
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null && selectedObject.activeInHierarchy) // Ensure a selection exists and is not an inactive object.
        {
            if (selectedObject == gameObject) StartCoroutine(Select(isNavigateBackward ? previous : next));
        }
        else SelectDefault();
    }

    void SelectDefault() => StartCoroutine(Select(Selectable.allSelectablesArray.OrderBy(s => Screen.width * (Screen.height - s.transform.position.y) + s.transform.position.x).FirstOrDefault()));

    IEnumerator Select(Selectable obj)
    {
        yield return 0;
        obj.Select();
    }
}