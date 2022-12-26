using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class Creature : MonoBehaviour
{
    [Header("Movement")]
    public Animator anim;
    public float speed = 8f;
    public float chaseRange = 8f;
    public float rotationSpeed = 3.0f;
    public float jumpHeight = 10f;
    private float jumpSec;
    private float rollSec;

    [Header("Idle")]
    public int idleSec = 5;
    public int walkSec = 10;
    public int rotSec = 3;

    [Header("Health")]
    public GameObject healthBar;
    public Slider slider;
    public float health = 1000;
    public float maxHealth = 1000;
    public int runAwayHealth = 500;

    [Header("Weapon")]
    public Class classes;
    public enum Class { Warrior, Archer, Mage, Ghost };
    public bool isBoss;
    public TMP_Text bossText;
    public Transform hand;
    public Transform offhand;
    public int attackRange;
    public float attackCoolDown = 0.5f;
    public bool canAttack = true;
    public GameObject[] weapons;
    public GameObject[] shields;
    private WeaponController curWeapon;
    private WeaponController curShield;
    private int attackNum;

    [Header("Ability")]
    public bool smashGround;
    public GameObject smashParticles;
    public AudioClip smashSound;
    public int particleTime = 2;

    public bool shootLazar;
    public GameObject lazar;
    public int lazarTime;
    public AudioClip lazarSound;

    public bool summonTroops;
    public GameObject troop;
    public GameObject troopParticle;
    public AudioClip summonSound;

    public bool teleport;
    public GameObject teleportParticle;
    public float teleportChance;
    public AudioClip teleportSound;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip footstep;
    public AudioClip breath;
    public AudioClip hurt;
    public AudioClip death;

    private Transform player;
    private float distanceToTarget = Mathf.Infinity;
    private bool isWandering = false;
    private bool isRotatingLeft = false;
    private bool isRotatingRight = false;
    private bool isIdle = false;
    private bool isWalking = false;
    private bool dead = false;
    private float period = 0.0f;
    private float period2 = 0.0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        health = maxHealth;
        slider.value = health / maxHealth;
        attackNum = Random.Range(0, 3);
        jumpSec = Random.Range(10, 25);
        rollSec = Random.Range(10, 35);

        //Assign Weapons
        if (weapons.Length > 0)
        {
            GameObject weapon = Instantiate(weapons[Random.Range(0, weapons.Length)], Vector3.zero, Quaternion.identity);
            weapon.transform.SetParent(hand);
            weapon.transform.localScale = new Vector3(1, 1, 1);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            var setWeapon = weapon.GetComponent<WeaponController>();
            setWeapon.accuracy = 2;
            setWeapon.isPlayer = false;
            setWeapon.isAI = true;
            setWeapon.isItem = false;
            curWeapon = setWeapon;
        }

        //Assign Shields
        if (shields.Length > 0)
        {
            GameObject shield = Instantiate(shields[Random.Range(0, shields.Length)], Vector3.zero, Quaternion.identity);
            shield.transform.SetParent(offhand);
            shield.transform.localScale = new Vector3(1, 1, 1);
            shield.transform.localPosition = Vector3.zero;
            shield.transform.localRotation = Quaternion.identity;
            var setShield = shield.GetComponent<WeaponController>();
            setShield.isPlayer = false;
            setShield.isAI = true;
            setShield.isItem = false;
            curShield = setShield;
        }

        //Assign Boss name
        if (isBoss)
        {
            bossText.text = gameObject.name;
        }
    }

    private void Update()
    {
        if (dead)
        {
            return;
        }

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
            if (!isWandering)
            {
                StartCoroutine(Wander());
            }
            if (isIdle)
            {
                anim.SetTrigger("Idle");
                if (health <= maxHealth)
                {
                    health += Time.deltaTime * 25f;
                    slider.value = health / maxHealth;
                }
            }
            if (isWalking)
            {
                anim.SetTrigger("Walk");
                transform.position += transform.forward * Time.deltaTime * speed;
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

        if (transform.position.y < -400)
            Destroy(gameObject);

        if (health >= maxHealth)
        {
            healthBar.SetActive(false);
        }
        else
        {
            healthBar.SetActive(true);
        }

        if (distanceToTarget > 200)
        {
            audioSource.enabled = false;
        }
        else
        {
            audioSource.enabled = true;
            if (!audioSource.isPlaying && breath != null)
            {
                audioSource.PlayOneShot(breath);
            }
        }
    }

    public void chaseTarget()
    {
        if (distanceToTarget <= attackRange)
        {
            if (canAttack)
            {
                randomAttack();
            } 
            else 
            {
                anim.SetTrigger("Idle");
            }
        }
        else
        {
            //Run
            transform.position += transform.forward * Time.deltaTime * speed;
            anim.SetTrigger("Run");

            //Jump
            if (period >= jumpSec)
            {
                gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(0, jumpHeight, 0), ForceMode.Impulse);
                anim.SetTrigger("Jump");
                jumpSec = Random.Range(15, 25);
                period = 0;
            }
            period += Time.deltaTime;

            //Roll
            if (period2 >= rollSec)
            {
                anim.SetTrigger("Roll");
                rollSec = Random.Range(15, 35);
                period2 = 0;
            }
            period2 += Time.deltaTime;
        }

        //Rotate to player
        Vector3 direction = (player.position - transform.position).normalized;
        if (classes == Class.Ghost)
            transform.LookAt(player);
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)), rotationSpeed * Time.deltaTime);
    }

    public void randomAttack()
    {
        if (curShield != null && attackNum == 0 && distanceToTarget <= 8)
        {
            anim.SetTrigger("Bash");
        }
        else
        {
            if (attackNum == 1 && (smashGround || shootLazar || summonTroops || teleport))
            {
                if (smashGround)
                {
                    anim.SetInteger("AbilityIndex", 0);
                }
                else if (shootLazar)
                {
                    anim.SetInteger("AbilityIndex", 1);
                }
                else if (summonTroops)
                {
                    anim.SetInteger("AbilityIndex", 2);
                }
                if (teleport && Random.value > 1 - teleportChance)
                {
                    Teleport();
                }
                anim.SetTrigger("Ability");
            }
            else
            {
                if (classes == Class.Warrior)
                {
                    anim.SetInteger("AttackIndex", 0);
                }
                else if (classes == Class.Archer)
                {
                    anim.SetInteger("AttackIndex", 1);
                }
                else if (classes == Class.Mage || classes == Class.Ghost)
                {
                    anim.SetInteger("AttackIndex", 2);
                }
                anim.SetTrigger("Attack");
            }
        }
        anim.SetTrigger("Idle");
    }

    public void runAway()
    {
        if (distanceToTarget <= attackRange)
        {
            if (canAttack)
            {
                randomAttack();
            } 
            else
            {
                anim.SetTrigger("Idle");
            }
        } 
        else if (distanceToTarget > attackRange)
        {
            transform.position -= transform.forward * Time.deltaTime * speed;
            anim.SetTrigger("RunAway");
            Vector3 direction = (player.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)), rotationSpeed * Time.deltaTime);
        }         
    }

    public IEnumerator Wander()
    {
        int rotTime = Random.Range(1, rotSec);
        int idleTime = Random.Range(1, idleSec);
        int walkTime = Random.Range(1, walkSec);
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

    public void Step()
    {
        if (audioSource.enabled)
            audioSource.PlayOneShot(footstep);
    }

    //Attacks
    public void SwordSound()
    {
        if (curWeapon != null)
        {
            audioSource.PlayOneShot(curWeapon.attackSound);
        }
    }

    public void ShieldSound()
    {
        if (curShield != null)
        {
            audioSource.PlayOneShot(curShield.attackSound);
        }
    }

    public void ThrowSpear()
    {
        audioSource.PlayOneShot(curWeapon.attackSound);

        GameObject projectile = Instantiate(curWeapon.spear, curWeapon.transform.position, curWeapon.transform.rotation);
        Vector3 forceToAdd = transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        curWeapon.gameObject.SetActive(false);
    }

    public void ChangeSpear()
    {
        curWeapon.gameObject.SetActive(true);
    }

    public void CastSpell()
    {
        audioSource.PlayOneShot(curWeapon.attackSound);

        GameObject projectile = Instantiate(curWeapon.spear, curWeapon.transform.position, curWeapon.transform.rotation);
        Vector3 forceToAdd = transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));
        if (classes == Class.Ghost)
        {
            forceToAdd -= new Vector3(0, 3, 0);
        }

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<WeaponController>().isAI = true;
        projectile.GetComponent<WeaponController>().weaponDamage = curWeapon.weaponDamage;
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
    }

    public IEnumerator ResetAttackCoolDown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCoolDown);
        attackNum = Random.Range(0, 3);
        canAttack = true;
    }

    //Abilities
    public void SmashGround()
    {
        if (isBoss)
        {
            for (int i = 0; i < Random.Range(2,6); i++)
            {
                if (Physics.Raycast(transform.position + new Vector3(Random.Range(-20, 20), 350, Random.Range(-20, 20)), Vector3.down, out RaycastHit hit, 350.0f))
                {
                    GameObject particle = Instantiate(smashParticles, hit.point, Quaternion.identity);
                    if (particleTime != 0)
                    {
                        Destroy(particle, particleTime);
                    }
                    audioSource.PlayOneShot(smashSound);
                    health += 50;
                    slider.value = health / maxHealth;
                }
            }
        } 
        else
        {
            if (smashParticles != null)
            {
                GameObject particle = Instantiate(smashParticles, hand.position, Quaternion.identity);
                Destroy(particle, particleTime);
                audioSource.PlayOneShot(smashSound);
            }
        }
    }

    public void ShootLazar()
    {
        if (isBoss)
        {
            for (int i = 0; i < Random.Range(1,3); i++)
            {
                GameObject particle = Instantiate(lazar, hand.position + new Vector3(Random.Range(-5, 5), -2, 0), Quaternion.identity);
                particle.transform.rotation = transform.rotation;
                particle.transform.SetParent(transform);
                Destroy(particle, lazarTime);
                audioSource.PlayOneShot(lazarSound);
                health += 50;
                slider.value = health / maxHealth;
            }
        } 
        else
        {
            GameObject particle = Instantiate(lazar, hand.position, Quaternion.identity);
            particle.transform.rotation = transform.rotation;
            particle.transform.SetParent(transform);
            Destroy(particle, lazarTime);
            audioSource.PlayOneShot(lazarSound);
        }
    }

    public void SummonTroops()
    {
        Instantiate(troop, hand.position, Quaternion.identity);
        GameObject particle = Instantiate(troopParticle, hand.position, Quaternion.identity);
        Destroy(particle, 3);
        audioSource.PlayOneShot(summonSound);
    }

    public void Teleport()
    {
        GameObject particle = Instantiate(teleportParticle, transform.position, Quaternion.identity);
        Destroy(particle, 2);
        transform.position = player.position + new Vector3(Random.Range(-25, 25), 0, Random.Range(-25, 25));
        audioSource.PlayOneShot(teleportSound);
    }

    public void ChangeHealth(float amount)
    {
        health += amount;

        slider.value = health / maxHealth;
        audioSource.PlayOneShot(hurt);
        anim.Play("Hit");

        if (health > maxHealth)
        {
            health = maxHealth;
        }

        if (health <= 0 && !dead)
        {
            dead = true;
            audioSource.PlayOneShot(death);
            anim.Play("Die");
            if (classes == Class.Ghost)
            {
                gameObject.GetComponent<Rigidbody>().useGravity = true;
            }

            if (curWeapon != null)
            {
                curWeapon.isAI = false;
                if (Random.value > 1 - curWeapon.weaponDropChance)
                {
                    GameObject weapon = Instantiate(curWeapon.gameObject, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
                    weapon.transform.localScale = new Vector3(5, 5, 5);
                    weapon.transform.SetParent(transform.parent);
                    var setWeapon = weapon.GetComponent<WeaponController>();
                    setWeapon.isPlayer = false;
                    setWeapon.isAI = false;
                    setWeapon.isItem = true;
                }
                curWeapon.gameObject.SetActive(false);
            }

            if (curShield != null)
            {
                curShield.isAI = false;
                if (Random.value > 1 - curShield.weaponDropChance)
                {
                    GameObject shield = Instantiate(curShield.gameObject, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
                    shield.transform.localScale = new Vector3(5, 5, 5);
                    shield.transform.SetParent(transform.parent);
                    var setShield = shield.GetComponent<WeaponController>();
                    setShield.isPlayer = false;
                    setShield.isAI = false;
                    setShield.isItem = true;
                }
                curShield.gameObject.SetActive(false);
            }

            if (isBoss)
            {
                MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
                if (mapGenerator.biome == (MapGenerator.Biome)7)
                {
                    mapGenerator.biome = 0;
                    FindObjectOfType<SettingsMenu>().endGamePause();
                }
                else
                {
                    mapGenerator.biome = (MapGenerator.Biome)(int)mapGenerator.biome + 1;
                }
                mapGenerator.CheckBiome();
            }

            Destroy(gameObject, 5);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}