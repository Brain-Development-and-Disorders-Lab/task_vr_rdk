using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StimulusManager : MonoBehaviour
{
    [SerializeField]
    private GameObject stimulusAnchor;

    // Use this for initialization
    void Start()
    {
        Debug.Log("StimulusManager active");
        CreateArc(4.0f, 0.0f, 181.0f, 1000, Color.white);
        CreateArc(4.0f, 180.0f, 361.0f, 1000, Color.white);
        CreateArc(3.0f, 0.0f, 181.0f, 1000, Color.cyan);
        CreateArc(3.0f, 180.0f, 361.0f, 1000, Color.magenta);
    }

    public void CreateArc(float radius, float startAngle, float endAngle, int segments, Color color)
    {
        // Create base GameObject
        GameObject arcObject = new GameObject();
        arcObject.transform.parent = stimulusAnchor.transform;
        arcObject.name = "rdk_arc_object";
        arcObject.AddComponent<LineRenderer>();

        // Generate points to form the arc
        Vector3[] arcPoints = new Vector3[segments];
        float angle = startAngle;
        float arcLength = endAngle - startAngle;

        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            arcPoints[i] = new Vector3(x, y, stimulusAnchor.transform.position.z);

            angle += (arcLength / segments);
        }

        // Create the LineRenderer
        LineRenderer line = arcObject.GetComponent<LineRenderer>();
        line.positionCount = arcPoints.Length;
        line.SetPositions(arcPoints);
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.material.SetColor("_Color", color);
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
    }
}
