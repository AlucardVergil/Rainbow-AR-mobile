using UnityEngine;
using Rainbow;
using Rainbow.Events;
using Rainbow.Model;
using System;
using System.Collections;
using System.IO;
using UnityEngine.UI;

public class RainbowManager : MonoBehaviour
{
    // Singleton instance
    public static RainbowManager Instance { get; private set; }

    // Rainbow SDK details
    public string appId = "e5001db070e011efa6661b0bb9c90370";
    public string appSecretKey = "qYeZc3HRs9I4b04RhOwYSuXg8ZgzpN1rxs4uXL3mT32APaESr3TDzkrUlqY9RvXl";
    public string hostName = "web-sandbox.openrainbow.com"; // Or "openrainbow.com" for production
    public InputField loginInputField;
    public InputField passwordInputField;
    //public string login = "";
    //public string password = "";

    private bool endProgram = false;
    private Rainbow.Application rbApplication;
    private AutoReconnection rbAutoReconnection;
    private Contacts rbContacts;

    public SpriteRenderer spriteRenderer;

    byte[] avatarData;
    // This is to inform when initialization is performed and execute the function from Update() because when I run the coroutine from
    // the event handler RbApplication_InitializationPerformed or the callback method (not sure which), it didn't execute due to threading issues bcz it
    // wasn't executed in the main thread and coroutines need to execute only in the main thread.
    bool initializationPerformedFlag = false;



    private void Awake()
    {
        // Implementing the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // Ensure only one instance exists
            return;
        }
        Instance = this;

        UnityMainThreadDispatcher.Instance();

