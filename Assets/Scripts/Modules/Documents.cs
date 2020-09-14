using Integrations;
using Integrations.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Documents : MonoBehaviour
{
    Folder CurentFolder;
    List<Folder> Path;

    public void OnEnable()
    {
        if (!Manager.isReady) { gameObject.SetActive(false); return; }
        Initialise();
    }
    public void Reset()
    {
        CurentFolder = null;
        Path = new List<Folder>();
    }

    public void Initialise()
    {
        if (!Manager.provider.TryGetModule(out Integrations.Documents module)) gameObject.SetActive(false);
        else if (Manager.Child.Documents == null) StartCoroutine(module.GetDocuments(() => refresh()));
        else refresh();

        void refresh()
        {
            Reset();
            Path.Add(Manager.Child.Documents);
            Refresh(Manager.Child.Documents);
        }
    }
    void Refresh(Folder parentFolder)
    {
        var parentBtn = transform.Find("Top").Find("Parent").GetComponent<Button>();
        parentBtn.onClick.RemoveAllListeners();
        parentBtn.gameObject.SetActive(Path.Count > 1);
        if (Path.Count > 1) parentBtn.onClick.AddListener(() =>
        {
            Path.Remove(Path.Last());
            Refresh(Path.Last());
        });
        CurentFolder = parentFolder;

        var content = transform.Find("Content").GetComponent<ScrollRect>().content;
        for (int i = 2; i < content.childCount; i++) Destroy(content.GetChild(i).gameObject);

        foreach (var folder in CurentFolder.folders)
        {
            var go = Instantiate(content.GetChild(0).gameObject, content).transform;
            go.name = go.Find("Name").GetComponent<Text>().text = folder.name;
            go.GetComponent<Button>().onClick.AddListener(() => { Path.Add(folder); Refresh(folder); });
            go.gameObject.SetActive(true);
        }
        foreach (var file in CurentFolder.documents)
        {
            var go = Instantiate(content.GetChild(1).gameObject, content).transform;
            go.name = go.Find("Name").GetComponent<Text>().text = file.name;
            go.Find("Infos").GetComponent<Text>().text = file.added?.ToShortDateString() + (file.added.HasValue && file.size.HasValue ? " - " : "") + (file.size.HasValue ? $"{file.size?.ToString()} B" : "");
            go.GetComponent<Button>().onClick.AddListener(() => UnityThread.executeCoroutine(file.download.GetDoc()));
            go.gameObject.SetActive(true);
        }
    }
}
