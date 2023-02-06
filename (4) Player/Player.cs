using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;
using System;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{
    [Header("Player References")]
    public CharacterController controller;
    public Animator anim;
    public Transform cam;
    public Transform playerObj;
    public GameObject playerUI;
    public GameObject landPostPro;
    public GameObject underwaterPostPro;
    public GameObject dustParticle;
    public Slider healthSlider;
    public Slider staminaSlider;
    public Slider levelSlider;
    public GameObject damageUI;
    public GameObject fadeInUI;
    public GameObject damageBar;
    public TMP_Text displayText;
    public TMP_Text levelText;

    [Header("Player Input")]
    public KeyCode jumpKey;
    public KeyCode sprintKey;
    public KeyCode rollKey;
    public KeyCode crouchKey;

    [Header("Player Settings")]
    [Range(1, 100)] public float walkSpeed;
    [Range(1, 100)] public float strafeSpeed;
    [Range(1, 10)] public float sprintFactor;
    [Range(1, 10)] public float rollFactor;
    [Range(0.1f, 1)] public float crouchFactor;
    [Range(1, 20f)] public float rotationSpeed;
    [Range(1, 50)] public float jumpHeight;
    [Range(1, 10)] public int maxJumps;
    public Vector2 swimHeight;
    [SyncVar]
    public float health = 10000;
    public float maxHealth = 10000;
    public float xpNum;
    public int levelNum;
    public bool randomSpawnPos;
    public bool testing;

    [Header("Player Weapon")]
    public Transform hand;
    public Transform offhand;
    public InventorySlot sword;
    public InventorySlot shield;
    public GameObject leftHand;
    public GameObject rightHand;
    public WeaponController curWeapon;
    public WeaponController curShield;
    public bool canAttack;
    public float cameraShakeIntensity = 3f;
    public float attackCoolDown = 0.7f;

    [Header("Player Inventory")]
    public GameObject Inventory;
    public Transform ItemContent;
    public Transform AddItemContent;
    public GameObject InventoryItem;
    public GameObject AddItem;
    public LayerMask layerMask;
    public LayerMask layerMask2;
    public float gridSize;
    public bool isClosed = true;
    public List<Item> Items = new List<Item>();
    public static Player Instance;

    [Header("Audio")]
    public AudioSource audioSource1;
    public AudioClip footstep;
    public AudioSource audioSource2;
    public AudioClip jump;
    public AudioClip hurt;
    public AudioClip equip;
    public AudioClip pickupGem;
    public AudioClip heal;

    [Header("Mirror")]
    [SerializeField] private NetworkAnimator networkAnimator = null;
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;
    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool Ready;
    private CustomNetworkManager manager;

    //Private Variables
    //Booleans
    private bool isRunning;
    private bool isCrouched;
    private bool isSwimming;
    private bool isIdle;
    private bool isDead;
    private bool isRolling = true;
    private bool stamina = true;
    //Movement
    private Vector3 velocity = Vector3.zero;
    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    private int jumpsSinceLastLand = 0;
    //Builiding
    private GameObject pendingObject;
    private Vector3 objectPos;
    private Vector3 dungeonPos;
    private Vector3 spawnPos;
    //Instance
    public static Player playerInstance;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        health = maxHealth;
        healthSlider.value = health / maxHealth;
        staminaSlider.value = 100;
        audioSource1.clip = footstep;
        DontDestroyOnLoad(transform.gameObject);
        Instance = this;
        playerInstance = this;

        if (SceneManager.GetActiveScene().name == "Menu")
        {
            Cursor.lockState = CursorLockMode.None;
            playerObj.gameObject.SetActive(false);
            cam.gameObject.SetActive(false);
            cam.parent.gameObject.SetActive(false);
            playerUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isDead)
        {
            anim.Play("Die");
            return;
        }

        if ((SceneManager.GetActiveScene().name == "Singleplayer" || SceneManager.GetActiveScene().name == "Multiplayer"))
        {
            if (playerObj.transform.gameObject.activeSelf == false)
            {
                Cursor.lockState = CursorLockMode.Locked;
                playerObj.gameObject.SetActive(true);
                RandomPosition();

                if (isLocalPlayer)
                {
                    playerUI.SetActive(true);
                    cam.gameObject.SetActive(true);
                    cam.parent.gameObject.SetActive(true);
                    gameObject.tag = "Player";
                }
                else
                {
                    playerUI.SetActive(false);
                    cam.gameObject.SetActive(false);
                    cam.parent.gameObject.SetActive(false);
                    gameObject.tag = "OtherPlayer";
                }
            }

            if (isLocalPlayer)
            {
                Controller();
                Attack();
                PickUp();
                Build();
            }
        }

        if (testing)
        {
            Controller();
            Attack();
            PickUp();
            Build();
        }
    }

    private void RandomPosition()
    {
        if (randomSpawnPos)
        {
            spawnPos = new Vector3(Random.Range(-300, 300), 1500, Random.Range(-300, 300));
            controller.enabled = false;
            transform.position = spawnPos;
            controller.enabled = true;
        }
    }

    private void Controller()
    {
        //Movement
        velocity.z = Input.GetAxis("Vertical") * walkSpeed;
        velocity.x = Input.GetAxis("Horizontal") * strafeSpeed;
        velocity = transform.TransformDirection(velocity);
        Vector3 inputDir = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");
        if (inputDir != Vector3.zero)
            playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);

        //Jump
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = 0;
            jumpsSinceLastLand = 0;
        }

        if (Input.GetKey(jumpKey) && !isSwimming)
        {
            if (controller.isGrounded || jumpsSinceLastLand < maxJumps)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
                jumpsSinceLastLand++;
                networkAnimator.SetTrigger("Jump");
                audioSource2.PlayOneShot(jump);
            }
        }

        //Gravity
        if (isSwimming)
        {
            if (Input.GetKey(jumpKey)) {
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
            velocity.y += Physics.gravity.y * Time.deltaTime * 4f;
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
            //Camera
            if (!SettingsMenu.isPause)
            {
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, Mathf.Atan2(0, 0) * Mathf.Rad2Deg + cam.eulerAngles.y, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            isIdle = false;
            //Running
            if (isRunning && !isCrouched && !isSwimming && stamina)
            {
                audioSource1.pitch = 1.6f;
                velocity.z *= sprintFactor;
                velocity.x *= sprintFactor;
                networkAnimator.SetTrigger("Run");
                staminaSlider.value -= 2f * Time.deltaTime;
            }
            //Crouching
            else if (!isRunning && isCrouched && !isSwimming)
            {
                audioSource1.pitch = 1.2f;
                velocity.z *= crouchFactor;
                velocity.x *= crouchFactor;
                networkAnimator.SetTrigger("CrouchWalk");
            }
            //Swimming
            else if (isSwimming)
            {
                networkAnimator.SetTrigger("Swim");
            }
            //Walking
            else if (!isRunning && !isCrouched && !isSwimming)
            {
                networkAnimator.SetTrigger("Walk");
            }

            //Rolling
            if (isRolling && Input.GetKey(rollKey) && !isCrouched && !isSwimming)
            {
                anim.Play("Roll");
                velocity.z *= rollFactor;
                velocity.x *= rollFactor;
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
                networkAnimator.SetTrigger("SwimIdle");
            }
            else if (isCrouched)
            {
                networkAnimator.SetTrigger("CrouchIdle");
            }
            else
            {
                networkAnimator.SetTrigger("Idle");
            }
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void Attack()
    {
        if (curWeapon == null && curShield == null && pendingObject == null)
        {
            rightHand.SetActive(true);
            leftHand.SetActive(true);
        } else
        {
            rightHand.SetActive(false);
            leftHand.SetActive(false);
        }

        if (canAttack && stamina && isClosed && pendingObject == null)
        {
            //Punch
            if (Input.GetMouseButtonDown(0) && curWeapon == null && curShield == null)
            {
                anim.SetInteger("PunchIndex", Random.Range(0, 2));
                networkAnimator.SetTrigger("Punch");
                StartCoroutine(ResetAttackCoolDown());
            }

            //Sword
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Melee)
            {
                anim.SetInteger("SwordIndex", Random.Range(0, 4));
                networkAnimator.SetTrigger("Sword");
                StartCoroutine(ResetAttackCoolDown());
            }

            //Spear
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Ranged)
            {
                anim.SetInteger("SpearIndex", Random.Range(0, 2));
                networkAnimator.SetTrigger("Spear");
                StartCoroutine(ResetAttackCoolDown());
            }

            //Magic
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Magic)
            {
                anim.SetInteger("MagicIndex", Random.Range(0, 2));
                networkAnimator.SetTrigger("Magic");
                StartCoroutine(ResetAttackCoolDown());
            }

            //Shield
            if (Input.GetMouseButtonDown(1) && curShield != null && curShield.type == WeaponController.Types.Shield)
            {
                anim.SetInteger("ShieldIndex", Random.Range(0, 2));
                networkAnimator.SetTrigger("Shield");
                StartCoroutine(ResetAttackCoolDown());
            }
        }
    }

    private void PickUp()
    {
        //Open Inventory
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isClosed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Inventory.SetActive(false);
                isClosed = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Inventory.SetActive(true);
                isClosed = false;
            }
        }

        //Hover
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit other, 200.0f)) {
            if (other.transform.tag == "Item")
            {
                displayText.text = "F to pick up";
                displayText.GetComponent<Animator>().Play("PopIn");
            }

            if (other.transform.GetComponent<WeaponController>() != null)
            {
                if (other.transform.GetComponent<WeaponController>().isItem && (other.transform.tag == "Weapon" || other.transform.tag == "Shield"))
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

            if (other.transform.tag == "Villager")
            {
                displayText.text = "F to trade";
                displayText.GetComponent<Animator>().Play("PopIn");
            }
        }

        //Pickup
        if (Input.GetKeyDown(KeyCode.F))
        {
            RaycastHit hit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 200.0f))
            {
                if (hit.transform.tag == "Weapon" && !hit.transform.GetComponent<WeaponController>().isPlayer && hit.transform.GetComponent<WeaponController>().isItem)
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
                        hit.transform.localScale = new Vector3(10, 10, 10);
                        sword.item = otherItem;
                        sword.icon.sprite = otherItem.icon;
                        sword.text.text = otherItem.name;
                        curWeapon = weapon;
                        Items.Add(otherItem);
                        var obj2 = Instantiate(AddItem, AddItemContent);
                        var addItemSlot = obj2.GetComponent<AddItemSlot>();
                        addItemSlot.text.text = "+" + otherItem.icon.name;
                        addItemSlot.icon.sprite = otherItem.icon;
                        obj2.GetComponent<Animator>().Play("FadeOut");
                    }
                    else
                    {
                        Instance.Add(otherItem);
                        other.transform.gameObject.SetActive(false);
                    }
                    audioSource2.PlayOneShot(equip);
                }

                if (hit.transform.tag == "Shield" && !hit.transform.GetComponent<WeaponController>().isPlayer && hit.transform.GetComponent<WeaponController>().isItem)
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
                        hit.transform.localScale = new Vector3(10, 10, 10);
                        shield.item = otherItem;
                        shield.icon.sprite = otherItem.icon;
                        shield.text.text = otherItem.name;
                        curShield = weapon;
                        Items.Add(otherItem);
                        var obj2 = Instantiate(AddItem, AddItemContent);
                        var addItemSlot = obj2.GetComponent<AddItemSlot>();
                        addItemSlot.text.text = "+" + otherItem.icon.name;
                        addItemSlot.icon.sprite = otherItem.icon;
                        obj2.GetComponent<Animator>().Play("FadeOut");
                    }
                    else
                    {
                        Instance.Add(otherItem);
                        other.transform.gameObject.SetActive(false);
                    }
                    audioSource2.PlayOneShot(equip);
                }

                if (hit.transform.tag == "Item")
                {
                    Instance.Add(other.transform.GetComponent<Item>());
                    hit.transform.gameObject.SetActive(false);
                    audioSource2.PlayOneShot(equip);
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

                if (hit.transform.tag == "Villager")
                {
                    Debug.Log("Trade");
                }
            }
        }
    }

    private void Build()
    {
        if (pendingObject != null)
        {
            pendingObject.transform.position = new Vector3(RoundToNearestGrid(objectPos.x), RoundToNearestGrid(objectPos.y), RoundToNearestGrid(objectPos.z));

            if (Input.GetMouseButtonDown(0))
            {
                pendingObject.GetComponent<Collider>().enabled = true;
                pendingObject = null;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                pendingObject.transform.Rotate(Vector3.up, 90);
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                pendingObject.transform.Rotate(Vector3.left, 90);
            }
        }

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 400, layerMask2))
        {
            objectPos = hit.point;

            if (pendingObject != null)
            {
                //pendingObject.transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal);
                pendingObject.transform.position = new Vector3(RoundToNearestGrid(hit.point.x), RoundToNearestGrid(hit.point.y), RoundToNearestGrid(hit.point.z));
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

    public IEnumerator RestoreRoll()
    {
        isRolling = false;
        yield return new WaitForSeconds(2);
        isRolling = true;
    }

    public void Step()
    {
        if (controller.isGrounded)
        {
            audioSource1.PlayOneShot(footstep);
        }
    }

    public void LeftPunchSound()
    {
        audioSource2.PlayOneShot(leftHand.GetComponent<WeaponController>().attackSound);
        CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
        staminaSlider.value -= 3;
    }

    public void RightPunchSound()
    {
        audioSource2.PlayOneShot(rightHand.GetComponent<WeaponController>().attackSound);
        CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
        staminaSlider.value -= 3;
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
        Vector3 forceToAdd = cam.transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
        curWeapon.gameObject.SetActive(false);
        staminaSlider.value -= 3;
        curWeapon.CheckDurability();
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
        Vector3 forceToAdd = cam.transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<WeaponController>().isPlayer = true;
        projectile.GetComponent<WeaponController>().weaponDamage = curWeapon.weaponDamage;
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        CinemachineShake.Instance.ShakeCamera(cameraShakeIntensity, 0.1f);
        staminaSlider.value -= 3;
        curWeapon.CheckDurability();
    }

    public void JumpParticle()
    {
        GameObject particle = Instantiate(dustParticle, transform.position - new Vector3(0, 10, 0), Quaternion.identity);
        particle.transform.localScale = new Vector3(10, 10, 10);
        Destroy(particle, 2);
    }

    public void Respawn()
    {
        health = maxHealth;
        healthSlider.value = health / maxHealth;
        staminaSlider.value = 100;
        levelSlider.value = 0;
        levelNum = 0;
        levelText.text = "Level " + levelNum.ToString();
        networkAnimator.SetTrigger("Idle");
        if (curWeapon != null)
        {
            Destroy(curWeapon.gameObject);
        }
        if (curShield != null)
        {
            Destroy(curShield.gameObject);
        }
        for (int i = 0; i < Items.Count; i++)
        {
            Instance.Items.Remove(Items[i]);
        }
        for (int i = ItemContent.childCount - 1; i >= 0; i--)
        {
            Destroy(ItemContent.GetChild(i).gameObject);
        }
        sword.DefaultSword();
        shield.DefaultShield();
        controller.enabled = false;
        transform.position = spawnPos;
        controller.enabled = true;
        isDead = false;
    }

    public void ChangeHealth(float amount)
    {
        if (!isDead)
        {
            health += amount;
            damageUI.GetComponent<Animator>().Play("Damage");
            anim.SetInteger("HitIndex", Random.Range(0, 3));
            networkAnimator.SetTrigger("Hit");
            damageBar.GetComponent<Animator>().Play("DamageBar");
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
            isDead = true;
        }
    }

    //Inventory
    public void Add(Item item)
    {
        var obj2 = Instantiate(AddItem, AddItemContent);
        var addItemSlot = obj2.GetComponent<AddItemSlot>();
        addItemSlot.text.text = "+" + item.amount + " " + item.icon.name;
        addItemSlot.icon.sprite = item.icon;
        obj2.GetComponent<Animator>().Play("FadeOut");
        for (int i = 0; i < Items.Count; i++)
        {
            if ((item.itemType != Item.ItemType.Weapon && item.itemType != Item.ItemType.Shield) && Items[i].icon == item.icon && ((Items[i].amount + item.amount) <= item.maxAmount))
            {
                Items[i].amount += item.amount;
                Destroy(item.gameObject);
                return;
            }
        }
        Items.Add(item);
        var obj = Instantiate(InventoryItem, ItemContent);
        obj.transform.localRotation = Quaternion.identity;
        var inventorySlot = obj.GetComponent<InventorySlot>();
        item.isSpinning = false;
        inventorySlot.item = item;
        inventorySlot.text.text = item.icon.name;
        inventorySlot.icon.sprite = item.icon;
    }

    //Building
    public void SelectObject(Item item)
    {
        pendingObject = Instantiate(item.gameObject);
        pendingObject.GetComponent<Item>().amount = 1;
        pendingObject.transform.localScale = new Vector3(10, 10, 10);
        pendingObject.SetActive(true);
        pendingObject.GetComponent<Collider>().enabled = false;
    }

    public float RoundToNearestGrid(float pos)
    {
        float xDiff = pos % gridSize;
        pos -= xDiff;
        if (xDiff > (gridSize / 2))
        {
            pos += gridSize;
        }
        return pos;
    }

    //Mirror
    private CustomNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        if (isServer)
        {
            this.Ready = newValue;
        }
        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    [Command]
    private void CMdSetPlayerReady()
    {
        this.PlayerReadyUpdate(this.Ready, !this.Ready);
    }

    public void ChangeReady()
    {
        if (hasAuthority)
        {
            CMdSetPlayerReady();
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "Player";
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();
    }

    public override void OnStartClient()
    {
        Manager.GamePlayers.Add(this);
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        Manager.GamePlayers.Remove(this);
        LobbyController.Instance.UpdatePlayerList();
    }

    [Command]
    private void CmdSetPlayerName(string PlayerName)
    {
        this.PlayerNameUpdate(this.PlayerName, PlayerName);
    }

    public void PlayerNameUpdate(string OldValue, string NewValue)
    {
        if (isServer)
        {
            this.PlayerName = NewValue;
        }
        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    public void CanStartGame(string SceneName)
    {
        if (hasAuthority)
        {
            CmdCanStartGame(SceneName);
        }
    }

    [Command]
    public void CmdCanStartGame(string SceneName)
    {
        manager.StartGame(SceneName);
    }

    public IEnumerator CmdDestroyObject(GameObject gameObject, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        destroyObject(gameObject);
    }

    [Command]
    public void destroyObject(GameObject gameObject)
    {
        NetworkServer.Destroy(gameObject);
    }

    [Command]
    public void CmdTakeDamage(float damage)
    {
        health -= damage;
    }
}

