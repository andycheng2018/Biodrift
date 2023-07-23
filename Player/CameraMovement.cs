using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float speed = 1;
    [SerializeField] private float rotAngle = 70;

    private void Update()
    {
        transform.rotation = Quaternion.Euler(0, -90 + Mathf.Sin(Time.realtimeSinceStartup * speed) * rotAngle, 0);
    }
}
