using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDamageFlash : MonoBehaviour
{
    private struct ColorEntry
    {
        public Material material;
        public string property;
        public Color original;
    }

    private readonly List<ColorEntry> entries = new List<ColorEntry>();
    private Coroutine flashRoutine;

    public static void Play(Transform character)
    {
        if (character == null) return;
        CharacterDamageFlash effect = character.GetComponent<CharacterDamageFlash>();
        if (effect == null) effect = character.gameObject.AddComponent<CharacterDamageFlash>();
        effect.PlayFlash();
    }

    private void PlayFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            Restore();
        }
        entries.Clear();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            string objectName = renderer.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("body") || objectName.Contains("rod") ||
                objectName.Contains("line") || objectName.Contains("fish")) continue;
            foreach (Material material in renderer.materials)
            {
                string property = material.HasProperty("_BaseColor") ? "_BaseColor" :
                    material.HasProperty("_Color") ? "_Color" : null;
                if (property == null) continue;
                entries.Add(new ColorEntry { material = material, property = property, original = material.GetColor(property) });
            }
        }
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        foreach (ColorEntry entry in entries)
            entry.material.SetColor(entry.property, Color.Lerp(entry.original, Color.red, 0.78f));
        yield return new WaitForSeconds(0.14f);
        float elapsed = 0f;
        while (elapsed < 0.38f)
        {
            elapsed += Time.deltaTime;
            float amount = Mathf.Clamp01(elapsed / 0.38f);
            foreach (ColorEntry entry in entries)
                entry.material.SetColor(entry.property, Color.Lerp(Color.Lerp(entry.original, Color.red, 0.78f), entry.original, amount));
            yield return null;
        }
        Restore();
        flashRoutine = null;
    }

    private void Restore()
    {
        foreach (ColorEntry entry in entries)
            if (entry.material != null) entry.material.SetColor(entry.property, entry.original);
    }

    private void OnDisable() { Restore(); }
}
