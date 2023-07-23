using Cinemachine;
using UnityEngine;

public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance { get; private set; }
    public CinemachineFreeLook cinemachineFreeLook;
    public float startingIntensity;
    public float shakeTimerTotal;
    private float shakeTimer;
    public bool canShake = true;

    private void Awake()
    {
        Instance = this;
    }

    public void ShakeCamera(float intensity, float time)
    {
        if (!canShake) { return; }
        for (int i = 0; i < 3; i++)
        {
            CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = cinemachineFreeLook.GetRig(i).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = intensity;
        }
        startingIntensity = intensity;
        shakeTimerTotal = time;
        shakeTimer = time;
    }

    private void Update()
    {
        if (!canShake) { return; }
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            for (int i = 0; i < 3; i++)
            {
                CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = cinemachineFreeLook.GetRig(i).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, (1 - (shakeTimer / shakeTimerTotal)));
            }
        }
    }
}
