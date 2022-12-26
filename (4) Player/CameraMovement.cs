using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float speed = 1;
    public float rotAngle = 70;

    void Update()
    {
        transform.rotation = Quaternion.Euler(0, Mathf.Sin(Time.realtimeSinceStartup * speed) * rotAngle, 0);
    }
}
