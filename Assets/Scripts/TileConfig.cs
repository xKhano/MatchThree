using UnityEngine;

[CreateAssetMenu(fileName = "TileConfig", menuName = "Scriptable Objects/Tile Config")]
public class TileConfig : ScriptableObject
{
    [field: SerializeField] public Sprite Sprite { get; private set; } = null;
    [field: SerializeField] public string Name { get; private set; } = "Tile";
    [field: SerializeField] public RuntimeAnimatorController AnimatorController { get; private set; } = null;
    [field: SerializeField] public uint StartHealth { get; private set; } = 1;
    [field: SerializeField] public bool Fallable { get; private set; } = false;
    [field: SerializeField] public bool Interactable { get; private set; } = false;

    [field: SerializeField]
    public Color ReplacementColor { get; private set; } = Color.white;
    [field: SerializeField,] public bool Moveable { get; private set; } = true;
    [field: SerializeField] public GameObject BlastVFXPrefab { get; private set; } = null;

    [SerializeField] private BlastPattern[] _blastPatterns;
    public BlastPattern[] BlastPatterns => _blastPatterns;

    [SerializeField] private MatchRule[] _matchRules;
    public MatchRule[] MatchRules => _matchRules;
}