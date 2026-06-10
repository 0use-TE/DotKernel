using System.Text;

namespace DotKernel.Example;

public sealed class FactoryTwinState
{
    private readonly Dictionary<string, ZoneSnapshot> _zones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["assembly"] = new("assembly", "装配区", false, 0, 12, "idle"),
        ["storage"] = new("storage", "仓储区", true, 20, 48, "idle") { AgvPresent = true },
        ["shipping"] = new("shipping", "出货区", false, 0, 6, "idle"),
    };

    public string AgvZoneId { get; private set; } = "storage";

    public string SetLight(string zone, bool on)
    {
        var z = Get(zone);
        z.LightOn = on;
        return $"{z.Name} 照明={(on ? "开" : "关")}";
    }

    public string SetConveyor(string zone, int speed, bool running)
    {
        var z = Get(zone);
        z.ConveyorSpeed = Math.Clamp(speed, 0, 100);
        z.ConveyorRunning = running && z.ConveyorSpeed > 0;
        return $"{z.Name} 传送带={z.ConveyorSpeed}%";
    }

    public string MoveAgv(string fromZone, string toZone)
    {
        var from = Get(fromZone);
        var to = Get(toZone);
        if (!from.AgvPresent)
        {
            return $"AGV 不在 {from.Name}";
        }

        from.AgvPresent = false;
        to.AgvPresent = true;
        AgvZoneId = to.Id;
        return $"AGV {from.Name}->{to.Name}";
    }

    public string DeployRobot(string zone, string task)
    {
        var z = Get(zone);
        z.RobotTask = task;
        return $"{z.Name} 机器人={task}";
    }

    public string ToSnapshotJson()
    {
        var builder = new StringBuilder();
        builder.Append("{\"agv_at\":\"").Append(AgvZoneId).Append("\",\"zones\":[");
        var first = true;
        foreach (var zone in _zones.Values)
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            builder.Append("{\"id\":\"").Append(zone.Id)
                .Append("\",\"name\":\"").Append(zone.Name)
                .Append("\",\"light_on\":").Append(zone.LightOn ? "true" : "false")
                .Append(",\"conveyor_speed\":").Append(zone.ConveyorSpeed)
                .Append(",\"conveyor_running\":").Append(zone.ConveyorRunning ? "true" : "false")
                .Append(",\"robot_task\":\"").Append(zone.RobotTask)
                .Append("\",\"inventory\":").Append(zone.Inventory)
                .Append(",\"agv_present\":").Append(zone.AgvPresent ? "true" : "false")
                .Append('}');
        }

        builder.Append("]}");
        return builder.ToString();
    }

    private ZoneSnapshot Get(string zoneId)
    {
        if (!_zones.TryGetValue(zoneId, out var zone))
        {
            throw new ArgumentException($"未知区域 {zoneId}");
        }

        return zone;
    }

    private sealed class ZoneSnapshot(string id, string name, bool lightOn, int conveyorSpeed, int inventory, string robotTask)
    {
        public string Id { get; } = id;
        public string Name { get; } = name;
        public bool LightOn { get; set; } = lightOn;
        public int ConveyorSpeed { get; set; } = conveyorSpeed;
        public bool ConveyorRunning { get; set; }
        public int Inventory { get; set; } = inventory;
        public string RobotTask { get; set; } = robotTask;
        public bool AgvPresent { get; set; }
    }
}
