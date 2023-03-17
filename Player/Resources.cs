using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Resources : NetworkBehaviour
{
    [Header("Resource Settings")]
    [SerializeField] private NetworkVariable<float> health = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public GameObject hitEffect;
    public GameObject[] loot;
    public Vector2 lootAmount;
    public AudioClip hitSound;

    public void ChangeHealth(float amount)
    {
        ChangeHealthServerRpc(amount);

        if (health.Value <= 0)
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

        health.Value += amount;
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
            var loots = Instantiate(loot[lootInt], gameObject.transform.position + new Vector3(Random.Range(-1, 8), Random.Range(25, 40), Random.Range(-8, 8)), Quaternion.identity);
            loots.transform.rotation = Random.rotation;
            if (loot[lootInt].GetComponent<Rigidbody>() != null)
                loots.GetComponent<Rigidbody>().isKinematic = false;
            loots.GetComponent<NetworkObject>().Spawn(true);
        }
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}
