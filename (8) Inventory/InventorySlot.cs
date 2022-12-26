using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Item item;
    public TMP_Text text;
    public TMP_Text amount;
    public Image icon;

    public Sprite defaultIcon;
    public string defaultText;
    private Player player;

    private void Start()
    {
        player = FindObjectOfType<Player>();
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
        }
    }

    public void Use()
    {
        if (item.itemType == Item.ItemType.Food && (player.health < player.maxHealth))
        {
            player.health += Random.Range(600, 1000);
            player.healthSlider.value = player.health / player.maxHealth;
            player.audioSource2.PlayOneShot(player.heal);

            if (item.amount == 1)
            {
                Player.Instance.Items.Remove(item);
                Destroy(gameObject);
            } else
            {
                item.amount--;
            }
        }

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
                weapon.transform.rotation = Quaternion.identity;
                weapon.transform.localScale = Vector3.one;
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
                weapon.transform.rotation = Quaternion.identity;
                weapon.transform.localScale = Vector3.one;
                player.curWeapon = weapon.GetComponent<WeaponController>();
                item = thisItem;
                icon.sprite = thisSprite;
                text.text = thisText;
            }
            player.curWeapon.transform.rotation = Quaternion.identity;
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
                weapon.transform.rotation = Quaternion.identity;
                weapon.transform.localScale = Vector3.one;
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
                weapon.transform.rotation = Quaternion.identity;
                weapon.transform.localScale = Vector3.one;
                player.curShield = weapon.GetComponent<WeaponController>();
                item = thisItem;
                icon.sprite = thisSprite;
                text.text = thisText;
            }
            player.curShield.transform.rotation = Quaternion.identity;
        }

        if (item.itemType == Item.ItemType.Building)
        {
            player.SelectObject(item);
            item.amount--;
            if (item.amount <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void Remove()
    {
        var weapon = item.gameObject;
        weapon.SetActive(true);
        weapon.transform.position = player.transform.position - new Vector3(0, 3, 0);
        weapon.transform.parent = null;
        weapon.transform.localScale = new Vector3(11, 11, 11);
        Player.Instance.Items.Remove(item);
        Destroy(gameObject);
        if (weapon.GetComponent<WeaponController>() != null)
        {
            weapon.GetComponent<WeaponController>().isPlayer = false;
            weapon.GetComponent<WeaponController>().isAI = false;
            weapon.GetComponent<WeaponController>().isItem = true;
        }
    }

    public void DefaultSword()
    {
        if (player.curWeapon != null)
        {
            var weapon = item.gameObject;
            weapon.transform.position = player.transform.position - new Vector3(0, 3, 0);
            weapon.transform.parent = null;
            weapon.transform.localScale = new Vector3(11, 11, 11);
            weapon.transform.rotation = Quaternion.identity;
            Player.Instance.Items.Remove(item);
            item = null;
            text.text = defaultText;
            amount.text = "1";
            icon.sprite = defaultIcon;
            player.curWeapon = null;
            weapon.GetComponent<WeaponController>().isPlayer = false;
            weapon.GetComponent<WeaponController>().isAI = false;
            weapon.GetComponent<WeaponController>().isItem = true;
        }
    }

    public void DefaultShield()
    {
        if (player.curShield != null)
        {
            var weapon = item.gameObject;
            weapon.transform.position = player.transform.position - new Vector3(0, 3, 0);
            weapon.transform.parent = null;
            weapon.transform.localScale = new Vector3(11, 11, 11);
            weapon.transform.rotation = Quaternion.identity;
            Player.Instance.Items.Remove(item);
            item = null;
            text.text = defaultText;
            amount.text = "1";
            icon.sprite = defaultIcon;
            player.curShield = null;
            weapon.GetComponent<WeaponController>().isPlayer = false;
            weapon.GetComponent<WeaponController>().isAI = false;
            weapon.GetComponent<WeaponController>().isItem = true;
        }
    }
}
