using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISFXPlay : MonoBehaviour
{
    public void PlayUIConfirmSound()
    {
        AudioManager.Instance?.PlaySFX("ui_confirm");
    }
}
