using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Sistema de Respawn")]
    public bool autoRegisterEnemies = true; // Registrar enemigos automáticamente al inicio

    [System.Serializable]
    public class EnemyData
    {
        public GameObject enemyPrefab;
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        public GameObject currentInstance; // Referencia a la instancia actual

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
        }
    }

    void Start()
    {
        if (autoRegisterEnemies)
        {
            RegisterAllEnemiesInScene();
        }
    }

    // Registrar todos los enemigos de la escena al inicio
    public void RegisterAllEnemiesInScene()
    {
        registeredEnemies.Clear();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            // Crear un "prefab virtual" guardando los componentes del enemigo
            EnemyData data = new EnemyData(
                enemy, // Guardaremos referencia al objeto original
                enemy.transform.position,
                enemy.transform.rotation
            );

            data.currentInstance = enemy;
            registeredEnemies.Add(data);
        }

        Debug.Log($"EnemyManager: {registeredEnemies.Count} enemigos registrados");
    }

    // Registrar un enemigo manualmente
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

        Debug.Log($"Enemigo registrado: {enemy.name}");
    }

    // Reaparecen TODOS los enemigos
    public void RespawnAllEnemies()
    {
        int respawnedCount = 0;

        foreach (EnemyData data in registeredEnemies)
        {
            // Si el enemigo actual existe y está vivo, no hacer nada
            if (data.currentInstance != null && data.currentInstance.activeInHierarchy)
            {
                // Restaurar vida completa usando método público
                Enemigo enemyScript = data.currentInstance.GetComponent<Enemigo>();
                if (enemyScript != null)
                {
                    enemyScript.RestoreFullHealth();
                }

                // Resetear posición
                data.currentInstance.transform.position = data.spawnPosition;
                data.currentInstance.transform.rotation = data.spawnRotation;

                respawnedCount++;
            }
            // Si el enemigo fue destruido, reactivarlo
            else if (data.currentInstance != null && !data.currentInstance.activeInHierarchy)
            {
                data.currentInstance.SetActive(true);
                data.currentInstance.transform.position = data.spawnPosition;
                data.currentInstance.transform.rotation = data.spawnRotation;

                // Restaurar vida completa usando método público
                Enemigo enemyScript = data.currentInstance.GetComponent<Enemigo>();
                if (enemyScript != null)
                {
                    enemyScript.RestoreFullHealth();
                    enemyScript.enabled = true;
                }

                respawnedCount++;
            }
            // Si la instancia fue completamente destruida, crear una nueva
            else
            {
                // Instantiate del prefab original
                GameObject newEnemy = Instantiate(data.enemyPrefab, data.spawnPosition, data.spawnRotation);
                newEnemy.tag = "Enemy";
                data.currentInstance = newEnemy;

                respawnedCount++;
            }
        }
        BossController[] bosses = FindObjectsOfType<BossController>();
        foreach (BossController boss in bosses)
        {
            if (boss != null)
            {
                boss.ResetState();
                respawnedCount++;
                Debug.Log("Boss reseteado por EnemyManager");
            }
        }

        Debug.Log($"EnemyManager: {respawnedCount} enemigos reaparecidos");
    }

    // Opcional: Reaparece solo enemigos en un radio
    public void RespawnEnemiesInRadius(Vector3 center, float radius)
    {
        foreach (EnemyData data in registeredEnemies)
        {
            if (Vector3.Distance(data.spawnPosition, center) <= radius)
            {
                if (data.currentInstance != null)
                {
                    data.currentInstance.SetActive(true);
                    data.currentInstance.transform.position = data.spawnPosition;
                    data.currentInstance.transform.rotation = data.spawnRotation;

                    Enemigo enemyScript = data.currentInstance.GetComponent<Enemigo>();
                    if (enemyScript != null)
                    {
                        enemyScript.health = enemyScript.maxHealth;
                    }
                }
            }
        }
    }

    // Llamar esto cuando un enemigo muera para desactivarlo en lugar de destruirlo
    public void OnEnemyDeath(GameObject enemy)
    {
        // Encontrar el enemigo en la lista
        foreach (EnemyData data in registeredEnemies)
        {
            if (data.currentInstance == enemy)
            {
                // Desactivar en lugar de destruir
                enemy.SetActive(false);
                Debug.Log($"Enemigo {enemy.name} desactivado (puede reaparecen)");
                return;
            }
        }
    }
}