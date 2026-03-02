using UnityEngine;

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
}
