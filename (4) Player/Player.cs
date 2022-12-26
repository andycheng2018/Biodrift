using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;
using Unity.VisualScripting;
using static UnityEditor.Progress;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public Animator anim;
    public CharacterController controller;
    public GameObject landPostPro;
    public GameObject underwaterPostPro;
    public KeyCode sprintKey;
    public KeyCode rollKey;
    public KeyCode crouchKey;
    [Range(0.5f, 50)] public float walkSpeed;
    [Range(0.5f, 50)] public float strafeSpeed;
    [Range(1, 5)] public float sprintFactor;
    [Range(1, 5)] public float rollFactor;
    [Range(0.1f, 1.0f)] public float crouchFactor;
    [Range(0.5f, 10)] public float jumpHeight;
    public int maxJumps;
    public Vector2 swimHeight;
    private Transform cam;
    private bool isRunning;
    private bool isCrouched;
    private bool isSwimming;
    private bool isIdle;
    private bool dead;

    private Vector3 velocity = Vector3.zero;
    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    private int jumpsSinceLastLand = 0;
    private Vector3 dungeonPos;
    private Vector3 spawnPos;

    [Header("Health")]
    public Slider healthSlider;
    public Slider staminaSlider;
    public GameObject damageUI;
    public GameObject fadeInUI;
    public float health = 10000;
    public float maxHealth = 10000;
    private bool stamina = true;

    [Header("Weapon")]
    public Transform hand;
    public Transform offhand;
    public InventorySlot sword;
    public InventorySlot shield;
    public bool canAttack;
    public float cameraShakeIntensity = 3f;
    public float attackCoolDown = 0.7f;

    public WeaponController curWeapon;
    public WeaponController curShield;
    public TMP_Text displayText;

    [Header("Inventory")]
    public static Player Instance;
    public GameObject Inventory;
    public Transform ItemContent;
    public GameObject InventoryItem;
    public List<Item> Items = new List<Item>();
    private bool isOpen = true;

    private GameObject pendingObject;
    public LayerMask layerMask;
    private Vector3 pos;

    [Header("Audio")]
    public AudioSource audioSource1;
    public AudioClip footstep;
    public AudioSource audioSource2;
    public AudioClip jump;
    public AudioClip hurt;
    public AudioClip openChest;
    public AudioClip equip;
    public AudioClip pickupGem;
    public AudioClip heal;

    private void Start()
    {
        cam = FindObjectOfType<Camera>().transform;
        Cursor.lockState = CursorLockMode.Locked;
        health = maxHealth;
        healthSlider.value = health / maxHealth;
        staminaSlider.value = 100;
        audioSource1.clip = footstep;

        //spawnPos = new Vector3(Random.Range(-300, 300), 1000.0f, Random.Range(-300, 300));

        //controller.enabled = false;
        //transform.position = spawnPos;
        //controller.enabled = true;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!dead)
        {
            Controller();
            Attack();
            PickUp();
            Build();
        }
    }

    private void Controller()
    {
        //Camera
        if (!SettingsMenu.isPause)
        {
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, Mathf.Atan2(0, 0) * Mathf.Rad2Deg + cam.eulerAngles.y, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        //Movement
        velocity.z = Input.GetAxis("Vertical") * walkSpeed;
        velocity.x = Input.GetAxis("Horizontal") * strafeSpeed;
        velocity = transform.TransformDirection(velocity);

        //Jump
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = 0;
            jumpsSinceLastLand = 0;
        }

        if (Input.GetButtonDown("Jump") && !isSwimming)
        {
            if (controller.isGrounded || jumpsSinceLastLand < maxJumps)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
                jumpsSinceLastLand++;
                anim.SetTrigger("Jump");
                audioSource2.PlayOneShot(jump);
            }
        }

        //Gravity
        if (isSwimming)
        {
            if (Input.GetButton("Jump")) {
                velocity.y += Time.deltaTime * 4;
            }
            else if (Input.GetKey(sprintKey)) {
                velocity.y -= Time.deltaTime * 4;
            }
            else
            {
                velocity.y = 0;
            }

            if (transform.position.y < swimHeight.y - 1)
            {
                underwaterPostPro.SetActive(true);
                landPostPro.SetActive(false);
            }
        }
        else
        {
            velocity.y += Physics.gravity.y * Time.deltaTime * 2f;
            underwaterPostPro.SetActive(false);
            landPostPro.SetActive(true);
        }

        //Animations
        //Swimming
        if (transform.position.y <= swimHeight.y && transform.position.y >= swimHeight.x)
        {
            isSwimming = true;
        } else
        {
            isSwimming = false;
        }

        //Running
        if (Input.GetKey(sprintKey))
        {
            if (!isCrouched)
            {
                isRunning = true;
            }
        } else
        {
            isRunning = false;
        }

        //Crouching
        if (Input.GetKey(crouchKey))
        {
            if (!isRunning)
            {
                isCrouched = true;
            }
        } else
        {
            isCrouched = false;
        }

        //Stamina
        if (staminaSlider.value > 0)
        {
            stamina = true;
        } else
        {
            stamina = false;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            isIdle = false;
            //Running
            if (isRunning && !isCrouched && !isSwimming && stamina)
            {
                audioSource1.pitch = 1.6f;
                velocity.z *= sprintFactor;
                velocity.x *= sprintFactor;
                anim.SetTrigger("Run");
                staminaSlider.value -= 2f * Time.deltaTime;
            }
            //Crouching
            else if (!isRunning && isCrouched && !isSwimming)
            {
                audioSource1.pitch = 1.2f;
                velocity.z *= crouchFactor;
                velocity.x *= crouchFactor;
                anim.SetTrigger("CrouchWalk");
            }
            //Swimming
            else if (isSwimming)
            {
                anim.SetTrigger("Swim");
            }
            //Walking
            else if (!isRunning && !isCrouched && !isSwimming)
            {
                anim.SetTrigger("Walk");
            }

            //Rolling
            if (Input.GetKeyDown(rollKey) && controller.isGrounded && !isCrouched && !isSwimming)
            {
                velocity.z *= rollFactor;
                velocity.x *= rollFactor;
                anim.SetTrigger("Roll");
                staminaSlider.value -= 2f * Time.deltaTime;
            }
        }
        else
        {
            isIdle = true;
            StartCoroutine(RestoreStamina());
            //Idles
            if (isSwimming)
            {
                anim.SetTrigger("SwimIdle");
            }
            else if (isCrouched)
            {
                anim.SetTrigger("CrouchIdle");
            }
            else
            {
                anim.SetTrigger("Idle");
            }
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void Attack()
    {
        if (canAttack && stamina && isOpen)
        {
            //Sword
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Melee)
            {
                anim.SetInteger("SwordIndex", Random.Range(0, 3));
                anim.SetTrigger("Sword");
                StartCoroutine(ResetAttackCoolDown());
            }

            //Spear
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Ranged)
            {
                anim.SetInteger("SpearIndex", Random.Range(0, 2));
                anim.SetTrigger("Spear");
                StartCoroutine(ResetAttackCoolDown());
            }

            //Magic
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Magic)
            {
                anim.SetInteger("MagicIndex", Random.Range(0, 3));
                anim.SetTrigger("Magic");
                StartCoroutine(ResetAttackCoolDown());
            }

            //Shield
            if (Input.GetMouseButtonDown(1) && curShield != null && curShield.type == WeaponController.Types.Shield)
            {
                anim.SetInteger("ShieldIndex", Random.Range(0, 3));
                anim.SetTrigger("Shield");
                StartCoroutine(ResetAttackCoolDown());
            }
        }
    }

    private void PickUp()
    {
        //Open Inventory
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Inventory.SetActive(false);
                isOpen = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Inventory.SetActive(true);
                isOpen = false;
            }
        }

        //Hover
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit other, 200.0f)) {
            if (other.transform.tag == "Mushroom")
            {
                displayText.text = "F to consume";
                displayText.GetComponent<Animator>().Play("PopIn");
            }
            if (other.transform.tag == "Chest")
            {
                displayText.text = "F to open";
                displayText.GetComponent<Animator>().Play("PopIn");
            }
            if (other.transform.GetComponent<WeaponController>() != null)
            {
                if (other.transform.GetComponent<WeaponController>().isItem && (other.transform.tag == "Weapon" || other.transform.tag == "Shield" || other.transform.tag == "Gem"))
                {
                    displayText.text = "F to pick up";
                    displayText.GetComponent<Animator>().Play("PopIn");
                }
            }
            if (other.transform.tag == "EnterDungeon")
            {
                displayText.text = "F to enter dungeon";
                displayText.GetComponent<Animator>().Play("PopIn");
            }
            if (other.transform.tag == "LeaveDungeon")
            {
                displayText.text = "F to leave dungeon";
                displayText.GetComponent<Animator>().Play("PopIn");
            }
            if (other.transform.tag == "Runestone")
            {
                displayText.text = "F to summon " + other.transform.GetComponent<Runestone>().boss.name;
                displayText.GetComponent<Animator>().Play("PopIn");
            }
        }

        //Pickup
        if (Input.GetKeyDown(KeyCode.F))
        {
            RaycastHit hit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 200.0f))
            {
                if (hit.transform.tag == "Chest")
                {
                    hit.transform.gameObject.GetComponent<Chest>().SpawnLoot();
                    audioSource2.PlayOneShot(openChest);
                }

                if (hit.transform.tag == "Weapon" && !hit.transform.GetComponent<WeaponController>().isPlayer)
                {
                    if (canAttack == false)
                    {
                        return;
                    }

                    WeaponController weapon = hit.transform.GetComponent<WeaponController>();
                    weapon.isPlayer = true;
                    weapon.isAI = false;
                    weapon.isItem = false;

                    var otherItem = other.transform.GetComponent<Item>();
                    if (curWeapon == null)
                    {
                        hit.transform.SetParent(hand);
                        hit.transform.localPosition = Vector3.zero;
                        hit.transform.localRotation = Quaternion.identity;
                        hit.transform.localScale = new Vector3(1, 1, 1);
                        sword.item = otherItem;
                        sword.icon.sprite = otherItem.icon;
                        sword.text.text = otherItem.name;
                        curWeapon = weapon;
                        Items.Add(otherItem);
                    }
                    else
                    {
                        Instance.Add(otherItem);
                        other.transform.gameObject.SetActive(false);
                    }
                    audioSource2.PlayOneShot(equip);
                }

                if (hit.transform.tag == "Shield" && !hit.transform.GetComponent<WeaponController>().isPlayer)
                {
                    if (canAttack == false)
                    {
                        return;
                    }

                    WeaponController weapon = hit.transform.GetComponent<WeaponController>();
                    weapon.isPlayer = true;
                    weapon.isAI = false;
                    weapon.isItem = false;

                    var otherItem = other.transform.GetComponent<Item>();
                    if (curShield == null)
                    {
                        hit.transform.SetParent(offhand);
                        hit.transform.localPosition = Vector3.zero;
                        hit.transform.localRotation = Quaternion.identity;
                        hit.transform.localScale = new Vector3(1, 1, 1);
                        shield.item = otherItem;
                        shield.icon.sprite = otherItem.icon;
                        shield.text.text = otherItem.name;
                        curShield = weapon;
                        Items.Add(otherItem);
                    }
                    else
                    {
                        Instance.Add(otherItem);
                        other.transform.gameObject.SetActive(false);
                    }
                    audioSource2.PlayOneShot(equip);
                }

                if (hit.transform.tag == "Mushroom")
                {
                    Instance.Add(other.transform.GetComponent<Item>());
                    hit.transform.gameObject.SetActive(false);
                    audioSource2.PlayOneShot(equip);
                }

                if (hit.transform.tag == "Runestone")
                {
                    hit.transform.GetComponent<Runestone>().SpawnBoss();
                    spawnPos = hit.point;
                }

                if (hit.transform.tag == "Gem")
                {
                    Instance.Add(hit.transform.GetComponent<Item>());
                    audioSource2.PlayOneShot(pickupGem);
                    hit.transform.gameObject.SetActive(false);
                }

                if (hit.transform.tag == "EnterDungeon")
                {
                    fadeInUI.GetComponent<Animator>().Play("FadeIn");
                    controller.enabled = false;
                    dungeonPos = transform.position;
                    transform.position = other.transform.parent.GetComponent<DungeonGenerator>().dungeonLocation + new Vector3(0, 15, 0);
                    controller.enabled = true;
                }

                if (hit.transform.tag == "LeaveDungeon")
                {
                    fadeInUI.GetComponent<Animator>().Play("FadeIn");
                    controller.enabled = false;
                    transform.position = dungeonPos + new Vector3(10, 0, 10);
                    controller.enabled = true;
                }
            }
        }
    }

    private void Build()
    {
        if (pendingObject != null)
        {
            pendingObject.transform.position = pos;

            if (Input.GetMouseButtonDown(0))
            {
                pendingObject = null;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                pendingObject.transform.Rotate(Vector3.up, 90);
            }
        }

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 400, layerMask))
        {
            pos = hit.point;

            if (pendingObject != null)
            {
                pendingObject.transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal);
            }
        }
    }

    private IEnumerator ResetAttackCoolDown()
    {
        canAttack = false;
        isIdle = false;
        yield return new WaitForSeconds(attackCoolDown);
        canAttack = true;
    }

    private IEnumerator RestoreStamina()
    {
        yield return new WaitForSeconds(1);
        if (isIdle)
        {
            staminaSlider.value += 20f * Time.deltaTime;
        }
    }

    public void Step()
    {
        audioSource1.PlayOneShot(footstep);
    }

    public void SwordSound()
    {
        audioSource2.PlayOneShot(curWeapon.attackSound);
        CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
        staminaSlider.value -= 3;
    }

    public void ShieldSound()
    {
        audioSource2.PlayOneShot(curShield.attackSound);
        CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
        staminaSlider.value -= 3;
    }

    public void ThrowSpear()
    {
        audioSource2.PlayOneShot(curWeapon.attackSound);

        GameObject projectile = Instantiate(curWeapon.spear, curWeapon.transform.position, curWeapon.transform.rotation);
        Vector3 forceToAdd = transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
        curWeapon.gameObject.SetActive(false);
        staminaSlider.value -= 3;
        curWeapon.weaponDurability -= 0.1f;
    }

    public void ChangeSpear()
    {
        if (curWeapon != null)
        {
            curWeapon.gameObject.SetActive(true);
        }
    }

    public void CastSpell()
    {
        audioSource2.PlayOneShot(curWeapon.attackSound);

        GameObject projectile = Instantiate(curWeapon.spear, curWeapon.transform.position, curWeapon.transform.rotation);
        Vector3 forceToAdd = transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<WeaponController>().isPlayer = true;
        projectile.GetComponent<WeaponController>().weaponDamage = curWeapon.weaponDamage;
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
        staminaSlider.value -= 3;
        curWeapon.weaponDurability -= 0.1f;
    }

    public void Respawn()
    {
        health = maxHealth;
        healthSlider.value = health / maxHealth;
        staminaSlider.value = 100;
        anim.SetTrigger("Idle");
        if (curWeapon != null)
        {
            Destroy(curWeapon.gameObject);
        }
        if (curShield != null)
        {
            Destroy(curShield.gameObject);
        }
        controller.enabled = false;
        transform.position = spawnPos;
        controller.enabled = true;
        dead = false;
    }

    public void ChangeHealth(float amount)
    {
        if (!dead)
        {
            health += amount;
            damageUI.GetComponent<Animator>().Play("Damage");
            anim.SetTrigger("Hit");
            CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
            audioSource2.PlayOneShot(hurt);

            healthSlider.value = health / maxHealth;
        }

        if (health > maxHealth)
        {
            health = maxHealth;
        }

        if (health <= 0)
        {
            anim.SetTrigger("Die");
            dead = true;
        }
    }

    //Inventory
    public void Add(Item item)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (item.itemType != Item.ItemType.Weapon && Items[i].icon == item.icon)
            {
                Items[i].amount++;
                return;
            }
        }
        Items.Add(item);
        var obj = Instantiate(InventoryItem, ItemContent);
        var inventorySlot = obj.GetComponent<InventorySlot>();
        inventorySlot.item = item;
        inventorySlot.text.text = item.name;
        inventorySlot.icon.sprite = item.icon;
    }

    //Building
    public void SelectObject(Item item)
    {
        pendingObject = Instantiate(item.gameObject, pos, transform.rotation);
        pendingObject.SetActive(true);
    }
}

