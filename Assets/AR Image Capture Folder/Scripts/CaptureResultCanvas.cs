using UnityEngine;
using UnityEngine.UI;

public class CaptureResultCanvas : MonoBehaviour
{
    #region PROPERTIES

    public Image resultImage;
    public Button captureBtn;

    #endregion

    #region MEMBERS

    public void EnableCaptureBtn()
    {
        captureBtn.gameObject.SetActive(true);
    }

    public void DisableCaptureBtn()
    {
        captureBtn.gameObject.SetActive(false);
    }

    public void ShowCaptureResult()
    {
        SetResultImage();
        resultImage.gameObject.SetActive(true);
    }

    public void HideCaptureResult()
    {
        resultImage.gameObject.SetActive(false);
    }

    void SetResultImage()
    {
        resultImage.sprite = Sprite.Create(
            OpenCVImageCapture.instance.resultTexture,
            new Rect(0, 0, OpenCVImageCapture.instance.resultTexture.width, OpenCVImageCapture.instance.resultTexture.height),
            new Vector2(0.5f, 0.5f)
            );
        resultImage.preserveAspect = true;
    }

    #endregion

}
