using System;
using System.Collections;
using UnityEngine;

public class ObjectPoolVFX : MonoBehaviour
{
    public ObjectPooler Pool;
    [SerializeField] private ParticleSystem _particleSystem;

    private void OnEnable()
    {
        StartCoroutine(ParticeRoutine());
    }

    private IEnumerator ParticeRoutine()
    {
        _particleSystem.Play();
        yield return new WaitForSeconds(_particleSystem.main.duration);
        Pool.Release(gameObject);
    }
}
