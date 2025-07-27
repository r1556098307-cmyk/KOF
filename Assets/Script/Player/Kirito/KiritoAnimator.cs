using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KiritoAnimator : MonoBehaviour
{
    private KiritoController controller;
    private Animator anim;

    public bool justLanded;
    public bool startedJumping;

    private void Awake()
    {
        controller = GetComponent<KiritoController>();
        anim = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        CheckAnimationState();

        // 重置一次性动画标志
        if (justLanded)
        {
            justLanded = false;
        }

        if (startedJumping)
        {
            startedJumping = false;
        }
    }

    private void CheckAnimationState()
    {
        anim.SetBool("isWalk", controller.isWalk);
        anim.SetBool("isAttack", controller.isAttack);


        anim.SetBool("isJump", controller.isJump);
        anim.SetBool("isJumpFall", controller.isJumpFall);
        anim.SetBool("isGround", controller.isGround);

        // 一次性触发的动画
        if (startedJumping)
        {
            anim.SetTrigger("jumpStart");
        }

        if (justLanded)
        {
            anim.SetTrigger("land");
        }
    }

    public void Attack()
    {
        anim.SetTrigger("attack");
    }

    public void StartJump()
    {
        startedJumping = true;
    }

    public void JustLanded()
    {
        justLanded = true;
    }




}
