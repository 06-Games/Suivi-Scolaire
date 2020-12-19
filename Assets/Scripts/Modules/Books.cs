using Integrations;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Books : MonoBehaviour, Module
{
    public void Reset() { /* There is nothing to reset */ }
    public void OnEnable()
    {
        if (!Manager.isReady) gameObject.SetActive(false);
        else if (Manager.Data.ActiveChild.Books?.Count > 0) Refresh();
        else Reload();
    }
    public void Reload()
    {
        if (!Manager.provider.TryGetModule(out Integrations.Books module)) gameObject.SetActive(false);
        else StartCoroutine(module.GetBooks(() => Refresh()));
    }

    void Refresh()
    {
        var content = transform.Find("Content").GetComponent<ScrollRect>().content;
        for (int i = 1; i < content.childCount; i++) Destroy(content.GetChild(i).gameObject);

        foreach (var book in Manager.Data.ActiveChild.Books)
        {
            var go = Instantiate(content.GetChild(0).gameObject, content).transform;
            go.name = go.Find("Title").GetComponent<Text>().text = book.name;
            go.Find("Subject").GetComponent<Text>().text = book.subjects.FirstOrDefault().name;
            if (book.cover != null) go.Find("Cover").GetComponent<Image>().sprite = book.cover;
            go.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(Manager.provider.GetModule<Integrations.Books>().OpenBook(book)));
            go.gameObject.SetActive(true);
        }
    }
}
