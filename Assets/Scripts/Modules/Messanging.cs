using Integrations;
using Integrations.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Modules
{
    public class Messanging : MonoBehaviour, Module
    {
        Integrations.Messanging module;
        public void Reset() { module = null; }
        public void OnEnable()
        {
            if (!Manager.isReady || !Manager.provider.TryGetModule(out module)) { gameObject.SetActive(false); return; }
            if (Manager.Child.Messages == null || Manager.Child.Messages.Count == 0) StartCoroutine(module.GetMessages(() => Refresh()));
            else Refresh();
            Manager.OpenModule(gameObject);
        }

        public void Refresh()
        {
            var content = transform.Find("Content");
            foreach (Transform go in content) go.gameObject.SetActive(false);

            var isntEmpty = Manager.Child.Messages?.Count != 0;

            if (!isntEmpty) { content.Find("Empty").gameObject.SetActive(true); return; }

            var list = content.Find("List").GetComponent<ScrollRect>();
            list.gameObject.SetActive(isntEmpty);
            var lContent = list.content;
            for (int i = 1; i < lContent.childCount; i++) Destroy(lContent.GetChild(i).gameObject);

            foreach (var message in Manager.Child.Messages)
            {
                var go = Instantiate(lContent.GetChild(0).gameObject, lContent).transform;
                go.GetComponent<Button>().onClick.AddListener(() => OpenMsg(message));
                go.gameObject.name = message.id.ToString();
                var indicator = go.Find("Indicator").GetComponent<Image>();
                if (!message.read) indicator.color = new Color32(135, 135, 135, 255);
                else if (message.type == Message.Type.sent) indicator.color = new Color32(85, 115, 175, 255);
                else indicator.color = new Color32(0, 0, 0, 0);
                go.Find("Subject").GetComponent<Text>().text = message.subject;
                go.Find("Correspondents").GetComponent<Text>().text = string.Join(" / ", message.correspondents);
                go.Find("Date").GetComponent<Text>().text = message.date.ToString("dd/MM/yyyy HH:mm");
                go.gameObject.SetActive(true);
            }
        }

        public void OpenMsg(Message message)
        {
            message.read = true;
            var content = transform.Find("Content");
            foreach (Transform go in content) go.gameObject.SetActive(false);

            if (message.content == null) StartCoroutine(module.LoadExtraMessageData(message.id, () => SetData()));
            else SetData();

            void SetData()
            {
                var detail = content.Find("Detail");

                var top = detail.Find("Top");
                top.Find("Subject").GetComponent<Text>().text = message.subject;
                top.Find("Correspondents").GetComponent<Text>().text = string.Join(" / ", message.correspondents);
                top.Find("Date").GetComponent<Text>().text = message.date.ToString("dd/MM/yyyy HH:mm");

                var contentPanel = detail.Find("Content").GetComponent<ScrollRect>().content;
                contentPanel.Find("Message Content").GetComponent<TMPro.TMP_InputField>().text = message.content;
                var docs = contentPanel.Find("Docs");
                for (int i = 2; i < docs.childCount; i++) Destroy(docs.GetChild(i).gameObject);
                foreach (var doc in message.documents)
                {
                    var docGo = Instantiate(docs.GetChild(1).gameObject, docs).transform;
                    docGo.GetComponent<TMPro.TextMeshProUGUI>().text = System.IO.Path.GetFileNameWithoutExtension(doc.docName);
                    docGo.GetComponent<Button>().onClick.AddListener(() => UnityThread.executeCoroutine(doc.GetDoc()));
                    docGo.gameObject.SetActive(true);
                }
                docs.gameObject.SetActive(docs.childCount > 2);

                detail.gameObject.SetActive(true);
            }
        }
    }
}
