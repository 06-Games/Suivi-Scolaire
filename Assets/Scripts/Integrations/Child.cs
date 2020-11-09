using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Integrations.Data
{

    public class Child
    {
        internal static string GetImage(Sprite sprite) => sprite == null ? null : Convert.ToBase64String(sprite.texture.EncodeToPNG());
        internal static Sprite SetImage(string value)
        {
            if (value == null) return null;
            var tex = new Texture2D(1, 1);
            tex.LoadImage(Convert.FromBase64String(value));
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
        }

        //General informations
        public string id;
        public string name;
        [XmlIgnore] public Sprite sprite;
        public string image { get => GetImage(sprite); set => sprite = SetImage(value); }
        public List<string> modules;
        [XmlIgnore] public Dictionary<string, string> extraData;
        [XmlArrayItem("Pair")]
        public SerializableKeyValue<string, string>[] extraDatas
        {
            get => extraData?.Select(v => new SerializableKeyValue<string, string>(v)).ToArray();
            set => extraData = value.ToDictionary(v => v.key, v => v.value);
        }
        public string GetExtraData(string key) {
                if (extraData == null) return "";
                return extraData.TryGetValue(key, out var v) ? v : "";
        }

        public List<Period> Periods;
        public List<Subject> Subjects;

        //Other information
        public List<Trimester> Trimesters;
        public List<Mark> Marks;
        public List<Homework> Homeworks;
        public List<ScheduledEvent> Schedule;
        public List<Message> Messages;
        public List<Book> Books;
        public Folder Documents;
    }


    #region General informations
    public class Period
    {
        public string name;
        public DateTime start;
        public DateTime end;
        public bool holiday;
    }
    public class Subject
    {
        public string id;
        public string name;
        public float coef;
        public string[] teachers;
    }
    #endregion

    #region Other information
    public class Mark
    {
        //Date
        public string trimesterID;
        public DateTime date;
        public DateTime dateAdded;

        //Infos
        public string subjectID;
        [XmlIgnore] public Subject subject => Manager.Child.Subjects.FirstOrDefault(s => s.id == subjectID);
        public string name;
        public float coef;
        public float? mark;
        public float markOutOf;
        public Skill[] skills;
        public float? classAverage;
        public bool notSignificant;

        public class Skill
        {
            public uint? id;
            public string name;
            public uint? value;
            public uint categoryID;
            public string categoryName;
        }
    }
    public class Trimester
    {
        public string id;
        public string name;
        public DateTime start;
        public DateTime end;
    }

    public class Homework
    {
        public string subjectID;
        [XmlIgnore] public Subject subject => Manager.Child.Subjects.FirstOrDefault(s => s.id == subjectID);
        public string periodID;
        public DateTime forThe;
        public DateTime addedThe;
        public string addedBy;
        public string content;
        public bool done;
        public bool exam;
        public List<Request> documents = new List<Request>();

        public class Period
        {
            public string name;
            public string id;
            public TimeRange timeRange;
        }
    }

    public class ScheduledEvent
    {
        public string subjectID;
        [XmlIgnore] public Subject subject => Manager.Child.Subjects.FirstOrDefault(s => s.id == subjectID);
        public string teacher;
        public DateTime start;
        public DateTime end;
        public string room;
        public bool canceled;
    }

    public class Message
    {
        public uint id;
        public string subject;
        public DateTime date;
        public bool read;
        public enum Type { received, sent }
        public Type type;
        public List<string> correspondents;

        public string content;
        public List<Request> documents = new List<Request>();
    }

    public class Book
    {
        public string id;
        public string[] subjectsID;
        [XmlIgnore] public IEnumerable<Subject> subjects => Manager.Child.Subjects.Where(s => subjectsID.Contains(s.id));

        public string name;
        public string editor;
        [XmlIgnore] public Sprite cover;
        public string image { get => Child.GetImage(cover); set => cover = Child.SetImage(value); }
        [XmlIgnore] public IEnumerator url;
    }

    public class Folder
    {
        public string id;
        public string name;
        public List<Folder> folders = new List<Folder>();
        public List<Document> documents = new List<Document>();
    }
    public class Document
    {
        public string id;
        public string name;
        public DateTime? added;
        public uint? size;
        public Request download;
    }
    #endregion

    public class Request
    {
        public string docName;
        public string url;
        [XmlIgnore] public Func<Dictionary<string, string>> headers;
        public List<SerializableKeyValue<string, string>> requestHeaders
        {
            get => headers?.Invoke().Select(v => new SerializableKeyValue<string, string>(v)).ToList();
            set => headers = () => value.ToDictionary(v => v.key, v => v.value);
        }
        public enum Method { Get, Post }
        public Method method;
        [XmlIgnore] public Func<WWWForm> postData;
        public List<SerializableKeyValue<string, string>> formData
        {
            get => postData?.Invoke().headers.Select(v => new SerializableKeyValue<string, string>(v)).ToList();
            set
            {
                var form = new WWWForm();
                foreach (var data in value.ToDictionary(v => v.key, v => v.value)) form.AddField(data.Key, data.Value);
                postData = () => form;
            }
        }

        [XmlIgnore]
        public UnityEngine.Networking.UnityWebRequest request
        {
            get
            {
                UnityEngine.Networking.UnityWebRequest webRequest = null;
                if (method == Method.Get) webRequest = UnityEngine.Networking.UnityWebRequest.Get(url);
                else if (method == Method.Post) webRequest = UnityEngine.Networking.UnityWebRequest.Post(url, postData?.Invoke() ?? new WWWForm());
                if (headers != null) foreach (var header in headers.Invoke()) webRequest?.SetRequestHeader(header.Key, header.Value);
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
            if (request.error == null) Manager.HideLoadingPanel();
            else { Manager.FatalErrorDuringLoading("Error downloading file", request.error); yield break; }


#if UNITY_ANDROID && !UNITY_EDITOR
            var path = "/storage/emulated/0/Download/Suivi-Scolaire/";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            File.WriteAllBytes(path + docName, _request.downloadHandler.data);
            UnityAndroidOpenUrl.AndroidOpenUrl.OpenFile(path + docName);
#else
            var path = Application.temporaryCachePath + docName;
            File.WriteAllBytes(path, _request.downloadHandler.data);

#if UNITY_IOS
            drstc.DocumentHandler.DocumentHandler.OpenDocument(path);
#else
            Application.OpenURL(path);
#if !UNITY_STANDALONE && !UNITY_EDITOR
            Debug.LogWarning($"Unsupported plateform ({Application.platform}), we are unable to certify that the opening worked. The file has been saved at \"{path}\"");
#endif
#endif
#endif
        }
    }
    public class SerializableKeyValue<T1, T2>
    {
        [XmlAttribute] public T1 key;
        [XmlText] public T2 value;
        public SerializableKeyValue() { }
        public SerializableKeyValue(KeyValuePair<T1, T2> pair) { key = pair.Key; value = pair.Value; }
        public KeyValuePair<T1, T2> ToKeyValue() => new KeyValuePair<T1, T2>(key, value);
        public override string ToString() => $"[{key}, {value}]";
    }
}
