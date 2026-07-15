using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DG.Tweening;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("BREAK ARTS III.exe")]

public class Plugin : BasePlugin
{
	internal static new ManualLogSource Log;
	public static ConfigFile config;
	public override void Load()
	{
		Log = BepInEx.Logging.Logger.CreateLogSource("BossMod");
		var harmony = Harmony.CreateAndPatchAll(typeof(Main));
		harmony.PatchAll(typeof(UI));
	}
	public static void LogInfo(object data) => Log.LogInfo(data);
}

public class Main
{
	public static bool isLoadingModRace = false;
	public static bool isInModRace = false;
	public static bool isMatchInProgress = false;
	public static bool init = false;

	// DATA
	public static string[] filenames = ["Immortal", "Castigator"]; // FILE NAMES MUST BE EXACT, THIS DICTATES WHICH BOSSES WILL LOAD [!!!]
	public static string[] bossnames = new string[5]; // Automatic utility.

	// This is where the AI behaviour is set up.
	public static Dictionary<string, Dictionary<string, dynamic>> bossStats = new()
	{
		{ // Immortal
			filenames[0], new(){
				{"health", 1_250_000},
				{"shield", 10_000},
				{"weight", 8000},
				{"generation", 10_000_000},
				{"maxEnergy", 10_000_000},
				{"cooling", 10_000_000},
				{"maxHeat", 10_000_000},
				{"lockSpeed", 5000},
				{"lockRange", 3000},
				{"driveSpeed", .65f}, // 0-0.99, 1 bugs out sometimes.
				{"groundOffset", 0},
				{"killBonus", .15f}, // Starts from 0, translates to +X %
				{"delayedSpawn", false},
				{"delayedSpawnTime", 0},
				{"AI", new d_Enemy(){
					// Don't really know what the _Acts do.
					_ActA = d_Enemy.SpAct.randomLong,
					_ActB = d_Enemy.SpAct.randomLong,
					_ActC = d_Enemy.SpAct.none,
					_ActAType = d_Enemy.SpType.Continuous,
					_ActBType = d_Enemy.SpType.Continuous,
					_ActCType = d_Enemy.SpType.Continuous,
					_ActA_BtOr = d_Enemy.SpAct.none,
					_ActB_BtOr = d_Enemy.SpAct.none,
					_ActC_BtOr = d_Enemy.SpAct.none,
					_ActA_BtOr_Inv = false,
					_ActB_BtOr_Inv = false,
					_ActC_BtOr_Inv = false,
					_ActA_Inv = false,
					_ActB_Inv = false,
					_ActC_Inv = false,

					_Level = 10,
					_Type = d_Enemy.EnemyType.Battle,
					_AbleOnWater = true, // Dunno. Probably avoid / ignore water.
					_Aerial = 0f, // 0-1 Prioritize jumping
					_Aggressive = .8f, // 0-1 Prioritize combat
					_Accuracy = .5f, // 0-1 Fire whenever / prioritize accurate shots.
					_Tracking = .1f, // 0-1 Objective awareness, or patience. Lower = Stubborn. Doesn't seem to do much in Combat mode.
					_Elusion = .7f, // 0-1 Prioritize beelining / dodging and circling movement.
					_Brave = .1f, // 0-1 Prioritize survival.
					_ThrustPriority = .6f, // 0.5-1.5 Detonator to Thruster usage ratio.
					_WallDodgePriority = .1f, // 0-2 Obstacle dodging priority (race).
					_InFight = .1f, // 0-1 Melee chance. Don't think it does anything if they can't kick or burst.
					_TargetRange = 100, // Optimal disance from target during combat, in metres.
					_Random = .1f, // 0-0.5 Messes around with the AI brain. Most AIs seem to be set to .1
					_SeparateType = d_Enemy.SeparateType.Random, // Presumably either Separator, or more likely, Diffractor usage.
					_NoLockTarget = true, // true avoids AIs from killing eachother (but they still lock, which I fixed elsewhere).
					_OnlyLockPlayer = false, // Bugged, do not change.
					_FollowTarget = "", // This makes AIs follow another. See "Empress" and her "Pawn"s.
				}},
			}
		},
		{ // Castigator
			filenames[1], new(){
				{"health", 650_000},
				{"shield", 10_000},
				{"weight", 8000},
				{"generation", 10_000_000},
				{"maxEnergy", 10_000_000},
				{"cooling", 10_000_000},
				{"maxHeat", 10_000_000},
				{"lockSpeed", 5000},
				{"lockRange", 3000},
				{"driveSpeed", .65f},
				{"groundOffset", 70},
				{"killBonus", .10f},
				{"delayedSpawn", true},
				{"delayedSpawnTime", 50},
				{"AI", new d_Enemy(){
					_ActA = d_Enemy.SpAct.none,
					_ActB = d_Enemy.SpAct.randomLong,
					_ActC = d_Enemy.SpAct.none,
					_ActAType = d_Enemy.SpType.Continuous,
					_ActBType = d_Enemy.SpType.Continuous,
					_ActCType = d_Enemy.SpType.Continuous,
					_ActA_BtOr = d_Enemy.SpAct.none,
					_ActB_BtOr = d_Enemy.SpAct.none,
					_ActC_BtOr = d_Enemy.SpAct.none,
					_ActA_BtOr_Inv = false,
					_ActB_BtOr_Inv = false,
					_ActC_BtOr_Inv = false,
					_ActA_Inv = false,
					_ActB_Inv = false,
					_ActC_Inv = false,

					_Level = 10,
					_Type = d_Enemy.EnemyType.Battle,
					_AbleOnWater = true,
					_Aerial = 0f, // Prioritize hopping
					_Aggressive = .6f, // 0-1 Prioritize combat
					_Accuracy = .8f, // 0-1 Prioritize firing if accurate
					_Tracking = .1f, // 0-1 Objective awareness, or patience. Lower = Stubborn
					_Elusion = .7f, // 0-1 Increased dodgy movement
					_Brave = .1f, // 0-1 Prioritize survival
					_ThrustPriority = .8f, // 0.5-1.5 Thruster usage
					_WallDodgePriority = .1f, // 0-2 Dodging (race)
					_InFight = .1f, // 0-1 Melee chance
					_TargetRange = 200, // Optimal disance from target during combat, in metres
					_Random = .1f, // 0-0.5
					_SeparateType = d_Enemy.SeparateType.Random,
					_NoLockTarget = true,
					_OnlyLockPlayer = false,
					_FollowTarget = "",
				}},
			}
		},
	};

