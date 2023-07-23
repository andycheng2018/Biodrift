using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("Inventory Slot")]
    public Image background;
    public Image icon;
    public TMP_Text text;
    public TMP_Text amount;
    public TMP_Text equipText;
    public Slider durabilitySlider;
    public GameObject selectionPanel;
    public Button button;
    public Color normal;
    public Color selected;
    [HideInInspector] public Item item;
    [HideInInspector] public Player player;

    private void Start()
    {
        player = Player.instance;
        background.color = normal;
    }

    private void Update()
    {
        if (item == null) { return; }

        //Update Count
        amount.text = item.networkAmount.Value + "/" + item.amount.y;

        //Hide Count
        if (item.networkAmount.Value == 1)
        {
            amount.enabled = false;
        }
        else
        {
            amount.enabled = true;
        }

        //Destroy if equal to zero
        if (item.networkAmount.Value <= 0)
        {
            player.Items.Remove(item);
            Destroy(gameObject);
        }

        if (item.itemType == Item.ItemType.Weapon)
        {
            var weaponController = item.GetComponent<WeaponController>();
            durabilitySlider.value = weaponController.weaponDurability;

            //Hide durability slider if value is 1
            if (durabilitySlider.value == 1)
            {
                durabilitySlider.gameObject.SetActive(false);
            }
            else
            {
                durabilitySlider.gameObject.SetActive(true);
            }
        } else
        {
            durabilitySlider.gameObject.SetActive(false);
        }

        if (item.itemType == Item.ItemType.Food)
        {
            background.color = normal;
            equipText.text = "Consume";
        }

        if (item.itemType == Item.ItemType.Resource)
        {
            background.color = normal;
            equipText.text = "Cancel";
        }

        if (item.itemType == Item.ItemType.Building)
        {
            background.color = normal;
            equipText.text = "Place";
        }

        if (item.itemType == Item.ItemType.Weapon)
        {
            WeaponController thisWeapon = item.GetComponent<WeaponController>();
            if ((player.curShield != null && player.curShield == thisWeapon) || (player.curWeapon != null && player.curWeapon == thisWeapon))
            {
                background.color = selected;
                equipText.text = "Unequip";
            } else
            {
                background.color = normal;
                equipText.text = "Equip";
            }
        }

        if (item.itemType == Item.ItemType.Armor)
        {
            Armor thisArmor = item.GetComponent<Armor>();
            if ((player.currentHelmet != null && player.currentHelmet == thisArmor) || (player.currentChestplate != null && player.currentChestplate == thisArmor) || (player.currentLeggings != null && player.currentLeggings == thisArmor) || (player.currentBoots != null && player.currentBoots == thisArmor))
            {
                background.color = selected;
                equipText.text = "Unequip";
            }
            else
            {
                background.color = normal;
                equipText.text = "Equip";
            }
        }
    }

    public void SelectionPanel1()
    {
        if (selectionPanel.activeSelf)
        {
            selectionPanel.SetActive(false);
        } else
        {
            selectionPanel.SetActive(true);
        }
    }

    public void Equip()
    {
        if (item == null) { return; }

        if (item.itemType == Item.ItemType.Weapon)
        {
            WeaponController thisWeapon = item.GetComponent<WeaponController>();
            item.setActiveServerRpc(true);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.GetComponent<Rigidbody>().isKinematic = true;

            if (thisWeapon.type == WeaponController.Types.Shield)
            {
                if (player.curShield == thisWeapon)
                {
                    player.curShield.ChangeParentServerRpc(3, player.GetComponent<NetworkObject>());
                    player.curShield.GetComponent<Item>().setActiveServerRpc(false);
                    player.curShield = null;
                } else if (player.curShield != null) {
                    player.curShield.GetComponent<Item>().setActiveServerRpc(false);
                    player.curShield = thisWeapon;
                    player.curShield.ChangeParentServerRpc(2, player.GetComponent<NetworkObject>());
                } else
                {
                    player.curShield = thisWeapon;
                    player.curShield.ChangeParentServerRpc(2, player.GetComponent<NetworkObject>());
                }
            } 
            else
            {
                if (player.curWeapon == thisWeapon)
                {
                    player.curWeapon.ChangeParentServerRpc(3, player.GetComponent<NetworkObject>());
                    player.curWeapon.GetComponent<Item>().setActiveServerRpc(false);
                    player.curWeapon = null;
                }
                else if (player.curWeapon != null)
                {
                    player.curWeapon.GetComponent<Item>().setActiveServerRpc(false);
                    player.curWeapon = thisWeapon;
                    player.curWeapon.ChangeParentServerRpc(1, player.GetComponent<NetworkObject>());
                }
                else
                {
                    player.curWeapon = thisWeapon;
                    player.curWeapon.ChangeParentServerRpc(1, player.GetComponent<NetworkObject>());
                }
            }
        }

        if (item.itemType == Item.ItemType.Armor)
        {
            Armor thisArmor = item.GetComponent<Armor>();
            item.setActiveServerRpc(true);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.GetComponent<Rigidbody>().isKinematic = true;

            if (thisArmor.armorType == Armor.ArmorType.Helmet)
            {
                if (player.currentHelmet == thisArmor)
                {
                    player.currentHelmet.GetComponent<Item>().setActiveServerRpc(false);
                    player.currentHelmet = null;
                    HideArmor();
                } else
                {
                    player.currentHelmet = thisArmor;
                    for (int i = 0; i < player.helmet.Length; i++)
                    {
                        player.helmet[i].SetActive(false);
                    }
                    if (thisArmor.rarity == Armor.Rarity.Stone)
                    {
                        player.helmet[0].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Gold)
                    {
                        player.helmet[1].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Iron)
                    {
                        player.helmet[2].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Sapphire)
                    {
                        player.helmet[3].SetActive(true);
                    }
                }
            }
            else if (thisArmor.armorType == Armor.ArmorType.Chestplate)
            {
                if (player.currentChestplate == thisArmor)
                {
                    player.currentChestplate.GetComponent<Item>().setActiveServerRpc(false);
                    player.currentChestplate = null;
                    HideArmor();
                } else
                {
                    player.currentChestplate = thisArmor;
                    for (int i = 0; i < player.chestplate.Length; i++)
                    {
                        player.chestplate[i].SetActive(false);
                    }
                    if (thisArmor.rarity == Armor.Rarity.Stone)
                    {
                        player.chestplate[0].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Gold)
                    {
                        player.chestplate[1].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Iron)
                    {
                        player.chestplate[2].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Sapphire)
                    {
                        player.chestplate[3].SetActive(true);
                    }
                }
            }
            else if (thisArmor.armorType == Armor.ArmorType.Leggings)
            {
                if (player.currentLeggings == thisArmor)
                {
                    player.currentLeggings.GetComponent<Item>().setActiveServerRpc(false);
                    player.currentLeggings = null;
                    HideArmor();
                } else
                {
                    player.currentLeggings = thisArmor;
                    for (int i = 0; i < player.leggings.Length; i++)
                    {
                        player.leggings[i].SetActive(false);
                    }
                    if (thisArmor.rarity == Armor.Rarity.Stone)
                    {
                        player.leggings[0].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Gold)
                    {
                        player.leggings[1].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Iron)
                    {
                        player.leggings[2].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Sapphire)
                    {
                        player.leggings[3].SetActive(true);
                    }
                }
            }
            else if (thisArmor.armorType == Armor.ArmorType.Boots)
            {
                if (player.currentBoots == thisArmor)
                {
                    player.currentBoots.GetComponent<Item>().setActiveServerRpc(false);
                    player.currentBoots = null;
                    HideArmor();
                }
                else
                {
                    player.currentBoots = thisArmor;
                    for (int i = 0; i < player.boots.Length; i++)
                    {
                        player.boots[i].SetActive(false);
                        player.boots2[i].SetActive(false);
                    }
                    if (thisArmor.rarity == Armor.Rarity.Stone)
                    {
                        player.boots[0].SetActive(true);
                        player.boots2[0].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Gold)
                    {
                        player.boots[1].SetActive(true);
                        player.boots2[1].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Iron)
                    {
                        player.boots[2].SetActive(true);
                        player.boots2[2].SetActive(true);
                    }
                    else if (thisArmor.rarity == Armor.Rarity.Sapphire)
                    {
                        player.boots[3].SetActive(true);
                        player.boots2[3].SetActive(true);
                    }
                }   
            }
        }

        if (item.itemType == Item.ItemType.Food && (player.hungerSlider.value < 100))
        {
            player.hungerSlider.value += item.foodAmount;
            item.ChangeAmountServerRpc(-1, false);
        }

        if (item.itemType == Item.ItemType.Building)
        {
            item.transform.rotation = Quaternion.identity;
            player.currentSlot = gameObject.GetComponent<InventorySlot>();
            player.currentObject = item;
            player.SelectObject();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            player.Inventory.SetActive(false);
            player.isClosed = true;
            player.defaultContent.gameObject.SetActive(true);
            player.craftingContent.gameObject.SetActive(false);
            player.anvilContent.gameObject.SetActive(false);
            player.furnanceContent.gameObject.SetActive(false);
            for (int i = player.materialsContent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(player.materialsContent.transform.GetChild(i).gameObject);
            }
            player.craftingButton.recipe = null;
            player.craftingButton.icon.sprite = null;
            player.craftingButton.text.text = "Craft Item";
        }

        if (item.itemType == Item.ItemType.Resource)
        {
            selectionPanel.SetActive(false);
        }
    }

    public void Remove()
    {
        if (item == null) { return; }

        item.setActiveServerRpc(true);
        player.Items.Remove(item);
        WeaponController thisWeapon = item.GetComponent<WeaponController>();

        //Check if item is current weapon
        if (player.curWeapon != null && player.curWeapon == thisWeapon)
        {
            player.curWeapon.target = null;
            player.curWeapon = null;
        }

        //Check if item is current shield
        if (player.curShield != null && player.curShield == thisWeapon)
        {
            player.curShield.target = null;
            player.curShield = null;
        }

        //Drop item
        item.ChangePositionServerRpc(player.transform.position + new Vector3(2, 4, 2));
        item.transform.localRotation = Quaternion.identity;
        item.GetComponent<Collider>().enabled = true;
        item.GetComponent<Collider>().isTrigger = false;
        durabilitySlider.gameObject.SetActive(false);

        if (item.itemType == Item.ItemType.Weapon)
        {
            thisWeapon.setWeaponServerRpc(false, false);
        }

        if (item.itemType == Item.ItemType.Armor)
        {
            HideArmor();
        }


        if (item.GetComponent<Rigidbody>() != null)
        {
            item.GetComponent<Rigidbody>().isKinematic = false;
        }

        if (item.itemType == Item.ItemType.Building)
        {
            player.UndoPlacement();
        }

        Destroy(gameObject);
    }

    public void HideArmor()
    {
        Armor thisArmor = item.GetComponent<Armor>();
        if (thisArmor.armorType == Armor.ArmorType.Helmet)
        {
            player.currentHelmet = null;
            for (int i = 0; i < player.helmet.Length; i++)
            {
                player.helmet[i].SetActive(false);
            }
        }
        else if (thisArmor.armorType == Armor.ArmorType.Chestplate)
        {
            player.currentChestplate = null;
            for (int i = 0; i < player.chestplate.Length; i++)
            {
                player.chestplate[i].SetActive(false);
            }
        }
        else if (thisArmor.armorType == Armor.ArmorType.Leggings)
        {
            player.currentLeggings = null;
            for (int i = 0; i < player.leggings.Length; i++)
            {
                player.leggings[i].SetActive(false);
            }
        }
        else if (thisArmor.armorType == Armor.ArmorType.Boots)
        {
            player.currentBoots = null;
            for (int i = 0; i < player.boots.Length; i++)
            {
                player.boots[i].SetActive(false);
                player.boots2[i].SetActive(false);
            }
        }
    }
}


