using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_EnemyShootingSpider : scr_Enemy
{
    [Header("Spot Area")]
    [SerializeField] private BoxCollider2D spotAreaTrigger;
    [SerializeField] private Vector2 spotAreaSize;
    [SerializeField] private Vector2 spotAreaOffset;

    [Header("Attack")]
    [SerializeField] private GameObject webProjectile;
    [SerializeField] private Transform model;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown;
    private float attackTimer;
    [SerializeField] private float webSpeed;

    public bool playerInArea;

    protected override void Start()
    {
        base.Start();
        spotAreaTrigger.size = spotAreaSize;
        spotAreaTrigger.offset = spotAreaOffset;
    }

    void Update()
    {
        if (attackTimer < attackCooldown)
        {
            attackTimer += Time.deltaTime;
        }

        if (playerInArea)
        {
            Vector3 raycastStart = model.position;
            float distance = Vector2.Distance(player.position, raycastStart);

            Debug.DrawRay(raycastStart, (player.position - raycastStart).normalized * distance, Color.red);
        }
    }

    private void FixedUpdate()
    {
        if (playerInArea)
        {
            Vector3 relativePos = player.position - model.position;
            model.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1), -relativePos);

            if (attackTimer >= attackCooldown && PlayerInSight())
            {
                attackTimer = 0f;
                firePoint.position = model.position - model.up * 0.25f;
                GameObject newWebProjectile = Instantiate(webProjectile, firePoint.position, Quaternion.Euler(0, 0, 0));
                newWebProjectile.transform.parent = transform.parent;
                Rigidbody2D rb = newWebProjectile.GetComponent<Rigidbody2D>();
                rb.AddForce(-model.up * webSpeed, ForceMode2D.Impulse);
            }
        }
        else
        {
            model.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    private new bool PlayerInSight()
    {
        raycastStart = model.position;
        direction = (player.position - raycastStart).normalized;
        distance = Vector2.Distance(player.position, raycastStart);

        return base.PlayerInSight();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((Vector2)transform.parent.position + spotAreaOffset, spotAreaSize);
    }
}
