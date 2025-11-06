using UnityEngine;
using System.Collections.Generic;
public class AU1FillFromOutline : MonoBehaviour
{
    public MeshFilter faceMeshFilter;

    public Color fillColor = new Color(0.45f, 0.70f, 1.00f, 1f);
    [Range(0,255)] public int alphaByte = 110;

    public List<int> leftOutline = new List<int> {421, 349, 159, 209, 231, 225, 226, 350, 423, 422};
    public List<int> rightOutline = new List<int> { 851, 782, 608, 657, 666, 1037, 1038, 1039, 1040, 852};

    public List<int> frontalisLeft = new List<int> {225, 226, 227, 350, 423, 422, 233, 232, 231};
    public List<int> frontalisRight = new List<int> {1040, 1039, 1038, 1037, 666, 667, 668, 852};
    public List<int> depressorLeft = new List<int> {422, 233, 232, 231, 209, 159, 349, 421};
    public List<int> depressorRight = new List<int> {666, 667, 668, 852, 851, 782, 608, 657};

    public float shrink = 1.1f;
    private ButtonManager buttonManager;
    private LiveFaceTracker tracker;

    private Vector3 neutralFrontalisLeft;
    private Vector3 neutralFrontalisRight;
    private Vector3 neutralDepressorLeft;
    private Vector3 neutralDepressorRight;
    private bool neutralPositionsCaptured = false;

    Color32[] baseColors;

    void Start()
    {
        if (!faceMeshFilter) faceMeshFilter = GetComponent<MeshFilter>();
        //buttonManager = FindObjectOfType<ButtonManager>();
        buttonManager = FindFirstObjectByType<ButtonManager>();
        tracker = FindFirstObjectByType<LiveFaceTracker>();
    }

    public void CaptureNeutralMusclePositions(Mesh neutralMesh)
    {
        if (neutralMesh == null) return;

        var verts = neutralMesh.vertices;

        neutralFrontalisLeft = CalculateAveragePosition(verts, frontalisLeft);
        neutralFrontalisRight = CalculateAveragePosition(verts, frontalisRight);
        neutralDepressorLeft = CalculateAveragePosition(verts, depressorLeft);
        neutralDepressorRight = CalculateAveragePosition(verts, depressorRight);

        neutralPositionsCaptured = true;
        Debug.Log($"[AU1Fill] Neutral positions captured - FrontalisL: {neutralFrontalisLeft}, DepressorL: {neutralDepressorLeft}");
    }

    private Vector3 CalculateAveragePosition(Vector3[] vertices, List<int> indices)
    {
        if (indices == null || indices.Count == 0) return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        int validCount = 0;
        
        foreach (int i in indices)
        {
            if (i >= 0 && i < vertices.Length)
            {
                sum += vertices[i];
                validCount++;
            }
        }
        
        return validCount > 0 ? sum / validCount : Vector3.zero;
    }

