using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Runestone : MonoBehaviour
{
    public GameObject boss;

    public void SpawnBoss()
    {
        GameObject thisBoss = Instantiate(boss, transform.position, Quaternion.identity);
        thisBoss.transform.rotation = transform.rotation;
        thisBoss.transform.SetParent(transform.parent);
        thisBoss.transform.localScale = new Vector3(3, 3, 3);
        thisBoss.name = boss.name;
        Destroy(gameObject);
    }
}