	// This is the custom GP config.
	public static GrandprixDef AddModdedGP()
	{
		GrandprixDef gpdef = new()
		{
			_Difficulty = 10, // Difficulty GP <10>-4
			_ID = 4, // Level number GP 10-<4>
			_Name = "BOSSFIGHT EVENT", // Match name
			_EnemyCodes = filenames,
			_EnemyLevels = new([10, 10]), // Always match the amount of entries.
			_EnemyLevels_Hard = new([10, 10]), // And copy everything from normal mode to hard mode.
			_Stages = new([new CustomMatchData.MatchData(){ // Copy this to _Stages_Hard too.
				_Energy = VenueData.Energy.High,
				_Grav = VenueData.Grav.Mid,
				_IsSet = true,
				_Land = VenueData.Landscape.Cyber,
				_Layout = VenueData.VenueLayout.None,
				_Place = SinglePlayData.MatchPlace.Cyber_Battle,
				_Temp = VenueData.Temperature.None,
				_Time = VenueData.Time.Night,
				_Rules = new CustomMatchData.d_Rules() {
					_Drone = CustomMatchData.EnemyDroneType.None,
					_MaxLap = 1,
					_Rebuild = CustomMatchData.EnableRebuildingType.Enable,
					_Type = (GameVariable.MatchType)3,
					_SpecialRule = CustomMatchData.SpecialRuleType.None
				},
			}]),
			_Stages_Hard = new([new CustomMatchData.MatchData(){
				_Energy = VenueData.Energy.High,
				_Grav = VenueData.Grav.Mid,
				_IsSet = true,
				_Land = VenueData.Landscape.Cyber,
				_Layout = VenueData.VenueLayout.None,
				_Place = SinglePlayData.MatchPlace.Cyber_Battle,
				_Temp = VenueData.Temperature.None,
				_Time = VenueData.Time.Night,
				_Rules = new CustomMatchData.d_Rules() {
					_Drone = CustomMatchData.EnemyDroneType.None,
					_MaxLap = 1,
					_Rebuild = CustomMatchData.EnableRebuildingType.Enable,
					_Type = (GameVariable.MatchType)3,
					_SpecialRule = CustomMatchData.SpecialRuleType.None
				},
			}]),
			_PrizeMultiplier = 4, // Doesn't do anything?
			_ScoreAttackTargets = new([]),
			_ForceMachineKey = "", // Forces you into a specific preset mech.
			_TutorialDialogType = GrandprixDef.TutorialDialogType.None,
		};

		return gpdef;
	}


