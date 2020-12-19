using UnityEngine;
#if !UNITY_EDITOR
using System.IO.Compression;
#endif

namespace Integrations
{
    public class Saving
    {
        public static System.IO.DirectoryInfo dataPath
        {
            get
            {
                var dir = new System.IO.DirectoryInfo($"{Application.persistentDataPath}/data/");
                if (!dir.Exists) dir.Create();
                return dir;
            }
        }
#if UNITY_EDITOR
        public static string dataExtension => ".xml";
#else
    public static string dataExtension => ".xml.gz";
#endif
        static System.IO.FileInfo dataFile => new System.IO.FileInfo($"{dataPath.FullName}/{Manager.Data.ID}{dataExtension}");
        public static Data.Data LoadData(System.IO.FileInfo file = null)
        {
            if (file == null) file = dataFile;
            if (file.Exists)
            {
                string text;
#if UNITY_EDITOR
                text = System.IO.File.ReadAllText(file.FullName);
#else
            using (var msi = dataFile.OpenRead())
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            using (var mso = new System.IO.StreamReader(gs)) text = mso.ReadToEnd();
#endif
                Logging.Log("Data loaded");
                return FileFormat.XML.Utils.XMLtoClass<Data.Data>(text);
            }
            return null;
        }
        public static void SaveData()
        {
            if (!Manager.isReady || Manager.Data == null) return;

            var text = FileFormat.XML.Utils.ClassToXML(Manager.Data, false);
#if UNITY_EDITOR
            System.IO.File.WriteAllText(dataFile.FullName, text);
#else
        using (var msi = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)))
        using (var mso = new System.IO.MemoryStream())
        {
            using (var gs = new GZipStream(mso, CompressionMode.Compress)) msi.CopyTo(gs);
            System.IO.File.WriteAllBytes(dataFile.FullName, mso.ToArray());
        }
#endif

            Logging.Log("Data saved");
        }
    }
}
