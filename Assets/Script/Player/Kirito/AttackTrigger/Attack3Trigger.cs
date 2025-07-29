using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack3Trigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag != "Player")
            return;
        PlayerAnimator playerAnimator = collision.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
            playerAnimator.Knockdown();

    }
}
