using System;
using System.Collections;
using UnityEngine;

public class ObjectPoolVFX : MonoBehaviour
{
    public ObjectPooler Pool;
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private ParticleSystem _childParticle;
    private void OnEnable()
    {
        StartCoroutine(ParticleRoutine());
    }

    public void ChangeColor(Color color)
    {
        ParticleSystem.MainModule module = _particleSystem.main;
        ParticleSystem.MainModule childModule = _childParticle.main;
        module.startColor = color;
        childModule.startColor = color;
    }

    private IEnumerator ParticleRoutine()
    {
        _particleSystem.Play();
        yield return new WaitForSeconds(_particleSystem.main.duration);
        Pool.Release(gameObject);
    }
}
