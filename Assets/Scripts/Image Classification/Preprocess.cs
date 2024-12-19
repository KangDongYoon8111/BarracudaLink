using UnityEngine; // Unity 엔진 기본 네임스페이스
using UnityEngine.Events; // Unity 이벤트 시스템을 사용하기 위한 네임스페이스
using UnityEngine.Rendering; // GPU 관련 렌더링 작업을 위한 네임스페이스

// AI 모델의 입력 데이터를 효율적으로 처리하기 위한 전처리 클래스로, GPU를 활용해 고속으로 이미지를 변환
// 실시간 카메라 데이터를 입력받아 AI 모델이 요구하는 형식으로 변환하는 역할을 수행
public class Preprocess : MonoBehaviour
{
    // RenderTexture : GPU에서 렌더링 작업을 처리하기 위한 텍스처
    private RenderTexture renderTexture;

    // 이미지의 스케일(가료, 세로 비율)과 크롭(잘라내기) 오프셋을 위한 벡터
    private Vector2 scale = new Vector2(1, 1); // 기본 스케일은 1:1
    private Vector2 offset = Vector2.zero; // 기본 오프셋은 0(변경 없음)

    // 이미지 데이터를 처리할 때 호출될 콜백 함수 (데이터 타입: byte 배열)
    UnityAction<byte[]> callback;

    /// <summary>
    /// WebCamTexture를 스케일 조정하고 크롭한 뒤 GPU에서 데이터를 읽는 메서드
    /// </summary>
    /// <param name="webCamTexture">실시간 카메라 텍스처</param>
    /// <param name="desiredSize">목표 크기(픽셀 단위)</param>
    /// <param name="callback">전처리 완료 후 호출할 콜백 함수</param>
    public void ScaleAndCropImage(WebCamTexture webCamTexture, int desiredSize, UnityAction<byte[]> callback)
    {
        this.callback = callback; // 콜백 함수를 클래스 변수로 저장

        // RenderTexture가 생성되지 않았다면 초기화
        if(renderTexture == null)
        {
            // 지정된 크기와 포맷으로 RenderTexture 생성
            renderTexture = new RenderTexture(desiredSize, desiredSize, 0, RenderTextureFormat.ARGB32);
        }

        // 스케일 비율 계산 : 카메라 텍스처의 높이와 너비를 기준으로 계산
        scale.x = (float)webCamTexture.height / (float)webCamTexture.width;
        offset.x = (1 - scale.x) / 2f; // 스케일 조정 후 남는 여백 계산

        // GPU에서 WebCamTexture 데이터를 RenderTexture로 복사하며, 스케일과 오프셋 적용
        Graphics.Blit(webCamTexture, renderTexture, scale, offset);
        // GPU에서 RenderTexture 데이터를 비동기로 읽기 시작
        AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, OnCompleteReadback);
    }

    /// <summary>
    /// GPU에서 데이터를 읽기 완료했을 때 호출되는 콜백 함수
    /// </summary>
    /// <param name="request">GPU 읽기 요청 객체</param>
    private void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        // GPU 읽기 작업 중 에러가 발생했는지 확인
        if(request.hasError)
        {
            Debug.Log("GPU readback error detected."); // 에러 로그 출력
            return; // 작업 중단
        }

        // GPU에서 읽어온 데이터를 콜백 함수에 전달
        callback.Invoke(request.GetData<byte>().ToArray());
    }
}
