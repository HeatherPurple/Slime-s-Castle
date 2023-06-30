using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_EnemyTrigger : MonoBehaviour
{
    [SerializeField] private scr_EnemySpider spider;
    [SerializeField] private scr_EnemyShootingSpider shootingSpider;
    [SerializeField] private scr_EnemyFlytrap flytrap;

    [SerializeField] private EnemyType enemyType;

    private enum EnemyType
    {
        Spider,
        ShootingSpider,
        Flytrap
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch (enemyType)
        {
            case EnemyType.Spider:
                spider.TryAttack(collision);
                break;
            case EnemyType.Flytrap:
                flytrap.React(collision);
                break;
            default:
                break;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        switch (enemyType)
        {
            case EnemyType.Spider:
                spider.TryAttack(collision);
                break;
            case EnemyType.ShootingSpider:
                if (collision.gameObject.CompareTag("Player"))
                {
                    shootingSpider.playerInArea = true;
                }
                break;
            case EnemyType.Flytrap:
                flytrap.React(collision);
                break;
            default:
                break;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        switch (enemyType)
        {
            case EnemyType.ShootingSpider:
                if (collision.gameObject.CompareTag("Player"))
                {
                    shootingSpider.playerInArea = false;
                }
                break;
            default:
                break;
        }
    }

}
