using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
public class OverlayAnimator : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private float fadeInDelay = 1f;
    [SerializeField] private float fadeInDuration = .5f;
    [SerializeField] private float fadeOutDelay = 1f;
    [SerializeField] private float fadeOutDuration = .5f;

    public void SetOverlay(bool active)
    {
        StartCoroutine(FadeCorotine(active));
    }
    public void SetOverlayInversed(bool active)
    {
        StartCoroutine(FadeCorotine(!active));
    }

    private IEnumerator FadeCorotine(bool active)
    {
        if (active)
        {
            _image.enabled = true;
            _image.CrossFadeAlpha(0f, 0f, true);
            yield return new WaitForSeconds(fadeInDelay);
            _image.CrossFadeAlpha(1f, fadeInDuration, true);
            yield return new WaitForSeconds(fadeInDuration);
        }
        else
        {
            _image.CrossFadeAlpha(1f, 0f, true);
            yield return new WaitForSeconds(fadeOutDelay);
            _image.CrossFadeAlpha(0f, fadeOutDuration, true);
            yield return new WaitForSeconds(fadeInDuration);
            _image.enabled = false;
        }
    }
}
