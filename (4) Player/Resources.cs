using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

public class Resources : NetworkBehaviour
{
    [Header("Resource Settings")]
    [SyncVar]
    public float health;
    public GameObject hitEffect;
    public GameObject[] loot;
    public Vector2 lootAmount;
    public AudioClip hitSound;
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
            Destroyed();
        }
    }

    [Command(requiresAuthority = false)]
    public void Destroyed()
    {
        for (int i = 0; i < Random.Range(lootAmount.x, lootAmount.y); i++)
        {
            int lootInt = Random.Range(0, loot.Length);
            var loots = Instantiate(loot[lootInt], gameObject.transform.position + new Vector3(Random.Range(-8, 8), Random.Range(8, 15), Random.Range(-8, 8)), Quaternion.identity);
            loots.transform.rotation = Random.rotation;
            if (loot[lootInt].GetComponent<Rigidbody>() != null)
                loots.GetComponent<Rigidbody>().isKinematic = false;
            NetworkServer.Spawn(loots);
        }
        StartCoroutine(player.CmdDestroyObject(gameObject,0));
    }
}
