using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;

namespace CKY.MultiAgentFramework.Demos.SmartHome.Providers
{
    /// <summary>
    /// 智能家居槽位定义提供者
    /// Smart home slot definition provider
    /// </summary>
    public class SmartHomeSlotDefinitionProvider : ISlotDefinitionProvider
    {
        private readonly Dictionary<string, IntentSlotDefinition> _definitions;

        public SmartHomeSlotDefinitionProvider()
        {
            _definitions = new Dictionary<string, IntentSlotDefinition>
            {
                ["control_device"] = new IntentSlotDefinition
                {
                    Intent = "control_device",
                    RequiredSlots = new()
                    {
                        new SlotDefinition
                        {
                            SlotName = "Device",
                            Description = "设备名称",
                            Type = SlotType.String
                        },
                        new SlotDefinition
                        {
                            SlotName = "Action",
                            Description = "操作类型",
                            Type = SlotType.Enumeration,
                            ValidValues = new[] { "打开", "关闭", "调节" }
                        },
                        new SlotDefinition
                        {
                            SlotName = "Location",
                            Description = "位置",
                            Type = SlotType.String
                        }
                    },
                    OptionalSlots = new()
                    {
                        new SlotDefinition
                        {
                            SlotName = "Mode",
                            Description = "模式",
                            Type = SlotType.Enumeration,
                            ValidValues = new[] { "制冷", "制热", "除湿", "送风" },
                            HasDefaultValue = true,
                            DefaultValue = "自动"
                        },
                        new SlotDefinition
                        {
                            SlotName = "Temperature",
                            Description = "温度",
                            Type = SlotType.Integer,
                            HasDefaultValue = true,
                            DefaultValue = 26
                        }
                    }
                },
                ["query_weather"] = new IntentSlotDefinition
                {
                    Intent = "query_weather",
                    RequiredSlots = new()
                    {
                        new SlotDefinition
                        {
                            SlotName = "Location",
                            Description = "城市",
                            Type = SlotType.String
                        }
                    },
                    OptionalSlots = new()
                    {
                        new SlotDefinition
                        {
                            SlotName = "Date",
                            Description = "日期",
                            Type = SlotType.DateTime,
                            HasDefaultValue = true,
                            DefaultValue = DateTime.Today
                        },
                        new SlotDefinition
                        {
                            SlotName = "TimeRange",
                            Description = "时间范围",
                            Type = SlotType.String,
                            ValidValues = new[] { "今天", "明天", "本周", "最近几天" }
                        }
                    }
                }
            };
        }

        public IntentSlotDefinition? GetDefinition(string intent)
        {
            return _definitions.GetValueOrDefault(intent);
        }

        public Dictionary<string, IntentSlotDefinition> GetAllDefinitions()
        {
            return _definitions;
        }
    }
}
