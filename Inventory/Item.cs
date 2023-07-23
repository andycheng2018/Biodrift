using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour
{
    [Header("Item Settings")]
    public ItemType itemType;
    public enum ItemType { Resource, Weapon, Armor, Food, Building };
    public Sprite icon;
    public Vector2 amount;
    public float offset;
    public float foodAmount;
    public NetworkVariable<float> networkAmount = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }
        networkAmount.Value = amount.x;
    }

    private void Update()
    {
        if (!IsServer) { return; }

        if (transform.position.y <= -50)
        {
            gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void setActiveServerRpc(bool b)
    {
        if (!IsServer) { return; }
        setActiveClientRpc(b);
    }

    [ClientRpc]
    public void setActiveClientRpc(bool b)
    {
        gameObject.SetActive(b);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeAmountServerRpc(float amount, bool fixedAmount)
    {
        ChangeAmountClientRpc(amount, fixedAmount);
    }

    [ClientRpc]
    public void ChangeAmountClientRpc(float amount, bool fixedAmount)
    {
        if (!IsServer) { return; }

        if (fixedAmount)
        {
            networkAmount.Value = amount;
        } else
        {
            networkAmount.Value += amount;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangePositionServerRpc(Vector3 vector3)
    {
        ChangePositionClientRpc(vector3);
    }

    [ClientRpc]
    public void ChangePositionClientRpc(Vector3 vector3)
    {
        if (!IsServer) { return; }

        transform.position = vector3;
    }

    public void ChangePosition(Vector3 vector3)
    {
        transform.position = vector3;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            Player player = other.GetComponent<Player>();
            player.checkPickUp(gameObject);
            return;
        }
    }
}
