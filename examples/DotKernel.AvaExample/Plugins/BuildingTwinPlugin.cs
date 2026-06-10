using DotKernel;
using DotKernel.AvaExample.Twin;

namespace DotKernel.AvaExample.Plugins;

[KernelPlugin("Twin")]
public partial class BuildingTwinPlugin(BuildingTwinState state)
{
    [KernelFunction("get_snapshot")]
    [KernelDescription("读取产线数字孪生当前全量状态，操作前应先调用")]
    public Task<string> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.ToSnapshotJson());
    }

    [KernelFunction("set_light")]
    [KernelDescription("开启或关闭指定区域照明")]
    public Task<string> SetLightAsync(
        [KernelDescription("区域: assembly | storage | shipping")] string zone,
        [KernelDescription("true=开灯, false=关灯")] bool on,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.SetLight(zone, on));
    }

    [KernelFunction("set_conveyor")]
    [KernelDescription("设置传送带速度与启停")]
    public Task<string> SetConveyorAsync(
        [KernelDescription("区域: assembly | storage | shipping")] string zone,
        [KernelDescription("速度 0-100")] double speed,
        [KernelDescription("true=运行, false=停止")] bool running,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.SetConveyor(zone, (int)speed, running));
    }

    [KernelFunction("move_agv")]
    [KernelDescription("调度 AGV 从一个区域移动到另一个区域")]
    public Task<string> MoveAgvAsync(
        [KernelDescription("起始区域")] string from_zone,
        [KernelDescription("目标区域")] string to_zone,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.MoveAgv(from_zone, to_zone));
    }

    [KernelFunction("deploy_robot")]
    [KernelDescription("部署区域机器人执行任务")]
    public Task<string> DeployRobotAsync(
        [KernelDescription("区域: assembly | storage | shipping")] string zone,
        [KernelDescription("任务: idle | assemble | inspect | pack")] string task,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.DeployRobot(zone, task));
    }

    [KernelFunction("adjust_inventory")]
    [KernelDescription("调整区域库存数量")]
    public Task<string> AdjustInventoryAsync(
        [KernelDescription("区域: assembly | storage | shipping")] string zone,
        [KernelDescription("增减数量，负数表示出库")] double delta,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.AdjustInventory(zone, (int)delta));
    }

    [KernelFunction("raise_alert")]
    [KernelDescription("在区域触发告警横幅")]
    public Task<string> RaiseAlertAsync(
        [KernelDescription("区域: assembly | storage | shipping")] string zone,
        [KernelDescription("告警内容")] string message,
        [KernelDescription("级别: info | warning | critical")] string severity = "warning",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(state.RaiseAlert(zone, message, severity));
    }
}
