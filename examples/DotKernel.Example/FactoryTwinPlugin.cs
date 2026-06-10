using DotKernel;

namespace DotKernel.Example;

[KernelPlugin("Twin")]
public partial class FactoryTwinPlugin(FactoryTwinState state)
{
    [KernelFunction("get_snapshot")]
    [KernelDescription("读取产线状态")]
    public Task<string> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(state.ToSnapshotJson());

    [KernelFunction("set_light")]
    [KernelDescription("设置区域照明")]
    public Task<string> SetLightAsync(string zone, bool on, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetLight(zone, on));

    [KernelFunction("set_conveyor")]
    [KernelDescription("设置传送带")]
    public Task<string> SetConveyorAsync(string zone, double speed, bool running, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetConveyor(zone, (int)speed, running));

    [KernelFunction("move_agv")]
    [KernelDescription("调度 AGV")]
    public Task<string> MoveAgvAsync(string from_zone, string to_zone, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.MoveAgv(from_zone, to_zone));

    [KernelFunction("deploy_robot")]
    [KernelDescription("部署机器人")]
    public Task<string> DeployRobotAsync(string zone, string task, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.DeployRobot(zone, task));
}
