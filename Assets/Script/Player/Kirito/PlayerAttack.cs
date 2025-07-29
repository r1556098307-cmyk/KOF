using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject attack1RangeTrigger;
    public GameObject attack2RangeTrigger;
    public GameObject attack3RangeTrigger;
    public GameObject crouchAttack1RangeTrigger;
    public GameObject crouchAttack2RangeTrigger;
    public GameObject crouchAttack3RangeTrigger;
    public GameObject specialMove1RangeTrigger;
    public GameObject specialMove2RangeTrigger;
    public GameObject specialMove3RangeTrigger;

    public void attack1()
    {
        attack1RangeTrigger.SetActive(true);
    }

    public void attack1Finished()
    {
        attack1RangeTrigger.SetActive(false);
    }

    public void attack2()
    {
        attack2RangeTrigger.SetActive(true);
    }

    public void attack2Finished()
    {
        attack2RangeTrigger.SetActive(false);
    }

    public void attack3()
    {
        attack3RangeTrigger.SetActive(true);
    }

    public void attack3Finished()
    {
        attack3RangeTrigger.SetActive(false);
    }

    public void crouchAttack1()
    {
        crouchAttack1RangeTrigger.SetActive(true);
    }

    public void crouchAttack1Finished()
    {
        crouchAttack1RangeTrigger.SetActive(false);
    }

    public void crouchAttack2()
    {
        crouchAttack2RangeTrigger.SetActive(true);
    }

    public void crouchAttack2Finished()
    {
        crouchAttack2RangeTrigger.SetActive(false);
    }

    public void crouchAttack3()
    {
        crouchAttack3RangeTrigger.SetActive(true);
    }

    public void crouchAttack3Finished()
    {
        crouchAttack3RangeTrigger.SetActive(false);
    }

    public void specialMoveAttack1()
    {
        specialMove1RangeTrigger.SetActive(true);
    }

    public void specialMove1Finished()
    {
        specialMove1RangeTrigger.SetActive(false);
    }

    public void specialMove2()
    {
        specialMove2RangeTrigger.SetActive(true);
    }

    public void specialMove2Finished()
    {
        specialMove2RangeTrigger.SetActive(false);
    }

    public void specialMove3()
    {
        specialMove3RangeTrigger.SetActive(true);
    }

    public void specialMove3Finished()
    {
        specialMove3RangeTrigger.SetActive(false);
    }

}
