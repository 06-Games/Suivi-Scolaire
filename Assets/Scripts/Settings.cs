using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public Transform Content;

    public Vector2 refResolution = new Vector2(1366, 768);

    private void Start() => Refresh();
    public void Refresh()
    {
        LanguageChanged(PlayerPrefs.GetString("settings.language", Application.systemLanguage.ToString()));

        var size = Content.Find("Size").GetComponentInChildren<Slider>();
        size.value = PlayerPrefs.GetInt("settings.size", 300 / Screen.dpi < 300 ? 2 : 3);
        if (!Content.gameObject.activeInHierarchy) ChangeSize(size);

        Content.Find("App Infos").GetComponent<Text>().text = LangueAPI.Get(
            "settings.infos", "[0] (v[1])\nBuild: [2] ([3])",
            Application.productName, Application.version, //First line
            string.IsNullOrEmpty(Application.buildGUID) ? "<i>Editor</i>" : Application.buildGUID, Application.unityVersion //Second line
        );

#if !UNITY_STANDALONE || !UNITY_EDITOR
        Content.Find("Logs").gameObject.SetActive(false);
#endif
    }

    public void LanguageChanged(string language)
    {
        foreach (Transform btn in Content.Find("Language").Find("List")) btn.GetComponent<Button>().interactable = btn.name != language;
        if (PlayerPrefs.GetString("settings.language") == language) return;

        PlayerPrefs.SetString("settings.language", language);
        LangueAPI.ReloadData();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void SizeChanged(Slider slider)
    {
        StartCoroutine(enumerator());
        IEnumerator enumerator()
        {
            var value = slider.value;
            yield return new WaitForSeconds(0.1F);
            if (value == slider.value) ChangeSize(slider);
        }
    }
    void ChangeSize(Slider slider)
    {
        slider.handleRect.GetComponentInChildren<Text>().text = slider.value.ToString();
        PlayerPrefs.SetInt("settings.size", (int)slider.value);

        var factor = 1.6F - 0.3F * (slider.value - 1);
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var component = root.GetComponent<CanvasScaler>();
            if (component != null) component.referenceResolution = refResolution * factor;
        }
    }

    public void OpenLogs() => Application.OpenURL("file://" + Application.persistentDataPath + "/logs/");
}
