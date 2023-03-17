using UnityEngine;

public class Projectile : MonoBehaviour
{
    private WeaponController weaponController;
    private Rigidbody rb;
    private bool launched = true;

    private void Awake()
    {
        weaponController = gameObject.GetComponent<WeaponController>();
        transform.localScale = new Vector3(10, 10, 10);
        rb = gameObject.GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;
        weaponController.type = WeaponController.Types.Projectile;
        weaponController.weaponDamage *= 1.5f;
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
                Destroy(gameObject);
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
        }

        weaponController.enabled = false;
        rb.isKinematic = true;
        transform.SetParent(other.transform);
        Destroy(gameObject, 3);
        launched = false;
    }
}
