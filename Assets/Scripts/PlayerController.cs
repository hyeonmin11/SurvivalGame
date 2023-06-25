using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]  // 외부 스크립트에서는 수정을 못하지만 인스펙터에서는 접근 가능하게
    private float walkSpeed;

    [SerializeField]
    private float lookSensitivity; 

    [SerializeField]
    private float cameraRotationLimit;  
    private float currentCameraRotationX;  

    [SerializeField]
    private Camera theCamera; 
    private Rigidbody myRigid;

    void Start() 
    {
        myRigid = GetComponent<Rigidbody>();  // private
    }

    void Update()  // 컴퓨터마다 다르지만 대략 1초에 60번 실행
    {
        Move();                 // 1️⃣ 키보드 입력에 따라 이동
        CameraRotation();       // 2️⃣ 마우스를 위아래(Y) 움직임에 따라 카메라 X 축 회전 
        CharacterRotation();    // 3️⃣ 마우스 좌우(X) 움직임에 따라 캐릭터 Y 축 회전 
    }

    private void Move()
    {
        //Input.GetAxisRaw(): -1, 0, 1 세가지 값 중 하나가 즉시 반응해서 반환 ,,, Input.GetAxis() : -1.0f ~1.0f 사이의 값 반환
        float _moveDirX = Input.GetAxisRaw("Horizontal");  
        float _moveDirZ = Input.GetAxisRaw("Vertical");  
        Vector3 _moveHorizontal = transform.right * _moveDirX; 
        Vector3 _moveVertical = transform.forward * _moveDirZ; 

        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * walkSpeed; // 방향벡터 * 속도

        myRigid.MovePosition(transform.position + _velocity * Time.deltaTime);
    }

    private void CameraRotation(){
        float _xRotation = Input.GetAxis("Mouse Y");
        float _cameraRotationX = _xRotation * lookSensitivity;

        currentCameraRotationX -= _cameraRotationX;
        //최소 최대 값을 설정하여 float 값이 범위 이외의 값을 넘지 않도록 합니다.
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);
        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
    }

    private void CharacterRotation() //좌우 캐릭터 회전
    {
        float _yRotation = Input.GetAxisRaw("Mouse X");
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;
        myRigid.MoveRotation(myRigid.rotation * Quaternion.Euler(_characterRotationY)); // 오일러각 쿼터니언으로 변경시켜 사용한다.

    }
}

/*
캐릭터의 크기는 캡슐의 크기로 어림 잡으면 편하다.
Main Camera 오브젝트를 A라는 이름의 오브젝트의 자식으로 넣으면 Main Camera는 A의 1인칭 카메라가 된다. 즉 A의 시점으로 화면을 비춘다. 카메라의 위치가 A의 시점이 되도록 Transform값만
조정해주면 A를 따라다니며 A의 시점을 촬용하는 1인칭 카메라가 된다.


유니티에서는 X축 이동 -> 좌우, Z축 이동 -> 앞뒤, Y축 이동 -> 위아래
myRigid : Rigidbody컴포넌트를 할당할 변수로 private하기 때문에 게임 시작시 GetComponent 함수를 통해 할당해준다.
_moveDirX : 좌우 이동 크기  A: -1 D : 1 (스칼라)
_moveDirZ : 앞뒤 이동 크기 S : -1 W : 1

_moveHorizontal : 좌우 이동 벡터값
방향 * 크기 = transform.right * _moveDirX (transform.right은 오브젝트의 오른쪽 방향을 나타내는 방향단위벡터)

_moveVertical : 앞뒤 이동 벡터값

(현재의 위치 + 속도 X 델타타임)
Move() 함수는 업데이트함수 내부에서 실행될 것이기 때문에 1초동안 _velocity이 더해질 수 있도록 1프레임 당 1/60 * _velocity만큼 더해주어야 한다.
Time.deltaTime이 1/60이 된다.
*/

/*
해당 축을 고정한 체 해당 축의 입장에서 시계 방향으로 회전한다. 따라서 X축 회전은 고개를 숙이는게 Positive 방향 회전이다.
X축 회전: X축을 중심으로 회전한다는 의미. X축 회전값은 변함 없다.
X축은 평면가로축(좌우)
따라서 마우스를 위아래로 움직이면 고개를숙였다 올렸다 하는 1인칭 시점을 구현해야 하므로 오브젝트의 머리에 달려있는 1인칭 카메라인 MainCamera를 X축 회전해주어야 한ㄷ아.
오브젝트는 가만히 있는 상태에서 카메라만 X축으로 회전시키는 것이다.


Y축 회전: 
Y축은 수직축
마우스를 양옆으로 움직이면 고개를 좌우로 돌리는 1인칭 시점을 구현해야 할 때 y축 회전시킨다.
카메라는 가만히 있고 오브젝트는 Y축으롤 회전. 몸을 마우스가 향하는 방향으로 돌린다.

Z축 회전:
Z축은 평면 수직축
Z축은 양 옆으로 굴러가는 회전을 하는 것과 같다.
*/

/*
theCamera는 메인 카메라가 할당된다. 컴포넌트가 아니라서 GetComponent로 가져올 수 없고 private이지만 유니티 에디터에서 할당해줄 수 있도록
SieralizeFiled 속성을 붙여주었다. Hierarchy 창에서 해당하는 오브젝트를 찾아주는 FindObjectOfType함수를 사용할 수도 있지만 오브젝트가 굉장히 많다면
너무 느리고 카메라도 여러개를 둘 수 있기 때문에 이 방법은 비추한다.

마우스 커서는 2d다. x y 축 이동 밖에 없다.
Input.GetAxisRaw("Mouse Y")역시 -1 0 1 만 나타낸다.
마우스가 아래로 움직이면 -1 위로 움직이면 1 , 안 움직이면 0

카메라가 X축으로 회전할 만큼의 양 : _cameraRotationX
 : 마우스가 Y 방향으로 움직인 정도 * 민감도

카메라 x축 방향 회전 값 currentCameraRotationX
 : _cameraRotationX 만큼 뺴준다.
 x축 방향으로 회전할 때 아래로 숙이는게 Positive 방향이고 정면을 보는게 0이고 위로 젖히는게 Negative 방향이다.
 마우스가 아래로 움직이면 _cameraRotationX값이 Input.GetAxisRaw에 의해 음수가 되기 때문에 
 _cameraRoationX만큼을 더해주게 되면 currentCameraRotationX값이 감소하여 카메라가 위로 회전하게 된다.
 따라서 += 로 해주면 오히려 마우스를 아래로 내릴 수록 위를 쳐다보게 되기 때문에
 마우스가 아래로 움직이면 카메라의 x축회전값이 커져 카메라가 아래로 숙이게끔 -= QOwndjTek.

 Mathf.Clamp를 사용하여 최대최소 회전범위 설정했다. ( 마우스를 위아래로 움직일 때 360도 회전해버리면 안되고 고개를 젖힐 때나 숙일때나 어느정도 제한을 두어야해서)

 두 쿼터니언의 회전량을 더하려면 두 쿼터니언 값을 곱해야한다.

 Rigidbody의 경우 캡슐오브젝트가 밑이 동그래서 자꾸 양옆 앞뒤로 넘어지므로 X Z회전은 안되게끔 막아두었다.
*/

/*

 키보드입력에 따른 이동
 마우스 Y회전에 따른 카메라의 X축 회전
 마우스 X회전에 따른 캡슐의 Y축 회전
*/