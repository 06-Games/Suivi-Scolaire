﻿using FileFormat;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UpdateManager : MonoBehaviour
{
    private void Awake()
    {
#if !UNITY_EDITOR && !UNITY_STANDALONE
        gameObject.SetActive(false);
#endif
        for (int i = 1; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
    }
    void Start() => StartCoroutine(CheckUpdate());

    JSON lastestRelease;
    IEnumerator CheckUpdate()
    {
        yield return new WaitForSeconds(5);

        var request = UnityWebRequest.Get("https://api.github.com/repos/06-Games/Suivi-Scolaire/releases/latest");
        yield return request.SendWebRequest();
        lastestRelease = new JSON(request.downloadHandler.text);

        var version = lastestRelease.Value<string>("tag_name");
        if (version != Application.version)
        {
            var popup = transform.Find("Popup");
            popup.Find("Text").Find("Versions").GetComponent<Text>().text = $"{Application.version} -> {version}";
            popup.GetComponent<SimpleSideMenu>().Open();
        }
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
#else
        Application.OpenURL(lastestRelease.Value<string>("html_url"));
#endif
    }

    IEnumerator DownloadUpdate(Transform installPanel)
    {
        var ext = ".exe";
        var url = lastestRelease.jToken.SelectToken("assets").FirstOrDefault(a => a.Value<string>("name").EndsWith(ext))?.Value<string>("browser_download_url");
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