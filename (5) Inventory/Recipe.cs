using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Recipe : MonoBehaviour
{
    public Player player;
    public Image image;
    public TMP_Text planks;
    public TMP_Text stone;
    public TMP_Text sticks;

    public GameObject building;
    public Image item;
    public Button button;
    public int planksAmount;
    public int stoneAmount;
    public int sticksAmount;

    public void UpdateCrafting()
    {
        image.sprite = item.sprite;
        planks.text = "x" + planksAmount + " Planks";
        stone.text = "x" + stoneAmount + " Stone";
        sticks.text = "x" + sticksAmount + " Sticks";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Craft);
    }

    public void Craft()
    {
        /*if (player.planks >= planksAmount && player.stone >= stoneAmount && player.sticks >= sticksAmount)
        {
            player.Add(building.GetComponentInChildren<Item>());
        }*/
        //building.GetComponentInChildren<Item>().amount = 1;
        player.Add(building.GetComponentInChildren<Item>());
    }
}
