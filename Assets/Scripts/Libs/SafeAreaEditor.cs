#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SafeArea)), CanEditMultipleObjects]
public class SafeAreaEditor : Editor
{
    bool settings;
    bool values;

    static bool playMode;
    public void OnEnable()
    {
        playMode = EditorApplication.isPlaying;

        values = true;
        settings = !playMode;
    }

    public override void OnInspectorGUI()
    {
        var alignRight = GUI.skin.label;
        alignRight.alignment = TextAnchor.MiddleRight;

        var sa = (SafeArea)target;

        settings = EditorGUILayout.BeginFoldoutHeaderGroup(settings, "Settings");
        if (settings)
        {
            var titleWidth = GUILayout.Width(60);
            var labelWidth = GUILayout.Width(45);

            //Resize
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Resize", titleWidth);
            EditorGUILayout.LabelField("Left", alignRight, labelWidth);
            sa.resizeLeft = EditorGUILayout.Toggle(sa.resizeLeft);
            EditorGUILayout.LabelField("Right", alignRight, labelWidth);
            sa.resizeRight = EditorGUILayout.Toggle(sa.resizeRight);
            EditorGUILayout.LabelField("Top", alignRight, labelWidth);
            sa.resizeTop = EditorGUILayout.Toggle(sa.resizeTop);
            EditorGUILayout.LabelField("Bottom", alignRight, labelWidth);
            sa.resizeBottom = EditorGUILayout.Toggle(sa.resizeBottom);
            EditorGUILayout.EndHorizontal();

            //Padding
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Padding", titleWidth);
            EditorGUILayout.LabelField("Left", alignRight, labelWidth);
            sa.paddingLeft = EditorGUILayout.Toggle(sa.paddingLeft);
            EditorGUILayout.LabelField("Right", alignRight, labelWidth);
            sa.paddingRight = EditorGUILayout.Toggle(sa.paddingRight);
            EditorGUILayout.LabelField("Top", alignRight, labelWidth);
            sa.paddingTop = EditorGUILayout.Toggle(sa.paddingTop);
            EditorGUILayout.LabelField("Bottom", alignRight, labelWidth);
            sa.paddingBottom = EditorGUILayout.Toggle(sa.paddingBottom);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        values = EditorGUILayout.BeginFoldoutHeaderGroup(values, "Default values");
        if (values)
        {
            var titleWidth = GUILayout.Width(60);
            var labelWidth = GUILayout.Width(10);
            var toggleWidth = GUILayout.Width(15);
            var fieldWidth = new[] { GUILayout.MinWidth(10), GUILayout.ExpandWidth(true) };

            //Min size
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min size", titleWidth);
            sa.overrideMinSize = EditorGUILayout.Toggle(sa.overrideMinSize, toggleWidth);
            GUI.enabled = sa.overrideMinSize;
            sa.minSize = EditorGUILayout.Vector2Field(GUIContent.none, sa.minSize, fieldWidth);
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            //Preferred size
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pref size", titleWidth);
            sa.overridePreferredSize = EditorGUILayout.Toggle(sa.overridePreferredSize, toggleWidth);
            GUI.enabled = sa.overridePreferredSize;
            sa.preferredSize = EditorGUILayout.Vector2Field(GUIContent.none, sa.preferredSize, fieldWidth);
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            //Padding
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Padding", titleWidth);
            sa.overrideMinPadding = EditorGUILayout.Toggle(sa.overrideMinPadding, toggleWidth);
            GUI.enabled = sa.overrideMinPadding;
            EditorGUILayout.LabelField("L", alignRight, labelWidth);
            sa.minPadding.left = EditorGUILayout.IntField(sa.minPadding.left, fieldWidth);
            EditorGUILayout.LabelField("R", alignRight, labelWidth);
            sa.minPadding.right = EditorGUILayout.IntField(sa.minPadding.right, fieldWidth);
            EditorGUILayout.LabelField("T", alignRight, labelWidth);
            sa.minPadding.top = EditorGUILayout.IntField(sa.minPadding.top, fieldWidth);
            EditorGUILayout.LabelField("B", alignRight, labelWidth);
            sa.minPadding.bottom = EditorGUILayout.IntField(sa.minPadding.bottom, fieldWidth);
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
#endif
