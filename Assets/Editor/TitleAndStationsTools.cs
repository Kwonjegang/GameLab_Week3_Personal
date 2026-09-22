using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Scene authoring commands; excluded from player builds.
public static class TitleAndStationsTools
{
    [MenuItem("Tools/Title and Stations/Apply Layout %&F9")]
    public static void ApplyLayout()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/FishLooper.unity") throw new InvalidOperationException("Open FishLooper first.");
        var roots = scene.GetRootGameObjects();
        var canvas = roots.Single(r => r.name == "Canvas").transform;
        foreach (string path in new[] { "TitleSequencePanel/FallingTitleText", "TitleSequencePanel/TitleStartPrompt" })
        {
            var label = canvas.Find(path).GetComponent<Text>();
            var outline = label.GetComponent<Outline>() ?? Undo.AddComponent<Outline>(label.gameObject);
            Undo.RecordObject(outline, "Outline title");
            outline.effectColor = Color.black;
            outline.effectDistance = path.EndsWith("FallingTitleText") ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }
        var hours = canvas.Find("TitleBlackFade/WorkHoursText").GetComponent<Text>();
        Undo.RecordObject(hours, "Update introduction");
        Undo.RecordObject(hours.rectTransform, "Resize introduction");
        hours.supportRichText = true;
        hours.text = "근무 시간은 오전 9시부터 오후 3시까지 입니다.\n\n<size=38>(썬배드에 F를 누르면 미니게임을 진행할 수 있습니다.)</size>";
        hours.rectTransform.sizeDelta = new Vector2(1700f, 320f);
        hours.alignment = TextAnchor.MiddleCenter;

        if (roots.All(r => r.name != "SunbedStations"))
        {
            var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            if (terrain == null) throw new InvalidOperationException("Missing terrain.");
            var holder = new GameObject("SunbedStations");
            Undo.RegisterCreatedObjectUndo(holder, "Create sunbed stations");
            var source = new GameObject("Station_00_Original");
            Undo.RegisterCreatedObjectUndo(source, "Group original minigame");
            source.transform.SetParent(holder.transform);
            source.transform.position = new Vector3(382.07f, 80.12f, 501.11f);
            string[] names = { "SunLounger_LowPoly", "SunLounger_LowPoly (1)", "StagePoints", "Enemy",
                "StageCamera", "AIFishGetCamera", "FishFocusCamera", "fish01_shade" };
            foreach (string name in names)
                Undo.SetTransformParent(roots.Single(r => r.name == name).transform, source.transform, "Group minigame");
            var station = Undo.AddComponent<SunbedStation>(source);
            var fields = new SerializedObject(station);
            fields.FindProperty("interactionPoint").objectReferenceValue = source.transform.Find("StagePoints/Player_InteractionPoint");
            fields.FindProperty("liePoint").objectReferenceValue = source.transform.Find("StagePoints/Player_LiePoint");
            fields.FindProperty("stageCamera").objectReferenceValue = source.transform.Find("StageCamera").GetComponent<Unity.Cinemachine.CinemachineCamera>();
            fields.FindProperty("enemyIntro").objectReferenceValue = source.GetComponentInChildren<EnemyStageIntro>(true);
            fields.ApplyModifiedProperties();

            Vector3[] locations = { new Vector3(430, 0, 380), new Vector3(550, 0, 520),
                new Vector3(610, 0, 700), new Vector3(700, 0, 340), new Vector3(570, 0, 210), new Vector3(420, 0, 630) };
            float[] angles = { 45, 180, 90, 225, 270, 0 };
            string[] labels = { "Southwest", "Central", "North", "Southeast", "South", "Northwest" };
            var placements = new StringBuilder();
            for (int i = 0; i < locations.Length; i++)
            {
                Quaternion rotation = Quaternion.Euler(0, angles[i], 0);
                Vector3 point = FindFlatLocation(terrain, locations[i], rotation, out float relief);
                var clone = UnityEngine.Object.Instantiate(source, holder.transform);
                clone.name = $"Station_{i + 1:00}_{labels[i]}";
                Undo.RegisterCreatedObjectUndo(clone, "Duplicate sunbed station");
                clone.transform.SetPositionAndRotation(point, rotation);
                var enemy = clone.GetComponentInChildren<EnemyStageIntro>(true);
                Vector3 spawn = enemy.transform.position;
                spawn.y = terrain.SampleHeight(spawn) + terrain.transform.position.y + 0.1f;
                enemy.transform.position = spawn;
                var move = clone.transform.Find("StagePoints/Enemy_SpawnPoint");
                Vector3 approach = move.position;
                approach.y = terrain.SampleHeight(approach) + terrain.transform.position.y + 0.1f;
                move.position = approach;
                placements.AppendLine($"{clone.name}: {point}, yaw={angles[i]}, ground relief={relief:F2}");
            }
            Directory.CreateDirectory("Temp/TitleQA");
            File.WriteAllText("Temp/TitleQA/stations.txt", placements.ToString());
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Title UI updated; seven complete sunbed stations saved in FishLooper.");
    }

