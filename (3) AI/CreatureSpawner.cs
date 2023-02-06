using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureSpawner : NetworkBehaviour
{
    [Header("Creature Spawner Settings")]
    public GameObject spawner;
    [Range(100, 1000)] public int creatureSpawnSize;
    [Range(1, 20)] public float spawnTime;
    [Range(1, 10)] public int spawnLimit;
    [Range(100, 1000)] public int spawnRange;
    public bool canSpawn = false;
    public Creatures[] creatures;

    //Private Variables
    private Transform player;
    private float distanceToTarget = Mathf.Infinity;
    private int creatureCount = 0;

    private void Start()
    {
        player = Player.playerInstance.transform;
        InvokeRepeating("SpawnRandom", 0, spawnTime);
    }

    private void Update()
    {
        distanceToTarget = Vector3.Distance(player.position, spawner.transform.position);

        if ((creatureCount < spawnLimit) && (distanceToTarget <= spawnRange))
        {
            canSpawn = true;
        }
        else
        {
            canSpawn = false;
        }
    }

    public void SpawnRandom()
    {
        if (canSpawn)
        {
            RaycastHit hit;
            for (int i = 0; i < creatures.Length; i++)
            {
                if (Physics.Raycast(new Vector3(Random.Range(-creatureSpawnSize, creatureSpawnSize), 1500, Random.Range(-creatureSpawnSize, creatureSpawnSize)) + gameObject.transform.position, Vector3.down, out hit, 1500) && hit.point.y > creatures[i].spawnHeight.x && hit.point.y < creatures[i].spawnHeight.y && hit.collider.tag == "Untagged")
                {
                    GameObject creature = Instantiate(creatures[Random.Range(0, creatures.Length)].creature, hit.point, Quaternion.identity);
                    float randomScale = Random.Range(creatures[i].scale.x, creatures[i].scale.y);
                    creature.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                    creature.transform.SetParent(spawner.transform.parent);
                    creatureCount += 1;
                    NetworkServer.Spawn(creature);
                    break;
                }
            }
        }
    }

    [System.Serializable]
    public struct Creatures
    {
        public GameObject creature;
        public Vector2 spawnHeight;
        public Vector2 scale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRange);
    }
}
