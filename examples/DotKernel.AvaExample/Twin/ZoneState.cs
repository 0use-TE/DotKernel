using CommunityToolkit.Mvvm.ComponentModel;

namespace DotKernel.AvaExample.Twin;

public partial class ZoneState : ObservableObject
{
    public ZoneState(string id, string name, bool lightOn, int conveyorSpeed, int inventory)
    {
        Id = id;
        Name = name;
        LightOn = lightOn;
        ConveyorSpeed = conveyorSpeed;
        Inventory = inventory;
    }

    public string Id { get; }

    public string Name { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RobotStatus))]
    private bool _lightOn;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConveyorStatus))]
    private int _conveyorSpeed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConveyorStatus))]
    private bool _conveyorRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RobotStatus))]
    private string _robotTask = "idle";

    [ObservableProperty]
    private int _inventory;

    [ObservableProperty]
    private bool _isAgvHere;

    [ObservableProperty]
    private string? _alertMessage;

    [ObservableProperty]
    private string _alertSeverity = "info";

    public string RobotStatus => RobotTask switch
    {
        "assemble" => "装配中",
        "inspect" => "巡检中",
        "pack" => "打包中",
        _ => "待机",
    };

    public string ConveyorStatus => ConveyorRunning
        ? $"运行 {ConveyorSpeed}%"
        : "停止";

    public void Reset(
        bool lightOn,
        int conveyorSpeed,
        int inventory,
        string robotTask,
        bool isAgvHere = false,
        bool conveyorRunning = false)
    {
        LightOn = lightOn;
        ConveyorSpeed = conveyorSpeed;
        ConveyorRunning = conveyorRunning || conveyorSpeed > 0;
        Inventory = inventory;
        RobotTask = robotTask;
        IsAgvHere = isAgvHere;
        AlertMessage = null;
        AlertSeverity = "info";
    }
}
