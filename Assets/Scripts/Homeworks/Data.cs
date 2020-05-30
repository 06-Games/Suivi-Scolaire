using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DateTime = System.DateTime;

namespace Homeworks
{
    public class Homework
    {
        public Subject subject;
        public DateTime forThe;
        public DateTime addedThe;
        public string addedBy;
        public string content;
        public bool done;
        public bool exam;
        public IEnumerable<Request> documents = new List<Request>();
    }

    public class Period
    {
        public string name;
        public string id;
        public TimeRange timeRange;
    }
}

public class Request
{
    public string docName;
    public string url;
    public Dictionary<string, string> headers;
    public enum Method { Get, Post }
    public Method method;
    public WWWForm postData;

    public UnityEngine.Networking.UnityWebRequest request
    {
        get
        {
            UnityEngine.Networking.UnityWebRequest webRequest = null;
            if (method == Method.Get) webRequest = UnityEngine.Networking.UnityWebRequest.Get(url);
            else if (method == Method.Post) webRequest = UnityEngine.Networking.UnityWebRequest.Post(url, postData ?? new WWWForm());
            if (headers != null) foreach (var header in headers) webRequest?.SetRequestHeader(header.Key, header.Value);
            return webRequest;
        }
    }


    public IEnumerator GetDoc()
    {
        var _request = request;
        _request.SendWebRequest();
        while (!_request.isDone)
        {
            Manager.UpdateLoadingStatus("homeworks.downloading", "Downloading: [0]%", false, (_request.downloadProgress * 100).ToString("0"));
            yield return new WaitForEndOfFrame();
        }
        Manager.HideLoadingPanel();

#if UNITY_STANDALONE
        var path = Application.temporaryCachePath + "/Docs___" + docName;
        File.WriteAllBytes(path, _request.downloadHandler.data);
        Application.OpenURL(path);
#else
        var path = "/storage/emulated/0/Download/Suivi-Scolaire/" + docName;
        if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, request.downloadHandler.data);
        UnityAndroidOpenUrl.AndroidOpenUrl.OpenFile(path);
#endif
    }
}
