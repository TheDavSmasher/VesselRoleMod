using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MiraAPI;
using MiraAPI.PluginLoading;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System.Globalization;
using TownOfUs;

namespace VesselRoleMod;

[BepInAutoPlugin("toumiragames.tou.vesselrole", "Tou Mira: Vessel Role")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
[BepInDependency(TownOfUsPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class VesselRoleModPlugin : BasePlugin, IMiraPlugin
{
	/// <summary>
	///     Gets the specified Culture for string manipulations.
	/// </summary>
	public static CultureInfo Culture => TownOfUsPlugin.Culture;

	public Harmony Harmony { get; } = new(Id);

	public string OptionsTitleText => "TOU Mira: Vessel Role";

	/// <summary>
	///     Determines if the current build is a dev build or not. This will change certain visuals as well as always grab news locally to be up to date.
	/// </summary>
	public static bool IsDevBuild => true;


	/// <inheritdoc />
	public ConfigFile GetConfigFile()
	{
		return Config;
	}

	public override void Load()
	{
		ReactorCredits.Register("Tou Extension: Vessel Role", Version, IsDevBuild, ReactorCredits.AlwaysShow);
		IL2CPPChainloader.Instance.Finished += Modules.ExtensionLocale.SearchInternalLocale; // Initialise AFTER the mods are loaded to ensure maximum parity (no need for the soft dependency either then)
		
		Harmony.PatchAll();
	}
}
