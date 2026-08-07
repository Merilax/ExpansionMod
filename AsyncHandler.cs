using System.IO;
using BepInEx;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ExpansionMod;

public class AsyncHandler : MonoBehaviour
{
	public AsyncOperationHandle handle;
	public bool execute = false;

	public delegate void OnLoadComplete();
	public event OnLoadComplete OnLoadCompleteEvent;
	public delegate void OnLoadFailed();
	public event OnLoadFailed OnLoadFailedEvent;

	public void AddHandle(AsyncOperationHandle handle)
	{
		this.handle = handle;
	}

	public void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	public void Update()
	{
		if (!execute || handle == null) return;
		if (handle.IsDone)
		{
			execute = false;
			if (handle.Status == AsyncOperationStatus.Succeeded)
				OnLoadCompleteEvent();
			else
				OnLoadFailedEvent();
			// Destroy(this);
		}
	}

	public static AsyncHandler LoadAsync<T>(string key)
	{
		// Plugin.LogInfo($"LoadAsync: {key} of type {typeof(T)}");
		GameObject asyncHandler = new("ExpansionMod AsyncHandler");
		AsyncHandler handler = asyncHandler.AddComponent<AsyncHandler>();

		AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(key);
		handler.AddHandle(handle);
		handler.execute = true;
		return handler;
	}
}
