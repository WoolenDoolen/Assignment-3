using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager
{
    public enum GameState
    {
        PREGAME,
        INWAVE,
        WAVEEND,
        COUNTDOWN,
        GAMEOVER,
        VICTORY
    }
    public GameState state;

    public int countdown;
    public int wave;
    public int maxWaves;
    public string levelName;
    public string resultMessage;
    public int enemiesSpawned;
    public int enemiesDefeated;

    private static GameManager theInstance;
    public static GameManager Instance {  get
        {
            if (theInstance == null)
                theInstance = new GameManager();
            return theInstance;
        }
    }

    public GameObject player;
    
    public ProjectileManager projectileManager;
    public SpellIconManager spellIconManager;
    public EnemySpriteManager enemySpriteManager;
    public PlayerSpriteManager playerSpriteManager;
    public RelicIconManager relicIconManager;

    private List<GameObject> enemies;
    public int enemy_count { get { return enemies.Count; } }

    public void AddEnemy(GameObject enemy)
    {
        enemies.Add(enemy);
        enemiesSpawned++;
    }
    public void RemoveEnemy(GameObject enemy)
    {
        if (enemies.Remove(enemy))
        {
            enemiesDefeated++;
        }
    }

    public GameObject GetClosestEnemy(Vector3 point)
    {
        if (enemies == null || enemies.Count == 0) return null;
        if (enemies.Count == 1) return enemies[0];
        return enemies.Aggregate((a,b) => (a.transform.position - point).sqrMagnitude < (b.transform.position - point).sqrMagnitude ? a : b);
    }

    public void NewRun(string level, int waves)
    {
        levelName = level;
        maxWaves = waves;
        wave = 1;
        enemiesSpawned = 0;
        enemiesDefeated = 0;
        resultMessage = "";
    }

    public void ClearEnemies()
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                UnityEngine.Object.Destroy(enemy);
            }
        }
        enemies.Clear();
    }

    private GameManager()
    {
        enemies = new List<GameObject>();
        state = GameState.PREGAME;
        wave = 1;
    }
}
