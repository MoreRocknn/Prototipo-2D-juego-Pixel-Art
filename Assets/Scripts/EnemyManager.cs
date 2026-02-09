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
        // Nota: Asegúrate de tener la clase BossController o ajusta este nombre
        foreach (var boss in FindObjectsOfType<BossController>(true))
            boss?.ResetState();
    }

    void RespawnNormalEnemies()
    {
        foreach (var data in registeredEnemies)
        {
            // Si tienes BossController, evita respawnearlo aquí si ya se manejó arriba
            if (data.currentInstance != null && data.currentInstance.GetComponent<BossController>() != null)
                continue;
            RespawnEnemy(data);
        }
    }

    // --- CORRECCIÓN AQUÍ ---
    void RespawnEnemy(EnemyData data)
    {
        if (data.currentInstance != null)
        {
            // 1. Restaurar posición y rotación
            data.currentInstance.transform.position = data.spawnPosition;
            data.currentInstance.transform.rotation = data.spawnRotation;

            // 2. Asegurar que el objeto esté activo para que sus scripts funcionen
            data.currentInstance.SetActive(true);

            // 3. Buscar ambos tipos de scripts (Terrestre y Volador)
            var enemigoTerrestre = data.currentInstance.GetComponent<Enemigo>();
            var enemigoVolador = data.currentInstance.GetComponent<EnemigoVolador>();

            // 4. Ejecutar la restauración en el que exista
            if (enemigoTerrestre != null)
            {
                enemigoTerrestre.RestoreFullHealth();
                enemigoTerrestre.enabled = true;
            }
            else if (enemigoVolador != null)
            {
                enemigoVolador.RestoreFullHealth();
                enemigoVolador.enabled = true;
            }
        }
        else if (data.enemyPrefab != null)
        {
            // Si la instancia fue destruida, crear una nueva
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