using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ExpansionMod;

public class UI
{
	public static TMP_FontAsset font;
	public static Material fontMat;
	
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
	public static void MainStart(Scene_MainMenu __instance)
	{
	}
}