using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class RunInferenceModel : MonoBehaviour
{
    private const int INPUT_RESOLUTION_Y = 224; // 모델 입력 이미지의 높이
    private const int INPUT_RESOLUTION_X = 224; // 모델 입력 이미지의 너비

    [Header("Model 관련 데이터")]
    public NNModel srcModel; // ONNX 형식의 신경망 모델
    public TextAsset labelsAsset; // 클래스 레이블이 포함된 텍스트 파일

    // 사용자 정의 가능한 속성
    [Header("Scene 관련 데이터")]
    public Texture2D[] inputImage; // 추론에 사용할 입력 이미지 배열
    public int selectedImage = 0; // 현재 선택된 이미지의 ID
    public RawImage displayImage; // 선택된 이미지를 표시할 UI 요소
    public TextMeshProUGUI resultClassText; // 추론 결과를 표시할 UI 텍스트
    public Material preprocessMaterial; // 이미지 전처리에 사용할 머티리얼 (예: 정규화)
    public TMP_Dropdown backendDropdown; // 추론 백엔드를 선택할 드롭다운

    // 내부 필드
    private string inferenceBackend = "CSharpBurst"; // 기본 추론 백엔드
    private Model model; // Barracuda로 로드된 모델
    private IWorker engine; // 모델을 실행할 워커
    private Dictionary<string, Tensor> inputs = new Dictionary<string, Tensor>(); // 입력 텐서를 저장하는 딕셔너리
    private string[] labels; // 클래스 레이블 배열
    private RenderTexture targetRT; // 이미지 전처리에 사용할 RenderTexture

    void Start()
    {
        //Application.targetFrameRate = 60; // 프레임 속도를 60FPS로 제한
        //Screen.orientation = ScreenOrientation.LandscapeLeft; // 화면 방향을 가로로 설정

        AddBackendOptions(); // 사용 가능한 백엔드 옵션을 드롭다운에 추가

        // 제공된 텍스트 파일에서 레이블을 파싱
        labels = labelsAsset.text.Split('\n');

        // Barracuda를 사용하여 ONNX 모델 로드
        model = ModelLoader.Load(srcModel);

        // 입력 이미지 포맷팅을 위한 RenderTexture 준비
        // RenderTexture.GetTemporary : Unity에서 RenderTexture를 임시로 생성하는 작업을 수행
        // INPUT_RESOLUTION_X, INPUT_RESOLUTION_Y : 생성할 RenderTexture의 해상도를 설정
        // 0 : RenderTexture의 깊이 버퍼(Depth Buffer)크기를 설정 ( 0은 사용하지 않음을 의미 )
        // RenderTextureFormat.ARGBHalf : RenderTexture의 픽셀 포맷을 설정
        targetRT = RenderTexture.GetTemporary(INPUT_RESOLUTION_X, INPUT_RESOLUTION_Y, 0, RenderTextureFormat.ARGBHalf);

        // 기본 설정으로 추론 실행
        SelectBackendAndExecuteML();
    }

    /// <summary>
    /// 사용 가능한 추론 백엔드를 드롭다운에 추가
    /// </summary>
    private void AddBackendOptions()
    {
        List<string> options = new List<string>();

        options.Add("CSharpBurst"); // CPU 기반 백엔드
#if !UNITY_WEBGL
        options.Add("ComputePrecompiled"); // GPU 기반 백엔드 (WebGL에서는 사용 불가)
#endif
        options.Add("PixelShader"); // 픽셀 세이더 기반 백엔드

        // 드롭다운 옵션 업데이트
        backendDropdown.ClearOptions();
        backendDropdown.AddOptions(options);
    }

    private void SelectBackendAndExecuteML()
    {
        // 드롭다운에서 선택된 백엔드를 확인
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

        // 선택된 백엔드로 추론 실행
        ExecuteML(selectedImage);
    }

    public void ExecuteML(int imageID)
    {
        // 선택된 이미지 인덱스를 설정하고 디스플레이 업데이트
        selectedImage = imageID;
        displayImage.texture = inputImage[selectedImage];

        // IWorker : Unity Barracuda에서 신경망 모델을 실행하는 인터페이스.
        // 모델의 실행(추론)을 관리하며, 입력 데이터를 처리하고 결과를 반환하는 역할
        // 일반적인 사용방법
        // 1. IWorker 생성 : WorkerFactory.CreateWorker를 통해 특정 백엔드와 모델을 결합하여 추론 엔진 생성.
        // 2. 입력 데이터 처리 : 입력 데이터를 Tensor 객체로 변환하여 IWorker에 전달.
        // 3. 추론 실행 : engine.Execute(inputTensor)를 호출하여 모델 실행.
        // 4. 출력 결과 확인 : engine.PeekOutput() 으로 모델의 출력 값을 가져옴.
        // 5. 해석하여 사용자에게 표시
        // 6. 리소스 정리.

        // 1. IWorker 생성 : 선택된 백엔드에 따라 추론 엔진 생성
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

        // 2. 입력 데이터 처리 : 선택된 이미지를 전처리하고 텐서로 변환
        // Tensor : 신경망 모델에서 데이터를 처리하는 기본 단위이며, 다차원 배열로 데이터를 표현.
        // 두번째 인수로 입력된 3은 채널의 개수를 나타내며, RGB 이미지를 의미.
        var input = new Tensor(PrepareTextureForInput(inputImage[selectedImage]), 3);

        // 3. 추론 실행 : 입력 텐서를 사용하여 모델 추론 실행
        engine.Execute(input);

        // 4. 출력 결과 확인 : 출력 텐서를 가져오기
        var output = engine.PeekOutput();

        // 5. 해석하여 사용자에게 표시 : 가장 높은 확률을 가진 클래스의 인덱스 찾기
        var res = output.ArgMax()[0];
        // 해당 클래스 레이블과 확률 가져오기
        var label = labels[res];
        Debug.Log("Label : " + res);
        var accuracy = output[res];
        Debug.Log("Accuracy : " + accuracy);
        // UI에 결과 표시
        //resultClassText.text = $"{label}\n{Math.Round(accuracy * 100, 1)}%";
        resultClassText.text = $"{label}\n{accuracy}%";

        // 6. 리소스 정리 : 메모리를 해제하여 리소스 정리
        input.Dispose(); // 입력 텐서를 메모리에서 해제.
        engine.Dispose(); // 추론 엔진을 메모리에서 해제.
        Resources.UnloadUnusedAssets(); // 사용되지 않는 리소스를 정리하여 메모리 누수 방지.
    }

    /// <summary>
    /// 신경망 모델에 입력하기 위해 이미지를 모델이 요구하는 형식에 맞게 변환
    /// </summary>
    private Texture PrepareTextureForInput(Texture2D src)
    {
        // RenderTexture를 활성화하여 GPU에서 이미지를 처리하기 위한 중간 렌더링 대상(RenderTexture)을 활성화.
        RenderTexture.active = targetRT;
        // 소스 텍스처를 전처리 머티리얼을 사용하여 RenderTexture로 블릿 : 쉐이더 기반 정규화 수행
        // 입력 이미지를 렌더링 대상(targetRT)으로 복사하며, preprocessMaterial을 사용해 정규화 수행.
        Graphics.Blit(src, targetRT, preprocessMaterial);

        // 처리된 이미지를 Texture2D로 읽어오기
        var result = new Texture2D(targetRT.width, targetRT.height, TextureFormat.RGBAHalf, false);
        // GPU에 있는 RenderTexture 데이터를 CPU 메모리로 복사하여 Texture2D로 변환.
        result.ReadPixels(new Rect(0, 0, targetRT.width, targetRT.height), 0, 0);
        // 적용
        result.Apply();

        // 처리된 Texture2D 반환
        return result;
    }

    // Unity MonoBehaviour 클래스에서 제공하는 생명주기 메서드
    // GameObject가 파괴될 때 호출되며, 객체가 더 이상 필요하지 않을 경우 리소스를 정리하는 데 사용됩니다.
    // 리소스 관리의 필수 작업으로, 메모리 누구를 방지하고 애플리케이션의 안정성을 유지합니다.
    private void OnDestroy()
    {
        // 추론 엔진 및 입력 텐서를 해제하여 메모리 확보
        engine?.Dispose();

        foreach(var key in inputs.Keys)
        {
            inputs[key].Dispose();
        }

        inputs.Clear();
    }
}
