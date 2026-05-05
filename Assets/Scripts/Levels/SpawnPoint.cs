using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public enum SpawnName
    {
        RED, GREEN, BONE
    }

    public SpawnName kind;

    // To match against location like "red", "green", "bone" in the JSON file

    public string kindString()
    {
        return kind.ToString().ToLower();       /// Return lowercase to compare with JSON file
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
