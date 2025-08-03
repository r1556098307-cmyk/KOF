using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkillAnimationData
{
    public string skillName;
    public string animationTrigger;
}

public class PlayerAnimator : MonoBehaviour
{
    private PlayerController controller;
    private Animator anim;

    public bool justLanded;
    public bool startedJumping;

    [SerializeField] private List<SkillAnimationData> skillAnimations;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        CheckAnimationState();

        //// 重置一次性动画标志
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
        anim.SetBool("isJump", controller.isJump);
        anim.SetBool("isJumpFall", controller.isJumpFall);
        anim.SetBool("isGround", controller.isGround);
        anim.SetBool("isDash", controller.isDash);
        anim.SetBool("isCrouch", controller.isCrouch);
        anim.SetBool("isBlock", controller.isBlock);

        // 一次性触发的动画
        //if (startedJumping)
        //{
        //    anim.SetTrigger("jumpStart");
        //}

        //if (justLanded)
        //{
        //    anim.SetTrigger("land");
        //}
    }

    public void PlaySkill(string skillName)
    {
        var skillData = skillAnimations.Find(s => s.skillName == skillName);
        if (skillData != null)
        {
            anim.SetTrigger(skillData.animationTrigger);
        }
    }

    //public void StartJump()
    //{
    //    startedJumping = true;
    //}

    //public void JustLanded()
    //{
    //    justLanded = true;
    //}


    public void PlayAnimation(string animationName)
    {
        anim.ResetTrigger("hurtLight");
        anim.ResetTrigger("hurtHeavy");
        anim.ResetTrigger("knockdown");
        anim.ResetTrigger("hitStunEnd");

        switch (animationName)
        {
            case "LightHit":
                anim.SetTrigger("hurtLight");
                break;
            case "HeavyHit":
                anim.SetTrigger("hurtHeavy");
                break;
            case "KnockdownHit":
                anim.SetTrigger("knockdown");
                break;
            case"GetUp":
                anim.SetTrigger("getup");
                break;
            case "HitstunEnd":
                anim.SetTrigger("hitStunEnd");
                break;
        }

    }

    public Animator getAnim()
    {
        return anim;
    }


}
