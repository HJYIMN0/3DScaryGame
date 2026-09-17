using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Day00Manager : MonoBehaviour
{
    [SerializeField] private float DayDurationInSeconds = 60f;
    [SerializeField] private bool isDebugMode;
    [SerializeField][Range(1, 10)] private float dayDurationOnDebugModeOn = 5f;

    void Start()
    {
        StartCoroutine(WaitAndLoadNextDay());
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
