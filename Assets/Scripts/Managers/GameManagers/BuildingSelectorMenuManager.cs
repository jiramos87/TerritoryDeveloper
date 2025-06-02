using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingSelectorMenuManager : MonoBehaviour
{
    [Serializable]
    public class ItemType
    {
        public string name;
        public Sprite icon;
        public int price;
    }

    public GameObject itemButtonPrefab;
    public Transform content;

    public string populatedWith;

    public void PopulateItems(List<ItemType> itemList, Action<ItemType> onItemSelected, string type)
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in itemList)
        {
            CreateNewItemButton(item, onItemSelected);
        }

        populatedWith = type;
    }

    private void CreateNewItemButton(ItemType item, Action<ItemType> onItemSelected)
    {
        GameObject newButton = Instantiate(itemButtonPrefab, content);

        newButton.transform.Find("Image").GetComponent<Image>().sprite = item.icon;
        newButton.transform.Find("PriceText").GetComponent<TextMeshProUGUI>().text = item.price.ToString();
        newButton.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = item.name;

        newButton.GetComponent<Button>().onClick.AddListener(() => onItemSelected(item));
    }

    public string GetPopupType()
    {
        return populatedWith;
    }
}
