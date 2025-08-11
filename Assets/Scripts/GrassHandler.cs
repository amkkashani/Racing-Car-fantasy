using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GrassHandler : MonoBehaviour, IEnvUpdater
{
    [SerializeField] private List<Transform> vegetations;

    public void SpawnNewVegetation()
    {
        for (int i = 0; i < vegetations.Count; i++)
        {
            vegetations[i].gameObject.SetActive(false);
        }

        vegetations[Random.Range(0, vegetations.Count)].gameObject.SetActive(true);
    }

    public void UpdateEnv()
    {
        SpawnNewVegetation();
    }


    private void Start()
    {
        SpawnNewVegetation();
    }
}

public interface IEnvUpdater
{
    void UpdateEnv();
}