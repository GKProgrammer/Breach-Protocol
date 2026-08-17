using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraCullingExpander : MonoBehaviour
{
    [Tooltip("How much wider to expand the culling area (2.0 = double size)")]
    public float cullingMultiplier = 2.0f;
    
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        // Create an artificially expanded projection matrix for culling
        float expandedFOV = Mathf.Min(cam.fieldOfView * cullingMultiplier, 175f);
        float expandedFar = cam.farClipPlane * cullingMultiplier;

        Matrix4x4 expandedProjection = Matrix4x4.Perspective(
            expandedFOV, 
            cam.aspect, 
            cam.nearClipPlane, 
            expandedFar
        );

        // Tell the camera to use the huge area for culling, while rendering normally
        cam.cullingMatrix = expandedProjection * cam.worldToCameraMatrix;
    }

    private void OnDisable()
    {
        if (cam != null)
        {
            cam.ResetCullingMatrix();
        }
    }
}