using Unity.Netcode;
using UnityEngine;

public class Chest : NetworkBehaviour
{
    [Header("Chest Settings")]
    [SerializeField] private NetworkVariable<float> health = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public GameObject hitEffect;
    [SerializeField] private GameObject[] chestLoot;
    [SerializeField] private GameObject particle;
    public AudioClip hitChest;

    public void ChangeHealth(float amount)
    {
        ChangeHealthServerRpc(amount);

        if (health.Value <= 0)
        {
            SpawnLootServerRpc();
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
    public void SpawnLootServerRpc()
    {
        SpawnLootClientRpc();
    }

    [ClientRpc]
    public void SpawnLootClientRpc()
    {
        if (!IsServer) { return; }
        var loot = chestLoot[Random.Range(0, chestLoot.Length)];
        GameObject weapon = Instantiate(loot, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
        weapon.name = loot.name;
        //GameObject particles = Instantiate(particle, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
        //Destroy(particles, 2);
        weapon.transform.SetParent(transform.parent);
        var setWeapon = weapon.GetComponent<WeaponController>();
        if (setWeapon != null)
        {
            setWeapon.isPlayer = false;
            setWeapon.isAI = false;
            setWeapon.isItem = true;
        }
        weapon.GetComponent<NetworkObject>().Spawn(true);
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}
