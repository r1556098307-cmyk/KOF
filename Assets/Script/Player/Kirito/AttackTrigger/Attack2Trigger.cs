using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack2Trigger : MonoBehaviour
{
    [Header("攻击2设置")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockupForce = 0.3f;
    [SerializeField] private LayerMask targetLayers; // 可攻击的层级

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰撞对象是否在目标层级（Player 或 PlayerPassThrough）
        bool isInTargetLayer = ((1 << collision.gameObject.layer) & targetLayers) != 0;

        // 如果不在目标层级，或者碰撞对象是自身，则忽略
        if (!isInTargetLayer || collision.transform.parent == transform.parent)
            return;


        // 获取目标的组件
        PlayerAnimator targetAnimator = collision.GetComponent<PlayerAnimator>();
        Rigidbody2D targetRb = collision.GetComponent<Rigidbody2D>();

        if (targetAnimator != null)
        {
            // 播放受击动画
            targetAnimator.HurtHeavy();

            // 施加击退力
            if (targetRb != null)
            {
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                knockbackDirection.y = knockupForce; // 稍微向上的击退
                targetRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
}
