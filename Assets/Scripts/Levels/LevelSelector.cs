using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

public class Levels
{
    public string name { get; set; }
    public int waves { get; set; }
    public List<Spawn> spawns { get; set; }
}

public class Spawn
{
    public string enemy { get; set; }
    public string count { get; set; }
    public string hp { get; set; }
    public string speed { get; set; }
    public string damage { get; set; }
    public string delay { get; set; }
    public int[] sequence { get; set; }
    public string location { get; set; }
}

public class LevelSelector
{
    public string Difficulty;
    public List<Levels> levelConfig;

    private static LevelSelector theInstance;
    public static LevelSelector Instance { get
        {
            if (theInstance == null)
                theInstance = new LevelSelector();
            return theInstance;
        }
    }
    public void ChangeLevel(string level)
    {
        Difficulty = level;
    }

    public Levels GetLevel(string name)
    {
        foreach (Levels level in levelConfig)
        {
            if (name == level.name) return level;
        }
        return null;
    }

    public List<Spawn> GetSpawn(string name)
    {
        Levels level = GetLevel(name);
        return level?.spawns;
    }

    private LevelSelector()
    {
        string levelData = File.ReadAllText("./Assets/Resources/levels.json");
        Difficulty = "Easy";
        levelConfig = JsonConvert.DeserializeObject<List<Levels>>(levelData);
    }
}
