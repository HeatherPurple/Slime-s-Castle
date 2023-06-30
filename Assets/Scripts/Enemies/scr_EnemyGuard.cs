using UnityEngine;
using System.Collections;

public class scr_EnemyGuard : scr_Enemy
{
    [Header("Movement")]
    [SerializeField][Range(0, 50f)] private float speed;
    [SerializeField][Range(0, 30f)] private float patrolDistance;
    [SerializeField] private Transform endOfPlatformChecker;
    [SerializeField] private LayerMask groundLayer;
    private float checkerRadius = 0.1f;
    private bool closeToEndOfPlatform;
    private Vector3 startPosition;
    private Vector2 velocityVector;
    private float leftEgde;
    private float rightEgde;
    private bool movingRight;
    private bool flipCoroutineIsRunning = false;
    private bool playerIsGrounded;

    [Header("Attack")]
    [SerializeField] private bool aggressive;
    [SerializeField][Range(0, 30f)] private float _aggroRange;
    private float _initAggroRange;

    [Header("Status")]   
    [SerializeField] private bool patrol;
    [SerializeField] private bool attack;
    [SerializeField] private bool goBack;
    //[SerializeField] private bool immobilized;

    protected override void Start()
    {
        base.Start();
        startPosition = transform.position;
        _initAggroRange = _aggroRange;
        leftEgde = startPosition.x - patrolDistance;
        rightEgde = startPosition.x + patrolDistance;
    }

    private void FixedUpdate() 
    {
        closeToEndOfPlatform = !Physics2D.OverlapCircle(endOfPlatformChecker.position, checkerRadius, groundLayer);
        playerIsGrounded = scr_SlimeAttack.isGrounded;
        Vector3 playerPosition = player.position;

        if (movingRight)
        {
            transform.localScale = new Vector2(1, 1);
            velocityVector = new Vector2(speed, 0);
        }
        else
        {
            transform.localScale = new Vector2(-1, 1);
            velocityVector = new Vector2(-speed, 0);
        }

        if (!closeToEndOfPlatform)
        {
            rb.velocity = velocityVector;
        }
        else if (!flipCoroutineIsRunning)
        {
            StartCoroutine(Flip());
        }

        if (Mathf.Abs(transform.position.x - startPosition.x) < patrolDistance + 0.1f && !attack)
        {
            patrol = true;
            goBack = false;
        }

        if (aggressive)
        {
            if (Mathf.Abs(transform.position.x - playerPosition.x) < _aggroRange - 0.1f 
                && !(playerPosition.y - transform.position.y > 0.2f && playerIsGrounded)
                && playerPosition.y - transform.position.y <= 1.25f
                && playerPosition.y - transform.position.y >= -0.2f
                && PlayerInSight())
            {
                attack = true;
                patrol = false;
                goBack = false;
            }

            if (Mathf.Abs(transform.position.x - playerPosition.x) > _aggroRange && attack)
            {
                goBack = true;
                attack = false;
            }
        }

        if (patrol)
        {
            Patrol();
        }
        else if (attack)
        {
            Attack(playerPosition);
        }
        else if (goBack)
        {
            GoBack();
        }
    }

    private IEnumerator Flip()
    {
        flipCoroutineIsRunning = true;
        _aggroRange = 0;
        rb.velocity = Vector2.zero;
        movingRight = !movingRight;
        goBack = true;
        attack = false;
        patrol = false;
        yield return new WaitForSeconds(1.5f);
        _aggroRange = _initAggroRange;
        flipCoroutineIsRunning = false;
    }


    private void Attack(Vector3 playerPosition)
    {
        if (playerPosition.y - transform.position.y > 1.25f || playerPosition.y - transform.position.y < -0.2f
            || (playerPosition.y - transform.position.y > 0.2f || !PlayerInSight()) && playerIsGrounded)
        {
            goBack = true;
            attack = false;
        }

        if (playerPosition.x - transform.position.x > 0.4f && playerPosition.y - transform.position.y <= 1.25f
            && playerPosition.y - transform.position.y >= -0.2f)
        {
            movingRight = true;
        }
        else if (playerPosition.x - transform.position.x < -0.4f)
        {
            movingRight = false;
        }
    }

    private void Patrol()
    {
        if (transform.position.x - leftEgde <= 0.01f)
        {
            movingRight = true;
        }
        else if (transform.position.x - rightEgde >= -0.01f)
        {
            movingRight = false;
        }
    }

    private void GoBack()
    {
        if (transform.position.x - rightEgde > 0)
        {
            movingRight = false;
        }
        else if (transform.position.x - leftEgde < 0)
        {
            movingRight = true;
        }
    }

    //public void Immobilize()
    //{
    //
    //}

    private new bool PlayerInSight()
    {
        raycastStart = transform.position;
        direction = (player.position - transform.position).normalized;
        distance = _aggroRange;

        return base.PlayerInSight();
    }

    private void OnDrawGizmos() 
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireCube(transform.parent.position, new Vector3(patrolDistance * 2, 0.5f, 0));
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(_aggroRange * 2, 0.5f, 0));
    }

}
