using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossMod;

public class UI
{
	public static TMP_FontAsset font;
	public static Material fontMat;
	private static GameObject scoreLabel;
	private static TextMeshProUGUI scoreText;
	private static GameObject HPContainer;
	private static List<Slider> HPSliders = [];
	private static List<TextMeshProUGUI> HPTexts = [];
	public static bool[] deadBosses;
	public static bool[] spawnDelaysProcessed;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
	public static void MainStart(Scene_MainMenu __instance)
	{
		HPSliders.Clear();
		HPTexts.Clear();

		GameObject canvas = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(e => e.name == "Canvas_MainMenu");
		GameObject mainMenuButtons = canvas.transform.GetChild(1).GetChild(4).GetChild(0).gameObject;

		GameObject bossfightBtnObj = GameObject.Instantiate(mainMenuButtons.transform.GetChild(2).gameObject);
		bossfightBtnObj.transform.SetParent(mainMenuButtons.transform);
		bossfightBtnObj.active = true;
		TextMeshProUGUI textMesh = bossfightBtnObj.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
		textMesh.text = "HELL ON EARTH"; // Button text
		font = textMesh.font;
		fontMat = textMesh.fontMaterial;

		Button bossfightBtn = bossfightBtnObj.GetComponent<Button>();
		bossfightBtn.onClick.RemoveAllListeners();
		bossfightBtn.onClick.AddListener((Action)(() =>
		{
			Main.isLoadingModRace = true;
			Main.setMusic = true;

			Transform player = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(e => e.name == "Players").transform.Find("MyPlayer");
			FireControl fc = player.GetComponent<FireControl>();
			foreach (var f in fc.AllUsingController)
			{
				if (f._BulletType_Add == (BulletManager.BulletType_Add)7) // Overkill
					Application.Quit();
				if (f._BulletType != (BulletManager.BulletType)7 )//f._BulletType_Add != (BulletManager.BulletType_Add)6) 
					Application.Quit();
			}
			if (player.GetComponent<StatusManager>().Plugins[2].ability_C == PluginData.C_Ability.jkr) // JKR
				Application.Quit();
asdasdasdasd
			MatchManager.ins.LoadMatchData_Inner(52); // 51 is the last vanilla GP.
			MatchManager.ins.StartMatch();
			__instance.Main_Hide();
		}));

		// Setup in-match UI
		{
			scoreLabel = new("Damage score");
			scoreLabel.transform.SetParent(canvas.transform);
			RectTransform rect = scoreLabel.AddComponent<RectTransform>();
			rect.pivot = new(0, 0);
			rect.anchorMin = new(0, .8f);
			rect.anchorMax = new(0, .8f);
			rect.sizeDelta = new(350, 40);
			rect.anchoredPosition = Vector2.zero;
			Image image = scoreLabel.AddComponent<Image>();
			image.color = new Color(.1f, .1f, .1f, .75f);

			GameObject textObj = new("Score");
			textObj.transform.SetParent(scoreLabel.transform);
			rect = textObj.AddComponent<RectTransform>();
			rect.anchorMin = new(0, 0);
			rect.anchorMax = new(1, 1);
			rect.sizeDelta = new(0, 0);
			rect.anchoredPosition = Vector2.zero;
			TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
			text.m_marginLeft = 5;
			text.font = font;
			text.fontMaterial = fontMat;
			text.text = "0";
			text.fontSize = 30;
			text.alignment = TextAlignmentOptions.Left;
			scoreText = text;


			HPContainer = new("Healthbars");
			HPContainer.transform.SetParent(canvas.transform);
			rect = HPContainer.AddComponent<RectTransform>();
			rect.anchorMin = new(0, 0);
			rect.anchorMax = new(1, 1);
			rect.sizeDelta = new(0, 0);
			rect.anchoredPosition = Vector2.zero;
			VerticalLayoutGroup group = HPContainer.AddComponent<VerticalLayoutGroup>();
			group.padding = new(80, 80, 80, 0);
			group.spacing = 20;
			group.childForceExpandHeight = false;

			foreach (var item in Main.filenames)
			{
				// HP bar
				GameObject HPObj = new("HP Bar");
				HPObj.transform.SetParent(HPContainer.transform);
				Slider slider = HPObj.AddComponent<Slider>();
				slider.maxValue = 1000000;
				slider.minValue = 0;
				slider.value = 1000000;
				slider.wholeNumbers = false;
				slider.interactable = false;
				LayoutElement layout = HPObj.AddComponent<LayoutElement>();
				layout.preferredHeight = 24;
				layout.flexibleWidth = 1;
				// Background bar
				GameObject background = new("Back Fill");
				background.transform.SetParent(HPObj.transform);
				RectTransform backRect = background.AddComponent<RectTransform>();
				backRect.pivot = new Vector2(.5f, .5f);
				backRect.sizeDelta = new Vector2(0, 0);
				backRect.anchoredPosition = Vector2.zero;
				Image backgroundImage = background.AddComponent<Image>();
				backgroundImage.color = new Color(.1f, .1f, .1f, .8f);
				// Filled progress bar
				GameObject fill = new("Fill");
				fill.transform.SetParent(HPObj.transform);
				RectTransform fillRect = fill.AddComponent<RectTransform>();
				fillRect.pivot = new Vector2(.5f, .5f);
				fillRect.sizeDelta = new Vector2(0, 0);
				fillRect.anchoredPosition = Vector2.zero;
				Image fillImage = fill.AddComponent<Image>();
				fillImage.color = new Color(1, .1f, .1f);
				slider.fillRect = fillRect;

				// Bar text
				GameObject HPText = new("HP Text");
				HPText.transform.SetParent(HPObj.transform);
				rect = HPText.AddComponent<RectTransform>();
				rect.anchorMin = new(0, 0);
				rect.anchorMax = new(1, 1);
				rect.sizeDelta = new(0, 0);
				rect.anchoredPosition = Vector2.zero;
				TextMeshProUGUI HPTextMesh = HPText.AddComponent<TextMeshProUGUI>();
				HPTextMesh.font = font;
				HPTextMesh.fontMaterial = fontMat;
				HPTextMesh.text = "BOSS: ";
				HPTextMesh.fontSize = 24;
				HPTextMesh.alignment = TextAlignmentOptions.Center;
				HPTextMesh.outlineWidth = .2f;
				HPTextMesh.fontStyle = FontStyles.Bold;
				HPTextMesh.enableWordWrapping = false;

				HPObj.active = false;
				HPObj.transform.localScale = new(0, 1, 1);

				HPSliders.Add(slider);
				HPTexts.Add(HPTextMesh);
			}

			scoreLabel.active = false;
			HPContainer.active = false;

			bossfightBtnObj.transform.localScale = Vector3.one;
			scoreLabel.transform.localScale = Vector3.one;
			HPContainer.transform.localScale = Vector3.one;
		}
	}


	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchManager), nameof(MatchManager.Player_DeactiveOthers))]
	public static void Player_DeactiveOthers()
	{
		ResetUI();
		Main.isInModRace = false;
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchManager), nameof(MatchManager.CheckNextMatchOrCelemony))]
	public static void CheckNextMatchOrCelemony()
	{
		Main.isMatchInProgress = false;
		processScore = false;
		HPContainer.active = false;
		for (int i = 0; i < Main.filenames.Length; i++)
		{
			AllPlayerData.ins.Players[i + 1].Relay.gameObject.active = true;
		}
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchManager), nameof(MatchManager.RetireMatch))]
	public static void RetireMatch()
	{
		ResetUI();
		Main.isInModRace = false;
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchManager), nameof(MatchManager.RetryMatch))]
	public static void RetryMatch()
	{
		ResetUI();
		Main.isInModRace = false;
		for (int i = 0; i < Main.filenames.Length; i++)
		{
			AllPlayerData.ins.Players[i + 1].Relay.GetComponent<StatusManager>().IsEnable = false;
			AllPlayerData.ins.Players[i + 1].Relay.GetComponent<FireControl>().IsEnable = false;
			AllPlayerData.ins.Players[i + 1].Relay.transform.Find("AI").GetComponent<AI_Main>().AI_Control = false;
		}
		Main.setMusic = true;
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.ShowMainMenu))]
	public static void ShowMainMenu()
	{
		ResetUI();
		Main.isInModRace = false;
	}


	public static void ResetUI()
	{
		if (!Main.isInModRace) return;
		GameObject playerList = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(e => e.name == "Canvas_InGame").transform.FindChild("RightSide").gameObject;
		playerList.GetComponent<CanvasGroup>().alpha = 1;

		score = 0;
		timeDelta = 0;
		scoreText.text = "0";
		scoreLabel.active = false;
		for (int i = 0; i < Main.filenames.Length; i++)
		{
			HPSliders[i].value = HPSliders[i].maxValue;
			HPSliders[i].gameObject.active = false;
			deadBosses[i] = false;
			AllPlayerData.ins.Players[i + 1].Relay.gameObject.active = true; // 0 is always the player, we don't touch it.
		}
		HPContainer.active = false;
		Main.isMatchInProgress = false;
		processScore = true;
	}


	private static float timeDelta = 0;
	public static float timeMultDelta = 0;

	// Delay Castigator's spawn.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MatchManager), nameof(MatchManager.Update))]
	public static void OnUIInGameUpdate()
	{
		if (!Main.isInModRace && !Main.isMatchInProgress) return;
		timeDelta += Time.deltaTime;
		timeMultDelta += Time.deltaTime;

		for (int i = 0; i < Main.filenames.Length; i++)
		{
			string key = Main.filenames[i];
			if (Main.bossStats[key]["delayedSpawn"] == true)
			{
				if (Main.bossStats[key]["delayedSpawnTime"] < timeDelta)
					EnableBoss(i, true);
			}
		}
	}

	// Count match time elapsed for delayed boss spawns. Also hides the leaderboard.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UI_InGame), nameof(UI_InGame._InMatch_Countdown_b__185_0))]
	public static void InMatch_Countdown_b__185_0()
	{
		if (!Main.isInModRace) return;
		spawnDelaysProcessed = new bool[Main.filenames.Length];
		timeDelta = 0;
		timeMultDelta = 0;
		Main.isMatchInProgress = true;

		GameObject playerList = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(e => e.name == "Canvas_InGame").transform.FindChild("RightSide").gameObject;
		playerList.GetComponent<CanvasGroup>().alpha = 0; // Hide leaderboard.

		scoreLabel.active = true;
		HPContainer.active = true;

		for (int i = 0; i < Main.filenames.Length; i++)
		{
			string key = Main.filenames[i];
			if (Main.bossStats[key]["delayedSpawn"] == false)
				EnableBoss(i);
		}
	}


	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineManager), nameof(MachineManager.UpdateMachine))]
	public static void PostUpdateMachine(MachineManager __instance)
	{
		if (!Main.isInModRace) return;
		int idx = Main.bossnames.IndexOf(__instance._statusManager.MachineName);
		if (idx == -1) return;

		HPSliders[idx]?.maxValue = __instance._statusManager.MaxHealth;
		HPSliders[idx]?.value = __instance._statusManager.MaxHealth;
	}

	private static bool processScore = false;
	private static float score = 0;
	[HarmonyPostfix]
	[HarmonyPatch(typeof(StatusManager), nameof(StatusManager.Update))]
	public static void ProcessHealthAndScore(StatusManager __instance)
	{
		if (!Main.isInModRace) return;
		int idx = Main.bossnames.IndexOf(__instance.MachineName);
		if (idx == -1) return;

		HPSliders[idx].maxValue = __instance.MaxHealth;
		HPSliders[idx].value = __instance.CurrentHealth;
		HPTexts[idx].text = $"{__instance.MachineName}: {(HPSliders[idx].value / HPSliders[idx].maxValue * 100).ToString("n2")}%";

		if (!processScore) return;

		float finalMult = 1;
		deadBosses.DoIf(e => e == true, x => finalMult += Main.bossStats[Main.filenames[idx]]["killBonus"]);
		if (deadBosses.All(e => e == true)) finalMult += Mathf.Clamp(15 / timeMultDelta, 0f, .3f);

		score = 0;
		foreach (var slider in HPSliders)
		{
			if (!slider) continue;
			score += slider.maxValue - slider.value;
		}
		score *= finalMult;

		string multStr = deadBosses.Any(e => e == true) ? $"(+{(finalMult - 1) * 100:n2}% !)" : "";

		scoreText.text = $"{(int)score} {multStr}";

		if (deadBosses.All(e => e == true))
		{
			MatchManager.ins.CheckNextMatchOrCelemony();
		}
	}


	public static void EnableBoss(int index, bool wasDelayed = false)
	{
		if (!Main.isInModRace) return;
		if (spawnDelaysProcessed[index] == true) return;
		spawnDelaysProcessed[index] = true;

		HPSliders[index].gameObject.active = true;
		HPSliders[index].value = HPSliders[index].maxValue;
		if (wasDelayed) HPSliders[index].transform.DOScaleX(1, .5f).From(0, true).SetDelay(2.7f).Play();
		else HPSliders[index].transform.DOScaleX(1, .5f).From(0, true).Play();

		var machine = AllPlayerData.ins.Players[index + 1].Relay.gameObject;
		machine.active = false;
		machine.transform.position = new(1000, 3, 500); // Spawns the bosses in front of you. This is not desired if there'll be multiple ones active at the start.
		machine.transform.rotation = Quaternion.EulerAngles(0, 180, 0); // Tied to the above.
		if (wasDelayed)
			machine.transform.position = new(1000, 300, 1000); // Castigator spawns very high in the air in the middle of the arena. Change this as desired.
		machine.active = true;
		machine.GetComponent<MachineManager>().InitConfiguableJoints();
	}
}