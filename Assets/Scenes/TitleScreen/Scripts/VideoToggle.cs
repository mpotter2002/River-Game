using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoToggle : MonoBehaviour
{
    [SerializeField] Image buttonImage;
    [SerializeField] Sprite pauseSprite, playSprite;
    public void TogglePausePlay()
    {
        VideoPlayer player = GetComponent<VideoPlayer>();
        
        if(player.isPlaying)
        {
            player.Pause();
            buttonImage.sprite = playSprite;
        }
        else if(player.isPaused)
        {
            player.Play();
            buttonImage.sprite = pauseSprite;
        }
    }
}
