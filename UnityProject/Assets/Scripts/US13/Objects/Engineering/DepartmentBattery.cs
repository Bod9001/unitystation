using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Managers;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Electricity.NodeModules;

namespace US13.Objects.Engineering
{
	public enum BatteryStateSprite
	{
		Full,
		Half,
		Empty,
	}

	public class DepartmentBattery : NetworkBehaviour, ICheckedInteractable<HandApply>, INodeControl, ICheckedInteractable<AiActivate>, IExaminable
	{
		public DepartmentBatterySprite CurrentSprite = DepartmentBatterySprite.Default;
		public SpriteRenderer Renderer;

		public Sprite BatteryOpenPresent;
		public Sprite BatteryOpenMissing;
		public Sprite BatteryClosedMissing;

		public Sprite BatteryCharged;
		public Sprite PartialCharge;
		[SyncVar(hook = nameof(UpdateBattery))]
		public BatteryStateSprite CurrentState;

		public Sprite LightOn;
		public Sprite LightOff;
		public Sprite LightRed;

		public SpriteRenderer BatteryCompartmentSprite;
		public SpriteRenderer BatteryIndicatorSprite;
		public SpriteRenderer PowerIndicator;

		public List<DepartmentBatterySprite> enums;
		public List<Sprite> Sprite;
		public Dictionary<DepartmentBatterySprite, Sprite> Sprites = new Dictionary<DepartmentBatterySprite, Sprite>();

		public ElectricalNodeControl ElectricalNodeControl;
		public BatterySupplyingModule BatterySupplyingModule;
		private float MaxCharge => BatterySupplyingModule.CapacityMax;
		private float CurrentCharge => BatterySupplyingModule.GetSetCurrentCapacity;
		private int ChargePercent => Mathf.RoundToInt(CurrentCharge * 100 / MaxCharge);
		private bool IsCharging => BatterySupplyingModule.ChargingWatts > 10f;
		public event Action<PowerState, PowerState> OnStateChangeEvent;
		private PowerState currentState = PowerState.Off;

		[SyncVar(hook = nameof(UpdateState))]
		public bool isOn = true;

		private bool hasInit;


		private void Awake()
		{
			ElectricalNodeControl = this.GetComponent<ElectricalNodeControl>();
			BatterySupplyingModule = this.GetComponent<BatterySupplyingModule>();
		}

		private void Start()
		{
			EnsureInit();
		}

		private void OnValidate()
		{
			if (enums.Count > 0)
			{
				for (int i = 0; i < enums.Count; i++)
				{
					Sprites[enums[i]] = Sprite[i];
				}
				Renderer.sprite = Sprites[CurrentSprite];
			}
		}

		private void EnsureInit()
		{
			if (hasInit) return;
			for (int i = 0; i < enums.Count; i++)
			{
				Sprites[enums[i]] = Sprite[i];
			}

			if (enums.Count > 0)
			{
				Renderer.sprite = Sprites[CurrentSprite];
			}

			hasInit = true;
			if (isServer)
			{
				UpdateServerState();
			}
		}

		public override void OnStartClient()
		{
			EnsureInit();
			base.OnStartClient();
			UpdateState(isOn, isOn);
		}

		public void PowerNetworkUpdate()
		{
			BatteryStateSprite newState;

			var Capacity = BatterySupplyingModule.GetSetCurrentCapacity;

			if (Capacity <= 0)
			{
				newState = BatteryStateSprite.Empty;
			}
			else if (Capacity <= (BatterySupplyingModule.CapacityMax / 2))
			{
				newState = BatteryStateSprite.Half;
			}
			else
			{
				newState = BatteryStateSprite.Full;
			}

			if (CurrentState != newState)
			{
				UpdateBattery(CurrentState, newState);
			}
			SetPowerStateFromVoltage();
		}

		public string Examine(Vector3 worldPos = default)
		{
			return $"The charge indicator shows a {ChargePercent} percent charge. " +
			       $"The input level is: {BatterySupplyingModule.InputLevel} % The output level is: {BatterySupplyingModule.OutputLevel} %. " +
			       $"The power input/output is " +
			       $"enabled, and it seems to {(IsCharging ? "be" : "not be")} charging. " +
			       "Use a crowbar to adjust the output level and a wrench to adjust the input level.";
		}


		public PowerState SetPowerStateFromVoltage()
		{
			PowerState newState = currentState;
			float chargeFraction = BatterySupplyingModule.GetSetCurrentCapacity / BatterySupplyingModule.CapacityMax;

			if (chargeFraction <= 0.01f) newState = PowerState.Off;
			else if (chargeFraction <= 0.1f) newState = PowerState.LowVoltage;
			else if (chargeFraction >= 1.05f) newState = PowerState.LowVoltage;
			else newState = PowerState.On;

			if (newState == currentState) return currentState;
			OnStateChangeEvent?.Invoke(currentState, newState);
			currentState = newState;
			return currentState;
		}

		private void UpdateBattery(BatteryStateSprite oldState, BatteryStateSprite State)
		{
			EnsureInit();
			CurrentState = State;

			if (BatteryIndicatorSprite == null) return;

			switch (CurrentState)
			{
				case BatteryStateSprite.Full:
					if (BatteryIndicatorSprite.enabled == false)
					{
						BatteryIndicatorSprite.enabled = true;
					}
					BatteryIndicatorSprite.sprite = BatteryCharged;
					break;
				case BatteryStateSprite.Half:
					if (BatteryIndicatorSprite.enabled == false)
					{
						BatteryIndicatorSprite.enabled = true;
					}
					BatteryIndicatorSprite.sprite = PartialCharge;
					break;
				case BatteryStateSprite.Empty:
					BatteryIndicatorSprite.enabled = false;
					break;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.HandObject != null) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			isOn = !isOn;
			UpdateServerState();
		}

		public void UpdateServerState()
		{
			if (isOn)
			{
				ElectricalNodeControl.TurnOnSupply();
			}
			else
			{
				ElectricalNodeControl.TurnOffSupply();
			}
		}

		public void UpdateState(bool _wasOn, bool _isOn)
		{
			EnsureInit();
			isOn = _isOn;
			if (isOn)
			{
				PowerIndicator.sprite = LightOn;
			}
			else
			{
				PowerIndicator.sprite = LightOff;
			}
		}

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			isOn = !isOn;
			UpdateServerState();
		}

		#endregion
	}
}