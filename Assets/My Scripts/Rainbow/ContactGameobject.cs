using Rainbow;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContactGameobject : MonoBehaviour
{
    private Rainbow.Application rbApplication;
    private InstantMessaging instantMessaging;
    private Conversations rbConversations;
    private Contacts rbContacts;

    public Image spriteRenderer;


    // Start is called before the first frame update
    void Start()
    {
        rbApplication = RainbowManager.Instance.GetRainbowApplication();

        instantMessaging = rbApplication.GetInstantMessaging();
        rbConversations = rbApplication.GetConversations();
        rbContacts = rbApplication.GetContacts();
    }




    // Get the avatar of a contact
    public void GetContactAvatar(string contactId, int size = 80)
    {
        rbContacts.GetAvatarFromContactId(contactId, size, callback =>
        {
            if (callback.Result.Success)
            {
                byte[] avatarData = callback.Data;

                StartCoroutine(HandleAvatarData(avatarData));

                Debug.Log("Avatar retrieved successfully.");
                // Handle avatar image usage in Unity as needed (e.g., texture for UI)
            }
            else
            {
                HandleError(callback.Result);
            }
        });
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

            //if (spriteRenderer != null)
            //{
            //    spriteRenderer.sprite = avatarSprite; // Set the sprite
            //}
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



    private void HandleError(SdkError error)
    {
        // A pb occurs
        if (error.Type == SdkError.SdkErrorType.IncorrectUse)
        {
            // Bad parameters used
            Debug.LogError($"Incorrect use error: {error.IncorrectUseError.ErrorMsg}");
        }
        else
        {
            // Exception occurs
            Debug.LogError($"Exception: {error.ExceptionError}");
        }
    }
}
