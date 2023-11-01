using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using RectOpenCV = OpenCVForUnity.CoreModule.Rect;
using System;

public class OpenCVImageCapture : Singleton<OpenCVImageCapture>
{
    public Texture inputTexture;
    //public Texture2D croppedImage;
    public Texture2D resultTexture;

    // Update is called once per frame
    void Update()
    {
    }

    // Use this for initialization
    Point[] Convert2DVectorsToPoints(Vector2[] vectors)
    {
        Point[] points = new Point[vectors.Length];

        for (int i = 0; i < vectors.Length; i++)
            points[i] = new Point(vectors[i].x, vectors[i].y);

        return points;
    }

    void SetInputTexture(Texture input)
    {
        inputTexture = input;
    }

    void SetResultTexture(Texture2D result)
    {
        //ArSceneManager.instance.SetTextureToARObject(resultTexture);
        resultTexture = result;
    }

    Point[] InvertirY(Point[] puntos, int height)
    {
        Point[] puntosInvertidos = puntos.Clone() as Point[];

        for (int i = 0; i < puntos.Length; i++)
        {
            puntosInvertidos[i].y = height - puntos[i].y;
        }

        return puntosInvertidos;
    }

    RectOpenCV GetBoundingBox(Point[] points)
    {
        RectOpenCV boundBox = null;

        float[] xValues = new float[points.Length];
        float[] yValues = new float[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            xValues[i] = (float)points[i].x;
            yValues[i] = (float)points[i].y;
        }

        float minX = Mathf.Min(xValues);
        float maxX = Mathf.Max(xValues);
        float minY = Mathf.Min(yValues);
        float maxY = Mathf.Max(yValues);

        Point bottomLeftPt = new Point(minX, minY);
        Point topRightPt = new Point(maxX, maxY);

        boundBox = new RectOpenCV(bottomLeftPt, topRightPt);

        return boundBox;
    }

    Point[] GetBoundingBoxCornerPoints(Point[] points, RectOpenCV boundingBox)
    {
        Point[] newPoints = new Point[points.Length];
        float minX = boundingBox.x;
        float minY = boundingBox.y;

        for (int i = 0; i < points.Length; i++)
        {
            float newX = (float)points[i].x - minX;
            float newY = (float)points[i].y - minY;
            newPoints[i] = new Point(newX, newY);
        }

        return newPoints;
    }

    public Texture2D CaptureImageTargetTexture(Texture texture, Vector2[] points)
    {
        //SetInputTexture(ImageFocusValidator.instance.GetCameraTexture());
        //Vector2[] points = ImageFocusValidator.instance.Convert3DPointsTo2DCoor(inputTexture.width, inputTexture.height);
        SetInputTexture(texture);

        if (points == null) return null;

        Mat inputMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC4);

        Utils.texture2DToMat((Texture2D)inputTexture, inputMat);
        //Debug.Log("inputMat.ToString() " + inputMat.ToString());
        Debug.Log("PASO METODO texture2DToMat()");

        Point[] invertedPoints = Convert2DVectorsToPoints(points);
        //invertedPoints = InvertirY(invertedPoints, inputTexture.height);

        RectOpenCV boundingBox = GetBoundingBox(invertedPoints);
        invertedPoints = GetBoundingBoxCornerPoints(invertedPoints, boundingBox);

        Mat outputMat = inputMat.clone();

        inputMat = new Mat(inputMat, boundingBox);

        Mat src_mat = new Mat(4, 1, CvType.CV_32FC2);
        Mat dst_mat = new Mat(4,  1, CvType.CV_32FC2);

        Debug.Log("PASO CREACION DE MATs: src_mat y dst_mat");

        src_mat.put(0, 0,
            invertedPoints[0].x, invertedPoints[0].y,
            invertedPoints[1].x, invertedPoints[1].y,
            invertedPoints[2].x, invertedPoints[2].y,
            invertedPoints[3].x, invertedPoints[3].y
            );

        Debug.Log("PASO METODO src_mat.put()");

        dst_mat.put(0, 0, 0.0, 0.0, inputMat.cols(), 0.0, 0.0, inputMat.rows(), inputMat.cols(), inputMat.rows());

        Debug.Log("PASO METODO dst_mat.put()");

        Mat perspectiveTransform = Imgproc.getPerspectiveTransform(src_mat, dst_mat);

        Debug.Log("PASO METODO getPerspectiveTransform()");

        //Debug.Log("perspectiveTransform " + perspectiveTransform.dump());

        Imgproc.warpPerspective(inputMat, outputMat, perspectiveTransform, new Size(inputMat.cols(), inputMat.rows()));

        Debug.Log("PASO METODO warpPerspective()");

        Imgproc.resize(outputMat, outputMat, new Size(1024.0, 979.0));

        Debug.Log("PASO METODO resize()");

        Texture2D outputTexture = new Texture2D(outputMat.cols(), outputMat.rows(), TextureFormat.RGBA32, false);
        outputTexture = new Texture2D(outputMat.cols(), outputMat.rows(), TextureFormat.RGBA32, false);

        Utils.fastMatToTexture2D(outputMat, outputTexture);

        Debug.Log("PASO METODO fastMatToTexture2D()");

        SetResultTexture(outputTexture);
        //CanvasManager.instance.SetImageResult(outputTexture);

        //croppedImage = new Texture2D(inputMat.cols(), inputMat.rows(), TextureFormat.RGBA32, false);

        src_mat.Dispose();
        dst_mat.Dispose();
        inputMat.Dispose();
        outputMat.Dispose();
        perspectiveTransform.Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return outputTexture;
    }
}
