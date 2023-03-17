using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CinemachineShake : NetworkBehaviour
{
    public static CinemachineShake Instance { get; private set; }
    public CinemachineFreeLook cinemachineFreeLook;
    public float startingIntensity;
    public float shakeTimerTotal;
    private float shakeTimer;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Instance = this;
        }
    }

    public void ShakeCamera(float intensity, float time)
    {
        if (!IsOwner) { return; }
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
        if (!IsOwner) { return; }
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                for (int i = 0; i < 3; i++)
                {
                    CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = cinemachineFreeLook.GetRig(i).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, (1 - (shakeTimer / shakeTimerTotal)));
                }
            }
        }
    }
}
