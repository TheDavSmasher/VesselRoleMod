using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using System.Reflection;
using TownOfUs.Roles.Neutral;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Patches.ControlSystem.Interactions;

[HarmonyPatch]
public static class PoltergeistVentPatches
{
	[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
	[HarmonyPostfix]
	public static void VesselEnterVentPostfix(Vent __instance, PlayerControl pc)
	{
		if (pc.TryGetModifier<VesselPossessedModifier>(out var mod) &&
			mod.Ghost != null)
		{
			if (mod.Ghost.AmOwner)
			{
				Vent.currentVent = __instance;
				ConsoleJoystick.SetMode_Vent();
			}
			else if (mod.Vessel.AmOwner && ((IVesselModifier)mod).Role is not JesterRole)
			{
				Vent.currentVent.SetButtons(true);
			}
		}
	}

	[HarmonyPatch]
	public static class PhysicsEnterVentPatch
	{
		public static MethodBase TargetMethod()
		{
			return Helpers.GetStateMachineMoveNext<PlayerPhysics>(nameof(PlayerPhysics.CoEnterVent))!;
		}

		public static void Postfix(Il2CppObjectBase __instance, bool __result)
		{
			if (__result)
			{
				return;
			}

			var wrapper = new StateMachineWrapper<PlayerPhysics>(__instance);

			var id = wrapper.GetParameter<int>("id");
			var physics = wrapper.Instance;
			var player = physics.myPlayer;

			if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
				mod.Ghost != null && mod.Ghost.AmOwner)
			{
				VentilationSystem.Update(VentilationSystem.Operation.Enter, id);
			}
		}
	}

	[HarmonyPatch]
	public static class PhysicsExitVentPatch
	{
		public static MethodBase TargetMethod()
		{
			return Helpers.GetStateMachineMoveNext<PlayerPhysics>(nameof(PlayerPhysics.CoExitVent))!;
		}

		public static void Postfix(Il2CppObjectBase __instance, bool __result)
		{
			if (__result)
			{
				return;
			}

			var wrapper = new StateMachineWrapper<PlayerPhysics>(__instance);

			var id = wrapper.GetParameter<int>("id");
			var physics = wrapper.Instance;
			var player = physics.myPlayer;

			if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
				mod.Ghost != null && mod.Ghost.AmOwner)
			{
				VentilationSystem.Update(VentilationSystem.Operation.Exit, id);
			}
		}
	}

	[HarmonyPatch]
	public static class VentExitVesselPatch
	{
		public static MethodBase TargetMethod()
		{
			return Helpers.GetStateMachineMoveNext<Vent>(nameof(Vent.ExitVent))!;
		}

		public static void Prefix(Il2CppObjectBase __instance)
		{
			var wrapper = new StateMachineWrapper<Vent>(__instance);

			var player = wrapper.GetParameter<PlayerControl>("pc");

			if (wrapper.GetState() != 0)
			{
				return;
			}

			if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
				mod.Ghost != null)
			{
				if (mod.Vessel.AmOwner || mod.Ghost.AmOwner)
				{
					Vent.currentVent.SetButtons(false);
				}
				if (mod.Ghost.AmOwner)
				{
					Vent.currentVent = null;
					ConsoleJoystick.SetMode_Gameplay();
				}
			}
		}
	}

	[HarmonyPatch(typeof(Vent), nameof(Vent.TryMoveToVent))]
	[HarmonyPrefix]
	public static bool TryMoveVesselToVentPrefix(Vent __instance, Vent otherVent, ref string error, ref bool __result)
	{
		if (otherVent == null)
		{
			return true;
		}
		var localPlayer = PlayerControl.LocalPlayer;
		if (!localPlayer.TryGetModifierOfType<IVesselPossessModifier>(out var mod) ||
			mod.Vessel == null)
		{
			return true;
		}
		if (!VesselControlState.HasControl(localPlayer.PlayerId))
		{
			error = "Player does not have control.";
			return (__result = false);
		}
		if (!mod.Vessel.inVent)
		{
			error = "Vessel is not currently inside a vent";
			return (__result = false);
		}
		if (mod.Vessel.walkingToVent || mod.Vessel.Visible)
		{
			error = "Vessel was still in the middle of animating into current vent; not allowed to move vents that fast";
			return (__result = false);
		}
		VesselRole.RpcVesselTryMoveToVent(localPlayer, mod.Ghost, mod.Vessel, __instance.Id, otherVent.Id);
		error = string.Empty;
		__result = true;
		return false;
	}

	[HarmonyPatch(typeof(Vent), nameof(Vent.SetButtons))]
	public static class VentSetButtonsPatch
	{
		public static bool Prefix(bool enabled)
		{
			if (!enabled)
			{
				return true;
			}
			var localPlayer = PlayerControl.LocalPlayer;
			if (!localPlayer.TryGetModifierOfType<IVesselPossessModifier>(out var mod) ||
				mod.Vessel == null || mod.Ghost == null)
			{
				return true;
			}
			if (mod.Role is JesterRole)
			{
				return false;
			}
			return true;
		}

		public static void Postfix(Vent __instance, bool enabled)
		{
			if (!enabled)
			{
				return;
			}

			var localPlayer = PlayerControl.LocalPlayer;
			if (!localPlayer.TryGetModifierOfType<IVesselPossessModifier>(out var mod) ||
				mod.Vessel == null || mod.Ghost == null)
			{
				return;
			}
			if (mod.Role is JesterRole)
			{
				return;
			}

			var hasControl = VesselControlState.HasControl(localPlayer.PlayerId);

			Vent[] nearbyVents = __instance.NearbyVents;
			for (int i = 0; i < __instance.Buttons.Length; i++)
			{
				ButtonBehavior buttonBehavior = __instance.Buttons[i];
				Vent vent = nearbyVents[i];
				if (vent && vent.enabled)
				{
					buttonBehavior.spriteRenderer.color = hasControl ? Palette.EnabledColor : Palette.DisabledGrey;
				}
			}
		}
	}
}
