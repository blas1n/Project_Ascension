namespace ProjectAscension.GameSimulation.Tutorial
{
    /// <summary>
    /// Where in the world the guide is telling the player to go — a station ID, not a Unity transform,
    /// so this stays engine-free (ADR: Unity is a shell). The client resolves each value to a real
    /// world position in whatever scene is currently loaded (or "not here"), and puts a marker there.
    /// </summary>
    public enum TutorialGuideStation
    {
        /// <summary>No place to point at. Three different reasons a step lands here:
        /// CreateCharacter is a screen, not a place, and hasn't happened in the world yet;
        /// FirstDiscovery arises from HOW you fight, not from reaching anywhere — marking a spot for
        /// it would turn a behavioural discovery into a fetch quest (docs: "발견은 보상으로 지급되지
        /// 않는다. 행동으로 발생한다"); and FirstDeath is a directed ambush (stage 8) — beaconing the
        /// exact spot you're about to die would spoil the one beat in the first hour that is
        /// supposed to catch you by surprise. The guide still SPEAKS for all three; it just points
        /// at nothing.</summary>
        None,
        TrainingGround,
        EquipmentStation,
        ContractBoard,
        Clerk,
        SurveyMarker,
        ReturnPad,
    }

    /// <summary>What the guide says, verbatim, and where it points while saying it.</summary>
    public sealed record TutorialGuideLine(string Text, TutorialGuideStation Station);

    /// <summary>
    /// The dedicated first-hour guide's SCRIPT — pure and deterministic (ADR: Unity is a shell), the
    /// same discipline as <see cref="TutorialDirector"/>. This never decides progression (that stays
    /// the director's job, and only its job); it only supplies what a guide NPC should SAY and POINT
    /// AT for whatever step the director says the player is on. The client owns everything physical:
    /// spawning the guide, walking it to the player, resolving a station ID to a real position,
    /// drawing the popup.
    ///
    /// Per docs/03-gameplay/first-hour-experience.md's own thesis — "목표는 튜토리얼이 아니다. 목표는
    /// 세계의 규칙을 직접 체험하게 하는 것이다. 플레이어는 설명을 듣지 않고, 경험을 통해 이해한다" — the
    /// guide LEADS and PROVOKES. Lines are short, in-world, and never explain what the player is about
    /// to feel; they say where to go (or nothing at all) and let the moment do the rest.
    /// </summary>
    public static class TutorialGuideScript
    {
        public static TutorialGuideLine For(TutorialStep step) => step switch
        {
            // 0 — a screen, not a place. The guide has nothing to add before you have a name; the
            // character sheet already owns the moment (and the UiFocus gate).
            TutorialStep.CreateCharacter =>
                new("You're new here. Everyone was, once.", TutorialGuideStation.None),

            // 2 — 훈련장. Minimal, present-tense, exactly what the doc asks for.
            TutorialStep.Training =>
                new("The yard's this way. Move. Jump. Hit something.", TutorialGuideStation.TrainingGround),

            // 3 — 첫 장비 선택. No steer toward a "right" pair — the doc is explicit that there isn't one.
            TutorialStep.ChooseEquipment =>
                new("Two hands, two choices — the rack won't judge you.", TutorialGuideStation.EquipmentStation),

            // 4 — 첫 발견. Behavioural, not a destination. No marker (see TutorialGuideStation.None).
            TutorialStep.FirstDiscovery =>
                new("Fight it your own way. See what happens.", TutorialGuideStation.None),

            // 5 — 첫 계약 (외곽 조사).
            TutorialStep.AcceptSurveyContract =>
                new("The board's got idle work on it. Go read it.", TutorialGuideStation.ContractBoard),

            // 6 — 지도 시스템. The map is earned by reaching the marker, not handed over.
            TutorialStep.EarnMap =>
                new("Outskirts. Find the marker, and come back with proof.", TutorialGuideStation.SurveyMarker),

            // 7 — 불가능한 계약 (심층 조사). The guide doesn't warn you it's too hard — you aren't
            // supposed to know that yet.
            TutorialStep.AcceptDeepContract =>
                new("There's another posting up. Take a look.", TutorialGuideStation.ContractBoard),

            // 8 — 첫 사망. A directed ambush. No warning, no marker — see TutorialGuideStation.None.
            TutorialStep.FirstDeath =>
                new("Go deeper, if you're going.", TutorialGuideStation.None),

            // 9 — 계약 위임. Offered right after the world has proved the point.
            TutorialStep.DelegateContract =>
                new("You don't have to finish that alone. Mira takes work off hands like yours.", TutorialGuideStation.Clerk),

            // 10 — 발주.
            TutorialStep.IssueContract =>
                new("Or pay someone else to carry it. She'll set that up too.", TutorialGuideStation.Clerk),

            // 11 — 첫 귀환.
            TutorialStep.Return =>
                new("Get back to the pad. The city keeps the lights on for you.", TutorialGuideStation.ReturnPad),

            // Complete — the guide has nothing left to say, and nowhere left to send you. It leaves.
            _ => new("", TutorialGuideStation.None),
        };
    }
}
