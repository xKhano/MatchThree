using UnityEngine;
using UnityEngine.TextCore.LowLevel;

[CreateAssetMenu(fileName = "BoardConfig", menuName = "Scriptable Objects/BoardConfig")]
public class BoardConfig : ScriptableObject
{
    [field: SerializeField] public Sprite Red { get; private set; }
    [field: SerializeField] public Sprite Green { get; private set; }
    [field: SerializeField] public Sprite Blue { get; private set; }
    [field: SerializeField] public Sprite Yellow { get; private set; }
    [field: SerializeField] public Sprite RocketHorizontal { get; private set; }
    [field: SerializeField] public Sprite RocketVertical { get; private set; }
    [field: SerializeField] public Sprite Barrel { get; private set; }
    [field: SerializeField] public Sprite DiscoBall { get; private set; }

    public Sprite GetSprite(uint cell)
    {
        cell = cell & 0x000F;
        switch (cell)
        {
            case 1:
                return Red;
            case 2:
                return Green;
            case 3:
                return Blue;
            case 4:
                return Yellow;
            default:
                return null;
        }
    }
}
