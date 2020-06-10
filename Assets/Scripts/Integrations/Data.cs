using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Integrations.Data
{
    public class Data
    {
        //Data file information
        public string Label;
        public string Provider;
        public DateTime CreationDate;

        //General informations
        public List<Period> Periods;
        public List<Subject> Subjects;

        //Other information
        public List<Trimester> Trimesters;
        public List<Mark> Marks;
        public List<Homework> Homeworks;
        public List<ScheduledEvent> Schedule;
        public List<Message> Messages;
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
        [XmlIgnore] public Subject subject => Manager.Data.Subjects.FirstOrDefault(s => s.id == subjectID);
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
        [XmlIgnore] public Subject subject => Manager.Data.Subjects.FirstOrDefault(s => s.id == subjectID);
        public string periodID;
        public DateTime forThe;
        public DateTime addedThe;
        public string addedBy;
        public string content;
        public bool done;
        public bool exam;
        [XmlIgnore] public IEnumerable<Request> documents = new List<Request>();

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
        [XmlIgnore] public Subject subject => Manager.Data.Subjects.FirstOrDefault(s => s.id == subjectID);
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
        [XmlIgnore] public IEnumerable<Request> documents = new List<Request>();
    }
    #endregion

    public class Request
    {
        public string docName;
        public string url;
        public Dictionary<string, string> headers;
        public enum Method { Get, Post }
        public Method method;
        public UnityEngine.WWWForm postData;

        public UnityEngine.Networking.UnityWebRequest request
        {
            get
            {
                UnityEngine.Networking.UnityWebRequest webRequest = null;
                if (method == Method.Get) webRequest = UnityEngine.Networking.UnityWebRequest.Get(url);
                else if (method == Method.Post) webRequest = UnityEngine.Networking.UnityWebRequest.Post(url, postData ?? new UnityEngine.WWWForm());
                if (headers != null) foreach (var header in headers) webRequest?.SetRequestHeader(header.Key, header.Value);
                return webRequest;
            }
        }


        public System.Collections.IEnumerator GetDoc()
        {
            var _request = request;
            _request.SendWebRequest();
            while (!_request.isDone)
            {
                Manager.UpdateLoadingStatus("homeworks.downloading", "Downloading: [0]%", false, (_request.downloadProgress * 100).ToString("0"));
                yield return new UnityEngine.WaitForEndOfFrame();
            }
            Manager.HideLoadingPanel();

#if UNITY_STANDALONE
            var path = UnityEngine.Application.temporaryCachePath + "/Docs___" + docName;
            System.IO.File.WriteAllBytes(path, _request.downloadHandler.data);
            UnityEngine.Application.OpenURL(path);
#else
        var path = "/storage/emulated/0/Download/Suivi-Scolaire/" + docName;
        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path))) System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, request.downloadHandler.data);
        UnityAndroidOpenUrl.AndroidOpenUrl.OpenFile(path);
#endif
        }
    }
}
