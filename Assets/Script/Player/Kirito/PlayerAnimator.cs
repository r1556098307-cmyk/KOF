using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private PlayerController controller;
    private Animator anim;

    public bool justLanded;
    public bool startedJumping;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        CheckAnimationState();

        //// ����һ���Զ�����־
        //if (justLanded)
        //{
        //    justLanded = false;
        //}

        //if (startedJumping)
        //{
        //    startedJumping = false;
        //}
    }

    private void CheckAnimationState()
    {
        anim.SetBool("isWalk", controller.isWalk);
        anim.SetBool("isAttack", controller.isAttack);
        anim.SetBool("isJump", controller.isJump);
        anim.SetBool("isJumpFall", controller.isJumpFall);
        anim.SetBool("isGround", controller.isGround);
        anim.SetBool("isDash", controller.isDash);
        anim.SetBool("isCrouch", controller.isCrouch);
        anim.SetBool("isBlock", controller.isBlock);

        // һ���Դ����Ķ���
        //if (startedJumping)
        //{
        //    anim.SetTrigger("jumpStart");
        //}

        //if (justLanded)
        //{
        //    anim.SetTrigger("land");
        //}
    }

    public void Attack()
    {
        anim.SetTrigger("attack");
    }

    //public void StartJump()
    //{
    //    startedJumping = true;
    //}

    //public void JustLanded()
    //{
    //    justLanded = true;
    //}

    public void HurtLight()
    {
        anim.SetTrigger("hurtLight");
    }

    public void HurtHeavy()
    {
        anim.SetTrigger("hurtHeavy");
    }

    public void Knockdown()
    {
        anim.SetTrigger("knockdown");
    }

    public void SpecailMove1()
    {
        Debug.Log("����1");
        anim.SetTrigger("specialMove1");
    }

    public void SpecailMove2()
    {
        Debug.Log("����2");
        anim.SetTrigger("specialMove2");
    }

    public void SpecailMove3()
    {
        Debug.Log("����3");
        anim.SetTrigger("specialMove3");
    }


}
