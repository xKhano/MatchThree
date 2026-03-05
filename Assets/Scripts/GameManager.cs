using Sirenix.OdinInspector;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [field:SerializeField] public GameConfig GameConfig { get; private set; }
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
}
[CreateAssetMenu(fileName = "Game Configuration", menuName = "Config/Game Config")]
public sealed class GameConfig : SerializedScriptableObject
{
    [field:SerializeField] public TileDatabase TileDB { get; private set; }
    [field:SerializeField] public LevelDatabase LevelDB { get; private set; }
}

[CreateAssetMenu(fileName = "Level Database", menuName = "Database/Level Database")]
public sealed class LevelDatabase : SerializedScriptableObject
{
    [field: SerializeField] private LevelData Levels;
}
[CreateAssetMenu(fileName = "Level Data", menuName = "Data/Level Data")]
internal class LevelData : ScriptableObject
{
    
}
