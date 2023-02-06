using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Mirror;
using static UnityEngine.ParticleSystem;

public class Creature : NetworkBehaviour
{
    [Header("Creature References")]
    public Animator anim;
    [SerializeField] private NetworkAnimator networkAnimator = null;
    public Slider slider;
    public TMP_Text bossText;
    public bool isBoss;

    [Header("Creature Settings")]
    [Range(1, 50)] public float speed;
    [Range(1, 10)] public float rotationSpeed;
    [Range(0, 500)] public float chaseRange;
    [Range(1, 20)] public float jumpHeight;
    [SyncVar]
    [Range(1000, 10000)] public float health;
    [Range(1000, 10000)] public float maxHealth;
    [Range(0, 10000)] public int runAwayHealth;
    [Range(0, 1000)] public int xp; 

    [Header("Creature Weapon")]
    public Class classes;
    public enum Class { Warrior, Archer, Mage, Ghost, Villager, Guard };
    public Transform hand;
    public Transform offhand;
    public GameObject dustParticle;
    [Range(1, 100)] public int attackRange;
    [Range(1f, 5f)] public float attackCoolDown;
    public bool canAttack = true;
    public bool isProvoked;
    public GameObject[] weapons;
    public GameObject[] shields;

    [Header("Creature Abilities")]
    public bool smashGround;
    public GameObject smashParticles;
    public AudioClip smashSound;
    public int particleTime;

    public bool shootLazar;
    public GameObject lazar;
    public AudioClip lazarSound;
    public int lazarTime;

    public bool summonTroops;
    public GameObject troop;
    public GameObject troopParticle;
    public AudioClip summonSound;

    public bool teleport;
    public GameObject teleportParticle;
    public AudioClip teleportSound;
    public float teleportChance;

    [Header("Audio Settings")]
    public AudioClip[] idle;
    public AudioClip[] damaged;
    public AudioClip[] death;

    //Private Variables
    //Booleans
    private bool isWandering;
    private bool isRotatingLeft;
    private bool isRotatingRight;
    private bool isIdle;
    private bool isWalking;
    private bool isDead;
    //References
    private Transform player;
    private AudioSource audioSource;
    private float distanceToTarget = Mathf.Infinity;
    private float jumpSec;
    private float rollSec;
    private float idleAudioSec;
    //Weapons
    private int attackNum;
    private WeaponController curWeapon;
    private WeaponController curShield;

    private void Start()
    {
        player = Player.playerInstance.transform;
        audioSource = GetComponent<AudioSource>();
        health = maxHealth;
        slider.value = health / maxHealth;
        attackNum = Random.Range(0, 3);
        jumpSec = Random.Range(10, 25);
        rollSec = Random.Range(10, 30);
        idleAudioSec = Random.Range(1, 5);
        InvokeRepeating("jump", jumpSec, jumpSec);
        InvokeRepeating("roll", rollSec, rollSec);
        InvokeRepeating("idleAudio", idleAudioSec, idleAudioSec);

        //Assign Weapons
        if (weapons.Length > 0)
        {
            GameObject weapon = Instantiate(weapons[Random.Range(0, weapons.Length)], Vector3.zero, Quaternion.identity);
            weapon.transform.SetParent(hand);
            weapon.transform.localScale = new Vector3(10, 10, 10);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            var setWeapon = weapon.GetComponent<WeaponController>();
            setWeapon.accuracy = 1;
            setWeapon.throwForce = Random.Range(90, 110);
            setWeapon.isPlayer = false;
            setWeapon.isAI = true;
            setWeapon.isItem = false;
            curWeapon = setWeapon;
            setWeapon.creature = this;
            NetworkServer.Spawn(weapon);
        }

        //Assign Shields
        if (shields.Length > 0)
        {
            GameObject shield = Instantiate(shields[Random.Range(0, shields.Length)], Vector3.zero, Quaternion.identity);
            shield.transform.SetParent(offhand);
            shield.transform.localScale = new Vector3(10, 10, 10);
            shield.transform.localPosition = Vector3.zero;
            shield.transform.localRotation = Quaternion.identity;
            var setShield = shield.GetComponent<WeaponController>();
            setShield.isPlayer = false;
            setShield.isAI = true;
            setShield.isItem = false;
            curShield = setShield;
            setShield.creature = this;
            NetworkServer.Spawn(shield);
        }

        //Assign Boss name
        if (isBoss)
        {
            bossText.text = gameObject.name;
        }
    }

