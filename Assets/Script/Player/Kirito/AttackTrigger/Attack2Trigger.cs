using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack2Trigger : MonoBehaviour
{
    [Header("����2����")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockupForce = 0.3f;
    [SerializeField] private LayerMask targetLayers; // �ɹ����Ĳ㼶

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // �����ײ�����Ƿ���Ŀ��㼶��Player �� PlayerPassThrough��
        bool isInTargetLayer = ((1 << collision.gameObject.layer) & targetLayers) != 0;

        // �������Ŀ��㼶��������ײ���������������
        if (!isInTargetLayer || collision.transform.parent == transform.parent)
            return;


        // ��ȡĿ������
        PlayerAnimator targetAnimator = collision.GetComponent<PlayerAnimator>();
        Rigidbody2D targetRb = collision.GetComponent<Rigidbody2D>();

        if (targetAnimator != null)
        {
            // �����ܻ�����
            targetAnimator.HurtHeavy();

            // ʩ�ӻ�����
            if (targetRb != null)
            {
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                knockbackDirection.y = knockupForce; // ��΢���ϵĻ���
                targetRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
}
