using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Item item;
    public TMP_Text text;
    public TMP_Text amount;
    public Image icon;
    public Slider durabilitySlider;
    public GameObject itemObject;

    public Sprite defaultIcon;
    public string defaultText;
    private Player player;
    private Vector3 scale;

    private void Start()
    {
        player = Player.playerInstance;
        if (item != null)
        {
            itemObject = item.gameObject;
            scale = itemObject.transform.localScale;
        }
    }

    private void Update()
    {
        if (item != null)
        {
            amount.text = item.amount.ToString();
            if (item.amount == 1)
            {
                amount.enabled = false;
            }
            else
            {
                amount.enabled = true;
            }

            if (item.itemType == Item.ItemType.Weapon || item.itemType == Item.ItemType.Shield)
            {
                var weaponController = itemObject.GetComponent<WeaponController>();
                durabilitySlider.value = weaponController.weaponDurability;
                if (durabilitySlider.value == 0)
                {
                    if (weaponController.type == WeaponController.Types.Shield)
                    {
                        if (player.curShield != null)
                        {
                            Destroy(itemObject);
                            Player.playerInstance.Items.Remove(item);
                            item = null;
                            text.text = defaultText;
                            amount.text = "1";
                            icon.sprite = defaultIcon;
                            player.curShield = null;
                        }
                    }
                    else
                    {
                        if (player.curWeapon != null)
                        {
                            Destroy(itemObject);
                            Player.playerInstance.Items.Remove(item);
                            item = null;
                            text.text = defaultText;
                            amount.text = "1";
                            icon.sprite = defaultIcon;
                            player.curWeapon = null;
                        }
                    }
                }

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
        } else
        {
            durabilitySlider.gameObject.SetActive(false);
        }
    }

    public void Use()
    {
        if (item.itemType == Item.ItemType.Weapon)
        {
            if (player.curWeapon == null)
            {
                player.sword.item = item;
                player.sword.icon.sprite = item.icon;
                player.sword.text.text = item.name;
                var weapon = item.gameObject;
                weapon.SetActive(true);
                weapon.transform.SetParent(player.hand);
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;
                weapon.transform.localScale = new Vector3(10, 10, 10);
                player.curWeapon = weapon.GetComponent<WeaponController>();
                Destroy(gameObject);
            }
            else
            {
                var thisItem = player.sword.item;
                var thisSprite = player.sword.item.icon;
                var thisText = player.sword.item.name;
                player.sword.item = item;
                player.sword.icon.sprite = item.icon;
                player.sword.text.text = item.name;
                player.curWeapon.gameObject.SetActive(false);
                var weapon = item.gameObject;
                weapon.SetActive(true);
                weapon.transform.SetParent(player.hand);
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;
                weapon.transform.localScale = new Vector3(10, 10, 10);
                player.curWeapon = weapon.GetComponent<WeaponController>();
                item = thisItem;
                icon.sprite = thisSprite;
                text.text = thisText;
            }
        }

        if (item.itemType == Item.ItemType.Shield)
        {
            if (player.curShield == null)
            {
                player.shield.item = item;
                player.shield.icon.sprite = item.icon;
                player.shield.text.text = item.name;
                var weapon = item.gameObject;
                weapon.SetActive(true);
                weapon.transform.SetParent(player.offhand);
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;
                weapon.transform.localScale = new Vector3(10, 10, 10);
                player.curShield = weapon.GetComponent<WeaponController>();
                Destroy(gameObject);
            }
            else
            {
                var thisItem = player.shield.item;
                var thisSprite = player.shield.item.icon;
                var thisText = player.shield.item.name;
                player.shield.item = item;
                player.shield.icon.sprite = item.icon;
                player.shield.text.text = item.name;
                player.curShield.gameObject.SetActive(false);
                var weapon = item.gameObject;
                weapon.SetActive(true);
                weapon.transform.SetParent(player.offhand);
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;
                weapon.transform.localScale = new Vector3(10, 10, 10);
                player.curShield = weapon.GetComponent<WeaponController>();
                item = thisItem;
                icon.sprite = thisSprite;
                text.text = thisText;
            }
        }

        if (item.itemType == Item.ItemType.Food /*&& (player.health.Value < player.maxHealth)*/)
        {
            //player.health.Value += item.healAmount;
            //player.healthSlider.value = player.health.Value / player.maxHealth;
            player.audioSource2.PlayOneShot(player.heal);

            if (item.amount == 1)
            {
                Player.playerInstance.Items.Remove(item);
                Destroy(gameObject);
            } else
            {
                item.amount--;
            }
        }

        if (item.itemType == Item.ItemType.Building)
        {
            player.SelectObject(item.gameObject);
            if (item.amount == 1)
            {
                Player.playerInstance.Items.Remove(item);
                Destroy(gameObject);
            } else
            {
                item.amount--;
            }
        }
    }

    public void Remove()
    {
        item.setActiveServerRpc(true);
        itemObject.transform.position = player.transform.position - new Vector3(0, 3, 0);
        itemObject.transform.localRotation = Quaternion.identity;
        itemObject.transform.localScale = scale;
        itemObject.GetComponent<Collider>().isTrigger = true;
        itemObject.tag = "Item";
        Player.playerInstance.Items.Remove(item);
        Destroy(gameObject);
        if (itemObject.GetComponent<WeaponController>() != null)
        {
            itemObject.GetComponent<WeaponController>().isPlayer = false;
            itemObject.GetComponent<WeaponController>().isAI = false;
            itemObject.GetComponent<WeaponController>().isItem = true;
        }
    }

    public void DefaultSword()
    {
        if (player.curWeapon != null)
        {
            itemObject.transform.position = player.transform.position - new Vector3(0, 3, 0);
            itemObject.transform.parent = null;
            itemObject.transform.localRotation = Quaternion.identity;
            itemObject.transform.localScale = new Vector3(10, 10, 10);
            Player.playerInstance.Items.Remove(item);
            item = null;
            text.text = defaultText;
            amount.text = "1";
            icon.sprite = defaultIcon;
            player.curWeapon = null;
            itemObject.GetComponent<WeaponController>().isPlayer = false;
            itemObject.GetComponent<WeaponController>().isAI = false;
            itemObject.GetComponent<WeaponController>().isItem = true;
            itemObject.GetComponent<Collider>().isTrigger = true;
        }
    }

    public void DefaultShield()
    {
        if (player.curShield != null)
        {
            itemObject.transform.position = player.transform.position - new Vector3(0, 3, 0);
            itemObject.transform.parent = null;
            itemObject.transform.localRotation = Quaternion.identity;
            itemObject.transform.localScale = new Vector3(10, 10, 10);
            Player.playerInstance.Items.Remove(item);
            item = null;
            text.text = defaultText;
            amount.text = "1";
            icon.sprite = defaultIcon;
            player.curShield = null;
            itemObject.GetComponent<WeaponController>().isPlayer = false;
            itemObject.GetComponent<WeaponController>().isAI = false;
            itemObject.GetComponent<WeaponController>().isItem = true;
            itemObject.GetComponent<Collider>().isTrigger = true;
        }
    }
}
