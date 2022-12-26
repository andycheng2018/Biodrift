using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public Rarity rarity;
    public enum Rarity { Common, Uncommon, Rare, Legendary };
    public enum Types { Melee, Shield, Ranged, Magic, Projectile };
    public Types type;
    public GameObject hitEffect;
    public float weaponDamage = 100;
    public float weaponDurability = 1.0f;
    public float weaponDropChance = 0.5f;
    public bool isPlayer;
    public bool isAI;
    public bool isItem;

    [Header("Ranged Weapon")]
    public GameObject spear;
    public float throwForce = 30;
    public float accuracy = 5;

    private void Update()
    {
        if (isItem && !isAI && !isPlayer)
        {
            transform.Rotate(0, 50 * Time.deltaTime, 0);
        }

        if (weaponDurability <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isAI && other.tag == "Player")
        {
            GameObject particle = Instantiate(hitEffect, other.transform.position, other.transform.rotation);
            particle.transform.SetParent(transform);
            Destroy(particle, 2);
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
        }

        if (isPlayer && other.tag == "Enemy" && !FindObjectOfType<Player>().canAttack)
        {
            GameObject particle = Instantiate(hitEffect, other.transform.position, other.transform.rotation);
            particle.transform.SetParent(transform);
            Destroy(particle, 2);
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
            weaponDurability -= 0.1f;
            if ((type == Types.Melee || type == Types.Shield) && Random.value > 0.97)
            {
                StartCoroutine(SlowMotion());
            }
        }

        if (type == Types.Projectile && isPlayer && other.tag == "Enemy")
        {
            GameObject particle = Instantiate(hitEffect, other.transform.position, other.transform.rotation);
            particle.transform.SetParent(transform);
            Destroy(particle, 2);
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
        }

        if (other.tag == "Vegetation" && other.GetComponent<Resources>() != null)
        {
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
            audioSource.PlayOneShot(other.GetComponent<Resources>().audioClip);
        }
    }

    private IEnumerator SlowMotion()
    {
        float curWeaponDamage = weaponDamage;
        weaponDamage *= 2;
        Time.timeScale = 0.5f;
        Time.fixedDeltaTime = 0.02F * Time.timeScale;
        yield return new WaitForSeconds(0.6f);
        weaponDamage = curWeaponDamage;
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02f;
    }
}
