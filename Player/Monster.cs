using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class Monster : MonoBehaviour
{
    [Header("Monster References")]
    public Animator anim;
    public Slider healthSlider;
    public Class classes;
    public enum Class { Melee, Ranged, Magic, Flying };
    public Abilitiy ability;
    public Transform hand;
    public Transform offhand;
    public GameObject[] weapons;
    public GameObject[] shields;

    [Header("Creature Settings")]
    [Range(0, 20)] public float walkSpeed;
    [Range(0, 20)] public float runSpeed;
    [Range(0, 100)] public float chaseRange;
    [Range(0, 100)] public int attackRange;
    [Range(0, 500)] public float health;
    [Range(0, 500)] public float maxHealth;
    [Range(0, 500)] public int runAwayHealth;
    [Range(0, 1000)] public int xp;
    public float attackCoolDown;
    public bool canAttack;

    [Header("Audio Settings")]
    public AudioClip[] idle;
    public AudioClip[] damaged;
    public AudioClip[] death;

    private bool isWandering;
    private bool isRotatingLeft;
    private bool isRotatingRight;
    private bool isIdle;
    private bool isWalking;
    private bool isDead;

    private AudioSource audioSource;
    private Transform player;
    private float distanceToTarget = Mathf.Infinity;
    private float idleAudioSec;
    private WeaponController curWeapon;
    private WeaponController curShield;
    private int attackNum;

    private void Start()
    {
        player = FindObjectOfType<Player>().transform;
        audioSource = GetComponent<AudioSource>();
        health = maxHealth;
        healthSlider.value = health / maxHealth;
        attackNum = Random.Range(1, 3);
        idleAudioSec = Random.Range(1, 6);
        InvokeRepeating("idleAudio", idleAudioSec, idleAudioSec);

        if (weapons.Length > 0)
        {
            GameObject weapon = Instantiate(weapons[Random.Range(0, weapons.Length)], Vector3.zero, Quaternion.identity);
            weapon.transform.SetParent(hand);
            weapon.transform.localScale = new Vector3(1, 1, 1);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            var setWeapon = weapon.GetComponent<WeaponController>();
            setWeapon.accuracy = 1;
            setWeapon.setWeaponServerRpc(false, true);
            curWeapon = setWeapon;
            setWeapon.monster = this;
            weapon.GetComponent<Collider>().enabled = false;
        }

        if (shields.Length > 0)
        {
            GameObject shield = Instantiate(shields[Random.Range(0, shields.Length)], Vector3.zero, Quaternion.identity);
            shield.transform.SetParent(offhand);
            shield.transform.localScale = new Vector3(1, 1, 1);
            shield.transform.localPosition = Vector3.zero;
            shield.transform.localRotation = Quaternion.identity;
            var setShield = shield.GetComponent<WeaponController>();
            setShield.setWeaponServerRpc(false, true);
            curShield = setShield;
            setShield.monster = this;
            shield.GetComponent<Collider>().enabled = false;
        }
    }

    private void Update()
    {
        if (isDead) { return; }

        if (transform.position.y < -50) { Destroy(gameObject); }

        distanceToTarget = Vector3.Distance(player.position, transform.position);

        if (distanceToTarget <= chaseRange)
        {
            if (health >= runAwayHealth)
            {
                chaseTarget();
            }
            else
            {
                runAway();
            }
        }
        else
        {
            wanderCheck();
        }

        if (health >= maxHealth)
        {
            healthSlider.gameObject.SetActive(false);
        }
        else
        {
            healthSlider.gameObject.SetActive(true);
        }

        if (distanceToTarget > chaseRange)
        {
            audioSource.enabled = false;
        }
        else
        {
            audioSource.enabled = true;
        }
    }

    public void wanderCheck()
    {
        if (!isWandering)
        {
            StartCoroutine(Wander());
        }
        if (isIdle)
        {
            anim.SetTrigger("Idle");
        }
        if (isWalking)
        {
            anim.SetTrigger("Walk");
            transform.position += transform.forward * Time.deltaTime * walkSpeed;
        }
        if (isRotatingLeft)
        {
            transform.Rotate(transform.up * Time.deltaTime * 100);
        }
        if (isRotatingRight)
        {
            transform.Rotate(-transform.up * Time.deltaTime * 100);
        }
    }

    public IEnumerator Wander()
    {
        int rotTime = Random.Range(1, 3);
        int idleTime = Random.Range(1, 10);
        int walkTime = Random.Range(1, 8);
        int turnChance = Random.Range(1, 3);

        isWandering = true;

        isIdle = true;
        yield return new WaitForSeconds(idleTime);
        isIdle = false;

        isWalking = true;
        yield return new WaitForSeconds(walkTime);
        isWalking = false;

        if (turnChance == 1)
        {
            isRotatingLeft = true;
        }
        else
        {
            isRotatingRight = true;
        }
        yield return new WaitForSeconds(rotTime);
        isRotatingLeft = false;
        isRotatingRight = false;

        isWandering = false;
    }

    public void chaseTarget()
    {
        if (distanceToTarget <= attackRange && canAttack)
        {
            attackTarget();
        }
        else
        {
            anim.SetTrigger("Run");
            transform.position += transform.forward * Time.deltaTime * runSpeed;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        if (classes == Class.Flying)
            transform.LookAt(player);
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)), 3 * Time.deltaTime);
    }

    public void attackTarget()
    {
        if (curWeapon == null) { return; }

        if (canAttack)
        {
            if (attackNum == 1)
            {
                curWeapon.weaponTrail.Play();
                anim.SetInteger("SwordIndex", Random.Range(0, 4));
                anim.SetTrigger("Sword");
            } else if (attackNum == 2)
            {
                curShield.weaponTrail.Play();
                anim.SetInteger("ShieldIndex", Random.Range(0, 2));
                anim.SetTrigger("Shield");
            }
        }
    }

    public void runAway()
    {
        if (distanceToTarget <= attackRange)
        {
            attackTarget();

            Vector3 direction = (player.position - transform.position).normalized;
            if (classes == Class.Flying)
                transform.LookAt(player);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)), 3 * Time.deltaTime);
        }
        else
        {
            anim.SetTrigger("RunAway");
            transform.position -= transform.forward * Time.deltaTime * walkSpeed;
            Vector3 direction = (player.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)), 3 * Time.deltaTime);
        }
    }

    public void idleAudio()
    {
        if (idle.Length > 0 && audioSource.enabled && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(idle[Random.Range(0, idle.Length)]);
        }
        idleAudioSec = Random.Range(1, 5);
    }

    private IEnumerator ResetAttackCoolDown()
    {
        canAttack = false;
        if (curWeapon != null)
        {
            curWeapon.GetComponent<Collider>().enabled = true;
        }
        if (curShield != null)
        {
            curShield.GetComponent<Collider>().enabled = true;
        }

        yield return new WaitForSeconds(attackCoolDown);

        canAttack = true;
        if (curWeapon != null)
        {
            curWeapon.GetComponent<Collider>().enabled = false;
        }
        if (curShield != null)
        {
            curShield.GetComponent<Collider>().enabled = false;
        }
        attackNum = Random.Range(1, 3);
    }

    //Attacks
    public void Step()
    {

    }

    public void SwordSound()
    {
        if (curWeapon != null)
        {
            audioSource.PlayOneShot(curWeapon.attackSound);
            StartCoroutine(ResetAttackCoolDown());
        }
    }

    public void ShieldSound()
    {
        if (curShield != null)
        {
            audioSource.PlayOneShot(curShield.attackSound);
            StartCoroutine(ResetAttackCoolDown());
        }
    }

    public void ThrowSpear()
    {
        audioSource.PlayOneShot(curWeapon.attackSound);

        GameObject projectile = Instantiate(curWeapon.arrow, curWeapon.transform.position, curWeapon.transform.rotation);
        Vector3 forceToAdd = transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        curWeapon.gameObject.SetActive(false);
        StartCoroutine(ResetAttackCoolDown());
    }

    public void ChangeSpear()
    {
        curWeapon.gameObject.SetActive(true);
    }

    public void CastSpell()
    {
        audioSource.PlayOneShot(curWeapon.attackSound);

        GameObject projectile = Instantiate(curWeapon.arrow, curWeapon.transform.position, curWeapon.transform.rotation);
        Vector3 forceToAdd = transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));
        if (classes == Class.Flying)
        {
            forceToAdd -= new Vector3(0, 3, 0);
        }
        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<WeaponController>().setWeaponServerRpc(false, true);
        projectile.GetComponent<WeaponController>().weaponDamage = curWeapon.weaponDamage;
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        StartCoroutine(ResetAttackCoolDown());
    }

    //Abilities
    public void SmashGround()
    {
        if (ability.smashParticle != null)
        {
            GameObject particle = Instantiate(ability.smashParticle, hand.position, Quaternion.identity);
            Destroy(particle, 2);
            audioSource.PlayOneShot(ability.smashSound);
            StartCoroutine(ResetAttackCoolDown());
        }
    }

    public void ShootLazar()
    {
        GameObject particle = Instantiate(ability.lazarParticle, hand.position, Quaternion.identity);
        particle.transform.rotation = transform.rotation;
        particle.transform.SetParent(transform);
        Destroy(particle, 2);
        audioSource.PlayOneShot(ability.lazarSound);
        StartCoroutine(ResetAttackCoolDown());
    }

    public void SummonTroops()
    {
        GameObject troops = Instantiate(ability.troop, hand.position, Quaternion.identity);
        Destroy(troops, 10);
        GameObject particle = Instantiate(ability.troopParticle, hand.position, Quaternion.identity);
        Destroy(particle, 3);
        audioSource.PlayOneShot(ability.summonSound);
        StartCoroutine(ResetAttackCoolDown());
    }

    public void Teleport()
    {
        GameObject particle = Instantiate(ability.teleportParticle, transform.position, Quaternion.identity);
        Destroy(particle, 2);
        transform.position = player.position + new Vector3(Random.Range(-25, 25), 0, Random.Range(-25, 25));
        audioSource.PlayOneShot(ability.teleportSound);
        StartCoroutine(ResetAttackCoolDown());
    }

    public void ChangeHealth(float amount)
    {
        if (isDead) { return; }

        health += amount;
        healthSlider.value = health / maxHealth;

        if (damaged.Length > 0 && audioSource.enabled)
            audioSource.PlayOneShot(damaged[Random.Range(0, damaged.Length)]);
        anim.Play("Hit");

        if (health <= 0)
        {
            isDead = true;
            if (death.Length > 0 && audioSource.enabled)
                audioSource.PlayOneShot(death[Random.Range(0, death.Length)]);

            if (classes == Class.Flying)
                gameObject.GetComponent<Rigidbody>().useGravity = true;

            if (curWeapon != null)
            {
                curWeapon.setWeaponServerRpc(false, false);
                if (Random.value > 1 - curWeapon.weaponDropChance)
                {
                    GameObject weapon = Instantiate(curWeapon.gameObject, transform.position, Quaternion.identity);
                    weapon.transform.localScale = new Vector3(1, 1, 1);
                    weapon.transform.SetParent(transform.parent);
                    var setWeapon = weapon.GetComponent<WeaponController>();
                    setWeapon.setWeaponServerRpc(false, false);
                    setWeapon.weaponDurability = Random.Range(0.1f, 0.8f);
                }
                curWeapon.gameObject.SetActive(false);
            }

            if (curShield != null)
            {
                curShield.setWeaponServerRpc(false, false);
                if (Random.value > 1 - curShield.weaponDropChance)
                {
                    GameObject shield = Instantiate(curShield.gameObject, transform.position, Quaternion.identity);
                    shield.transform.localScale = new Vector3(1, 1, 1);
                    shield.transform.SetParent(transform.parent);
                    var setShield = shield.GetComponent<WeaponController>();
                    setShield.setWeaponServerRpc(false, false);
                    setShield.weaponDurability = Random.Range(0.1f, 0.8f);
                }
                curShield.gameObject.SetActive(false);
            }

            anim.enabled = false;
            gameObject.GetComponent<Collider>().enabled = false;
            Destroy(gameObject, 10);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}

[System.Serializable]
public struct Abilitiy
{
    [Header("Smash Ground")]
    public bool smashGround;
    public GameObject smashParticle;
    public AudioClip smashSound;

    [Header("Shoot Lazar")]
    public bool shootLazar;
    public GameObject lazarParticle;
    public int lazarTime;
    public AudioClip lazarSound;

    [Header("Summon Troop")]
    public bool summonTroop;
    public GameObject troopParticle;
    public GameObject troop;
    public AudioClip summonSound;

    [Header("Teleport")]
    public bool teleport;
    public GameObject teleportParticle;
    public float teleportChance;
    public AudioClip teleportSound;
}