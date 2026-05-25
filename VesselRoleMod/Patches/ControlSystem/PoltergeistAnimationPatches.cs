using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using System.Collections;
using System.Reflection;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch]
public static class PoltergeistAnimationPatches
{
	[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.StartClimb))]
	[HarmonyPostfix]
	public static void VesselClimbLadderPostfix(PlayerPhysics __instance, bool down)
	{
		if (__instance.myPlayer.TryGetModifier<VesselPossessedModifier>(out var mod) &&
			mod.Ghost != null)
		{
			mod.Ghost.MyPhysics.StartClimb(down);
		}
	}

	[HarmonyPatch]
	public static class AnimateZiplinePatch
	{
		public static MethodBase TargetMethod()
		{
			return Helpers.GetStateMachineMoveNext<ZiplineBehaviour>(nameof(ZiplineBehaviour.CoAnimateZiplineAndPlayer))!;
		}

		public static void Prefix(Il2CppObjectBase __instance)
		{
			var wrapper = new StateMachineWrapper<ZiplineBehaviour>(__instance);
			if (wrapper.GetState() != 0)
			{
				return;
			}

			var player = wrapper.GetParameter<PlayerControl>("player");
			var fromTop = wrapper.GetParameter<bool>("fromTop");
			var instance = wrapper.Instance;

			if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
				mod.Ghost != null)
			{
				float travelSeconds;
				Vector3 handleEndPosition;
				if (fromTop)
				{
					travelSeconds = instance.downTravelTime;
					handleEndPosition = instance.dropPositionBottom.position;
				}
				else
				{
					travelSeconds = instance.upTravelTime;
					handleEndPosition = instance.dropPositionTop.position;
				}
				mod.Ghost.MyPhysics.enabled = false;
				instance.StartCoroutine(CoGhostAnimateZipline(mod.Ghost, travelSeconds, handleEndPosition).WrapToIl2Cpp());
			}
		}

		private static IEnumerator CoGhostAnimateZipline(PlayerControl player, float travelSeconds, Vector3 handleEndPosition)
		{
			Vector3 zero = Vector3.zero;
			Vector3 startPos = player.transform.position;

			for (float time = 0f; time < travelSeconds; time += Time.deltaTime)
			{
				float t = time / travelSeconds;
				if (player != null)
				{
					zero.x = Mathf.SmoothStep(startPos.x, handleEndPosition.x, t);
					zero.y = Mathf.SmoothStep(startPos.y, handleEndPosition.y, t);
					zero.z = player.transform.localPosition.z;
					player.transform.position = zero;
					yield return null;
					continue;
				}
				Debug("Player transform was destroyed while moving");
				break;
			}

			player?.MyPhysics.enabled = true;
		}
	}

	[HarmonyPatch]
	public static class AlightFromZiplinePatch
	{
		public static MethodBase TargetMethod()
		{
			return Helpers.GetStateMachineMoveNext<ZiplineBehaviour>(nameof(ZiplineBehaviour.CoAlightPlayerFromZipline))!;
		}

		public static void Postfix(Il2CppObjectBase __instance, bool __result)
		{
			var wrapper = new StateMachineWrapper<ZiplineBehaviour>(__instance);

			if (__result)
			{
				return;
			}

			var player = wrapper.GetParameter<PlayerControl>("player");

			if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
				mod.Ghost != null)
			{
				mod.Ghost.transform.position = player.transform.position;
			}
		}
	}

	[HarmonyPatch(typeof(ZiplineBehaviour), nameof(ZiplineBehaviour.CoUseZipline))]
	[HarmonyPostfix]
	public static void VesselUseZiplinePostfix(ZiplineBehaviour __instance, PlayerControl player)
	{
		if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
			mod.Ghost != null && mod.Ghost.AmOwner && __instance.lastUsedConsole)
		{
			__instance.lastUsedConsole.SetDestinationCooldown();
		}
	}

	[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
	[HarmonyPostfix]
	public static void VesselEnterVentPostfix(Vent __instance, PlayerControl pc)
	{
		if (pc.TryGetModifier<VesselPossessedModifier>(out var mod) &&
			mod.Ghost != null)
		{
			Vent.currentVent = __instance;
			ConsoleJoystick.SetMode_Vent();
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

		public static void Postfix(Il2CppObjectBase __instance)
		{
			var wrapper = new StateMachineWrapper<Vent>(__instance);

			var player = wrapper.GetParameter<PlayerControl>("pc");

			if (wrapper.GetState() != 0)
			{
				return;
			}

			if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
				mod.Ghost != null && mod.Ghost.AmOwner)
			{
				Vent.currentVent = null;
				ConsoleJoystick.SetMode_Gameplay();
			}
		}
	}
}
