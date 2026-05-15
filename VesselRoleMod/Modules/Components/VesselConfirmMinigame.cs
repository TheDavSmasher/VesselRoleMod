using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.LocalSettings;
using MiraAPI.Patches.Stubs;
using Reactor.Utilities;
using Reactor.Utilities.Attributes;
using Reactor.Utilities.Extensions;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using TMPro;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using UnityEngine;
using UnityEngine.Events;
using VesselRoleMod.Assets;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Modules.Components;

[RegisterInIl2Cpp]
[SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Unity")]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Unity")]
public sealed class VesselConfirmMinigame(IntPtr cppPtr) : Minigame(cppPtr)
{
	public TextMeshPro TitleText;
	public SpriteRenderer RoleIcon;
	public TextMeshPro PossessionText;
	public GameObject Divider;
	public GameObject Box;
	public GameObject DenyButton;
	public GameObject AcceptButton;
	public TextMeshPro AcceptText;
	private string GhostName;

	private readonly Color _bgColor = new Color32(24, 0, 0, 215);
	private Action<bool> clickHandler;

	private float Timer;
	private bool TimerActive;

	private static float MaxTime => OptionGroupSingleton<VesselOptions>.Instance.MaxDecisionTime.Value;

	private static string TimerString(float time)
	{
		return Math.Max(time, 0f).ToString("0", NumberFormatInfo.InvariantInfo);
	}

	private void Awake()
	{
		if (Instance)
		{
			Instance.Close();
		}

		var status = transform.FindChild("Status");
		TitleText = status.FindChildComponent<TextMeshPro>("Title");
		RoleIcon = status.FindChildComponent<SpriteRenderer>("RoleImage");
		PossessionText = status.FindChildComponent<TextMeshPro>("RetrainText");
		Divider = status.FindChildObject("Divider");
		Box = status.FindChildObject("Box");
		DenyButton = status.FindChildObject("DenyButton");
		AcceptButton = status.FindChildObject("AcceptButton");
		AcceptText = AcceptButton.transform.FindChildComponent<TextMeshPro>("AcceptText");

		TitleText.font = HudManager.Instance.TaskPanel.taskText.font;
		TitleText.fontMaterial = HudManager.Instance.TaskPanel.taskText.fontMaterial;
		TitleText.text = "Vessel Possession";

		PossessionText.font = HudManager.Instance.TaskPanel.taskText.font;
		PossessionText.fontMaterial = HudManager.Instance.TaskPanel.taskText.fontMaterial;
		PossessionText.text =
			$"{GhostName} is trying to possess you. Do you accept?";

		RoleIcon.sprite = VesselRoleIcons.Vessel.LoadAsset();
		RoleIcon.SetSizeLimit(2.8f);

		AcceptText.text = $"Accept ({TimerString(MaxTime)})";

		TitleText.gameObject.SetActive(false);
		RoleIcon.gameObject.SetActive(false);
		PossessionText.gameObject.SetActive(false);
		Divider.SetActive(false);
		Box.SetActive(false);
		DenyButton.SetActive(false);
		AcceptButton.SetActive(false);
	}

	public void FixedUpdate()
	{
		if (!TimerActive)
		{
			return;
		}

		Timer -= Time.deltaTime;

		if (Timer <= 0f)
		{
			clickHandler.Invoke(true);
			TimerActive = false;
		}

		AcceptText.text = $"Accept ({TimerString(Timer)})";
	}

	public static VesselConfirmMinigame Create()
	{
		var gameObject = Instantiate(TouAssets.ConfirmMinigame.LoadAsset(), HudManager.Instance.transform);
		gameObject.GetComponent<Minigame>().DestroyImmediate();
		gameObject.SetActive(false);

		return gameObject.AddComponent<VesselConfirmMinigame>();
	}

	[HideFromIl2Cpp]
	public void Open(string name, Action<bool> onClick)
	{
		clickHandler = onClick;
		GhostName = name;

		Coroutines.Start(CoOpen(this));
	}

	private static IEnumerator CoOpen(VesselConfirmMinigame minigame)
	{
		while (ExileController.Instance)
		{
			yield return new WaitForSeconds(0.65f);
		}

		minigame.gameObject.SetActive(true);
		minigame.Begin();
	}

	public override void Close()
	{
		HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(_bgColor, Color.clear));
		MinigameStubs.Close(this);
	}

	private void Begin()
	{
		HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, _bgColor));

		TitleText.gameObject.SetActive(true);
		RoleIcon.gameObject.SetActive(true);
		PossessionText.gameObject.SetActive(true);
		Divider.SetActive(true);
		Box.SetActive(true);
		DenyButton.SetActive(true);
		AcceptButton.SetActive(true);

		DenyButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
		DenyButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityAction)(() =>
		{
			clickHandler.Invoke(false);
		}));

		AcceptButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
		AcceptButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityAction)(() =>
		{
			clickHandler.Invoke(true);
		}));

		TransType = TransitionType.Alpha;
		Timer = MaxTime;
		TimerActive = true;
		Begin(null);
	}
}
