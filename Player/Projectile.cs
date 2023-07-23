using UnityEngine;

public class Projectile : MonoBehaviour
{
    private WeaponController weaponController;
    private Rigidbody rb;
    private bool launched = true;

    private void Awake()
    {
        weaponController = gameObject.GetComponent<WeaponController>();
        transform.localScale = Vector3.one;
        rb = gameObject.GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;
        gameObject.tag = "Projectile";
        weaponController.weaponDamage *= 1.5f;
        gameObject.GetComponent<Collider>().enabled = true;
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
        if (weaponController.isPlayer.Value)
        {
            if (other.tag == "Player" || other.tag == "Weapon")
            {
                return;
            }

            if (other.tag == "Enemy")
            {
                gameObject.GetComponent<Collider>().enabled = false;
                weaponController.enabled = false;
                rb.isKinematic = true;
                transform.SetParent(other.transform);
                Destroy(gameObject, 1);
                launched = false;
            }
        }

        if (weaponController.isAI.Value)
        {
            if (other.gameObject.layer == 7 || other.tag == "Weapon")
            {
                return;
            }

            if (other.GetComponent<WeaponController>() != null && other.GetComponent<WeaponController>().isAI.Value)
            {
                return;
            }
        }

        weaponController.enabled = false;
        rb.isKinematic = true;
        gameObject.GetComponent<Collider>().enabled = false;
        transform.SetParent(other.transform);
        Destroy(gameObject, 10);
        launched = false;
    }
}
