using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using System.Collections;
using System.Reflection;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;

namespace VesselRoleMod.Patches.ControlSystem.Interactions;

[HarmonyPatch]
public static class PoltergeistAnimationPatches
{
	#region Ladders
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
	public static class ClimbLadderPatch
	{
		public static MethodBase TargetMethod()
		{
			return Helpers.GetStateMachineMoveNext<PlayerPhysics>(nameof(PlayerPhysics.CoClimbLadder))!;
		}

		public static void Postfix(Il2CppObjectBase __instance, bool __result)
		{
			var wrapper = new StateMachineWrapper<PlayerPhysics>(__instance);

			if (__result)
			{
				return;
			}

			var instance = wrapper.Instance;
			var player = instance.myPlayer;
			var source = wrapper.GetParameter<Ladder>("source");

			if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
				mod.Ghost != null && mod.Ghost.AmOwner)
			{
				source.SetDestinationCooldown();
			}
		}
	}
	#endregion

	#region Zipline
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

	[HarmonyPatch]
	public static class UseZiplinePatch
	{
		public static MethodBase TargetMethod()
		{
			return Helpers.GetStateMachineMoveNext<ZiplineBehaviour>(nameof(ZiplineBehaviour.CoUseZipline))!;
		}

		public static void Postfix(Il2CppObjectBase __instance, bool __result)
		{
			var wrapper = new StateMachineWrapper<ZiplineBehaviour>(__instance);

			if (__result)
			{
				return;
			}

			var instance = wrapper.Instance;
			var player = wrapper.GetParameter<PlayerControl>("player");

			if (player.TryGetModifier<VesselPossessedModifier>(out var mod) &&
				mod.Ghost != null && mod.Ghost.AmOwner && instance.lastUsedConsole)
			{
				instance.lastUsedConsole.SetDestinationCooldown();
			}
		}
	}
	#endregion
}
