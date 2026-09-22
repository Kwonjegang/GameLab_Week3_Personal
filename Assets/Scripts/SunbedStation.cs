using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// Keep one complete minigame under each station so it can be moved or deleted as a unit.
public class SunbedStation : MonoBehaviour
{
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform liePoint;
    [SerializeField] private CinemachineCamera stageCamera;
    [SerializeField] private EnemyStageIntro enemyIntro;
    private static readonly List<SunbedStation> ActiveStations = new List<SunbedStation>();

    public Transform InteractionPoint => interactionPoint;
    public Transform LiePoint => liePoint;
    public CinemachineCamera StageCamera => stageCamera;
    public EnemyStageIntro EnemyIntro => enemyIntro;
    public static bool HasStations => ActiveStations.Count > 0;

    private void OnEnable() { if (!ActiveStations.Contains(this)) ActiveStations.Add(this); }
    private void OnDisable() { ActiveStations.Remove(this); }

    public static SunbedStation FindNearest(Vector3 position, float range)
    {
        SunbedStation nearest = null;
        float best = range * range;
        foreach (SunbedStation station in ActiveStations)
        {
            if (station == null || station.interactionPoint == null || station.liePoint == null
                || station.stageCamera == null || station.enemyIntro == null) continue;
            Vector3 offset = station.interactionPoint.position - position;
            // Avoid interacting with a station on a different cliff or from deep underwater.
            if (Mathf.Abs(offset.y) > 12f) continue;
            offset.y = 0f;
            if (offset.sqrMagnitude > best) continue;
            best = offset.sqrMagnitude;
            nearest = station;
        }
        return nearest;
    }
}
