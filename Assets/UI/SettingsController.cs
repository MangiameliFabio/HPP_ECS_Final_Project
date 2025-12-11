using System.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;


public struct KillCount
{
    public static int FightersKilled = 0;
    public static int StarDestroyerKilled = 0;
}

public class SettingsController : MonoBehaviour
{
    [SerializeField] public FreeCamera Camera;
    
    private EntityManager _em;
    private Entity _configEntity = Entity.Null;
    private Entity _fighterSettingsEntity = Entity.Null;
    private Entity _starDestroyerSettingsEntity = Entity.Null;
    private VisualElement _ui;
    private TextField _fighterSpawnCount;
    private TextField _starDestroyerSpawnCount;
    private TextField _starDestroyerHealth;
    private DropdownField _dropDownRunningType;
    private Keyboard _keyboard;
    private VisualElement _settingsContainer;
    private Label _fighterKilledLabel;
    private Label _starDestroyerKilledLabel;
    
    private readonly System.Collections.Generic.Dictionary<string, Slider> _sliders
        = new System.Collections.Generic.Dictionary<string, Slider>();
    private void Awake()
    {
        _ui = GetComponent<UIDocument>().rootVisualElement;
    }

    private void Start()
    {
        _keyboard = Keyboard.current;
        Cursor.visible = false;                  
        Cursor.lockState = CursorLockMode.Locked;
        
        string[] sliderNames = new string[]
        {
            "MinSpeed",
            "MaxSpeed",
            "MinRotationSpeed",
            "MaxRotationSpeed",
            "NeighbourDetectionRadius",
            "AlignmentFactor",
            "CrowdingFactor",
            "NeighbourCounterForceFactor",
            "AvoidanceFactor",
            "TargetTrendFactor",
            "TargetMinDistance",
            "FireCooldown",
            "SDSpeed",
            "SDMovementRadius"
        };
        
        foreach (var name in sliderNames)
        {
            var slider = _ui.Q<Slider>(name);
            if (slider != null)
            {
                _sliders[name] = slider;
                slider.RegisterValueChangedCallback(evt => OnSliderChanged(name, evt));
            }
        }
        
        _settingsContainer = _ui.Q<VisualElement>("Settings");
        _settingsContainer.visible = false;
        
        _fighterKilledLabel = _ui.Q<Label>("AmountFighter");
        _starDestroyerKilledLabel = _ui.Q<Label>("AmountStarDestroyer");
        
        _fighterSpawnCount = _ui.Q<TextField>("fighterSpawnCount");
        _fighterSpawnCount.RegisterValueChangedCallback(OnFighterSpawnCountChanged);
        
        _starDestroyerSpawnCount = _ui.Q<TextField>("SDSpawnCount");
        _starDestroyerSpawnCount.RegisterValueChangedCallback(OnDestroyerSpawnCountChanged);
        
        _starDestroyerHealth = _ui.Q<TextField>("SDHealth");
        _starDestroyerHealth.RegisterValueChangedCallback(OnDestroyerHealth);
        
        _dropDownRunningType = _ui.Q<DropdownField>("RunningType");
        _dropDownRunningType.RegisterValueChangedCallback(OnRunParallelChanged);
        
        StartCoroutine(WaitForECSWorld());
    }
    
    void Update()
    {
        if (_keyboard != null && _keyboard.f11Key.wasPressedThisFrame)
        {
            Camera.enabled = !Camera.enabled;
            _settingsContainer.visible = !_settingsContainer.visible;
            Cursor.visible = !Cursor.visible;  
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        }
        
        _fighterKilledLabel.text = KillCount.FightersKilled.ToString();
        _starDestroyerKilledLabel.text = KillCount.StarDestroyerKilled.ToString();
    }
    
    private IEnumerator WaitForECSWorld()
    {
        while (World.DefaultGameObjectInjectionWorld == null)
            yield return null;
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        
        EntityQuery configQuery = _em.CreateEntityQuery(typeof(Config));
        while (configQuery.IsEmpty)
            yield return null;
        _configEntity = configQuery.GetSingletonEntity();
        
        EntityQuery fighterQuery = _em.CreateEntityQuery(typeof(FighterSettings));
        while (fighterQuery.IsEmpty)
            yield return null;
        _fighterSettingsEntity = fighterQuery.GetSingletonEntity();
        
        EntityQuery sdQuery = _em.CreateEntityQuery(typeof(StarDestroyerSettings));
        while (sdQuery.IsEmpty)
            yield return null;
        _starDestroyerSettingsEntity = sdQuery.GetSingletonEntity();
        
        var destroyerSettings = _em.GetComponentData<StarDestroyerSettings>(_starDestroyerSettingsEntity);
        var fighterSettings = _em.GetComponentData<FighterSettings>(_fighterSettingsEntity);
        SetSliderValues(fighterSettings, destroyerSettings);
        
        var cfg = _em.GetComponentData<Config>(_configEntity);
        _fighterSpawnCount.value = cfg.FighterCount.ToString();
        _dropDownRunningType.index = (int)cfg.RunType;
    }
    
    private void OnDisable()
    {
        _fighterSpawnCount.UnregisterValueChangedCallback(OnFighterSpawnCountChanged);
    }

    private void OnFighterSpawnCountChanged(ChangeEvent<string> evt)
    {
        if (_configEntity == Entity.Null)
            return;
        
        if (!int.TryParse(evt.newValue, out var count))
            return;

        var cfg = _em.GetComponentData<Config>(_configEntity);
        cfg.FighterCount = count;
        _em.SetComponentData(_configEntity, cfg);
    }
    
