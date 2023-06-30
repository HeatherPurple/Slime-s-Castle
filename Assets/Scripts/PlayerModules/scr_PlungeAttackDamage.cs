using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_PlungeAttackDamage : MonoBehaviour
{
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = transform.GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        Debug.DrawLine(new Vector3(transform.position.x - boxCollider.bounds.extents.x, transform.position.y + boxCollider.bounds.extents.y), 
                       new Vector3(transform.position.x + boxCollider.bounds.extents.x, transform.position.y + boxCollider.bounds.extents.y), Color.red);
        Debug.DrawLine(new Vector3(transform.position.x - boxCollider.bounds.extents.x, transform.position.y + boxCollider.bounds.extents.y),
                       new Vector3(transform.position.x - boxCollider.bounds.extents.x, transform.position.y - boxCollider.bounds.extents.y), Color.red);
        Debug.DrawLine(new Vector3(transform.position.x + boxCollider.bounds.extents.x, transform.position.y - boxCollider.bounds.extents.y),
                       new Vector3(transform.position.x - boxCollider.bounds.extents.x, transform.position.y - boxCollider.bounds.extents.y), Color.red);
        Debug.DrawLine(new Vector3(transform.position.x + boxCollider.bounds.extents.x, transform.position.y - boxCollider.bounds.extents.y),
                       new Vector3(transform.position.x + boxCollider.bounds.extents.x, transform.position.y + boxCollider.bounds.extents.y), Color.red);

    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Enemy"))
        {
            collider.gameObject.GetComponent<scr_IDamageable>().ApplyDamage(scr_SlimeAttack._damageSA, gameObject.transform.tag);
        }
    }
}