	// MAIN
	// Initializer
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
	public static void MainStart(Scene_MainMenu __instance)
	{
		if (!init)
		{
			string bundlePath = Path.Combine(Paths.PluginPath, "BossMod", "music");
			if (!File.Exists(bundlePath))
			{
				Plugin.Log.LogError($"Bundle not found at: {bundlePath}");
			}
			else
			{
				UniverseLib.AssetBundle bundle = UniverseLib.AssetBundle.LoadFromFile(bundlePath);
				if (bundle != null)
				{
					var clip = bundle.LoadAsset<AudioClip>("OracleBossMusic.ogg");
					clip.LoadAudioData();
					AuMa.ins.Data_BGM[11] = clip;
				}
				else
				{
					Plugin.Log.LogError("Failed to load assets!");
				}
			}

			List<GrandprixDef> gpDatas = MatchManager.ins._GrandprixData.grandprixDatas.ToList();
			gpDatas.Add(AddModdedGP());
			MatchManager.ins._GrandprixData.grandprixDatas = new Il2CppReferenceArray<GrandprixDef>(gpDatas.ToArray());

			foreach (string key in filenames)
			{
				EnemyData.AllEnemyData.Add(key, bossStats[key]["AI"]);
			}

			init = true;
		}
	}

	public static bool setMusic = false;
	[HarmonyPrefix]
	[HarmonyPatch(typeof(AuMa), nameof(AuMa.PlayBGM), [typeof(AuMa.BGMType), typeof(bool)])]
	public static void PlayMusic(ref bool __runOriginal)
	{
		if (!setMusic) return;
		setMusic = false;
		__runOriginal = false;
		AuMa.ins.PlayBGM(11, AuMa.PlayMode.LongFade, true);
	}

	// STAGE INFO
	// Setup the custom arena.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(StageInfo), nameof(StageInfo.SetupStage))]
	public static void SetupStage()
	{
		if (GameVariable.Match_Key != "GP_10_4") return;

		// setMusic = true;
		// PlayMusic();

		isLoadingModRace = false;
		UI.deadBosses = new bool[filenames.Length];
		UI.spawnDelaysProcessed = new bool[filenames.Length];

		for (int i = 0; i < filenames.Length; i++)
		{
			bossnames[i] = AllPlayerData.ins.Players[i + 1].Relay.GetComponent<StatusManager>().MachineName;
		}

		isInModRace = true;
		UI.ResetUI();

		if (SceneManager.GetActiveScene().name != "Cyber_01") return;

		// Custom arena.
		GameObject[] roots = SceneManager.GetSceneByName("Cyber_01").GetRootGameObjects();
		roots.First(e => e.name == "Tower").transform.position = new(1024, -519.4f, 1024);

		roots.First(e => e.name == "Ground").GetComponent<MeshRenderer>().material.color = Color.black;

		var orna = roots.First(e => e.name == "Circuits").transform.Find("Orna");
		for (int i = 0; i < 4; i++)
		{
			orna.GetChild(i).GetChild(0).GetComponent<MeshRenderer>().material.color = Color.black;
			orna.GetChild(i).DOLocalRotate(new(0, 360, 0), UnityEngine.Random.Range(50, 110), RotateMode.LocalAxisAdd).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).Play();
		}

		Transform cubes = roots.First(e => e.name == "Cubes").transform;
		cubes.GetChild(0).position = new(1162, 25, 510);
		cubes.GetChild(1).position = new(1230, 25, 710);
		cubes.GetChild(2).position = new(1132, 25, 1610);
		cubes.GetChild(3).position = new(830, 25, 1330);
		cubes.GetChild(4).position = new(810, 25, 980);
		cubes.GetChild(5).position = new(880, 25, 1550);
		cubes.GetChild(6).position = new(1230, 25, 1260);
		cubes.GetChild(7).position = new(900, 25, 650);
		float[] deterministic_durations = [9, 12, 8, 11, 7, 8, 12, 10, 11, 12, 9, 10];
		float[] deterministic_delays = [4, 3.5f, 3, 4, 3, 4.5f, 3.5f, 3.5f, 4.5f, 4, 4, 3.5f];
		for (int i = 0; i < cubes.childCount; i++)
		{
			var cube = cubes.GetChild(i).DOMoveY(-25f, deterministic_durations[i]).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(deterministic_delays[i]).SetUpdate(false).Play();
		}

		Light spot = roots.First(e => e.name == "Lightings").transform.GetChild(0).GetComponent<Light>();
		Light sun = roots.First(e => e.name == "Lightings").transform.GetChild(1).GetComponent<Light>();
		Color sunColor = new(.6f, .2f, .3f);
		Color sunBrightColor = new(1f, .9f, .9f);
		Color spotColor = new(.45f, .45f, .45f);
		Color spotBrightColor = new(1, 1, 1);
		sun.color = sunColor;
		spot.color = spotColor;