    private void Update()
    {
        if (isDead)
        {
            anim.Play("Die");
            CancelInvoke();
            return;
        }

        distanceToTarget = Vector3.Distance(player.position, transform.position);

        if (distanceToTarget <= chaseRange)
        {
            if (health >= runAwayHealth)
            {
                if ((classes == Class.Villager || classes == Class.Guard) && !isProvoked)
                {
                    wanderCheck();
                }
                else
                {
                    if (classes == Class.Villager)
                    {
                        runAway();
                    } 
                    else
                    {
                        chaseTarget();
                    }
                }
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

        if (transform.position.y < -400)
            StartCoroutine(player.GetComponent<Player>().CmdDestroyObject(gameObject, 0));

        if (health >= maxHealth)
        {
            slider.gameObject.SetActive(false);
        }
        else
        {
            slider.gameObject.SetActive(true);
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
            networkAnimator.SetTrigger("Idle");
            if (health <= maxHealth)
            {
                health += Time.deltaTime * 25f;
                slider.value = health / maxHealth;
            }
        }
        if (isWalking)
        {
            networkAnimator.SetTrigger("Walk");
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
                networkAnimator.SetTrigger("Idle");
            }
        }
        else
        {
            //Run
            transform.position += transform.forward * Time.deltaTime * speed;
            networkAnimator.SetTrigger("Run");
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
        if (curShield != null && attackNum == 2 && distanceToTarget <= 20)
        {
            networkAnimator.SetTrigger("Bash");
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
                if (canAttack)
                    networkAnimator.SetTrigger("Ability");
            }
            else
            {
                if (classes == Class.Warrior || classes == Class.Guard)
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
                if (canAttack)
                    networkAnimator.SetTrigger("Attack");
            }
        }
        networkAnimator.SetTrigger("Idle");
    }

    public void runAway()
    {
        if (distanceToTarget <= attackRange)
        {
            if (canAttack)
            {
                randomAttack();

                //Rotate to player
                Vector3 direction = (player.position - transform.position).normalized;
                if (classes == Class.Ghost)
                    transform.LookAt(player);
                else
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)), rotationSpeed * Time.deltaTime);
            } 
            else
            {
                networkAnimator.SetTrigger("Idle");
            }
        } 
        else
        {
            if (classes == Class.Villager)
            {
                transform.position += transform.forward * Time.deltaTime * speed;
                networkAnimator.SetTrigger("Run");
                Vector3 direction2 = (transform.position - player.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction2.x, 0, direction2.z)), rotationSpeed * Time.deltaTime);
            } else
            {
                transform.position -= transform.forward * Time.deltaTime * speed;
                networkAnimator.SetTrigger("RunAway");
                Vector3 direction = (player.position - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)), rotationSpeed * Time.deltaTime);
            }
        }         
    }

    public void jump()
    {
        gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(0, jumpHeight, 0), ForceMode.Impulse);
        networkAnimator.SetTrigger("Jump");
        jumpSec = Random.Range(10, 25);
    }

    public void roll()
    {
        gameObject.GetComponent<Rigidbody>().AddForce(0, 0, 1, ForceMode.Impulse);
        networkAnimator.SetTrigger("Roll");
        rollSec = Random.Range(10, 30);
    }

    public void idleAudio()
    {
        if (idle.Length > 0 && audioSource.enabled && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(idle[Random.Range(0, idle.Length)]);
        }
        idleAudioSec = Random.Range(1, 5);
    }

    public void JumpParticle()
    {
        GameObject particle = Instantiate(dustParticle, transform.position - new Vector3(0, 10, 0), Quaternion.identity);
        particle.transform.localScale = new Vector3(10, 10, 10);
        Destroy(particle, 2);
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
        NetworkServer.Spawn(projectile);
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
        NetworkServer.Spawn(projectile);
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
                    NetworkServer.Spawn(particle);
                    if (particleTime != 0)
                    {
                        StartCoroutine(player.GetComponent<Player>().CmdDestroyObject(particle, particleTime));
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
                NetworkServer.Spawn(particle);
                StartCoroutine(player.GetComponent<Player>().CmdDestroyObject(particle, particleTime));
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
                NetworkServer.Spawn(particle);
                StartCoroutine(player.GetComponent<Player>().CmdDestroyObject(particle, particleTime));
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
            NetworkServer.Spawn(particle);
            StartCoroutine(player.GetComponent<Player>().CmdDestroyObject(particle, particleTime));
            audioSource.PlayOneShot(lazarSound);
        }
    }

    public void SummonTroops()
    {
        GameObject troops = Instantiate(troop, hand.position, Quaternion.identity);
        NetworkServer.Spawn(troops);
        GameObject particle = Instantiate(troopParticle, hand.position, Quaternion.identity);
        NetworkServer.Spawn(particle);
        StartCoroutine(player.GetComponent<Player>().CmdDestroyObject(particle, 3));
        audioSource.PlayOneShot(summonSound);
    }

    public void Teleport()
    {
        GameObject particle = Instantiate(teleportParticle, transform.position, Quaternion.identity);
        NetworkServer.Spawn(particle);
        StartCoroutine(player.GetComponent<Player>().CmdDestroyObject(particle, 2));
        transform.position = player.position + new Vector3(Random.Range(-25, 25), 0, Random.Range(-25, 25));
        audioSource.PlayOneShot(teleportSound);
    }

    public void ChangeHealth(float amount)
    {
        health += amount;

        slider.value = health / maxHealth;
        if (damaged.Length > 0 && audioSource.enabled)
            audioSource.PlayOneShot(damaged[Random.Range(0, damaged.Length)]);
        anim.Play("Hit");

        if (health > maxHealth)
        {
            health = maxHealth;
        }

        if (health <= 0 && !isDead)
        {
            isDead = true;
            if (death.Length > 0 && audioSource.enabled)
                audioSource.PlayOneShot(death[Random.Range(0, death.Length)]);

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
                    weapon.transform.localScale = new Vector3(10, 10, 10);
                    weapon.transform.SetParent(transform.parent);
                    var setWeapon = weapon.GetComponent<WeaponController>();
                    setWeapon.isPlayer = false;
                    setWeapon.isAI = false;
                    setWeapon.isItem = true;
                    setWeapon.weaponDurability = Random.Range(0.1f, 0.8f);
                    NetworkServer.Spawn(weapon);
                }
                curWeapon.gameObject.SetActive(false);
            }

            if (curShield != null)
            {
                curShield.isAI = false;
                if (Random.value > 1 - curShield.weaponDropChance)
                {
                    GameObject shield = Instantiate(curShield.gameObject, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
                    shield.transform.localScale = new Vector3(10, 10, 10);
                    shield.transform.SetParent(transform.parent);
                    var setShield = shield.GetComponent<WeaponController>();
                    setShield.isPlayer = false;
                    setShield.isAI = false;
                    setShield.isItem = true;
                    setShield.weaponDurability = Random.Range(0.1f, 0.8f);
                    NetworkServer.Spawn(shield);
                }
                curShield.gameObject.SetActive(false);
            }

            var playerXP = player.GetComponent<Player>();
            playerXP.xpNum += xp;
            while (playerXP.xpNum >= 100)
            {
                playerXP.xpNum -= 100;
                playerXP.levelNum++;
            }
            playerXP.levelSlider.value = playerXP.xpNum;
            playerXP.levelText.text = "Level " + playerXP.levelNum;

            StartCoroutine(player.GetComponent<Player>().CmdDestroyObject(gameObject, 5));
        }
    }

    public void dead()
    {
        anim.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}