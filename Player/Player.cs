using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;
using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    [Header("Player References")]
    public CharacterController controller;
    public Animator anim;
    public NetworkAnimator networkAnim;
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
    public SettingsMenu settingsMenu;
    public TMP_Text healthText;
    public TMP_Text displayText;
    public TMP_Text levelText;
    public TMP_Text biomeText;
    public TMP_Text dayText;
    public TMP_Text coordinateText;
    public TMP_Text playerName;
    public NetworkVariable<FixedString128Bytes> networkPlayerName = new NetworkVariable<FixedString128Bytes>("Player: 0", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Player Input")]
    public KeyCode jumpKey;
    public KeyCode sprintKey;
    public KeyCode rollKey;
    public KeyCode crouchKey;
    public KeyCode inventoryKey;
    public KeyCode pickupKey;

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
    [SerializeField] private NetworkVariable<float> health = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public float maxHealth;
    public float xpNum;
    public int levelNum;
    public bool randomSpawnPos;

    [Header("Player Weapon")]
    public Transform hand;
    public Transform offhand;
    public InventorySlot sword;
    public InventorySlot shield;
    public GameObject leftHand;
    public GameObject rightHand;
    public WeaponController curWeapon;
    public WeaponController curShield;
    public float attackCoolDown;
    public bool canAttack;

    [Header("Player Inventory")]
    public GameObject Inventory;
    public Transform ItemContent;
    public Transform AddItemContent;
    public GameObject InventoryItem;
    public GameObject AddItem;
    public LayerMask layerMask;
    public LayerMask buildMask;
    public float gridSize;
    public int maxItemsStored;
    public bool isClosed = true;
    public List<Item> Items = new List<Item>();

    [Header("Audio")]
    public AudioSource audioSource1;
    public AudioClip footstep;
    public AudioSource audioSource2;
    public AudioClip jump;
    public AudioClip hurt;
    public AudioClip equip;
    public AudioClip pickupGem;
    public AudioClip heal;

    //Private Variables
    //Booleans
    private bool isRunning;
    private bool isCrouched;
    private bool isSwimming;
    private bool isIdle;
    private bool isRolling = true;
    private bool stamina = true;
    private bool leftPunch;
    private bool rightPunch;
    //Movement
    private Vector3 velocity = Vector3.zero;
    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    private int jumpsSinceLastLand = 0;
    //Builiding
    public GameObject pendingObject;
    private Vector3 objectPos;
    private Vector3 dungeonPos;
    private Vector3 spawnPos;
    //Instance
    public static Player playerInstance; 

    public override void OnNetworkSpawn()
    {
        DontDestroyOnLoad(transform.gameObject);
        Cursor.lockState = CursorLockMode.Locked;
        health.Value = maxHealth;
        healthSlider.value = health.Value / maxHealth;
        staminaSlider.value = 100;
        audioSource1.clip = footstep;

        if (!IsOwner)
        {
            playerUI.SetActive(false);
            cam.gameObject.SetActive(false);
            cam.parent.gameObject.SetActive(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            playerObj.gameObject.SetActive(true);
            playerUI.SetActive(true);
            cam.gameObject.SetActive(true);
            cam.parent.gameObject.SetActive(true);
            RandomPosition();

            var storeVariables = FindObjectOfType<StoreVariables>();
            if (storeVariables != null)
            {
                settingsMenu.slider.value = storeVariables.sliderValue;
                settingsMenu.graphicsDropdown.value = storeVariables.graphicsValue;
            }
        }

        if (IsOwner)
        {
            playerInstance = this;
            networkPlayerName.Value = "Player: " + (OwnerClientId + 1);
            playerName.text = networkPlayerName.Value.ToString();
        }
    }

    private void Update()
    {
        if (!IsOwner) { return; }
        Controller();
        Attack();
        PickUp();
        Build();
    }

    private void RandomPosition()
    {
        if (randomSpawnPos)
        {
            spawnPos = new Vector3(Random.Range(-300, 300), 800, Random.Range(-300, 300));
            controller.enabled = false;
            transform.position = spawnPos;
            controller.enabled = true;
        }
    }

    private void Controller()
    {
        //Dead
        if (health.Value <= 0)
        {
            anim.Play("Die");
            return;
        }

        //Movement
        var verticalAxis = Input.GetAxis("Vertical");
        var horizontalAxis = Input.GetAxis("Horizontal");
        velocity.z = verticalAxis * walkSpeed;
        velocity.x = horizontalAxis * strafeSpeed;
        velocity = transform.TransformDirection(velocity);
        Vector3 inputDir = transform.forward * verticalAxis + transform.right * horizontalAxis;
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
                networkAnim.SetTrigger("Jump");
                audioSource2.PlayOneShot(jump);
            }
        }

        //Gravity
        if (isSwimming)
        {
            if (Input.GetKey(jumpKey)) {
                velocity.y += Time.deltaTime * 6;
            }
            else if (Input.GetKey(sprintKey)) {
                velocity.y -= Time.deltaTime * 6;
            }
            else
            {
                velocity.y = 0;
            }
        }
        else
        {
            velocity.y += Physics.gravity.y * Time.deltaTime * 6f;
        }

        //Post Processing
        if (cam.transform.position.y < swimHeight.y - 1)
        {
            underwaterPostPro.SetActive(true);
            landPostPro.SetActive(false);
        }
        else
        {
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
            } else
            {
                isRunning = false;
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
            else
            {
                isCrouched = false;
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
                networkAnim.SetTrigger("Run");
                staminaSlider.value -= 4f * Time.deltaTime;
            }
            //Crouching
            else if (!isRunning && isCrouched && !isSwimming)
            {
                audioSource1.pitch = 1.2f;
                velocity.z *= crouchFactor;
                velocity.x *= crouchFactor;
                networkAnim.SetTrigger("CrouchWalk");
            }
            //Swimming
            else if (isSwimming)
            {
                networkAnim.SetTrigger("Swim");
            }
            //Walking
            else if (!isRunning && !isCrouched && !isSwimming)
            {
                networkAnim.SetTrigger("Walk");
                StartCoroutine(RestoreStamina());
            }

            //Rolling
            if (isRolling && Input.GetKey(rollKey) && !isCrouched && !isSwimming)
            {
                anim.Play("Roll");
                velocity.z *= rollFactor;
                velocity.x *= rollFactor;
                staminaSlider.value -= 4f * Time.deltaTime;
            }
        }
        else
        {
            isIdle = true;
            StartCoroutine(RestoreStamina());
            //Idles
            if (isSwimming)
            {
                networkAnim.SetTrigger("SwimIdle");
            }
            else if (isCrouched)
            {
                networkAnim.SetTrigger("CrouchIdle");
            }
            else
            {
                networkAnim.SetTrigger("Idle");
            }
        }
        coordinateText.text = "X,Z:" + " (" + (int)transform.position.x + "," + (int)transform.position.z + ")";
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

        if (canAttack && isClosed && pendingObject == null)
        {
            //Punch
            if (Input.GetMouseButtonDown(0) && curWeapon == null && curShield == null)
            {
                anim.SetInteger("PunchIndex", Random.Range(0, 2));
                networkAnim.SetTrigger("Punch");
            }

            //Sword
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Melee)
            {
                anim.SetInteger("SwordIndex", Random.Range(0, 4));
                networkAnim.SetTrigger("Sword");
            }

            //Spear
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Ranged)
            {
                anim.SetInteger("SpearIndex", Random.Range(0, 2));
                networkAnim.SetTrigger("Spear");
            }

            //Magic
            if (Input.GetMouseButtonDown(0) && curWeapon != null && curWeapon.type == WeaponController.Types.Magic)
            {
                anim.SetInteger("MagicIndex", Random.Range(0, 2));
                networkAnim.SetTrigger("Magic");
            }

            //Shield
            if (Input.GetMouseButtonDown(1) && curShield != null && curShield.type == WeaponController.Types.Shield)
            {
                anim.SetInteger("ShieldIndex", Random.Range(0, 2));
                networkAnim.SetTrigger("Shield");
            }
        }
    }

    private void PickUp()
    {
        //Open Inventory
        if (Input.GetKeyDown(inventoryKey))
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

            if (other.transform.tag == "Runestone")
            {
                displayText.text = "F to summon Boss";
                displayText.GetComponent<Animator>().Play("PopIn");
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
        if (Input.GetKeyDown(pickupKey))
        {
            RaycastHit hit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 200.0f))
            {
                if (hit.transform.tag == "Weapon" && !hit.transform.GetComponent<WeaponController>().isPlayer && hit.transform.GetComponent<WeaponController>().isItem && Items.Count <= maxItemsStored)
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
                        Add(otherItem);
                        other.transform.gameObject.SetActive(false);
                    }
                    audioSource2.PlayOneShot(equip);
                    curWeapon.GetComponent<Collider>().isTrigger = false;
                }

                if (hit.transform.tag == "Shield" && !hit.transform.GetComponent<WeaponController>().isPlayer && hit.transform.GetComponent<WeaponController>().isItem && Items.Count <= maxItemsStored)
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
                        Add(otherItem);
                        other.transform.gameObject.SetActive(false);
                    }
                    audioSource2.PlayOneShot(equip);
                    curShield.GetComponent<Collider>().isTrigger = false;
                }

                if (hit.transform.tag == "Item" && Items.Count <= maxItemsStored)
                {
                    var item = hit.transform.GetComponent<Item>();
                    //item.isSpinning = false;
                    Add(item);
                    item.setActiveServerRpc(false);
                    audioSource2.PlayOneShot(equip);
                }

                if (hit.transform.tag == "Runestone")
                {
                    hit.transform.gameObject.GetComponent<Runestone>().SpawnBoss();
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

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 400, buildMask))
        {
            objectPos = hit.point;

            if (pendingObject != null)
            {
                pendingObject.transform.position = new Vector3(RoundToNearestGrid(hit.point.x), RoundToNearestGrid(hit.point.y), RoundToNearestGrid(hit.point.z));
            }
        }
    }

    private IEnumerator ResetAttackCoolDown()
    {
        canAttack = false;
        isIdle = false;

        if (leftPunch)
        {
            rightHand.GetComponent<Collider>().enabled = true;
        }
        if (rightPunch)
        {
            leftHand.GetComponent<Collider>().enabled = true;
        }

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
        leftPunch = false;
        rightPunch = false;

        leftHand.GetComponent<Collider>().enabled = false;
        rightHand.GetComponent<Collider>().enabled = false;
        if (curWeapon != null)
        {
            curWeapon.GetComponent<Collider>().enabled = false;
        }
        if (curShield != null)
        {
            curShield.GetComponent<Collider>().enabled = false;
        }
    }

    private IEnumerator RestoreStamina()
    {
        yield return new WaitForSeconds(1);
        if (isIdle || (!isRunning && !isCrouched && !isSwimming))
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
        if (!IsOwner) { return; }
        audioSource1.PlayOneShot(footstep);
    }

    public void LeftPunchSound()
    {
        if (!IsOwner) { return; }
        audioSource2.PlayOneShot(leftHand.GetComponent<WeaponController>().attackSound);
        staminaSlider.value -= 5;
        leftPunch = true;
        StartCoroutine(ResetAttackCoolDown());
    }

    public void RightPunchSound()
    {
        if (!IsOwner) { return; }
        audioSource2.PlayOneShot(rightHand.GetComponent<WeaponController>().attackSound);
        staminaSlider.value -= 5;
        rightPunch = true;
        StartCoroutine(ResetAttackCoolDown());
    }

    public void SwordSound()
    {
        if (!IsOwner) { return; }
        audioSource2.PlayOneShot(curWeapon.attackSound);
        staminaSlider.value -= 5;
        StartCoroutine(ResetAttackCoolDown());
    }

    public void ShieldSound()
    {
        if (!IsOwner) { return; }
        audioSource2.PlayOneShot(curShield.attackSound);
        staminaSlider.value -= 5;
        StartCoroutine(ResetAttackCoolDown());
    }

    public void ThrowSpear()
    {
        if (!IsOwner) { return; }
        audioSource2.PlayOneShot(curWeapon.attackSound);

        GameObject projectile = Instantiate(curWeapon.spear, curWeapon.transform.position, curWeapon.transform.rotation);
        Vector3 forceToAdd = cam.transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        curWeapon.gameObject.SetActive(false);
        staminaSlider.value -= 5;
        curWeapon.CheckDurability();
        StartCoroutine(ResetAttackCoolDown());
    }

    public void ChangeSpear()
    {
        if (!IsOwner) { return; }
        if (curWeapon != null)
        {
            curWeapon.gameObject.SetActive(true);
        }
    }

    public void CastSpell()
    {
        if (!IsOwner) { return; }
        audioSource2.PlayOneShot(curWeapon.attackSound);

        GameObject projectile = Instantiate(curWeapon.spear, curWeapon.transform.position, curWeapon.transform.rotation);
        Vector3 forceToAdd = cam.transform.forward * curWeapon.throwForce + new Vector3(Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy), Random.Range(0, curWeapon.accuracy));

        projectile.SetActive(true);
        projectile.AddComponent<Rigidbody>();
        projectile.AddComponent<Projectile>();
        projectile.GetComponent<WeaponController>().isPlayer = true;
        projectile.GetComponent<WeaponController>().weaponDamage = curWeapon.weaponDamage;
        projectile.GetComponent<Rigidbody>().AddForce(forceToAdd, ForceMode.Impulse);
        staminaSlider.value -= 5;
        curWeapon.CheckDurability();
        StartCoroutine(ResetAttackCoolDown());
    }

    public void JumpParticle()
    {
        GameObject particle = Instantiate(dustParticle, transform.position - new Vector3(0, 10, 0), Quaternion.identity);
        particle.transform.localScale = new Vector3(10, 10, 10);
        Destroy(particle, 2);
    }

    public void Respawn()
    {
        health.Value = maxHealth;
        healthSlider.value = health.Value / maxHealth;
        healthText.text = health.Value + "/" + maxHealth;
        staminaSlider.value = 100;
        levelSlider.value = 0;
        levelNum = 0;
        levelText.text = "Level " + levelNum.ToString();
        networkAnim.SetTrigger("Idle");
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
            Items.Remove(Items[i]);
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
    }

    public void ChangeHealth(float amount)
    {
        ChangeHealthServerRpc(amount);
        damageUI.GetComponent<Animator>().Play("Damage");
        anim.SetInteger("HitIndex", Random.Range(0, 3));
        networkAnim.SetTrigger("Hit");
        audioSource2.PlayOneShot(hurt);
        healthSlider.value = health.Value / maxHealth;
        healthText.text = health.Value + "/" + maxHealth;

        if (health.Value > maxHealth) { 
            health.Value = maxHealth; 
        }
    }

    //Inventory
    public void Add(Item item)
    {
        if (!IsOwner) { return; }
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
        inventorySlot.item = item;
        inventorySlot.text.text = item.icon.name;
        inventorySlot.icon.sprite = item.icon;
        item.gameObject.GetComponent<Collider>().isTrigger = false;
    }

    //Building
    public void SelectObject(GameObject item)
    {
        pendingObject = item;
        pendingObject.transform.localScale = item.transform.localScale;
        pendingObject.GetComponent<Collider>().enabled = false;
        pendingObject.SetActive(true);
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

    //Netcoding
    [ServerRpc(RequireOwnership = false)]
    public void ChangeHealthServerRpc(float amount)
    {
        ChangeHealthClientRpc(amount);
    }

    [ClientRpc]
    public void ChangeHealthClientRpc(float amount)
    {
        if (!IsOwner) { return; }

        health.Value += amount;
    }
}

