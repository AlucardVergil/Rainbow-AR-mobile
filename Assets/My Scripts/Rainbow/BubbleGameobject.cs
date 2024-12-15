using Rainbow.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BubbleGameobject : MonoBehaviour
{
    public Bubble bubble;

    private ConferenceManager conferenceManager;


    private void Start()
    {
        conferenceManager = GameObject.Find("RainbowManagerGameObject").GetComponent<ConferenceManager>();

        GetComponent<Button>().onClick.AddListener(() => {

            StartConference();


        });
    }


    public void StartConference()
    {
        Debug.Log("Bubble id = " + bubble.Id);
        conferenceManager.StartPersonalConference(bubble.Id);
    }


}
