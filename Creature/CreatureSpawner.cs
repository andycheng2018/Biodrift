using Unity.Netcode;
using UnityEngine;

public class CreatureSpawner : NetworkBehaviour
{
    [Header("Creature Spawner Settings")]
    [SerializeField] private GameObject spawner;
    [SerializeField][Range(100, 1000)] private int creatureSpawnSize;
    [SerializeField][Range(1, 20)] private float spawnTime;
    [SerializeField][Range(1, 10)] private int spawnLimit;
    [SerializeField][Range(100, 1000)] private int spawnRange;
    [SerializeField] private bool canSpawn = false;
    [SerializeField] private Creatures[] creatures;

    //Private Variables
    private Transform player;
    private float distanceToTarget = Mathf.Infinity;
    private NetworkVariable<int> creatureCount = new NetworkVariable<int>(default);

    public override void OnNetworkSpawn()
    {
        player = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.transform;

        if (IsHost)
        {
            InvokeRepeating("SpawnRandomClientRpc", spawnTime, spawnTime);
        }
        else
        {
            InvokeRepeating("SpawnRandomServerRpc", spawnTime, spawnTime);
        }
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        distanceToTarget = Vector3.Distance(player.position, spawner.transform.position);

        if ((creatureCount.Value < spawnLimit) && (distanceToTarget <= spawnRange))
        {
            canSpawn = true;
        }
        else
        {
            canSpawn = false;
        }
    }

    [ClientRpc]
    public void SpawnRandomClientRpc()
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
                    creatureCount.Value += 1;
                    creature.GetComponent<NetworkObject>().Spawn();
                    break;
                }
            }
        }
    }

    [ServerRpc]
    public void SpawnRandomServerRpc()
    {
        SpawnRandomClientRpc();
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
