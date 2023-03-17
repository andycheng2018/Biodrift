using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Recipe : MonoBehaviour
{
    public GameObject item;
    public Image thisIcon;
    public Image icon;
    public Button button;

    public void UpdateCrafting()
    {
        icon.sprite = thisIcon.sprite;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Craft);
    }

    public void Craft()
    {
        if (Player.playerInstance.Items.Count <= 20)
        {
            Player.playerInstance.Add(item.GetComponent<Item>());
        }
    }
}
