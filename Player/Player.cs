using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.SceneManagement;
using Steamworks;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    [Header("Player References")]
    public CharacterController controller;
    public NetworkAnimator networkAnimator;
    public Animator animator;
    public GameObject cameraController;
    public Transform playerCamera;
    public Transform playerObject;
    public GameObject playerUI;
    public GameObject playerNameTag;
    public Canvas settingsMenu;

    [Header("Player UI")]
    public GameObject landPostPro;
    public GameObject underwaterPostPro;
    public GameObject damageUI;
    public Slider healthSlider;
    public Slider hungerSlider;
    public Slider staminaSlider;
    public Slider objectSlider;
    public TMP_Text healthText;
    public TMP_Text displayText;
    public TMP_Text dayText;
    public TMP_Text coordinateText;
    public TMP_Text objectText;

    [Header("Player Settings")]
    [Range(1, 50)] public float walkSpeed;
    [Range(1, 5)] public float sprintFactor;
    [Range(1, 5)] public float swimFactor;
    [Range(1, 5)] public float rollFactor;
    [Range(0.1f, 1)] public float crouchFactor;
    [Range(1, 25)] public float jumpHeight;
    public Vector2 swimHeight;
    [SerializeField] private NetworkVariable<float> health = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<FixedString512Bytes> name = new NetworkVariable<FixedString512Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Player Weapon")]
    public Transform hand;
    public Transform offhand;
    public GameObject leftHand;
    public GameObject rightHand;
    public float attackCoolDown;
    public GameObject[] helmet;
    public GameObject[] chestplate;
    public GameObject[] leggings;
    public GameObject[] boots;
    public GameObject[] boots2;

    [Header("Player Inventory")]
    public GameObject Inventory;
    public GameObject InventoryItem;
    public GameObject AddItem;
    public Transform ItemContent;
    public Transform AddItemContent;
    public Transform defaultContent;
    public Transform materialsContent;
    public Transform craftingContent;
    public Transform fletchingContent;
    public Transform anvilContent;
    public Transform furnanceContent;
    public CraftingButton craftingButton;
    public int maxItemsStored;
    public List<Item> Items = new List<Item>();

    [Header("Player Building")]
    public LayerMask buildMask;
    public Material normalMat;
    public Material transparentMat;
    public InventorySlot currentSlot;
    public Item currentObject;
    public GameObject pendingObject;
    public float gridSize;

    [Header("Player Audio")]
    public AudioSource audioSource1;
    public AudioClip footstep;
    public AudioSource audioSource2;
    public AudioClip jump;
    public AudioClip hurt;
    public AudioClip equip;

    [Header("Debug Variables")]
    public WeaponController curWeapon;
    public WeaponController curShield;
    public Armor currentHelmet;
    public Armor currentChestplate;
    public Armor currentLeggings;
    public Armor currentBoots;
    public Recipe currentRecipe;
    public bool randomSpawnPos;
    public bool canAttack;
    public bool isClosed = true;

    //Booleans
    private bool isIdle;
    private bool isRunning;
    private bool isCrouched;
    private bool isSwimming;
    private bool isJumping = true;
    private bool isRolling = true;
    private bool stamina = true;
    private bool hunger = true;
    private bool isGrounded;
    private bool canDrown;
    private bool leftPunch;
    private bool rightPunch;
    private bool isLoaded;
    private bool isBlocking;
    //Movement
    private Vector3 velocity = Vector3.zero;
    private float turnSmoothVelocity;
    private int jumpsSinceLastLand = 0;
    private Vector3 spawnPos;
    public static Player instance;
    private SettingsMenu settings;

    public override void OnNetworkSpawn()
    {
        audioSource1.clip = footstep;
        leftHand.GetComponent<WeaponController>().setWeaponServerRpc(true, false);
        rightHand.GetComponent<WeaponController>().setWeaponServerRpc(true, false);
        settings = settingsMenu.GetComponent<SettingsMenu>();

        if (randomSpawnPos)
        {
            spawnPos = new Vector3(Random.Range(-50, 50), 200, Random.Range(-50, 50));
            controller.enabled = false;
            transform.position = spawnPos;
            controller.enabled = true;
        }
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            cameraController.SetActive(false);
            playerUI.SetActive(false);
            playerObject.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            NameServerRpc();
            Cursor.visible = true;
        }

        if (SceneManager.GetActiveScene().name == "Game" && !isLoaded)
        {
            if (!IsOwner)
            {
                cameraController.SetActive(false);
                playerUI.SetActive(false);
                playerObject.gameObject.SetActive(true);
            }
            else
            {
                cameraController.SetActive(true);
                playerUI.SetActive(true);
                playerObject.gameObject.SetActive(true);
                instance = this;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                playerNameTag.gameObject.SetActive(false);
            }
            isLoaded = true;
        }

        if (!IsOwner || health.Value <= 0 || SceneManager.GetActiveScene().name == "Menu") { return; }

        Controller();
        Attack();
        PickUp();
        Build();
    }

    private void Controller()
    {
        //Movement
        var verticalAxis = Input.GetAxis("Vertical");
        var horizontalAxis = Input.GetAxis("Horizontal");
        velocity.z = verticalAxis * walkSpeed;
        velocity.x = horizontalAxis * walkSpeed;
        velocity = transform.TransformDirection(velocity);
        Vector3 inputDir = transform.forward * verticalAxis + transform.right * horizontalAxis;
        if (inputDir != Vector3.zero)
            playerObject.forward = Vector3.Slerp(playerObject.forward, inputDir.normalized, Time.deltaTime * 5);

        //Jump
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = 0;
            jumpsSinceLastLand = 0;
        }

        if (Input.GetKey(settings.keys["Jump"]) && !isSwimming && isJumping && stamina && isGrounded)
        {
            if (controller.isGrounded || jumpsSinceLastLand < 1)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
                jumpsSinceLastLand++;
                networkAnimator.SetTrigger("Jump");
                staminaSlider.value -= 6f;
                hungerSlider.value -= 1f;
                audioSource2.PlayOneShot(jump);
                StartCoroutine(ResetJump());
            }
        }

        //Gravity
        if (isSwimming)
        {
            if (Input.GetKey(settings.keys["Jump"]))
            {
                velocity.y += Time.deltaTime * 3;
            }
            else if (Input.GetKey(settings.keys["Sprint"]))
            {
                velocity.y -= Time.deltaTime * 3;
            }
            else
            {
                velocity.y = 0;
            }
        }
        else
        {
            velocity.y += Physics.gravity.y * Time.deltaTime * 3f;
        }

        //Grounded
        bool previousGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position + Vector3.up * .001f, Vector3.down, 5);

        if (!isSwimming)
        {
            if (!isGrounded && (controller.velocity.y < -1))
            {
                animator.SetBool("Fall", true);
            }

            if (!previousGrounded && isGrounded)
            {
                animator.SetBool("Fall", false);
                networkAnimator.SetTrigger("Land");
            }
        }

        //Post Processing
        if (playerCamera.transform.position.y <= swimHeight.y)
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
            animator.SetBool("Fall", false);
        }
        else
        {
            isSwimming = false;
        }

        if (transform.position.y <= swimHeight.y)
        {
            canDrown = true;
            staminaSlider.value -= 6f * Time.deltaTime;
            hungerSlider.value -= 1f * Time.deltaTime;
            if (!stamina)
            {
                health.Value -= 8 * Time.deltaTime;
                healthSlider.value = health.Value;
                healthText.text = (int)health.Value + "/" + 100;
                if (health.Value <= 0)
                {
                    Respawn();
                }
            }
        }
        else
        {
            canDrown = false;
        }

        //Crouching
        if (Input.GetKey(settings.keys["Crouch"]))
        {
            if (!isRunning)
                isCrouched = true;
        }
        else
        {
            isCrouched = false;
        }

        //Stamina
        if (staminaSlider.value > 0)
        {
            stamina = true;
        }
        else
        {
            stamina = false;
        }

        //Hunger
        if (hungerSlider.value > 0)
        {
            hunger = true;
        }
        else
        {
            hunger = false;
        }

        if (Input.GetKey(settings.keys["Forward"]) || Input.GetKey(settings.keys["Left"]) || Input.GetKey(settings.keys["Right"]) || Input.GetKey(settings.keys["Backward"]))
        {
            //Camera
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, Mathf.Atan2(0, 0) * Mathf.Rad2Deg + playerCamera.eulerAngles.y, ref turnSmoothVelocity, 0.1f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            isIdle = false;

            //Check Running
            if (Input.GetKey(settings.keys["Sprint"]))
            {
                if (!isCrouched)
                    isRunning = true;
            }
            else
            {
                isRunning = false;
            }

            //Running
            if (isRunning && !isCrouched && !isSwimming && stamina)
            {
                audioSource1.pitch = 1.6f;
                velocity.z *= sprintFactor;
                velocity.x *= sprintFactor;
                networkAnimator.SetTrigger("Run");
                staminaSlider.value -= 6f * Time.deltaTime;
                hungerSlider.value -= 1f * Time.deltaTime;
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
                velocity.z *= swimFactor;
                velocity.x *= swimFactor;
            }
            //Walking
            else
            {
                networkAnimator.SetTrigger("Walk");
                StartCoroutine(RestoreStamina());
            }

            //Rolling
            if (Input.GetKey(settings.keys["Roll"]) && !isCrouched && !isSwimming && stamina && isRolling)
            {
                animator.Play("Roll");
                velocity.z *= rollFactor;
                velocity.x *= rollFactor;
                staminaSlider.value -= 8f * Time.deltaTime;
                hungerSlider.value -= 1f * Time.deltaTime;
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

        coordinateText.text = "X,Z:" + " (" + (int)transform.position.x + "," + (int)transform.position.z + ")";

        controller.Move(velocity * Time.deltaTime);
    }

    private void Attack()
    {
        //Punch
        if (curWeapon == null && curShield == null)
        {
            rightHand.SetActive(true);
            leftHand.SetActive(true);
            if (Input.GetMouseButtonDown(0) && canAttack && isClosed && currentObject == null && settingsMenu.enabled == false)
            {
                animator.SetInteger("PunchIndex", Random.Range(0, 2));
                networkAnimator.SetTrigger("Punch");
            }
        }
        else
        {
            rightHand.SetActive(false);
            leftHand.SetActive(false);
        }

        if (canAttack && isClosed && currentObject == null && settingsMenu.enabled == false)
        {
            if (curWeapon != null && Input.GetMouseButtonDown(0) && !isBlocking)
            {
                if (curWeapon.GetComponent<WeaponController>().type == WeaponController.Types.Bow)
                {
                    curWeapon.weaponTrail.Play();
                    networkAnimator.SetTrigger("Bow");
                } else
                {
                    curWeapon.weaponTrail.Play();
                    animator.SetInteger("SwordIndex", Random.Range(0, 4));
                    networkAnimator.SetTrigger("Sword");
                }
            }

            if (curShield != null && Input.GetMouseButtonDown(1))
            {
                curShield.weaponTrail.Play();
                if (isBlocking)
                {
                    animator.SetBool("Shield", false);
                    isBlocking = false;
                }
                else
                {
                    animator.SetBool("Shield", true);
                    isBlocking = true;
                }
            }
        }

        if (curShield == null)
        {
            animator.SetBool("Shield", false);
            isBlocking = false;
        }
    }

    private void PickUp()
    {
        //Open Inventory
        if (Input.GetKeyDown(settings.keys["Inventory"]) && !settingsMenu.enabled)
        {
            if (!isClosed)
            {
                isClosed = true;
                Inventory.SetActive(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                //Hide Contents
                defaultContent.gameObject.SetActive(true);
                craftingContent.gameObject.SetActive(false);
                fletchingContent.gameObject.SetActive(false);
                anvilContent.gameObject.SetActive(false);
                furnanceContent.gameObject.SetActive(false);

                //Destroy Materials Contents
                for (int i = materialsContent.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(materialsContent.transform.GetChild(i).gameObject);
                }

                //Reset Crafting Button
                craftingButton.recipe = null;
                craftingButton.icon.sprite = null;
                craftingButton.text.text = "Craft Item";
            }
            else
            {
                isClosed = false;
                Inventory.SetActive(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        //Open Settings
        if (Input.GetKeyDown(KeyCode.Escape) && isClosed)
        {
            if (!settingsMenu.enabled)
            {
                settingsMenu.enabled = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                settingsMenu.enabled = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        //Hover
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit other, 20f))
        {
            //Display Resource Health
            if (other.transform.GetComponent<Resource>() != null)
            {
                var resource = other.transform.GetComponent<Resource>();
                objectSlider.gameObject.SetActive(true);
                objectSlider.value = resource.networkHealth.Value / resource.maxHealth;
                objectText.text = resource.resourceName + ": \n" + resource.networkHealth.Value + "/" + resource.maxHealth;
            }
            else
            {
                objectSlider.gameObject.SetActive(false);
            }

            //Interact Key to Pickup Item
            if (other.transform.tag == "Item")
            {
                displayText.text = settings.keys["Interact"] + " to pick up " + other.transform.GetComponent<Item>().icon.name;
                displayText.GetComponent<Animator>().Play("PopIn");
            }

            //Interact Key to Open Chest
            if (other.transform.tag == "Chest")
            {
                displayText.text = settings.keys["Interact"] + " to open chest";
                displayText.GetComponent<Animator>().Play("PopIn");
            }

            //Open Workbench
            if (Input.GetMouseButtonDown(1) && other.transform.GetComponent<Item>() != null)
            {
                //Hide every content
                defaultContent.gameObject.SetActive(false);
                craftingContent.gameObject.SetActive(false);
                fletchingContent.gameObject.SetActive(false);
                anvilContent.gameObject.SetActive(false);
                furnanceContent.gameObject.SetActive(false);

                if (other.transform.GetComponent<Item>().icon.name == "Workbench")
                {
                    craftingContent.gameObject.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Inventory.SetActive(true);
                    isClosed = false;
                }
                else if (other.transform.GetComponent<Item>().icon.name == "Fletching Table")
                {
                    fletchingContent.gameObject.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Inventory.SetActive(true);
                    isClosed = false;
                }
                else if (other.transform.GetComponent<Item>().icon.name == "Anvil")
                {
                    anvilContent.gameObject.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Inventory.SetActive(true);
                    isClosed = false;
                }
                else if (other.transform.GetComponent<Item>().icon.name == "Furnance")
                {
                    furnanceContent.gameObject.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Inventory.SetActive(true);
                    isClosed = false;
                }
            }

            //Pickup Item
            if (Input.GetKeyDown(settings.keys["Interact"]))
            {
                //Pickup Items
                if (other.transform.tag == "Item")
                {
                    if (Items.Count > maxItemsStored) { return; }

                    var item = other.transform.GetComponent<Item>();
                    InventoryAdd(item);
                    item.setActiveServerRpc(false);
                    audioSource2.PlayOneShot(equip);
                }

                //Pickup Weapon
                if (other.transform.tag == "Weapon")
                {
                    if (!canAttack || other.transform.GetComponent<WeaponController>().isPlayer.Value || (Items.Count > maxItemsStored)) { return; }

                    //Set Weapon to Player's
                    var weapon = other.transform.GetComponent<WeaponController>();
                    weapon.setWeaponServerRpc(true, false);

                    //Add to Player Inventory
                    var item = other.transform.GetComponent<Item>();
                    InventoryAdd(item);
                    item.setActiveServerRpc(false);
                    audioSource2.PlayOneShot(equip);
                }

                //Open Chests
                if (other.transform.tag == "Chest")
                {
                    var chest = other.transform.GetComponent<Resource>();
                    audioSource2.PlayOneShot(chest.hitSound);
                    var effect = Instantiate(chest.hitEffect, chest.transform.position, Quaternion.identity);
                    Destroy(effect, 2);
                    chest.DestroyedServerRpc();
                }
            }
        }
        else
        {
            objectSlider.gameObject.SetActive(false);
        }
    }

    public void checkPickUp(GameObject other)
    {
        //Pickup Items
        if (other.transform.tag == "Item")
        {
            if (Items.Count > maxItemsStored) { return; }

            var item = other.transform.GetComponent<Item>();
            InventoryAdd(item);
            item.setActiveServerRpc(false);
            audioSource2.PlayOneShot(equip);
        }

        //Pickup Weapon
        if (other.transform.tag == "Weapon")
        {
            if (!canAttack || other.transform.GetComponent<WeaponController>().isPlayer.Value || (Items.Count > maxItemsStored)) { return; }

            //Change Weapon status to Player's
            var weapon = other.transform.GetComponent<WeaponController>();
            weapon.setWeaponServerRpc(true, false);

            //Add to Player Inventory
            var item = other.transform.GetComponent<Item>();
            InventoryAdd(item);
            item.setActiveServerRpc(false);
            audioSource2.PlayOneShot(equip);
        }
    }

    private void Build()
    {
        if (pendingObject == null) { return; }

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 20, buildMask))
        {
            pendingObject.transform.position = new Vector3(RoundToNearestGrid(hit.point.x), RoundToNearestGrid(hit.point.y) + currentObject.offset, RoundToNearestGrid(hit.point.z));
        }

        if (Input.GetMouseButtonDown(0))
        {
            pendingObject.GetComponent<Collider>().enabled = true;
            pendingObject.GetComponent<Collider>().isTrigger = false;
            pendingObject.GetComponent<Item>().ChangeAmountServerRpc(1, true);
            pendingObject.GetComponent<Renderer>().material = normalMat;
            pendingObject.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 1f);
            pendingObject = null;
            SelectObject();
            if (currentObject == null) { return; }
            if (currentObject.networkAmount.Value == 1)
            {
                Items.Remove(currentObject);
                Destroy(currentSlot.gameObject);
                Destroy(pendingObject);
                Destroy(currentObject);
            }
            else
            {
                currentObject.ChangeAmountServerRpc(-1, false);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            pendingObject.transform.Rotate(Vector3.up, 45);
            currentObject.transform.Rotate(Vector3.up, 45);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            UndoPlacement();
        }
    }

    public void SelectObject()
    {
        if (currentObject == null) { return; }
        pendingObject = Instantiate(currentObject.gameObject);
        pendingObject = currentObject.gameObject;
        pendingObject.GetComponent<NetworkObject>().Spawn(true);
        pendingObject.GetComponent<Collider>().enabled = false;
        pendingObject.GetComponent<Item>().setActiveServerRpc(true);
        pendingObject.GetComponent<Renderer>().material = transparentMat;
        pendingObject.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0.3f);
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

    public void UndoPlacement()
    {
        Destroy(pendingObject);
        pendingObject = null;
        currentObject = null;
        currentSlot = null;
    }

    public void InventoryAdd(Item item)
    {
        var obj2 = Instantiate(AddItem, AddItemContent);
        var addItemSlot = obj2.GetComponent<AddItemSlot>();
        addItemSlot.text.text = "+" + item.networkAmount.Value + " " + item.icon.name;
        addItemSlot.icon.sprite = item.icon;
        obj2.GetComponent<Animator>().Play("FadeOut");
        for (int i = 0; i < Items.Count; i++)
        {
            if ((item.itemType != Item.ItemType.Weapon) && Items[i].icon == item.icon && ((Items[i].networkAmount.Value + item.networkAmount.Value) <= item.amount.y))
            {
                Items[i].ChangeAmountServerRpc(item.networkAmount.Value, false);
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
        item.gameObject.GetComponent<Collider>().isTrigger = true;
    }

    //Health System
    public void ChangeHealth(float amount)
    {
        ChangeHealthServerRpc(amount);

        if (health.Value > 100)
        {
            health.Value = 100;
        }
    }

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
        healthSlider.value = health.Value;
        healthText.text = health.Value + "/" + 100;
        damageUI.GetComponent<Animator>().Play("Damage");
        animator.SetInteger("HitIndex", Random.Range(0, 3));
        networkAnimator.SetTrigger("Hit");
        audioSource2.PlayOneShot(hurt);

        if (health.Value <= 0)
        {
            Respawn();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NameServerRpc()
    {
        NameClientRpc();
    }

    [ClientRpc]
    public void NameClientRpc()
    {
        if (!IsOwner) { return; }

        name.Value = SteamClient.Name;
        playerNameTag.GetComponentInChildren<TMP_Text>().text = name.Value.ToString();
    }

    [ServerRpc(RequireOwnership = false)]
    public void spawnServerRpc()
    {
        Debug.Log(craftingButton.recipe.item.gameObject);
        Debug.Log(currentRecipe);
        var item = Instantiate(currentRecipe.gameObject, transform.position, Quaternion.identity);
        item.GetComponent<NetworkObject>().Spawn(true);
        var itemComponent = item.GetComponent<Item>();
        itemComponent.setActiveServerRpc(true);
        item.GetComponent<Collider>().isTrigger = true;
        itemComponent.ChangeAmountServerRpc(1, true);
    }

    public void Respawn()
    {
        health.Value = 100;
        healthSlider.value = health.Value;
        healthText.text = health.Value + "/" + 100;
        staminaSlider.value = 100;
        hungerSlider.value = 100;
        networkAnimator.SetTrigger("Idle");
        if (curWeapon != null)
        {
            curWeapon.gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
        if (curShield != null)
        {
            curShield.gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
        for (int i = 0; i < Items.Count; i++)
        {
            Items.Remove(Items[i]);
        }
        for (int i = ItemContent.childCount - 1; i >= 0; i--)
        {
            Destroy(ItemContent.GetChild(i).gameObject);
        }
        controller.enabled = false;
        transform.position = spawnPos;
        controller.enabled = true;
    }

    //Coroutines
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
        yield return new WaitForSeconds(1.5f);
        if ((isIdle || (!isRunning && !isCrouched && !isSwimming) || isCrouched) && !canDrown && hunger)
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

    private IEnumerator ResetJump()
    {
        isJumping = false;
        yield return new WaitForSeconds(1f);
        isJumping = true;
    }

    //Animations
    public void Step()
    {
        if (isGrounded && IsOwner)
            audioSource1.PlayOneShot(footstep);
    }

    public void LeftPunchSound()
    {
        if (!IsOwner) { return; }
        audioSource2.PlayOneShot(leftHand.GetComponent<WeaponController>().attackSound);
        staminaSlider.value -= 4;
        hungerSlider.value -= 1f;

        leftPunch = true;
        StartCoroutine(ResetAttackCoolDown());
    }

    public void RightPunchSound()
    {
        if (!IsOwner) { return; }
        audioSource2.PlayOneShot(rightHand.GetComponent<WeaponController>().attackSound);
        staminaSlider.value -= 4;
        hungerSlider.value -= 1f;
        rightPunch = true;
        StartCoroutine(ResetAttackCoolDown());
    }

    public void SwordSound()
    {
        if (!IsOwner) { return; }
        if (curWeapon == null) { return; }
        audioSource2.PlayOneShot(curWeapon.attackSound);
        staminaSlider.value -= 4;
        hungerSlider.value -= 1f;
        StartCoroutine(ResetAttackCoolDown());
    }

    //Shield Block Sound
    public void ShieldSound()
    {
        if (!IsOwner) { return; }
        if (curShield == null) { return; }
        audioSource2.PlayOneShot(curShield.attackSound);
        staminaSlider.value -= 5;
        hungerSlider.value -= 1.25f;
        StartCoroutine(ResetAttackCoolDown());
    }
}