		float[] light_delays = [8, .12f, .05f, 1.5f, 7, .04f, .1f, 10, .4f, .06f, .3f, 8, .4f, .14f, .2f, 5, .06f, .1f, 8, .02f, .14f, .2f, 10, .08f, .1f, .4f, 7, .03f, .3f, .1f];

		Sequence sunSequence = DOTween.Sequence().SetLoops(-1, LoopType.Restart);
		Sequence spotSequence = DOTween.Sequence().SetLoops(-1, LoopType.Restart);

		foreach (float delay in light_delays)
		{
			float rand = UnityEngine.Random.RandomRange(.02f, .08f);
			sunSequence.Append(sun.DOColor(sunBrightColor, .02f).SetDelay(delay));
			spotSequence.Append(spot.DOColor(spotBrightColor, .02f).SetDelay(delay));
			sunSequence.Append(sun.DOColor(sunColor, rand).SetDelay(.06f));
			spotSequence.Append(spot.DOColor(spotColor, rand).SetDelay(.06f));
		}
	}


	// DATA MANAGER
	[HarmonyPrefix]
	[HarmonyPatch(typeof(DataManager), nameof(DataManager.LoadBuildData))]
	public static void PreLoadBuildData(ref string path, ref bool UseResources, ref bool UseFullPath, DataManager __instance)
	{
		if (!isLoadingModRace) return;
		if (filenames.Contains(path))
		{
			UseResources = false;
			UseFullPath = false;
			path = $"Machine/{path}.es3";
		}
	}


	// MACHINE MANAGER
	// This is where stats are faked. Player stats are capped here too.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(StatusManager), nameof(StatusManager.Update))]
	public static void PostUpdateMachine(StatusManager __instance)
	{
		if (GameVariable.Match_Key != "GP_10_4") return;

		if (__instance.IsMyPlayer)
		{
			if (__instance.IsInJKR())
				Application.Quit();
			if (__instance.MaxLockRange > 500)
				__instance.MaxLockRange = 500;
		}
		else
		{
			if (bossnames.Contains(__instance.MachineName))
			{
				int idx = bossnames.IndexOf(__instance.MachineName);
				var stats = bossStats[filenames[idx]];
				__instance.MaxHealth = stats["health"];
				__instance.Shield = stats["shield"];
				__instance.Weight = stats["weight"];
				__instance.Generation = stats["generation"];
				__instance.MaxEnergy = stats["maxEnergy"];
				__instance.Cooling = stats["cooling"];
				__instance.MaxHeat = stats["maxHeat"];
				__instance.GroundOffset = stats["groundOffset"]; // This is how Castigator "flies".
				__instance.ActionSpeedMultiplier = stats["driveSpeed"];
				__instance.MaxLockRange = stats["lockRange"];
				__instance.LockSpeed = stats["lockSpeed"];
			}
			// This is the custom movement boundary so they don't sit in the arena borders. This is presumably no longer required, since that "bug" was instead caused by insane stun times. Maybe remove it?
			// __instance.transform.position = new(Mathf.Clamp(__instance.transform.position.x, 785, 1320), __instance.transform.position.y, Mathf.Clamp(__instance.transform.position.x, 470, 1575));
		}
	}

	// Never tested this. I think this happens when someone uses Diffractors?
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineManager), nameof(MachineManager.UpdateParamOnSeparation))]
	public static void UpdateParamOnSeparation(MachineManager __instance)
	{
		if (!isInModRace) return;
		Plugin.LogInfo(__instance._statusManager?.MachineName);
		if (__instance._statusManager.IsMyPlayer)
		{
			if (__instance._statusManager.MaxLockRange > 500)
				__instance._statusManager.MaxLockRange = 500;
		}
	}

	private static bool retarget = false;
	private static float retargetTimer = 0;
	// Begin force retargetting delay
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineManager), nameof(MachineManager.SetReturnPoint))]
	public static void SetReturnPoint(MachineManager __instance)
	{
		if (!isInModRace) return;
		retarget = true;
		retargetTimer = 0;
	}

	// Force retarget the AIs to bypass a vanilla bug. The AIs are forcibly spun towards the player, which from what I've seen,
	// should be enough to attract their attention. This is why my AIs have absurd lock range.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineManager), nameof(MachineManager.Update))]
	public static void Retarget()
	{
		if (!isInModRace) return;
		if (!retarget) return;
		retargetTimer += Time.deltaTime;
		if (retargetTimer < 5.5f) return;
		retarget = false;
		retargetTimer = 0;

		for (int i = 1; i < 3; i++)
		{
			GameObject player = AllPlayerData.ins.Players[0].Relay.gameObject;
			if (!player) return;
			if (player.active == false) return;
			LockTarget playerLock = player.transform.Find("Lock-on").Find("MyLockTarget").GetComponent<LockTarget>();
			if (!playerLock) return;
			GameObject self = AllPlayerData.ins.Players[i].Relay.gameObject;
			if (!self) return;
			if (self.active == false) return;
			if (self.GetComponent<StatusManager>()?.IsEnable == false) return;
			if (self.GetComponent<FireControl>()?.IsEnable == false) return;
			if (self.transform.Find("AI").GetComponent<AI_Main>()?.AI_Control == false) return;

			self.transform.GetComponent<Rigidbody>()?.rotation = Quaternion.LookRotation(player.transform.position - self.transform.position);
			player.transform.Find("AI").GetComponent<AI_Main>()?._CurrentLockTarget = playerLock;
		}
	}


	// STATUS MANAGER
	// This is how I get bosses out of play to "spawn" them later.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(StatusManager), nameof(StatusManager.SetEnable))]
	public static void SetEnable(StatusManager __instance, bool IsEnable)
	{
		if (!isInModRace) return;
		if (bossnames.Contains(__instance.MachineName) && IsEnable == true)
		{
			int idx = bossnames.IndexOf(__instance.MachineName);
			if (bossStats[filenames[idx]]["delayedSpawn"] == true)
			{
				__instance.gameObject.transform.position = new(4000, 100, 1000);
				__instance.gameObject.active = false;
			}
		}
	}

	// This blocks bosses from respawning. Causes a few non-fatal errors in the console.
	[HarmonyPrefix]
	[HarmonyPatch(typeof(StatusManager._BreakRoutine_d__133), nameof(StatusManager._BreakRoutine_d__133.MoveNext))]
	public static void CallRespawn(StatusManager._BreakRoutine_d__133 __instance, bool __result)
	{
		if (!isInModRace) return;
		if (bossnames.Contains(__instance.__4__this.MachineName))
		{
			int idx = bossnames.IndexOf(__instance.__4__this.MachineName);
			__instance.__4__this.gameObject.active = false;
			UI.deadBosses[idx] = true;
		}
	}

	// This is where I modify the stun time.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(StatusManager), nameof(StatusManager.SetRigor))]
	public static void SetRigor(StatusManager __instance, ref float time)
	{
		if (!isInModRace) return;
		if (bossnames.Contains(__instance.MachineName))
			__instance.RigorTime = 2f;
	}
}