    void LateUpdate()
    {
        var mesh = faceMeshFilter ? faceMeshFilter.sharedMesh : null;
        if (mesh == null) return;

        if (baseColors == null || baseColors.Length != mesh.vertexCount)
        {
            baseColors = new Color32[mesh.vertexCount];
            for (int i = 0; i < baseColors.Length; i++) baseColors[i] = new Color32(0, 0, 0, 0);
        }

        //var colors = (Color32[])baseColors.Clone();
        var colors = mesh.colors32 != null && mesh.colors32.Length == mesh.vertexCount
            ? mesh.colors32
            : new Color32[mesh.vertexCount];
        byte r = (byte)(fillColor.r * 255f);
        byte g = (byte)(fillColor.g * 255f);
        byte b = (byte)(fillColor.b * 255f);
        byte a = (byte)Mathf.Clamp(alphaByte, 0, 255);

        // float darken = 0.6f;          
        // float redBoost = 1.15f;
        // float newAlpha = Mathf.Min(1.0f, fillColor.a + 0.4f); 

        // byte r = (byte)(Mathf.Clamp01(fillColor.r * darken * redBoost) * 255f);
        // byte g = (byte)(Mathf.Clamp01(fillColor.g * darken * 0.9f) * 255f);
        // byte b = (byte)(Mathf.Clamp01(fillColor.b * darken * 0.8f) * 255f);
        // byte a = (byte)(Mathf.Clamp(newAlpha * 255f, 0f, 255f));

        FillPolygon(mesh, leftOutline, shrink, r, g, b, a, colors);
        FillPolygon(mesh, rightOutline, shrink, r, g, b, a, colors);

        mesh.colors32 = colors;

        //if (faceMeshFilter && mesh != null)
        //if (buttonManager != null && faceMeshFilter && mesh != null && neutralPositionsCaptured)
        if (buttonManager != null && faceMeshFilter && mesh != null && neutralPositionsCaptured && tracker.isTrackingActive)
        {
            bool leftSelected = buttonManager.IsLeftSelected;

            Vector3 neutralFrontalis = leftSelected ? neutralFrontalisLeft : neutralFrontalisRight;
            Vector3 neutralDepressor = leftSelected ? neutralDepressorLeft : neutralDepressorRight;

            List<int> frontalis = leftSelected ? frontalisLeft : frontalisRight;
            List<int> depressor = leftSelected ? depressorLeft : depressorRight;

            float frontalisDisp = DisplacementFromNeutral(mesh, frontalis, neutralFrontalis);
            float depressorDisp = DisplacementFromNeutral(mesh, depressor, neutralDepressor);

            Debug.Log($"[AU1Fill] Frontalis displacement: {frontalisDisp:F4}, Depressor displacement: {depressorDisp:F4}");

            // List<int> dominantMuscle = (frontalisDisp > depressorDisp) ? frontalis : depressor;
            // string dominantName = (frontalisDisp > depressorDisp) ? "Frontalis" : "Depressor";
            // Debug.Log($"[AU1Fill] Dominant muscle: {dominantName}");

            // // bright overlay for dominant muscle
            // byte r2 = (byte)(fillColor.r * 255f); 
            // byte g2 = (byte)(fillColor.g * 255f);  
            // byte b2 = (byte)(fillColor.b * 255f);
            // byte a2 = (byte)Mathf.Clamp(alphaByte + 80, 0, 255);  // more opacity

            // var colors2 = mesh.colors32;
            // foreach (int id in dominantMuscle)
            // {
            //     if (id >= 0 && id < colors2.Length)
            //         colors2[id] = new Color32(r2, g2, b2, a2);
            // }

            // mesh.colors32 = colors2;
            

            // Only highlight if difference is significant -> add a threshold
            float threshold = 0.0002f;
            if (Mathf.Abs(frontalisDisp - depressorDisp) > threshold)
            {
                List<int> dominantMuscle = (frontalisDisp > depressorDisp) ? frontalis : depressor;
                string dominantName = (frontalisDisp > depressorDisp) ? "Frontalis" : "Depressor";  
                Debug.Log($"[AU1Fill] Dominant muscle: {dominantName}");  

                // Bright yellowish orange for dominant muscle
                byte r2 = 255;  
                byte g2 = 100;  
                byte b2 = 255;    
                byte a2 = 255; 

                // darker shade for dominant muscle
                // byte r2 = (byte)(fillColor.r * 255f * 0.4f); 
                // byte g2 = (byte)(fillColor.g * 255f * 0.4f);
                // byte b2 = (byte)(fillColor.b * 255f * 0.4f);
                // byte a2 = (byte)Mathf.Clamp(alphaByte, 0, 255);
                // float darken = 0.7f;          //how much to darken 
                // float redBoost = 1.4f;       //small red tint boost
                // float newAlpha = Mathf.Min(1.0f, fillColor.a + 0.6f); //add opacity

                // byte r2 = (byte)(Mathf.Clamp01(fillColor.r * darken * redBoost) * 255f);
                // byte g2 = (byte)(Mathf.Clamp01(fillColor.g * darken * 0.9f) * 255f);
                // byte b2 = (byte)(Mathf.Clamp01(fillColor.b * darken * 0.8f) * 255f);
                // byte a2 = (byte)(Mathf.Clamp(newAlpha * 255f, 0f, 255f));


                var colors2 = mesh.colors32;
                foreach (int id in dominantMuscle)
                {
                    if (id >= 0 && id < colors2.Length)
                        colors2[id] = new Color32(r2, g2, b2, a2);
                }

                mesh.colors32 = colors2;
                Debug.Log("[AU1Fill] Applied yellow/orange to dominant muscle");
            }
            else
            {
                Debug.Log("[AU1Fill] Difference too small, not highlighting");
            }
        }
    }
    float DisplacementFromNeutral(Mesh currentMesh, List<int> indices, Vector3 neutralPosition)
    {
        if (currentMesh == null || indices == null || indices.Count == 0) return 0f;
        if (!neutralPositionsCaptured) return 0f;

        var verts = currentMesh.vertices;
        Vector3 currentPosition = CalculateAveragePosition(verts, indices);
        
        // Calculate distance moved from neutral
        float displacement = Vector3.Distance(currentPosition, neutralPosition); 
        return displacement;
    }

