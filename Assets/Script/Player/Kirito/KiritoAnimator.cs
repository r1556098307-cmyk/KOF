using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KiritoAnimator : MonoBehaviour
{
    private KiritoController controller;
    private Animator anim;

    private void Awake()
    {
        controller = GetComponent<KiritoController>();
        anim = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        CheckAnimationState();
    }

    private void CheckAnimationState()
    {
        anim.SetBool("isWalk", controller.isWalk);
        anim.SetBool("isAttack", controller.isAttack);

    }

    public void Attack()
    {
        anim.SetTrigger("attack");
    }

    public void DownAttack()
    {
        anim.SetTrigger("downAttack");
    }



}
