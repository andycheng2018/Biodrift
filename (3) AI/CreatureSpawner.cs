using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{
    public GameObject spawner;
    public int creatureSpawnSize = 300;
    public float spawnTime = 10;
    public int spawnLimit = 50;
    public int spawnRange = 100;
    public Creatures[] creatures;
    public bool canSpawn = false;

    private Transform player;
    private float distanceToTarget = Mathf.Infinity;
    private int creatureCount = 0;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
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
                if (Physics.Raycast(new Vector3(Random.Range(-creatureSpawnSize, creatureSpawnSize), 350, Random.Range(-creatureSpawnSize, creatureSpawnSize)) + gameObject.transform.position, Vector3.down, out hit, 350.0f) && hit.point.y > creatures[i].spawnHeight.x && hit.point.y < creatures[i].spawnHeight.y && hit.collider.tag == "Untagged")
                {
                    GameObject creature = Instantiate(creatures[Random.Range(0, creatures.Length)].creature, hit.point, Quaternion.identity);
                    float randomScale = Random.Range(creatures[i].scale.x, creatures[i].scale.y);
                    creature.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                    creature.transform.SetParent(spawner.transform);
                    creatureCount += 1;
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