        //InitializeRainbow();
    }


    public void RainbowLogin()
    {
        InitializeRainbow(loginInputField.text, passwordInputField.text);
    }




    // Initialize Rainbow SDK connection
    private void InitializeRainbow(string login, string password)
    {
        login = "vagelisro@gmail.com";
        password = "Uom123456789#";


        // Create objects from SDK
        rbApplication = new Rainbow.Application();
        rbAutoReconnection = rbApplication.GetAutoReconnection();
        rbContacts = rbApplication.GetContacts();

        // Set events from Rainbow.Application
        rbApplication.AuthenticationFailed += RbApplication_AuthenticationFailed;
        rbApplication.AuthenticationSucceeded += RbApplication_AuthenticationSucceeded;
        rbApplication.ConnectionStateChanged += RbApplication_ConnectionStateChanged;
        rbApplication.InitializationPerformed += RbApplication_InitializationPerformed;

        // Set events from Rainbow.AutoReconnection
        rbAutoReconnection.Started += RbAutoReconnection_Started;
        rbAutoReconnection.Cancelled += RbAutoReconnection_Cancelled;
        rbAutoReconnection.MaxNbAttemptsReached += RbAutoReconnection_MaxNbAttemptsReached;
        rbAutoReconnection.OneNetworkInterfaceOperational += RbAutoReconnection_OneNetworkInterfaceOperational;
        rbAutoReconnection.TokenExpired += RbAutoReconnection_TokenExpired;

        // Set global configuration info
        rbApplication.SetApplicationInfo(appId, appSecretKey);
        rbApplication.SetHostInfo(hostName);

        // Set a limit of auto reconnection attempts
        rbAutoReconnection.MaxNbAttempts = 10;


        //if (login == "" && password == "")
        //{
        //    login = rbApplication.GetUserLoginFromCache();
        //    password = rbApplication.GetUserPasswordFromCache();
        //}

        //login = rbApplication.GetUserLoginFromCache();
        //password = rbApplication.GetUserPasswordFromCache();



        // Start the login process
        rbApplication.Login(login, password);


        // Start the coroutine to wait until the process completes
        StartCoroutine(WaitForEndProgram());
    }




    private IEnumerator WaitForEndProgram()
    {
        while (!endProgram)
        {
            yield return new WaitForSeconds(0.1f); // Yield to prevent freezing the main thread
        }
    }




    // Event handlers
    private void RbApplication_InitializationPerformed(object sender, EventArgs e)
    {
        Debug.Log("InitializationPerformed");

        FetchCurrentProfile();

        //GetComponent<ConversationsAndContacts>().InitializeConversationsAndContacts();
        //GetComponent<BubbleManager>().InitializeBubblesManager();
    }

    private void RbApplication_ConnectionStateChanged(object sender, ConnectionStateEventArgs e)
    {
        Debug.Log($"ConnectionStateChanged: [{e.ConnectionState.State}]");

        if (e.ConnectionState.State == ConnectionState.Connecting)
        {
            Debug.Log($"Current number of attempts: [{rbAutoReconnection.CurrentNbAttempts}]");
        }
    }

    private void RbApplication_AuthenticationSucceeded(object sender, EventArgs e)
    {
        Debug.Log("AuthenticationSucceeded");
    }

    private void RbApplication_AuthenticationFailed(object sender, SdkErrorEventArgs e)
    {
        Debug.LogError($"AuthenticationFailed: {e.SdkError}");
    }

    private void RbAutoReconnection_Cancelled(object sender, StringEventArgs e)
    {
        var connectionState = rbAutoReconnection.GetServerDisconnectionInformation();
        Debug.Log($"AutoReconnection service has been cancelled/stopped - Reason: [{e.Value}] - connectionState: [{connectionState}]");
        endProgram = true;
    }

    private void RbAutoReconnection_Started(object sender, EventArgs e)
    {
        Debug.Log("AutoReconnection service started");
    }

    private void RbAutoReconnection_TokenExpired(object sender, EventArgs e)
    {
        Debug.Log("User token has expired");
    }

    private void RbAutoReconnection_OneNetworkInterfaceOperational(object sender, BooleanEventArgs e)
    {
        Debug.Log(e.Value ? "One network interface is operational" : "No network interface is operational");
    }

    private void RbAutoReconnection_MaxNbAttemptsReached(object sender, EventArgs e)
    {
        Debug.Log("Max reconnection attempts reached");
    }




    // Expose Rainbow Application singleton instance
    public Rainbow.Application GetRainbowApplication()
    {
        return rbApplication;
    }




    private void Update()
    {
        // This is to inform when initialization is performed and execute the function from Update() because when I run the coroutine from
        // the event handler RbApplication_InitializationPerformed or the callback method (not sure which), it didn't execute due to threading issues bcz it
        // wasn't executed in the main thread and coroutines need to execute only in the main thread.

        // When i called the FetchCurrentProfile() from the event handler it didn't execute the coroutine or the rest of the code. When i called the entire
        //FetchCurrentProfile() from the Update() it didn't execute the coroutine but executed the rest of the code in FetchCurrentProfile() and when i called
        //the FetchCurrentProfile() from the event handler but only called the coroutine from the Update() it worked correctly.
        if (initializationPerformedFlag)
        {
            StartCoroutine(HandleAvatarData(avatarData));
            initializationPerformedFlag = false;


            GetComponent<ConversationsAndContacts>().InitializeConversationsAndContacts();
            GetComponent<BubbleManager>().InitializeBubblesManager();
        }
    }




    // Fetch current profile
    public void FetchCurrentProfile()
    {
        Contact myContact = rbContacts.GetCurrentContact();        
        if (myContact != null)
        {
            Debug.Log($"First Name: {myContact.FirstName}");
            Debug.Log($"Last Name: {myContact.LastName}");
            Debug.Log($"Display Name: {myContact.DisplayName}");

            // Get avatar
            rbContacts.GetAvatarFromCurrentContact(80, callback =>
            {
                if (callback.Result.Success)
                {
                    avatarData = callback.Data;
                    initializationPerformedFlag = true;
                }
                else
                {
                    Debug.LogError("Failed to fetch avatar.");
                }
            });


            // Get presence
            Presence myPresence = rbContacts.GetPresenceFromCurrentContact();
            Debug.Log($"Presence: {myPresence.PresenceLevel}");
        }
        else
        {
            Debug.LogError("Failed to fetch current contact.");
        }
    }




    // Coroutine to process avatar data and apply it to a SpriteRenderer
    private IEnumerator HandleAvatarData(byte[] avatarData)
    {
        // Convert byte[] to Texture2D on the main thread
        Texture2D avatarTexture = GetImageFromBytes(avatarData);

        if (avatarTexture != null)
        {
            Debug.Log("Avatar fetched and converted successfully.");

            // Convert Texture2D to Sprite
            Sprite avatarSprite = ConvertTextureToSprite(avatarTexture);

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = avatarSprite; // Set the sprite
            }
        }
        else
        {
            Debug.LogError("Failed to convert avatar image.");
        }

        yield return null; // Yielding to let Unity update the main thread
    }




    public Texture2D GetImageFromBytes(byte[] data)
    {
        Texture2D texture = null;

        try
        {
            // Create a new Texture2D instance
            texture = new Texture2D(5, 5); // Size will be overwritten by LoadImage
            if (texture.LoadImage(data)) // Automatically resizes the texture dimensions
            {
                Debug.Log("Image loaded successfully");
            }
            else
            {
                Debug.LogError("Image loading failed");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GetImageFromBytes] Exception: {e.Message}");
        }

        return texture;
    }




    public Sprite ConvertTextureToSprite(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("Texture is null, cannot convert to Sprite.");
            return null;
        }

        // Create a new sprite using the Texture2D
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }






    // Update profile information
    public void UpdateProfile(string firstName, string lastName)
    {
        Contact myContact = rbContacts.GetCurrentContact();
        if (myContact != null)
        {
            myContact.FirstName = firstName;
            myContact.LastName = lastName;

            rbContacts.UpdateCurrentContact(myContact, callback =>
            {
                if (callback.Result.Success)
                {
                    Debug.Log("Profile updated successfully");
                }
                else
                {
                    Debug.LogError($"Failed to update profile: {callback.Result.IncorrectUseError?.ErrorMsg}");
                }
            });
        }
    }

    // Update avatar
    public void UpdateAvatar(string imagePath)
    {
        if (File.Exists(imagePath))
        {
            byte[] avatarData = File.ReadAllBytes(imagePath);
            rbContacts.UpdateAvatarFromCurrentContact(ref avatarData, "JPG", callback =>
            {
                if (callback.Result.Success)
                {
                    Debug.Log("Avatar updated successfully");
                }
                else
                {
                    Debug.LogError($"Failed to update avatar: {callback.Result.IncorrectUseError?.ErrorMsg}");
                }
            });
        }
        else
        {
            Debug.LogError("Image file not found.");
        }
    }
}
