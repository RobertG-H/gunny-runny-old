using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJuice : MonoBehaviour
{
    [SerializeField]
    private Animator modelAnimator;

    [SerializeField]
    private float chargeSpeed;

    [SerializeField]
    private bool isEnabled;
    public void OnCharge()
    {
        if (!isEnabled) return;
        modelAnimator.SetFloat("ActionSpeed", chargeSpeed);
        modelAnimator.SetTrigger("Charge");
    }

    public void OnBoost()
    {
        if (!isEnabled) return;

        modelAnimator.SetTrigger("Boost");
    }

    public void OnIdle()
    {
        if (!isEnabled) return;

        modelAnimator.SetTrigger("Idle");
    }
}
