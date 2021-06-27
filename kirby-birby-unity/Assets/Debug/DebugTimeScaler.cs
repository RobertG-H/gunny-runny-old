using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class DebugTimeScaler : MonoBehaviour
{
    public void OnSlowTime(InputAction.CallbackContext context)
    {
        
        float triggerValue = context.ReadValue<float>();
        Debug.Log(triggerValue);
        if(triggerValue > 0) {
            Time.timeScale = Mathf.Lerp(1f, 0f, triggerValue);
        }
        else {
            Time.timeScale = 1f;
        }
    }


}
