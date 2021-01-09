using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Layout Switcher")]
public class LayoutSwitcher : HorizontalOrVerticalLayoutGroup
{
    /// <summary>Called by the layout system. Also see ILayoutElement</summary>
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        CalcAlongAxis(0, mode == Mode.Vertical);
    }
    /// <summary>Called by the layout system. Also see ILayoutElement</summary>
    public override void CalculateLayoutInputVertical() => CalcAlongAxis(1, mode == Mode.Vertical);
    /// <summary>Called by the layout system. Also see ILayoutElement</summary>
    public override void SetLayoutHorizontal() => SetChildrenAlongAxis(0, mode == Mode.Vertical);
    /// <summary>Called by the layout system. Also see ILayoutElement</summary>
    public override void SetLayoutVertical() => SetChildrenAlongAxis(1, mode == Mode.Vertical);

    public enum Mode { Horizontal, Vertical }
    public Mode mode;
    public bool autoSwitch = true;

    public Mode lastMode;
    protected override void Update()
    {
        base.Update();
        mode = GetMode();
        if (mode != lastMode) { lastMode = mode; Switch(mode); }
    }
    protected override void Awake() { base.Awake(); Switch(GetMode()); }
    Mode GetMode()
    {
        if (autoSwitch) return Screen.width > Screen.height ? Mode.Horizontal : Mode.Vertical;
        else return mode;
    }

    public event System.Action<Mode> switched;
    public void Switch(Mode _mode)
    {
        mode = _mode;
        GetComponent<SafeArea>()?.UpdateValues();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        GetComponent<SafeArea>()?.UpdateValues();
        switched?.Invoke(mode);
    }
}
