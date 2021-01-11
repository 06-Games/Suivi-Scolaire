using Integrations;
using Integrations.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Documents : MonoBehaviour, Module
{
    List<Folder> Path;

    public void Reset() => Path = new List<Folder>();
    public void OnEnable()
    {
        if (!Manager.isReady) gameObject.SetActive(false);
        else if (Manager.Data.ActiveChild.Documents == null) Reload();
        else Load();
    }
    public void Reload()
    {
        if (!Manager.provider.TryGetModule(out Integrations.Documents module)) gameObject.SetActive(false);
        else StartCoroutine(module.GetDocuments(() => Load()));
    }
    void Load()
    {
        Reset();
        Path.Add(Manager.Data.ActiveChild.Documents);
        Refresh(Manager.Data.ActiveChild.Documents);
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
        var CurentFolder = parentFolder;

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
            go.Find("Infos").GetComponent<Text>().text = file.added?.ToString("d", LangueAPI.Culture) + (file.added.HasValue && file.size.HasValue ? " - " : "") + FileSize(file.size);
            go.GetComponent<Button>().onClick.AddListener(() => UnityThread.executeCoroutine(Manager.provider.GetModule<Integrations.Documents>().OpenDocument(file)));
            go.gameObject.SetActive(true);
        }
    }
    string FileSize(uint? size)
    {
        if (!size.HasValue) return null;
        var f = new System.Globalization.NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalSeparator = "," };
        var unit = new[] { "B", "KB", "MB" };
        float s = size.Value;
        int i; for (i = 0; i < unit.Length && s >= 1000; i++) s /= 1000;
        return $"{s.ToString("0", f)} {unit[i]}";
    }
}
