using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class BuildManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject buttonPrefab;
    public GameObject[] buildObjects;

    [Header("Audiosources")]
    public AudioSource slideIn;
    public AudioSource slideOut;

    [Header("Main Panels")]
    public Transform interiorMenu;
    public Transform decorationMenu;
    public Transform groundMenu;

    [Header("Contents")]
    public Transform interiorContent;
    public Transform decorationContent;
    public Transform groundContent;

    public Animator buildAnim;

    private List<GameObject> menuObjects = new List<GameObject>();

    private List<GameObject> buttons = new List<GameObject>();

    private List<GameObject> interiorObjects = new List<GameObject>();
    private List<GameObject> decorationObjects = new List<GameObject>();
    private List<GameObject> groundObjects = new List<GameObject>();

    private int menuIndex = 0;
    private bool isOpen = false;

    void Awake()
    {
        menuObjects.Add(interiorMenu.gameObject);
        menuObjects.Add(decorationMenu.gameObject);
        menuObjects.Add(groundMenu.gameObject);

        foreach (GameObject go in buildObjects)
        {
            if (go.GetComponent<ObjectData>() != null)
            {
                ObjectType type = go.GetComponent<ObjectData>().objectType;
                GameObject instantiatedButton;

                if (type == ObjectType.Interior)
                {
                    interiorObjects.Add(go);
                    instantiatedButton = Instantiate(buttonPrefab, interiorContent);
                }
                else if (type == ObjectType.Decorations)
                {
                    decorationObjects.Add(go);
                    instantiatedButton = Instantiate(buttonPrefab, decorationContent);
                }
                else if (type == ObjectType.Ground)
                {
                    groundObjects.Add(go);
                    instantiatedButton = Instantiate(buttonPrefab, groundContent);
                }
                else
                {
                    continue;
                }

                instantiatedButton.GetComponent<Image>().sprite = go.GetComponent<SpriteRenderer>().sprite;
                instantiatedButton.GetComponent<ObjectToPlace>().objectToPlace = go;
            }
        }

        for (int i = 0; i < menuObjects.Count; i++)
        {
            menuObjects[i].SetActive(i == 0);
        }
    }

    private void Update()
    {
        for (int i = 0; i < menuObjects.Count; i++)
        {
            menuObjects[i].SetActive(i == menuIndex);
        }

        if (Input.GetKeyDown(KeyCode.Tab) && !isOpen)
        {
            isOpen = true;
            buildAnim.SetTrigger("TriggerIn");
            slideIn.Play();
        }
        else if (Input.GetKeyDown(KeyCode.Tab) && isOpen)
        {
            isOpen = false;
            buildAnim.SetTrigger("TriggerOut");
            slideOut.Play();
        }
    }

    public void Next()
    {
        if (menuIndex < menuObjects.Count - 1)
        {
            menuObjects[menuIndex].SetActive(false); 
            menuIndex++;
            menuObjects[menuIndex].SetActive(true); 
        }
    }

    public void Previous()
    {
        if (menuIndex > 0)
        {
            menuObjects[menuIndex].SetActive(false); 
            menuIndex--;
            menuObjects[menuIndex].SetActive(true); 
        }
    }
}
