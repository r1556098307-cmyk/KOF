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

    [SerializeField] private List<SkillAnimationData> skillAnimations;



    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        CheckAnimationState();

        PlaySFX();


    }





    private void PlaySFX()
    {
        if (controller.isWalk
            && controller.isGround
            && !controller.isJump
            && !controller.isJumpFall
            && !controller.isDash
            && !controller.isAttack)
        {
            AudioManager.Instance.PlayLoopingSFX("run");
        }
        else
        {
            AudioManager.Instance.StopLoopingSFX("run");
        }
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


    }

    public void PlaySkill(string skillName)
    {
        var skillData = skillAnimations.Find(s => s.skillName == skillName);
        if (skillData != null)
        {
            anim.SetTrigger(skillData.animationTrigger);
        }
    }

    public void PlayVFX(string vfxName)
    {
        VFXManager.Instance.PlayVFXAt(vfxName, transform, controller.isFacingRight);
    }


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
