using Integrations;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SimpleSideMenu))]
public class Menu : MonoBehaviour
{
    public SimpleSideMenu sideMenu { get; private set; }
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
            if (!Manager.isReady) return;

            var provider = Manager.provider;
            var modulePanel = transform.Find("Panel").Find("Modules");
            var modules = provider.Modules().ToList();
            foreach (Transform module in modulePanel) module.gameObject.SetActive(modules.Contains(module.name));
        };
    }

    private void Update()
    {
        if (sideMenu.CurrentState == SimpleSideMenu.State.Open && Input.GetKeyDown(KeyCode.Escape)) sideMenu.Close();
    }
}
