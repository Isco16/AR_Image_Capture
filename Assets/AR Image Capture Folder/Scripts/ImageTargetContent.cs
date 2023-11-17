using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

[RequireComponent(typeof(ImageTargetBehaviour), typeof(DefaultObserverEventHandler))]
public class ImageTargetContent : MonoBehaviour
{
    #region PROPERTIES

    public GameObject arContent;
    [Range(1f,5f)] public float maxDistanceFactor = 5f;
    public Renderer[] targetRenderers;
    public bool drawImageFrame;
    public GameObject linePrefab;

    float minDistance;
    float maxDistance;
    Vector3[] worldCornersPos;
    
    ImageTargetBehaviour itb;
    DefaultObserverEventHandler observer;
    LineRenderer targetFrame;

    #endregion

    #region MONOBEHAVIOUR INHERITED MEMBERS 

    IEnumerator Start()
    {
        GetRenderers();

        arContent.SetActive(false);

        observer = GetComponent<DefaultObserverEventHandler>();
        observer.OnTargetFound.AddListener(OnTargetFound);
        observer.OnTargetLost.AddListener(OnTargetLost);

        yield return new WaitUntil(delegate(){ 
            return VuforiaBehaviour.Instance.VideoBackground != null;
        });

        yield return new WaitUntil(delegate () {
            return VuforiaBehaviour.Instance.VideoBackground.VideoBackgroundTexture != null;
        });

        yield return new WaitUntil(delegate () {
            return ImageTargetCapture.instance.arCam != null;
        });

        float horizontalFOV = Camera.VerticalToHorizontalFieldOfView(ImageTargetCapture.instance.arCam.fieldOfView, ImageTargetCapture.instance.arCam.aspect);
        float baseAngle = horizontalFOV / 2f;
        float FOVradians = baseAngle * Mathf.Deg2Rad;
        minDistance = (itb.GetSize().x / 2f) / Mathf.Tan(FOVradians);
        maxDistance = minDistance * maxDistanceFactor;
    }

    private void OnDestroy()
    {
        observer.OnTargetFound.RemoveListener(OnTargetFound);
        observer.OnTargetLost.RemoveListener(OnTargetLost);
    }

    #endregion

    #region PRIVATE MEMBERS

    void GetRenderers()
    {
        MeshRenderer[] meshes = arContent.GetComponentsInChildren<MeshRenderer>();
        SkinnedMeshRenderer[] skinnedMeshes = arContent.GetComponentsInChildren<SkinnedMeshRenderer>();
        List<Renderer> renderers = new List<Renderer>();

        if (meshes.Length > 0)
            renderers.AddRange(meshes);

        if (skinnedMeshes.Length > 0)
            renderers.AddRange(skinnedMeshes);

        targetRenderers = renderers.ToArray();

        meshes = null;
        skinnedMeshes = null;
        renderers.Clear();
    }

    void InstantiateLineRenderer()
    {
        GameObject obj = GameObject.Instantiate(linePrefab);
        obj.transform.parent = transform;
        targetFrame = obj.GetComponent<LineRenderer>();
    }

    #endregion

    #region PUBLIC MEMBERS

    public float GetMinDistance()
    {
        return minDistance;
    }

    public float GetMaxDistance()
    {
        return maxDistance;
    }

    public void OnTargetFound()
    {
        ImageTargetCapture.instance.OnTargetFound(itb.TargetName);
    }

    public void OnTargetLost()
    {
        ControlArContent(false);
        ImageTargetCapture.instance.OnTargetLost();
    }

    public ImageTargetBehaviour GetImageTargetBehaviour()
    {
        if (!itb)
            itb = GetComponent<ImageTargetBehaviour>();
        return itb;
    }

    public Vector3[] GetWorldCornersPositions()
    {
        Vector2 size = itb.GetSize();
        float width = size.x / 2f;
        float height = size.y / 2f;
        worldCornersPos = new Vector3[4];
        worldCornersPos[0] = transform.TransformPoint(width * -1, 0f, height);
        worldCornersPos[1] = transform.TransformPoint(width, 0f, height);
        worldCornersPos[2] = transform.TransformPoint(width * -1, 0f, height * -1);
        worldCornersPos[3] = transform.TransformPoint(width, 0f, height * -1);
        return worldCornersPos;
    }

    public void ControlArContent(bool value)
    {
        arContent.SetActive(value);
    }

    public void ApplyCapture2Renderers(Texture2D capture)
    {
        foreach (Renderer rend in targetRenderers)
            rend.material.SetTexture("_MainTex",capture);
    }

    public void DrawImageTargetFrame()
    {
        Vector3[] arrengedPoints = new Vector3[worldCornersPos.Length];
        arrengedPoints[0] = worldCornersPos[0];
        arrengedPoints[1] = worldCornersPos[1];
        arrengedPoints[2] = worldCornersPos[3];
        arrengedPoints[3] = worldCornersPos[2];
        if (!targetFrame)
            InstantiateLineRenderer();
        targetFrame.SetPositions(arrengedPoints);
    }

    #endregion
}
