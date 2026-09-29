using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Day00Manager : MonoBehaviour
{
    [SerializeField] private float DayDurationInSeconds = 60f;
    [SerializeField] private bool isDebugMode;
    [SerializeField][Range(1, 10)] private float dayDurationOnDebugModeOn = 5f;

    [Header("Lights settings")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float startLightIntensity = 1f;
    [SerializeField] private float endLightIntensity = 1000f;
    [SerializeField] private float speedLightIntensityChange = 0.1f;

    void Start()
    {
        StartCoroutine(WaitAndLoadNextDay());
        pointLight.intensity = startLightIntensity;
    }

    private void Update()
    {
        pointLight.intensity = Mathf.Lerp(pointLight.intensity, endLightIntensity, Time.deltaTime * speedLightIntensityChange);
    }

    private IEnumerator WaitAndLoadNextDay()
    {
        float duration = isDebugMode ? dayDurationOnDebugModeOn : DayDurationInSeconds;
        Debug.Log($"Day 00 will last for {duration} seconds.");
        yield return new WaitForSeconds(duration);
        Debug.Log("Day 00 has ended. Loading next day...");
        LoadNextDay();
    }

    private void LoadNextDay()
    {
        GameFlowManager.Instance.LoadNextDay(GameFlowManager.Instance.FadeDuration);
    }
}
