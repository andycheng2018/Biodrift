using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    public DungeonGenerator dungeonGenerator;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            other.transform.position = dungeonGenerator.dungeonLocation + new Vector3(0, 15, 0);
        }
    }
}
