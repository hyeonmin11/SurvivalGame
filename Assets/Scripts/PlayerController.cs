using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]  // 외부 스크립트에서는 수정을 못하지만 인스펙터에서는 접근 가능하게
    private float walkSpeed;

    [SerializeField]
    private float runSpeed;
    [SerializeField]
    private float crouchSpeed;
    private float applySpeed;

    //점프 정도
    [SerializeField]
    private float jumpForce;

    //상태 변수
    private bool isRun = false;
    private bool isGround = false;
    private bool isCrouch = false;

    //앉았을 때 얼마나 앉을지 결정하는 변수
    private float crouchPosY;
    private float originPosY;
    private float applyCrouchPosY;

    /*
    applySpeed에 walkspeed , runspeed, crouchspeed 셋 중 하나가 할당된다.
    applyCrouchPosY에 crouchPosY, originPosY 둘 중 하나가 할당된다.

    이렇게 apply변수를 따로 두는 이유는, 만약에 따로 두지 않는다면 아래와 같이 walkspeed를 사용하는 걷기함수
    runspeed를 사용하는 뛰기함수 crouchspeed를 사용하는 앉아서 걷기 함수 이렇게 3가지 함수를 따로따로 만들어주거나 if, switch문을 두어야한다. 
    비효율적이다.

    그냥 applySpeed 변수 하나 두고 상황에 맞게 walkspeed runspeed crouchspeed을 applyspeed에 할당해주면
    아래의 move()에서 applyspeed 하나로 걸을 때, 달리 때, 앉아서 걸을 때 이렇게 3가지를 다 기능할 수 있다.

    */


    [SerializeField]
    private float lookSensitivity; 

    [SerializeField]
    private float cameraRotationLimit;  
    private float currentCameraRotationX;  

    [SerializeField]
    private Camera theCamera; 
    private Rigidbody myRigid;
    private CapsuleCollider capsuleCollider;


    void Start() 
    {
        //컴포넌트 할당
        myRigid = GetComponent<Rigidbody>();  // private
        capsuleCollider = GetComponent<CapsuleCollider>();

        //초기화
        applySpeed = walkSpeed;  // 실제 변수 와 처음에 초기화할 때 사용할 변수. 전자는 private 후자는 serialized
        originPosY = theCamera.transform.localPosition.y;
        applyCrouchPosY = originPosY;
    }

    void Update()  // 컴퓨터마다 다르지만 대략 1초에 60번 실행
    {
        IsGround();
        TryJump();
        TryRun();
        TryCrouch();
        Move();                 // 1️⃣ 키보드 입력에 따라 이동
        CameraRotation();       // 2️⃣ 마우스를 위아래(Y) 움직임에 따라 카메라 X 축 회전 
        CharacterRotation();    // 3️⃣ 마우스 좌우(X) 움직임에 따라 캐릭터 Y 축 회전 
    }

/*
점프 땅 착지 검사
-> 땅에 착지해 있을 때만 점프가 가능하게 하는게 포인트
TryJump()함수 : 점프가 가능한 상태인지를 검사한다.
스페이스 바를 누르고 있는 상태(GetKeyDown) and 땅에 착지해있는 상태(isGround가 True)일 때만 Jump()함수를 호출한다.
isGround의 기본값은 착지 상태인 True다

isGround()함수: 오브젝트가 땅에 착지한 상태인지를 검사하여 isGround값을 업데이트한다.
현재 위치로부터 절대적인 월드좌표계 기준에서의 아래방향으로(플레이어 오브젝트의 중심적부터 발끝까지의 수직길이 + 0.1f)길이의
광선을 쐈을 때 충돌이 감지되는 것이 있다면 땅에 착지했다는 뜻이다.

Vector3.down == (0,-1,0) , 절대적인 월드좌표계기준에서의 아래방향이다.

capsuleCollider.bounds.extents.y + 0.1f
캡슐 콜라이더를 바운딩 박스 모양으로 나타낸 것에 대한 정보를 담는 bounds 구조체. 
extents는 이 바운딩 박스의 절반에 해당하는 크기를 나타내는 벡터이다.
캡슐 콜라이더는 현재 이 스크립트가 붙어임ㅆ는 플레이오브젝트에 붙어있으므로 즉, capsuleCollider.bounds.extents.y은 플레이 오브젝트의 수직길이(의 절반)이다.
피봇위치, 즉 transform.position는 플레이어 오브젝트의 중심점이므로 transform.position로부터 
capsuleCollider.bounds.extents.y 길이를 더하는 것은 오브젝트의 중심점에서 발끝 지점까지의 길이를 더하는 것과 같다.
이 길이만큼의 광선을 절대적인 아래방향으로 쐈을 때 닿는게 없다는 ㅇ도브젝트가 땅에 붙이고 있지 않고 공중에 있는 상태인 것이다. 광선의 길이는 몸의 절반이기 때문에

*/


    //지면 체크
    private void IsGround(){
        isGround = Physics.Raycast(transform.position, Vector3.down, capsuleCollider.bounds.extents.y + 0.1f);
    }

