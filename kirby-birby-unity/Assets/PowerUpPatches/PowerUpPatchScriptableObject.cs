using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpPatch", menuName = "ScriptableObjects/PowerUpPatch", order = 1)]
public class PowerUpPatchScriptableObject : ScriptableObject
{
    public PowerUpPatches powerUpName;
    public Sprite graphic;
}
