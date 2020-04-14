using Integrations;
using System.Linq;
using Tools;
using UnityEngine;

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
        var modulePanel = transform.Find("Panel").Find("Modules");
        var modules = FirstStart.selectedAccount.child.modules;
        foreach (Transform module in modulePanel) module.gameObject.SetActive(module.name == "Home" | modules?.Contains(module.name) ?? false);
    }

    private void Update()
    {
        if (sideMenu.CurrentState == SimpleSideMenu.State.Open && Input.GetKeyDown(KeyCode.Escape)) sideMenu.Close();

        var openModule = manager.GetEnumerator().ToIEnumerable().OfType<Transform>().FirstOrDefault(c => c.gameObject.activeInHierarchy)?.Find("Top");
        if (openModule != null) sideMenu.handle.GetComponent<RectTransform>().offsetMax = new Vector2(25, -openModule.GetComponent<RectTransform>().sizeDelta.y);
    }
}
