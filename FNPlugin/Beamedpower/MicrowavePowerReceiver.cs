﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FNPlugin.Propulsion;
using FNPlugin.Extensions;
using FNPlugin.Microwave;

namespace FNPlugin
{
    class MonitorData
    {
        public Guid partId { get; set; }
        public double spotsize { get; set; }
        public double receivedPower { get; set; }
        public double distanceToSpot { get; set; }
        public double aperture { get; set; }
        public float wavelength { get; set; }

        public Vessel receivingVessel { get; set; }

        public Vessel sendingVessel { get; set; }
    }

    class SolarBeamedPowerReceiverDish : MicrowavePowerReceiver { } // receives less of a power cpacity nerve in NF mode

    class SolarBeamedPowerReceiver : MicrowavePowerReceiver {} // receives less of a power cpacity nerve in NF mode

    class MicrowavePowerReceiverDish: MicrowavePowerReceiver  {} // tweakscales with exponent 2.25

    [KSPModule("Beamed Power Receiver")]
    class MicrowavePowerReceiver : FNResourceSuppliableModule, IPowerSource, IElectricPowerGeneratorSource // tweakscales with exponent 2.5
    {
        //Persistent True
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Bandwidth")]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedBandwidthConfiguration = 0;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Enabled")]
        public bool receiverIsEnabled;
        [KSPField(isPersistant = true)]
        public double storedTemp;

        [KSPField(isPersistant = true)]
        public bool animatonDeployed = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Linked for Relay")]
        public bool linkedForRelay;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Mode"), UI_Toggle(disabledText = "Electric", enabledText = "Thermal")]
        public bool thermalMode = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Function"), UI_Toggle(disabledText = "Beamed Power", enabledText = "Radiator")]
        public bool radiatorMode = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Mode"), UI_Toggle(disabledText = "Beamed Power", enabledText = "Solar Only")]
        public bool solarPowerMode = true;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Receive Efficiency", guiUnits = "%", guiFormat = "F0")]
        public double efficiencyPercentage = GameConstants.microwave_dish_efficiency;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Target Wavelength", guiFormat = "F5")]
        public double targetWavelength = 0;
        [KSPField(isPersistant = true)]
        public bool forceActivateAtStartup = false;

        //Persistent False
        [KSPField(isPersistant = false)]
        public bool autoDeploy = true; 
        [KSPField(isPersistant = false)]
        public int supportedPropellantAtoms = 121;
        [KSPField(isPersistant = false)]
        public int supportedPropellantTypes = 127;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Electric Wasteheat Exponent")]
        public double electricWasteheatExponent = 1;
        [KSPField(isPersistant = false)]
        public double electricMaxEfficiency = 1;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Wasteheat Ratio", guiFormat = "F6")]
        public double wasteheatRatio;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Wasteheat Electric Efficiency", guiFormat = "F6")]
        public double wasteheatElectricConversionEfficiency;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Beamed Electric Efficiency", guiFormat = "F6")]
        public double effectiveBeamedPowerElectricEfficiency;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Solar Electric Efficiency", guiFormat = "F6")]
        public double effectiveSolarThermalElectricEfficiency;

        [KSPField(isPersistant = false, guiActive = false, guiName = "instance ID")]
        public int instanceId;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public double facingThreshold = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public double facingSurfaceExponent = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public double facingEfficiencyExponent = 0.1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public double spotsizeNormalizationExponent = 1;
        [KSPField(isPersistant = false)]
        public bool canLinkup = true;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Solar Efficiency", guiFormat = "F4")]
        public double solarReceptionEfficiency = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Solar Efficiency", guiFormat = "F4")]
        public double solarElectricEfficiency = 0.33;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Solar Surface Area", guiFormat = "F2")]
        public double solarReceptionSurfaceArea = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "SolarFacing Exponent", guiFormat = "F2")]
        public double solarFacingExponent = 1;

        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false)]
        public string animTName;
        [KSPField(isPersistant = false)]
        public string animGenericName;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Receiver Diameter", guiUnits = " m")]
        public float diameter = 1;
        [KSPField(isPersistant = false)]
        public bool isThermalReceiver = false;
        [KSPField(isPersistant = false)]
        public bool isEnergyReceiver = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Is Slave")]
        public bool isThermalReceiverSlave = false;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Input Power", guiFormat = "F3", guiUnits = " MJ")]
        public double powerInputMegajoules = 0;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Max Input Power", guiFormat = "F3", guiUnits = " MJ")]
        public double powerInputMegajoulesMax = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Effective Thermal Power", guiFormat = "F3", guiUnits = " MJ")]
        public double ThermalPower;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Radius", guiUnits = " m")]
        public double radius = 2.5;
        [KSPField(isPersistant = false)]
        public float alternatorRatio = 1;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "min Wavelength")]
        public double minimumWavelength = 0.00000001;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "max Wavelength")]
        public double maximumWavelength = 1;

        [KSPField(isPersistant = false)]
        public double heatTransportationEfficiency = 0.7;
        [KSPField(isPersistant = false)]
        public double powerHeatExponent = 0.7;
        [KSPField(isPersistant = false)]
        public double powerHeatBase = 3200;
        [KSPField(isPersistant = false)]
        public int receiverType = 0;
        [KSPField(isPersistant = false)]
        public double receiverFracionBonus = 0;
        [KSPField(isPersistant = false)]
        public double wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public double apertureMultiplier = 1;
        [KSPField(isPersistant = false)]
        public double highSpeedAtmosphereFactor = 0;
        [KSPField(isPersistant = false)]
        public double atmosphereToleranceModifier = 1;
        [KSPField(isPersistant = false)]
        public double thermalPropulsionEfficiency = 1;
        [KSPField(isPersistant = false)]
        public double thermalEnergyEfficiency = 1;
        [KSPField(isPersistant = false)]
        public double chargedParticleEnergyEfficiency = 1;
        [KSPField(isPersistant = false)]
        public double thermalProcessingModifier = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public bool canSwitchBandwidthInEditor = false;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public bool canSwitchBandwidthInFlight = false;
        [KSPField(isPersistant = false)]
        public string bandWidthName;

        [KSPField(isPersistant = false)]
        public int connectStackdepth = 4;
        [KSPField(isPersistant = false)]
        public int connectParentdepth = 2;
        [KSPField(isPersistant = false)]
        public int connectSurfacedepth = 2;

        //GUI
        [KSPField(isPersistant = true, guiActive = true, guiName = "Reception"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 100, minValue = 1)]
        public float receiptPower = 100;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Direct Wavelengths")]
        public int directWavelengths;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Facing Factor", guiFormat = "F5")]
        public double effectivefacingFactor;
        //[KSPField(isPersistant = false, guiActive = false, guiName = "Spot Size(s)")]
        //public string effectiveSpotSize;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Distance Effectivity", guiFormat = "F4")]
        public double effectiveDistanceFacingEfficiency;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Atmosphere Efficiency", guiFormat = "F4")]
        public double effectiveAtmosphereEfficency;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Transmit Efficiency", guiFormat = "F4")]
        public double effectiveTransmitterEfficency;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Core Temperature")]
        public string coreTempererature;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Produced Power")]
        public string beamedpower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Satellites Connected")]
        public string connectedsats;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Relays Connected")]
        public string connectedrelays;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Network Depth")]
        public string networkDepthString;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Recieve Efficiency")]
        public string toteff;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Connected Slaves")]
        public int slavesAmount;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Maximum Input Power", guiUnits = " MW", guiFormat = "F2")]
        public double maximumPower = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Maximum Electric Power", guiUnits = " MW", guiFormat = "F2")]
        public double maximumElectricPower = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Maximum Thermal Power", guiUnits = " MW", guiFormat = "F2")]
        public double maximumThermalPower = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Power Source", guiFormat = "F2", guiUnits = "MW")]
        public double maxAvailablePowerFromSource;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Route Efficiency", guiFormat = "F4")]
        public double routeEfficiency;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Other Power Usage", guiFormat = "F2", guiUnits = " MW")]
        public double currentPowerUsageByOtherRecievers;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Remaining Beamed Power", guiFormat = "F2", guiUnits = " MW")]
        public double remainingPowerFromSource;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Sun Facing Factor", guiFormat = "F4")]
        public double solarFacingFactor;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Solar Flux", guiFormat = "F4")]
        public double solarFlux;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Buffer Power", guiFormat = "F4", guiUnits = " MW")]
        public double partBaseMegajoules;

        [KSPField(isPersistant = false, guiActive = true, guiName = "FlowRate", guiFormat = "F4")]
        public double flowRate;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public double kerbalismPowerOutput;

        [KSPField(isPersistant = false)]
        public double powerMult = 1;
        [KSPField(isPersistant = false)]
        public double powerHeatMultiplier = 1;

        protected double previousDeltaTime;
        protected double previousInputMegajoules = 0;
        protected double total_beamed_power = 0;
        protected double total_beamed_power_max = 0;
        protected double total_beamed_wasteheat = 0;

        protected BaseField _radiusField;
        protected BaseField _coreTempereratureField;
        protected BaseField _field_kerbalism_output;

        protected BaseEvent _linkReceiverBaseEvent;
        protected BaseEvent _unlinkReceiverBaseEvent;
        protected BaseEvent _activateReceiverBaseEvent;
        protected BaseEvent _disableReceiverBaseEvent;

        protected ModuleDeployableSolarPanel deployableSolarPanel;
        protected ModuleDeployableRadiator deployableRadiator;
        protected ModuleDeployableAntenna deployableAntenna;

        protected ModuleActiveRadiator activeRadiator;
        protected FNRadiator fnRadiator;
        protected PartModule warpfixer;
        
        public Queue<double> solarFluxQueue = new Queue<double>(50);
        public Queue<double> flowRateQueue = new Queue<double>(50);

        //Internal 
        protected bool isLoaded = false;
        protected bool waitForAnimationToComplete = false;
        protected double total_conversion_waste_heat_production;
        protected double connectedRecieversSum;
        protected int initializationCountdown;



        protected Dictionary<Vessel, double> received_power = new Dictionary<Vessel, double>();
        protected List<MicrowavePowerReceiver> thermalReceiverSlaves = new List<MicrowavePowerReceiver>();

        // reference types
        protected Dictionary<Guid, double> connectedRecievers = new Dictionary<Guid, double>();
        protected Dictionary<Guid, double> connectedRecieversFraction = new Dictionary<Guid, double>();

        protected Dictionary<Guid, MonitorData> _monitorDataStore = new Dictionary<Guid,MonitorData>();

        protected double storedIsThermalEnergyGenratorActive;
        protected double currentIsThermalEnergyGenratorActive;

        protected GUIStyle bold_black_style;
        protected GUIStyle text_black_style;

        public float windowPositionX = 200;
        public float windowPositionY = 100;

        private const int labelWidth = 200;
        private const int valueWidth = 100;

        // GUI elements declaration
        private Rect windowPosition;
        private int windowID;
        private bool _render_window;

        public Part Part { get { return this.part; } }

        public int ProviderPowerPriority { get { return 1; } }

        public double ProducedThermalHeat { get { return powerInputMegajoules + solarInputMegajoules; } }

        private double _requestedThermalHeat;
        public double RequestedThermalHeat
        {
            get { return _requestedThermalHeat; }
            set { _requestedThermalHeat = value; }
        }

        public double PowerRatio { get { return receiptPower / 100.0; } }

        public double PowerCapacityEfficiency
        {
            get 
            { 
                return HighLogic.LoadedSceneIsFlight 
                    ? CheatOptions.IgnoreMaxTemperature 
                        ? 1 
                        : (1 - getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT)) 
                    : 1; 
            }
        }

        public void FindAndAttachToPowerSource()
        {
            // do nothing
        }

        public double WasteheatElectricConversionEfficiency
        {
            get 
            {
                if (!HighLogic.LoadedSceneIsFlight || CheatOptions.IgnoreMaxTemperature || electricWasteheatExponent == 0) return 1;

                if (electricWasteheatExponent == 1)
                    return 1 - wasteheatRatio;
                else
                    return 1 -  Math.Pow(wasteheatRatio, electricWasteheatExponent);
            }
        }

        public double MaximumRecievePower
        {
            get
            {
                var maxPower = thermalMode && maximumThermalPower > 0 
                    ? maximumThermalPower 
                    : maximumElectricPower > 0 
                        ? maximumElectricPower 
                        : maximumPower;

                var scaledPower = maxPower * powerMult;
                return CanBeActiveInAtmosphere ? scaledPower : scaledPower * highSpeedAtmosphereFactor;
            }
        }

        public void RegisterAsSlave(MicrowavePowerReceiver receiver)
        {
            thermalReceiverSlaves.Add(receiver);
        }

        public bool SupportMHD { get { return false; } }

        public double MinimumThrottle { get { return 0; } }

        public void ConnectWithEngine(IEngineNoozle engine) { }

        public void DisconnectWithEngine(IEngineNoozle engine) { }

        public int SupportedPropellantAtoms { get { return supportedPropellantAtoms; } }

        public int SupportedPropellantTypes { get { return supportedPropellantTypes; } }

        public bool FullPowerForNonNeutronAbsorbants { get { return true; } }

        public double ReactorSpeedMult { get { return 1; } }

        public double ThermalProcessingModifier { get { return thermalProcessingModifier; } }

        public double EfficencyConnectedThermalEnergyGenerator { get { return storedIsThermalEnergyGenratorActive; } }

        public double EfficencyConnectedChargedEnergyGenerator { get { return 0; } }

        public IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        public IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio, ElectricGeneratorType generatorType)
        {
            currentIsThermalEnergyGenratorActive = efficency;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio, ElectricGeneratorType generatorType) { }

        public bool IsThermalSource
        {
            get { return this.isThermalReceiver; }
        }

        public double RawMaximumPower { get { return MaximumRecievePower; } }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType) { return false; }

        public void AttachThermalReciever(Guid key, double radius)
        {
            try
            {
                if (!connectedRecievers.ContainsKey(key))
                {
                    connectedRecievers.Add(key, radius);
                    connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                    connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
                }
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[KSPI] - InterstellarReactor.ConnectReciever exception: " + error.Message);
            }
        }

        public double ProducedWasteHeat { get { return total_conversion_waste_heat_production; } }

        public void Refresh() { }

        public void DetachThermalReciever(Guid key)
        {
            if (connectedRecievers.ContainsKey(key))
            {
                connectedRecievers.Remove(key);
                connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
            }
        }

        public double GetFractionThermalReciever(Guid key)
        {
            double result;
            if (connectedRecieversFraction.TryGetValue(key, out result))
                return result;
            else
                return 0;
        }

        protected PartResource wasteheatResource;
        protected PartResource megajouleResource;
        protected PartResource electricResource;
        protected PartResource thermalResource;

        protected Animation animation;
        protected Animation animT;

        protected MicrowavePowerTransmitter part_transmitter;
        protected ModuleAnimateGeneric genericAnimation;

        protected CelestialBody localStar;

        protected int connectedsatsi = 0;
        protected int connectedrelaysi = 0;
        protected int networkDepth = 0;
        protected long deactivate_timer = 0;
        protected double solarInputMegajoules = 0;
        protected double solarInputMegajoulesMax = 0;
        
        protected double partBaseWasteheat;

        protected bool has_transmitter = false;

        private static readonly double microwaveAngleTan = Math.Tan(GameConstants.microwave_angle);//this doesn't change during game so it's readonly 
        private static readonly double microwaveAngleTanSquared = microwaveAngleTan * microwaveAngleTan;

        public double RawTotalPowerProduced { get { return ThermalPower * TimeWarp.fixedDeltaTime; } }

        public double ChargedPowerRatio { get { return 0; } }

        public double PowerBufferBonus { get { return 0; } }

        public double ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }

        public double ThermalEnergyEfficiency { get { return thermalEnergyEfficiency; } }

        public double ThermalPropulsionEfficiency { get { return thermalPropulsionEfficiency; } }

        public double ChargedParticleEnergyEfficiency { get { return 0; } }

        public double ChargedParticlePropulsionEfficiency { get { return 0; } }

        public bool IsSelfContained { get { return false; } }

        public double CoreTemperature { get { return powerHeatBase; } }

        public double HotBathTemperature { get { return CoreTemperature; } }

        public double StableMaximumReactorPower { get { return RawMaximumPower; } }

        public double MaximumPower { get { return MaximumThermalPower; } }

        public double MaximumThermalPower { get { return ThermalPower; } }

        public double NormalisedMaximumPower { get { return ThermalPower; } }

        public double MaximumChargedPower { get { return 0; } }

        public double MinimumPower { get { return 0; } }

        public bool IsVolatileSource { get { return true; } }

        public bool IsActive { get { return receiverIsEnabled; } }

        public bool IsNuclear { get { return false; } }

        [KSPEvent(guiActive = true, guiName = "Link Receiver for Relay", active = true)]
        public void LinkReceiver()
        {
            linkedForRelay = true;

            ShowDeployAnimation(true);
        }

        [KSPEvent(guiActive = true, guiName = "Unlink Receiver for Relay", active = true)]
        public void UnlinkReceiver()
        {
            linkedForRelay = false;

            ShowUndeployAnimation(true);
        }


        [KSPEvent(guiActive = true, guiName = "Activate Receiver", active = true)]
        public void ActivateReceiver()
        {
            ActivateRecieverState();
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Receiver Interface", active = true)]
        public void ToggleWindow()
        {
            _render_window = !_render_window;
        }

        private void ActivateRecieverState(bool forced = false)
        {
            receiverIsEnabled = true;

            // force activate to trigger any fairings and generators

            Debug.Log("MicrowaveReceiver Force Activate ");
            this.part.force_activate();
            forceActivateAtStartup = true;

            ShowDeployAnimation(forced);
        }

        private void ShowDeployAnimation(bool forced)
        {
            Debug.Log("MicrowaveReceiver ShowDeployAnimation is called ");

            if (deployableAntenna != null)
            {
                deployableAntenna.Extend();
            }

            if (deployableSolarPanel != null)
            {
                deployableSolarPanel.Extend();
            }

            if (deployableRadiator != null)
            {
                deployableRadiator.Extend();
            }

            if (animation != null)
            {
                if (forced || !animatonDeployed)
                {
                    waitForAnimationToComplete = true;
                    animatonDeployed = true;

                    if (animation[animName].normalizedTime == 1)
                        animation[animName].normalizedTime = 0;

                    animation[animName].speed = 1;
                    animation.Blend(animName, 2);
                }
            }

            if (genericAnimation != null && genericAnimation.GetScalar < 1)
            {
                genericAnimation.Toggle();
            }

            if (fnRadiator != null && fnRadiator.ModuleActiveRadiator != null)
                fnRadiator.ModuleActiveRadiator.Activate();
        }

        [KSPEvent(guiActive = true, guiName = "Disable Receiver", active = true)]
        public void DisableReceiver()
        {
            DeactivateRecieverState();
        }

        private void DeactivateRecieverState(bool forced = false)
        {
            receiverIsEnabled = false;

            ShowUndeployAnimation(forced);
        }

        private void ShowUndeployAnimation(bool forced)
        {
            if (deployableAntenna != null)
            {
                deployableAntenna.Retract();
            }

            if (deployableSolarPanel != null)
            {
                deployableSolarPanel.Retract();
            }

            if (deployableRadiator != null)
            {
                deployableRadiator.Retract();
            }

            if (animation != null)
            {
                if (forced || animatonDeployed)
                {
                    waitForAnimationToComplete = true;
                    animatonDeployed = false;

                    if (animation[animName].normalizedTime == 0)
                        animation[animName].normalizedTime = 1;

                    animation[animName].speed = -1;
                    animation.Blend(animName, 2);
                }
            }

            if (genericAnimation != null && genericAnimation.GetScalar > 0 )
            {
                genericAnimation.Toggle();
            }

            if (fnRadiator != null && fnRadiator.ModuleActiveRadiator != null)
                fnRadiator.ModuleActiveRadiator.Shutdown();
        }

        [KSPAction("Activate Receiver")]
        public void ActivateReceiverAction(KSPActionParam param)
        {
            ActivateReceiver();
        }

        [KSPAction("Disable Receiver")]
        public void DisableReceiverAction(KSPActionParam param)
        {
            DisableReceiver();
        }

        [KSPAction("Toggle Receiver")]
        public void ToggleReceiverAction(KSPActionParam param)
        {
            if (receiverIsEnabled)
                DisableReceiver();
            else
                ActivateReceiver();
        }

        private BandwidthConverter activeBandwidthConfiguration;

        private List<BandwidthConverter> _bandwidthConverters;
        public  List<BandwidthConverter> BandwidthConverters
        {
            get 
            {
                if (_bandwidthConverters != null)
                    return _bandwidthConverters;

                var availableBandwithConverters = part.FindModulesImplementing<BandwidthConverter>().Where(m => PluginHelper.HasTechRequirementOrEmpty(m.techRequirement0));

                _bandwidthConverters = availableBandwithConverters.OrderByDescending(m => m.TargetWavelength).ToList();

                // initialize maximum tech level
                _bandwidthConverters.ForEach(b => b.Initialize());

                return _bandwidthConverters;
            }
        }

        public static double PhotonicLaserMomentum(double Lambda, uint Time, ulong Wattage)//Lamdba= Wavelength in nanometers, Time in seconds, Wattage in normal Watts, returns momentum of whole laser
        {
            double EnergySingle = 6.626e-34 * 3e8 / Lambda;
            double PhotonImpulse = Wattage * Time / EnergySingle;
            double MomentumSingle = 6.626e-34 / Lambda;
            double MomentumWhole = MomentumSingle * PhotonImpulse;

            return 2 * MomentumWhole; //output is in Newtons per second
        }

        public static double PhotonicLaserMomentum2(double Lambda, int Time, long Wattage)//Lamdba= Wavelength in nanometers, Time in seconds, Wattage in normal Watts, returns momentum of whole laser
        {
            double PhotonImpulse;
            double EnergyLaser = Wattage * Time;
            double EnergySingle;
            double MomentumSingle;
            double relativisticMassSingle;
            double MomentumWhole;
            relativisticMassSingle = 6.626e-34 / (3e8 * Lambda);

            EnergySingle = 6.626e-34 * 3e8 / Lambda;
            PhotonImpulse = EnergyLaser / EnergySingle;
            MomentumSingle = (6.626e-34 / (3e8 * Lambda)) * 3e8;
            MomentumWhole = MomentumSingle * PhotonImpulse;

            return 2 * MomentumWhole;
        }

        public override void OnStart(PartModule.StartState state)
        {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_MEGAJOULES, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_THERMALPOWER };

            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            // while in edit mode, listen to on attach/detach event
            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
            }

            InitializeThermalModeSwitcher();

            InitializeBrandwitdhSelector();

            instanceId = GetInstanceID();

            _linkReceiverBaseEvent = Events["LinkReceiver"];
            _unlinkReceiverBaseEvent = Events["UnlinkReceiver"];
            _activateReceiverBaseEvent = Events["ActivateReceiver"];
            _disableReceiverBaseEvent = Events["DisableReceiver"];

            _radiusField = Fields["radius"];

            coreTempererature = CoreTemperature.ToString("0.0") + " K";
            _coreTempereratureField = Fields["coreTempererature"];

            if (part.Modules.Contains("WarpFixer"))
            {
                warpfixer = part.Modules["WarpFixer"];
                _field_kerbalism_output = warpfixer.Fields["field_output"];
            }

            if (IsThermalSource && !isThermalReceiverSlave)
            {
                _radiusField.guiActive = true;
                _radiusField.guiActiveEditor = true;
                _coreTempereratureField.guiActive = true;
                _coreTempereratureField.guiActiveEditor = true;
            }
            else
            {
                _radiusField.guiActive = false;
                _radiusField.guiActiveEditor = false;
                _coreTempereratureField.guiActive = false;
                _coreTempereratureField.guiActiveEditor = false;
            }

            // Determine currently maximum and minimum wavelength
            if (BandwidthConverters.Any())
            {
                if (canSwitchBandwidthInEditor)
                {
                    minimumWavelength = activeBandwidthConfiguration.minimumWavelength;
                    maximumWavelength = activeBandwidthConfiguration.maximumWavelength;
                }
                else
                {
                    minimumWavelength = BandwidthConverters.Min(m => m.minimumWavelength);
                    maximumWavelength = BandwidthConverters.Max(m => m.maximumWavelength);
                }
            }

            deployableAntenna = part.FindModuleImplementing<ModuleDeployableAntenna>();
            if (deployableAntenna != null)
            {
                try
                {
                    deployableAntenna.Events["Extend"].guiActive = false;
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI] - Error while disabling antenna deploy button " + e.Message + " at " + e.StackTrace);
                }
            }

            deployableSolarPanel = part.FindModuleImplementing<ModuleDeployableSolarPanel>();
            if (deployableSolarPanel != null)
            {
                try
                {
                    deployableSolarPanel.Events["Extend"].guiActive = false;
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI] - Error while disabling solar deploy button " + e.Message + " at " + e.StackTrace);
                }
            }

            var isInSolarModeField = Fields["solarPowerMode"];
            isInSolarModeField.guiActive = deployableSolarPanel != null;
            isInSolarModeField.guiActiveEditor = deployableSolarPanel != null;

            if (deployableSolarPanel == null)
                solarPowerMode = false;

            fnRadiator = part.FindModuleImplementing<FNRadiator>();
            if (fnRadiator != null)
            {
                _activateReceiverBaseEvent.guiName = "Deploy";
                _disableReceiverBaseEvent.guiName = "Retract";
                fnRadiator.showControls = false;
                fnRadiator.canRadiateHeat = radiatorMode;
                fnRadiator.radiatorIsEnabled = radiatorMode;
            }

            var isInRatiatorMode = Fields["radiatorMode"];
            isInRatiatorMode.guiActive = fnRadiator != null;
            isInRatiatorMode.guiActiveEditor = fnRadiator != null;

            if (state == StartState.Editor) { return; }

            // create the id for the GUI window

            windowPosition = new Rect(windowPositionX, windowPositionY, labelWidth + valueWidth * 5, 100);
            windowID = new System.Random(part.GetInstanceID()).Next(int.MinValue, int.MaxValue);

            localStar = GetCurrentStar();

            // compensate for stock solar initialisation heating bug
            initializationCountdown = 10;

            if (forceActivateAtStartup)
                part.force_activate();

            if (isThermalReceiverSlave)
            {
                var result = PowerSourceSearchResult.BreadthFirstSearchForThermalSource(this.part, (s) => s is MicrowavePowerReceiver && (MicrowavePowerReceiver)s != this, connectStackdepth, connectParentdepth, connectSurfacedepth, true);

                if (result == null || result.Source == null)
                    UnityEngine.Debug.LogWarning("[KSPI] - MicrowavePowerReceiver - BreadthFirstSearchForThermalSource-Failed to find thermal receiver");
                else
                    ((MicrowavePowerReceiver)(result.Source)).RegisterAsSlave(this);
            }

            wasteheatResource = part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT];
            megajouleResource = part.Resources[FNResourceManager.FNRESOURCE_MEGAJOULES];
            thermalResource = part.Resources[FNResourceManager.FNRESOURCE_THERMALPOWER];
            electricResource = part.Resources[FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE];

            // calculate WasteHeat Capacity
            partBaseWasteheat = part.mass * 2.0e+5 * wasteHeatMultiplier;

            // calculate Power Capacity buffer
            partBaseMegajoules = StableMaximumReactorPower * 0.05;

            // initialize power buffers
            UpdateBuffers(powerInputMegajoules);

            // look for any transmitter partmodule
            part_transmitter = part.FindModuleImplementing<MicrowavePowerTransmitter>();
            if (part_transmitter != null)
            {
                has_transmitter = true;
            }

            activeRadiator = part.FindModuleImplementing<ModuleActiveRadiator>();
            deployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
            if (deployableRadiator != null)
            {
                try
                {
                    deployableRadiator.Events["Extend"].guiActive = false;
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI] - Error while disabling radiator button " + e.Message + " at " + e.StackTrace);
                }
            }

            if (!String.IsNullOrEmpty(animTName))
            {
                animT = part.FindModelAnimators(animTName).FirstOrDefault();
                if (animT != null)
                {
                    animT[animTName].enabled = true;
                    animT[animTName].layer = 1;
                    animT[animTName].normalizedTime = 0;
                    animT[animTName].speed = 0.001f;

                    animT.Sample();
                }
            }

            if (!String.IsNullOrEmpty(animGenericName))
                genericAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == animGenericName);

            if (deployableAntenna == null && deployableSolarPanel == null && deployableRadiator == null && genericAnimation == null && !String.IsNullOrEmpty(animName))
                animation = part.FindModelAnimators(animName).FirstOrDefault();
        }

        private void UpdateBuffers(double inputPower)
        {
            try
            {

                if (wasteheatResource != null && TimeWarp.fixedDeltaTime != previousDeltaTime)
                {
                    var ratio = Math.Min(1, wasteheatResource.amount / wasteheatResource.maxAmount);
                    wasteheatResource.maxAmount = partBaseWasteheat * TimeWarp.fixedDeltaTime; ;
                    wasteheatResource.amount = wasteheatResource.maxAmount * ratio;
                }

                if (inputPower > 0)
                {
                    if (thermalResource != null)
                    {
                        var ratio = Math.Min(1, thermalResource.amount / thermalResource.maxAmount);
                        thermalResource.maxAmount = inputPower * TimeWarp.fixedDeltaTime;
                        thermalResource.amount = thermalResource.maxAmount * ratio;
                    }

                    if (megajouleResource != null)
                    {
                        var ratio = Math.Min(1, megajouleResource.amount / megajouleResource.maxAmount);
                        megajouleResource.maxAmount = inputPower * TimeWarp.fixedDeltaTime;
                        megajouleResource.amount = megajouleResource.maxAmount * ratio;
                    }
                    if (electricResource != null)
                    {
                        var ratio = Math.Min(1, electricResource.amount / electricResource.maxAmount);
                        electricResource.maxAmount = inputPower * TimeWarp.fixedDeltaTime;
                        electricResource.amount = electricResource.maxAmount * ratio;
                    }
                }
                else
                {
                    if (thermalResource != null && thermalResource.maxAmount > 0.001)
                    {
                        var ratio = Math.Min(1, thermalResource.amount / thermalResource.maxAmount);
                        thermalResource.maxAmount = Math.Min(StableMaximumReactorPower * TimeWarp.fixedDeltaTime, thermalResource.maxAmount * 0.99);
                        thermalResource.amount = Math.Min(thermalResource.maxAmount, thermalResource.amount);
                    }
                    if (megajouleResource != null && megajouleResource.maxAmount > 0.001)
                    {
                        var ratio = Math.Min(1, megajouleResource.amount / megajouleResource.maxAmount);
                        megajouleResource.maxAmount = Math.Min(StableMaximumReactorPower * TimeWarp.fixedDeltaTime, megajouleResource.maxAmount * 0.9);
                        megajouleResource.amount = Math.Min(megajouleResource.maxAmount, megajouleResource.amount);
                    }
                    if (electricResource != null && electricResource.maxAmount > 0.001)
                    {
                        var ratio = Math.Min(1, electricResource.amount / electricResource.maxAmount);
                        electricResource.maxAmount = Math.Min(StableMaximumReactorPower * TimeWarp.fixedDeltaTime, electricResource.maxAmount * 0.9);
                        electricResource.amount = Math.Min(electricResource.maxAmount, electricResource.amount);
                    }
                }
            }    
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - MicrowavePowerReceiver.UpdateBuffers " + e.Message);
            }
            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }

        /// <summary>
        /// Event handler called when part is attached to another part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI] - attach " + part.partInfo.title);
                foreach (var node in part.attachNodes)
                {
                    if (node.attachedPart == null) continue;

                    var generator = node.attachedPart.FindModuleImplementing<FNGenerator>();
                    if (generator != null)
                        generator.FindAndAttachToPowerSource();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - MicrowavePowerReceiver.OnEditorAttach " + e.Message);
            }
        }

        /// <summary>
        /// Event handler called when part is detached from vessel
        /// </summary>
        private void OnEditorDetach()
        {
            try
            {
                Debug.Log("[KSPI] - detach " + part.partInfo.title);
                if (ConnectedChargedParticleElectricGenerator != null)
                    ConnectedChargedParticleElectricGenerator.FindAndAttachToPowerSource();

                if (ConnectedThermalElectricGenerator != null)
                    ConnectedThermalElectricGenerator.FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Reactor.OnEditorDetach " + e.Message);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            part.temperature = storedTemp;
            part.skinTemperature = storedTemp;
        }

        private void InitializeThermalModeSwitcher()
        {
            // ensure valid values 
            if (isThermalReceiver && !isEnergyReceiver)
                thermalMode = true;
            else if (!isThermalReceiver && isEnergyReceiver)
                thermalMode = false;

            var isInThermalModeField = Fields["thermalMode"];

            isInThermalModeField.guiActive = isThermalReceiver && isEnergyReceiver;
            isInThermalModeField.guiActiveEditor = isThermalReceiver && isEnergyReceiver;
        }

        private void InitializeBrandwitdhSelector()
        {
            try
            {
                Debug.Log("[KSP Interstellar] - Setup Receiver BrandWidth Configurations for " + part.partInfo.title);

                var bandWidthNameField = Fields["bandWidthName"];
                bandWidthNameField.guiActiveEditor = !canSwitchBandwidthInEditor;
                bandWidthNameField.guiActive = !canSwitchBandwidthInFlight && canSwitchBandwidthInEditor;

                var chooseField = Fields["selectedBandwidthConfiguration"];
                chooseField.guiActiveEditor = canSwitchBandwidthInEditor;
                chooseField.guiActive = canSwitchBandwidthInFlight;

                var names = BandwidthConverters.Select(m => m.bandwidthName).ToArray();

                var chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
                chooseOptionEditor.options = names;

                var chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;
                chooseOptionFlight.options = names;

                UpdateFromGUI(chooseField, selectedBandwidthConfiguration);

                // connect on change event
                chooseOptionEditor.onFieldChanged = UpdateFromGUI;
                chooseOptionFlight.onFieldChanged = UpdateFromGUI;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Error in MicrowaveReceiver InitializeBrandwitdhSelector " + e.Message + " at " + e.StackTrace);
            }
        }

        private void LoadInitialConfiguration()
        {
            try
            {
                isLoaded = true;

                var currentWavelength = targetWavelength != 0 ? targetWavelength : 1;

                Debug.Log("[KSPI] - LoadInitialConfiguration initialize initial beam configuration with wavelength target " + currentWavelength);

                // find wavelength closes to target wavelength
                activeBandwidthConfiguration = BandwidthConverters.FirstOrDefault();
                bandWidthName = activeBandwidthConfiguration.bandwidthName;
                selectedBandwidthConfiguration = 0;
                var lowestWavelengthDifference = Math.Abs(currentWavelength - activeBandwidthConfiguration.TargetWavelength);
                if (BandwidthConverters.Any())
                {
                    foreach (var currentConfig in BandwidthConverters)
                    {
                        var configWaveLengthDifference = Math.Abs(currentWavelength - currentConfig.TargetWavelength);
                        if (configWaveLengthDifference < lowestWavelengthDifference)
                        {
                            activeBandwidthConfiguration = currentConfig;
                            lowestWavelengthDifference = configWaveLengthDifference;
                            selectedBandwidthConfiguration = BandwidthConverters.IndexOf(currentConfig);
                            bandWidthName = activeBandwidthConfiguration.bandwidthName;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Error in MicrowaveReceiver LoadInitialConfiguration " + e.Message + " at " + e.StackTrace);
            }
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            try
            {
                Debug.Log("[KSP Interstellar] UpdateFromGUI is called with selectedBandwidth " + selectedBandwidthConfiguration);

                if (!BandwidthConverters.Any())
                    return;

                Debug.Log("[KSP Interstellar] UpdateFromGUI found " + BandwidthConverters.Count + " BandwidthConverters");

                if (isLoaded == false)
                    LoadInitialConfiguration();
                else
                {
                    if (selectedBandwidthConfiguration < BandwidthConverters.Count)
                    {
                        Debug.Log("[KSP Interstellar] UpdateFromGUI selectedBeamGenerator < orderedBeamGenerators.Count");
                        activeBandwidthConfiguration = BandwidthConverters[selectedBandwidthConfiguration];
                    }
                    else
                    {
                        Debug.Log("[KSP Interstellar] UpdateFromGUI selectedBeamGenerator >= orderedBeamGenerators.Count");
                        selectedBandwidthConfiguration = BandwidthConverters.Count - 1;
                        activeBandwidthConfiguration = BandwidthConverters.Last();
                    }
                }

                if (activeBandwidthConfiguration == null)
                {
                    Debug.LogWarning("[KSP Interstellar] UpdateFromGUI failed to find BandwidthConfiguration");
                    return;
                }

                targetWavelength = activeBandwidthConfiguration.TargetWavelength;
                bandWidthName = activeBandwidthConfiguration.bandwidthName;

                // update wavelength we can receive
                if (canSwitchBandwidthInEditor)
                {
                    minimumWavelength = activeBandwidthConfiguration.minimumWavelength;
                    maximumWavelength = activeBandwidthConfiguration.maximumWavelength;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Error in MicrowaveReceiver UpdateFromGUI " + e.Message + " at " + e.StackTrace);
            }
        }

        private bool CanBeActiveInAtmosphere
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return true;

                if (deployableAntenna != null && deployableAntenna.isBreakable)
                {
                    return !deployableAntenna.ShouldBreakFromPressure();
                }
                else if (deployableRadiator != null && deployableRadiator.isBreakable)
                {
                    return !deployableRadiator.ShouldBreakFromPressure();
                }
                else if (deployableSolarPanel != null && deployableSolarPanel.isBreakable)
                {
                    return !deployableSolarPanel.ShouldBreakFromPressure();
                }
                else
                {
                    if (animation == null)
                        return true;

                    var pressure = FlightGlobals.getStaticPressure(vessel.transform.position) / 100;
                    var dynamic_pressure = 0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325;

                    if (dynamic_pressure <= 0) return true;

                    var pressureLoad = dynamic_pressure / 1.4854428818159e-3 * 100;
                    if (pressureLoad > 100 * atmosphereToleranceModifier)
                        return false;
                    else
                        return true;
                }
            }
        }

        protected CelestialBody GetCurrentStar()
        {
            int Depth = 0;
            var star = FlightGlobals.currentMainBody;
            while ((Depth < 10) && (star.GetTemperature(0) < 2000))
            {
                star = star.referenceBody;
                Depth++;
            }

            if ((star.GetTemperature(0) < 2000) || (star.name == "Galactic Core"))
                star = null;

            return star;
        }

        public override void OnUpdate()
        {
            bool transmitter_on = has_transmitter && part_transmitter.isActive();
            bool canBeActive = CanBeActiveInAtmosphere;

            _linkReceiverBaseEvent.active = canLinkup && !linkedForRelay && !receiverIsEnabled && !transmitter_on && canBeActive;
            _unlinkReceiverBaseEvent.active = linkedForRelay;
            
            _activateReceiverBaseEvent.active = !linkedForRelay && !receiverIsEnabled && !transmitter_on && canBeActive;
            _disableReceiverBaseEvent.active = receiverIsEnabled;

            var isNotRelayingOrTransmitting = !linkedForRelay && !transmitter_on;

            Fields["beamedpower"].guiActive = isNotRelayingOrTransmitting;
            Fields["powerInputMegajoules"].guiActive = isNotRelayingOrTransmitting;
            Fields["linkedForRelay"].guiActive = isNotRelayingOrTransmitting;
            Fields["diameter"].guiActive = isNotRelayingOrTransmitting;

            Fields["slavesAmount"].guiActive = thermalMode;
            Fields["ThermalPower"].guiActive = isThermalReceiverSlave || thermalMode;

            Fields["efficiencyPercentage"].guiActive = receiverIsEnabled;
            //Fields["effectiveSpotSize"].guiActive = receiverIsEnabled;
            Fields["effectivefacingFactor"].guiActive = receiverIsEnabled;
            Fields["receiptPower"].guiActive = receiverIsEnabled;
            //Fields["effectiveDistanceFacingEfficiency"].guiActive = receiverIsEnabled;
            //Fields["effectiveAtmosphereEfficency"].guiActive = receiverIsEnabled;
            Fields["effectiveTransmitterEfficency"].guiActive = receiverIsEnabled;
            Fields["maxAvailablePowerFromSource"].guiActive = receiverIsEnabled;
            Fields["routeEfficiency"].guiActive = receiverIsEnabled;
            Fields["currentPowerUsageByOtherRecievers"].guiActive = receiverIsEnabled;
            Fields["remainingPowerFromSource"].guiActive = receiverIsEnabled;          
            Fields["minimumWavelength"].guiActive = receiverIsEnabled;
            Fields["maximumWavelength"].guiActive = receiverIsEnabled;

            Fields["selectedBandwidthConfiguration"].guiActive = canSwitchBandwidthInFlight && receiverIsEnabled;

            Fields["solarFacingFactor"].guiActive = solarReceptionSurfaceArea > 0;
            Fields["solarFlux"].guiActive = solarReceptionSurfaceArea > 0;

            Fields["toteff"].guiActive = (connectedsatsi > 0 || connectedrelaysi > 0);

            if (IsThermalSource)
                coreTempererature = CoreTemperature.ToString("0.0") + " K";

            if (receiverIsEnabled)
            {
                if (ProducedThermalHeat > 1)
                    beamedpower = (ProducedThermalHeat).ToString("0.00") + "MW";
                else
                    beamedpower = (ProducedThermalHeat / 1000).ToString("0.00") + "KW";
            }
            else
                beamedpower = "Offline.";

            connectedsats = string.Format("{0}/{1}", connectedsatsi, MicrowaveSources.instance.globalTransmitters.Count);
            connectedrelays = string.Format("{0}/{1}", connectedrelaysi, MicrowaveSources.instance.globalRelays.Count);
            networkDepthString = networkDepth.ToString();
            toteff = efficiencyPercentage.ToString("0.00") + "%";



            //if (receiverIsEnabled && anim != null && (!waitForAnimationToComplete || (!anim.isPlaying && waitForAnimationToComplete)))
            //{
            //    waitForAnimationToComplete = false;

            //    if (connectedsatsi > 0 || connectedrelaysi > 0)
            //    {
            //        if (!animatonDeployed)
            //        {
            //            //ScreenMessages.PostScreenMessage("Enable Microwave Receiver Tmp", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            //            animatonDeployed = true;

            //            if (anim[animName].normalizedTime == 1f)
            //                anim[animName].normalizedTime = 0;

            //            anim[animName].speed = 1;
            //            anim.Blend(animName, 2);
            //        }
            //    }
            //    else
            //    {
            //        if (animatonDeployed)
            //        {
            //            //ScreenMessages.PostScreenMessage("Disable Microwave Receiver Tmp", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            //            animatonDeployed = false;

            //            if (anim[animName].normalizedTime == 0)
            //                anim[animName].normalizedTime = 1;

            //            anim[animName].speed = -1;
            //            anim.Blend(animName, 2);
            //        }
            //    }
            //}
        }

        private double GetSolarFacingFactor(CelestialBody localStar, Vector3 vesselPosition)
        {
            try
            {
                if (localStar == null) return 0;

                Vector3d dolarDirectionVector = (localStar.transform.position - vesselPosition).normalized;

                 if (receiverType == 3) 
                     return Math.Max(0, 1 - Vector3d.Dot(part.transform.forward, dolarDirectionVector)) / 2;
                 else
                     return Math.Max(0, Vector3d.Dot(part.transform.up, dolarDirectionVector));
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in GetSolarFacingFactor " + e.Message + " at " + e.StackTrace);
                return 0; 
            }
        }

        uint counter = 0;       // OnFixedUpdate cycle counter

        private double GetAtmosphericEfficiency(Vessel v)
        {
            return Math.Exp(-(FlightGlobals.getStaticPressure(v.transform.position) / 100) / 5);
        }

        private double GetAtmosphericEfficiency(double transmitterPresure, double recieverPressure, double waveLengthAbsorbtion, double distanceInMeter, Vessel recieverVessel, Vessel transmitterVessel) 
        {
            // if both in space, efficiency is 100%
            if (transmitterPresure == 0 && recieverPressure == 0)
                return 1;

            var atmosphereDepthInMeter = transmitterVessel.mainBody.atmosphereDepth;

            // calculate the weighted distance a signal has to travel through the atmosphere
            double atmosphericDistance;
            if (recieverVessel.mainBody == transmitterVessel.mainBody)
            {
                var recieverAltitudeModifier = recieverVessel.altitude > atmosphereDepthInMeter ? atmosphereDepthInMeter / recieverVessel.altitude : 1;
                var transmitterAltitudeModifier = transmitterVessel.altitude > atmosphereDepthInMeter ? atmosphereDepthInMeter / transmitterVessel.altitude : 1;
                atmosphericDistance = transmitterAltitudeModifier * recieverAltitudeModifier * distanceInMeter;
            }
            else
            {
                // use fixed atmospheric distance when not in the same SOI
                atmosphericDistance = atmosphereDepthInMeter * 2;
            }

            double absortion = Math.Pow(atmosphericDistance, Math.Sqrt(Math.Pow(transmitterPresure, 2) + Math.Pow(recieverPressure, 2))) / atmosphereDepthInMeter * waveLengthAbsorbtion;

            return Math.Exp(-absortion);
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor) return;

            if (!part.enabled)
                base.OnFixedUpdate();
        }

        private void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && _render_window)
                windowPosition = GUILayout.Window(windowID, windowPosition, DrawGui, "Universal Mining Interface");

        }

        private void DrawGui(int window)
        {
            windowPositionX = windowPosition.x;
            windowPositionY = windowPosition.y;

            InitializeStyles();

            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                _render_window = false;

            GUILayout.BeginVertical();

            PrintToGUILayout("Type", part.partInfo.title, bold_black_style, text_black_style);
            PrintToGUILayout("Facing Efficiency", effectiveDistanceFacingEfficiency.ToString("0.0000"), bold_black_style, text_black_style);
            PrintToGUILayout("Atmosphere Efficency", effectiveAtmosphereEfficency.ToString("0.0000"), bold_black_style, text_black_style);
            PrintToGUILayout("Transmitter Efficency", effectiveTransmitterEfficency.ToString("0.0000"), bold_black_style, text_black_style); 

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vessel", bold_black_style, GUILayout.Width(labelWidth));
            GUILayout.Label("Distance", bold_black_style, GUILayout.Width(valueWidth));
            GUILayout.Label("Aperture", bold_black_style, GUILayout.Width(valueWidth));
            GUILayout.Label("Spotsize", bold_black_style, GUILayout.Width(valueWidth));
            GUILayout.Label("Wavelength", bold_black_style, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            foreach (var monitorData in _monitorDataStore.Where(m => m.Value.receivingVessel == this.vessel))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(monitorData.Value.sendingVessel.name, text_black_style, GUILayout.Width(labelWidth));
                GUILayout.Label((monitorData.Value.distanceToSpot).ToString("0") + " m", text_black_style, GUILayout.Width(valueWidth));
                GUILayout.Label((monitorData.Value.aperture).ToString("##.######") + " m", text_black_style, GUILayout.Width(valueWidth));
                GUILayout.Label((monitorData.Value.spotsize).ToString("##.######") + " m", text_black_style, GUILayout.Width(valueWidth));
                GUILayout.Label((monitorData.Value.wavelength).ToString() + " m", text_black_style, GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void InitializeStyles()
        {
            if (bold_black_style == null)
            {
                bold_black_style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    font = PluginHelper.MainFont
                };
            }

            if (text_black_style == null)
            {
                text_black_style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    font = PluginHelper.MainFont
                };
            }
        }

        protected void PrintToGUILayout(string label, string value, GUIStyle bold_style, GUIStyle text_style, int witdhLabel = 130, int witdhValue = 130)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, bold_style, GUILayout.Width(witdhLabel));
            GUILayout.Label(value, text_style, GUILayout.Width(witdhValue));
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// FixedUpdate is only called when stage or force activated
        /// </summary>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            total_conversion_waste_heat_production = 0;
            currentIsThermalEnergyGenratorActive = 0;
            storedIsThermalEnergyGenratorActive = currentIsThermalEnergyGenratorActive;

            wasteheatRatio = CheatOptions.IgnoreMaxTemperature ? 0 : Math.Min(1, getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT));

            CalculateThermalSolarPower();

            if (initializationCountdown > 0)
            {
                initializationCountdown--;

                part.temperature = storedTemp;
                part.skinTemperature = storedTemp;
            }
            else
                storedTemp = part.temperature;

            if (receiverIsEnabled && radiatorMode)
            {
                if (fnRadiator != null)
                {
                    fnRadiator.canRadiateHeat = true;
                    fnRadiator.radiatorIsEnabled = true;
                }
                UpdateBuffers(0);
                return;
            }
            else
            {
                if (fnRadiator != null)
                {
                    fnRadiator.canRadiateHeat = false;
                    fnRadiator.radiatorIsEnabled = false;
                }
            }

            try
            {

                if (receiverIsEnabled && !radiatorMode)
                {
                    if (wasteheatRatio >= 0.95 && !isThermalReceiver)
                    {
                        receiverIsEnabled = false;
                        deactivate_timer++;
                        if (FlightGlobals.ActiveVessel == vessel && deactivate_timer > 2)
                            ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency microwave power shutdown occuring NOW!", 5f, ScreenMessageStyle.UPPER_CENTER);
                        UpdateBuffers(0);
                        return;
                    }

                    if (solarPowerMode)
                    {
                        total_beamed_power = 0;
                        total_beamed_power_max = 0;
                        total_beamed_wasteheat = 0;
                        connectedsatsi = 0;
                        connectedrelaysi = 0;
                        networkDepth = 0;

                        //add solar power tot toal power
                        powerInputMegajoules = solarInputMegajoules;
                        powerInputMegajoulesMax = solarInputMegajoulesMax;
                    }
                    else if (!solarPowerMode && (++counter + instanceId) % 11 == 0)       // recalculate input once per 10 physics cycles. Relay route algorythm is too expensive
                    {
                        int activeSatsIncr = 0;

                        // reset all output variables at stat of loop
                        total_beamed_power = 0;
                        total_beamed_power_max = 0;
                        total_beamed_wasteheat = 0;
                        connectedsatsi = 0;
                        connectedrelaysi = 0;
                        networkDepth = 0;
                        powerInputMegajoules = 0;
                        powerInputMegajoulesMax = 0;
                        deactivate_timer = 0;

                        // clear _monitorDataStore
                        if (_monitorDataStore.Any())
                            _monitorDataStore.Clear();

                        HashSet<VesselRelayPersistence> usedRelays = new HashSet<VesselRelayPersistence>();

                        //Transmitters power calculation
                        foreach (var connectedTransmitterEntry in GetConnectedTransmitters())
                        {
                            VesselMicrowavePersistence transmitterPersistance = connectedTransmitterEntry.Key;

                            // first reset owm recieved power to get correct amount recieved by others
                            received_power[transmitterPersistance.Vessel] = 0;

                            KeyValuePair<MicrowaveRoute, IEnumerable<VesselRelayPersistence>> keyvaluepair = connectedTransmitterEntry.Value;
                            var microwaveRoute = keyvaluepair.Key;
                            routeEfficiency = microwaveRoute.Efficiency;
                            IEnumerable<VesselRelayPersistence> relays = keyvaluepair.Value;

                            // calculate maximum power avialable from beamed power network
                            currentPowerUsageByOtherRecievers = MicrowavePowerReceiver.getEnumeratedPowerFromSatelliteForAllLoadedVesssels(transmitterPersistance);

                            // convert initial beamed power from source into MegaWatt
                            maxAvailablePowerFromSource = transmitterPersistance.getAvailablePower() / 1000;

                            // subtract any power already recieved by other recievers
                            remainingPowerFromSource = Math.Max(0, (maxAvailablePowerFromSource * routeEfficiency) - currentPowerUsageByOtherRecievers);

                            // take into account maximum route capacity
                            double sateliteNetworkPowerCapacity = relays != null && relays.Count() > 0 ? Math.Min(remainingPowerFromSource, relays.Min(m => m.PowerCapacity)) : remainingPowerFromSource;

                            // determine allowed power
                            var maximumRecievePower = MaximumRecievePower;
                            var currentRecievalPower = maximumRecievePower * Math.Min(PowerCapacityEfficiency, PowerRatio);
                            var maximumRecievalPower = maximumRecievePower * PowerCapacityEfficiency;

                            // select active or compatible brandWith Converter
                            var selectedBrandWith = canSwitchBandwidthInEditor
                                ? activeBandwidthConfiguration
                                : BandwidthConverters.FirstOrDefault(m => microwaveRoute.WaveLength >= minimumWavelength && microwaveRoute.WaveLength <= m.maximumWavelength);

                            // get effective beamtoPower efficiency
                            if (selectedBrandWith != null)
                                efficiencyPercentage = thermalMode
                                    ? selectedBrandWith.MaxThermalEfficiencyPercentage
                                    : selectedBrandWith.MaxElectricEfficiencyPercentage;
                            else
                                efficiencyPercentage = 0;

                            // convert to fraction
                            var efficiency_fraction = efficiencyPercentage / 100;

                            // limit by amount of beampower the reciever is able to process
                            double satPower = Math.Min(currentRecievalPower, sateliteNetworkPowerCapacity * efficiency_fraction);
                            double satPowerMax = Math.Min(maximumRecievalPower, sateliteNetworkPowerCapacity * efficiency_fraction);
                            double satWasteheat = Math.Min(currentRecievalPower, sateliteNetworkPowerCapacity * (1 - efficiency_fraction));

                            // generate conversion wasteheat
                            total_conversion_waste_heat_production += satPower * (1 - efficiency_fraction);

                            // register amount of raw power recieved
                            received_power[transmitterPersistance.Vessel] = efficiency_fraction > 0 ? satPower / efficiency_fraction : satPower;

                            // convert raw power into effecive power
                            total_beamed_power += satPower;
                            total_beamed_power_max += satPowerMax;
                            total_beamed_wasteheat += satWasteheat;

                            if (satPower > 0)
                            {
                                activeSatsIncr++;
                                if (relays != null)
                                {
                                    foreach (var relay in relays)
                                    {
                                        usedRelays.Add(relay);
                                    }
                                    networkDepth = Math.Max(networkDepth, relays.Count());
                                }
                            }
                        }

                        connectedsatsi = activeSatsIncr;
                        connectedrelaysi = usedRelays.Count;

                        powerInputMegajoules = total_beamed_power + solarInputMegajoules;
                        powerInputMegajoulesMax = total_beamed_power_max + solarInputMegajoulesMax;
                    }

                    // process conversion wasteheat
                    if (!CheatOptions.IgnoreMaxTemperature)
                        supplyFNResourcePerSecond(total_conversion_waste_heat_production, FNResourceManager.FNRESOURCE_WASTEHEAT);

                    // add energy from solar panel to power management and subtract generated power
                    ProcesSolarCellEnergy();

                    // add alternator power
                    AddAlternatorPower();

                    // update energy buffers
                    UpdateBuffers(powerInputMegajoules);

                    if (isThermalReceiverSlave || thermalMode)
                    {
                        slavesAmount = thermalReceiverSlaves.Count;

                        // first do solar power
                        supplyFNResourcePerSecondWithMax(solarInputMegajoules, solarInputMegajoulesMax, FNResourceManager.FNRESOURCE_THERMALPOWER);
                        double supplied_beamed_thermal_per_second = supplyFNResourcePerSecondWithMax(total_beamed_power, total_beamed_power_max, FNResourceManager.FNRESOURCE_THERMALPOWER);

                        if (!CheatOptions.IgnoreMaxTemperature)
                        {
                            // thermal power does not have conversion wasteheat, only distritribution wasteheat
                            supplyFNResourcePerSecondWithMax(supplied_beamed_thermal_per_second, total_beamed_power_max, FNResourceManager.FNRESOURCE_WASTEHEAT);
                            supplyFNResourcePerSecondWithMax(solarInputMegajoules, solarInputMegajoulesMax, FNResourceManager.FNRESOURCE_WASTEHEAT);
                        }

                        var cur_thermal_power_per_second = solarInputMegajoules + total_beamed_power;

                        var total_thermal_power = isThermalReceiver
                            ? cur_thermal_power_per_second + thermalReceiverSlaves.Sum(m => m.ThermalPower)
                            : cur_thermal_power_per_second;

                        if (animT != null)
                        {
                            var maximumRecievePower = MaximumRecievePower;
                            animT[animTName].normalizedTime = maximumRecievePower > 0 ? (float)Math.Min(total_thermal_power / maximumRecievePower, 1) : 0;
                            animT.Sample();
                        }

                        if (ThermalPower <= 0)
                            ThermalPower = total_thermal_power;
                        else
                            ThermalPower = total_thermal_power * GameConstants.microwave_alpha + (1.0f - GameConstants.microwave_alpha) * ThermalPower;
                    }
                    else
                    {
                        wasteheatElectricConversionEfficiency = WasteheatElectricConversionEfficiency;

                        // convert the received beamed energy  into effective electric power
                        effectiveBeamedPowerElectricEfficiency = wasteheatElectricConversionEfficiency * electricMaxEfficiency;
                        var avaialbleElectricBeamedPower = total_beamed_power * effectiveBeamedPowerElectricEfficiency;
                        var suppliedBeamedPower = supplyManagedFNResourcePerSecond(avaialbleElectricBeamedPower, FNResourceManager.FNRESOURCE_MEGAJOULES);

                        // only generate wasteheat from beamed power when actualy using the energy
                        if (!CheatOptions.IgnoreMaxTemperature)
                        {
                            var supplyRatio = suppliedBeamedPower > 0 ? suppliedBeamedPower / avaialbleElectricBeamedPower : 0;
                            supplyFNResourcePerSecond(total_beamed_wasteheat * supplyRatio, FNResourceManager.FNRESOURCE_WASTEHEAT);
                        }

                        // convert all solar thermal energy into electric energy
                        effectiveSolarThermalElectricEfficiency = wasteheatElectricConversionEfficiency * solarElectricEfficiency;
                        supplyFNResourcePerSecond(solarInputMegajoules * effectiveSolarThermalElectricEfficiency, FNResourceManager.FNRESOURCE_MEGAJOULES);

                        // always generate wasteheat because the sun cannot be turned off
                        if (!CheatOptions.IgnoreMaxTemperature)
                        {
                            var effectiveSolarElectricWasteheatRatio = 1 - effectiveSolarThermalElectricEfficiency;
                            supplyFNResourcePerSecondWithMax(solarInputMegajoules * effectiveSolarElectricWasteheatRatio, solarInputMegajoules, FNResourceManager.FNRESOURCE_WASTEHEAT);
                        }
                    }
                }
                else
                {
                    total_beamed_power = 0;
                    total_beamed_power_max = 0;
                    total_beamed_wasteheat = 0;

                    powerInputMegajoules = 0;
                    powerInputMegajoulesMax = 0;

                    solarInputMegajoules = 0;
                    solarInputMegajoulesMax = 0;

                    solarFacingFactor = 0;
                    connectedsatsi = 0;
                    connectedrelaysi = 0;
                    ThermalPower = 0;

                    UpdateBuffers(0);

                    received_power.Clear();

                    if (animT != null)
                    {
                        animT[animTName].normalizedTime = 0;
                        animT.Sample();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in MicrowavePowerReceiver.OnFixedUpdateResourceSuppliable " + e.Message + " at " + e.StackTrace);
            }
        }

        private void CalculateThermalSolarPower()
        {
            if (solarReceptionSurfaceArea <= 0 || solarReceptionEfficiency <= 0)
                return;

            solarFluxQueue.Enqueue(part.vessel.solarFlux);

            if (solarFluxQueue.Count > 50)
                solarFluxQueue.Dequeue();

            solarFlux = solarFluxQueue.Count > 10
                ? solarFluxQueue.OrderBy(m => m).Skip(10).Average()
                : solarFluxQueue.Average();

            solarInputMegajoulesMax = solarReceptionSurfaceArea * (solarFlux / 1e+6) * solarReceptionEfficiency;
            solarFacingFactor = Math.Pow(GetSolarFacingFactor(localStar, part.WCoM), solarFacingExponent);
            solarInputMegajoules = solarInputMegajoulesMax * solarFacingFactor;
        }

        private void AddAlternatorPower()
        {
            if (alternatorRatio == 0)
                return;
            
            supplyFNResourceFixed(alternatorRatio * powerInputMegajoules * TimeWarp.fixedDeltaTime / 1000, FNResourceManager.FNRESOURCE_MEGAJOULES);
        }

        private void ProcesSolarCellEnergy()
        {
            if (deployableSolarPanel == null)
                return;

            // readout kerbalism solar power output so we can remove it
            if (_field_kerbalism_output != null)
                kerbalismPowerOutput = _field_kerbalism_output.GetValue<double>(warpfixer);

            flowRate = deployableSolarPanel.flowRate > 0
                ? deployableSolarPanel.flowRate
                : deployableSolarPanel.chargeRate * deployableSolarPanel._flowRate;

            flowRateQueue.Enqueue(flowRate);

            if (flowRateQueue.Count > 50)
                flowRateQueue.Dequeue();

            var stabalizedFlowRate = flowRateQueue.Count > 10
                ? flowRateQueue.OrderBy(m => m).Skip(10).Average()
                : flowRateQueue.Average();

            double maxSupply = deployableSolarPanel._distMult > 0
                ? Math.Max(stabalizedFlowRate, deployableSolarPanel.chargeRate * deployableSolarPanel._distMult * deployableSolarPanel._efficMult)
                : stabalizedFlowRate;

            // extract power otherwise we end up with double power
            var power_reduction = deployableSolarPanel.flowRate > 0 ? deployableSolarPanel.flowRate : kerbalismPowerOutput;

            if (deployableSolarPanel.resourceName == FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
            {
                part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, power_reduction * TimeWarp.fixedDeltaTime);

                if (stabalizedFlowRate > 0)
                    stabalizedFlowRate /= 1000;
                if (maxSupply > 0)
                    maxSupply /= 1000;
            }
            else if (deployableSolarPanel.resourceName == FNResourceManager.FNRESOURCE_MEGAJOULES)
            {
                part.RequestResource(FNResourceManager.FNRESOURCE_MEGAJOULES, power_reduction * TimeWarp.fixedDeltaTime);
            }
            else
            {
                stabalizedFlowRate = 0;
                maxSupply = 0;
            }

            if (stabalizedFlowRate > 0)
                supplyFNResourcePerSecondWithMax(stabalizedFlowRate, maxSupply, FNResourceManager.FNRESOURCE_MEGAJOULES);
        }

        public double MaxStableMegaWattPower
        {
            get { return isThermalReceiver ? 0 : powerInputMegajoules; }
        }

        public virtual double GetCoreTempAtRadiatorTemp(double rad_temp)
        {
            return 3500;
        }

        public double GetThermalPowerAtTemp(double temp)
        {
            return ThermalPower;
        }

        public double GetRadius()
        {
            return radius;
        }

        public bool isActive()
        {
            return receiverIsEnabled;
        }

        public bool shouldScaleDownJetISP()
        {
            return false;
        }

        public void EnableIfPossible()
        {
            if (!receiverIsEnabled && autoDeploy)
                ActivateRecieverState();
        }

        public override string GetInfo()
        {
            return "Diameter: " + diameter + " m";
        }

        public double getPowerFromSatellite(VesselMicrowavePersistence vmp)
        {
            if (received_power.ContainsKey(vmp.Vessel) && receiverIsEnabled)
                return received_power[vmp.Vessel];

            return 0;
        }

        public static double getEnumeratedPowerFromSatelliteForAllLoadedVesssels(VesselMicrowavePersistence vmp)
        {
            double enumerated_power = 0;
            foreach (Vessel vess in FlightGlobals.Vessels)
            {
                List<MicrowavePowerReceiver> receivers = vess.FindPartModulesImplementing<MicrowavePowerReceiver>();
                foreach (MicrowavePowerReceiver receiver in receivers)
                {
                    enumerated_power += receiver.getPowerFromSatellite(vmp);
                }
            }
            return enumerated_power;
        }

        #region RelayRouting
        protected double ComputeVisibilityAndDistance(VesselRelayPersistence relay, Vessel targetVessel)
        {
            var result = PluginHelper.HasLineOfSightWith(relay.Vessel, targetVessel, 0)
                ? Vector3d.Distance(PluginHelper.getVesselPos(relay.Vessel), PluginHelper.getVesselPos(targetVessel))
                : -1;

            return result;
        }

        protected double ComputeDistance(Vessel v1, Vessel v2)
        {
            return Vector3d.Distance(PluginHelper.getVesselPos(v1), PluginHelper.getVesselPos(v2));
        }

        protected double ComputeSpotSize(WaveLengthData waveLengthData, double distanceToSpot, double transmitterAperture, Vessel receivingVessel, Vessel sendingVessel)
        {
            if (transmitterAperture == 0)
                transmitterAperture = 1;

            if (waveLengthData.wavelength == 0)
                waveLengthData.wavelength = 1;

            MonitorData monitordata;
            if (!_monitorDataStore.TryGetValue(waveLengthData.partId, out monitordata))
            {
                monitordata = new MonitorData() 
                { 
                    partId = waveLengthData.partId 
                };
                _monitorDataStore.Add(waveLengthData.partId, monitordata);
            }

            var effectiveAperureBonus = waveLengthData.wavelength >= 0.001 
                ? PluginHelper.MicrowaveApertureDiameterMult 
                : PluginHelper.NonMicrowaveApertureDiameterMult;

            monitordata.spotsize = (distanceToSpot * waveLengthData.wavelength) / (transmitterAperture * effectiveAperureBonus * apertureMultiplier);
            monitordata.distanceToSpot = distanceToSpot;
            monitordata.wavelength = (float)waveLengthData.wavelength;
            monitordata.aperture = transmitterAperture;
            monitordata.receivingVessel = receivingVessel;
            monitordata.sendingVessel = sendingVessel;

            return monitordata.spotsize;
        }

        protected double ComputeDistanceFacingEfficiency(double spotSizeDiameter, double facingFactor, double recieverDiameter)
        {
            //Debug.Log("[KSP Interstellar]: ComputeDistanceFacingEfficiency spotSize: " + spotSize + " facingFactor: " + facingFactor + " recieverDiameter: " + recieverDiameter);

            if (spotSizeDiameter == 0)
                return 0;

            if (spotSizeDiameter < 0)
            {
                Debug.LogWarning("[KSPI] - ComputeDistanceFacingEfficiency spotSizeDiameter < 0");
                return 0;
            }

            if (facingFactor == 0)
                return 0;

            if (facingFactor < 0)
            {
                Debug.LogWarning("[KSPI] - ComputeDistanceFacingEfficiency facingFactor <= 0");
                return 0;
            }

            if (recieverDiameter <= 0)
            {
                Debug.LogWarning("[KSPI] - ComputeDistanceFacingEfficiency recieverDiameter <= 0");
                return 0;
            }

            effectiveDistanceFacingEfficiency = Math.Pow(facingFactor, facingEfficiencyExponent) * Math.Pow(Math.Min(1, recieverDiameter * facingFactor / spotSizeDiameter), spotsizeNormalizationExponent);

            return effectiveDistanceFacingEfficiency;
        }

        protected double ComputeFacingFactor(Vessel transmitterVessel)
        {
            // retrun if no recieval is possible
            if (highSpeedAtmosphereFactor == 0 && !CanBeActiveInAtmosphere)
                return 0;

            return ComputeFacingFactor(PluginHelper.getVesselPos(transmitterVessel), this.vessel.transform.position);
        }

        protected double ComputeFacingFactor(Vector3 transmitPosition, Vector3 receiverPosition)
        {
            double facingFactor;
            Vector3d directionVector = (transmitPosition - receiverPosition).normalized;


            if (receiverType == 6) 
            {
                facingFactor = Math.Min(1, Math.Abs(Vector3d.Dot(part.transform.forward, directionVector)));
            }
            else if( receiverType == 5)
            {
                //Scale energy reception based on angle of reciever to transmitter from bottom
                facingFactor = Math.Max(0, -Vector3d.Dot(part.transform.up, directionVector));
            }
            else if (receiverType == 4) // used by single pivoting solar arrays
            {
                facingFactor = Math.Min(1 -Math.Abs(Vector3d.Dot(part.transform.right, directionVector)), 1);
            }
            else if (receiverType == 3)
            {
                //Scale energy reception based on angle of reciever to transmitter from back
                facingFactor = Math.Max(0, -Vector3d.Dot(part.transform.forward, directionVector));
            }
            else if (receiverType == 2)
            {
                // get the best result of inline and directed reciever
                facingFactor = Math.Min(1 - Math.Abs(Vector3d.Dot(part.transform.up, directionVector)), 1);
            }
            else if (receiverType == 1)
            {
                // recieve from sides
                facingFactor = Math.Min(1 - Math.Abs(Vector3d.Dot(part.transform.up, directionVector)), 1);
            }
            else // receiverType == 0
            {
                //Scale energy reception based on angle of reciever to transmitter from top
                facingFactor = Math.Max(0, Vector3d.Dot(part.transform.up, directionVector));
            }


            if (facingFactor > facingThreshold)
                facingFactor = Math.Pow(facingFactor, facingSurfaceExponent);
            else
                facingFactor = 0;

            if (receiverType == 2)
            {
                var facingFactorB = Math.Round(0.4999 + Math.Max(0, Vector3d.Dot(part.transform.up, directionVector)));
                facingFactor = Math.Max(facingFactor, facingFactorB);
            }

            var localfacingFactor = CanBeActiveInAtmosphere ? facingFactor : highSpeedAtmosphereFactor * facingFactor;

            //Debug.Log("[KSP Interstellar]: ComputeFacingFactor: " + localfacingFactor);

            return localfacingFactor;
        }

        /// <summary>
        /// Returns transmitters which to which this vessel can connect, route efficiency and relays used for each one.
        /// </summary>
        /// <param name="maxHops">Maximum number of relays which can be used for connection to transmitter</param>
        protected IDictionary<VesselMicrowavePersistence, KeyValuePair<MicrowaveRoute, IEnumerable<VesselRelayPersistence>>> GetConnectedTransmitters(int maxHops = 25)
        {
            directWavelengths = 0;

            //these two dictionaries store transmitters and relays and best currently known route to them which is replaced if better one is found. 

            var transmitterRouteDictionary = new Dictionary<VesselMicrowavePersistence, MicrowaveRoute>(); // stores all transmitter we can have a connection with
            var relayRouteDictionary = new Dictionary<VesselRelayPersistence, MicrowaveRoute>();

            var transmittersToCheck = new List<VesselMicrowavePersistence>();//stores all transmiters to which we want to connect

            double recieverAtmosphericPresure = FlightGlobals.getStaticPressure(this.vessel.transform.position) / 100;

            foreach (VesselMicrowavePersistence transmitter in MicrowaveSources.instance.globalTransmitters.Values)
            {
                //first check for direct connection from current vessel to transmitters, will always be optimal
                if (transmitter.getAvailablePower() <= 0)
                {
                    Debug.Log("[KSPI] - Transmitter vessel has no power available");
                    continue;
                }

                //ignore if no power or transmitter is on the same vessel
                if (transmitter.Vessel == vessel)
                {
                    Debug.Log("[KSPI] - Transmitter vessel is equal to receiver vessel");
                    continue;
                }

                bool hasLineOfSight = PluginHelper.HasLineOfSightWith(this.vessel, transmitter.Vessel);
                if (hasLineOfSight)
                {
                    //Debug.Log("[KSPI] - has line of sight with vessel " + transmitter.Vessel.name);
                    var possibleWavelengths = new List<MicrowaveRoute>();
                    double distanceInMeter = ComputeDistance(this.vessel, transmitter.Vessel);
                    double facingFactor = ComputeFacingFactor(transmitter.Vessel);

                    //Debug.Log("[KSP Interstellar]: Has line of sight with Transmitters vessel " + transmitter.Vessel.id + " Facing factor " + facingFactor + " at distance " + distanceInMeter);

                    effectivefacingFactor = facingFactor;

                    double transmitterAtmosphericPresure = FlightGlobals.getStaticPressure(transmitter.Vessel.transform.position) / 100;

                    foreach (WaveLengthData wavelenghtData in transmitter.SupportedTransmitWavelengths)
                    {
                        if (wavelenghtData.wavelength.NotWithin(this.maximumWavelength, this.minimumWavelength))
                        {
                            //Debug.Log("[KSP Interstellar]: GetConnectedTransmitters: transmit wavelength " + wavelenghtData.wavelength + " is not within " + this.maximumWavelength + " and " + this.minimumWavelength);
                            continue;
                        }

                        directWavelengths++;

                        double spotsize = ComputeSpotSize(wavelenghtData, distanceInMeter, transmitter.Aperture, this.vessel, transmitter.Vessel);

                        //Debug.Log("[KSP Interstellar]: GetConnectedTransmitters spotSize: " + spotsize + " facingFactor: " + facingFactor + " recieverDiameter: " + this.diameter);
                        double distanceFacingEfficiency = ComputeDistanceFacingEfficiency(spotsize, facingFactor, this.diameter);
                        double atmosphereEfficency = GetAtmosphericEfficiency(transmitterAtmosphericPresure, recieverAtmosphericPresure, wavelenghtData.atmosphericAbsorption, distanceInMeter, this.vessel, transmitter.Vessel);
                        effectiveAtmosphereEfficency = atmosphereEfficency;
                        double transmitterEfficency = distanceFacingEfficiency * atmosphereEfficency;
                        effectiveTransmitterEfficency = transmitterEfficency;

                        possibleWavelengths.Add(new MicrowaveRoute(transmitterEfficency, distanceInMeter, facingFactor, spotsize, wavelenghtData.wavelength)); 
                    }

                    var mostEfficientWavelength = possibleWavelengths.Count == 0 ? null : 
                        possibleWavelengths.FirstOrDefault(m => m.Efficiency ==  possibleWavelengths.Max(n => n.Efficiency));

                    if (mostEfficientWavelength != null)
                    {
                        //store in dictionary that optimal route to this transmitter is direct connection, can be replaced if better route is found
                        transmitterRouteDictionary[transmitter] = mostEfficientWavelength;
                    }
                }

                // add all tranmitters that are not located on the recieving vessel
                transmittersToCheck.Add(transmitter);
            }

            //this algorithm processes relays in groups in which elements of the first group must be visible from receiver, 
            //elements from the second group must be visible by at least one element from previous group and so on...

            var relaysToCheck = new List<VesselRelayPersistence>();//relays which we have to check - all active relays will be here
            var currentRelayGroup = new List<KeyValuePair<VesselRelayPersistence, int>>();//relays which are in line of sight, and we have not yet checked what they can see. Their index in relaysToCheck is also stored

            int relayIndex = 0;
            foreach (VesselRelayPersistence relay in MicrowaveSources.instance.globalRelays.Values)
            {
                if (!relay.IsActive) continue;

                if (PluginHelper.HasLineOfSightWith(this.vessel, relay.Vessel))
                {
                    var possibleWavelengths = new List<MicrowaveRoute>();
                    double distanceInMeter = ComputeDistance(this.vessel, relay.Vessel);
                    double facingFactor = ComputeFacingFactor(relay.Vessel);

                    //Debug.Log("[KSP Interstellar]: GetConnected Relays: " + facingFactor);

                    double transmitterAtmosphericPresure = FlightGlobals.getStaticPressure(relay.Vessel.transform.position) / 100;

                    foreach (var wavelenghtData in relay.SupportedTransmitWavelengths)
                    {
                        if (wavelenghtData.wavelength.NotWithin(this.maximumWavelength, this.minimumWavelength))
                            continue;

                        double spotsize = ComputeSpotSize(wavelenghtData, distanceInMeter, relay.Aperture, this.vessel, relay.Vessel);
                        double distanceFacingEfficiency = ComputeDistanceFacingEfficiency(spotsize, facingFactor, this.diameter);

                        double atmosphereEfficency = GetAtmosphericEfficiency(transmitterAtmosphericPresure, recieverAtmosphericPresure, wavelenghtData.atmosphericAbsorption, distanceInMeter, this.vessel, relay.Vessel);
                        double transmitterEfficency = distanceFacingEfficiency * atmosphereEfficency;

                        possibleWavelengths.Add(new MicrowaveRoute(transmitterEfficency, distanceInMeter, facingFactor, spotsize, wavelenghtData.wavelength));
                    }

                    var mostEfficientWavelength = possibleWavelengths.Count == 0 ? null :
                        possibleWavelengths.SingleOrDefault(m => m.Efficiency == possibleWavelengths.Max(n => n.Efficiency));

                    if (mostEfficientWavelength != null)
                    {
                        //store in dictionary that optimal route to this relay is direct connection, can be replaced if better route is found
                        relayRouteDictionary[relay] = mostEfficientWavelength;
                        currentRelayGroup.Add(new KeyValuePair<VesselRelayPersistence, int>(relay, relayIndex));
                    }
                }
                relaysToCheck.Add(relay);
                relayIndex++;
            }

            int hops = 0; //number of hops between relays

            //pre-compute distances and visibility thus limiting number of checks to (Nr^2)/2 + NrNt +Nr + Nt
            if (hops < maxHops && transmittersToCheck.Any())
            {
                double[,] relayToRelayDistances = new double[relaysToCheck.Count, relaysToCheck.Count];
                double[,] relayToTransmitterDistances = new double[relaysToCheck.Count, transmittersToCheck.Count];

                for (int i = 0; i < relaysToCheck.Count; i++)
                {
                    var relayToCheck = relaysToCheck[i];
                    for (int j = i + 1; j < relaysToCheck.Count; j++)
                    {
                        double visibilityAndDistance = ComputeVisibilityAndDistance(relayToCheck, relaysToCheck[j].Vessel);
                        relayToRelayDistances[i, j] = visibilityAndDistance;
                        relayToRelayDistances[j, i] = visibilityAndDistance;
                    }
                    for (int t = 0; t < transmittersToCheck.Count; t++)
                    {
                        relayToTransmitterDistances[i, t] = ComputeVisibilityAndDistance(relayToCheck, transmittersToCheck[t].Vessel);
                    }
                }

                HashSet<int> coveredRelays = new HashSet<int>();

                //runs as long as there is any relay to which we can connect and maximum number of hops have not been breached
                while (hops < maxHops && currentRelayGroup.Any())
                {
                    var nextRelayGroup = new List<KeyValuePair<VesselRelayPersistence, int>>();//will put every relay which is in line of sight of any relay from currentRelayGroup here
                    foreach (var relayEntry in currentRelayGroup) //relays visible from receiver in first iteration, then relays visible from them etc....
                    {
                        VesselRelayPersistence relayPersistance = relayEntry.Key;
                        MicrowaveRoute relayRoute = relayRouteDictionary[relayPersistance];// current best route for this relay
                        double relayRouteFacingFactor = relayRoute.FacingFactor;// it's always facing factor from the beggining of the route
                        double relayAtmosphericPresure = FlightGlobals.getStaticPressure(relayPersistance.Vessel.transform.position) / 100;

                        for (int t = 0; t < transmittersToCheck.Count; t++)//check if this relay can connect to transmitters
                        {
                            double distanceInMeter = relayToTransmitterDistances[relayEntry.Value, t];

                            //it's >0 if it can see
                            if (distanceInMeter <= 0) continue;

                            VesselMicrowavePersistence transmitterToCheck = transmittersToCheck[t];
                            double newDistance = relayRoute.Distance + distanceInMeter;// total distance from receiver by this relay to transmitter
                            double transmitterAtmosphericPresure = FlightGlobals.getStaticPressure(transmitterToCheck.Vessel.transform.position) / 100;
                            var possibleWavelengths = new List<MicrowaveRoute>();

                            foreach (var transmitterWavelenghtData in transmitterToCheck.SupportedTransmitWavelengths)
                            {
                                if (transmitterWavelenghtData.wavelength.NotWithin(relayPersistance.MaximumRelayWavelenght, relayPersistance.MinimumRelayWavelenght))
                                    continue;

                                double spotsize = ComputeSpotSize(transmitterWavelenghtData, distanceInMeter, transmitterToCheck.Aperture, relayPersistance.Vessel, transmitterToCheck.Vessel);
                                double distanceFacingEfficiency = ComputeDistanceFacingEfficiency(spotsize, 1, relayPersistance.Aperture);

                                double atmosphereEfficency = GetAtmosphericEfficiency(transmitterAtmosphericPresure, relayAtmosphericPresure, transmitterWavelenghtData.atmosphericAbsorption, distanceInMeter, transmitterToCheck.Vessel, relayPersistance.Vessel);
                                double efficiencyTransmitterToRelay = distanceFacingEfficiency * atmosphereEfficency;
                                double efficiencyForRoute = efficiencyTransmitterToRelay * relayRoute.Efficiency;

                                possibleWavelengths.Add(new MicrowaveRoute(efficiencyForRoute, newDistance, relayRouteFacingFactor, spotsize, transmitterWavelenghtData.wavelength, relayPersistance));
                            }

                             var mostEfficientWavelength = possibleWavelengths.Count == 0 ? null :
                                    possibleWavelengths.SingleOrDefault(m => m.Efficiency == possibleWavelengths.Max(n => n.Efficiency));

                            if (mostEfficientWavelength != null)
                            {
                                //this will return true if there is already a route to this transmitter
                                MicrowaveRoute currentOptimalRoute;
                                if (transmitterRouteDictionary.TryGetValue(transmitterToCheck, out currentOptimalRoute))
                                {
                                    if (currentOptimalRoute.Efficiency < mostEfficientWavelength.Efficiency)
                                    {
                                        //if route using this relay is better then replace the old route
                                        transmitterRouteDictionary[transmitterToCheck] = mostEfficientWavelength;
                                    }
                                }
                                else
                                {
                                    //there is no other route to this transmitter yet known so algorithm puts this one as optimal
                                    transmitterRouteDictionary[transmitterToCheck] = mostEfficientWavelength;
                                }
                            }
                        }

                        for (int r = 0; r < relaysToCheck.Count; r++)
                        {
                            var nextRelay = relaysToCheck[r];
                            if (nextRelay == relayPersistance)
                                continue;

                            double distanceToNextRelay = relayToRelayDistances[relayEntry.Value, r];
                            if (distanceToNextRelay <= 0) continue;

                            var possibleWavelengths = new List<MicrowaveRoute>();
                            var relayToNextRelayDistance = relayRoute.Distance + distanceToNextRelay;

                            foreach (var transmitterWavelenghtData in relayPersistance.SupportedTransmitWavelengths)
                            {
                                if (transmitterWavelenghtData.wavelength.NotWithin(relayPersistance.MaximumRelayWavelenght, relayPersistance.MinimumRelayWavelenght))
                                    continue;

                                double spotsize = ComputeSpotSize(transmitterWavelenghtData, distanceToNextRelay, relayPersistance.Aperture, nextRelay.Vessel, relayPersistance.Vessel);
                                double efficiencyByThisRelay = ComputeDistanceFacingEfficiency(spotsize, 1, relayPersistance.Aperture);
                                double efficiencyForRoute = efficiencyByThisRelay * relayRoute.Efficiency;

                                possibleWavelengths.Add(new MicrowaveRoute(efficiencyForRoute, relayToNextRelayDistance, relayRouteFacingFactor, spotsize, transmitterWavelenghtData.wavelength, relayPersistance));
                            }

                            var mostEfficientWavelength = possibleWavelengths.Count == 0 ? null :
                                possibleWavelengths.SingleOrDefault(m => m.Efficiency == possibleWavelengths.Max(n => n.Efficiency));

                            if (mostEfficientWavelength != null)
                            {
                                MicrowaveRoute currentOptimalPredecessor;
                                if (relayRouteDictionary.TryGetValue(nextRelay, out currentOptimalPredecessor))
                                //this will return true if there is already a route to next relay
                                {
                                    //if route using this relay is better
                                    if (currentOptimalPredecessor.Efficiency < mostEfficientWavelength.Efficiency)
                                    {
                                        //we put it in dictionary as optimal
                                        relayRouteDictionary[nextRelay] = mostEfficientWavelength;
                                    }
                                }
                                else //there is no other route to this relay yet known so we put this one as optimal
                                {
                                    relayRouteDictionary[nextRelay] = mostEfficientWavelength;
                                }

                                if (!coveredRelays.Contains(r))
                                {
                                    nextRelayGroup.Add(new KeyValuePair<VesselRelayPersistence, int>(nextRelay, r));
                                    //in next iteration we will check what next relay can see
                                    coveredRelays.Add(r);
                                }
                            }
                        }
                    }
                    currentRelayGroup = nextRelayGroup;
                    //we don't have to check old relays so we just replace whole List
                    hops++;
                }

            }

            //building final result
            var resultDictionary = new Dictionary<VesselMicrowavePersistence, KeyValuePair<MicrowaveRoute, IEnumerable<VesselRelayPersistence>>>();

            foreach (var transmitterEntry in transmitterRouteDictionary)
            {
                var vesselPersistance = transmitterEntry.Key;
                var microwaveRoute = transmitterEntry.Value;

                Stack<VesselRelayPersistence> relays = new Stack<VesselRelayPersistence>();//Last in, first out so relay visible from receiver will always be first
                VesselRelayPersistence relay = microwaveRoute.PreviousRelay;
                while (relay != null)
                {
                    relays.Push(relay);
                    relay = relayRouteDictionary[relay].PreviousRelay;
                }

                resultDictionary.Add(vesselPersistance, new KeyValuePair<MicrowaveRoute, IEnumerable<VesselRelayPersistence>>(microwaveRoute, relays));
                //Debug.Log("[KSP Interstellar]:   Add to Result Dictionary Transmitter power: " + transmitterEntry.Key.NuclearPower + " with route efficiency " + transmitterEntry.Value.Efficiency);
            }

            return resultDictionary;
        }
        #endregion RelayRouting

        public override int getPowerPriority()
        {
            return 1;
        }
    }
}
