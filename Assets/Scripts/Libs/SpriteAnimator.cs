using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpriteAnimator : MonoBehaviour
{
    public int fps;
    //public UnityEngine.AddressableAssets.AssetReference SpriteSheet;
    Sprite[] frames;

    Image spriteRenderer;

    public bool playOnEnable = true;
    public bool playing { get; private set; }
    public int currentFrame { get; private set; }
    public bool loop { get; private set; }

    bool loaded;
    void Awake()
    {
        spriteRenderer = GetComponent<Image>();
        spriteRenderer.enabled = false;
        /*SpriteSheet.LoadAssetAsync<IList<Sprite>>().Completed += (a) =>
        {
            frames = a.Result?.ToArray();
            loaded = true;
            if (gameObject.activeInHierarchy & playOnEnable) Play();
        };*/
    }
    void OnEnable() { if (playOnEnable) Play(); }
    void OnDisable() { Stop(); }

    public void Play(bool loop = true, int startFrame = 0)
    {
        if (loaded) play();
        else StartCoroutine(WaitForLoaded());
        IEnumerator WaitForLoaded()
        {
            yield return new WaitUntil(() => loaded);
            play();
        }

        void play()
        {
            if (!playing & frames != null)
            {
                this.loop = loop;
                playing = true;
                currentFrame = startFrame;
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = frames[currentFrame];
                StopAllCoroutines();
                StartCoroutine(PlayAnimation());
            }
        }
    }
    public void Stop()
    {
        playing = false;
        StopAllCoroutines();
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