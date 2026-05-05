# CMPM 121 Assignment 2 Report

## Architecture Diagram

```mermaid
classDiagram
    GameManager --> EnemySpawner : tracks run state and enemies
    EnemySpawner --> LevelSelector : reads selected level
    LevelSelector --> Levels : stores levels.json entries
    Levels --> Spawn : contains enemy spawn rules
    EnemySpawner --> Enemy : reads enemies.json entries
    EnemySpawner --> SpawnPoint : chooses spawn location
    EnemySpawner --> EnemyController : assigns hp speed damage
    EnemyController --> Hittable : takes damage and dies
    PlayerController --> GameManager : reports player death
    RewardScreenManager --> GameManager : shows continue or return button
    WaveLabelController --> GameManager : shows wave and result text

    class GameManager {
        +int wave
        +int maxWaves
        +string levelName
        +string resultMessage
        +int enemiesSpawned
        +int enemiesDefeated
        +void NewRun(string level, int waves)
        +void ClearEnemies()
    }

    class RewardScreenManager {
        +GameObject rewardUI
        +private TextMeshProUGUI buttonText
        +private TextMeshProUGUI messageText
        +void Start()
        +void Update()
        +void GetMessage()
    }

    class WaveLabelController {
        +TextMeshProUGUI tmp
        +void Start()
        +void Update()
    }

    class PlayerController {
        +private Coroutine manaRoutine
    }

    class EnemySpawner {
        +Dictionary<string, int> dict
        +private List<Enemy> enemyConfig
        +private bool spawning
        +void ReturnToStart()
        +Enemy FindEnemy(string name)
        +SpawnPoint GetSpawnPoint(string location)
        -private SpawnPoint ChooseSpawnPoint(string location)
        -private bool SpawnLocationMatches(SpawnPoint spawnPoint, string location)
        +void SpawnSingleEnemy(Spawn mob, Enemy mobEntity)
        +IEnumerator SpawnEnemies(Spawn mob)
    }

    class Enemy {
        +string name
        +int sprite
        +int hp
        +int speed
        +int damage
    }

    class LevelSelector {
        +string Difficulty
        +List<Levels> levelConfig
        +private static LevelSelector theInstance
        +LevelSelector Instance
        +void ChangeLevel()
        +Levels GetLevel(string name)
        +List<Spawn> GetSpawn(string name)
        +private LevelSelector()
    }

    class Levels {
        +string name
        +int waves
        +List<Spawn> spawns
    }

    class Spawn {
        +string enemy
        +string count
        +string hp
        +string speed
        +string damage
        +string delay
        +int[] sequence
        +string location
    }

    class SpawnPoint {
        +string kindString()
    }

    class EnemyController {
        +private Unit unit
        +private Vector3 last_position
        +private float stuck_time
        +private int turn_direction
        +Vector2 PickMoveDirection(Vector2 direction)
        +void TrackStuck()
    }

```

## Architecture Description

The level and enemy data are loaded from JSON into small data classes. `LevelSelector` stores the available level definitions, while `EnemySpawner` reads enemy definitions and uses the selected level's spawn rules to create enemies each wave. Spawn rules can choose enemy type, count, HP, damage, delay, sequence, and spawn location.

`GameManager` stores the current game state, wave number, enemy count, and simple run stats. `EnemyController` handles enemy movement and attacks, while `PlayerController` starts the player stats and reports game over when the player dies. `RewardScreenManager` reuses the existing reward screen button for either continuing to the next wave or returning to the start after victory or defeat.

## Added Classes And Methods

- Added classes `Levels` and `Spawn` for storing levels.json into a new class: `LevelSelector`.
- Added class `Enemy` for storing enemies.json.
- Added fields to `Enemy` for JSON enemy stats.
- Added `speed` and `damage` fields to `Spawn`.
- Added `LevelSelector.GetLevel`.
- Added wave/end-state fields and helper methods in `GameManager`.
- Added data-driven spawn helpers in `EnemySpawner`.
- Added enemy `damage` support and movement stopping in `EnemyController`.
- Updated `RewardScreenManager` and `WaveLabelController` to display wave, victory, and defeat states.
- Added two new enemy types: "medusa" (in Endless mode) and "ghost" (in Medium difficulty) to enemies.json and levels.json.

## Contributions

Todd Crandell fixed bugs and enemy movement behavior, including stopping enemies correctly while attacking and using configured enemy damage.

Branson Guan worked on the initial assignment implementation, including the level selection flow, level JSON structure, and wave spawning setup.

Saurav Shah worked on the initial assignment implementation, including enemy JSON data, enemy type setup, additional enemy types, and spawn rule integration.
