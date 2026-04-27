using MiraAPI.Events;
using System;
using UnityEngine;

namespace VesselRoleMod.Events;

public class CustomAbilityEvent<T> : MiraEvent where T : Enum
{
	/// <summary>
	///     Initializes a new instance of the <see cref="CustomAbilityEvent{T}" /> class.
	/// </summary>
	/// <param name="ability">The player's ability that was used.</param>
	/// <param name="result">The ability's result in text, used for detailed logging.</param>
	/// <param name="player">The player who used the ability.</param>
	/// <param name="target">The player's target, if available.</param>
	/// <param name="target2">The player's second target, if available.</param>
	public CustomAbilityEvent(T ability, string result, PlayerControl player, MonoBehaviour? target = null,
		MonoBehaviour? target2 = null)
	{
		AbilityType = ability;
		Player = player;
		Target = target;
		Target2 = target2;
		Result = result;
	}
	/// <summary>
	///     Initializes a new instance of the <see cref="CustomAbilityEvent{T}" /> class.
	/// </summary>
	/// <param name="ability">The player's ability that was used.</param>
	/// <param name="player">The player who used the ability.</param>
	/// <param name="target">The player's target, if available.</param>
	/// <param name="target2">The player's second target, if available.</param>
	public CustomAbilityEvent(T ability, PlayerControl player, MonoBehaviour? target = null,
		MonoBehaviour? target2 = null)
	{
		AbilityType = ability;
		Player = player;
		Target = target;
		Target2 = target2;
		Result = "No Information";
	}

	/// <summary>
	///     Gets the player who used the ability.
	/// </summary>
	public PlayerControl Player { get; }

	/// <summary>
	///     Gets the target of the ability, if any.
	/// </summary>
	public MonoBehaviour? Target { get; set; }

	/// <summary>
	///     Gets the second target of the ability, if any.
	/// </summary>
	public MonoBehaviour? Target2 { get; set; }

	/// <summary>
	///     Gets the ability used by the player.
	/// </summary>
	public T AbilityType { get; }

	/// <summary>
	///     Gets the detailed results from the ability, if any.
	/// </summary>
	public string Result { get; }
}
