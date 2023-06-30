using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class scr_SlimeAttack : MonoBehaviour
{
    private Transform player;
    private Rigidbody2D playerRigidbody;
    private InputManager input;

    [Header("---Ground Check---")]
    [SerializeField] private Transform groundChecker;
    [SerializeField] private LayerMask platformsLayer;
    private Vector2 boxSize;
    public static bool isGrounded;

    [Header("---Normal Attack---")]
    [Header("Parameters")]
    [SerializeField] private float groundAttackDuration;
    [SerializeField] private float airAttackDuration;
    [SerializeField] private float damageNA;

    private Vector3 zAxis = new Vector3(0, 0, 1);
    private Vector3 initPosition;
    private float angle = 90f;
    private bool canNormalAttack = true;

    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D capsuleCollider2D;

    [Header("---Super Attack---")]
    [Header("Parameters")]
    [SerializeField] private float waveSize;
    [SerializeField] private float waveSpeed;
    [SerializeField] private float cooldown;
    [SerializeField] private float forceY;
    [SerializeField] private float damageSA;
    public static float _damageSA;
    private Vector2 superAttackInitColliderSize;
    private Vector2 superAttackColliderMaxSize;
    private Vector2 superAttackColliderSize;

    private bool cooldownEnded = true;
    private bool canSuperAttack = false;
    private bool superAttackPerformed = false;
    private bool damageWaveIsActive = false;
    public static bool movingDownAfterAttack = false;

    private scr_cnpt_FormBehavior formBehavior;
    private BoxCollider2D boxCollider2D;
    private GameObject plungeAttack;

    [Header("Player Knockback")]
    public bool knockbackCoroutineIsRunning = false;
    [SerializeField] private float knockbackMaxTime = 0.6f;
    private bool knockbackTimerFinished = false;


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        plungeAttack = transform.GetChild(0).gameObject;
        boxCollider2D = plungeAttack.transform.GetComponent<BoxCollider2D>();
        
        input = InputManager.instance;
        
        input.playerInput.actions["Attack"].performed += NormalAttack;
        input.playerInput.actions["SuperAttack"].performed += SuperAttack;
    }


    private void Start()
    {
        player = transform.parent;
        playerRigidbody = player.GetComponent<Rigidbody2D>();
        initPosition = transform.localPosition;

        formBehavior = scr_cnpt_FormBehavior.instance;
        float radius = player.GetComponent<CircleCollider2D>().radius;
        superAttackInitColliderSize = new Vector2(radius * 2, radius * 2);
        boxSize = new Vector2(radius * 0.95f, 0.1f);
        superAttackColliderMaxSize = new Vector2(superAttackInitColliderSize.x * waveSize, superAttackInitColliderSize.y);

        _damageSA = damageSA;
    }

    private void Update()
    {
        isGrounded = Physics2D.OverlapBox(groundChecker.position, boxSize, 0, platformsLayer);
        
        //Normal Attack
        if (transform.localRotation.eulerAngles.z > 0)
        {
            if (player.localScale.x > 0)
            {
                transform.RotateAround(player.position, zAxis, -angle / airAttackDuration * Time.deltaTime);
            }
            else if (player.localScale.x < 0)
            {
                transform.RotateAround(player.position, zAxis, angle / airAttackDuration * Time.deltaTime);
            }
        }

        //Super Attack
        if (isGrounded && superAttackPerformed)
        {
            movingDownAfterAttack = false;
        }

        if (!isGrounded && !superAttackPerformed && cooldownEnded)
        {
            canSuperAttack = true;
        }
        else
        {
            canSuperAttack = false;
        }

        if (isGrounded && superAttackPerformed && !damageWaveIsActive)
        {
            damageWaveIsActive = true;
            plungeAttack.transform.position = new Vector3(player.position.x, player.position.y);
        }
        else if (!damageWaveIsActive && superAttackPerformed)
        {
            plungeAttack.transform.position = new Vector3(player.position.x, player.position.y);
        }

        if (damageWaveIsActive)
        {
            superAttackColliderSize = Vector2.Lerp(boxCollider2D.size, superAttackColliderMaxSize, waveSpeed * Time.deltaTime);
            boxCollider2D.size = new Vector2(superAttackColliderSize.x, superAttackInitColliderSize.y);

            if (Mathf.Abs(boxCollider2D.size.x - superAttackColliderMaxSize.x) <= 0.01f)
            {
                StartCoroutine(SuperAttackEnd());
            }
        }

    }

    private void NormalAttack(InputAction.CallbackContext context)
    {
        
        if (canNormalAttack && scr_cnpt_FormBehavior.instance._currentForm.GetType() == typeof(scr_SlimeForm))
        {
            if (isGrounded)
            {
                //scr_AudioManager.instance.PlayMusic("Test");
                StartCoroutine(GroundAttack());
            }
            else
            {
                StartCoroutine(AirAttack());
            }
        }
    }

    private IEnumerator GroundAttack()
    {
        scr_AudioManager.instance.PlaySound("PlayerAttack", gameObject);
        canNormalAttack = false;

        spriteRenderer.enabled = true;
        capsuleCollider2D.enabled = true;
        yield return new WaitForSeconds(groundAttackDuration);
        spriteRenderer.enabled = false;
        capsuleCollider2D.enabled = false;

        canNormalAttack = true;
    }

    private IEnumerator AirAttack()
    {
        scr_AudioManager.instance.PlaySound("AirAttack", gameObject);
        canNormalAttack = false;
        transform.localRotation = Quaternion.Euler(0, 0, 90);
        transform.localPosition = new Vector3(0, initPosition.x, 0);

        spriteRenderer.enabled = true;
        capsuleCollider2D.enabled = true;
        yield return new WaitForSeconds(airAttackDuration);
        transform.localPosition = initPosition;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        yield return new WaitForSeconds(groundAttackDuration - airAttackDuration);
        spriteRenderer.enabled = false;
        capsuleCollider2D.enabled = false;

        canNormalAttack = true;
    }

    private void SuperAttack(InputAction.CallbackContext context)
    {
        if (formBehavior._currentForm.GetType() == typeof(scr_FireflyForm))
        {
            scr_TilemapManager.instance.SetTileOnFire(scr_GameManager.instance.player.transform.position);
        }

        if (canSuperAttack && formBehavior._currentForm.GetType() == typeof(scr_SlimeForm))
        {
            canSuperAttack = false;
            superAttackPerformed = true;
            movingDownAfterAttack = true;
            player.GetComponent<Rigidbody2D>().velocity = new Vector2(0, -forceY);

            plungeAttack.transform.SetParent(null);
            plungeAttack.SetActive(true);
            boxCollider2D.size = superAttackInitColliderSize;
        }
    }

    private IEnumerator SuperAttackEnd()
    {
        cooldownEnded = false;
        plungeAttack.transform.SetParent(transform);
        plungeAttack.SetActive(false);
        //yield return new WaitForSeconds(0f);
        damageWaveIsActive = false;
        superAttackPerformed = false;
        yield return new WaitForSeconds(cooldown);
        cooldownEnded = true;
    }

    public IEnumerator PlayerKnockback(float forceX, float forceY, float enemyPositionX)
    {
        knockbackCoroutineIsRunning = true;

        if (!movingDownAfterAttack)
        {
            scr_PlayerFormBase.inKnockback = true;

            if (player.position.x >= enemyPositionX)
            {
                playerRigidbody.velocity = new Vector2(forceX, forceY);
            }
            else
            {
                playerRigidbody.velocity = new Vector2(-forceX, forceY);
            }

            knockbackTimerFinished = false;
            StartCoroutine(PlayerKnockbackTimer());
            yield return new WaitUntil(() => !isGrounded);
            yield return new WaitUntil(() => isGrounded || knockbackTimerFinished);
            StopCoroutine(PlayerKnockbackTimer());
            scr_PlayerFormBase.inKnockback = false;
        }

        knockbackCoroutineIsRunning = false;
    }

    private IEnumerator PlayerKnockbackTimer()
    {
        yield return new WaitForSeconds(knockbackMaxTime);
        knockbackTimerFinished = true;
    }


    private void OnTriggerEnter2D(Collider2D collider) 
    {
        if (collider.CompareTag("Enemy"))
        {
            collider.gameObject.GetComponent<scr_IDamageable>().ApplyDamage(damageNA, gameObject.tag);
        }
    }

    /*protected bool CheckIfOverlap(Transform checker, float radius, LayerMask mask)
    {
        return Physics2D.OverlapCircleAll(checker.position, radius, mask).Length != 0;
    }*/

    private void OnDestroy()
    {
        scr_PlayerFormBase.inKnockback = false;
        input.playerInput.actions["Attack"].performed -= NormalAttack;
        input.playerInput.actions["SuperAttack"].performed -= SuperAttack;
    }
}
