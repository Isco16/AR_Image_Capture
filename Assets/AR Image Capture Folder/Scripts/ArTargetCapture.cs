using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Vuforia;
using UnityEngine.Events;

public class ArTargetCapture : Singleton<ArTargetCapture>
{
    // ArSceneManager
    public Camera arCam;
    public AR_State arState;
    public List<ArTargetContent> imageTargets;
    public ArTargetContent currentTarget;
    public string currentTargetId;
    public bool isTracking;

    Dictionary<string, ArTargetContent> arTargetContents;
    public float timer;
    public float countDown;

    // ImageFocusValidator
    public Vector3[] cornerPositions;
    public bool allVisible = false;
    public bool stillCamera;

    public float rangoMovimiento;
    public int largoMovingList;
    public List<float> movingValList;

    public Transform videoPlaneTrans;
    Vector3 previousPos;
    MeshFilter meshFilter;
    MeshRenderer planeMeshRenderer;
    public float lastDif;

    public UnityEvent onTargetCaptured;

    #region UNITY MONOBEHAVIOUR METHODS

    protected override void Awake()
    {
        base.Awake();

        imageTargets = new List<ArTargetContent>();
        imageTargets.AddRange(FindObjectsOfType<ArTargetContent>());

        arTargetContents = new Dictionary<string, ArTargetContent>();

        foreach (ArTargetContent itc in imageTargets)
            arTargetContents.Add(itc.itb.TargetName, itc);

        arCam = Camera.main;
        ResetFocusingTimer();
        arState = AR_State.Searching;

        Camera.onPostRender += OnPostRenderCamera;
        
        Resources.UnloadUnusedAssets();
    }

    private IEnumerator Start()
    {
        GameObject plane = default(GameObject);

        yield return new WaitUntil(delegate() {
            plane = null;
            if (VuforiaBehaviour.Instance.transform.childCount > 0)
                plane = VuforiaBehaviour.Instance.transform.GetChild(0).gameObject;
            return plane; 
        }) ;

        videoPlaneTrans = plane.transform;
        meshFilter = videoPlaneTrans.gameObject.GetComponent<MeshFilter>();
        planeMeshRenderer = videoPlaneTrans.gameObject.GetComponent<MeshRenderer>();
        movingValList = new List<float>();
        cornerPositions = new Vector3[4];
    }

