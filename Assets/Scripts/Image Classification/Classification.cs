using UnityEngine;          // Unity �⺻ ���ӽ����̽�
using Unity.Barracuda;      // Barracuda : AI �� �߷� ���̺귯��
using TMPro;                // TextMeshPro : �ؽ�Ʈ ������ ���̺귯��
using System.Linq;          // LINQ : �÷��� ������ ������ ���� ��ƿ��Ƽ
using System.Collections;   // Coroutine ����� ���� ���ӽ����̽�
using System.Collections.Generic;   // Dictionary ����� ���� ���ӽ����̽�

public class Classification : MonoBehaviour
{
    private const int IMAGE_SIZE = 224;             // AI ���� �䱸�ϴ� �Է� �̹��� ũ�� (224*224)
    private const string INPUT_NAME = "images";     // Barracuda ���� �Է� �ټ� �̸�
    private const string OUTPUT_NAME = "Softmax";   // Barracuda ���� ��� �ټ� �̸�

    [Header("Model ���� ������")]
    public NNModel modelFile;    // ONNX ������ AI �� ���� (Unity NNModel ����)
    public TextAsset labelAsset; // �з� ����� ����� �� ���� (�ؽ�Ʈ ����)

    [Header("Scene ���� ������")]
    public CameraView cameraView;   // ī�޶� �Է��� �����ϴ� ��ũ��Ʈ
    public Preprocess preprocess;   // �̹��� ��ó���� ����ϴ� ��ũ��Ʈ
    public TextMeshProUGUI uiText;  // UI�� ����� ǥ���ϱ� ���� TextMeshPro ��ü

    private string[] labels;    // �з� ������ �� �迭
    private IWorker worker;     // Barracuda�� ��Ŀ: �� ������ ����

    void Start()
    {
        // ���� �ε��ϰ� Barracuda ��Ŀ�� ����
        var model = ModelLoader.Load(modelFile);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);

        // �� �����͸� �ε��Ͽ� �з� ����� Ȱ��
        LoadLabels();
    }

    /// <summary>
    /// �� �ؽ�Ʈ �����͸� �迭�� �ε�
    /// </summary>
    private void LoadLabels()
    {
        // �� ���Ͽ��� ����ǥ("") ���� �ؽ�Ʈ�� ����
        var stringArray = labelAsset.text.Split('"').Where((item, index) => index % 2 != 0);
        // ����� ���ڿ� �߿��� �� ��° �ؽ�Ʈ�� ������ �󺧷� ����
        labels = stringArray.Where((x, i) => i % 2 != 0).ToArray();
    }

    void Update()
    {
        // CameraView���� WebCamTexture ��ü�� ������
        WebCamTexture webCamTexture = cameraView.GetCamImage();

        // ���� �����ӿ��� ���ο� �̹����� ������Ʈ�Ǿ���, ��ȿ�� �ػ�(�ʺ� > 100)�� ���
        if(webCamTexture.didUpdateThisFrame && webCamTexture.width > 100)
        {
            // �̹��� �����͸� ��ó�� �� �� ���� �޼���(RunModel)�� ����
            preprocess.ScaleAndCropImage(webCamTexture, IMAGE_SIZE, RunModel);
        }
    }

    /// <summary>
    /// ��ó���� �̹��� �����͸� �޾� �� ����
    /// </summary>
    /// <param name="pixels">��ó���� �̹��� ������</param>
    private void RunModel(byte[] pixels)
    {
        // �񵿱� �ڷ�ƾ���� �� ����
        StartCoroutine(RunModelRoutine(pixels));
    }

    /// <summary>
    /// �� ���� �� ��� ó�� �ڷ�ƾ
    /// </summary>
    /// <param name="pixels">��ó���� �̹��� ������</param>
    private IEnumerator RunModelRoutine(byte[] pixels)
    {
        // �Է� �����͸� Tensor �������� ��ȯ
        Tensor tensor = TransformInput(pixels);

        // �Է� �����͸� ��ųʸ��� ������ Barracuda �𵨿� ����
        var inputs = new Dictionary<string, Tensor>
        {
            { INPUT_NAME, tensor } // �� �Է� �̸��� ������ ����
        };

        // �� ����
        worker.Execute(inputs);

        // �� ��� �ټ��� ������
        Tensor outputTensor = worker.PeekOutput(OUTPUT_NAME);
        
        // ��� �ټ� �����͸� ����Ʈ�� ��ȯ �� �ִ� Ž��
        List<float> temp = outputTensor.ToReadOnlyArray().ToList();
        float max = temp.Max(); // ���� ���� Ȯ����
        int index = temp.IndexOf(max); // �ִ��� �ε���

        // UI �ؽ�Ʈ�� �з� ��� ���
        uiText.text = labels[index];

        // �ټ� �޸� ����
        tensor.Dispose();
        outputTensor.Dispose();

        yield return null; // �ڷ�ƾ ����
    }

    /// <summary>
    /// �Է� ������(�̹���)�� Tensor�� ��ȯ
    /// </summary>
    /// <param name="pixels">��ó���� �̹��� ������</param>
    private Tensor TransformInput(byte[] pixels)
    {
        // �ȼ� �����͸� [-1, 1] ������ ����ȭ
        float[] transformedPixels = new float[pixels.Length];

        for(int i = 0; i < pixels.Length; i++)
        {
            transformedPixels[i] = (pixels[1] - 127f) / 128f; // ����ȭ ����
        }

        // ����ȭ�� �����͸� ������� Tensor ����
        return new Tensor(1, IMAGE_SIZE, IMAGE_SIZE, 3, transformedPixels);
    }
}
