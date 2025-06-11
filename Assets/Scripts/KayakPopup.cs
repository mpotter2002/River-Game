using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KayakPopup : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private Image backgroundImage;
    [SerializeField] private float moveSpeed = 1f;
    private float currentLifetime;
    private float lifetime = 3f;

    [SerializeField] private float textFadeDelay = 0.5f;
    [SerializeField] private float textFadeDuration = 1.0f;

    [SerializeField] private float backgroundFadeDuration = 0.8f;

    public void SetDuration(float duration)
    {
        lifetime = duration;
        currentLifetime = duration;
    }

    private void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage = GetComponent<Image>();

        if (textMesh != null)
        {
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.enableWordWrapping = true;
            textMesh.raycastTarget = false;
        }

        currentLifetime = lifetime;
    }

    private void Update()
    {
        // Move downward
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;

        // Fade out background
        float bgAlpha = 1f;
        if (currentLifetime < backgroundFadeDuration)
        {
            bgAlpha = currentLifetime / backgroundFadeDuration;
        }
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = Mathf.Clamp01(bgAlpha);
            backgroundImage.color = bgColor;
        }

        // Fade out text (slower)
        if (textMesh != null)
        {
            float textAlpha = 1f;
            if (currentLifetime < textFadeDuration)
            {
                textAlpha = currentLifetime / textFadeDuration;
            }
            Color textColor = textMesh.color;
            textColor.a = Mathf.Clamp01(textAlpha);
            textMesh.color = textColor;
        }

        // Destroy when lifetime is up
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            Destroy(gameObject);
        }
    }
} 