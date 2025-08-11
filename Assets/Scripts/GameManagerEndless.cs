using System;
using System.Collections.Generic;
using System.Linq;
using Racing2D;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;
using Random = UnityEngine.Random;

public class GameManagerEndless : SingletonMagic<GameManagerEndless>
{
    [SerializeField] private float distanceMoved = 0;

    // [SerializeField] private Transform parent_car_pool;
    [SerializeField] private List<Transform> roadBlocks;
    [SerializeField] private Transform player;
    [SerializeField] private Transform followingCamera;


    //car spawn
    [SerializeField] private float waitTimeCarSpawn = 1.5f;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private List<Transform> targetPoints;

    [SerializeField] private Transform carPoolParent;

    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private float noPysicsTime = 5.0f;
    [SerializeField] private KeyCode respawnKey = KeyCode.R;
    [SerializeField] private KeyCode noPhysicsKey = KeyCode.T;

    private Vector3 startPosition;
    private CarController[] m_CarControllers;
    private const float BLOCK_SIZE = 60.0f;

    [SerializeField] private List<CarController> activeCarPool = new List<CarController>();
    [SerializeField] private List<CarController> deactiveCarPool = new List<CarController>();

    private float timeFromLastSpawn = 0.0f;
    private Vector3 firstPosCamera;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPosition = player.position;
        m_CarControllers = carPoolParent.GetComponentsInChildren<CarController>();
        TurnOffAllCars();

        deactiveCarPool = m_CarControllers.ToList();
        firstPosCamera = followingCamera.position;
    }

    // Update is called once per frame
    void Update()
    {
        //score
        distanceMoved = (player.position - startPosition).z;
        followingCamera.position = firstPosCamera + Vector3.forward * distanceMoved;

        scoreText.text = (Mathf.RoundToInt(distanceMoved / 10)).ToString();


        //check teleport new block
        Transform firstRoadBlock = FindFirstRoadBlock();

        if (player.position.z - firstRoadBlock.position.z > BLOCK_SIZE * 2f)
        {
            Debug.Log("Z Shift");
            MoveFirstRoadBlocksToEnd(firstRoadBlock);
            UpdateSpawnPointsPosition(BLOCK_SIZE);
        }


        timeFromLastSpawn += Time.deltaTime;

        if (timeFromLastSpawn >= waitTimeCarSpawn)
        {
            timeFromLastSpawn = 0.0f;
            SpawnCarFromPool();
        }

        if (Input.GetKey(respawnKey))
        {
            TransformCarInTheMiddle();
        }

        if (Input.GetKey(noPhysicsKey))
        {
            TurnOffCarPhysics();
        }
    }


    private void SpawnCarFromPool()
    {
        // How many cars do we want to spawn this frame?
        int amountToSpawn = Random.Range(1, spawnPoints.Count + 1); // never 0 now
        List<int> spawnPointsSample = GenerateRandomSample(amountToSpawn, spawnPoints.Count);

        for (int i = 0; i < spawnPointsSample.Count; i++)
        {
            if (deactiveCarPool.Count == 0) break; // safeguard

            int selectedCarIndex = Random.Range(0, deactiveCarPool.Count);
            CarController car = deactiveCarPool[selectedCarIndex];
            deactiveCarPool.RemoveAt(selectedCarIndex); // *** ← remove immediately ***

            activeCarPool.Add(car);

            Transform t = car.transform;
            t.gameObject.SetActive(true);
            t.SetPositionAndRotation(
                spawnPoints[spawnPointsSample[i]].position,
                spawnPoints[spawnPointsSample[i]].rotation);

            car.GetComponent<CarAIControl>()
                .SetTarget(targetPoints[spawnPointsSample[i]]);
        }

        // No second loop needed
        DeactiveDeadNPCs();
    }

    // private void SpawnCarFromPool()
    // {
    //     List<int> spawnPointsSample = GenerateRandomSample(Random.Range(0, spawnPoints.Count), spawnPoints.Count);
    //
    //     //update deactives and activs
    //     for (int i = 0; i < spawnPointsSample.Count; i++)
    //     {
    //         int selectedCarIndex = Random.Range(0, deactiveCarPool.Count);
    //         activeCarPool.Add(deactiveCarPool[selectedCarIndex]);
    //         Transform carTransform = deactiveCarPool[selectedCarIndex].transform;
    //         carTransform.gameObject.SetActive(true);
    //         carTransform.position = spawnPoints[spawnPointsSample[i]].position;
    //         carTransform.rotation = spawnPoints[spawnPointsSample[i]].rotation;
    //
    //         CarAIControl carAIControl = carTransform.GetComponent<CarAIControl>();
    //         carAIControl.SetTarget(targetPoints[spawnPointsSample[i]]);
    //     }
    //
    //     for (int i = 0; i < spawnPointsSample.Count; i++)
    //     {
    //         deactiveCarPool.Remove(deactiveCarPool[i]);
    //         
    //     }
    //     
    //     DeactiveDeadNPCs();
    // }

    private void DeactiveDeadNPCs()
    {
        for (int i = activeCarPool.Count - 1; i >= 0; i--)
        {
            if (activeCarPool[i].transform.position.y < -25)
            {
                deactiveCarPool.Add(activeCarPool[i]);
                activeCarPool[i].gameObject.SetActive(false);
                activeCarPool.RemoveAt(i);
            }
        }
    }

    private void TurnOffAllCars()
    {
        for (int i = 0; i < m_CarControllers.Length; i++)
        {
            m_CarControllers[i].gameObject.SetActive(false);
        }
    }

    private Transform FindFirstRoadBlock()
    {
        float minZ = float.MaxValue;
        int index = 0;
        for (int i = 0; i < roadBlocks.Count; i++)
        {
            if (roadBlocks[i].position.z < minZ)
            {
                minZ = roadBlocks[i].position.z;
                index = i;
            }
        }

        return roadBlocks[index];
    }

    private void MoveFirstRoadBlocksToEnd(Transform roadBlock)
    {
        roadBlock.position += Vector3.forward * (BLOCK_SIZE * roadBlocks.Count);
        IEnvUpdater[] updaters = roadBlock.GetComponentsInChildren<IEnvUpdater>();

        for (int i = 0; i < updaters.Length; i++)
        {
            updaters[i].UpdateEnv();
        }
    }

    private void UpdateSpawnPointsPosition(float Zshift)
    {
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            // i don't know why it works ??? Zshift/2
            spawnPoints[i].position += Vector3.forward * Zshift;
            targetPoints[i].position += Vector3.forward * Zshift;
        }
    }


    public static List<int> GenerateRandomSample(int count, int max_size)
    {
        List<int> samples = new List<int>();
        int success = 0;

        while (success < count)
        {
            int newSample = +Random.Range(0, max_size);

            if (!samples.Contains(newSample))
            {
                samples.Add(newSample);
                success++;
            }
        }

        return samples;
    }

    public void TransformCarInTheMiddle()
    {
        player.position = Vector3.zero + Vector3.forward * player.position.z + Vector3.up * 1f;
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        player.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        player.rotation = Quaternion.identity;
    }

    public void TurnOffCarPhysics()
    {
        StartCoroutine(player.GetComponent<CarUserControl>().TemporaryLayerChange(noPysicsTime));
    }
}