using StockSight.Core.Enums;
using StockSight.Core.Models;
using Xunit;

namespace StockSight.Tests.Models;

public class AlertTests
{
    [Theory]
    [InlineData(AlertCondition.Above, 100, 105, true)]
    [InlineData(AlertCondition.Above, 100, 95, false)]
    [InlineData(AlertCondition.Below, 100, 95, true)]
    [InlineData(AlertCondition.Below, 100, 105, false)]
    public void IsMet_EvaluatesConditionCorrectly(AlertCondition condition, decimal target, decimal price, bool expected)
    {
        var alert = new Alert { Condition = condition, TargetPrice = target };

        Assert.Equal(expected, alert.IsMet(price));
    }

    [Fact]
    public void IsMet_AtExactTarget_FiresForBothConditions()
    {
        var above = new Alert { Condition = AlertCondition.Above, TargetPrice = 50 };
        var below = new Alert { Condition = AlertCondition.Below, TargetPrice = 50 };

        Assert.True(above.IsMet(50));
        Assert.True(below.IsMet(50));
    }
}
