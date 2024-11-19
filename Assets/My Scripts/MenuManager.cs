using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject[] panels;
    public GameObject[] chatPanels;


   
    private void Start()
    {
        // Disable all panels except the 1st one which will be the rainbow login panel
        for (int i = 1; i < panels.Length; i++)
        {
            panels[i].SetActive(false);
        }

        for (int i = 1; i < chatPanels.Length; i++)
        {
            chatPanels[i].SetActive(false);
        }
    }


    public void OpenCloseMenuPanel(int panelIndex)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(false);
        }

        panels[panelIndex].SetActive(true);
    }



    public void OpenCloseChatPanels(int panelIndex)
    {
        for (int i = 0; i < chatPanels.Length; i++)
        {
            chatPanels[i].SetActive(false);
        }

        chatPanels[panelIndex].SetActive(true);
    }





}
