using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_EnemyJumpSpider : scr_Enemy, scr_IDamageable
{
    [Header("Checkers")]
    [SerializeField] private Transform groundChecker;
    [SerializeField] private Transform endOfPlatformChecker;
    [SerializeField] private Transform wallChecker;
    [SerializeField] private LayerMask groundLayer;
    private Vector2 boxSize;
    private float checkerRadius = 0.1f;

    [Header("Enemy Behaviour")]
    [SerializeField] private bool aggressive;

    [Header("Movement parameters")]
    [SerializeField] private float moveSpeed;
    private scr_TilemapManager tilemapManager;
    private float moveDirection = 1;
    private bool movingLeft = false;
    private bool moveCoroutineIsRunning = false;
    //private bool waiting = false;
    [SerializeField] private float idleDuration;

    [Header("Jump")]
    [SerializeField] private Vector2 jumpZone;
    [SerializeField] private float jumpZoneOffset;
    private float initJumpZoneY;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float jumpHeight;
    private bool jumpWasPerformed = false;
    [SerializeField] private float jumpCooldown;
    private float jumpCooldownTimer;
    private bool jumpCoroutineIsRunning = false;

    [Header("Attack")]
    [SerializeField] private GameObject attackCollider;
    [SerializeField] private float attackCooldown;
    private float attackCooldownTimer;

    [Header("Components")]
    [SerializeField] private BoxCollider2D physicsBoxCollider;
    //private Animator anim;

    [Header("Debug Info")]
    public bool closeToEndOfPlatform;
    public bool closeToWall;
    public bool isGrounded;
    public bool playerInJumpZone;
    public bool playerIsVisible;
    public bool playerSpotted = false;
    public bool flippedTowardsPlayer = false;


    private void Awake()
    {
        //anim = GetComponent<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        boxSize = new Vector2(GetComponent<BoxCollider2D>().bounds.size.x * 0.9f, 0.1f);
        initJumpZoneY = jumpZone.y;
        aggroRange = jumpZone.x / 2 + jumpZoneOffset;
        initAggroRange = aggroRange;
        attackCollider.SetActive(false);

        tilemapManager = scr_TilemapManager.instance;
    }

    private void FixedUpdate()
    {
        closeToEndOfPlatform = !Physics2D.OverlapCircle(endOfPlatformChecker.position, checkerRadius, groundLayer);
        closeToWall = Physics2D.OverlapCircle(wallChecker.position, checkerRadius, groundLayer);
        isGrounded = Physics2D.OverlapBox(groundChecker.position, boxSize, 0, groundLayer);

        playerInJumpZone = (movingLeft) ? Physics2D.OverlapBox(transform.position - new Vector3(jumpZoneOffset, 0), jumpZone, 0, playerLayer)
                                        : Physics2D.OverlapBox(transform.position + new Vector3(jumpZoneOffset, 0), jumpZone, 0, playerLayer);

        playerIsVisible = PlayerInSight();

        //AnimationController();

        if (isGrounded)
        {
            if (playerInJumpZone && playerIsVisible && aggressive)
            {
                jumpZone.y = initJumpZoneY * 8f;
                aggroRange = initAggroRange * 2f;
            }
            else
            {
                jumpZone.y = initJumpZoneY;
                aggroRange = initAggroRange;

                if (!moveCoroutineIsRunning && !attackCollider.activeInHierarchy)
                {
                    if (playerSpotted)
                    {
                        playerSpotted = false;
                        FlipToPlayerDuringAttack();
                    }

                    StartCoroutine(Move());
                }
            }

            if (jumpWasPerformed)
            {
                jumpWasPerformed = false;
                rb.velocity = Vector2.zero;
                FlipToPlayerDuringAttack();
            }

            attackCooldownTimer += Time.fixedDeltaTime;
            jumpCooldownTimer += Time.fixedDeltaTime;

            if (aggressive && (playerInJumpZone && playerIsVisible || playerSpotted))
            {
                if (Mathf.Abs(player.position.x - transform.position.x) <= 0.6f && Mathf.Abs(player.position.y - transform.position.y) <= 0.1f && attackCooldownTimer >= attackCooldown)
                {
                    StartCoroutine(Attack());
                }
                else if (Mathf.Abs(player.position.x - transform.position.x) > 0.6f && !jumpCoroutineIsRunning)
                { 
                    StartCoroutine(Jump()); 
                }
            }
        }
    }

    private IEnumerator Move()
    {
        moveCoroutineIsRunning = true;
        flippedTowardsPlayer = false;

        float adjustedSpeed = tilemapManager.GetTileMovementSpeed(transform.position) * moveSpeed;

        rb.velocity = new Vector2(adjustedSpeed * moveDirection, rb.velocity.y);

        if (closeToEndOfPlatform || closeToWall)
        {
            rb.velocity = Vector2.zero;
            //waiting = true;
            yield return new WaitForSeconds(idleDuration);
            //waiting = false;

            if (isGrounded && !playerSpotted && !flippedTowardsPlayer)
            {
                Flip();
            }
        }

        moveCoroutineIsRunning = false;
    }

    private IEnumerator Jump()
    {
        jumpCoroutineIsRunning = true;
        yield return new WaitForSeconds(0.25f);

        if (isGrounded && !attackCollider.activeInHierarchy)
        {
            FlipToPlayer();
        }

        yield return new WaitForSeconds(0.5f);

        if (jumpCooldownTimer < jumpCooldown || attackCollider.activeInHierarchy)
        {
            jumpCoroutineIsRunning = false;
            yield break;
        }

        playerSpotted = false;
        float distance = player.position.x - transform.position.x;

        if (isGrounded && playerInJumpZone)
        {
            jumpCooldownTimer = 0f;
            rb.AddForce(new Vector2(distance * 1.5f, jumpHeight), ForceMode2D.Impulse);
            StartCoroutine(Wait());
        }
        else
        {
            FlipToPlayerDuringAttack();
        }

        yield return new WaitUntil(() => isGrounded);
        jumpCoroutineIsRunning = false;
    }

    private IEnumerator Attack()
    {
        attackCooldownTimer = 0f;
        FlipToPlayerDuringAttack();
        attackCollider.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        attackCollider.SetActive(false);
        FlipToPlayerDuringAttack();
    }

/*    void JumpAttack()
    {
        if (jumpCooldownTimer >= jumpCooldown)
            jumpCooldownTimer = 0f;
        else return;

        playerSpotted = false;
        float distance = player.position.x - transform.position.x;

        if (isGrounded && playerInJumpZone)
        {
            rb.AddForce(new Vector2(distance * 1.5f, jumpHeight), ForceMode2D.Impulse);
            StartCoroutine(Wait());
        }
    }*/

    private IEnumerator Wait()
    {
        yield return new WaitForSeconds(0.1f);
        jumpWasPerformed = true;
    }

    private void FlipToPlayer()
    {
        rb.velocity = Vector2.zero;
        playerSpotted = true;
        float distance = player.position.x - transform.position.x;

        if (distance < 0 && !movingLeft)
        {
            Flip();
        }
        else if (distance > 0 && movingLeft)
        {
            Flip();
        }

        flippedTowardsPlayer = true;
    }

    private void FlipToPlayerDuringAttack()
    {
        float distance = player.position.x - transform.position.x;

        if (distance < 0 && !movingLeft)
        {
            Flip();
        }
        else if (distance > 0 && movingLeft)
        {
            Flip();
        }

        flippedTowardsPlayer = true;
    }

    private void Flip()
    {
        moveDirection *= -1;
        movingLeft = !movingLeft;
        transform.Rotate(0, 180, 0);
    }

/*    void AnimationController()
    {
        if (aggressive)
        {
            anim.SetBool("canSeePlayer", playerInJumpZone && playerIsVisible || playerSpotted);
            anim.SetBool("isGrounded", isGrounded);
        }
        anim.SetBool("waiting", waiting);
    }*/

    private new bool PlayerInSight()
    {
        raycastStart = transform.position + new Vector3(0, physicsBoxCollider.bounds.extents.y);
        direction = (player.position - raycastStart).normalized;
        distance = aggroRange;

        return base.PlayerInSight();
    }

    private void OnDrawGizmosSelected()
    {
        /*Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(endOfPlatformChecker.position, checkerRadius);
        Gizmos.DrawWireSphere(wallChecker.position, checkerRadius);*/
        //Gizmos.DrawWireCube(groundChecker.position, boxSize);
        Gizmos.color = Color.red;

        if (movingLeft)
        {
            Gizmos.DrawWireCube(transform.position - new Vector3(jumpZoneOffset, 0), jumpZone);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position + new Vector3(jumpZoneOffset, 0), jumpZone);
        }
    }

}
