using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UpdateManager : MonoBehaviour
{
    void Awake() { for (int i = 1; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false); }
    void Start() => StartCoroutine(CheckUpdate());

    List<JToken> releases;
    JToken lastestRelease;
    IEnumerator CheckUpdate()
    {
        yield return new WaitForSeconds(5);

        var request = UnityWebRequest.Get("https://api.github.com/repos/06-Games/Suivi-Scolaire/releases");
        yield return request.SendWebRequest();
        if (request.isNetworkError) yield break;
        releases = JArray.Parse(request.downloadHandler.text).OrderByDescending(v => v.Value<string>("published_at")).ToList();

        lastestRelease = releases.FirstOrDefault();
        var popup = transform.Find("Popup");
        var version = lastestRelease.Value<string>("tag_name");
        popup.Find("Text").Find("Infos").Find("Versions").GetComponent<Text>().text = $"{Application.version} -> {version}";
#if (UNITY_STANDALONE || UNITY_ANDROID) && !UNITY_EDITOR
        if (version != Application.version) popup.GetComponent<SimpleSideMenu>().Open();
#endif
    }

    public void SeeChangelog()
    {
        if (releases == null) return;
        var panel = transform.Find("Changelog");
        panel.Find("Top").Find("Update").gameObject.SetActive(lastestRelease.Value<string>("tag_name") != Application.version);
        var content = panel.Find("Content").GetComponent<ScrollRect>().content;
        for (int i = 1; i < content.childCount; i++) Destroy(content.GetChild(i).gameObject);

        var culture = LangueAPI.Culture;
        foreach (var release in releases)
        {
            var go = Instantiate(content.GetChild(0).gameObject, content).transform;
            go.Find("Version").GetComponent<Text>().text = $"{release.Value<string>("tag_name")} <size=20><color=grey>({release.Value<System.DateTime>("published_at").ToString("d", culture)})</color></size>";
            go.Find("Body").GetComponent<TMPro.TextMeshProUGUI>().text = Integrations.Renderer.Markdown.ToRichText(release.Value<string>("body"));
            go.gameObject.SetActive(true);
        }
        panel.gameObject.SetActive(true);
    }

    public void UpdateApp()
    {
        var popup = transform.Find("Popup").GetComponent<SimpleSideMenu>();
        var speed = popup.transitionSpeed;
        popup.transitionSpeed = 0;
        popup.Close();
        popup.transitionSpeed = speed;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        var install = transform.Find("Install");
        install.gameObject.SetActive(true);
        StartCoroutine(DownloadUpdate(install.Find("Panel")));
#elif UNITY_ANDROID
        Application.OpenURL("http://play.google.com/store/apps/details?id=com.fr_06Games.SuiviScolaire");
#else
        Application.OpenURL(lastestRelease.Value<string>("html_url"));
#endif
    }
    IEnumerator DownloadUpdate(Transform installPanel)
    {
        var ext = ".exe";
        var url = lastestRelease.SelectToken("assets").FirstOrDefault(a => a.Value<string>("name").EndsWith(ext))?.Value<string>("browser_download_url");
        if (string.IsNullOrEmpty(url)) { Debug.LogError("No download link"); yield break; }

        var progressBar = installPanel.Find("Progress").GetComponent<Scrollbar>();
        var progressText = progressBar.handleRect.GetChild(0).GetComponent<Text>();
        var progressData = installPanel.Find("Text").Find("Progress").GetComponent<Text>();
        progressBar.size = 0;
        if (progressBar.TryGetComponent<UnityEngine.EventSystems.EventTrigger>(out var trigger)) Destroy(trigger);
        progressText.text = "0%";
        progressData.text = "";
        var pow = Mathf.Pow(10, 6);

        var request = UnityWebRequest.Get(url);
        request.SendWebRequest();
        while (!request.isDone)
        {
            progressBar.size = request.downloadProgress;
            progressText.text = $"{(request.downloadProgress * 100).ToString("0")}%";
            progressData.text = $"{(request.downloadedBytes / pow).ToString("0.00")} MB / {(request.downloadedBytes / request.downloadProgress / pow).ToString("0.00")} MB";
            yield return new WaitForEndOfFrame();
        }
        var path = Application.temporaryCachePath + "/installer" + ext;
        File.WriteAllBytes(path, request.downloadHandler.data);

        System.Diagnostics.Process.Start(path);
        Application.Quit();
    }
}
