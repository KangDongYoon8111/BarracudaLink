using UnityEngine;
using UnityEngine.UI;

/* 
 * �� ��ũ��Ʈ�� Unity�� WebCamTexture�� ����Ͽ� ī�޶� Ȱ��ȭ�ϰ�,
 * RawImage UI ������Ʈ�� ī�޶��� �ǽð� �並 ǥ���ϴ� ������ �մϴ�.
 * AspectRatioFitter�� Ȱ���� ī�޶� ȭ���� ������ �����մϴ�.
 */
public class CameraView : MonoBehaviour
{
    public RawImage rawImage; // RawImage ������Ʈ, ī�޶� ������ ����� UI ���
    public AspectRatioFitter fitter; // ȭ���� ���� ���� ������ ���߱� ���� ������Ʈ

    private WebCamTexture webCamTexture; // ī�޶��� �ǽð� ������ �������� WebCamTexture
    private bool ratioSet; // ������ �����Ǿ����� ���θ� Ȯ���ϴ� �÷���

    private void Start()
    {
        // ��ķ �ʱ�ȭ �޼��� ȣ��
        InitWebCam();
    }

    private void Update()
    {
        // ��ķ�� �ػ󵵰� ��ȿ�ϰ� ������ ���� �������� �ʾҴٸ�
        if (webCamTexture.width > 100 && !ratioSet)
        {
            ratioSet = true; // ���� ���� �Ϸ� �÷��� Ȱ��ȭ
            SetAspectRatio(); // ȭ�� ���� ����
        }
    }

    // ��ķ �ʱ�ȭ
    private void InitWebCam()
    {
        // ����� ī�޶��� �̸��� ������ (�⺻������ ù ��° ī�޶� ����)
        string camName = WebCamTexture.devices[0].name;
        // WebCamTexture�� �����ϸ�, ȭ�� �ػ󵵿� ������ ����Ʈ ���� 
        webCamTexture = new WebCamTexture(camName, Screen.width, Screen.height, 30);
        // RawImage�� �ؽ�ó�� WebCamTexture�� ����
        rawImage.texture = webCamTexture;
        // ��ķ ���� ��� ����
        webCamTexture.Play();
    }

    // ȭ���� ���� ���� ���� ����
    private void SetAspectRatio()
    {
        // WebCamTexture�� ���ο� ���� ������ ����Ͽ� AspectRatioFitter�� ����
        fitter.aspectRatio = (float)webCamTexture.width / (float)webCamTexture.height;
    }

    /// <summary>
    /// �ǽð� ī�޶� ���� �޼���
    /// </summary>
    /// <returns>������ WebCamTexture</returns>
    public WebCamTexture GetCamImage()
    {
        return webCamTexture;
    }
}
