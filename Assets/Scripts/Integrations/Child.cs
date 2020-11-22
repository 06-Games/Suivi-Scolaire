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
        [XmlAttribute] public string id;
        [XmlAttribute] public string name;
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
        public string GetExtraData(string key)
        {
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
        [XmlAttribute] public string name;
        [XmlAttribute] public DateTime start;
        [XmlAttribute] public DateTime end;
        [XmlAttribute] public bool holiday;
    }
    public class Subject
    {
        [XmlAttribute] public string id;
        [XmlAttribute] public string name;
        [XmlAttribute] public float coef;
        [XmlElement("teacher")] public string[] teachers;
    }
    #endregion

    #region Other information
    public class Mark
    {
        //Date
        [XmlAttribute] public string trimesterID;
        [XmlAttribute(DataType = "date")] public DateTime date;
        [XmlAttribute(DataType = "date")] public DateTime dateAdded;

        //Infos
        [XmlAttribute] public string subjectID;
        [XmlIgnore] public Subject subject => Manager.Child.Subjects.FirstOrDefault(s => s.id == subjectID);
        [XmlAttribute] public string name;
        [XmlAttribute] public float coef;
        public float? mark;
        [XmlAttribute] public float markOutOf;
        public Skill[] skills;
        public float? classAverage;
        [XmlAttribute] public bool notSignificant;

        public class Skill
        {
            public uint? id;
            [XmlAttribute] public string name;
            public uint? value;
            [XmlAttribute] public uint categoryID;
            [XmlAttribute] public string categoryName;
        }
    }
    public class Trimester
    {
        [XmlAttribute] public string id;
        [XmlAttribute] public string name;
        [XmlAttribute(DataType = "date")] public DateTime start;
        [XmlAttribute(DataType = "date")] public DateTime end;
    }

    public class Homework
    {
        [XmlAttribute] public string subjectID;
        [XmlIgnore] public Subject subject => Manager.Child.Subjects.FirstOrDefault(s => s.id == subjectID);
        [XmlAttribute] public string periodID;
        [XmlAttribute(DataType = "date")] public DateTime forThe;
        [XmlAttribute(DataType = "date")] public DateTime addedThe;
        [XmlAttribute] public string addedBy;
        public string content;
        [XmlAttribute] public bool done;
        [XmlAttribute] public bool exam;
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
        [XmlAttribute] public string subjectID;
        [XmlIgnore] public Subject subject => Manager.Child.Subjects.FirstOrDefault(s => s.id == subjectID);
        [XmlAttribute] public string teacher;
        [XmlAttribute] public DateTime start;
        [XmlAttribute] public DateTime end;
        [XmlAttribute] public string room;
        [XmlAttribute] public bool canceled;
    }

    public class Message
    {
        [XmlAttribute] public uint id;
        [XmlAttribute] public string subject;
        [XmlAttribute] public DateTime date;
        [XmlAttribute] public bool read;
        public enum Type { received, sent }
        [XmlAttribute] public Type type;
        public List<string> correspondents;

        public string content;
        public List<Request> documents = new List<Request>();
    }

    public class Book
    {
        [XmlAttribute] public string id;
        [XmlAttribute] public string[] subjectsID;
        [XmlIgnore] public IEnumerable<Subject> subjects => Manager.Child.Subjects.Where(s => subjectsID.Contains(s.id));

        [XmlAttribute] public string name;
        [XmlAttribute] public string editor;
        [XmlIgnore] public Sprite cover;
        [XmlText] public string image { get => Child.GetImage(cover); set => cover = Child.SetImage(value); }
        [XmlIgnore] public IEnumerator url;
    }


    public class Folder
    {
        [XmlAttribute] public string id;
        [XmlAttribute] public string name;
        public List<Folder> folders = new List<Folder>();
        public List<Document> documents = new List<Document>();
    }
    public class Document
    {
        [XmlAttribute] public string id;
        [XmlAttribute] public string name;
        public DateTime? added;
        public uint? size;
        public Request download;
    }
    #endregion

    public class Request
    {
        [XmlAttribute] public string docName;
        [XmlAttribute] public string url;
        [XmlIgnore] public Func<Dictionary<string, string>> headers;
        public SerializableKeyValue<string, string>[] requestHeaders
        {
            get => headers?.Invoke().Select(v => new SerializableKeyValue<string, string>(v)).ToArray();
            set => headers = () => value.ToDictionary(v => v.key, v => v.value);
        }
        public enum Method { Get, Post }
        [XmlAttribute] public Method method;
        [XmlIgnore] public Func<WWWForm> postData;
        public SerializableKeyValue<string, string>[] formData
        {
            get => postData?.Invoke().headers.Select(v => new SerializableKeyValue<string, string>(v)).ToArray();
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
            Debug.LogWarning($"Unsupported platform ({Application.platform}), we are unable to certify that the opening worked. The file has been saved at \"{path}\"");
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
