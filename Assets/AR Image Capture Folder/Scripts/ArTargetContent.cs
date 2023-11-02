using System.Collections.Generic;
using UnityEngine;
using Vuforia;

[RequireComponent(typeof(ImageTargetBehaviour), typeof(DefaultObserverEventHandler))]
public class ArTargetContent : MonoBehaviour
{
    public string imageName;
    public Texture2D targetTex;
    public GameObject arContent;
    public Renderer[] targetRenderers;
    
    [Header("Datos Imagen")]
    public Vector3 imgBounds;
    [Range(0f, 10f)]
    public float distRange = 1f;
    public float heightRange = 1f;
    
    ImageTargetBehaviour itb;
    DefaultObserverEventHandler observer;

    void Awake()
    {
        imageName = GetImageTargetBehaviour().TargetName;

        GetRenderers();

        arContent.SetActive(false);

        Vector2 bounds2D = (itb.GetSize() / 2f);
        
        imgBounds = new Vector3(bounds2D.x * distRange, (bounds2D.x + bounds2D.y) * 2f, bounds2D.y * distRange);

        observer = GetComponent<DefaultObserverEventHandler>();
        observer.OnTargetFound.AddListener(OnTargetFound);
        observer.OnTargetLost.AddListener(OnTargetLost);
    }

    private void OnDestroy()
    {
        observer.OnTargetFound.RemoveListener(OnTargetFound);
        observer.OnTargetLost.RemoveListener(OnTargetLost);
    }

    public ImageTargetBehaviour GetImageTargetBehaviour()
    {
        if (!itb)
            itb = GetComponent<ImageTargetBehaviour>();
        return itb;
    }

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

    public void ControlArContent(bool value)
    {
        arContent.SetActive(value);
    }

    public void ApplyCapture2ArContent(Texture2D capture)
    {
        foreach (Renderer rend in targetRenderers)
            rend.material.SetTexture("_MainTex",capture);
    }

    public void OnTargetFound()
    {
        ArTargetCapture.instance?.OnTargetFound(itb.TargetName);
    }

    public void OnTargetLost()
    {
        ControlArContent(false);
        ArTargetCapture.instance?.OnTargetLost();
    }
}
