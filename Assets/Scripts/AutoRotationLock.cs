using UnityEngine;

public class AutoRotationLock : MonoBehaviour
{
    void OnApplicationFocus(bool haveFocus)
    {
        if (haveFocus) ToggleAutoRotation();
    }

    static void ToggleAutoRotation()
    {
        var AutoRotationOn = DeviceAutoRotationIsOn();
        Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = AutoRotationOn;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    static bool DeviceAutoRotationIsOn()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");
            var rotationOn = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "accelerometer_rotation");

            return rotationOn == 1;
        }
#endif
        return true;
    }

}