    void Update()
    {
        switch (arState)
        {
            case AR_State.Searching:
                ResetValidationValues();
                break;
            case AR_State.Focusing:
                FocusingBehaviour();
                break;
            case AR_State.Showing:
                break;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Camera.onPostRender -= OnPostRenderCamera;
    }

    #endregion

    public void OnTargetFound(string trgIdx)
    {
        currentTargetId = trgIdx;
        currentTarget = arTargetContents[currentTargetId];
        isTracking = true;
        arState = AR_State.Focusing;
    }

    public void OnTargetLost()
    {
        isTracking = AnyImageTargetTracked();

        if (!isTracking)
        {
            currentTargetId = string.Empty;
            currentTarget = null;
            arState = AR_State.Searching;
        }

        Resources.UnloadUnusedAssets();
    }

    // FUNCTION WHICH DETERMINE WHETHER TO TAKE A CAPTURE OR NOT
    public void FocusingBehaviour()
    {
        bool isFacing = FacingImageTarget(arCam.transform, currentTarget.transform, currentTarget.imgBounds, currentTarget.heightRange);

        if (isFacing)
        {
            timer -= Time.deltaTime;
        }
        else
        {
            ResetFocusingTimer();
        }

        if (timer < 0f)
        {
            Texture cameraTex = GetCameraTexture();
            Vector2[] projectedPoints = Convert3DPointsTo2DCoor(cameraTex.width, cameraTex.height);
            Texture2D capture = OpenCVImageCapture.instance.CaptureImageTargetTexture(cameraTex, projectedPoints);

            arState = AR_State.Showing;
            currentTarget.ControlArContent(true);
            currentTarget.ApplyCapture2ArContent(capture);
            ResetFocusingTimer();
            onTargetCaptured?.Invoke();
        }
    }

    public bool AnyImageTargetTracked()
    {
        bool isAny = false;

        foreach (KeyValuePair<string, ArTargetContent> itc in arTargetContents)
        {
            if (itc.Value.itb.TargetStatus.Status == Status.TRACKED)
            {
                isAny = true;
                break;
            }
        }

        return isAny;
    }

    void ResetFocusingTimer()
    {
        timer = countDown;
    }

    public void SetTextureToARObject(Texture2D texture)
    {

        for (int i = 0; i < currentTarget.targetRenderers.Length; i++)
        {
            for (int j = 0; j < currentTarget.targetRenderers[i].sharedMaterials.Length; j++)
                currentTarget.targetRenderers[i].sharedMaterials[j].mainTexture = texture;
        }

    }

    private void OnPostRenderCamera(Camera cam)
    {
        if(currentTarget)
            CheckAllCornersVisible(currentTarget.transform, currentTarget.itb.GetSize());
    }

    public bool CheckAllCornersVisible(Transform centerTran, Vector2 size)
    {
        bool allVisible = true;
        float width = size.x / 2f;
        float height = size.y / 2f;

        cornerPositions[0] = centerTran.TransformPoint(width * -1, 0f, height);
        cornerPositions[1] = centerTran.TransformPoint(width, 0f, height);
        cornerPositions[2] = centerTran.TransformPoint(width * -1, 0f, height * -1);
        cornerPositions[3] = centerTran.TransformPoint(width, 0f, height * -1);

        for (int i = 0; i < cornerPositions.Length; i++)
        {
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(cornerPositions[i]);

            if (viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f)
            {
                allVisible = false;
                break;
            }
        }

        this.allVisible = allVisible;
        return allVisible;
    }

    public void ResetValidationValues()
    {
        if (movingValList.Count > 0)
            movingValList.Clear();

        stillCamera = false;
        allVisible = false;
        previousPos = Vector3.zero;
    }

    //EN UPDATE
    public Vector2[] Convert3DPointsTo2DCoor(int imageWidth, int imageHeight)
    {
        if (!videoPlaneTrans) return null;

        Vector3[] proyectedPoints = GetProyectedPoints();
        Vector2[] cornerPoints2D = new Vector2[cornerPositions.Length];

        for (int i = 0; i < cornerPositions.Length; i++)
        {
            cornerPoints2D[i] = PlaneCoordinates(videoPlaneTrans, i, proyectedPoints[i], meshFilter.mesh.vertices[2], meshFilter.mesh.vertices[1] * 2f);
        }

        if (!CheckValid2dPoints(cornerPoints2D, imageWidth, imageHeight))
            cornerPoints2D = null;

        return cornerPoints2D;
    }

    public Vector3[] GetProyectedPoints()
    {
        if (!videoPlaneTrans) return null;

        Vector3 cameraPoint = arCam.transform.position;
        Vector3 planePoint = videoPlaneTrans.position;
        Vector3 planeNormDir = videoPlaneTrans.TransformDirection(Vector3.up).normalized;
        Vector3[] worldVertices = new Vector3[cornerPositions.Length];
        Vector3[] proyectedPoints = new Vector3[cornerPositions.Length];

        for (int i = 0; i < cornerPositions.Length; i++)
        {
            Vector3 cornerPoint = cornerPositions[i];

            float x = (((planePoint.y - cornerPoint.y) * planeNormDir.y + (planePoint.z - cornerPoint.z) * planeNormDir.z + planeNormDir.x * planePoint.x) * cameraPoint.x -
                cornerPoint.x * ((planePoint.y - cameraPoint.y) * planeNormDir.y + (planePoint.z - cameraPoint.z) * planeNormDir.z + planeNormDir.x * planePoint.x))
                /
                ((cameraPoint.x - cornerPoint.x) * planeNormDir.x + (cameraPoint.y - cornerPoint.y) * planeNormDir.y + (cameraPoint.z - cornerPoint.z) * planeNormDir.z);

            float y = (((planePoint.x - cornerPoint.x) * planeNormDir.x + (planePoint.z - cornerPoint.z) * planeNormDir.z + planeNormDir.y * planePoint.y) * cameraPoint.y -
                cornerPoint.y * ((planePoint.x - cameraPoint.x) * planeNormDir.x + (planePoint.z - cameraPoint.z) * planeNormDir.z + planeNormDir.y * planePoint.y))
                /
                ((cameraPoint.y - cornerPoint.y) * planeNormDir.y + (cameraPoint.x - cornerPoint.x) * planeNormDir.x + (cameraPoint.z - cornerPoint.z) * planeNormDir.z);

            float z = (((planePoint.y - cornerPoint.y) * planeNormDir.y + (planePoint.x - cornerPoint.x) * planeNormDir.x + planeNormDir.z * planePoint.z) * cameraPoint.z -
                cornerPoint.z * ((planePoint.y - cameraPoint.y) * planeNormDir.y + (planePoint.x - cameraPoint.x) * planeNormDir.x + planeNormDir.z * planePoint.z))
                /
                ((cameraPoint.z - cornerPoint.z) * planeNormDir.z + (cameraPoint.y - cornerPoint.y) * planeNormDir.y + (cameraPoint.x - cornerPoint.x) * planeNormDir.x);

            float k = ((cameraPoint.x - planePoint.x) * planeNormDir.x + (cameraPoint.y - planePoint.y) * planeNormDir.y + (cameraPoint.z - planePoint.z) * planeNormDir.z)
                /
                ((cameraPoint.x - cornerPoint.x) * planeNormDir.x + (cameraPoint.y - cornerPoint.y) * planeNormDir.y + (cameraPoint.z - cornerPoint.z) * planeNormDir.z);

            Vector3 projectedPoint = new Vector3(x, y, z);

            proyectedPoints[i] = projectedPoint;


#if(UNITY_EDITOR)
            Debug.DrawRay(cornerPositions[i], projectedPoint, Color.green);
            worldVertices[i] = videoPlaneTrans.TransformPoint(meshFilter.mesh.vertices[i]);
            Debug.DrawLine(worldVertices[i], projectedPoint, Color.red);
#endif
        }

        return proyectedPoints;
    }

    public Vector2 PlaneCoordinates(Transform planeTrans, int vertexIdx, Vector3 worldPoint, Vector3 originWorldPoint, Vector3 size)
    {
        Vector2 point2d = Vector2.zero;
        print(originWorldPoint);
        Vector3 planePoint = (planeTrans.InverseTransformPoint(worldPoint) - originWorldPoint);
        float xSize = planeMeshRenderer.sharedMaterial.mainTexture.width;
        float ySize = planeMeshRenderer.sharedMaterial.mainTexture.height;
        point2d = new Vector2((planePoint.x / size.x) * xSize, (planePoint.z / size.z) * ySize);

        print($"VERTEX {vertexIdx}: {point2d.x} - {point2d.y}");

        return point2d;
    }

    bool CheckValid2dPoints(Vector2[] points, int imageWidth, int imageHeight)
    {
        bool valid = true;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].x < 0f || points[i].x > imageWidth || points[i].y < 0f || points[i].y > imageHeight)
            {
                valid = false;
                break;
            }
        }

        return valid;
    }

    public Texture GetCameraTexture()
    {
        return CameraImageAccess.instance.GetCameraTexture();
    }

    public bool FacingImageTarget(Transform obj1, Transform obj2, Vector3 imgBounds, float heightRange)
    {
        Vector3 bounds = imgBounds;
        Vector3 rotation1 = obj1.forward;
        Vector3 rotation2 = obj2.up;
        float dotProd = Vector3.Dot(rotation1, rotation2);
        bool isMirrored = dotProd < -0.5f;

        //Vector3 CamtoLocalPos = obj2.InverseTransformPoint(obj1.position);
        Vector3 obj2Pos = obj2.position;

        bool insideWidth, insideLength, insideDepth = false;

        insideWidth = Mathf.Abs(obj2Pos.x) < bounds.x;
        insideLength = Mathf.Abs(obj2Pos.y) < bounds.y;
        insideDepth = Mathf.Abs(obj2Pos.z) < (bounds.z + heightRange) && Mathf.Abs(obj2Pos.z) > bounds.z;

        bool cameraStill = IsCameraStill();

        return isMirrored && insideWidth && insideLength && insideDepth && allVisible && cameraStill;
    }

    public bool IsCameraStill()
    {
        stillCamera = false;

        Vector3[] points3D = GetProyectedPoints();
        Vector3 viewportPos = videoPlaneTrans.InverseTransformPoint(points3D[0]);
        float widthDist = Vector3.Distance(videoPlaneTrans.InverseTransformPoint(points3D[0]), videoPlaneTrans.InverseTransformPoint(points3D[1]));

        float dif = Mathf.Abs(Vector3.Distance(previousPos, viewportPos));

        float difProp = dif / widthDist;

        float average = Mathf.Infinity;

        if (movingValList.Count < largoMovingList)
        {
            movingValList.Add(difProp);
        }
        else
        {
            movingValList.RemoveAt(0);
            movingValList.Add(difProp);
            average = movingValList.Max();

            if (average <= rangoMovimiento && difProp <= rangoMovimiento)
                stillCamera = true;
            else
                movingValList.Clear();

            lastDif = difProp;
        }

        previousPos = viewportPos;

        return stillCamera;
    }
}

public enum AR_State { Searching, Focusing, Showing }