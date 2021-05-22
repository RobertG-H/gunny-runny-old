using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum PowerUpPatches
{
    TopSpeed,
    Turn,
    ChargeRate,
    Boost,
    HP,
    Damage
};

public class PowerUpPatch : NetworkBehaviour
{
    [SerializeField] PowerUpPatchScriptableObject powerUpPatchValues;
    [SerializeField] float fallSpeed;
    [SerializeField] float duration;
    Collider col;
    SpriteRenderer sr;
    int layerMask;
    bool landed = false;
    void Awake()
    {
        col = GetComponent<Collider>();
        sr = GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        layerMask = 1 << LayerMask.NameToLayer("PowerUpPatch");
        layerMask = ~layerMask;
    }

    public void Initialize(PowerUpPatchScriptableObject powerUpPatchValues)
    {
        this.powerUpPatchValues = powerUpPatchValues;
        sr.sprite = powerUpPatchValues.graphic;
        StartCoroutine(DeathTimer());
    }

    void Update()
    {
        if (!isServer) return;
        if (landed) return;
        if (!IsGrounded())
        {
            Vector3 displacement = Vector3.down * fallSpeed * Time.deltaTime;
            transform.position = transform.position + displacement;
        }
    }

    bool IsGrounded()
    {
        float skinWidth = 0.2f;
        Vector3 raycastOrigin = new Vector3(transform.position.x, transform.position.y - (col.bounds.size.y / 2), transform.position.z);


        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hit, fallSpeed * Time.deltaTime + skinWidth, layerMask))
        {
            landed = true;
            return true;
        }
        return false;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IReceivePowerUpPatches target = other.GetComponent<IReceivePowerUpPatches>();
            if (powerUpPatchValues.powerUpName == PowerUpPatches.TopSpeed)
            {
                target.TopSpeedIncrease();
            }
            else if (powerUpPatchValues.powerUpName == PowerUpPatches.Turn)
            {
                target.TurnIncrease();
            }
            else if (powerUpPatchValues.powerUpName == PowerUpPatches.ChargeRate)
            {
                target.ChargeRateIncrease();
            }
            else if (powerUpPatchValues.powerUpName == PowerUpPatches.Boost)
            {
                target.BoostIncrease();
            }
        }
        Destroy(gameObject);
    }

    IEnumerator DeathTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(duration);
            Destroy(gameObject);
        }
    }
}
