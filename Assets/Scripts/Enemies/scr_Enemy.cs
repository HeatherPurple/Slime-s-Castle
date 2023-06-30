using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_Enemy : MonoBehaviour, scr_IDamageable
{
    [Header("Components")]
    [SerializeField] protected GameObject enemy;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Rigidbody2D rb;

    [Header("Sight")]
    [SerializeField] protected LayerMask raycastMask;
    protected Transform player;
    protected RaycastHit2D[] sightHits;
    protected Vector3 raycastStart;
    protected Vector3 direction;
    protected float distance;
    protected bool platformOnPath;
    protected bool playerOnPath;
    protected float aggroRange;
    protected float initAggroRange;

    [Header("Health")]
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected bool canTakeDamage = true;
    [SerializeField] [Range(0, 10f)] protected float damageRate;
    [SerializeField] private string hitSound;
    [SerializeField] private string deathSound;
    protected float nextDamage;
    public int mobID;
    protected bool dead;

    [Header("Enemy Knockback")]
    [SerializeField] protected bool enableEnemyKnockback;
    [SerializeField] protected float enemyForceX = 2f;
    [SerializeField] protected float enemyForceY = 2f;

    [Header("Player Knockback")]
    [SerializeField] protected bool enablePlayerKnockback;
    [SerializeField] private float playerForceX = 2f;
    [SerializeField] private float playerForceY = 2.5f;

    [Header("Damage")]
    public float damage = 1f;
    [SerializeField] protected List<CreatureType> whoCanBeDamaged = new List<CreatureType>();

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        player = scr_GameManager.instance.player.transform;
    }

    protected bool PlayerInSight()
    {
        platformOnPath = false;
        playerOnPath = false;

        Debug.DrawRay(raycastStart, direction * distance, Color.red);

        sightHits = Physics2D.RaycastAll(raycastStart, direction, distance, raycastMask);

        for (int x = 0; x < sightHits.Length; x++)
        {
            if (sightHits[x].transform.gameObject.layer == LayerMask.NameToLayer("Platforms"))
            {
                platformOnPath = true;
                break;
            }
            if (sightHits[x].transform.CompareTag("Player"))
            {
                playerOnPath = true;
                break;
            }
        }

        return playerOnPath && !platformOnPath;
    }

    public void ApplyDamage(float damage, string tag, bool instantKill)
    {
        if (Time.time > nextDamage && canTakeDamage)
        {
            nextDamage = Time.time + damageRate;
            currentHealth -= damage;

            if (currentHealth > 0 && hitSound != "")
            {
                scr_AudioManager.instance.PlaySound(hitSound, gameObject);
            }

            StartCoroutine(DamageEffect());

            if (tag == "SlimeAttack" || tag == "PlungeAttack")
            {
                EnemyKnockback();
            }

            if (currentHealth <= 0)
            {
                canTakeDamage = false;

                if (deathSound != "")
                {
                    scr_AudioManager.instance.PlaySoundAtPosition(deathSound, transform.position);
                }

                StartCoroutine(Die());
            }
        }
    }
    
    protected void EnemyKnockback()
    {
        if (enableEnemyKnockback)
        {
            rb.velocity = (player.position.x >= transform.position.x) ? new Vector2(-enemyForceX, enemyForceY) : rb.velocity = new Vector2(enemyForceX, enemyForceY);
        }
    }

    protected IEnumerator DamageEffect()
    {
        spriteRenderer.color = new Color(1, 0, 0, 0.75f);
        yield return new WaitForSeconds(damageRate / 2);
        spriteRenderer.color = Color.white;
    }

    public IEnumerator Die()
    {
        yield return null;
        scr_EventSystem.instance.mobDeath.Invoke(mobID);
        Destroy(enemy);
    }

    protected enum CreatureType
    {
        Player,
        Enemy
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        TryDamage(col);
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        TryDamage(col);
    }

    protected void TryDamage(Collider2D col)
    {
        foreach (var type in whoCanBeDamaged)
        {
            if (col.CompareTag(type.ToString()))
            {
                if (enablePlayerKnockback && !scr_Player.invulnerable)
                {
                    scr_SlimeAttack slimeAttack = player.Find("SlimeAttack").GetComponent<scr_SlimeAttack>();

                    if (!slimeAttack.knockbackCoroutineIsRunning)
                    {
                        StartCoroutine(slimeAttack.PlayerKnockback(playerForceX, playerForceY, transform.position.x));
                    }
                }

                if (col.gameObject.transform.parent != transform.parent)
                {
                    if (type.ToString() == "Player" && scr_SlimeAttack.movingDownAfterAttack)
                    {
                        col.gameObject.GetComponent<scr_IDamageable>().ApplyDamage(damage / 2);
                    }
                    else
                    {
                        col.gameObject.GetComponent<scr_IDamageable>().ApplyDamage(damage);
                    }
                }

                break;
            }
        }
    }

}
