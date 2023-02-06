using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Projectile : NetworkBehaviour
{
    private WeaponController weaponController;
    private Rigidbody rb;
    private bool launched = true;
    private Player player;

    private void Awake()
    {
        weaponController = gameObject.GetComponent<WeaponController>();
        transform.localScale = new Vector3(10, 10, 10);
        rb = gameObject.GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;
        weaponController.type = WeaponController.Types.Projectile;
        weaponController.weaponDamage *= 1.5f;
        player = Player.playerInstance;
    }

    private void LateUpdate()
    {
        if (launched)
        {
            transform.LookAt(transform.position + rb.velocity);
            transform.rotation *= Quaternion.AxisAngle(Vector3.right, Mathf.PI / 2);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (weaponController.isPlayer)
        {
            if (other.tag == "Player" || other.tag == "Weapon")
            {
                return;
            }

            if (other.GetComponent<WeaponController>() != null && other.GetComponent<WeaponController>().isPlayer)
            {
                return;
            }

            if (other.tag == "Enemy" || other.tag == "Villager")
            {
                StartCoroutine(player.CmdDestroyObject(gameObject, 0));
            }
        }

        if (weaponController.isAI)
        {
            if (other.tag == "Enemy" || other.tag == "Weapon")
            {
                return;
            }

            if (other.GetComponent<WeaponController>() != null && other.GetComponent<WeaponController>().isAI)
            {
                return;
            }

            if (other.tag == "Player")
            {
                GameObject particle = Instantiate(weaponController.hitEffect, transform.position, transform.rotation);
                NetworkServer.Spawn(particle);
                StartCoroutine(player.CmdDestroyObject(particle, 2));
                weaponController.audioSource.PlayOneShot(weaponController.damageSound);
                other.gameObject.SendMessageUpwards("ChangeHealth", -weaponController.weaponDamage, SendMessageOptions.DontRequireReceiver);
                StartCoroutine(player.CmdDestroyObject(gameObject, 0));
            }
        }

        weaponController.enabled = false;
        rb.isKinematic = true;
        transform.SetParent(other.transform);
        StartCoroutine(player.CmdDestroyObject(gameObject, 3));
        launched = false;
    }
}
