using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoRenderer : MonoBehaviour
{
    public RawImage RawImage;

    public void Play() => StartCoroutine(playVideo());
    public void Stop() => StopAllCoroutines();
    IEnumerator playVideo()
    {
        RawImage.enabled = false;
        var videoPlayer = gameObject.GetComponent<VideoPlayer>();
        while (!videoPlayer.isPrepared) yield return null; //Wait until video is prepared
        (RawImage ?? gameObject.AddComponent<RawImage>()).texture = videoPlayer.texture; //Assign the Texture from Video to RawImage to be displayed
        RawImage.enabled = true;
        videoPlayer.Play(); //Play Video
        while (videoPlayer.isPlaying) yield return null;
    }
}
