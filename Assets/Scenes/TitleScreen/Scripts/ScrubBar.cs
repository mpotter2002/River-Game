using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ScrubBar : MonoBehaviour
{
    Slider slider;
    bool dragging = false;
    [SerializeField] VideoPlayer player;
    [SerializeField] GameObject screen;


    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void StartDrag()
    {
        dragging = true;
    }
    public void EndDrag()
    {
        dragging = false;
    }

    private void Update()
    {
        if(!dragging)
            slider.value = (float)(player.time / player.length);

        if(!dragging && slider.value >= 0.999f)
        {
            player.Stop();
            screen.SetActive(false);
        }
    }

    public void ScrubVideo()
    {
        float t = slider.value;
        float time = t * (float)player.length;

        if(dragging)
            player.time = time;
    }
}
