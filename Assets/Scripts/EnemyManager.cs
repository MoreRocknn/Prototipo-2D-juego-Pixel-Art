using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Sistema de Respawn")]
    public bool autoRegisterEnemies = true;

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

    [Header("Debug")]
    public List<EnemyData> registeredEnemies = new List<EnemyData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (autoRegisterEnemies)
        {
            RegisterAllEnemiesInScene();
        }
    }

    public void RegisterAllEnemiesInScene()
    {
        registeredEnemies.Clear();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            EnemyData data = new EnemyData(
                enemy,
                enemy.transform.position,
                enemy.transform.rotation
            );

            data.currentInstance = enemy;
            registeredEnemies.Add(data);
        }

        Debug.Log($"[EnemyManager] {registeredEnemies.Count} enemigos registrados");
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        EnemyData data = new EnemyData(
            enemy,
            enemy.transform.position,
            enemy.transform.rotation
        );

        data.currentInstance = enemy;
        registeredEnemies.Add(data);
    }

    public void RespawnAllEnemies()
    {
        Debug.Log("========== RESPAWN DE ENEMIGOS INICIADO ==========");

        int respawnedCount = 0;

        // PASO 1: Resetear TODOS los bosses primero
        respawnedCount += RespawnAllBosses();

        // PASO 2: Resetear enemigos normales
        respawnedCount += RespawnNormalEnemies();

        Debug.Log($"========== {respawnedCount} ENEMIGOS RESETEADOS ==========");
    }

    int RespawnAllBosses()
    {
        int count = 0;

        BossController[] bosses = FindObjectsOfType<BossController>(true);

        foreach (BossController boss in bosses)
        {
            if (boss != null)
            {
                Debug.Log($"[Boss] Reseteando: {boss.gameObject.name}");
                boss.ResetState();
                count++;
            }
        }

        return count;
    }

    int RespawnNormalEnemies()
    {
        int count = 0;

        foreach (EnemyData data in registeredEnemies)
        {
            if (data.currentInstance != null && data.currentInstance.GetComponent<BossController>() != null)
                continue;

            if (RespawnEnemy(data))
                count++;
        }

        return count;
    }

    bool RespawnEnemy(EnemyData data)
    {
        if (data.currentInstance != null && data.currentInstance.activeInHierarchy)
        {
            ResetActiveEnemy(data);
            return true;
        }

        if (data.currentInstance != null && !data.currentInstance.activeInHierarchy)
        {
            ReactivateEnemy(data);
            return true;
        }

        if (data.enemyPrefab != null)
        {
            RecreateEnemy(data);
            return true;
        }

        return false;
    }

    void ResetActiveEnemy(EnemyData data)
    {
        Enemigo enemyScript = data.currentInstance.GetComponent<Enemigo>();
        if (enemyScript != null)
        {
            enemyScript.RestoreFullHealth();
        }

        data.currentInstance.transform.position = data.spawnPosition;
        data.currentInstance.transform.rotation = data.spawnRotation;
    }

    void ReactivateEnemy(EnemyData data)
    {
        data.currentInstance.SetActive(true);
        data.currentInstance.transform.position = data.spawnPosition;
        data.currentInstance.transform.rotation = data.spawnRotation;

        Enemigo enemyScript = data.currentInstance.GetComponent<Enemigo>();
        if (enemyScript != null)
        {
            enemyScript.RestoreFullHealth();
            enemyScript.enabled = true;
        }
    }

    void RecreateEnemy(EnemyData data)
    {
        GameObject newEnemy = Instantiate(data.enemyPrefab, data.spawnPosition, data.spawnRotation);
        newEnemy.tag = "Enemy";
        data.currentInstance = newEnemy;
    }

    public void RespawnEnemiesInRadius(Vector3 center, float radius)
    {
        foreach (EnemyData data in registeredEnemies)
        {
            if (Vector3.Distance(data.spawnPosition, center) <= radius)
            {
                RespawnEnemy(data);
            }
        }
    }

    public void OnEnemyDeath(GameObject enemy)
    {
        foreach (EnemyData data in registeredEnemies)
        {
            if (data.currentInstance == enemy)
            {
                enemy.SetActive(false);
                return;
            }
        }
    }
}
