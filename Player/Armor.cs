using UnityEngine;

public class Armor : MonoBehaviour
{
    public ArmorType armorType;
    public enum ArmorType { Helmet, Chestplate, Leggings, Boots };
    public Rarity rarity;
    public enum Rarity { Stone, Gold, Iron, Sapphire };
    [Range(0, 1)] public float damageReductionPercentage;
    [Range(0, 1)] public float armorDurability = 1;

    public void CheckDurability()
    {
        if (rarity == Rarity.Stone)
        {
            armorDurability -= 0.025f; //40 times
        }
        else if (rarity == Rarity.Gold)
        {
            armorDurability -= 0.016f; //60 times
        }
        else if (rarity == Rarity.Iron)
        {
            armorDurability -= 0.01f; //100 times
        }
        else if (rarity == Rarity.Sapphire)
        {
            armorDurability -= 0.005f; //200 times
        }
    }
}