/*
jump() 함수 : 실제로 점프하는 기능을 수행한다.
isCrouch가 True면, 즉 앉아있는 상태면 다시 일어선다. Crouch()
점프는 서서하는게 자연스러우므로 카메라를 원래 위치로 올려준다.

실제 물리적인 점프시도
Rigidbody의 velocity속도 벡터값을 (나 자신을 기준으로 한 위쪽 방향 벡터 * 점프 스칼라 크기) 결과인 벡터로 설정해준다
velocity 속도벡터는 물리적 상황에 따라 값이 변화하는데, 이렇게 직접 설정해주면 한순간에 현재 위치에서 
transform.up * jumpForce 만큼 이동하게 된다. 한 순간에 이만큼 이동해버리니 속도개념임.
한 순간에 점프를 하고 난 후에는 Rigidbody의 물리 기능에 따라 중력의 영향을 받으면서 서서히 떨어지게 된다. 
velocity 벡터값도 중력가속도 영향을 받아 원래 물리적 현상 대로 바뀌게 된다.
점프를 구현할 때는 Rigidbody의 velocity값을 변경해주는 것이 좋다. 점프 특성상 한 순간에 점프가 되어야하며 
점프가 된 후에는 중력의 영향을 받아 자연스레 떨어져야 하기 때문이다.
*/


    //점프 시도
    private void TryJump(){
        if(Input.GetKeyDown(KeyCode.Space) && isGround){
            Jump();
        }
    }

    private void Jump(){
        if(isCrouch){
            Crouch();
        }
        myRigid.velocity = transform.up * jumpForce;
    }

/*
달리는 입력키인 leftshift 입력을 받는다
leftshift키를 누르는 동안에는 running() 함수를 실행한다.
--------- -------- ---------- ------------ ------------
running() -> move()에서 달릴 수 있도록 변수 업데이트한다.
isCrouch가 True면, 즉 앉아있는 상태면 다시 일어난다. Crouch()
isRun을 True로 설정하고 applySpeed를 runspeed로 설정한다.
이 과정덕분에 다음 프레임에서 runspeed로 move()를 실행한다.
------------------------- --------------- ----------------
leftshift키를 떼는 순간에 runningcancel()함수 실행한다.
isRun을 false로 설정
applyspeed를 walkspeed로 설정

이처럼 applySpeed를 업데이트하는 과정때문에 tryrun()이 move()보다 먼저 실행되어야한다.
*/

    private void TryRun(){
        if(Input.GetKey(KeyCode.LeftShift)){
            Running();
        }
        if(Input.GetKeyUp(KeyCode.LeftShift)){
            RunningCancel();
        }
    }

    private void Running(){
        if(isCrouch){
            Crouch();
        }
        isRun = true;
        applySpeed = runSpeed;
    }

    private void RunningCancel(){
        isRun = false;
        applySpeed = walkSpeed;
    }

