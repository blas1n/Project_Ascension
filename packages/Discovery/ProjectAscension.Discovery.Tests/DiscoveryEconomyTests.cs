using ProjectAscension.SkillForge;
using Xunit;

namespace ProjectAscension.Discovery.Tests;

/// <summary>
/// The discovery economy (ADR 0010). Two rules, and they exist to protect the same promise —
/// progression-model.md: "신규 플레이어와 100시간 플레이어의 차이는 숫자가 아니다… 성장은 강함이 아니라
/// 세계 속 위치의 변화이다."
///
///   1. The Nth discovery in a space costs EXPONENTIALLY more. Grinding one act exhausts itself.
///   2. Numbers are NOT for sale. The budget buys what a skill DOES, never how hard it hits.
/// </summary>
public class DiscoveryEconomyTests
{
    private static readonly DiscoveryTuning T = DiscoveryTuning.Default;

    private static TriggerOutcome Score(int depth, params (string b, int n)[] behaviors)
    {
        var counts = new Dictionary<string, int>();
        foreach (var (b, n) in behaviors) counts[b] = n;
        return TriggerEvaluator.Evaluate(
            new BehaviorSignature(counts, System.Array.Empty<string>(), depth, Persistence: 0), T);
    }

    // --- 1. climbing costs exponentially more --------------------------------------------------

    [Fact]
    public void TheRungsAreSpacedEXPONENTIALLY_NotEvenly()
    {
        // A style yields one discovery per rarity rung (see the claim key), and you climb by scoring
        // higher. So the rung SPACING is the anti-inflation lever: 100 / 150 / 225 / 338 / 506.
        int[] rungs = { T.FireThreshold, T.UncommonScore, T.RareScore, T.EpicScore, T.LegendaryScore };

        for (int i = 1; i < rungs.Length; i++)
            Assert.True(rungs[i] > rungs[i - 1]);

        // Each step up costs MORE than the one before it — that is what "exponential" means here, and
        // it is the difference between a ladder you can grind and one you cannot.
        for (int i = 2; i < rungs.Length; i++)
            Assert.True(rungs[i] - rungs[i - 1] > rungs[i - 1] - rungs[i - 2]);
    }

    [Fact]
    public void RepetitionRaisesScoreLINEARLY_SoEachFurtherDiscoveryCostsExponentiallyMoreOfIt()
    {
        // Doing the same thing twice as long roughly doubles the score...
        int once = Score(0, ("RangedAttack", 20)).Score;
        int twice = Score(0, ("RangedAttack", 40)).Score;
        Assert.True(twice < once * 2 + 20); // linear-ish, not explosive

        // ...but each rung needs ~1.5x the last. So grinding buys you the first rung and then stalls:
        // the fourth discovery in a style would need five times the play of the first.
        Assert.True(T.LegendaryScore > T.FireThreshold * 4);
    }

    [Fact]
    public void ComposingBetter_IsTheWayUP_NotGrindingHarder()
    {
        // The whole argument in one assertion: a player who WEAVES reaches a rung that a player who
        // merely repeats cannot, however long they keep at it. The exit from the ceiling is ADR 0009's
        // grammar — not more of the same.
        var grind = Score(0, ("RangedAttack", 40), ("Chain:firearm", 10));
        var artistry = Score(0,
            ("RangedAttack", 10),
            ("Fuse:arcane>firearm", 8),
            ("Seq:jump>firearm", 5),
            ("While:firearm@airborne", 4));

        Assert.True(artistry.Score > grind.Score);
        Assert.True(artistry.Rarity > grind.Rarity); // a rarer rung, from artistry rather than volume
    }

    [Fact]
    public void RarityIsEarned_NotAccumulated()
    {
        // Rarity tracks how well you played THIS time, not how long you have been playing. There is no
        // drip of significance that eventually makes anything legendary.
        Assert.Equal(Rarity.Common, Score(0, ("Jump", 5)).Rarity);
        Assert.True(Score(0, ("Fuse:arcane>firearm", 20)).Rarity > Rarity.Common);
    }

    // --- 2. numbers are not for sale ----------------------------------------------------------

    [Fact]
    public void MagnitudeCostsNOTHING_SoSignificanceCannotBuyABiggerNumber()
    {
        // The correction that started this: cost used to be (tier+1) * kind, so a richer budget bought
        // HIGHER TIERS — i.e. bigger numbers. Now tier is free and flat-capped: a legendary skill and a
        // common one hit about as hard. What differs is what they DO.
        var weak = new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Projectile, 0));
        var mighty = new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Projectile, EffectGraph.MaxTier));

        Assert.Equal(EffectGraph.Cost(weak), EffectGraph.Cost(mighty));
    }

    [Fact]
    public void TheBudgetBuysBREADTH_MoreEffectsCostMore()
    {
        var one = new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Projectile, 3));
        var many = new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[]
        {
            new Emit(EmitDelivery.Projectile, 3),
            new Dot(3, 3),
            new Control(ControlEffect.Stun, 3),
        }));

        Assert.True(EffectGraph.Cost(many) > EffectGraph.Cost(one));
    }

    [Fact]
    public void AnUnaffordableSkill_LosesAnEFFECT_NotItsTeeth()
    {
        // The packer used to shave tiers first, so a skill you couldn't afford simply hit softer —
        // magnitude was the currency. Now a modest discovery gives up what it can DO, and whatever
        // survives still hits full force.
        var rich = new Trigger(TriggerKind.OnCast, new Sequence(new EffectNode[]
        {
            new Emit(EmitDelivery.Projectile, 3),
            new Control(ControlEffect.Stun, 3), // the costliest — first to go
            new Dot(3, 3),
        }));

        var packed = (Trigger)EffectGraphBudgetPacker.Pack(rich, new PowerBudget(7));
        var steps = ((Sequence)packed.Child).Steps;

        Assert.True(EffectGraph.Cost(packed) <= 7);
        Assert.DoesNotContain(steps, n => n is Control);            // an effect was surrendered...
        Assert.Contains(steps, n => n is Emit { Tier: 3 });          // ...and what remains is undiminished
    }
}
