using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cortex;
using Rainbow.WebRTC.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This demo shows a basic demonstration of the session local distributed storage and will be enabled after a call started
/// </summary>
public class DemoDistributedStorage : BaseStartOrEnabled
{
    #region internal ui

    [SerializeField]
    private RectTransform Content;

    [SerializeField]
    private TMP_InputField InputKey;
    [SerializeField]
    private TMP_InputField InputValue;

    [SerializeField]
    private Button ButtonSet;

    #endregion // internal ui

    #region service objects

    [SerializeField]
    private DistributedStorage DistributedStorage;

    [SerializeField]
    private DataChannelRequester DataChannelRequester;

    #endregion // service objects

    #region lifecycle

    protected override void OnStartOrEnable()
    {
        if (DistributedStorage == null)
        {
            DistributedStorage = FindFirstObjectByType<DistributedStorage>();
        }

        ButtonSet.onClick.AddListener(OnClickSet);

        DistributedStorage.OnChange += OnStorageChange;

        // this script is deployed in a call, so we won't check, if a call is ready, but in general we could check the current call and register to the OnCallConnected of the ConnectionModel

        WaitForStorageSynched();
    }

    void OnDisable()
    {
        ButtonSet.onClick.RemoveListener(OnClickSet);
        DistributedStorage.OnChange -= OnStorageChange;

        InputKey.text = "";
        InputValue.text = "";

        foreach (Transform c in Content.transform)
        {
            Destroy(c.gameObject);
        }

        DistributedStorage.Stop();
    }

    async void WaitForStorageSynched()
    {
        // disable button while we synch

        // this will still be on the unity thread, as the first await is afterward
        ButtonSet.interactable = false;

        // for simple tasks, this method is not necessary, but we can try to make sure that all contacts are connected
        // in the future, this might be simplified

        // we wait a short time to be sure that call participants have been set
        // wait 0.5s
        await Task.Delay(200);

        // wait for data channel requests to be finished
        try
        {
            await DataChannelRequester.WaitForRequestsToFinish();
        }
        catch (Exception e)
        {
            // we ignore the exceptions here, since if an error ocurred, this just means that the timer ran out and there might just be a non-usable data channel
            Debug.LogException(e);
        }

        // wait for open data channels to become active
        await RainbowUtils.WaitForAllDataChannelsActive(DataTransportManager.Instance);

        // Start the stage
        DistributedStorage.Begin();

        // we wait a short time to be sure that call participants have been set
        await Task.Delay(100);

        // wait for all contacts to agree on a leader
        bool agree = false;
        while (!agree)
        {
            agree = await DistributedStorage.CheckLeaderAgreement();
            await Task.Delay(100);
        }

        // enable set button
        // we need to do this in the unity thread
        UnityExecutor.Execute(() =>
        {
            ButtonSet.interactable = true;
        });
    }

    #endregion // lifecycle

    #region internal

    private void OnStorageChange(IReadOnlyDictionary<string, string> state, string key, string value, DistributedStorage.ChangeType type)
    {
        // this handler is called when a value in the distributed storage changes

        // optimally, we would just use this changed value to update the display, but here we choose the simpler method for readability
        // simply recreate all values on each change
        foreach (Transform c in Content.transform)
        {
            Destroy(c.gameObject);
        }

        foreach (var (k, v) in state)
        {
            CreateTableEntry(k, v);
        }
    }

    private void OnClickSet()
    {
        var key = InputKey.text;
        var value = InputValue.text;

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            return;
        }

        DistributedStorage.RequestSet(key, value);
    }

    void SetAnchor(GameObject obj, float minX, float maxX, float minY, float maxY)
    {
        // create a rect transform, if it doesn't yet exist
        if (!obj.TryGetComponent<RectTransform>(out var rect))
        {
            rect = obj.AddComponent<RectTransform>();
        }

        //set anchor extends
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
    }

    void SetPadding(GameObject obj, float left, float right, float bottom, float top)
    {
        // create a rect transform, if it doesn't yet exist

        if (!obj.TryGetComponent<RectTransform>(out var rect))
        {
            rect = obj.AddComponent<RectTransform>();
        }

        // set padding offsets like from the inspector
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    void CreateTableEntry(string key, string value)
    {
        // very basic table like element via code

        GameObject entry = new("Entry");
        entry.AddComponent<RectTransform>();

        var layout = entry.AddComponent<LayoutElement>();
        layout.minWidth = 150;
        layout.flexibleWidth = 1;
        entry.transform.SetParent(Content, false);

        {
            GameObject user = new("Key");
            SetAnchor(user, 0.0f, 0.3f, 0.0f, 1.0f);
            SetPadding(user, 5.0f, 5.0f, 5.0f, 5.0f);

            var userText = user.AddComponent<TextMeshProUGUI>();

            userText.text = key;
            userText.fontSize = 32.0f;
            userText.color = Color.blue;
            userText.verticalAlignment = VerticalAlignmentOptions.Middle;

            user.transform.SetParent(entry.transform, false);
        }

        // spacer
        {
            GameObject spacer = new("Spacer");
            SetAnchor(spacer, 0.3f, 0.4f, 0.0f, 1.0f);
            SetPadding(spacer, 5.0f, 5.0f, 5.0f, 5.0f);

            var spacerText = spacer.AddComponent<TextMeshProUGUI>();
            spacerText.text = "|";
            spacerText.fontSize = 32.0f;
            spacerText.color = Color.black;
            spacerText.verticalAlignment = VerticalAlignmentOptions.Middle;

            spacer.transform.SetParent(entry.transform, false);
        }

        {
            GameObject msg = new("Value");
            SetAnchor(msg, 0.4f, 0.7f, 0.0f, 1.0f);
            SetPadding(msg, 5.0f, 5.0f, 5.0f, 5.0f);

            var msgText = msg.AddComponent<TextMeshProUGUI>();
            msgText.text = value;
            msgText.color = Color.magenta;
            msgText.fontSize = 24.0f;
            msgText.verticalAlignment = VerticalAlignmentOptions.Middle;

            msg.transform.SetParent(entry.transform, false);
        }

        {
            Button remove = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources()).GetComponent<Button>();
            SetAnchor(remove.gameObject, 0.7f, 1.0f, 0.0f, 1.0f);
            SetPadding(remove.gameObject, 5.0f, 5.0f, 5.0f, 5.0f);
            remove.onClick.AddListener(() =>
            {
                DistributedStorage.RequestRemove(key);
            });

            var buttonText = remove.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = "Remove";
            buttonText.fontSize = 24.0f;

            remove.transform.SetParent(entry.transform, false);
        }
    }

    #endregion // internal
}