    static void FillPolygon(Mesh mesh, List<int> outline, float shrink,
                            byte r, byte g, byte b, byte a, Color32[] colors)
    {
        if (outline == null || outline.Count < 3) return;

        var verts = mesh.vertices;
        var norms = mesh.normals;

        //Find plane from outline
        Vector3 center = Vector3.zero, n = Vector3.zero;
        int used = 0;
        foreach (int id in outline)
        {
            if (id < 0 || id >= verts.Length) continue;
            center += verts[id];
            if (norms != null && norms.Length == verts.Length) n += norms[id];
            used++;
        }
        if (used == 0) return;
        center /= used;
        n = n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.forward;

        //Basis on the plane (u,v)
        Vector3 u = Vector3.ProjectOnPlane(Vector3.right, n);
        if (u.sqrMagnitude < 1e-6f) u = Vector3.ProjectOnPlane(Vector3.up, n);
        u.Normalize();
        Vector3 v = Vector3.Cross(n, u);

        Vector2 Proj(Vector3 p) => new Vector2(Vector3.Dot(p - center, u),
                                               Vector3.Dot(p - center, v));

        //Build and angle-sort polygon
        var poly = new List<Vector2>(outline.Count);
        foreach (int id in outline) if (id >= 0 && id < verts.Length) poly.Add(Proj(verts[id]));
        if (poly.Count < 3) return;

        Vector2 c2 = Vector2.zero;
        for (int i = 0; i < poly.Count; i++) c2 += poly[i];
        c2 /= poly.Count;

        poly.Sort((A, B) =>
            Mathf.Atan2(A.y - c2.y, A.x - c2.x).CompareTo(Mathf.Atan2(B.y - c2.y, B.x - c2.x)));

        if (Mathf.Abs(shrink - 1f) > 1e-4f)
            for (int i = 0; i < poly.Count; i++) poly[i] = c2 + (poly[i] - c2) * shrink;

        Vector2 min = poly[0], max = poly[0];
        for (int i = 1; i < poly.Count; i++) { min = Vector2.Min(min, poly[i]); max = Vector2.Max(max, poly[i]); }

        //Fill
        for (int vi = 0; vi < verts.Length; vi++)
        {
            Vector2 p = Proj(verts[vi]);
            if (p.x < min.x || p.x > max.x || p.y < min.y || p.y > max.y) continue;
            if (PointInPolygon(p, poly)) colors[vi] = new Color32(r, g, b, a);
        }
    }

    static bool PointInPolygon(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 a = poly[i], b = poly[j];
            bool hit = ((a.y > p.y) != (b.y > p.y)) &&
                       (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + 1e-12f) + a.x);
            if (hit) inside = !inside;
        }
        return inside;
    }
}


