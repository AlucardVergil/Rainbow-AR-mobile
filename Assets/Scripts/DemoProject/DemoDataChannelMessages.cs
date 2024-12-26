using System.Collections;
using Cortex;
using Rainbow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemoDataChannelMessages : BaseStartOrEnabled
{
    private const string TopicName = "demo/chat";
    public RectTransform Content;
    public TMP_InputField Input;

    public ScrollRect ScrollRect;
    public Button ButtonSend;

    public DataTransportManager DataTransportManager;

    protected override void OnStartOrEnable()
    {
        if (DataTransportManager == null)
        {
            DataTransportManager = FindFirstObjectByType<DataTransportManager>();
        }

        ButtonSend.onClick.AddListener(OnClickSend);

        // Register for messages
        DataTransportManager.RegisterMessageHandler(TopicName, ReceiveChatMessage);
    }

    void OnDisable()
    {
        ButtonSend.onClick.RemoveListener(OnClickSend);
        DataTransportManager.RemoveMessageHandler(TopicName, ReceiveChatMessage);

        foreach (Transform c in Content)
        {
            Destroy(c.gameObject);
        }
    }

    private void ReceiveChatMessage(MessageData data, MessageAnswer answer)
    {
        var model = ConnectionModel.Instance;
        var contacts = model.Contacts;
        var contact = contacts.GetContactFromContactId(data.ContactId);
        CreateChatEntry(Util.GetContactDisplayName(contact), data.ParseJson<MsgValue<string>>().value, Color.blue);
    }

    private void OnClickSend()
    {
        var text = Input.text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Input.text = "";
        var msg = new MsgValue<string>(text);
        DataTransportManager.BroadcastTypedData(TopicName, msg);

        var model = ConnectionModel.Instance;

        CreateChatEntry(Util.GetContactDisplayName(model.CurrentUser), text, Color.red);
    }

    void CreateChatEntry(string userName, string text, Color nameColor)
    {
        // create basic ui elements for a chat entry via code

        GameObject entry = new("Entry");
        entry.AddComponent<RectTransform>();

        var layout = entry.AddComponent<LayoutElement>();
        layout.minWidth = 50;
        layout.preferredWidth = 150;
        layout.flexibleWidth = 1;
        entry.transform.SetParent(Content, false);

        GameObject user = new("UserName");
        var userRect = user.AddComponent<RectTransform>();
        var userText = user.AddComponent<TextMeshProUGUI>();
        userText.text = userName;
        userText.color = nameColor;
        userText.fontSize = 32.0f;
        userText.verticalAlignment = VerticalAlignmentOptions.Middle;

        user.transform.SetParent(entry.transform, false);
        userRect.anchorMin = new Vector2(0.0f, 0.0f);
        userRect.anchorMax = new Vector2(0.25f, 1.0f);
        userRect.offsetMin = new Vector2(5.0f, 5.0f);
        userRect.offsetMax = new Vector2(-5.0f, -5.0f);

        GameObject msg = new("MessageName");
        var msgRect = msg.AddComponent<RectTransform>();
        var msgText = msg.AddComponent<TextMeshProUGUI>();
        msgText.text = text;
        msgText.color = Color.black;
        msgText.fontSize = 24.0f;
        msgText.verticalAlignment = VerticalAlignmentOptions.Middle;

        msg.transform.SetParent(entry.transform, false);
        msgRect.anchorMin = new Vector2(0.25f, 0.0f);
        msgRect.anchorMax = new Vector2(1.0f, 1.0f);
        msgRect.offsetMin = new Vector2(5.0f, 5.0f);
        msgRect.offsetMax = new Vector2(-5.0f, -5.0f);

        StartCoroutine(ScrollDownRect());
    }

    IEnumerator ScrollDownRect()
    {
        // this should wait for updates to be finished. Then we can scroll to the end
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        ScrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}
