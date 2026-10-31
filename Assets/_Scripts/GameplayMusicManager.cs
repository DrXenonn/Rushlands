using System.Collections;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class GameplayMusicManager : MonoBehaviour
{
    [SerializeField] private List<AudioClip> MusicTracks;

    [SerializeField] private AudioSource AudioSource;
    private readonly Queue<AudioClip> _playlist = new Queue<AudioClip>();

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);
        ShuffleAndQueueTracks();
        PlayNextTrack();
    }

    private void Update()
    {
        if (!AudioSource.isPlaying && _playlist.Count > 0)
        {
            PlayNextTrack();
        }
    }

    private void ShuffleAndQueueTracks()
    {
        List<AudioClip> shuffled = new List<AudioClip>(MusicTracks);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
        }

        _playlist.Clear();
        foreach (var clip in shuffled)
            _playlist.Enqueue(clip);
    }

    private void PlayNextTrack()
    {
        if (_playlist.Count == 0)
        {
            ShuffleAndQueueTracks();
        }

        if (_playlist.Count > 0)
        {
            AudioSource.clip = _playlist.Dequeue();
            AudioSource.Play();
        }
    }
}