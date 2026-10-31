using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class Settings : MonoBehaviour
{
    [Header("Toggles")]
    [SerializeField] private Toggle VSync, WindowedMode;

    [Header("Sliders")]
    [SerializeField] private Slider MusicSlider, SFXSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer MusicMixer, SfxMixer;

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        if (PlayerPrefs.HasKey("MUSIC_VOLUME"))
        {
            var musicVol = PlayerPrefs.GetFloat("MUSIC_VOLUME");
            MusicSlider.value = musicVol;
            SetMusicVolume(musicVol);
        }

        if (PlayerPrefs.HasKey("SFX_VOLUME"))
        {
            var sfxVol = PlayerPrefs.GetFloat("SFX_VOLUME");
            SFXSlider.value = sfxVol;
            SetSFXVolume(sfxVol);
        }
    }
    
    private void OnEnable()
    {
        if (PlayerPrefs.HasKey("VSYNC"))
            QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSYNC");

        VSync.isOn = QualitySettings.vSyncCount == 1;

        if (PlayerPrefs.HasKey("WINDOWED"))
        {
            var isWindowed = PlayerPrefs.GetInt("WINDOWED") == 1;
            WindowedMode.isOn = isWindowed;
            ToggleWindowedMode(isWindowed);
        }
    }

    public void ToggleVSync(bool state)
    {
        var s = state ? 1 : 0;
        QualitySettings.vSyncCount = s;
        PlayerPrefs.SetInt("VSYNC", s);
        PlayerPrefs.Save();
    }

    public void ToggleWindowedMode(bool state)
    {
        var mode = state ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
        Screen.fullScreenMode = mode;
        PlayerPrefs.SetInt("WINDOWED", state ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        var dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        MusicMixer.SetFloat("MusicVolume", dB);
        PlayerPrefs.SetFloat("MUSIC_VOLUME", value);
    }

    public void SetSFXVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20 + 14f;
        SfxMixer.SetFloat("SFXVolume", dB);
        PlayerPrefs.SetFloat("SFX_VOLUME", value);
    }

    public void ExitToMainMenu()
    {
        LeaveLobby();
    }
    
    private void LeaveLobby()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            ResetGameForEveryoneRpc();
            return;
        }
        
        SceneManager.LoadScene("MainMenu");
    }


    [Rpc(SendTo.Everyone)]
    public void ResetGameForEveryoneRpc()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
