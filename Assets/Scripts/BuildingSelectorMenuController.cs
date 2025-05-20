using System.Collections.Generic;
using UnityEngine;

public class BuildingSelectorMenuController : MonoBehaviour
{
    public BuildingSelectorMenuManager menuManager;
    public CursorManager cursorManager;
    public GameObject popupPanel;

    public void Start()
    {
        popupPanel.SetActive(false);
    }

    public void ShowPopup(List<BuildingSelectorMenuManager.ItemType> items, System.Action<BuildingSelectorMenuManager.ItemType> onItemSelected, string type)
    {
        TogglePopup(type);
        menuManager.PopulateItems(items, onItemSelected, type);

        cursorManager.SetDefaultCursor();
    }

    private void TogglePopup(string type)
    {
        if (popupPanel.activeSelf)
        {
            if (menuManager.GetPopupType() == type)
            {
                ClosePopup();
            }
            else
            {
                OpenPopup();
            }
        }
        else
        {
            OpenPopup();
        }
    }

    private void OpenPopup()
    {
        popupPanel.SetActive(true);
    }

    private void ClosePopup()
    {
        popupPanel.SetActive(false);
    }

    public bool IsPopupActive()
    {
        return popupPanel.activeSelf;
    }
}