using System.Collections.Generic;
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
                UnityEngine.Networking.UnityWebRequest request = null;
                switch (method)
                {
                    case Method.Get: request = UnityEngine.Networking.UnityWebRequest.Get(url); break;
                    case Method.Post: request = UnityEngine.Networking.UnityWebRequest.Post(url, postData ?? new UnityEngine.WWWForm()); break;
                }
                foreach (var header in headers ?? new Dictionary<string, string>()) request.SetRequestHeader(header.Key, header.Value);

                return request;
            }
        }
    }

    public class Period
    {
        public string name;
        public string id;
        public TimeRange timeRange;
    }
}
