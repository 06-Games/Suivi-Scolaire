using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Integrations
{
    public static class ProviderExtension
    {
        #region Extensions
        static string[] whiteList = new[] { "Auth" };
        public static bool TryGetModule<T>(this Provider provider, out T module)
        {
            module = GetModule<T>(provider);
            return !module?.Equals(default) ?? false;
        }
        public static T GetModule<T>(this Provider provider)
        {
            var moduleName = typeof(T).ToString().Substring("Integrations.".Length);
            if (!whiteList.Contains(moduleName) && (!Manager.Data.ActiveChild.modules?.Contains(moduleName) ?? false)) return default;
            try { return (T)provider; }
            catch { return default; }
        }
        #endregion

        #region Utils
        public static string RemoveEmptyLines(this string lines) => lines == null ? "" : System.Text.RegularExpressions.Regex.Replace(lines, @"^\s*$\n|\r", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline).TrimEnd();
        public static string FromBase64(this string b64) => System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(b64));
        public static DateTime UnixTimeStampToDateTime(this double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        #endregion

        #region Functions
        //Docs
        public static IEnumerator DownloadDoc(UnityEngine.Networking.UnityWebRequest request, Data.Document doc) => DownloadDoc(request, DocPath(doc));
        public static IEnumerator DownloadDoc(UnityEngine.Networking.UnityWebRequest request, FileInfo path)
        {
            request.SendWebRequest();
            while (!request.isDone)
            {
                Manager.UpdateLoadingStatus("homeworks.downloading", "Downloading: [0]%", false, (request.downloadProgress * 100).ToString("0"));
                yield return new WaitForEndOfFrame();
            }
            if (request.error == null) Manager.HideLoadingPanel();
            else { Manager.FatalErrorDuringLoading("Error downloading file", request.error); yield break; }

            File.WriteAllBytes(path.FullName, request.downloadHandler.data);
            OpenDoc(path);
        }
        public static FileInfo DocPath(Data.Document doc)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var path = "/storage/emulated/0/Download/Suivi-Scolaire/";
#else
            var path = Application.temporaryCachePath + Path.DirectorySeparatorChar;
#endif
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var fileName = doc.id + " - " + doc.name;
            foreach (var chara in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(chara, '.');
            return new FileInfo(path + fileName);
        }
        public static void OpenDoc(FileInfo path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            UnityAndroidOpenUrl.AndroidOpenUrl.OpenFile(path.FullName);
#elif UNITY_IOS && !UNITY_EDITOR
            drstc.DocumentHandler.DocumentHandler.OpenDocument(path.FullName);
#else
            Application.OpenURL(path.FullName);
#if !UNITY_STANDALONE && !UNITY_EDITOR
            Debug.LogWarning($"Unsupported platform ({Application.platform}), we are unable to certify that the opening worked. The file has been saved at \"{path.FullName}\"");
#endif
#endif
        }

        public static void GenerateSubjectColors()
        {
            var rnd = new System.Random();
            var colorPalette = new System.Collections.Generic.List<Color32> {
                new Color32(100, 140, 200, 255), // #648CC8: Blue
                new Color32(165, 170, 190, 255), // #A5AABE: Gray
                new Color32(112, 162, 136, 255), // #70A288: Green
                new Color32(218, 183, 133, 255), // #DAB785: Gold Ravenne
                new Color32(213, 137, 111, 255), // #D5896F: Dark Salmon
                new Color32(255, 188, 010, 255), // #FFBC0A: Orange Creamsicle
                new Color32(174, 118, 166, 255), // #AE76A6: Purple
                new Color32(204, 214, 235, 255), // #CCD6EB: Very Light Blue
                new Color32(163, 195, 217, 255), // #A3C3D9: Cerulean Pigeon Post (Sky blue)
                new Color32(170, 229, 153, 255), // #AAE599: Pale Green
                new Color32(223, 229, 192, 255), // #DFE5C0: Light Oatmeal
                new Color32(202, 175, 234, 255), // #CAAFEA: Pale Purple
                new Color32(249, 215, 194, 255), // #F9D7C2: Peachy Creaminess
                new Color32(217, 244, 146, 255), // #D9F492: Pale Light Green
                new Color32(241, 227, 243, 255)  // #F1E3F3: Light Purple
            };
            var defaultColor = colorPalette.First();

            foreach (var subject in Manager.Data.ActiveChild.Subjects.Where(s => s.color != Color.black)) colorPalette.Remove(subject.color);
            foreach (var subject in Manager.Data.ActiveChild.Subjects.Where(s => s.color == Color.black))
            {
                var index = rnd.Next(colorPalette.Count);
                subject.color = index < colorPalette.Count ? colorPalette[index] : defaultColor;
                colorPalette.Remove(subject.color);
            }
        }
        #endregion
    }
}
