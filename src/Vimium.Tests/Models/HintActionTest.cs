using Vimium.Models;
using Xunit;
using System.Text.Json;

namespace Vimium.Tests.Models;

/// <summary>
/// Tests for HintAction enum and ActionSlot JSON serialization.
/// </summary>
public class HintActionTest
{
    [Fact]
    public void HintAction_AllValues_RoundtripThroughJson()
    {
        var values = new[]
        {
            HintAction.Invoke,
            HintAction.LeftClick,
            HintAction.RightClick,
            HintAction.Hover,
            HintAction.Hover,
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        foreach (var value in values)
        {
            var json = JsonSerializer.Serialize(value, options);
            var restored = JsonSerializer.Deserialize<HintAction>(json, options);
            Assert.Equal(value, restored);
        }
    }

    [Fact]
    public void HintAction_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(HintAction.Invoke, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        Assert.Contains("\"Invoke\"", json);
        Assert.DoesNotContain("0", json); // not serialized as integer
    }

    [Fact]
    public void ActionSlot_Roundtrip_ThroughJson()
    {
        var slot = new ActionSlot
        {
            SlotIndex = 1,
            Modifier = "Ctrl+Shift",
            Action = HintAction.Hover,
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var json = JsonSerializer.Serialize(slot, options);
        var restored = JsonSerializer.Deserialize<ActionSlot>(json, options);

        Assert.NotNull(restored);
        Assert.Equal(slot.SlotIndex, restored!.SlotIndex);
        Assert.Equal(slot.Modifier, restored.Modifier);
        Assert.Equal(slot.Action, restored.Action);
    }

    [Fact]
    public void ActionSlot_DefaultValues_Roundtrip()
    {
        var slot = ActionSlot.CreateDefaults()[0];

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var json = JsonSerializer.Serialize(slot, options);
        var restored = JsonSerializer.Deserialize<ActionSlot>(json, options);

        Assert.NotNull(restored);
        Assert.Equal(0, restored!.SlotIndex);
        Assert.Equal("", restored.Modifier);
        Assert.Equal(HintAction.Invoke, restored.Action);
    }

    [Fact]
    public void ActionSlot_AllThreeDefaults_RoundtripAsArray()
    {
        var slots = ActionSlot.CreateDefaults();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var json = JsonSerializer.Serialize(slots, options);
        var restored = JsonSerializer.Deserialize<ActionSlot[]>(json, options);

        Assert.NotNull(restored);
        Assert.Equal(4, restored!.Length);
        Assert.Equal(0, restored[0].SlotIndex);
        Assert.Equal(1, restored[1].SlotIndex);
        Assert.Equal(2, restored[2].SlotIndex);
        Assert.Equal(3, restored[3].SlotIndex);
    }

    [Fact]
    public void HintAction_FromString_DeserializesCorrectly()
    {
        var json = "\"RightClick\"";
        var action = JsonSerializer.Deserialize<HintAction>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        Assert.Equal(HintAction.RightClick, action);
    }

    [Fact]
    public void ActionSlot_ModifierProperty_IsCamelCaseInJson()
    {
        var slot = new ActionSlot { SlotIndex = 1, Modifier = "Alt", Action = HintAction.Hover };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var json = JsonSerializer.Serialize(slot, options);

        Assert.Contains("\"slotIndex\"", json);
        Assert.Contains("\"modifier\"", json);
        Assert.Contains("\"action\"", json);
    }
}
