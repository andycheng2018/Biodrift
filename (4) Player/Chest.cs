using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : NetworkBehaviour
{
    [SyncVar]
    public float health;
    public GameObject hitEffect;
    public GameObject[] chestLoot;
    public GameObject particle;
    public AudioClip hitChest;
    private Player player;

    private void Start()
    {
        player = Player.playerInstance;
    }

    public void ChangeHealth(float amount)
    {
        health += amount;

        if (health <= 0)
        {
            SpawnLoot();
        }
    }

    [Command(requiresAuthority = false)]
    public void SpawnLoot()
    {
        var loot = chestLoot[Random.Range(0, chestLoot.Length)];
        GameObject weapon = Instantiate(loot, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
        weapon.name = loot.name;
        GameObject particles = Instantiate(particle, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
        Destroy(particles, 2);
        weapon.transform.SetParent(transform.parent);
        var setWeapon = weapon.GetComponent<WeaponController>();
        if (setWeapon != null)
        {
            setWeapon.isPlayer = false;
            setWeapon.isAI = false;
            setWeapon.isItem = true;
        }
        NetworkServer.Spawn(weapon);
        StartCoroutine(player.CmdDestroyObject(gameObject, 0));
    }
}
