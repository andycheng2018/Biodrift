using System;
using Unity.Netcode;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    [Header("Weapon Settings")]
    public AudioClip attackSound;
    public AudioClip damageSound;
    public GameObject hitEffect;
    public ParticleSystem weaponTrail;
    public ResourceType resourceType;
    public enum ResourceType { Wood, Rock, Flesh };
    public Rarity rarity;
    public enum Rarity { Wood, Stone, Gold, Iron, Sapphire };
    public Types type;
    public enum Types { Pickaxe, Axe, Sword, Shield, Bow };
    [Range(0, 50)] public float weaponDamage;
    [Range(0, 1)] public float weaponDurability = 1;
    [HideInInspector] public Monster monster;
    [SerializeField] public NetworkVariable<bool> isPlayer = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] public NetworkVariable<bool> isAI = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Ranged Weapon Settings")]
    public GameObject arrow;
    [Range(0, 50)] public float throwForce;
    [Range(0, 10)] public float accuracy;

    [HideInInspector] public GameObject target;
    [HideInInspector] public float weaponDropChance;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (rarity == Rarity.Wood)
        {
            weaponDropChance = 0.45f;
        }
        else if (rarity == Rarity.Stone)
        {
            weaponDropChance = 0.35f;
        }
        else if (rarity == Rarity.Gold)
        {
            weaponDropChance = 0.25f;
        }
        else if (rarity == Rarity.Iron)
        {
            weaponDropChance = 0.15f;
        }
        else if (rarity == Rarity.Sapphire)
        {
            weaponDropChance = 0.05f;
        }
    }

    private void Update()
    {
        if (!IsServer) { return; }
        if (target == null) { return; }

        transform.position = target.transform.position;
        transform.rotation = target.transform.rotation;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeParentServerRpc(int num, NetworkObjectReference networkObjectReference)
    {
        if (!IsServer) { return; }
        ChangeParentClientRpc(num, networkObjectReference);
    }

    [ClientRpc]
    public void ChangeParentClientRpc(int num, NetworkObjectReference networkObjectReference)
    {
        networkObjectReference.TryGet(out NetworkObject networkObject);
        Player player = networkObject.GetComponent<Player>();

        if (num == 1)
        {
            target = player.hand.gameObject;
        }
        else if (num == 2)
        {
            target = player.offhand.gameObject;
        }
        else if (num == 3)
        {
            target = null;
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void setWeaponServerRpc(bool isPlayer, bool isAI)
    {
        if (!IsServer) { return; }
        setWeaponClientRpc(isPlayer, isAI);
    }

    [ClientRpc]
    public void setWeaponClientRpc(bool isPlayer, bool isAI)
    {
        this.isPlayer.Value = isPlayer;
        this.isAI.Value = isAI;
    }

    private void OnTriggerEnter(Collider other)
    {
        //AI
        if (isAI.Value && monster != null)
        {
            if (other.tag == "Player")
            {
                other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                var player = other.GetComponent<Player>();
                GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
                Destroy(particle, 2);
                audioSource.PlayOneShot(damageSound);
            }
        }

        //Player
        if (isPlayer.Value)
        {
            if (other.tag == "Monster")
            {
                if (gameObject.tag == "Projectile")
                {
                    GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
                    Destroy(particle, 2);
                    audioSource.PlayOneShot(damageSound);
                    other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
                    Destroy(particle, 2);
                    audioSource.PlayOneShot(damageSound);
                    if (resourceType == ResourceType.Flesh)
                    {
                        other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                    } else
                    {
                        other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage/2, SendMessageOptions.DontRequireReceiver);
                    }

                    if (type == Types.Sword || type == Types.Shield)
                    {
                        CinemachineShake.Instance.ShakeCamera(1f, 0.1f);
                    }
                    CheckDurability();
                }
            }

            if (other.GetComponent<Resource>() != null)
            {
                if (((int)rarity >= (int)other.GetComponent<Resource>().lowestRarity) && ((int)resourceType == (int)other.GetComponent<Resource>().resourceType))
                {
                    other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                }
                CinemachineShake.Instance.ShakeCamera(1f, 0.1f);
                var resource = other.GetComponent<Resource>();
                GameObject particle = Instantiate(resource.hitEffect, transform.position, transform.rotation);
                Destroy(particle, 2);
                audioSource.PlayOneShot(resource.hitSound);
                CheckDurability();
                gameObject.GetComponent<Collider>().enabled = false;
            }

            //other.gameObject.GetComponent<NetworkObject>() != NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject
            if (other.tag == "Player")
            {
                if (other.GetComponent<Player>().playerCamera == isActiveAndEnabled) { return; }
                other.gameObject.SendMessageUpwards("ChangeHealth", -weaponDamage, SendMessageOptions.DontRequireReceiver);
                CinemachineShake.Instance.ShakeCamera(1f, 0.1f);
                GameObject particle = Instantiate(hitEffect, transform.position, transform.rotation);
                Destroy(particle, 2);
                audioSource.PlayOneShot(damageSound);
                CheckDurability();
                gameObject.GetComponent<Collider>().enabled = false;
            }
        }
    }

    public void CheckDurability()
    {
        if (rarity == Rarity.Wood)
        {
            weaponDurability -= 0.05f; //20 times
        }
        else if (rarity == Rarity.Stone)
        {
            weaponDurability -= 0.025f; //40 times
        }
        else if (rarity == Rarity.Gold)
        {
            weaponDurability -= 0.016f; //60 times
        }
        else if (rarity == Rarity.Iron)
        {
            weaponDurability -= 0.01f; //100 times
        }
        else if (rarity == Rarity.Sapphire)
        {
            weaponDurability -= 0.005f; //200 times
        }
    }
}
