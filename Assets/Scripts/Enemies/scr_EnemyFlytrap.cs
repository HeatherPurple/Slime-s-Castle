using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_EnemyFlytrap : scr_Enemy, scr_IDamageable
{
    [Header("Components")]
    [SerializeField] private Transform rightHalf;
    [SerializeField] private Transform leftHalf;
    [SerializeField] private BoxCollider2D spotTrigger;

    private bool active = false;

    private Vector3 initPositionLeftHalf;
    private Vector3 initPositionRightHalf;
    private Vector3 zAxis = new Vector3(0, 0, 1);

    private bool canTakeDamageFromNormalAttack = false;

    [Header("Attack")]
    [SerializeField] private float activateDelay;
    [SerializeField] private float activeTime;
    [SerializeField] private float attackDuration;
    private bool canDamage = false;

    [Header("Debug Info")]
    public bool EnteredTrigger = false;
    public bool trapActivated = false;
    public bool recharge = false;
    public bool activateCoroutineIsRunning = false;

    protected override void Start()
    {
        base.Start();
        initPositionLeftHalf = leftHalf.localPosition;
        initPositionRightHalf = rightHalf.localPosition;
    }

    private void FixedUpdate()
    {
        if (trapActivated)
        {
            if (rightHalf.localRotation.eulerAngles.z >= 270 && rightHalf.localRotation.eulerAngles.z <= 345)
            {
                rightHalf.RotateAround(transform.position, zAxis, 90f / attackDuration * Time.fixedDeltaTime);
            }

            if (leftHalf.localRotation.eulerAngles.z >= 195 && leftHalf.localRotation.eulerAngles.z <= 270)
            {
                leftHalf.RotateAround(transform.position, zAxis, -90f / attackDuration * Time.fixedDeltaTime);
            }
        }

        if (recharge)
        {
            if (rightHalf.localRotation.eulerAngles.z >= 270 || rightHalf.localRotation.eulerAngles.z < 1)
            {
                rightHalf.RotateAround(transform.position, zAxis, -90f / attackDuration * Time.fixedDeltaTime);
            }
            else
            {
                EndRecharge();
            }

            if (leftHalf.localRotation.eulerAngles.z <= 270)
            {
                leftHalf.RotateAround(transform.position, zAxis, 90f / attackDuration * Time.fixedDeltaTime);
            }
            else
            {
                EndRecharge();
            }
        }
    }

    private void EndRecharge()
    {
        recharge = false;
        active = false;
        canTakeDamageFromNormalAttack = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (canDamage)
        {
            TryDamage(collision);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (canDamage)
        {
            TryDamage(collision);
        }
    }

    public void React(Collider2D collision)
    {
        if (!EnteredTrigger && (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy")) && !active)
        {
            EnteredTrigger = true;
            active = true;
        }

        if (EnteredTrigger && !trapActivated && !activateCoroutineIsRunning)
        {
            StartCoroutine(Activate());
        }
    }

    private IEnumerator Activate()
    {
        activateCoroutineIsRunning = true;
        rightHalf.localRotation = Quaternion.Euler(0, 0, 270);
        rightHalf.localPosition = initPositionRightHalf;
        leftHalf.localRotation = Quaternion.Euler(0, 0, 270);
        leftHalf.localPosition = initPositionLeftHalf;
        yield return new WaitForSeconds(activateDelay);
        trapActivated = true;
        yield return new WaitForSeconds(attackDuration * 0.2f);
        canTakeDamageFromNormalAttack = true;
        canDamage = true;
        yield return new WaitForSeconds(attackDuration * 0.8f);
        trapActivated = false;
        rightHalf.localRotation = Quaternion.Euler(0, 0, 345);
        leftHalf.localRotation = Quaternion.Euler(0, 0, 195);
        canDamage = false;
        yield return new WaitForSeconds(activeTime);
        recharge = true;
        EnteredTrigger = false;
        activateCoroutineIsRunning = false;
    }

    public new void ApplyDamage(float damage, string tag, bool instantKill)
    {
        if (Time.time > nextDamage && canTakeDamage)
        {
            if (tag == "SlimeAttack" && canTakeDamageFromNormalAttack)
            {
                nextDamage = Time.time + damageRate;
                currentHealth -= damage;
            }
            else if (tag == "PlungeAttack")
            {
                nextDamage = Time.time + damageRate;
                currentHealth -= damage;
            }

            if (currentHealth <= 0)
            {
                canTakeDamage = false;
                StartCoroutine(Die());
            }
        }
    }

}
