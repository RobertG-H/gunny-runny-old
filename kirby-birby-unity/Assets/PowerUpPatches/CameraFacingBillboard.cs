using UnityEngine;
using System.Collections;

/// <summary>
/// Src: http://wiki.unity3d.com/index.php/CameraFacingBillboard
/// </summary>
public class CameraFacingBillboard : MonoBehaviour
{
    Camera m_Camera;

    void LateUpdate()
    {
        if (!m_Camera)
        {
            FindCamera();
        }
        else
        {
            transform.LookAt(transform.position + m_Camera.transform.rotation * Vector3.forward,
                m_Camera.transform.rotation * Vector3.up);
        }

    }

    void FindCamera()
    {
        m_Camera = Camera.main;
    }
}