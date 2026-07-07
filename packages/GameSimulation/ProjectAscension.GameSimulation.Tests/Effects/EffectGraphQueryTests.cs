using ProjectAscension.GameSimulation.Effects;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Effects
{
    public class EffectGraphQueryTests
    {
        private static EffectNode Cast(params EffectNode[] steps)
            => new Trigger(TriggerKind.OnCast, steps.Length == 1 ? steps[0] : new Sequence(steps));

        [Fact]
        public void DeliveryStyle_FromTheFirstEmit()
        {
            Assert.Equal("beam", EffectGraphQuery.DeliveryStyle(Cast(new Emit(EmitDelivery.Beam, 1), new Damage(1))));
            Assert.Equal("nova", EffectGraphQuery.DeliveryStyle(Cast(new Emit(EmitDelivery.Nova, 2))));
        }

        [Fact]
        public void DeliveryStyle_EmptyWhenNothingEmitted()
            => Assert.Equal("", EffectGraphQuery.DeliveryStyle(Cast(new Control(ControlEffect.Stun, 1))));

        [Fact]
        public void HasHoming_DetectsTheNode()
        {
            Assert.True(EffectGraphQuery.HasHoming(Cast(new Emit(EmitDelivery.Projectile, 1), new Homing(1))));
            Assert.False(EffectGraphQuery.HasHoming(Cast(new Emit(EmitDelivery.Projectile, 1))));
        }
    }
}
