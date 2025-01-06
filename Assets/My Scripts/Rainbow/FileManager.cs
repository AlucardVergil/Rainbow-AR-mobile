using System;
using System.Collections.Generic;
using UnityEngine;
using Rainbow;
using Rainbow.Model;
using Rainbow.Events;
using Cortex;
using UnityEditor;
using TMPro;


public class FileManager : MonoBehaviour
{
    private FileStorage fileStorage;
    private InstantMessaging instantMessaging;
    private string fileDescriptorId; // To track file upload progress

    public TMP_Text uploadFilePath;
    public TMP_Text downloadFilePath;



    public void InitializeFileManager() // Probably will need to assign the variables in the other function bcz they are called too early and not assigned (TO CHECK)
    {
        ConnectionModel model = ConnectionModel.Instance;
        
        instantMessaging = model.InstantMessaging;
        fileStorage = model.FileStorage;

        // Subscribe to file upload progress updates
        fileStorage.FileUploadUpdated += FileUploadUpdatedHandler;
        fileStorage.FileDownloadUpdated += FileDownloadUpdatedHandler;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (fileStorage != null)
        {
            fileStorage.FileUploadUpdated -= FileUploadUpdatedHandler;
            fileStorage.FileDownloadUpdated -= FileDownloadUpdatedHandler;
        }
    }



    public void OpenUploadFileDialog()
    {
        // Open file panel
        string path = EditorUtility.OpenFilePanel("Choose a File", "", "*");

        // Check if a file was selected
        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log($"File selected: {path}");
            
            uploadFilePath.text = "File Selected: " + path;
        }
    }



    // Get all received file descriptors
    public void GetAllReceivedFiles()
    {
        fileStorage.GetAllFilesDescriptorsReceived(callback =>
        {
            if (callback.Result.Success)
            {
                List<FileDescriptor> fileDescriptors = callback.Data;
                foreach (var file in fileDescriptors)
                {
                    Debug.Log($"File Received - ID: {file.Id}, Name: {file.FileName}, Size: {file.Size} bytes");
                }
            }
            else
            {
                HandleError(callback.Result);
            }
        });
    }

    // Share a file with a conversation
    public void ShareFileWithConversation(Conversation conversation, string filePath, string message = "")
    {
        instantMessaging.SendMessageWithFileToConversation(conversation, message, filePath, null, UrgencyType.Std, null,
        callbackFileDescriptor =>
        {
            if (callbackFileDescriptor.Result.Success)
            {
                var fileDescriptor = callbackFileDescriptor.Data;
                fileDescriptorId = fileDescriptor.Id;
                Debug.Log($"FileDescriptor created. Upload started. ID: {fileDescriptorId}");
            }
            else
            {
                HandleError(callbackFileDescriptor.Result);
            }
        },
        callbackMessage =>
        {
            if (callbackMessage.Result.Success)
            {
                Debug.Log("File and message successfully sent to the conversation.");
            }
            else
            {
                HandleError(callbackMessage.Result);
            }
        });
    }

    


    #region List of files (received, sent or both) by conversation

    public void GetFilesReceived(string conversationId)
    {
        fileStorage.GetFilesDescriptorReceivedInConversationId(conversationId, callback =>
        {
            if (callback.Result.Success)
            {
                List<FileDescriptor> files = callback.Data;
                // Process the received files
            }
            else
            {
                Debug.LogError("Error retrieving received files: " + callback.Result);
            }
        });
    }

    public void GetFilesSent(string conversationId)
    {
        fileStorage.GetFilesDescriptorSentInConversationId(conversationId, callback =>
        {
            if (callback.Result.Success)
            {
                List<FileDescriptor> files = callback.Data;
                // Process the sent files
            }
            else
            {
                Debug.LogError("Error retrieving sent files: " + callback.Result);
            }
        });
    }

    public void GetAllFiles(string conversationId)
    {
        fileStorage.GetFilesDescriptorInConversationId(conversationId, callback =>
        {
            if (callback.Result.Success)
            {
                List<FileDescriptor> files = callback.Data;
                // Process all files                
            }
            else
            {
                Debug.LogError("Error retrieving all files: " + callback.Result);
            }
        });
    }


    #endregion



    #region How to share/upload a file without to send an IM message and Download file



    public void UploadFileOnFilesPanel()
    {
        //UploadFileWithoutMessage(downloadFilePath);
    }



    public void UploadFileWithoutMessage(string filePath, Conversation conversation)
    {
        string fileDescriptorId = null;        

        fileStorage.UploadFile(filePath, conversation.PeerId, conversation.Type,
            callbackFileDescriptor =>
            {
                if (callbackFileDescriptor.Result.Success)
                {
                    FileDescriptor fileDescriptor = callbackFileDescriptor.Data;
                    fileDescriptorId = fileDescriptor.Id;
                    Debug.Log($"File descriptor created: {fileDescriptorId}");
                }
                else
                {
                    Debug.LogError("Error creating file descriptor: " + callbackFileDescriptor.Result);
                }
            },
            callbackResult =>
            {
                if (callbackResult.Result.Success)
                {
                    Debug.Log("File uploaded successfully.");
                }
                else
                {
                    Debug.LogError("Error uploading file: " + callbackResult.Result);
                }
            });
    }



    public void DownloadSharedFile(string fileDescriptorId, string destinationFolder, string destinationFileName)
    {
        fileStorage.DownloadFile(fileDescriptorId, destinationFolder, destinationFileName, callback =>
        {
            if (callback.Result.Success)
            {
                Debug.Log("File download started successfully.");
            }
            else
            {
                Debug.LogError("Error starting file download: " + callback.Result);
            }
        });
    }


    #endregion







    // Utility method to handle errors
    private void HandleError(SdkError error)
    {
        if (error.Type == SdkError.SdkErrorType.IncorrectUse)
        {
            Debug.LogError("Error: " + error.IncorrectUseError.ErrorMsg);
        }
        else if (error.Type == SdkError.SdkErrorType.Exception)
        {
            Debug.LogError("Exception: " + error.ExceptionError.Message);
        }
    }


    // Follow file upload progress
    private void FileUploadUpdatedHandler(object sender, FileUploadEventArgs evt)
    {
        if (evt.FileDescriptor.Id == fileDescriptorId)
        {
            if (evt.InProgress)
            {
                Debug.Log($"Uploading: {evt.SizeUploaded} bytes uploaded so far.");
            }
            else if (evt.Completed)
            {
                Debug.Log("File upload completed successfully.");
            }
            else
            {
                Debug.LogWarning("File upload stopped or failed.");
            }
        }
    }


    // Follow file download progress
    private void FileDownloadUpdatedHandler(object sender, FileDownloadEventArgs evt)
    {
        if (evt.FileId == fileDescriptorId)
        {
            if (evt.InProgress)
            {
                Debug.Log($"Downloading: {evt.SizeDownloaded}/{evt.FileSize} bytes");
            }
            else if (evt.Completed)
            {
                Debug.Log("File download completed successfully.");
            }
        }
    }
}
