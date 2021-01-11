#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public class BuildProperties : MonoBehaviour
{
    [PostProcessBuild]
    public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS) return;
        // Get plist
        string plistPath = pathToBuiltProject + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        PlistElementDict rootDict = plist.root;

        // Change values
        rootDict.SetBoolean("UIFileSharingEnabled", true); // https://developer.apple.com/documentation/bundleresources/information_property_list/uifilesharingenabled
        rootDict.SetBoolean("LSSupportsOpeningDocumentsInPlace", true); // https://developer.apple.com/documentation/bundleresources/information_property_list/lssupportsopeningdocumentsinplace

        plist.WriteToFile(plistPath); // Write plist
    }
}
#endif
