using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Sistema de Respawn")]
    public bool autoRegisterEnemies = true;
    public bool resetBossOnPlayerDeath = false;

    [System.Serializable]
    public class EnemyData
    {
        public GameObject enemyPrefab;
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        public GameObject currentInstance;

        public EnemyData(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            enemyPrefab = prefab;
            spawnPosition = pos;
            spawnRotation = rot;
        }
    }

    public List<EnemyData> registeredEnemies = new List<EnemyData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (autoRegisterEnemies) RegisterAllEnemiesInScene();
    }

    public void RegisterAllEnemiesInScene()
    {
        registeredEnemies.Clear();
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            registeredEnemies.Add(new EnemyData(enemy, enemy.transform.position, enemy.transform.rotation) 
            { currentInstance = enemy });
        }
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        registeredEnemies.Add(new EnemyData(enemy, enemy.transform.position, enemy.transform.rotation) 
        { currentInstance = enemy });
    }

    public void RespawnAllEnemies()
    {
        if (resetBossOnPlayerDeath) RespawnBosses();
        RespawnNormalEnemies();
    }

    public void RespawnAllEnemiesIncludingBoss()
    {
        RespawnBosses();
        RespawnNormalEnemies();
    }

    void RespawnBosses()
    {
        foreach (var boss in FindObjectsOfType<BossController>(true))
            boss?.ResetState();
    }

    void RespawnNormalEnemies()
    {
        foreach (var data in registeredEnemies)
        {
            if (data.currentInstance != null && data.currentInstance.GetComponent<BossController>() != null)
                continue;
            RespawnEnemy(data);
        }
    }

    void RespawnEnemy(EnemyData data)
    {
        if (data.currentInstance != null)
        {
            if (data.currentInstance.activeInHierarchy)
            {
                data.currentInstance.GetComponent<Enemigo>()?.RestoreFullHealth();
                data.currentInstance.transform.position = data.spawnPosition;
                data.currentInstance.transform.rotation = data.spawnRotation;
            }
            else
            {
                data.currentInstance.SetActive(true);
                data.currentInstance.transform.position = data.spawnPosition;
                data.currentInstance.transform.rotation = data.spawnRotation;
                var e = data.currentInstance.GetComponent<Enemigo>();
                if (e != null) { e.RestoreFullHealth(); e.enabled = true; }
            }
        }
        else if (data.enemyPrefab != null)
        {
            data.currentInstance = Instantiate(data.enemyPrefab, data.spawnPosition, data.spawnRotation);
            data.currentInstance.tag = "Enemy";
        }
    }

    public void OnEnemyDeath(GameObject enemy)
    {
        foreach (var data in registeredEnemies)
        {
            if (data.currentInstance == enemy)
            {
                enemy.SetActive(false);
                return;
            }
        }
    }
}