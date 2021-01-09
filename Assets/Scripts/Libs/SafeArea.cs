using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("Layout/Safe Area")]
public class SafeArea : UnityEngine.EventSystems.UIBehaviour, ILayoutSelfController, ILayoutElement
{
    protected SafeArea() => UpdateValues(true);

    //Settings
    public bool resizeTop, resizeBottom, resizeLeft, resizeRight;
    public bool paddingTop, paddingBottom, paddingLeft, paddingRight;

    //Components
    RectTransform Rect;
    LayoutGroup Group;

    //Default values
    public bool overrideMinSize, overridePreferredSize, overrideMinPadding = true;
    public Vector2 minSize = new Vector2(-1, -1), preferredSize = new Vector2(-1, -1);
    public RectOffset minPadding = new RectOffset();
    public void UpdateValues(bool force = false)
    {
        if (force || !overrideMinSize)
        {
            minWidth = minHeight = -1; //Prevents the returned result from being the values contained in this script
            minSize = new Vector2(LayoutUtility.GetMinWidth(Rect), LayoutUtility.GetMinHeight(Rect));
        }
        if (force || !overridePreferredSize)
        {
            preferredWidth = preferredHeight = -1; //Prevents the returned result from being the values contained in this script
            preferredSize = new Vector2(LayoutUtility.GetPreferredWidth(Rect), LayoutUtility.GetPreferredHeight(Rect));
            if (preferredSize.x < minSize.x) preferredSize.x = minSize.x;
            if (preferredSize.y < minSize.y) preferredSize.y = minSize.y;
        }
        if (force || !overrideMinPadding) minPadding = Group == null ? new RectOffset() : new RectOffset(Group.padding.left, Group.padding.right, Group.padding.top, Group.padding.bottom);
    }


    //Layout updates
    protected override void Start()
    {
        base.Start();
        Group = GetComponent<LayoutGroup>();
        UpdateValues();
        Rebuild();
    }
    void LateUpdate() => UpdateValues();
    protected override void OnEnable() { base.OnEnable(); Rebuild(); }
    protected override void OnDisable() { Rebuild(); base.OnDisable(); }
    protected override void OnRectTransformDimensionsChange() => Rebuild();
    public void Rebuild()
    {
        if (!IsActive()) return;
        if (Rect == null) Rect = GetComponent<RectTransform>();
        LayoutRebuilder.MarkLayoutForRebuild(Rect);

        UnityThread.executeCoroutine(D());
        System.Collections.IEnumerator D()
        {
            yield return new WaitForEndOfFrame(); //Corrects some layout issues during the first frame
            LayoutRebuilder.MarkLayoutForRebuild(Rect);
        }
    }


    //Variables
    RectOffset padding;

    #region ILayoutElement
    public float minWidth { get; private set; }
    public float minHeight { get; private set; }
    public float preferredWidth { get; private set; }
    public float preferredHeight { get; private set; }

    public float flexibleWidth => 0;
    public float flexibleHeight => 0;
    public int layoutPriority => 1;

    public void CalculateLayoutInputHorizontal()
    {
        padding = GetOffset(Rect.lossyScale);
        var delta = (resizeLeft ? padding.left : 0) + (resizeRight ? padding.right : 0);
        minWidth = minSize.x + delta;
        preferredWidth = preferredSize.x + delta;
    }
    public void CalculateLayoutInputVertical()
    {
        padding = GetOffset(Rect.lossyScale);
        var delta = (resizeTop ? padding.top : 0) + (resizeBottom ? padding.bottom : 0);
        minHeight = minSize.y + delta;
        preferredHeight = preferredSize.y + delta;
    }
    RectOffset GetOffset(Vector2 scale)
    {
        var safeArea = Screen.safeArea;
        return new RectOffset
        {
            left = (int)(safeArea.x / scale.x),
            right = (int)((Screen.width - safeArea.xMax) / scale.x),
            bottom = (int)(safeArea.y / scale.y),
            top = (int)((Screen.height - safeArea.yMax) / scale.y)
        };
    }
    #endregion

    #region ILayoutSelfController
    /// <summary>
    /// Calculate and apply the horizontal component of the size to the RectTransform
    /// </summary>
    public virtual void SetLayoutHorizontal()
    {
        CalculateLayoutInputHorizontal();
        if (resizeLeft || resizeRight) Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
        if (Group != null)
        {
            if (paddingLeft) Group.padding.left = minPadding.left + padding.left;
            if (paddingRight) Group.padding.right = minPadding.right + padding.right;
        }
    }
    /// <summary>
    /// Calculate and apply the vertical component of the size to the RectTransform
    /// </summary>
    public virtual void SetLayoutVertical()
    {
        CalculateLayoutInputVertical();
        if (resizeTop || resizeBottom) Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        if (Group != null)
        {
            if (paddingTop) Group.padding.top = minPadding.top + padding.top;
            if (paddingBottom) Group.padding.bottom = minPadding.bottom + padding.bottom;
        }
    }
    #endregion
}
