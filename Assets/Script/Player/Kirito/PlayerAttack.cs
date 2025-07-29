using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject attck1RangeTrigger;
    public GameObject attck2RangeTrigger;
    public GameObject attck3RangeTrigger;

    public void attack1()
    {
        attck1RangeTrigger.SetActive(true);
    }

    public void attack1Finished()
    {
        attck1RangeTrigger.SetActive(false);
    }

    public void attack2()
    {
        attck2RangeTrigger.SetActive(true);
    }

    public void attack2Finished()
    {
        attck2RangeTrigger.SetActive(false);
    }

    public void attack3()
    {
        attck3RangeTrigger.SetActive(true);
    }

    public void attack3Finished()
    {
        attck3RangeTrigger.SetActive(false);
    }

}
