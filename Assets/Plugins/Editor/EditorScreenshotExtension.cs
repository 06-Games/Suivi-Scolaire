#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class EditorScreenshotExtension
{
    [MenuItem("Screenshot/Take Screenshot %#k")]
    public static void Screenshot()
    {
        var timestamp = System.DateTime.Now;
        var stampString = string.Format("_{0}-{1:00}-{2:00}_{3:00}-{4:00}-{5:00}", timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second);
        ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "Screenshot" + stampString + ".png"), 4);
    }
}
#endif