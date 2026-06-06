using UnityEngine;
using UnityEngine.EventSystems;

public class CorridorEntry : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;

        foreach (var cam in FindObjectsOfType<Camera>())
        {
            if (cam.gameObject.scene != gameObject.scene)
                cam.enabled = false;
        }

        foreach (var es in FindObjectsOfType<EventSystem>())
        {
            if (es.gameObject.scene != gameObject.scene)
                es.enabled = false;
        }
    }
}
