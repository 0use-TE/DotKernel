using DotKernel;
using DotKernel.AvaExample.Twin;

namespace DotKernel.AvaExample.Plugins;

[KernelPlugin("Twin")]
public partial class BuildingTwinPlugin(BuildingTwinState state)
{
    [KernelFunction("get_snapshot")]
    [KernelDescription("Read full digital twin state; call before other operations")]
    public Task<string> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.ToSnapshotJson());
    }

    [KernelFunction("set_light")]
    [KernelDescription("Turn zone lighting on or off")]
    public Task<string> SetLightAsync(
        [KernelDescription("Zone: assembly | storage | shipping")] string zone,
        [KernelDescription("true=on, false=off")] bool on,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.SetLight(zone, on));
    }

    [KernelFunction("set_conveyor")]
    [KernelDescription("Set conveyor speed and run/stop")]
    public Task<string> SetConveyorAsync(
        [KernelDescription("Zone: assembly | storage | shipping")] string zone,
        [KernelDescription("Speed 0-100")] double speed,
        [KernelDescription("true=run, false=stop")] bool running,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.SetConveyor(zone, (int)speed, running));
    }

    [KernelFunction("move_agv")]
    [KernelDescription("Dispatch AGV from one zone to another")]
    public Task<string> MoveAgvAsync(
        [KernelDescription("Source zone")] string from_zone,
        [KernelDescription("Destination zone")] string to_zone,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.MoveAgv(from_zone, to_zone));
    }

    [KernelFunction("deploy_robot")]
    [KernelDescription("Deploy zone robot with a task")]
    public Task<string> DeployRobotAsync(
        [KernelDescription("Zone: assembly | storage | shipping")] string zone,
        [KernelDescription("Task: idle | assemble | inspect | pack")] string task,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.DeployRobot(zone, task));
    }

    [KernelFunction("adjust_inventory")]
    [KernelDescription("Adjust zone inventory count")]
    public Task<string> AdjustInventoryAsync(
        [KernelDescription("Zone: assembly | storage | shipping")] string zone,
        [KernelDescription("Delta; negative removes stock")] double delta,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.AdjustInventory(zone, (int)delta));
    }

    [KernelFunction("raise_alert")]
    [KernelDescription("Raise an alert banner in a zone")]
    public Task<string> RaiseAlertAsync(
        [KernelDescription("Zone: assembly | storage | shipping")] string zone,
        [KernelDescription("Alert message")] string message,
        [KernelDescription("Severity: info | warning | critical")] string severity = "warning",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.RaiseAlert(zone, message, severity));
    }
}
