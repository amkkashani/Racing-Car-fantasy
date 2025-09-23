using System;
using System.Collections.Generic;
using System.Linq;
using Racing2D;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;
using Random = UnityEngine.Random;

public class GameManagerEndless : SingletonMagic<GameManagerEndless>
{
    [SerializeField] private float distanceMoved = 0;

    [SerializeField] private List<Transform> roadBlocks;
    [SerializeField] private Transform player;
    [SerializeField] private Transform followingCamera;

    //car spawn
    [SerializeField] private float waitTimeCarSpawn = 1.5f;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private List<Transform> targetPoints;

    [SerializeField] private Transform carPoolParent;

    [Header("Game Score Board")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private int health = 100;

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

    // ---------- NEW: game over + progression feed helpers ----------
    private bool isGameOver = false;
    private Vector3 lastPlayerPos; // for per-frame distance integration

    [Header("Progression Feed")]
    [Tooltip("Send per-frame meters moved to ProgressionSystem TotalKm.")]
    [SerializeField] private bool feedDistanceToProgression = true;

    [Tooltip("Ignore tiny jitters below this distance (meters) per frame.")]
    [SerializeField, Range(0f, 0.5f)] private float movementJitterTolerance = 0.02f;
    // ----------------------------------------------------------------
    
    [Header("UI")]
    [SerializeField] private GameObject loseMenu; // drag your Lose Menu here


    void Start()
    {
        startPosition = player.position;
        lastPlayerPos = player.position; // NEW: initialize integrator

        m_CarControllers = carPoolParent.GetComponentsInChildren<CarController>();
        TurnOffAllCars();

        deactiveCarPool = m_CarControllers.ToList();
        firstPosCamera = followingCamera.position;
        healthBar.SetHealthBar(health);
        
        if (loseMenu) loseMenu.SetActive(false);
    }

    void Update()
    {
        if (isGameOver) return; // NEW: freeze gameplay after losing

        //score
        distanceMoved = (player.position - startPosition).z;
        followingCamera.position = firstPosCamera + Vector3.forward * distanceMoved;

        scoreText.text = (Mathf.RoundToInt(distanceMoved / 10)).ToString();

        // ---------- NEW: feed actual path length to ProgressionSystem ----------
        if (feedDistanceToProgression && ProgressionSystem.Instance != null)
        {
            float deltaMeters = Vector3.Distance(player.position, lastPlayerPos);
            if (deltaMeters >= movementJitterTolerance)
            {
                ProgressionSystem.Instance.AddDistanceMeters(deltaMeters);
                lastPlayerPos = player.position;
            }
        }
        // ----------------------------------------------------------------------

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
        int amountToSpawn = Random.Range(1, spawnPoints.Count + 1); // never 0 now
        List<int> spawnPointsSample = GenerateRandomSample(amountToSpawn, spawnPoints.Count);

        for (int i = 0; i < spawnPointsSample.Count; i++)
        {
            if (deactiveCarPool.Count == 0) break;

            int selectedCarIndex = Random.Range(0, deactiveCarPool.Count);
            CarController car = deactiveCarPool[selectedCarIndex];
            deactiveCarPool.RemoveAt(selectedCarIndex);
            activeCarPool.Add(car);

            Transform t = car.transform;
            t.gameObject.SetActive(true);
            t.SetPositionAndRotation(
                spawnPoints[spawnPointsSample[i]].position,
                spawnPoints[spawnPointsSample[i]].rotation);

            car.GetComponent<CarAIControl>()
                .SetTarget(targetPoints[spawnPointsSample[i]]);
        }

        DeactiveDeadNPCs();
    }

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

    // Car moved to center
    public void TransformCarInTheMiddle(bool isDead = false)
    {
        if (isDead) SetHealth(0);
        player.position = Vector3.zero + Vector3.forward * player.position.z + Vector3.up * 1f;
        var rb = player.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.rotation = Quaternion.identity;
    }

    public void TurnOffCarPhysics()
    {
        StartCoroutine(player.GetComponent<CarUserControl>().TemporaryLayerChange(noPysicsTime));
    }

    public void SetDamage(int amount)
    {
        health -= amount;
        healthBar.SetHealthBar(health);

        if (health <= 0)
        {
            health = 0;
            Lose(); // ---------- NEW: trigger lose once ----------
        }
    }

    public void SetHealth(int value)
    {
        health = value;
        healthBar.SetHealthBar(health);
        if (health <= 0) { health = 0; Lose(); }
    }

    public int GetHealth() => health;

    public void Lose()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Activate UI losing
        if (loseMenu) loseMenu.SetActive(true);

        // Stop player control & motion
        var userCtrl = player.GetComponent<CarUserControl>();
        if (userCtrl) userCtrl.enabled = false;

        var rb = player.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // freeze only the player's car
        }

        // NOTE: we no longer freeze/stop NPC cars per your request

        Debug.Log("[GameManagerEndless] GAME OVER");
        ProgressionSystem.Instance.Save();
    }

    
    public void Replay(bool reset = true)
    {
        Debug.Log("Replay clicked");
        
        // allow gameplay again
        isGameOver = false;

        // hide lose UI (if any)
        if (loseMenu) loseMenu.SetActive(false);

        // ensure player control is enabled & unfreeze RB
        var userCtrl = player.GetComponent<CarUserControl>();
        if (userCtrl) userCtrl.enabled = true;

        var rb = player.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false; // unfreeze so physics resumes

        // reset cars to a clean state (hidden & ready to respawn)
        TurnOffAllCars();
        activeCarPool.Clear();
        if (carPoolParent != null)
            deactiveCarPool = carPoolParent.GetComponentsInChildren<CarController>(true).ToList();
        timeFromLastSpawn = 0f;

        // restore health
        this.SetHealth(100);

        // reposition & reset baselines (both branches do the same center reset)
        TransformCarInTheMiddle();

        if (reset)
        {
            distanceMoved = 0f;
            if (scoreText) scoreText.text = "0";
        }

        // set new baselines so future deltas are measured from here
        startPosition = player.position;
        lastPlayerPos = player.position;

        // snap camera back to its starting offset
        if (followingCamera) firstPosCamera = followingCamera.position;
    }


}
