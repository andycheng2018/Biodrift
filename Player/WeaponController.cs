using Unity.Netcode;
using UnityEngine;

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
    [Range(1, 150)] public float weaponDamage;
    [Range(0.1f, 1f)] public float weaponDropChance = 0.5f;
    [Range(0, 1)] public float weaponDurability = 1;
    public Creature creature;
    public bool isPlayer;
    public bool isAI;
    public bool isItem;

    [Header("Ranged Weapon Settings")]
    public GameObject spear;
    [Range(1, 150)] public float throwForce;
    [Range(0, 5)] public float accuracy;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) { return; }

        //AI
        if (isAI && creature != null)
        {
            if (other.tag == "Player" && (creature.classes == Creature.Class.Warrior || creature.classes == Creature.Class.Guard))
            {
                GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
                Destroy(particle, 2);
                audioSource.PlayOneShot(damageSound);
                other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
            }

            if ((other.tag == "Vegetation" || other.tag == "Structure") && other.GetComponent<Resources>() != null)
            {
                other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                var resource = other.GetComponent<Resources>();
                GameObject particle = Instantiate(resource.hitEffect, transform.position, transform.rotation);
                Destroy(particle, 2);
                audioSource.PlayOneShot(resource.hitSound);
            }
        }  

        //Player
        if (isPlayer)
        {
            if (other.tag == "Enemy" || other.tag == "Villager" || other.tag == "Player")
            {
                if (type == Types.Projectile)
                {
                    GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
                    Destroy(particle, 2);
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
                else
                {
                    GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
                    Destroy(particle, 2);
                    audioSource.PlayOneShot(damageSound);
                    other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                    if (type == Types.Melee || type == Types.Shield)
                    {
                        CinemachineShake.Instance.ShakeCamera(1f, 0.1f);
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
            }

            if ((other.tag == "Vegetation" || other.tag == "Structure") && other.GetComponent<Resources>() != null)
            {
                other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                CinemachineShake.Instance.ShakeCamera(1f, 0.1f);
                var resource = other.GetComponent<Resources>();
                GameObject particle = Instantiate(resource.hitEffect, transform.position, transform.rotation);
                Destroy(particle, 2);
                audioSource.PlayOneShot(resource.hitSound);
                CheckDurability();
            }

            if (other.tag == "Chest")
            {
                other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                CinemachineShake.Instance.ShakeCamera(1f, 0.1f);
                var chest = other.GetComponent<Chest>();
                GameObject particle = Instantiate(chest.hitEffect, transform.position, transform.rotation);
                Destroy(particle, 2);
                if (chest.hitChest != null)
                    audioSource.PlayOneShot(chest.hitChest);
                CheckDurability();
            }
        }
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
