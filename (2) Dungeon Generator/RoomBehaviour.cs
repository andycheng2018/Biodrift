using Mirror;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomBehaviour : NetworkBehaviour
{
    public GameObject[] walls; // 0 - Up 1 -Down 2 - Right 3- Left
    public GameObject[] doors;

    public int spawnRange;
    public DungeonProps[] dungeonProps;

    private void Start()
    {
        for (int i = 0; i < dungeonProps.Length; i++)
        {
            if (Random.value > 1 - dungeonProps[i].percent)
            {
                for (int j = 0; j < dungeonProps[i].amount; j++)
                {
                    GameObject gameObject = Instantiate(dungeonProps[i].prop, transform.position + new Vector3(Random.Range(-spawnRange, spawnRange), 0, Random.Range(-spawnRange, spawnRange)), Quaternion.identity);
                    gameObject.transform.SetParent(transform);
                    var scale = Random.Range(dungeonProps[i].scale.x, dungeonProps[i].scale.y);
                    gameObject.transform.localScale = new Vector3(scale, scale, scale);
                    NetworkServer.Spawn(gameObject);
                }
            }
        } 
    }

    public void UpdateRoom(bool[] status)
    {
        for (int i = 0; i < status.Length; i++)
        {
            doors[i].SetActive(status[i]);
            walls[i].SetActive(!status[i]);
        }
    }
}

[System.Serializable]
public struct DungeonProps
{
    public String propName;
    public GameObject prop;
    public Vector2 scale;
    public float percent;
    public float amount;
}