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
        CreateArc(4.0f, 0.0f, 181.0f, 1000, Color.white);
        CreateDots();
    }

    public void CreateArc(float radius, float startAngle, float endAngle, int segments, Color color)
    {
        // Create base GameObject
        GameObject arcObject = new GameObject();
        arcObject.name = "rdk_arc_object";
        arcObject.AddComponent<LineRenderer>();
        arcObject.transform.SetParent(stimulusAnchor.transform, false);

        // Generate points to form the arc
        Vector3[] arcPoints = new Vector3[segments];
        float angle = startAngle;
        float arcLength = endAngle - startAngle;

        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            arcPoints[i] = new Vector3(x, y, 0.0f);

            angle += arcLength / segments;
        }

        // Create the LineRenderer
        LineRenderer line = arcObject.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = arcPoints.Length;
        line.SetPositions(arcPoints);
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.material.SetColor("_Color", color);
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
    }

    public void CreateFixation()
    {
        // Create base GameObject
        GameObject fixationObjectParent = new GameObject();
        fixationObjectParent.name = "rdk_fixation_object";
        fixationObjectParent.transform.SetParent(stimulusAnchor.transform, false);

        // Create horizontal component
        GameObject fixationObjectHorizontal = new GameObject();
        fixationObjectHorizontal.name = "rdk_fixation_object_h";
        fixationObjectHorizontal.AddComponent<LineRenderer>();
        fixationObjectHorizontal.transform.SetParent(fixationObjectParent.transform, false);

        // Create horizontal LineRenderer
        LineRenderer horizontalLine = fixationObjectHorizontal.GetComponent<LineRenderer>();
        horizontalLine.useWorldSpace = false;
        horizontalLine.positionCount = 2;
        horizontalLine.SetPosition(0, new Vector3(-0.2f, 0.0f, 0.0f));
        horizontalLine.SetPosition(1, new Vector3(0.2f, 0.0f, 0.0f));
        horizontalLine.material = new Material(Shader.Find("Sprites/Default"));
        horizontalLine.material.SetColor("_Color", Color.white);
        horizontalLine.startWidth = 0.1f;
        horizontalLine.endWidth = 0.1f;

        // Create vertical component
        GameObject fixationObjectVertical = new GameObject();
        fixationObjectVertical.name = "rdk_fixation_object_v";
        fixationObjectVertical.AddComponent<LineRenderer>();
        fixationObjectVertical.transform.SetParent(fixationObjectParent.transform, false);

        // Create vertical LineRenderer
        LineRenderer verticalLine = fixationObjectVertical.GetComponent<LineRenderer>();
        verticalLine.useWorldSpace = false;
        verticalLine.positionCount = 2;
        verticalLine.SetPosition(0, new Vector3(0.0f, -0.2f, 0.0f));
        verticalLine.SetPosition(1, new Vector3(0.0f, 0.2f, 0.0f));
        verticalLine.material = new Material(Shader.Find("Sprites/Default"));
        verticalLine.material.SetColor("_Color", Color.white);
        verticalLine.startWidth = 0.1f;
        verticalLine.endWidth = 0.1f;
    }

    public void CreateDot()
    {
        // Create base GameObject
        GameObject dotObject = new GameObject();
        dotObject.name = "rdk_dot_object";
        dotObject.transform.SetParent(stimulusAnchor.transform, false);
        dotObject.AddComponent<SpriteRenderer>();

        // Create SpriteRenderer
        SpriteRenderer dotRenderer = dotObject.GetComponent<SpriteRenderer>();
        dotRenderer.drawMode = SpriteDrawMode.Sliced;
        dotRenderer.sprite = Resources.Load<Sprite>("Sprites/Dot");;
        dotRenderer.size = new Vector2(0.2f, 0.2f);
    }

    public void CreateDots()
    {
        CreateDot();
    }
}
