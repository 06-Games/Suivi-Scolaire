/*MIT License

Copyright(c) 2020 Mikhail5412

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#if UNITY_EDITOR && UNITY_ANDROID
using UnityEngine;
using UnityEditor;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System;

namespace UnityAndroidOpenUrl.EditorScripts
{
    /// <summary>
    /// Static class whose task is to update the package name in AndroidManifest.xml if it has been changed
    /// </summary>
    [InitializeOnLoad]
    public static class PackageNameChanger
    {
        private const string PLUGINS_DIR = "Plugins/Android Files Opener";
        private const string TEMP_DIR = "Temp";
        private const string AAR_NAME = "release.aar";
        private const string MANIFEST_NAME = "AndroidManifest.xml";
        private const string PROVIDER_PATHS_NAME = "res/xml/filepaths.xml";

        private static string pathToPluginsFolder;
        private static string pathToTempFolder;
        private static string pathToBinary;

        private static string lastPackageName = "com.company.product";

        private static bool stopedByError;
        static PackageNameChanger()
        {
            pathToPluginsFolder = Path.Combine(Application.dataPath, PLUGINS_DIR);
            if (!Directory.Exists(pathToPluginsFolder))
            {
                Debug.LogError("Plugins folder not found. Please re-import asset. See README.md for details...");
                return;
            }
            pathToTempFolder = Path.Combine(pathToPluginsFolder, TEMP_DIR);
            pathToBinary = Path.Combine(pathToPluginsFolder, AAR_NAME);
            if (!File.Exists(pathToBinary))
            {
                Debug.LogError("File release.aar not found. Please re-import asset. See README.md for details...");
                return;
            }

            EditorApplication.update += Update;
            TryUpdatePackageName();
        }

        static void Update()
        {
            if (stopedByError) return;
            if (lastPackageName != PlayerSettings.applicationIdentifier) TryUpdatePackageName();
        }

        private static void TryUpdatePackageName() => RepackBinary();

        private static void RepackBinary()
        {
            try { ExtractBinary(); }
            catch (Exception e)
            {
                Debug.LogError("Extract release.aar error: " + e.Message);
                stopedByError = true;
                return;
            }
            
            ChangePackageName();

            try { ZippingBinary(); }
            catch (Exception e)
            {
                Debug.LogError("Zipping release.aar error: " + e.Message);
                stopedByError = true;
                return;
            }

            Directory.Delete(pathToTempFolder, true);
        }

        private static void ExtractBinary()
        {
            if (!File.Exists(pathToBinary)) throw new Exception("File release.aar not found. Please reimport asset. See README.md for details...");
            if (!Directory.Exists(pathToTempFolder)) Directory.CreateDirectory(pathToTempFolder);

            using (FileStream fs = new FileStream(pathToBinary, FileMode.Open))
            using (ZipFile zf = new ZipFile(fs))
            {

                for (int i = 0; i < zf.Count; ++i)
                {
                    ZipEntry zipEntry = zf[i];
                    string fileName = zipEntry.Name;

                    if (zipEntry.IsDirectory)
                    {
                        Directory.CreateDirectory(Path.Combine(pathToTempFolder, fileName));
                        continue;
                    }

                    using (Stream zipStream = zf.GetInputStream(zipEntry))
                    using (FileStream streamWriter = File.Create(Path.Combine(pathToTempFolder, fileName)))
                        zipStream.CopyTo(streamWriter);
                }

                if (zf != null)
                {
                    zf.IsStreamOwner = true;
                    zf.Close();
                }
            }
        }

        private static void ChangePackageName()
        {
            string manifestPath = Path.Combine(pathToTempFolder, MANIFEST_NAME);
            string manifestText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + $"\n<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"{PlayerSettings.applicationIdentifier}\">"
                + $"\n	<application>"
                + $"\n    <provider android:name=\"android.support.v4.content.FileProvider\" android:authorities=\"{PlayerSettings.applicationIdentifier}.fileprovider\" android:exported=\"false\" android:grantUriPermissions=\"true\" >"
                + $"\n      <meta-data android:name=\"android.support.FILE_PROVIDER_PATHS\" android:resource=\"@xml/filepaths\" />"
                + $"\n    </provider>"
                + $"\n	</application>"
                + $"\n  <uses-permission android:name=\"android.permission.REQUEST_INSTALL_PACKAGES\" />"
                + $"\n  <uses-sdk android:minSdkVersion=\"{(int)PlayerSettings.Android.minSdkVersion}\" android:targetSdkVersion=\"{(int)PlayerSettings.Android.targetSdkVersion}\" />"
                + $"\n</manifest>";
            File.WriteAllText(manifestPath, manifestText);

            string filepathsPath = Path.Combine(pathToTempFolder, PROVIDER_PATHS_NAME);
            string filepathsText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + $"\n<paths xmlns:android=\"http://schemas.android.com/apk/res/android\">"
                + $"\n  <external-path path=\"Android/data/{PlayerSettings.applicationIdentifier}\" name=\"files_root\" />"
                + $"\n  <external-path path=\".\" name=\"external_storage_root\" />"
                + $"\n</paths>";
            File.WriteAllText(filepathsPath, filepathsText);
            lastPackageName = PlayerSettings.applicationIdentifier;
        }

        private static void ZippingBinary()
        {
            if (!File.Exists(pathToBinary)) throw new Exception("File release.aar not found. Please reimport asset. See README.md for details...");
            if (!Directory.Exists(pathToTempFolder)) throw new Exception("Temp folder not found. See README.md for details...");

            using (FileStream zipStream = new FileStream(pathToBinary, FileMode.Open))
            using (ZipFile zipFile = new ZipFile(zipStream))
            {
                zipFile.BeginUpdate();
                zipFile.Add(Path.Combine(pathToTempFolder, MANIFEST_NAME), MANIFEST_NAME);
                zipFile.Add(Path.Combine(pathToTempFolder, PROVIDER_PATHS_NAME), PROVIDER_PATHS_NAME);
                zipFile.CommitUpdate();
            }
        }
    }
}
#endif