    private void OnDestroyerSpawnCountChanged(ChangeEvent<string> evt)
    {
        if (_configEntity == Entity.Null)
            return;
        
        if (!int.TryParse(evt.newValue, out var count))
            return;

        var cfg = _em.GetComponentData<Config>(_configEntity);
        cfg.StarDestroyerCount = count;
        _em.SetComponentData(_configEntity, cfg);
    }
    
    private void OnDestroyerHealth(ChangeEvent<string> evt)
    {
        if (_starDestroyerSettingsEntity == Entity.Null)
            return;
        
        if (!int.TryParse(evt.newValue, out var health))
            return;

        var settings = _em.GetComponentData<StarDestroyerSettings>(_starDestroyerSettingsEntity);
        settings.Health = health;
        _em.SetComponentData(_starDestroyerSettingsEntity, settings);
    }
    
    private void OnRunParallelChanged(ChangeEvent<string> evt)
    {
        if (_configEntity == Entity.Null)
            return;

        var runType = evt.newValue;
        var cfg = _em.GetComponentData<Config>(_configEntity);

        switch (runType)
        {
            case "Main Thread":
                cfg.RunType = RunningType.MainThread;
                break;
            case "Scheduled":
                cfg.RunType = RunningType.Scheduled;
                break;
            case "Scheduled Parallel":
                cfg.RunType = RunningType.Parallel;
                break;
            default:
                break;
        }
        _em.SetComponentData(_configEntity, cfg);
    }
    
    private void SetSliderValues(FighterSettings fighterSettings, StarDestroyerSettings destroyerSettings)
    {
        if (_sliders.ContainsKey("MinSpeed")) _sliders["MinSpeed"].value = fighterSettings.MinSpeed;
        if (_sliders.ContainsKey("MaxSpeed")) _sliders["MaxSpeed"].value = fighterSettings.MaxSpeed;
        if (_sliders.ContainsKey("MinRotationSpeed")) _sliders["MinRotationSpeed"].value = fighterSettings.MinRotationSpeed;
        if (_sliders.ContainsKey("MaxRotationSpeed")) _sliders["MaxRotationSpeed"].value = fighterSettings.MaxRotationSpeed;
        if (_sliders.ContainsKey("NeighbourDetectionRadius")) _sliders["NeighbourDetectionRadius"].value = fighterSettings.NeighbourDetectionRadius;
        if (_sliders.ContainsKey("AlignmentFactor")) _sliders["AlignmentFactor"].value = fighterSettings.AlignmentFactor;
        if (_sliders.ContainsKey("CrowdingFactor")) _sliders["CrowdingFactor"].value = fighterSettings.CrowdingFactor;
        if (_sliders.ContainsKey("NeighbourCounterForceFactor")) _sliders["NeighbourCounterForceFactor"].value = fighterSettings.NeighbourCounterForceFactor;
        if (_sliders.ContainsKey("AvoidanceFactor")) _sliders["AvoidanceFactor"].value = fighterSettings.AvoidanceFactor;
        if (_sliders.ContainsKey("TargetTrendFactor")) _sliders["TargetTrendFactor"].value = fighterSettings.TargetTrendFactor;
        if (_sliders.ContainsKey("TargetMinDistance")) _sliders["TargetMinDistance"].value = fighterSettings.TargetMinDistance;
        if (_sliders.ContainsKey("FireCooldown")) _sliders["FireCooldown"].value = (float)fighterSettings.FireCooldown;
        if (_sliders.ContainsKey("SDSpeed")) _sliders["SDSpeed"].value = destroyerSettings.Speed * 100f;
        if (_sliders.ContainsKey("SDMovementRadius")) _sliders["SDMovementRadius"].value = destroyerSettings.MovementRadius;
    }
    
    private void OnSliderChanged(string name, ChangeEvent<float> evt)
    {
        if (_fighterSettingsEntity == Entity.Null) return;

        var fighterSettings = _em.GetComponentData<FighterSettings>(_fighterSettingsEntity);
        var starDestroyerSettings = _em.GetComponentData<StarDestroyerSettings>(_starDestroyerSettingsEntity);

        switch (name)
        {
            case "MinSpeed": fighterSettings.MinSpeed = evt.newValue; break;
            case "MaxSpeed": fighterSettings.MaxSpeed = evt.newValue; break;
            case "MinRotationSpeed": fighterSettings.MinRotationSpeed = evt.newValue; break;
            case "MaxRotationSpeed": fighterSettings.MaxRotationSpeed = evt.newValue; break;
            case "NeighbourDetectionRadius": fighterSettings.NeighbourDetectionRadius = evt.newValue; break;
            case "AlignmentFactor": fighterSettings.AlignmentFactor = evt.newValue; break;
            case "CrowdingFactor": fighterSettings.CrowdingFactor = evt.newValue; break;
            case "NeighbourCounterForceFactor": fighterSettings.NeighbourCounterForceFactor = evt.newValue; break;
            case "AvoidanceFactor": fighterSettings.AvoidanceFactor = evt.newValue; break;
            case "TargetTrendFactor": fighterSettings.TargetTrendFactor = evt.newValue; break;
            case "TargetMinDistance": fighterSettings.TargetMinDistance = evt.newValue; break;
            case "FireCooldown": fighterSettings.FireCooldown = evt.newValue; break;
            case "SDSpeed": starDestroyerSettings.Speed = evt.newValue / 100f; break;
            case "SDMovementRadius": starDestroyerSettings.MovementRadius = evt.newValue; break;
        }

        _em.SetComponentData(_fighterSettingsEntity, fighterSettings);
        _em.SetComponentData(_starDestroyerSettingsEntity, starDestroyerSettings);
    }
}