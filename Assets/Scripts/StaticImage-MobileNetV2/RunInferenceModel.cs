using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class RunInferenceModel : MonoBehaviour
{
    private const int INPUT_RESOLUTION_Y = 224; // �� �Է� �̹����� ����
    private const int INPUT_RESOLUTION_X = 224; // �� �Է� �̹����� �ʺ�

    [Header("Model ���� ������")]
    public NNModel srcModel; // ONNX ������ �Ű�� ��
    public TextAsset labelsAsset; // Ŭ���� ���̺��� ���Ե� �ؽ�Ʈ ����

    // ����� ���� ������ �Ӽ�
    [Header("Scene ���� ������")]
    public Texture2D[] inputImage; // �߷п� ����� �Է� �̹��� �迭
    public int selectedImage = 0; // ���� ���õ� �̹����� ID
    public RawImage displayImage; // ���õ� �̹����� ǥ���� UI ���
    public TextMeshProUGUI resultClassText; // �߷� ����� ǥ���� UI �ؽ�Ʈ
    public Material preprocessMaterial; // �̹��� ��ó���� ����� ��Ƽ���� (��: ����ȭ)
    public TMP_Dropdown backendDropdown; // �߷� �鿣�带 ������ ��Ӵٿ�

    // ���� �ʵ�
    private string inferenceBackend = "CSharpBurst"; // �⺻ �߷� �鿣��
    private Model model; // Barracuda�� �ε�� ��
    private IWorker engine; // ���� ������ ��Ŀ
    private Dictionary<string, Tensor> inputs = new Dictionary<string, Tensor>(); // �Է� �ټ��� �����ϴ� ��ųʸ�
    private string[] labels; // Ŭ���� ���̺� �迭
    private RenderTexture targetRT; // �̹��� ��ó���� ����� RenderTexture

    void Start()
    {
        //Application.targetFrameRate = 60; // ������ �ӵ��� 60FPS�� ����
        //Screen.orientation = ScreenOrientation.LandscapeLeft; // ȭ�� ������ ���η� ����

        AddBackendOptions(); // ��� ������ �鿣�� �ɼ��� ��Ӵٿ �߰�

        // ������ �ؽ�Ʈ ���Ͽ��� ���̺��� �Ľ�
        labels = labelsAsset.text.Split('\n');

        // Barracuda�� ����Ͽ� ONNX �� �ε�
        model = ModelLoader.Load(srcModel);

        // �Է� �̹��� �������� ���� RenderTexture �غ�
        // RenderTexture.GetTemporary : Unity���� RenderTexture�� �ӽ÷� �����ϴ� �۾��� ����
        // INPUT_RESOLUTION_X, INPUT_RESOLUTION_Y : ������ RenderTexture�� �ػ󵵸� ����
        // 0 : RenderTexture�� ���� ����(Depth Buffer)ũ�⸦ ���� ( 0�� ������� ������ �ǹ� )
        // RenderTextureFormat.ARGBHalf : RenderTexture�� �ȼ� ������ ����
        targetRT = RenderTexture.GetTemporary(INPUT_RESOLUTION_X, INPUT_RESOLUTION_Y, 0, RenderTextureFormat.ARGBHalf);

        // �⺻ �������� �߷� ����
        SelectBackendAndExecuteML();
    }

    /// <summary>
    /// ��� ������ �߷� �鿣�带 ��Ӵٿ �߰�
    /// </summary>
    private void AddBackendOptions()
    {
        List<string> options = new List<string>();

        options.Add("CSharpBurst"); // CPU ��� �鿣��
#if !UNITY_WEBGL
        options.Add("ComputePrecompiled"); // GPU ��� �鿣�� (WebGL������ ��� �Ұ�)
#endif
        options.Add("PixelShader"); // �ȼ� ���̴� ��� �鿣��

        // ��Ӵٿ� �ɼ� ������Ʈ
        backendDropdown.ClearOptions();
        backendDropdown.AddOptions(options);
    }

    private void SelectBackendAndExecuteML()
    {
        // ��Ӵٿ�� ���õ� �鿣�带 Ȯ��
        switch (backendDropdown.options[backendDropdown.value].text)
        {
            case "CSharpBurst":
                inferenceBackend = "CSharpBurst";
                break;
            case "ComputePrecompiled":
                inferenceBackend = "ComputePrecompiled";
                break;
            case "PixelShader":
                inferenceBackend = "PixelShader";
                break;
            default:
                Debug.Log("Invalid backend selection.");
                break;
        }

        // ���õ� �鿣��� �߷� ����
        ExecuteML(selectedImage);
    }

    public void ExecuteML(int imageID)
    {
        // ���õ� �̹��� �ε����� �����ϰ� ���÷��� ������Ʈ
        selectedImage = imageID;
        displayImage.texture = inputImage[selectedImage];

        // IWorker : Unity Barracuda���� �Ű�� ���� �����ϴ� �������̽�.
        // ���� ����(�߷�)�� �����ϸ�, �Է� �����͸� ó���ϰ� ����� ��ȯ�ϴ� ����
        // �Ϲ����� �����
        // 1. IWorker ���� : WorkerFactory.CreateWorker�� ���� Ư�� �鿣��� ���� �����Ͽ� �߷� ���� ����.
        // 2. �Է� ������ ó�� : �Է� �����͸� Tensor ��ü�� ��ȯ�Ͽ� IWorker�� ����.
        // 3. �߷� ���� : engine.Execute(inputTensor)�� ȣ���Ͽ� �� ����.
        // 4. ��� ��� Ȯ�� : engine.PeekOutput() ���� ���� ��� ���� ������.
        // 5. �ؼ��Ͽ� ����ڿ��� ǥ��
        // 6. ���ҽ� ����.

        // 1. IWorker ���� : ���õ� �鿣�忡 ���� �߷� ���� ����
        switch (inferenceBackend)
        {
            case "CSharpBurst":
                engine = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);
                break;
            case "ComputePrecompiled":
                engine = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
                break;
            case "PixelShader":
                engine = WorkerFactory.CreateWorker(WorkerFactory.Type.PixelShader, model);
                break;
            default:
                Debug.Log("Invalid backend selection.");
                break;
        }

        // 2. �Է� ������ ó�� : ���õ� �̹����� ��ó���ϰ� �ټ��� ��ȯ
        // Tensor : �Ű�� �𵨿��� �����͸� ó���ϴ� �⺻ �����̸�, ������ �迭�� �����͸� ǥ��.
        // �ι�° �μ��� �Էµ� 3�� ä���� ������ ��Ÿ����, RGB �̹����� �ǹ�.
        var input = new Tensor(PrepareTextureForInput(inputImage[selectedImage]), 3);

        // 3. �߷� ���� : �Է� �ټ��� ����Ͽ� �� �߷� ����
        engine.Execute(input);

        // 4. ��� ��� Ȯ�� : ��� �ټ��� ��������
        var output = engine.PeekOutput();

        // 5. �ؼ��Ͽ� ����ڿ��� ǥ�� : ���� ���� Ȯ���� ���� Ŭ������ �ε��� ã��
        var res = output.ArgMax()[0];
        // �ش� Ŭ���� ���̺�� Ȯ�� ��������
        var label = labels[res];
        Debug.Log("Label : " + res);
        var accuracy = output[res];
        Debug.Log("Accuracy : " + accuracy);
        // UI�� ��� ǥ��
        //resultClassText.text = $"{label}\n{Math.Round(accuracy * 100, 1)}%";
        resultClassText.text = $"{label}\n{accuracy}%";

        // 6. ���ҽ� ���� : �޸𸮸� �����Ͽ� ���ҽ� ����
        input.Dispose(); // �Է� �ټ��� �޸𸮿��� ����.
        engine.Dispose(); // �߷� ������ �޸𸮿��� ����.
        Resources.UnloadUnusedAssets(); // ������ �ʴ� ���ҽ��� �����Ͽ� �޸� ���� ����.
    }

    /// <summary>
    /// �Ű�� �𵨿� �Է��ϱ� ���� �̹����� ���� �䱸�ϴ� ���Ŀ� �°� ��ȯ
    /// </summary>
    private Texture PrepareTextureForInput(Texture2D src)
    {
        // RenderTexture�� Ȱ��ȭ�Ͽ� GPU���� �̹����� ó���ϱ� ���� �߰� ������ ���(RenderTexture)�� Ȱ��ȭ.
        RenderTexture.active = targetRT;
        // �ҽ� �ؽ�ó�� ��ó�� ��Ƽ������ ����Ͽ� RenderTexture�� �� : ���̴� ��� ����ȭ ����
        // �Է� �̹����� ������ ���(targetRT)���� �����ϸ�, preprocessMaterial�� ����� ����ȭ ����.
        Graphics.Blit(src, targetRT, preprocessMaterial);

        // ó���� �̹����� Texture2D�� �о����
        var result = new Texture2D(targetRT.width, targetRT.height, TextureFormat.RGBAHalf, false);
        // GPU�� �ִ� RenderTexture �����͸� CPU �޸𸮷� �����Ͽ� Texture2D�� ��ȯ.
        result.ReadPixels(new Rect(0, 0, targetRT.width, targetRT.height), 0, 0);
        // ����
        result.Apply();

        // ó���� Texture2D ��ȯ
        return result;
    }

    // Unity MonoBehaviour Ŭ�������� �����ϴ� �����ֱ� �޼���
    // GameObject�� �ı��� �� ȣ��Ǹ�, ��ü�� �� �̻� �ʿ����� ���� ��� ���ҽ��� �����ϴ� �� ���˴ϴ�.
    // ���ҽ� ������ �ʼ� �۾�����, �޸� ������ �����ϰ� ���ø����̼��� �������� �����մϴ�.
    private void OnDestroy()
    {
        // �߷� ���� �� �Է� �ټ��� �����Ͽ� �޸� Ȯ��
        engine?.Dispose();

        foreach(var key in inputs.Keys)
        {
            inputs[key].Dispose();
        }

        inputs.Clear();
    }
}