// public static class Unity6AssetBundleLoader
// {
// 	// Define the exact C++ unmanaged delegate signature used by Unity 6
// 	// pathPtr = IntPtr to the Native Il2CppString
// 	private delegate IntPtr d_LoadFromFile_Internal(IntPtr pathPtr, uint crc, ulong offset);
// 	private static d_LoadFromFile_Internal _rawLoadFromFileInternal;

// 	static Unity6AssetBundleLoader()
// 	{
// 		// Resolve the internal C++ function directly from the Unity Engine binaries
// 		IntPtr icallPtr = IL2CPP.il2cpp_resolve_icall("UnityEngine.AssetBundle::LoadFromFile_Internal(System.String,System.UInt32,System.UInt64)");

// 		if (icallPtr != IntPtr.Zero)
// 		{
// 			_rawLoadFromFileInternal = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<d_LoadFromFile_Internal>(icallPtr);
// 		}
// 	}

// 	public static AssetBundle LoadBundle(string bundlePath)
// 	{
// 		if (_rawLoadFromFileInternal == null)
// 		{
// 			Plugin.Log.LogError("Failed to resolve raw AssetBundle icall pointer!");
// 			return null;
// 		}

// 		// Convert the managed C# string into an unmanaged native IL2CPP string pointer object
// 		IntPtr nativeStringPtr = IL2CPP.ManagedStringToIl2Cpp(bundlePath);

// 		// Invoke the unmanaged C++ function directly, skipping the broken C# interop wrappers
// 		IntPtr nativeBundleResult = _rawLoadFromFileInternal(nativeStringPtr, 0, 0);

// 		if (nativeBundleResult == IntPtr.Zero)
// 		{
// 			return null;
// 		}

// 		// Cast the raw object pointer back to a managed AssetBundle proxy class
// 		return new AssetBundle(nativeBundleResult);
// 	}
// }