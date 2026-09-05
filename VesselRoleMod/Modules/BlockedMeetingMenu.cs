using HarmonyLib;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace VesselRoleMod.Modules;

public sealed class BlockedMeetingMenu : IDisposable
{
	public delegate bool Exemption(PlayerVoteArea voteArea);

	public delegate void OnClick(PlayerVoteArea voteArea, MeetingHud meeting);

	public BlockedMeetingMenu(
		RoleBehaviour owner,
		OnClick onClick,
		MeetingAbilityType abilityType,
		LoadableAsset<Sprite> activeSprite,
		LoadableAsset<Sprite> disabledSprite = null!,
		LoadableAsset<Sprite> blockedSprite = null!,
		Exemption exemption = null!,
		Exemption blocking = null!,
		Color? activeColor = null!,
		Color? disabledColor = null!,
		Color? blockedColor = null!,
		Color? hoverColor = null!,
		Vector3? position = null!)
	{
		Owner = owner;
		Click = onClick ?? throw new ArgumentException("onClick should exist");
		IsExempt = exemption;
		IsBlocked = blocking;
		ActiveSprite = activeSprite;
		DisabledSprite = disabledSprite;
		BlockedSprite = blockedSprite;
		ActiveColor = activeColor ?? Color.green;
		DisabledColor = disabledColor ?? Color.white;
		BlockedColor = blockedColor ?? Color.gray;
		HoverColor = hoverColor ?? Color.red;
		Type = abilityType;
		Position = position ?? new Vector3(-0.95f, 0.03f, -3f);
		AbilityName = string.Empty;

		Instances.Add(this);
	}

	public BlockedMeetingMenu(
		RoleBehaviour owner,
		OnClick onClick,
		string abilityName,
		MeetingAbilityType abilityType,
		LoadableAsset<Sprite> activeSprite,
		LoadableAsset<Sprite> disabledSprite = null!,
		LoadableAsset<Sprite> blockedSprite = null!,
		Exemption exemption = null!,
		Exemption blocking = null!,
		Color? activeColor = null!,
		Color? disabledColor = null!,
		Color? blockedColor = null!,
		Color? hoverColor = null!,
		Vector3? position = null!)
	{
		Owner = owner;
		Click = onClick ?? throw new ArgumentException("onClick should exist");
		IsExempt = exemption;
		IsBlocked = blocking;
		ActiveSprite = activeSprite;
		DisabledSprite = disabledSprite;
		BlockedSprite = blockedSprite;
		ActiveColor = activeColor ?? Color.green;
		DisabledColor = disabledColor ?? Color.white;
		BlockedColor = blockedColor ?? Color.gray;
		HoverColor = hoverColor ?? Color.red;
		Type = abilityType;
		Position = position ?? new Vector3(-0.95f, 0.03f, -3f);
		AbilityName = abilityName;

		Instances.Add(this);
	}

	public static List<BlockedMeetingMenu> Instances { get; set; } = [];

	public RoleBehaviour Owner { get; }
	public OnClick Click { get; }
	public Exemption IsExempt { get; }
	public Exemption IsBlocked { get; }
	public LoadableAsset<Sprite> ActiveSprite { get; }
	public LoadableAsset<Sprite> DisabledSprite { get; }
	public LoadableAsset<Sprite> BlockedSprite { get; }
	public string AbilityName { get; }
	private Color ActiveColor { get; }
	private Color DisabledColor { get; }
	private Color BlockedColor { get; }
	private Color HoverColor { get; }
	public MeetingAbilityType Type { get; }
	public Vector3 Position { get; set; }
	public Dictionary<byte, bool> Actives { get; } = [];
	public Dictionary<byte, bool> Blocked { get; } = [];
	public Dictionary<byte, GameObject> Buttons { get; } = [];
	private Dictionary<byte, SpriteRenderer> ButtonSprites { get; } = [];

	public void Dispose()
	{
		HideButtons();
	}

	public void HideButtons()
	{
		Buttons.Keys.ToList().ForEach(HideSingle);
		Buttons.Clear();
		Actives.Clear();
		Blocked.Clear();
		ButtonSprites.Clear();
	}

