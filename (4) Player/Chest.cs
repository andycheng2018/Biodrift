using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    public GameObject[] chestLoot;
    public GameObject particle;

    public void SpawnLoot()
    {
        var loot = chestLoot[Random.Range(0, chestLoot.Length)];
        GameObject weapon = Instantiate(loot, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
        weapon.name = loot.name;
        GameObject particles = Instantiate(particle, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
        weapon.transform.localScale += new Vector3(10, 10, 10);
        weapon.transform.SetParent(transform.parent);
        var setWeapon = weapon.GetComponent<WeaponController>();
        if (setWeapon != null)
        {
            setWeapon.isPlayer = false;
            setWeapon.isAI = false;
            setWeapon.isItem = true;
        }
        Destroy(gameObject);
        Destroy(particles, 2);
    }
}