/*
1인칭 카메라이므로 앉기를 구현할 땐 카메라의 y방향 위치만 내려주면 된다. 실제로 앉은건 아닌데
카메라만 내려서 앉는 듯한 효과를 줌

TryCrouch()함수 : 앉기 키인 leftcontrol 키 입력이 들어오면 Crouch() 함수를 실행한다.
Crouch() 함수 : 앉기에 필요한 변수 값들을 업데이트한다.
Crouch() 함수를 실행하면 isCouch값이 반전된다.
앉아있는 상태에서 이 함수를 실행하면 서게하고 서있는 상태에서 이 함수를 실행하면 다시 서게할 것이다.

앉으려면 이동속도는 crouchSpeed로 카메라위치는 crouchPosY로
서있으려면 이동속도는 walkSpeed로 카메라위치는 originPosY로

applyCrouchPosY : 카메라의 있어야 할 위치가 된다.
crouchPosY : 앉았을 때 위치할 카메라의 y 방향 위치
유니티에디터에서 0으로 초기화하였다. 즉 부모 오브젝트의 중심점 y 위치와 같게.

originPosY : 서있을 때인 원래 카메라의 y바아향 위치
Start() 함수에서 게임 시작시 카메라 위치였던 theCamera.transform.localPosition.y로 초기화해 두었다.

부드럽게 앉거나 서기
순간적으로 앉는건 딱딱 끊기는 듯하고 너무 부자연스럽기 때문에 카메라 위치를 아래 위로 옮길 때 부드럽게 옮겨지도록 하자

코루틴 함수를 사용하여 부드럽게 앉거나 서게 하기
CrouchCoroutine() 함수 : 코루틴 함수로, 카메라의 y방향 위치를 부드럽게 올리거나 내린다.
_posY : 카메라의 현재 Y 방향 위치
카메라의 현재 위치에서 출발하여 applyCrouchPosY와 일치할 때까지 계속해서 선형보간값(Lerp)으로 업데이트 될 것이다.

count : while문 반복횟수 카운트
_posY : applyCrouchPosY 가 될 때까지 반복한다. 보간하는 값이 클수록 빨리 옮겨지고 작을수록 천천히 더 부드럽게 옮겨진다./
count를 두어 15번 반복하면 while문을 빠녀나오게 한 이유
딱 떨어진 값으로 보간이 안되는 경우의 수들이 있다. 따라서 영원히 _posY == applyCrouchPosY가 되지 않고 
무한히 _posY는 applyCrouchPosY에 가까워지려는 연산을 계속해서 무한히 할 것이다.

그래서 그냥 어느정도 반복횟수에 다다르면 while문을 빠져나오게끔 해주는 것
이런경우에는 카메라의 y축 위치를 원래의 목표인 applyCrouchPosY로 덮어준다.
*/




    //앉기 동작
    private void TryCrouch(){
        if(Input.GetKeyDown(KeyCode.LeftControl)){
            Crouch();
        }
    }

    private void Crouch(){
        isCrouch = !isCrouch;
        if(isCrouch){
            applySpeed = crouchSpeed;
            applyCrouchPosY = crouchPosY;
        }
        else{
            applySpeed = walkSpeed;
            applyCrouchPosY = originPosY;
        }

        StartCoroutine(CrouchCoroutine());
    }

    IEnumerator CrouchCoroutine(){
        float _posY = theCamera.transform.localPosition.y;
        int count = 0;

        while(_posY != applyCrouchPosY){
            count ++;
            _posY = Mathf.Lerp(_posY, applyCrouchPosY, 0.2f);
            theCamera.transform.localPosition = new Vector3(0, _posY, 0);

            if(count > 15){
                break;
            }
            yield return null;
        }
        theCamera.transform.localPosition = new Vector3(0, applyCrouchPosY, 0);
    }

/*
runspeed 가 할당된 applySpeed로 move()함수가 실행되도록 하면 된다
move()함수 : 실제로 이동하는 기능을 수행한다.
applySpeed : Start() 함수에서 게임 시작시엔 walkSpeed가 할당되게끔한다.;
*/

    private void Move()
    {
        //Input.GetAxisRaw(): -1, 0, 1 세가지 값 중 하나가 즉시 반응해서 반환 ,,, Input.GetAxis() : -1.0f ~1.0f 사이의 값 반환
        //Input.GetAxisRaw("Horizontal") : 왼쪽 화살표 -1 오른쪽 화살표 1
        //Input.GetAxisRaw("Vertical") : 아래쪽 화살표 -1 위쪽 화살표 1
        
        float _moveDirX = Input.GetAxisRaw("Horizontal");  
        float _moveDirZ = Input.GetAxisRaw("Vertical");  
        Vector3 _moveHorizontal = transform.right * _moveDirX; 
        Vector3 _moveVertical = transform.forward * _moveDirZ; 

        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applySpeed; // 방향벡터 * 속도

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