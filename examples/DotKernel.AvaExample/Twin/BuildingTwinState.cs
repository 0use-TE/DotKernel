using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DotKernel.AvaExample.Twin;

public partial class BuildingTwinState : ObservableObject
{
    public BuildingTwinState()
    {
        Zones =
        [
            new ZoneState("assembly", "Assembly", lightOn: false, conveyorSpeed: 0, inventory: 12),
            new ZoneState("storage", "Storage", lightOn: true, conveyorSpeed: 20, inventory: 48) { IsAgvHere = true },
            new ZoneState("shipping", "Shipping", lightOn: false, conveyorSpeed: 0, inventory: 6),
        ];
    }

    public ObservableCollection<ZoneState> Zones { get; }

    public ObservableCollection<TwinActivityEntry> Activities { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AgvZoneName))]
    private string _agvZoneId = "storage";

    [ObservableProperty]
    private int _totalActions;

    public string AgvZoneName => GetZone(AgvZoneId).Name;

    public ZoneState GetZone(string zoneId)
    {
        var zone = Zones.FirstOrDefault(z => string.Equals(z.Id, zoneId, StringComparison.OrdinalIgnoreCase));
        if (zone is null)
        {
            throw new ArgumentException($"Unknown zone '{zoneId}'. Available: assembly, storage, shipping.");
        }

        return zone;
    }

    public string SetLight(string zoneId, bool on)
    {
        return RunOnUi(() =>
        {
            var z = GetZone(zoneId);
            z.LightOn = on;
            RecordActivityCore("Lighting", $"{z.Name} {(on ? "on" : "off")}");
            return $"{z.Name} lighting {(on ? "enabled" : "disabled")}.";
        });
    }

    public string SetConveyor(string zoneId, int speed, bool running)
    {
        return RunOnUi(() =>
        {
            var z = GetZone(zoneId);
            z.ConveyorSpeed = Math.Clamp(speed, 0, 100);
            z.ConveyorRunning = running && z.ConveyorSpeed > 0;
            RecordActivityCore("Conveyor", $"{z.Name} {(z.ConveyorRunning ? $"running {z.ConveyorSpeed}%" : "stopped")}");
            return $"{z.Name} conveyor {(z.ConveyorRunning ? $"running at {z.ConveyorSpeed}%" : "stopped")}.";
        });
    }

    public string MoveAgv(string fromZoneId, string toZoneId)
    {
        return RunOnUi(() =>
        {
            var from = GetZone(fromZoneId);
            var to = GetZone(toZoneId);

            if (!from.IsAgvHere)
            {
                return $"AGV is not at {from.Name}; currently at {GetZone(AgvZoneId).Name}.";
            }

            from.IsAgvHere = false;
            to.IsAgvHere = true;
            AgvZoneId = to.Id;
            RecordActivityCore("AGV", $"{from.Name} → {to.Name}");
            return $"AGV dispatched from {from.Name} to {to.Name}.";
        });
    }

    public string DeployRobot(string zoneId, string task)
    {
        return RunOnUi(() =>
        {
            var normalized = task.ToLowerInvariant();
            if (normalized is not ("idle" or "assemble" or "inspect" or "pack"))
            {
                return "Task must be idle, assemble, inspect, or pack.";
            }

            var z = GetZone(zoneId);
            z.RobotTask = normalized;
            RecordActivityCore("Robot", $"{z.Name} → {z.RobotStatus}");
            return $"{z.Name} robot status: {z.RobotStatus}.";
        });
    }

    public string AdjustInventory(string zoneId, int delta)
    {
        return RunOnUi(() =>
        {
            var z = GetZone(zoneId);
            z.Inventory = Math.Max(0, z.Inventory + delta);
            RecordActivityCore("Inventory", $"{z.Name} {(delta >= 0 ? "+" : "")}{delta} → {z.Inventory}");
            return $"{z.Name} inventory is now {z.Inventory} units.";
        });
    }

    public string RaiseAlert(string zoneId, string message, string severity)
    {
        return RunOnUi(() =>
        {
            var z = GetZone(zoneId);
            z.AlertMessage = message;
            z.AlertSeverity = severity.ToLowerInvariant() switch
            {
                "critical" => "critical",
                "info" => "info",
                _ => "warning",
            };
            RecordActivityCore("Alert", $"{z.Name}: {message}");
            return $"{z.Name} alert published.";
        });
    }

    public string ToSnapshotJson()
    {
        var builder = new StringBuilder();
        builder.Append("{\"agv_at\":\"").Append(AgvZoneId).Append("\",\"zones\":[");
        for (var i = 0; i < Zones.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            var z = Zones[i];
            builder.Append("{\"id\":\"").Append(z.Id)
                .Append("\",\"name\":\"").Append(z.Name)
                .Append("\",\"light_on\":").Append(z.LightOn ? "true" : "false")
                .Append(",\"conveyor_speed\":").Append(z.ConveyorSpeed)
                .Append(",\"conveyor_running\":").Append(z.ConveyorRunning ? "true" : "false")
                .Append(",\"robot_task\":\"").Append(z.RobotTask)
                .Append("\",\"inventory\":").Append(z.Inventory)
                .Append(",\"alert\":").Append(z.AlertMessage is null ? "null" : $"\"{z.AlertMessage}\"")
                .Append(",\"agv_present\":").Append(z.IsAgvHere ? "true" : "false")
                .Append('}');
        }

        builder.Append("]}");
        return builder.ToString();
    }

    public void Reset()
    {
        RunOnUi(() =>
        {
            Activities.Clear();
            TotalActions = 0;
            AgvZoneId = "storage";

            Zones[0].Reset(lightOn: false, conveyorSpeed: 0, inventory: 12, robotTask: "idle");
            Zones[1].Reset(lightOn: true, conveyorSpeed: 20, inventory: 48, robotTask: "idle", isAgvHere: true);
            Zones[2].Reset(lightOn: false, conveyorSpeed: 0, inventory: 6, robotTask: "idle");
        });
    }

    private void RecordActivityCore(string category, string message)
    {
        Activities.Insert(0, new TwinActivityEntry(category, message, DateTime.Now));
        while (Activities.Count > 14)
        {
            Activities.RemoveAt(Activities.Count - 1);
        }

        TotalActions++;
    }

    private static T RunOnUi<T>(Func<T> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return action();
        }

        return Dispatcher.UIThread.Invoke(action);
    }

    private static void RunOnUi(Action action) => RunOnUi(() =>
    {
        action();
        return 0;
    });
}
