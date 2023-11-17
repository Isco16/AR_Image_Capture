using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using RectOpenCV = OpenCVForUnity.CoreModule.Rect;
using System;

public class OpenCVImageCapture : Singleton<OpenCVImageCapture>
{
    #region PROPERTIES

    public Texture inputTexture;
    public Texture2D resultTexture;

    #endregion

    #region PUBLIC MEMBERS

    public Texture2D CaptureImageTargetTexture(Texture texture, Vector2[] points)
    {
        SetInputTexture(texture);

        if (points == null) return null;

        Mat inputMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC4);

        Utils.texture2DToMat((Texture2D)inputTexture, inputMat);

        Point[] invertedPoints = Convert2DVectorsToPoints(points);

        RectOpenCV boundingBox = GetBoundingBox(invertedPoints);
        invertedPoints = GetBoundingBoxCornerPoints(invertedPoints, boundingBox);

        Mat outputMat = inputMat.clone();

        inputMat = new Mat(inputMat, boundingBox);

        Mat src_mat = new Mat(4, 1, CvType.CV_32FC2);
        Mat dst_mat = new Mat(4,  1, CvType.CV_32FC2);

        src_mat.put(0, 0,
            invertedPoints[0].x, invertedPoints[0].y,
            invertedPoints[1].x, invertedPoints[1].y,
            invertedPoints[2].x, invertedPoints[2].y,
            invertedPoints[3].x, invertedPoints[3].y
            );

        dst_mat.put(0, 0, 0.0, 0.0, inputMat.cols(), 0.0, 0.0, inputMat.rows(), inputMat.cols(), inputMat.rows());

        Mat perspectiveTransform = Imgproc.getPerspectiveTransform(src_mat, dst_mat);

        Imgproc.warpPerspective(inputMat, outputMat, perspectiveTransform, new Size(inputMat.cols(), inputMat.rows()));

        Imgproc.resize(outputMat, outputMat, new Size(inputMat.cols(), inputMat.rows()));

        Texture2D outputTexture = new Texture2D(outputMat.cols(), outputMat.rows(), TextureFormat.RGBA32, false);

        Utils.fastMatToTexture2D(outputMat, outputTexture);

        SetResultTexture(outputTexture);

        src_mat.Dispose();
        dst_mat.Dispose();
        inputMat.Dispose();
        outputMat.Dispose();
        perspectiveTransform.Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return outputTexture;
    }

    #endregion

    #region PRIVATE MEMBERS

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
        resultTexture = result;
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

    #endregion
}
