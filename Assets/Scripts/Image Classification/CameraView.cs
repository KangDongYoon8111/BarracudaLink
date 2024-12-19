using UnityEngine;
using UnityEngine.UI;

/* 
 * 이 스크립트는 Unity의 WebCamTexture를 사용하여 카메라를 활성화하고,
 * RawImage UI 컴포넌트에 카메라의 실시간 뷰를 표시하는 역할을 합니다.
 * AspectRatioFitter를 활용해 카메라 화면의 비율을 유지합니다.
 */
public class CameraView : MonoBehaviour
{
    public RawImage rawImage; // RawImage 컴포넌트, 카메라 영상을 출력할 UI 요소
    public AspectRatioFitter fitter; // 화면의 가로 세로 비율을 맞추기 위한 컴포넌트

    private WebCamTexture webCamTexture; // 카메라의 실시간 영상을 가져오는 WebCamTexture
    private bool ratioSet; // 비율이 설정되었는지 여부를 확인하는 플래그

    private void Start()
    {
        // 웹캠 초기화 메서드 호출
        InitWebCam();
    }

    private void Update()
    {
        // 웹캠의 해상도가 유효하고 비율이 아직 설정되지 않았다면
        if (webCamTexture.width > 100 && !ratioSet)
        {
            ratioSet = true; // 비율 설정 완료 플래그 활성화
            SetAspectRatio(); // 화면 비율 설정
        }
    }

    // 웹캠 초기화
    private void InitWebCam()
    {
        // 사용할 카메라의 이름을 가져옴 (기본적으로 첫 번째 카메라 선택)
        string camName = WebCamTexture.devices[0].name;
        // WebCamTexture를 생성하며, 화면 해상도와 프레임 레이트 설정 
        webCamTexture = new WebCamTexture(camName, Screen.width, Screen.height, 30);
        // RawImage의 텍스처로 WebCamTexture를 설정
        rawImage.texture = webCamTexture;
        // 웹캠 영상 재생 시작
        webCamTexture.Play();
    }

    // 화면의 가로 세로 비율 설정
    private void SetAspectRatio()
    {
        // WebCamTexture의 가로와 세로 비율을 계산하여 AspectRatioFitter에 전달
        fitter.aspectRatio = (float)webCamTexture.width / (float)webCamTexture.height;
    }

    /// <summary>
    /// 실시간 카메라 연결 메서드
    /// </summary>
    /// <returns>연동된 WebCamTexture</returns>
    public WebCamTexture GetCamImage()
    {
        return webCamTexture;
    }
}
