using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UIElements;
using ZXing.QrCode.Internal;

public class PermissionAndServiceChecker : MonoBehaviour
{

    void Start()
    {
#if !UNITY_EDITOR
        // Check and request permissions
        StartCoroutine(CheckPermissions());
#endif
    }



    IEnumerator CheckPermissions()
    {
        bool cameraPermissionAsked = false;
        bool micPermissionAsked = false;


        if (!Permission.HasUserAuthorizedPermission("android.permission.CAMERA"))
        {
            if (!cameraPermissionAsked)
            {
                Permission.RequestUserPermission("android.permission.CAMERA");
                cameraPermissionAsked = true;
            }

            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission("android.permission.CAMERA"));
        }

        if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
        {
            if (!micPermissionAsked)
            {
                Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
                micPermissionAsked = true;
            }

            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"));
        }

    }

}
