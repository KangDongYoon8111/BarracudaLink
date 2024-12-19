using UnityEngine; // Unity ���� �⺻ ���ӽ����̽�
using UnityEngine.Events; // Unity �̺�Ʈ �ý����� ����ϱ� ���� ���ӽ����̽�
using UnityEngine.Rendering; // GPU ���� ������ �۾��� ���� ���ӽ����̽�

// AI ���� �Է� �����͸� ȿ�������� ó���ϱ� ���� ��ó�� Ŭ������, GPU�� Ȱ���� ������� �̹����� ��ȯ
// �ǽð� ī�޶� �����͸� �Է¹޾� AI ���� �䱸�ϴ� �������� ��ȯ�ϴ� ������ ����
public class Preprocess : MonoBehaviour
{
    // RenderTexture : GPU���� ������ �۾��� ó���ϱ� ���� �ؽ�ó
    private RenderTexture renderTexture;

    // �̹����� ������(����, ���� ����)�� ũ��(�߶󳻱�) �������� ���� ����
    private Vector2 scale = new Vector2(1, 1); // �⺻ �������� 1:1
    private Vector2 offset = Vector2.zero; // �⺻ �������� 0(���� ����)

    // �̹��� �����͸� ó���� �� ȣ��� �ݹ� �Լ� (������ Ÿ��: byte �迭)
    UnityAction<byte[]> callback;

    /// <summary>
    /// WebCamTexture�� ������ �����ϰ� ũ���� �� GPU���� �����͸� �д� �޼���
    /// </summary>
    /// <param name="webCamTexture">�ǽð� ī�޶� �ؽ�ó</param>
    /// <param name="desiredSize">��ǥ ũ��(�ȼ� ����)</param>
    /// <param name="callback">��ó�� �Ϸ� �� ȣ���� �ݹ� �Լ�</param>
    public void ScaleAndCropImage(WebCamTexture webCamTexture, int desiredSize, UnityAction<byte[]> callback)
    {
        this.callback = callback; // �ݹ� �Լ��� Ŭ���� ������ ����

        // RenderTexture�� �������� �ʾҴٸ� �ʱ�ȭ
        if(renderTexture == null)
        {
            // ������ ũ��� �������� RenderTexture ����
            renderTexture = new RenderTexture(desiredSize, desiredSize, 0, RenderTextureFormat.ARGB32);
        }

        // ������ ���� ��� : ī�޶� �ؽ�ó�� ���̿� �ʺ� �������� ���
        scale.x = (float)webCamTexture.height / (float)webCamTexture.width;
        offset.x = (1 - scale.x) / 2f; // ������ ���� �� ���� ���� ���

        // GPU���� WebCamTexture �����͸� RenderTexture�� �����ϸ�, �����ϰ� ������ ����
        Graphics.Blit(webCamTexture, renderTexture, scale, offset);
        // GPU���� RenderTexture �����͸� �񵿱�� �б� ����
        AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, OnCompleteReadback);
    }

    /// <summary>
    /// GPU���� �����͸� �б� �Ϸ����� �� ȣ��Ǵ� �ݹ� �Լ�
    /// </summary>
    /// <param name="request">GPU �б� ��û ��ü</param>
    private void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        // GPU �б� �۾� �� ������ �߻��ߴ��� Ȯ��
        if(request.hasError)
        {
            Debug.Log("GPU readback error detected."); // ���� �α� ���
            return; // �۾� �ߴ�
        }

        // GPU���� �о�� �����͸� �ݹ� �Լ��� ����
        callback.Invoke(request.GetData<byte>().ToArray());
    }
}
