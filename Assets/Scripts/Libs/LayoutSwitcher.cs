﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//[ExecuteAlways] //Edit mode
[AddComponentMenu("Layout/Layout Switcher")]
public class LayoutSwitcher : MonoBehaviour
{
    public enum Mode { Horizontal, Vertical }
    public Mode mode;

    [Header("Default values")]
    public RectOffset padding;
    public float spacing;
    public TextAnchor childAlignment;

    [System.Serializable] public class WH { public bool width; public bool height; }
    public WH controlChildSize;
    public WH useChildSize;
    public WH childForceExpand;

    [Header("Events")]
    public LayoutSwitcher[] childs;

    Mode lastMode = (Mode)(-1);
    void Update()
    {
        if (lastMode != mode)
        {
            Switch();
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
    }

    HorizontalOrVerticalLayoutGroup LayoutGroup;
    public void Switch(Mode _mode) { mode = _mode; Switch(); }
    public void Switch()
    {
        if (lastMode == mode) return;
        if (LayoutGroup != null) Destroy(LayoutGroup);
        foreach (var c in GetComponents<HorizontalOrVerticalLayoutGroup>()) Destroy(c);
        try
        {
            switch (mode)
            {
                case Mode.Horizontal:
                    LayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
                    break;
                case Mode.Vertical:
                    LayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
                    break;
            };
            LayoutGroup.hideFlags = HideFlags.HideAndDontSave;
            LayoutGroup.padding = padding;
            LayoutGroup.spacing = spacing;
            LayoutGroup.childAlignment = childAlignment;
            LayoutGroup.childControlWidth = controlChildSize.width;
            LayoutGroup.childControlHeight = controlChildSize.height;
            LayoutGroup.childScaleWidth = useChildSize.width;
            LayoutGroup.childScaleHeight = useChildSize.height;
            LayoutGroup.childForceExpandWidth = childForceExpand.width;
            LayoutGroup.childForceExpandHeight = childForceExpand.height;
            lastMode = mode;

            UnityThread.executeInUpdate(() =>
            {
                foreach (var child in childs) { child.Switch(mode); LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)child.transform); }
            });
        }
        catch { }
    }

    new void Destroy(Object obj)
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying) DestroyImmediate(obj);
        else
#endif
            Object.Destroy(obj);
    }
}