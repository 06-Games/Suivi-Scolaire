using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Language/Dropdown")]
public class DropdownTranslation : MonoBehaviour
{
    public string[] id;
    void Start()
    {
        var options = GetComponent<Dropdown>().options;
        for (int i = 0; i < options.Capacity & i < id.Length; i++) options[i].text = LangueAPI.Get(id[i], options[i].text);
        GetComponent<Dropdown>().options = options;
    }
}
