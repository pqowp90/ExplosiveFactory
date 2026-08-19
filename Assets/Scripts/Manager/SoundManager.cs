using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SoundData
{
	public string key;       // 사운드를 구분할 키 (예: "Jump", "Explosion")
	public AudioClip clip;
}

[SingletonLifeTime(LifeTime.Application)]
public class SoundManager : MonoSingleton<SoundManager>
{
	[Header("BGM Audio Source")]
	[SerializeField] private AudioSource bgmSource;

	[Header("SFX Audio Source (Pool)")]
	[SerializeField] private List<AudioSource> sfxSources = new();

	[Header("BGM & SFX Clips")]
	[SerializeField] private List<SoundData> bgmClips = new();
	[SerializeField] private List<SoundData> sfxClips = new();

	private readonly Dictionary<string, AudioClip> _bgmDict = new();
	private readonly Dictionary<string, AudioClip> _sfxDict = new();

	protected override void Awake()
	{
		base.Awake();
		InitializeAudioSources();
		InitializeDictionaries();
	}

	private void InitializeAudioSources()
	{
		if (bgmSource == null)
		{
			bgmSource = gameObject.AddComponent<AudioSource>();
			bgmSource.playOnAwake = false;
			bgmSource.loop = true;
		}

		if (sfxSources == null || sfxSources.Count == 0)
		{
			sfxSources = new List<AudioSource>();
			for (int i = 0; i < 4; i++)
			{
				var src = gameObject.AddComponent<AudioSource>();
				src.playOnAwake = false;
				sfxSources.Add(src);
			}
		}
	}

	private void InitializeDictionaries()
	{
		_bgmDict.Clear();
		foreach (var data in bgmClips)
		{
			if (data != null && !string.IsNullOrEmpty(data.key) && data.clip != null)
			{
				_bgmDict[data.key] = data.clip;
			}
		}

		_sfxDict.Clear();
		foreach (var data in sfxClips)
		{
			if (data != null && !string.IsNullOrEmpty(data.key) && data.clip != null)
			{
				_sfxDict[data.key] = data.clip;
			}
		}
	}

	#region BGM Control
	public void PlayBGM(string bgmKey, float volume = 1f, bool loop = true)
	{
		if (!_bgmDict.TryGetValue(bgmKey, out var clip))
		{
			Debug.LogWarning($"[SoundManager] BGM Key '{bgmKey}' not found.");
			return;
		}
		bgmSource.clip = clip;
		bgmSource.volume = volume;
		bgmSource.loop = loop;
		bgmSource.Play();
	}

	public void StopBGM()
	{
		if (bgmSource != null)
		{
			bgmSource.Stop();
		}
	}
	#endregion

	#region SFX Control
	public void PlaySFX(string sfxKey, float volume = 1f)
	{
		if (!_sfxDict.TryGetValue(sfxKey, out var clip))
		{
			Debug.LogWarning($"[SoundManager] SFX Key '{sfxKey}' not found.");
			return;
		}

		AudioSource source = GetAvailableSFXSource();
		source.spatialBlend = 0f;
		source.PlayOneShot(clip, volume);
	}

	public void Play3DSound(string key, Vector3 position, float volume = 1f)
	{
		if (!_sfxDict.TryGetValue(key, out var clip))
		{
			Debug.LogWarning($"[SoundManager] SFX Key '{key}' not found.");
			return;
		}

		AudioSource.PlayClipAtPoint(clip, position, volume);
	}

	private AudioSource GetAvailableSFXSource()
	{
		foreach (var src in sfxSources)
		{
			if (src != null && !src.isPlaying)
			{
				return src;
			}
		}
		var newSource = gameObject.AddComponent<AudioSource>();
		newSource.playOnAwake = false;
		sfxSources.Add(newSource);
		return newSource;
	}
	#endregion
}