	public void HideSingle(byte targetId)
	{
		Actives[targetId] = false;

		if (!Buttons.TryGetValue(targetId, out var button) || !button)
		{
			return;
		}

		button.SetActive(false);
		button.Destroy();
		Buttons[targetId] = null!;
		ButtonSprites[targetId] = null!;
	}

	private void GenButton(PlayerVoteArea voteArea, MeetingHud __instance)
	{
		Actives.Add(voteArea.PlayerId, false);
		Blocked.Add(voteArea.PlayerId, false);

		if (IsExempt(voteArea))
		{
			Buttons.Add(voteArea.PlayerId, null!);
			ButtonSprites.Add(voteArea.PlayerId, null!);
			return;
		}

		var targetBox = UObject.Instantiate(
			voteArea.Buttons.transform.Find("CancelButton").gameObject,
			voteArea.transform);
		targetBox.name = $"MeetingButton{Owner.GetRoleName().Replace(" ", "")}{voteArea.PlayerId}";
		targetBox.transform.localPosition = Position;
		var renderer = targetBox.GetComponent<SpriteRenderer>();

		if (IsBlocked(voteArea))
		{
			Blocked[voteArea.PlayerId] = true;

			renderer.sprite = BlockedSprite.LoadAsset();
			renderer.color = BlockedColor;

			targetBox.transform.GetChild(0).gameObject.Destroy();

			Buttons.Add(voteArea.PlayerId, targetBox);
			ButtonSprites.Add(voteArea.PlayerId, renderer);
			return;
		}

		renderer.sprite = (Type == MeetingAbilityType.Toggle ? DisabledSprite : ActiveSprite).LoadAsset();
		var button = targetBox.GetComponent<PassiveButton>();
		button.OverrideOnClickListeners(() => Click(voteArea, __instance));
		button.OverrideOnMouseOverListeners(() => renderer.color = HoverColor);
		button.OverrideOnMouseOutListeners(() => renderer.color =
			Type == MeetingAbilityType.Toggle && Actives[voteArea.PlayerId]
				? ActiveColor
				: DisabledColor);
		var collider = targetBox.GetComponent<BoxCollider2D>();
		collider.size = renderer.sprite.bounds.size;
		collider.offset = Vector2.zero;
		targetBox.transform.GetChild(0).gameObject.Destroy();

		var buttonText = UObject.Instantiate(
			__instance.MeetingAbilityButton.buttonLabelText.gameObject,
			targetBox.transform);
		buttonText.transform.localPosition = new Vector3(0, -0.2f, 0f);
		var tmpText = buttonText.GetComponent<TextMeshPro>();
		tmpText.color = Color.white;
		tmpText.text = AbilityName;
		//tmpText.ForceMeshUpdate();
		tmpText.fontSize = 2.5f;
		tmpText.fontSizeMax = 2.5f;
		tmpText.fontSizeMin = 2.5f;
		tmpText.m_enableWordWrapping = false;
		Buttons.Add(voteArea.PlayerId, targetBox);
		ButtonSprites.Add(voteArea.PlayerId, renderer);
	}

	public void GenButtons(MeetingHud meeting, bool usable)
	{
		HideButtons();

		if (!usable || !Owner.Player.AmOwner)
		{
			return;
		}

		Actives.Clear();
		Blocked.Clear();
		Buttons.Clear();
		ButtonSprites.Clear();
		meeting.playerStates.ToList().ForEach(x => GenButton(x, meeting));
	}

	public void Update()
	{
		if (!MeetingHud.Instance || Type != MeetingAbilityType.Toggle)
		{
			return;
		}

		foreach (var pair in ButtonSprites)
		{
			if (!pair.Value)
			{
				continue;
			}

			pair.Value.sprite = (Blocked[pair.Key] ? BlockedSprite : Actives[pair.Key] ? ActiveSprite : DisabledSprite).LoadAsset();
			pair.Value.color = Blocked[pair.Key] ? BlockedColor : Actives[pair.Key] ? ActiveColor : DisabledColor;
		}
	}

	public static void ClearAll()
	{
		Instances.Do(x => x.Dispose());
	}
}
