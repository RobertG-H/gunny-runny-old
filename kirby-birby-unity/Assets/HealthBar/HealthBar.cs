using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{

    public Slider slider;

    public void SetHealth(float health)
    {
        Debug.Log(health);
        slider.value = health;
    }
    // Start is called before the first frame update
    void Start()
    {

    }
    public void SetMaxHealth(float health)
    {
        slider.maxValue = health;
        slider.value = health;
    }
}