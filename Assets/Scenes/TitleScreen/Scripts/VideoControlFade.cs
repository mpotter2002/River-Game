using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VideoControlFade : MonoBehaviour
{
    [SerializeField] float fadeTime, timeUntilFade;
    [SerializeField] Image pausePlayButton, sliderBody, sliderFill, sliderHandle;
    [SerializeField] Button backButton;
    [SerializeField] GameObject videoScreen;
    [SerializeField] GameObject homeScreen;
    private float currentTime;
    bool controlsActive = true;

    public void Refresh()
    {
        if(gameObject.activeSelf)
            StartCoroutine(FadeIn());
        currentTime = 0;
    }

    private void Update()
    {
        if(controlsActive && currentTime >= timeUntilFade)
        {
            StartCoroutine(FadeOut());
        }

        currentTime += Time.deltaTime;
    }

    private IEnumerator FadeIn()
    {
        pausePlayButton.enabled = true;
        sliderBody.enabled = true;
        sliderHandle.enabled = true;
        sliderFill.enabled = true;
        if (backButton != null) backButton.gameObject.SetActive(true);

        controlsActive = true;

        float alpha = pausePlayButton.color.a;
        float elapsedTime = 0;

        while (alpha < 1)
        {
            float changeAmount = elapsedTime / fadeTime;
            alpha += changeAmount;

            SetAlpha(alpha);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

    }

    private IEnumerator FadeOut()
    {
        float alpha = 1;
        float elapsedTime = 0;

        controlsActive = false;

        while (alpha > 0)
        {
            float changeAmount = elapsedTime / fadeTime;
            alpha = 1 - changeAmount;

            SetAlpha(alpha);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        pausePlayButton.enabled = false;
        sliderBody.enabled = false;
        sliderHandle.enabled = false;
        sliderFill.enabled = false;
        if (backButton != null) backButton.gameObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        pausePlayButton.color = new Color(pausePlayButton.color.r, pausePlayButton.color.g, pausePlayButton.color.b, alpha);
        sliderBody.color = new Color(sliderBody.color.r, sliderBody.color.g, sliderBody.color.b, alpha);
        sliderHandle.color = new Color(sliderHandle.color.r, sliderHandle.color.g, sliderHandle.color.b, alpha);
        sliderFill.color = new Color(sliderFill.color.r, sliderFill.color.g, sliderFill.color.b, alpha);
    }

    public void OnBackButtonClicked()
    {
        Debug.Log("OnBackButtonClicked called");
        if (videoScreen != null)
        {
            Debug.Log("Deactivating videoScreen GameObject");
            videoScreen.SetActive(false);
        }
        Debug.Log("Loading TitleScreen scene");
        SceneManager.LoadScene("TitleScreen");
    }
}
