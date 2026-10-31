using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance { get; private set; }
    public AudioSource AudioSourcePrefab;

    private ObjectPool<AudioSource> _pool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _pool = new ObjectPool<AudioSource>(
            () =>
            {
                var obj = Instantiate(AudioSourcePrefab);
                return obj;
            },
            source =>
            {
                source.gameObject.SetActive(true);
            },
            source =>
            {
                source.Stop();
                source.gameObject.SetActive(false);
            },
            source => Destroy(source.gameObject),
            true, 15, 25
        );
    }

    public void PlaySfx(SfxData sfxData, Vector3 worldPosition)
    {
        if (sfxData == null) return;
        var source = _pool.Get();
        source.clip = sfxData.Clip;
        source.volume = sfxData.Volume;
        var pitch = Random.Range(sfxData.MinPitch, sfxData.MaxPitch);
        source.pitch = pitch;
        source.transform.position = worldPosition;
        var blend = sfxData._3D ? 1 : 0;
        source.spatialBlend = blend;
        source.minDistance = sfxData.MinDistance;
        source.maxDistance = sfxData.MaxDistance;
        source.Play();
        StartCoroutine(PoolReleaseCoroutine(source));
    }

    private IEnumerator PoolReleaseCoroutine(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length + 0.25f);
        _pool.Release(source);
    }
}