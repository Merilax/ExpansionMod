using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ArchEmperorLib;
using BepInEx;

namespace ArchExpansionMod;

public class ModBundleParser
{
	public class ModDefinition
	{
		public string bundleName = "";
		public string bundleVersion = "";
		public int moduleCount = 0;
		public List<string> armors = [];
		public List<string> coolers = [];
		public List<string> generators = [];
		public List<string> radars = [];
		public List<string> thrusters = [];
		public List<string> physics = [];
		public List<string> specials = [];
		public List<string> weapons = [];
		public List<string> joints = [];
		public List<string> ornaments = [];
		public List<TranslationTable> translationTables = [];
	}

	public static List<ModDefinition> pendingLoadModDefs = [];
	// public static List<ModDefinition> loadedModDefs = [];
	public static List<ModDefinition> loadedModDefs = [];
	public static List<BundleRecord> loadedModRecords = [];
	public static List<string> registeredPrefixes = [];

	public static void ParseModBundles()
	{
		string contentRoot = Path.Combine(Paths.PluginPath, Plugin.BUNDLE_ROOT);

		if (!Directory.Exists(contentRoot)) Directory.CreateDirectory(contentRoot);
		string[] dirs = Directory.GetDirectories(contentRoot);

		foreach (var dirPath in dirs)
		{
			try
			{
				Plugin.LogInfo("Loading mod bundle: " + dirPath);
				if (!File.Exists(Path.Combine(dirPath, "manifest.json")))
				{
					Plugin.Log.LogError("Could not find a manifest.json file in mod directory: " + dirPath);
					continue;
				}

				string jsonStr = File.ReadAllText(Path.Combine(dirPath, "manifest.json"));

				ModDefinition modDef = JsonSerializer.Deserialize<ModDefinition>(jsonStr, new JsonSerializerOptions() { IncludeFields = true }); // WriteIndented = true

				if (modDef.bundleName == null || modDef.bundleName.Length <= 0)
				{
					Plugin.Log.LogError("Manifest error: bundleName is empty.");
					continue;
				}
				if (modDef.armors == null || modDef.coolers == null || modDef.generators == null || modDef.radars == null || modDef.thrusters == null || modDef.physics == null || modDef.specials == null || modDef.weapons == null || modDef.joints == null || modDef.ornaments == null)
				{
					Plugin.Log.LogError("Manifest error: Missing category entries.");
					continue;
				}

				if (!File.Exists(Path.Combine(dirPath, modDef.bundleName)))
				{
					Plugin.Log.LogError("Could not find mod bundle file at: " + Path.Combine(dirPath, modDef.bundleName));
					continue;
				}

				pendingLoadModDefs.Add(modDef);
				loadedModRecords.Add(new(modDef.bundleName, modDef.bundleVersion));
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError("Unexpected error while loading mod bundle: " + dirPath);
				Plugin.Log.LogError(ex);
				continue;
			}
		}
		Plugin.LogInfo($"Detected and loaded {pendingLoadModDefs.Count} module bundles.");

		ModAssetHandler.PrepareAssets();
	}

	public static bool IsValidPrefix(string key)
	{
		bool flag = false;
		registeredPrefixes.ForEach(prefix => { if (key.Contains(prefix)) flag = true; });
		return flag;
	}
}

public class TranslationTable
{
	public string ID;
	public string English;
	public string Japanese;
	public string SimplifiedChinese;
	public string TraditionalChinese;
}