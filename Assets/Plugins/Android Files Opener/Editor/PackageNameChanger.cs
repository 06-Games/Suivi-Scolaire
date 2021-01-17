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
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityAndroidOpenUrl.EditorScripts
{
    /// <summary>
    /// Static class whose task is to update the package name in AndroidManifest.xml if it has been changed
    /// </summary>
    [InitializeOnLoad]
    public class PackageNameChanger : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string PLUGINS_DIR = "Plugins/Android Files Opener";
        private const string TEMP_DIR = ".AAR Plugin";
        private const string AAR_NAME = "Android Files Opener.aar";
        private const string MANIFEST_NAME = "AndroidManifest.xml";
        private const string PROVIDER_PATHS_NAME = "res/xml/filepaths.xml";

        private static string pathToPluginsFolder;
        private static string pathToTempFolder;
        private static string pathToBinary;

        private static string lastPackageName = "com.company.product";

        public PackageNameChanger()
        {
            pathToPluginsFolder = Path.Combine(Application.dataPath, PLUGINS_DIR);
            if (!Directory.Exists(pathToPluginsFolder))
            {
                Debug.LogError("Plugins folder not found. Please re-import asset. See README.md for details...");
                return;
            }
            pathToTempFolder = Path.Combine(pathToPluginsFolder, TEMP_DIR);
            pathToBinary = Path.Combine(pathToPluginsFolder, AAR_NAME);

            EditorApplication.update += () => { if (lastPackageName != PlayerSettings.applicationIdentifier) ChangePackageName(); };
            ChangePackageName();
        }

        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
            {
                ChangePackageName();

                if (!Directory.Exists(pathToTempFolder)) Debug.LogError("Temp folder not found. See README.md for details...");
                new ICSharpCode.SharpZipLib.Zip.FastZip().CreateZip(pathToBinary, pathToTempFolder, true, "");
            }
        }
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
            {
                File.Delete(pathToBinary);
                File.Delete(pathToBinary + ".meta");
            }
        }

        private static void ChangePackageName()
        {
            var appId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            string manifestPath = Path.Combine(pathToTempFolder, MANIFEST_NAME);
            string manifestText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + $"\n<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"{appId}\">"
                + $"\n	<application>"
                + $"\n    <provider android:name=\"android.support.v4.content.FileProvider\" android:authorities=\"{appId}.fileprovider\" android:exported=\"false\" android:grantUriPermissions=\"true\" >"
                + $"\n      <meta-data android:name=\"android.support.FILE_PROVIDER_PATHS\" android:resource=\"@xml/filepaths\" />"
                + $"\n    </provider>"
                + $"\n	</application>"
                + $"\n  <uses-permission android:name=\"android.permission.REQUEST_INSTALL_PACKAGES\" />"
                + $"\n  <uses-sdk android:minSdkVersion=\"{GetSDK(PlayerSettings.Android.minSdkVersion, 16)}\" android:targetSdkVersion=\"{GetSDK(PlayerSettings.Android.targetSdkVersion, 29)}\" />"
                + $"\n</manifest>";
            File.WriteAllText(manifestPath, manifestText);

            string filepathsPath = Path.Combine(pathToTempFolder, PROVIDER_PATHS_NAME);
            string filepathsText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + $"\n<paths xmlns:android=\"http://schemas.android.com/apk/res/android\">"
                + $"\n  <external-path path=\"Android/data/{appId}\" name=\"files_root\" />"
                + $"\n  <external-path path=\".\" name=\"external_storage_root\" />"
                + $"\n</paths>";
            File.WriteAllText(filepathsPath, filepathsText);
            lastPackageName = PlayerSettings.applicationIdentifier;

            int GetSDK(AndroidSdkVersions sdkVersions, int defaultSdk) => sdkVersions == AndroidSdkVersions.AndroidApiLevelAuto ? defaultSdk : (int)sdkVersions;
        }
    }
}
#endif
