using DanielLochner.Assets.SimpleSideMenu;
using Integrations;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SimpleSideMenu))]
public class Menu : MonoBehaviour
{
    void Start()
    {
        UnityThread.executeInUpdate(() =>
        {
            var overlay = GetComponent<SimpleSideMenu>().overlay;
            if (overlay.TryGetComponent<UnityEngine.EventSystems.EventTrigger>(out var eT)) Destroy(eT);
        });
        GetComponent<SimpleSideMenu>().onStateUpdate += (state) =>
        {
            if (state == SimpleSideMenu.State.Closed) return;
            if (!Manager.isReady) return;

            var provider = Manager.provider;
            var modulePanel = transform.Find("Panel").Find("Modules");
            var modules = provider.Modules().ToList();
            foreach (Transform module in modulePanel) module.gameObject.SetActive(modules.Contains(module.name));
        };
    }
}
