using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

public class scr_EnemyFly : scr_Enemy, scr_IDamageable
{
    public struct Graph
    {
        public GridGraph gridGraph;
        public Vector3 center;
        public Vector2 size;
    }

    [Header("Graph")]
    private Graph graph;

    [Header("Components")]
    private Transform enemyTransform;
    private Seeker seeker;
    private AIPath aiPath;

    [Header("Enemy")]
    [SerializeField] private float _aggroRange;
    [SerializeField] private float aggroRangeChasing;
    [SerializeField] private float aggroRangeReturning;
    private float _initAggroRange;
    private float initAggroRangeChasing;
    private float initAggroRangeReturning;
    private Vector3 defaultPosition;

    [Header("Sight")]
    private bool playerInRange = false;
    private bool playerIsVisible = false;

    [Header("Enemy Behaviour")]
    [SerializeField] private bool aggressive;
    [SerializeField] private bool checkLastSeenPosition;
    [SerializeField] private float waitOnLastSeenDuration;
    [SerializeField] private bool aggroAfterHit;
    [SerializeField] private bool toxic;

    [Header("Toxic Cloud")]
    [SerializeField] private Transform toxicCloud;
    [SerializeField] private float timeBeforeExplode;
    [SerializeField] private float timeBeforeDisappear;
    private bool startToxicCloudFirstStage = false;
    private bool startToxicCloudLastStage = false;
    [SerializeField] private Vector3 zeroScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 firstScale = new Vector3(2.5f, 2.5f, 2.5f);
    [SerializeField] private Vector3 secondScale = new Vector3(5f, 5f, 5f);
    [SerializeField] private Vector3 thirdScale = new Vector3(7.5f, 7.5f, 7.5f);
    [SerializeField] private float appearSpeed;
    [SerializeField] private float disappearSpeed;
    [SerializeField] private float disableColliderAlpha;
    [SerializeField] private float deleteToxicCloudAlpha;
    private bool deleteToxicCloud = false;
    private const int stages = 3;
    private Transform[] stage = new Transform[stages];
    private SpriteRenderer[] stageSpriteRenderer = new SpriteRenderer[stages];
    private CircleCollider2D toxicCloudCollider;

    [Header("Movement Parameters")]
    [SerializeField] private float speed;
    [SerializeField] private float idleDuration;
    private Vector3 initScale;
    private bool moveCoroutineIsRunning = false;
    private bool movingLeft = false;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private bool[] waypointsToStay;
    private int waypointIndex = 0;
    private Vector3 lastSeenPosition;
    private float deltaX;

