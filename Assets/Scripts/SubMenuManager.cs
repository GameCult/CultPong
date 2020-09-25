using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubMenuManager : MonoBehaviour {


    public GameObject[] SubMenus;

    private GameObject _activeSubMenu;


    public void ShowMenu(int i)
    {
        if (_activeSubMenu != null)
            _activeSubMenu.SetActive(false);
        
        _activeSubMenu = SubMenus[i];
        _activeSubMenu.SetActive(true);
    }

    private void OnEnable()
    {

        ShowMenu(0);

    }

  
}
