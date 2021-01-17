using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FileFormat
{
    public class JSON
    {
        public Newtonsoft.Json.Linq.JObject jToken;
        public JSON(string plainText)
        {
            if (!string.IsNullOrEmpty(plainText))
            {
                try { jToken = Newtonsoft.Json.Linq.JObject.Parse(plainText); }
                catch (System.Exception e) { Debug.LogError("Error parsing:\n" + plainText + "\nError details:\n" + e.Message); }
            }
        }
        public JSON(Newtonsoft.Json.Linq.JToken token)
        {
            try { jToken = (Newtonsoft.Json.Linq.JObject)token; }
            catch (System.Exception e) { Debug.LogError("Error parsing the token\nError details:\n" + e.Message); }
        }

        public JSON GetCategory(string token) { if (jToken == null) return new JSON(null); else return new JSON(jToken.SelectToken(token)); }
        public void Delete() { if (jToken != null) jToken.Remove(); }
        public bool ContainsValues { get { if (jToken == null) return false; else return jToken.HasValues; } }

        public IEnumerable<T> Values<T>() { if (jToken == null) return default; else return jToken.Values<T>(); }
        public T Value<T>(string value) { if (jToken == null) return default; else return jToken.Value<T>(value); }
        public bool ValueExist(string value) { if (jToken == null) return false; else return jToken.Value<string>(value) != null; }

        public override string ToString() { return jToken.ToString(); }
    }

    namespace XML
    {
        public static class Utils
        {
            public static string ClassToXML<T>(T data, bool minimised = true)
            {
                var _serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                var settings = new System.Xml.XmlWriterSettings
                {
                    NewLineHandling = System.Xml.NewLineHandling.Entitize,
                    Encoding = Encoding.UTF8,
                    Indent = !minimised
                };

                using (var stream = new StringWriter())
                using (var writer = System.Xml.XmlWriter.Create(stream, settings))
                {
                    _serializer.Serialize(writer, data);
                    return stream.ToString();
                }
            }
            public static T XMLtoClass<T>(string data)
            {
                System.Xml.Serialization.XmlSerializer _serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                if (string.IsNullOrEmpty(data)) return default;

                using (var stream = new StringReader(data))
                using (var reader = System.Xml.XmlReader.Create(stream))
                    return (T)_serializer.Deserialize(reader);
            }
        }
    }
}
