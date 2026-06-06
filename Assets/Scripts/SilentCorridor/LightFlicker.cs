using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light targetLight;
    public Light[] extraLights;
    public float minInterval = 0.05f;
    public float maxInterval = 0.3f;
    public float flickerDuration = 5f;

    private bool isFlickering;
    private float timer;
    private float nextToggle;
    private float elapsed;
    private Light[] allLights;

    public void StartFlicker()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();

        var list = new System.Collections.Generic.List<Light>();
        if (targetLight != null) list.Add(targetLight);
        if (extraLights != null) list.AddRange(extraLights);
        allLights = list.ToArray();

        isFlickering = true;
        elapsed = 0f;
        nextToggle = Random.Range(minInterval, maxInterval);
    }

    public void StopFlicker()
    {
        isFlickering = false;
        if (allLights != null)
        {
            foreach (var l in allLights)
                if (l != null) l.enabled = true;
        }
    }

    void Update()
    {
        if (!isFlickering) return;

        timer += Time.deltaTime;
        elapsed += Time.deltaTime;

        if (flickerDuration > 0 && elapsed >= flickerDuration)
        {
            StopFlicker();
            return;
        }

        if (timer >= nextToggle)
        {
            bool on = true;
            if (allLights != null && allLights.Length > 0 && allLights[0] != null)
                on = !allLights[0].enabled;

            foreach (var l in allLights)
                if (l != null) l.enabled = on;

            timer = 0f;
            nextToggle = Random.Range(minInterval, maxInterval);
        }
    }
}
