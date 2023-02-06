using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemType itemType;
    public enum ItemType { Weapon, Shield, Food, Resource, Building };
    public Sprite icon;
    public int amount;
    public int maxAmount;
    public int healAmount;
    public bool isSpinning;

    private void Update()
    {
        if (isSpinning)
        {
            transform.Rotate(0, 50 * Time.deltaTime, 0);
        }
    }
}
