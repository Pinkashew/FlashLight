using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private GameObject flashlightLight;
    private bool flashlightActive = false;
    public int clicked = -1;
    public bool jumpscare = false;
    public AudioSource audio;
    private bool audioPlayed = false;
    public GameObject wall1;
    public GameObject wall2;
    public GameObject wall3;
    public GameObject wall4;


    void Start()
    {
        flashlightLight.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Flashlight toggle key pressed.");
            flashlightActive = !flashlightActive;
            flashlightLight.SetActive(flashlightActive);
            clicked++;
        }

        if (clicked == 3 && !audioPlayed)
        {
            Debug.Log("Jumpscare triggered.");
            audio.Play();
            audioPlayed = true;
            jumpscare = true;
            clicked = -1;
            wall1.SetActive(false);
            wall2.SetActive(false);
            wall3.SetActive(false);
            wall4.SetActive(false);
        }
    }
}