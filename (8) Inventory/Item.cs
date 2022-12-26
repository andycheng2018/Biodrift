using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemType itemType;
    public enum ItemType { Food, Weapon, Shield, Building };
    public Sprite icon;
    public int amount;
}