    [Header("Debug info")]
    public bool returnedToStart = true;
    public bool returnedToLastSeen = false;
    public bool aggroed = false;
    public bool wasHit = false;
    public bool outOfZone = true;
    public bool isWaiting = false;
    public bool hasWaited = false;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _aggroRange);
    }

    private void Awake()
    {
        enemyTransform = GetComponent<Transform>();
        aiPath = GetComponent<AIPath>();
        seeker = GetComponent<Seeker>();

        if (toxic)
        {
            for (int i = 0; i < stages; i++)
            {
                stage[i] = toxicCloud.GetChild(i);
                stageSpriteRenderer[i] = stage[i].GetComponent<SpriteRenderer>();
            }

            toxicCloudCollider = stage[2].GetComponent<CircleCollider2D>();
        }
    }

    protected override void Start()
    {
        base.Start();
        _initAggroRange = _aggroRange;
        initAggroRangeChasing = aggroRangeChasing;
        initAggroRangeReturning = aggroRangeReturning;

        initScale = (enemyTransform.localScale.x >= 0) ? enemyTransform.localScale : new Vector3(-enemyTransform.localScale.x, enemyTransform.localScale.y, enemyTransform.localScale.z);

        uint graphId = (uint)Mathf.Log(seeker.graphMask.value, 2);
        print("name: " + gameObject.transform.parent.name + "; graphId: " + graphId);

        graph.gridGraph = AstarPath.active.data.FindGraphsOfType(typeof(GridGraph)).Cast<GridGraph>().ToArray()[graphId];
        graph.center = graph.gridGraph.center;
        graph.size = graph.gridGraph.size;

        //gridGraph = AstarPath.active.data.FindGraphsOfType(typeof(GridGraph)).Cast<GridGraph>().ToArray()[graphId];
        //gridGraphCenter = gridGraph.center;
        //gridGraphSize = gridGraph.size;

        if (toxic)
        {
            toxicCloud.gameObject.SetActive(false);

            for (int i = 0; i < stages; i++)
            {
                stage[i].localScale = zeroScale;
                stage[i].gameObject.SetActive(false);
            }
        }

        transform.position = waypoints[waypointIndex].transform.position;
        defaultPosition = enemyTransform.position;
        lastSeenPosition = player.position;

        if (waypointsToStay[0])
        {
            waypointIndex++;
        }

        StartCoroutine(Move());
    }

    private void UpdatePath()
    {
        if (seeker.IsDone() && !dead)
        {
            seeker.StartPath(rb.position, player.position);
        }
    }

    private void FixedUpdate()
    {
        if (!dead)
        {
            if (aiPath.desiredVelocity.x >= 0.01f)
            {
                transform.localScale = new Vector3(-initScale.x, initScale.y, initScale.z);
                movingLeft = false;
            }
            else if (aiPath.desiredVelocity.x <= -0.01f)
            {
                transform.localScale = new Vector3(initScale.x, initScale.y, initScale.z);
                movingLeft = true;
            }

            Vector3 playerPosistion = player.position;

            outOfZone = playerPosistion.x - (graph.center.x + graph.size.x / 2) >= 0.1f ||
                        playerPosistion.x - (graph.center.x - graph.size.x / 2) <= -0.1f ||
                        playerPosistion.y - (graph.center.y + graph.size.y / 2) >= 0.1f ||
                        playerPosistion.y - (graph.center.y - graph.size.y / 2) <= -0.1f;

            playerInRange = Vector2.Distance(rb.position, playerPosistion) <= _aggroRange;
            
            if (playerInRange)
            {
                playerIsVisible = PlayerInSight();
            }
            else
            {
                playerIsVisible = false;
            }

            if (returnedToStart)
            {
                aiPath.enabled = false;
                _aggroRange = _initAggroRange;
                CancelInvoke("CheckDist");
                CancelInvoke("UpdatePath");

                if (!moveCoroutineIsRunning)
                {
                    StartCoroutine(Move());
                }
            }

            if (playerIsVisible && (aggressive || wasHit))
            {
                aiPath.enabled = true;
                aggroed = true;
                InvokeRepeating("CheckDist", 0, 1f);
            }

            if (aggroed)
            {
                hasWaited = false;
                lastSeenPosition = (outOfZone) ? defaultPosition : playerPosistion;
            }
        }

        if (toxic && dead)
        {
            if (startToxicCloudFirstStage)
            {
                stage[0].localScale = Vector3.Lerp(stage[0].localScale, firstScale, appearSpeed * Time.fixedDeltaTime);
                stage[1].localScale = Vector3.Lerp(stage[1].localScale, secondScale, appearSpeed * Time.fixedDeltaTime);
                stage[2].localScale = Vector3.Lerp(stage[2].localScale, thirdScale, appearSpeed * Time.fixedDeltaTime);
            }

            if (startToxicCloudLastStage)
            {
                for (int i = 0; i < stages; i++)
                {
                    stageSpriteRenderer[i].color = Color.Lerp(stageSpriteRenderer[i].color,
                        new Color(stageSpriteRenderer[i].color.r, stageSpriteRenderer[i].color.g, stageSpriteRenderer[i].color.b, 0), 
                        disappearSpeed * Time.fixedDeltaTime);
                }

                if (stageSpriteRenderer[0].color.a <= disableColliderAlpha)
                {
                    toxicCloudCollider.enabled = false;

                    if (stageSpriteRenderer[0].color.a <= deleteToxicCloudAlpha)
                    {
                        deleteToxicCloud = true;
                    }
                }
            }
        }
    }

    private void CheckDist()
    {
        if (!dead)
        {
            if (aggroed)
            {
                waypointIndex = 0;
                _aggroRange = initAggroRangeChasing;
                returnedToLastSeen = false;
                returnedToStart = false;
                InvokeRepeating("UpdatePath", 0, 0.1f);
            }
            else
            {
                if (Vector2.Distance(rb.position, lastSeenPosition) >= 0.3f && !returnedToLastSeen && checkLastSeenPosition)
                {
                    if (outOfZone || !playerInRange || !playerIsVisible)
                    { 
                        ReturnToLastSeen(); 
                    }
                }
                else if (Vector2.Distance(rb.position, lastSeenPosition) < 0.3f || !checkLastSeenPosition)
                {
                    if (checkLastSeenPosition && !isWaiting && !hasWaited && lastSeenPosition != defaultPosition)
                    {
                        StartCoroutine(WaitOnLastSeen());
                    }

                    returnedToLastSeen = true;
                    aggroed = false;
                }

                if (Vector2.Distance(rb.position, defaultPosition) >= 0.1f && returnedToLastSeen && !returnedToStart)
                {
                    if (outOfZone || !playerInRange || !playerIsVisible)
                    {
                        ReturnToStart();
                    }
                }
                else if (Vector2.Distance(rb.position, defaultPosition) < 0.1f)
                {
                    returnedToStart = true;
                    wasHit = false;
                    aggroed = false;
                    waypointIndex = 0;
                }
            }

            if (!playerInRange || outOfZone)
            {
                CancelInvoke("UpdatePath");
                aggroed = false;
            }
        }
    }

    private new bool PlayerInSight()
    {
        platformOnPath = false;
        playerOnPath = false;

        Vector3 raycastStart = transform.position;
        Vector3 direction = (player.position - transform.position).normalized;

        //Player is behind/under/above enemy
        if (player.position.x - transform.position.x <= 0 && !movingLeft || player.position.x - transform.position.x >= 0 && movingLeft)
        {
            if (!aggroed)
            {
                Debug.DrawRay(raycastStart, direction * _aggroRange / 2, Color.red);
            }

            sightHits = Physics2D.RaycastAll(raycastStart, direction, _aggroRange / 2, raycastMask);
        }
        //Player is in front of enemy
        else if (player.position.x - transform.position.x > 0 && !movingLeft || player.position.x - transform.position.x < 0 && movingLeft)
        {
            if (!aggroed)
            {
                Debug.DrawRay(raycastStart, direction * _aggroRange, Color.red);
            }

            sightHits = Physics2D.RaycastAll(raycastStart, direction, _aggroRange, raycastMask);
        }

        for (int i = 0; i < sightHits.Length; i++)
        {
            if (sightHits[i].transform.gameObject.layer == LayerMask.NameToLayer("Platforms"))
            {
                platformOnPath = true;
                break;
            }
            if (sightHits[i].transform.CompareTag("Player"))
            {
                playerOnPath = true;
                break;
            }
        }

        return playerOnPath && !platformOnPath && !outOfZone;
    }

    private void ReturnToStart()
    {
        aggroed = false;
        CancelInvoke("CheckDist");
        _aggroRange = _initAggroRange;
        seeker.StartPath(rb.position, defaultPosition);
        InvokeRepeating("CheckDist", 0, 1f);
    }

    private void ReturnToLastSeen()
    {
        aggroed = false;
        CancelInvoke("CheckDist");

        if (outOfZone && lastSeenPosition == defaultPosition)
        {
            _aggroRange = _initAggroRange;
        }
        else if (lastSeenPosition != defaultPosition)
        {
            _aggroRange = initAggroRangeReturning;
        }

        seeker.StartPath(rb.position, lastSeenPosition);
        InvokeRepeating("CheckDist", 0, 1f);
    }

    private IEnumerator WaitOnLastSeen()
    {
        isWaiting = true;
        aiPath.enabled = false;
        yield return new WaitForSeconds(waitOnLastSeenDuration);
        aiPath.enabled = true;
        isWaiting = false;
        hasWaited = true;
    }

    private IEnumerator Move()
    {
        moveCoroutineIsRunning = true;

        transform.position = Vector2.MoveTowards(transform.position, waypoints[waypointIndex].position, speed * Time.deltaTime);

        if (transform.position == waypoints[waypointIndex].position)
        {
            if (waypointsToStay[waypointIndex])
            {
                yield return new WaitForSeconds(idleDuration);
            }

            if (waypointIndex < waypoints.Length - 1)
            {
                deltaX = waypoints[waypointIndex + 1].position.x - waypoints[waypointIndex].position.x;
                waypointIndex++;
            }
            else
            {
                deltaX = waypoints[0].position.x - waypoints[waypointIndex].position.x;
                waypointIndex = 0;
            }

            if (deltaX > 0)
            {
                movingLeft = false;
                transform.localScale = new Vector3(-initScale.x, initScale.y, initScale.z);
            }
            else if (deltaX < 0)
            {
                movingLeft = true;
                transform.localScale = new Vector3(initScale.x, initScale.y, initScale.z);
            }
        }

        moveCoroutineIsRunning = false;
    }

    public new void ApplyDamage(float damage, string tag, bool instantKill)
    {
        if (Time.time > nextDamage && canTakeDamage)
        {
            nextDamage = Time.time + damageRate;
            currentHealth -= damage;
            StartCoroutine(DamageEffect());

            if (aggroAfterHit)
            {
                wasHit = true;
            }

            if (currentHealth <= 0)
            {
                canTakeDamage = false;
                StartCoroutine(Die());
            }
        }
    }

    public new IEnumerator Die()
    {
        yield return null;
        scr_EventSystem.instance.mobDeath.Invoke(mobID);

        if (!toxic)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            dead = true;
            toxicCloud.gameObject.SetActive(true);
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetComponent<CircleCollider2D>().enabled = false;
            seeker.enabled = false;
            yield return new WaitForSeconds(timeBeforeExplode);

            for (int i = 0; i < stages; i++)
            {
                stage[i].gameObject.SetActive(true);
            }

            startToxicCloudFirstStage = true;
            yield return new WaitForSeconds(timeBeforeDisappear);
            startToxicCloudLastStage = true;
            yield return new WaitUntil(() => deleteToxicCloud);
            Destroy(enemy);
        }
    }

}
