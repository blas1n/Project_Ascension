using UnityEngine;
using ProjectAscension.GameSimulation.Tutorial;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Resolves a <see cref="TutorialGuideStation"/> (an engine-free ID from the pure
    /// <see cref="TutorialGuideScript"/>) to a real world position in WHATEVER scene is currently
    /// loaded. City and Frontier each only build some of the stations, so a lookup that doesn't apply
    /// here fails cleanly (returns false) instead of returning a stale or wrong position — every
    /// source here is either a live Interactable (null-checked; Unity's own destroyed-object equality
    /// makes that safe across scene teardown) or gated behind the owning blockout's IsBuilt flag.
    ///
    /// Shared by <see cref="TutorialGuide"/> (it turns to face the station while it talks, then walks
    /// to it and stands there once dismissed — a living waypoint) and <see cref="ObjectiveMarker"/>
    /// (it draws the beacon there) — one place that knows where things are, so the two can never
    /// point at different spots: the guide always ends up standing directly under its own beacon.
    /// </summary>
    public static class TutorialGuideStations
    {
        public static bool TryResolve(TutorialGuideStation station, out Vector3 position)
        {
            switch (station)
            {
                // The yard's ENTRANCE, not its centre (playtest: the guide used to stand dead centre
                // among the dummies, reading as "the middle of a crowd" rather than a place with a
                // door — see CityBlockout.TrainingGroundEntrance).
                case TutorialGuideStation.TrainingGround when CityBlockout.IsBuilt:
                    position = CityBlockout.TrainingGroundEntrance;
                    return true;

                case TutorialGuideStation.EquipmentStation when CityBlockout.EquipmentInteractable != null:
                    position = CityBlockout.EquipmentInteractable.transform.position;
                    return true;

                case TutorialGuideStation.ContractBoard when CityBlockout.BoardInteractable != null:
                    position = CityBlockout.BoardInteractable.transform.position;
                    return true;

                case TutorialGuideStation.Clerk when CityNpc.ClerkInteractable != null:
                    position = CityNpc.ClerkInteractable.transform.position;
                    return true;

                case TutorialGuideStation.SurveyMarker when FrontierBlockout.IsBuilt:
                    position = FrontierBlockout.SurveyMarker;
                    return true;

                case TutorialGuideStation.ReturnPad when FrontierBlockout.IsBuilt:
                    position = FrontierBlockout.ReturnPad;
                    return true;

                default:
                    position = default;
                    return false;
            }
        }
    }
}
