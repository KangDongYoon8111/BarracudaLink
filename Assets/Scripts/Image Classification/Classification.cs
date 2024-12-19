using UnityEngine;          // Unity 기본 네임스페이스
using Unity.Barracuda;      // Barracuda : AI 모델 추론 라이브러리
using TMPro;                // TextMeshPro : 텍스트 렌더링 라이브러리
using System.Linq;          // LINQ : 컬렉션 데이터 조작을 위한 유틸리티
using System.Collections;   // Coroutine 사용을 위한 네임스페이스
using System.Collections.Generic;   // Dictionary 사용을 위한 네임스페이스

public class Classification : MonoBehaviour
{
    private const int IMAGE_SIZE = 224;             // AI 모델이 요구하는 입력 이미지 크기 (224*224)
    private const string INPUT_NAME = "images";     // Barracuda 모델의 입력 텐서 이름
    private const string OUTPUT_NAME = "Softmax";   // Barracuda 모델의 출력 텐서 이름

    [Header("Model 관련 데이터")]
    public NNModel modelFile;    // ONNX 형식의 AI 모델 파일 (Unity NNModel 형태)
    public TextAsset labelAsset; // 분류 결과에 사용할 라벨 파일 (텍스트 형태)

    [Header("Scene 관련 데이터")]
    public CameraView cameraView;   // 카메라 입력을 제공하는 스크립트
    public Preprocess preprocess;   // 이미지 전처리를 담당하는 스크립트
    public TextMeshProUGUI uiText;  // UI에 결과를 표시하기 위한 TextMeshPro 객체

    private string[] labels;    // 분류 가능한 라벨 배열
    private IWorker worker;     // Barracuda의 워커: 모델 실행을 관리

    void Start()
    {
        // 모델을 로드하고 Barracuda 워커를 생성
        var model = ModelLoader.Load(modelFile);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);

        // 라벨 데이터를 로드하여 분류 결과에 활용
        LoadLabels();
    }

    /// <summary>
    /// 라벨 텍스트 데이터를 배열로 로드
    /// </summary>
    private void LoadLabels()
    {
        // 라벨 파일에서 따옴표("") 안의 텍스트만 추출
        var stringArray = labelAsset.text.Split('"').Where((item, index) => index % 2 != 0);
        // 추출된 문자열 중에서 두 번째 텍스트만 가져와 라벨로 설정
        labels = stringArray.Where((x, i) => i % 2 != 0).ToArray();
    }

    void Update()
    {
        // CameraView에서 WebCamTexture 객체를 가져옴
        WebCamTexture webCamTexture = cameraView.GetCamImage();

        // 현재 프레임에서 새로운 이미지가 업데이트되었고, 유효한 해상도(너비 > 100)인 경우
        if(webCamTexture.didUpdateThisFrame && webCamTexture.width > 100)
        {
            // 이미지 데이터를 전처리 후 모델 실행 메서드(RunModel)에 전달
            preprocess.ScaleAndCropImage(webCamTexture, IMAGE_SIZE, RunModel);
        }
    }

    /// <summary>
    /// 전처리된 이미지 데이터를 받아 모델 실행
    /// </summary>
    /// <param name="pixels">전처리된 이미지 데이터</param>
    private void RunModel(byte[] pixels)
    {
        // 비동기 코루틴으로 모델 실행
        StartCoroutine(RunModelRoutine(pixels));
    }

    /// <summary>
    /// 모델 실행 및 결과 처리 코루틴
    /// </summary>
    /// <param name="pixels">전처리된 이미지 데이터</param>
    private IEnumerator RunModelRoutine(byte[] pixels)
    {
        // 입력 데이터를 Tensor 형식으로 변환
        Tensor tensor = TransformInput(pixels);

        // 입력 데이터를 딕셔너리로 구성해 Barracuda 모델에 전달
        var inputs = new Dictionary<string, Tensor>
        {
            { INPUT_NAME, tensor } // 모델 입력 이름과 데이터 매핑
        };

        // 모델 실행
        worker.Execute(inputs);

        // 모델 출력 텐서를 가져옴
        Tensor outputTensor = worker.PeekOutput(OUTPUT_NAME);
        
        // 출력 텐서 데이터를 리스트로 변환 후 최댓값 탐색
        List<float> temp = outputTensor.ToReadOnlyArray().ToList();
        float max = temp.Max(); // 가장 높은 확률값
        int index = temp.IndexOf(max); // 최댓값의 인덱스

        // UI 텍스트에 분류 결과 출력
        uiText.text = labels[index];

        // 텐서 메모리 해제
        tensor.Dispose();
        outputTensor.Dispose();

        yield return null; // 코루틴 종료
    }

    /// <summary>
    /// 입력 데이터(이미지)를 Tensor로 변환
    /// </summary>
    /// <param name="pixels">전처리된 이미지 데이터</param>
    private Tensor TransformInput(byte[] pixels)
    {
        // 픽셀 데이터를 [-1, 1] 범위로 정규화
        float[] transformedPixels = new float[pixels.Length];

        for(int i = 0; i < pixels.Length; i++)
        {
            transformedPixels[i] = (pixels[1] - 127f) / 128f; // 정규화 공식
        }

        // 정규화된 데이터를 기반으로 Tensor 생성
        return new Tensor(1, IMAGE_SIZE, IMAGE_SIZE, 3, transformedPixels);
    }
}
