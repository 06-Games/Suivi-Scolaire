using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Language/Text")]
public class TextTranslation : MonoBehaviour
{
    public string id;
    public string[] arg = new string[0];

    void Start() { GetComponent<Text>().text = LangueAPI.Get(id, GetComponent<Text>().text, arg); }
}
