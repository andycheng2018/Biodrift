using Unity.Netcode;
using UnityEditor.Rendering;
using UnityEngine;

public class Item : NetworkBehaviour
{
    public ItemType itemType;
    public enum ItemType { Weapon, Shield, Food, Resource, Building };
    public Sprite icon;
    public int amount;
    public int maxAmount;
    public int healAmount;
    [SerializeField] private NetworkVariable<bool> isSpinning = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Update()
    {
        if (isSpinning.Value)
        {
            transform.Rotate(0, 50 * Time.deltaTime, 0);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void setActiveServerRpc(bool b)
    {
        setActiveClientRpc(b);
    }

    [ClientRpc]
    public void setActiveClientRpc(bool b)
    {
        gameObject.SetActive(b);
        if (!IsServer) { return; }
        gameObject.transform.SetParent(null);
        if (b)
        {
            isSpinning.Value = true;
        }
        else
        {
            isSpinning.Value = false;
        }
    }
}
