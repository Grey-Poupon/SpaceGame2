using System.Security.AccessControl;
using System;
using System.Threading;
using UnityEngine;
using System.Collections;

public class IntentLine : MonoBehaviour
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public int curveSegmentCount = 50;
    public float curveHeight = 3f;
    public float animationDuration = 3f;

    private LineRenderer lineRenderer;
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (startPoint != null && endPoint != null) DrawCurvedLine(startPoint, endPoint);
    }

    public void DrawCurvedLine(Vector3 startPoint, Vector3 endPoint)
    {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        transform.position = startPoint;
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

        Vector3[] linePoints = new Vector3[curveSegmentCount] ;
        for (int i = 0; i < curveSegmentCount; i++)
            linePoints [ i ] = CalculateBezierPoint(i/(float)curveSegmentCount, startPoint, endPoint, curveHeight);


        StartCoroutine(AnimateLine(linePoints));
    }

    private IEnumerator Animate(Vector3 startPoint, Vector3 endPoint, Vector3[] points)
    {
        float startTime = Time.time;
        Vector3 position;
        float t;
        float secondsPerSegment = animationDuration/curveSegmentCount;
        for (int i = 0; i <= curveSegmentCount; i++)
        {
            while (Time.time - startTime <= animationDuration)
            {
                float timeUntilSegment = i*secondsPerSegment - (Time.time - startTime);
                if (timeUntilSegment > 0) yield return new WaitForSeconds(timeUntilSegment);
                
                t = (Time.time - startTime) / animationDuration;
                
                lineRenderer.positionCount = i + 1;
                lineRenderer.SetPosition(i, points[i]);
            }
        }

    }

    private IEnumerator AnimateLine (Vector3[] points) 
    {
        float segmentDuration = animationDuration / curveSegmentCount ;
        lineRenderer.positionCount = curveSegmentCount;
        lineRenderer.SetPosition (0, points[0]);
        
        for (int i = 0; i < curveSegmentCount - 1; i++) {
            float startTime = Time.time ;

            Vector3 startPosition = points [i] ;
            Vector3 endPosition = points [i + 1] ;
            Vector3 pos = startPosition;

            while (pos != endPosition) {
                float t = (Time.time - startTime) / segmentDuration ;
                pos = Vector3.Lerp(startPosition, endPosition, t) ;

                // animate all other points except point at index i
                for (int j = i + 1; j < curveSegmentCount; j++)
                lineRenderer.SetPosition (j, pos) ;

                yield return null ;
            }
        }
    }

    Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, float height)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * (p0 + Vector3.up * height * 0.1f);
        p += 3 * u * tt * (p1 + Vector3.up * height * 0.1f);
        p += ttt * p1;

        return p;
    }
}
