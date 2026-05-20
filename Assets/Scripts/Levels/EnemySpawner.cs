using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;


public class Enemy
{
    public string name { get; set; }
    public int sprite { get; set; }
    public int hp { get; set; }
    public int speed { get; set; }
    public int damage { get; set; }
}

public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;
    public Dictionary<string, int> dict;
    private List<Enemy> enemyConfig;
    private bool spawning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int y = 40;
        int i = 0;
        foreach(Levels level in LevelSelector.Instance.levelConfig)
        {
            GameObject selector = Instantiate(button, level_selector.transform);
            selector.transform.localPosition = new Vector3(0, y-i);
            selector.GetComponent<MenuSelectorController>().spawner = this;
            selector.GetComponent<MenuSelectorController>().SetLevel(level.name);
            i += 40;
        }
        dict = new Dictionary<string, int>();
        dict.TryAdd("wave", 1); //initialize dict vars
        dict.TryAdd("base", 5);
        //store enemy info
        string enemyData = File.ReadAllText("./Assets/Resources/enemies.json");
        enemyConfig = JsonConvert.DeserializeObject<List<Enemy>>(enemyData);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartLevel(string levelname)
    {
        StopAllCoroutines();
        spawning = false;
        GameManager.Instance.ClearEnemies();
        level_selector.gameObject.SetActive(false);
        LevelSelector.Instance.Difficulty = levelname;
        
        Levels level = LevelSelector.Instance.GetLevel(levelname);
        GameManager.Instance.NewRun(levelname, level == null ? 0 : level.waves);
        // this is not nice: we should not have to be required to tell the player directly that the level is starting
        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        dict["wave"] = 1;
        
        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            GameObject playerObject = GameManager.Instance.player;
            PlayerController player = playerObject == null ? null : playerObject.GetComponent<PlayerController>();
            if (player != null && GameManager.Instance.pendingRelicRewards != null)
            {
                player.AddRelic(GameManager.Instance.GetSelectedRelicReward());
                GameManager.Instance.ClearPendingRelicRewards();
            }
            else if (player != null && GameManager.Instance.pendingSpellReward != null)
            {
                player.EquipSpell(GameManager.Instance.pendingSpellReward);
                GameManager.Instance.ClearPendingSpellReward();
            }

            StartCoroutine(SpawnWave());
            return;
        }

        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER ||
            GameManager.Instance.state == GameManager.GameState.VICTORY)
        {
            ReturnToStart();
        }
    }

    public void ReturnToStart()
    {
        StopAllCoroutines();
        spawning = false;
        GameManager.Instance.ClearEnemies();
        GameManager.Instance.state = GameManager.GameState.PREGAME;
        GameManager.Instance.resultMessage = "";
        GameManager.Instance.ClearPendingSpellReward();
        GameManager.Instance.ClearPendingRelicRewards();
        level_selector.gameObject.SetActive(true);
    }

    IEnumerator SpawnWave()
    {
        if (spawning) yield break;
        spawning = true;

        GameManager.Instance.wave = dict["wave"]; //update gamemanager
        PlayerController player = GameManager.Instance.player.GetComponent<PlayerController>();
        if (player != null)
        {
            player.ApplyWaveStats();
        }
        EventBus.Instance.DoWaveStarted(GameManager.Instance.wave);

        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        GameManager.Instance.countdown = 3;
        for (int i = 3; i > 0; i--)
        {
            yield return new WaitForSeconds(1);
            GameManager.Instance.countdown--;
        }
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        List<Spawn> spawns = LevelSelector.Instance.GetSpawn(LevelSelector.Instance.Difficulty);
        if(spawns != null)
        {
            foreach (Spawn mob in spawns)
            {
                if (GameManager.Instance.state != GameManager.GameState.INWAVE) break;
                yield return SpawnEnemies(mob);
            }
        }
        
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0 &&
                                        GameManager.Instance.state == GameManager.GameState.INWAVE);
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            spawning = false;
            yield break;
        }

        Levels lvl = LevelSelector.Instance.GetLevel(LevelSelector.Instance.Difficulty);
        EventBus.Instance.DoWaveCompleted(dict["wave"]);
        if (lvl != null && lvl.waves > 0 && dict["wave"] >= lvl.waves)
        {
            GameManager.Instance.resultMessage = "You cleared " + lvl.name + "!";
            GameManager.Instance.state = GameManager.GameState.VICTORY;
        }
        else
        {
            GameManager.Instance.state = GameManager.GameState.WAVEEND;
            dict["wave"]++;
        }
        spawning = false;
    }


    // find the right enemy data by name
    Enemy FindEnemy(string name)
    {
        foreach (Enemy e in enemyConfig)
        {
            if (e.name == name) return e;
        }
        return null;
    }

    // pick a spawn point based on the location string from json
    SpawnPoint GetSpawnPoint(string location)
    {
        if (location == null || location == "random")
        {
            // any random spawn point
            return SpawnPoints[Random.Range(0, SpawnPoints.Length)];
        }

        // "random red" means pick a random one from the red spawn points
        // split it and get the type part
        string[] parts = location.Split(' ');
        if (parts.Length >= 2)
        {
            string type = parts[1];
            // find all matching spawn points
            List<SpawnPoint> matching = new List<SpawnPoint>();
            foreach (SpawnPoint sp in SpawnPoints)
            {
                if (sp.kindString() == type)
                {
                    matching.Add(sp);
                }
            }
            if (matching.Count > 0)
            {
                return matching[Random.Range(0, matching.Count)];
            }
        }

        // fallback to random if nothing matched
        return SpawnPoints[Random.Range(0, SpawnPoints.Length)];
    }
    
    private SpawnPoint ChooseSpawnPoint(string location)
    {
        if (SpawnPoints == null || SpawnPoints.Length == 0) return null;

        List<SpawnPoint> choices = new List<SpawnPoint>();
        foreach (SpawnPoint spawnPoint in SpawnPoints)
        {
            if (SpawnLocationMatches(spawnPoint, location))
            {
                choices.Add(spawnPoint);
            }
        }
        if (choices.Count == 0) choices.AddRange(SpawnPoints);
        return choices[Random.Range(0, choices.Count)];
    }

    private bool SpawnLocationMatches(SpawnPoint spawnPoint, string location)
    {
        if (string.IsNullOrWhiteSpace(location)) return true;

        string lower = location.ToLowerInvariant();
        bool wantsRed = lower.Contains("red");
        bool wantsGreen = lower.Contains("green");
        bool wantsBone = lower.Contains("bone");

        if (!wantsRed && !wantsGreen && !wantsBone) return true;
        if (wantsRed && spawnPoint.kind == SpawnPoint.SpawnName.RED) return true;
        if (wantsGreen && spawnPoint.kind == SpawnPoint.SpawnName.GREEN) return true;
        if (wantsBone && spawnPoint.kind == SpawnPoint.SpawnName.BONE) return true;
        return false;
    }

    // spawn a single enemy with the right stats
    void SpawnSingleEnemy(Spawn mob, Enemy mobEntity)
    {
        if (GameManager.Instance.state != GameManager.GameState.INWAVE) return;

        SpawnPoint spawn_point = ChooseSpawnPoint(mob.location);
        if (spawn_point == null)
        {
            Debug.LogWarning("No spawn points are available.");
            return;
        }

        Vector2 offset = Random.insideUnitCircle * 1.8f;
        Vector3 initial_position = spawn_point.transform.position + new Vector3(offset.x, offset.y, 0);

        GameObject new_enemy = Instantiate(enemy, initial_position, Quaternion.identity);
        new_enemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(mobEntity.sprite);

        EnemyController en = new_enemy.GetComponent<EnemyController>();

        // evaluate hp using rpn, default to "base" if not specified
        dict["base"] = mobEntity.hp;
        int mobHP = RPNEvaluator.RPNEvaluator.Evaluate(mob.hp ?? "base", dict);

        // evaluate speed
        dict["base"] = mobEntity.speed;
        int mobSpeed = RPNEvaluator.RPNEvaluator.Evaluate(mob.speed ?? "base", dict);

        // evaluate damage
        dict["base"] = mobEntity.damage;
        int mobDamage = RPNEvaluator.RPNEvaluator.Evaluate(mob.damage ?? "base", dict);

        en.hp = new Hittable(mobHP, Hittable.Team.MONSTERS, new_enemy);
        en.speed = mobSpeed;
        en.attackDamage = mobDamage;

        GameManager.Instance.AddEnemy(new_enemy);
    }

    // spawn all enemies of one type using the sequence pattern
    IEnumerator SpawnEnemies(Spawn mob)
    {
        Enemy mobEntity = FindEnemy(mob.enemy);
        if (mobEntity == null)
        {
            Debug.LogWarning("Could not find enemy named " + mob.enemy);
            yield break;
        }

        int spawnCount = RPNEvaluator.RPNEvaluator.Evaluate(mob.count ?? "0", dict);
        if (spawnCount <= 0) yield break;

        // get delay, default is 2
        dict["base"] = 2;
        float delayTime = RPNEvaluator.RPNEvaluator.Evaluatef(mob.delay ?? "base", dict);

        // sequence defaults to [1] if not set
        int[] seq = mob.sequence;
        if (seq == null || seq.Length == 0)
        {
            seq = new int[] { 1 };
        }

        int spawned = 0;
        int seqIndex = 0;

        while (spawned < spawnCount && GameManager.Instance.state == GameManager.GameState.INWAVE)
        {
            // how many to spawn in this group
            int groupSize = seq[seqIndex % seq.Length];

            // dont spawn more than whats left
            if (spawned + groupSize > spawnCount)
                groupSize = spawnCount - spawned;

            // spawn the group
            for (int j = 0; j < groupSize; j++)
            {
                if (GameManager.Instance.state != GameManager.GameState.INWAVE) break;
                SpawnSingleEnemy(mob, mobEntity);
                spawned++;
            }

            seqIndex++;

            // wait between groups if theres more to spawn
            if (spawned < spawnCount)
            {
                yield return new WaitForSeconds(delayTime);
            }
        }
    }
}
