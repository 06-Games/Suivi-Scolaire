using Integrations;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Books : MonoBehaviour
{
    public void OnEnable()
    {
        if (!Manager.isReady) { gameObject.SetActive(false); return; }
        Initialise();
    }

    public void Initialise()
    {
        if (!Manager.provider.TryGetModule(out Integrations.Books module)) gameObject.SetActive(false);
        else if (Manager.Child.Books == null) StartCoroutine(module.GetBooks(() => Refresh()));
        else Refresh();
    }
    void Refresh()
    {
        var content = transform.Find("Content");
        for (int i = 1; i < content.childCount; i++) Destroy(content.GetChild(i).gameObject);

        foreach (var book in Manager.Child.Books)
        {
            var go = Instantiate(content.GetChild(0).gameObject, content).transform;
            go.name = go.Find("Title").GetComponent<Text>().text = book.name;
            go.Find("Subject").GetComponent<Text>().text = book.subjects.FirstOrDefault().name;
            if (book.cover != null) go.Find("Cover").GetComponent<Image>().sprite = book.cover;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(book.url);
            });
            go.gameObject.SetActive(true);
        }
    }
}
