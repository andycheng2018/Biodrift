using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class Resources : MonoBehaviour
{
    [Header("Resource Settings")]
    public GameObject healthBar;
    public Slider slider;
    public float health;
    public float maxHealth;
    public GameObject loot;
    public int lootAmount;
    public AudioClip audioClip;

    public void ChangeHealth(float amount)
    {
        health += amount;

        slider.value = health / maxHealth;

        if (health <= 0)
        {
            Destroyed();
        }
    }

    public void Destroyed()
    {
        Destroy(gameObject);
        for (int i = 0; i < lootAmount; i++)
        {
            Instantiate(loot, gameObject.transform.position + new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3)), Quaternion.identity);
        }
    }
}
