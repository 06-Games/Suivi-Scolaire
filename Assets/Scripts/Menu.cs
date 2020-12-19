using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SimpleSideMenu))]
public class Menu : MonoBehaviour
{
    public SimpleSideMenu sideMenu { get; private set; }
    Transform manager;
    private void Awake() { sideMenu = GetComponent<SimpleSideMenu>(); }
    void Start()
    {
        UnityThread.executeInUpdate(() =>
        {
            var overlay = sideMenu.overlay;
            if (overlay.TryGetComponent<UnityEngine.EventSystems.EventTrigger>(out var eT)) Destroy(eT);
        });
        sideMenu.onStateUpdate += (state) =>
        {
            if (state == SimpleSideMenu.State.Closed) return;
            UpdateModules();
        };
        manager = Manager.instance.transform;
    }

    public void UpdateModules()
    {
        if (!Manager.isReady) return;
        var modulePanel = transform.Find("Panel").Find("Modules").GetComponent<UnityEngine.UI.ScrollRect>().content;
        var modules = Manager.Data.ActiveChild.modules;
        foreach (Transform module in modulePanel) module.gameObject.SetActive(module.name == "Home" | modules?.Contains(module.name) ?? false);
    }

    private void Update()
    {
        if (sideMenu.CurrentState == SimpleSideMenu.State.Open && Input.GetKeyDown(KeyCode.Escape)) sideMenu.Close();

        var openModule = manager.GetEnumerator().ToIEnumerable().OfType<Transform>().FirstOrDefault(c => c.gameObject.activeInHierarchy)?.Find("Top");
        if (openModule != null) sideMenu.handle.GetComponent<RectTransform>().offsetMax = new Vector2(25, -openModule.GetComponent<RectTransform>().sizeDelta.y);
    }


    public static void SelectChild() { SelectChild(Manager.Data.ActiveChild); }
    public static void SelectChild(Integrations.Data.Child selectedChild)
    {
        var childSelection = Manager.instance.Menu.transform.Find("Panel").Find("Child").Find("Slide");

        var instance = Manager.instance.Account;
        Manager.Data.activeChild = selectedChild.id;

        var list = childSelection.Find("List");
        for (int i = 1; i < list.childCount; i++) Destroy(list.GetChild(i).gameObject);
        foreach (var child in Manager.Data.Children)
        {
            Transform go = null;
            if (child == selectedChild) go = childSelection.Find("Selected");
            else
            {
                go = Instantiate(list.GetChild(0).gameObject, list).transform;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ChangeAccount();
                    SelectChild(child);

                    foreach (Transform module in Manager.instance.transform)
                        if (module.gameObject.activeInHierarchy && module.TryGetComponent<Module>(out var m)) m.OnEnable();
                    Manager.instance.Menu.UpdateModules();
                });
            }
            go.Find("ImageBG").Find("Image").GetComponent<Image>().sprite = child.sprite ?? instance.defaultChildImage;
            go.Find("Text").GetComponent<Text>().text = child.name;
            go.gameObject.SetActive(true);
        }
        var rect = childSelection.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 40 * (Manager.Data.Children.Length - 1));
        childSelection.GetComponent<SimpleSideMenu>().Setup();
        rect.pivot = new Vector2(0.5F, 0);
    }
    public static void ResetData() { foreach (var d in Manager.instance.modules) d?.Reset(); }
    public static void ChangeAccount()
    {
        Integrations.Saving.SaveData();
        ResetData();
        Manager.instance.Reset();
    }
}