    private static Vector3 FindFlatLocation(Terrain terrain, Vector3 desired, Quaternion rotation, out float relief)
    {
        Vector3 best = desired;
        float score = float.MaxValue;
        relief = float.MaxValue;
        for (int dx = -35; dx <= 35; dx += 5)
        for (int dz = -35; dz <= 35; dz += 5)
        {
            Vector3 center = desired + new Vector3(dx, 0, dz);
            float min = float.MaxValue, max = float.MinValue;
            bool valid = true;
            // Two loungers plus the space between them and the enemy approach.
            for (int x = -8; x <= 17; x += 5)
            for (int z = -5; z <= 30; z += 5)
            {
                Vector3 p = center + rotation * new Vector3(x, 0, z);
                Vector3 local = p - terrain.transform.position;
                var data = terrain.terrainData;
                int hx = Mathf.FloorToInt(local.x / data.size.x * data.holesResolution);
                int hz = Mathf.FloorToInt(local.z / data.size.z * data.holesResolution);
                if (hx < 0 || hz < 0 || hx >= data.holesResolution || hz >= data.holesResolution || data.IsHole(hx, hz)) { valid = false; break; }
                float y = terrain.SampleHeight(p) + terrain.transform.position.y;
                min = Mathf.Min(min, y);
                max = Mathf.Max(max, y);
            }
            if (!valid || min < 56f) continue;
            float candidateScore = (max - min) * 12f + new Vector2(dx, dz).magnitude * 0.1f;
            if (candidateScore >= score) continue;
            score = candidateScore;
            relief = max - min;
            best = new Vector3(center.x, (min + max) * 0.5f + 0.35f, center.z);
        }
        if (score == float.MaxValue || relief > 4f) throw new InvalidOperationException($"No flat dry site near {desired}; relief={relief}");
        return best;
    }

    [MenuItem("Tools/Title and Stations/Inspect Layout %&F8")]
    public static void InspectLayout()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var report = new StringBuilder();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            report.AppendLine($"ROOT {root.name} pos={root.transform.position} rot={root.transform.eulerAngles} scale={root.transform.localScale}");
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (root.name != "Canvas" && root.name != "MinimalGround" && root.name != "Player" && root.name != "Enemy")
                    report.AppendLine($"  {t.name} pos={t.position} rot={t.eulerAngles}");
        }
        foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            if (c is TerrainCollider || c.name.Contains("Ground")) report.AppendLine($"GROUND {c.name} {c.bounds}");
        foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            report.AppendLine($"TERRAIN {t.name} pos={t.transform.position} size={t.terrainData.size}");
        Directory.CreateDirectory("Temp/TitleQA");
        File.WriteAllText("Temp/TitleQA/layout.txt", report.ToString());
        Physics.SyncTransforms();
        var grid = new StringBuilder("x,z,y,nx,ny,nz,collider\n");
        for (float x = 100; x <= 900; x += 20)
        for (float z = 100; z <= 900; z += 20)
            foreach (var hit in Physics.RaycastAll(new Vector3(x, 600, z), Vector3.down, 1000))
                if (!hit.collider.isTrigger)
                    grid.AppendLine(FormattableString.Invariant($"{x},{z},{hit.point.y},{hit.normal.x},{hit.normal.y},{hit.normal.z},{hit.collider.name}"));
        File.WriteAllText("Temp/TitleQA/ground.csv", grid.ToString());
        Debug.Log("Layout inspection written to Temp/TitleQA.");
    }
}
