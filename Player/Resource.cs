using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Resource : NetworkBehaviour
{
    [Header("Resource Settings")]
    public string resourceName;
    public float health;
    public ResourceType resourceType;
    public enum ResourceType { Wood, Rock };
    public Rarity lowestRarity;
    public enum Rarity { Wood, Stone, Gold, Iron, Sapphire };
    public NetworkVariable<float> networkHealth = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [HideInInspector] public float maxHealth;
    public GameObject hitEffect;
    public GameObject[] loot;
    public Vector2 lootAmount;
    public AudioClip hitSound;

    public override void OnNetworkSpawn()
    {
        maxHealth = health;
        if (!IsServer) { return; }
        networkHealth.Value = health;
    }

    public void ChangeHealth(float amount)
    {
        ChangeHealthServerRpc(amount);

        if (networkHealth.Value <= 0)
        {
            DestroyedServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeHealthServerRpc(float amount)
    {
        ChangeHealthClientRpc(amount);
    }

    [ClientRpc]
    public void ChangeHealthClientRpc(float amount)
    {
        if (!IsServer) { return; }
        networkHealth.Value += amount;
        health = networkHealth.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyedServerRpc()
    {
        DestroyedClientRpc();
    }

    [ClientRpc]
    public void DestroyedClientRpc()
    {
        if (!IsServer) { return; }
        for (int i = 0; i < Random.Range(lootAmount.x, lootAmount.y); i++)
        {
            int lootInt = Random.Range(0, loot.Length);
            var loots = Instantiate(loot[lootInt], gameObject.transform.position + new Vector3(Random.Range(-2, 2), Random.Range(2, 4), Random.Range(-2, 2)), Quaternion.identity);
            loots.transform.rotation = Random.rotation;
            if (loots.GetComponent<Rigidbody>() != null)
                loots.GetComponent<Rigidbody>().isKinematic = false;
            if (loots.GetComponent<NetworkObject>() != null)
                loots.GetComponent<NetworkObject>().Spawn(true);
        }

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            if (gameObject.transform.GetChild(i).GetComponent<Resource>() != null)
            {
                gameObject.transform.GetChild(i).GetComponent<Resource>().DestroyedServerRpc();
            }
        }
        GameObject particle = Instantiate(hitEffect, transform.position + new Vector3(0, 2, 0), Quaternion.identity);
        Destroy(particle, 2);
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}