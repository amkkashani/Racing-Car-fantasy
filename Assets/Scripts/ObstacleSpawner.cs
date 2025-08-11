using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour, IEnvUpdater
{
    [SerializeField] private List<Transform> obstacles;
    [SerializeField] private int numberOfSelection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TurnOffAllObstacles();
        UpdateEnv();
    }

    private void TurnOffAllObstacles()
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            obstacles[i].gameObject.SetActive(false);
        }
    }


    public void UpdateEnv()
    {
        TurnOffAllObstacles();
        int count = Random.Range(0, numberOfSelection);
        List<int> selected = GameManagerEndless.GenerateRandomSample(count, obstacles.Count);

        for (int i = 0; i < selected.Count; i++)
        {
            obstacles[selected[i]].gameObject.SetActive(true);
        }
    }
}