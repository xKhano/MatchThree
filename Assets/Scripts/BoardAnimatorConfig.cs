using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "Board Animator Config",menuName = "Scriptable Objects/Board Animator Config")]
    public class BoardAnimatorConfig : ScriptableObject
    {
        [field: SerializeField] public float CellSlideDuration { get; private set; } = .5f;
        [field: SerializeField] public float CellBlastDuration { get; private set; } = .25f;

    }
}