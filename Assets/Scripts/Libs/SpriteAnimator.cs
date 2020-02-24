using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Image))]
public class SpriteAnimator : MonoBehaviour
{
    public int fps;
    public UnityEngine.AddressableAssets.AssetReference SpriteSheet;
    Sprite[] frames;

    Image spriteRenderer;

    public bool playing { get; private set; }
    public int currentFrame { get; private set; }
    public bool loop { get; private set; }

    void Awake()
    {
        spriteRenderer = GetComponent<Image>();
        spriteRenderer.enabled = false;
        SpriteSheet.LoadAssetAsync<IList<Sprite>>().Completed += (a) =>
        {
            frames = a.Result?.ToArray();
            if (gameObject.activeInHierarchy) Play();
            spriteRenderer.enabled = true;
        };
    }
    void OnEnable() { Play(); }
    void OnDisable() { playing = false; }

    public void Play(bool loop = true, int startFrame = 0)
    {
        if (!playing) ForcePlay(loop, startFrame);
        else Debug.LogWarning("could not find animation");
    }

    public void ForcePlay(bool loop = true, int startFrame = 0)
    {
        if (frames != null)
        {
            this.loop = loop;
            playing = true;
            currentFrame = startFrame;
            spriteRenderer.sprite = frames[currentFrame];
            StopAllCoroutines();
            StartCoroutine(PlayAnimation());
        }
    }

    IEnumerator PlayAnimation()
    {
        float timer = 0f;
        float delay = 1f / fps;
        while (loop || currentFrame < frames.Length - 1)
        {

            while (timer < delay)
            {
                timer += Time.deltaTime;
                yield return 0f;
            }
            while (timer > delay)
            {
                timer -= delay;
                NextFrame();
            }

            spriteRenderer.sprite = frames[currentFrame];
        }
    }

    void NextFrame()
    {
        currentFrame++;
        if (currentFrame >= frames.Length)
        {
            if (loop) currentFrame = 0;
            else currentFrame = frames.Length - 1;
        }
    }
}