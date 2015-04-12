using UnityEngine;
using System.Collections;

public enum SoundEffects { ObstacleCollisionSoundEffect, CoinSoundEffect, FuelSoundEffect, GameOverSoundEffect, GUITapSoundEffect }
public class AudioController : MonoBehaviour
{
	static public AudioController instance;
	
	public AudioClip backgroundMusic;
	public AudioClip obstacleCollision;
	public AudioClip coinCollection;
	public AudioClip fuelCollection;
	public AudioClip gameOver;
	public AudioClip guiTap;
	
	public float backgroundMusicVolume;
	public float soundEffectsVolume;
	
	private AudioSource backgroundAudio;
	// use multiple sound effects audo sources so more than one sound effect can be played at the same time
	private AudioSource[] soundEffectsAudio;
	private int nextSoundEffectsAudioIndex = 0;
	
	public void Awake()
	{
		instance = this;
	}
	
	public void OnEnable()
	{
		AudioSource[] sources = Camera.main.GetComponents<AudioSource>();
		backgroundAudio = sources[0];
		soundEffectsAudio = new AudioSource[2];
		soundEffectsAudio[0] = sources[1];
		soundEffectsAudio[1] = sources[2];
		
		backgroundAudio.clip = backgroundMusic;
		backgroundAudio.loop = true;
		backgroundAudio.volume = Mathf.Clamp01(backgroundMusicVolume);
		
		soundEffectsAudio[0].volume = Mathf.Clamp01(soundEffectsVolume);
		soundEffectsAudio[1].volume = Mathf.Clamp01(soundEffectsVolume);
	}
	
	public void PlayBackgroundMusic(bool play)
	{
		if (play) {
			backgroundAudio.Play();
		} else {
			backgroundAudio.Pause();
		}
	}
	
	public void PlaySoundEffect(SoundEffects soundEffect)
	{
		AudioClip clip = null;
		float pitch = 1;
		switch (soundEffect) {
		case SoundEffects.ObstacleCollisionSoundEffect:
			clip = obstacleCollision;
			break;
			
		case SoundEffects.CoinSoundEffect:
			clip = coinCollection;
			pitch = 1.5f;
			break;
			
		case SoundEffects.FuelSoundEffect:
			clip = fuelCollection;
			break;
			
		case SoundEffects.GameOverSoundEffect:
			clip = gameOver;
			break;
			
		case SoundEffects.GUITapSoundEffect:
			clip = guiTap;
			break;
		}
		
		soundEffectsAudio[nextSoundEffectsAudioIndex].pitch = pitch;
		soundEffectsAudio[nextSoundEffectsAudioIndex].clip = clip;
		soundEffectsAudio[nextSoundEffectsAudioIndex].Play();
		nextSoundEffectsAudioIndex = (nextSoundEffectsAudioIndex + 1) % soundEffectsAudio.Length;
	}
}
