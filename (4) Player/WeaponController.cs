using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
public class WeaponController : NetworkBehaviour
{
    [Header("Weapon Settings")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip damageSound;
    public GameObject hitEffect;
    public Rarity rarity;
    public enum Rarity { Common, Uncommon, Rare, Legendary, Mythical };
    public enum Types { Melee, Shield, Ranged, Magic, Projectile };
    public Types type;
    [Range(1, 1000)] public float weaponDamage;
    [Range(0.1f, 1f)] public float weaponDropChance = 0.5f;
    [SyncVar]
    [Range(0, 1)] public float weaponDurability = 1;
    public Creature creature;
    public bool isPlayer;
    public bool isAI;
    public bool isItem;
    public Player player;

    [Header("Ranged Weapon Settings")]
    public GameObject spear;
    [Range(1, 150)] public float throwForce;
    [Range(0, 5)] public float accuracy;

    private void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].GetComponent<Player>().isLocalPlayer)
            {
                player = players[i].GetComponent<Player>();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (creature != null && isAI && other.tag == "Player" && !creature.canAttack && (creature.classes == Creature.Class.Warrior || creature.classes == Creature.Class.Guard))
        {
            GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
            NetworkServer.Spawn(particle);
            StartCoroutine(player.CmdDestroyObject(particle, 2));
            audioSource.PlayOneShot(damageSound);
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
        }

        if (isPlayer && (other.tag == "Enemy" || other.tag == "Villager") && !player.canAttack)
        {
            GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
            NetworkServer.Spawn(particle);
            StartCoroutine(player.CmdDestroyObject(particle, 2));
            audioSource.PlayOneShot(damageSound);
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
            if (type == Types.Melee || type == Types.Shield)
            {
                CinemachineShake.Instance.ShakeCamera(1.5f, 0.2f);
                if (Random.value > 0.97)
                {
                    StartCoroutine(SlowMotion());
                }
            }

            if (other.tag == "Villager")
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, 300);
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider.GetComponent<Creature>() != null)
                    {
                        hitCollider.GetComponent<Creature>().isProvoked = true;
                    }
                }
            }
            CheckDurability();
        }

        if (type == Types.Projectile && isPlayer && (other.tag == "Enemy" || other.tag == "Villager"))
        {
            GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
            NetworkServer.Spawn(particle);
            StartCoroutine(player.CmdDestroyObject(particle, 2));
            audioSource.PlayOneShot(damageSound);
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);

            if (other.tag == "Villager")
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, 300);
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider.GetComponent<Creature>() != null)
                    {
                        hitCollider.GetComponent<Creature>().isProvoked = true;
                    }
                }
            }
        }

        if (isPlayer && (other.tag == "Vegetation" || other.tag == "Structure") && other.GetComponent<Resources>() != null && !player.canAttack)
        {
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
            CinemachineShake.Instance.ShakeCamera(1f, 0.2f);
            var resource = other.GetComponent<Resources>();
            GameObject particle = Instantiate(resource.hitEffect, transform.position, transform.rotation);
            NetworkServer.Spawn(particle);
            StartCoroutine(player.CmdDestroyObject(particle, 2));
            audioSource.PlayOneShot(resource.hitSound);
            CheckDurability();
        }

        if (isPlayer && other.tag == "Chest" && !player.canAttack)
        {
            other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
            CinemachineShake.Instance.ShakeCamera(1f, 0.2f);
            var chest = other.GetComponent<Chest>();
            GameObject particle = Instantiate(chest.hitEffect, transform.position, transform.rotation);
            NetworkServer.Spawn(particle);
            StartCoroutine(player.CmdDestroyObject(particle, 2));
            if (chest.hitChest != null)
                audioSource.PlayOneShot(chest.hitChest);
            CheckDurability();
        }

        if (isPlayer && other.tag == "OtherPlayer" && !player.canAttack)
        {
            GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
            NetworkServer.Spawn(particle);
            StartCoroutine(player.CmdDestroyObject(particle, 2));
            audioSource.PlayOneShot(damageSound);
            other.gameObject.GetComponent<Player>().CmdTakeDamage(weaponDamage);
            if (type == Types.Melee || type == Types.Shield)
            {
                CinemachineShake.Instance.ShakeCamera(1.5f, 0.2f);
                if (Random.value > 0.97)
                {
                    StartCoroutine(SlowMotion());
                }
            }
            CheckDurability();
        }
    }

    private IEnumerator SlowMotion()
    {
        float curWeaponDamage = weaponDamage;
        weaponDamage *= 2;
        Time.timeScale = 0.5f;
        Time.fixedDeltaTime = 0.02F * Time.timeScale;
        CinemachineShake.Instance.ShakeCamera(1f, 0.1f);
        yield return new WaitForSeconds(0.5f);
        weaponDamage = curWeaponDamage;
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02f;
    }

    public void CheckDurability()
    {
        if (rarity == Rarity.Common)
        {
            weaponDurability -= 0.05f; //20 times
        }
        else if (rarity == Rarity.Uncommon)
        {
            weaponDurability -= 0.025f; //40 times
        }
        else if (rarity == Rarity.Rare)
        {
            weaponDurability -= 0.016f; //60 times
        }
        else if (rarity == Rarity.Legendary)
        {
            weaponDurability -= 0.01f; //100 times
        }
        else if (rarity == Rarity.Mythical)
        {
            weaponDurability -= 0.005f; //200 times
        }
    }
}
