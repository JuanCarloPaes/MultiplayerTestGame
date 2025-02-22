using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabsManager : MonoBehaviour
{
    [SerializeField] private GameObject[] tabs;
    [SerializeField] private Image[] tabButtons;
    [SerializeField] private Sprite inactiveTabBG, activeTabBG;

    public void SwtitchToTab(int tabID)
    {
        foreach(GameObject go in tabs)
        {
            go.SetActive(false);
        }
        tabs[tabID].SetActive(true);

        foreach(Image im in tabButtons)
        {
            im.sprite = inactiveTabBG;
        }
        tabButtons[tabID].sprite = activeTabBG;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
