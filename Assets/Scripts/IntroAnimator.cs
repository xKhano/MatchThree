using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IntroAnimator : MonoBehaviour
{
    [SerializeField] private Board _board;
    [SerializeField] private Animator _animator;
    [SerializeField] private TMP_Text _text;
    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        StartAnimation();
    }

    public void StartAnimation()
    {
        UpdateText();
        _animator.Play("IntroAnimator");
    }

    private void UpdateText()
    {
        // _text.text = "Level " + _board.CurrentLevel.ID;
    }

    public void EndAnimation()
    {
        _board.enabled = true;
    }
}
