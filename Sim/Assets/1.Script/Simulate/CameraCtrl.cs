using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraCtrl : MonoBehaviour {
    public static CameraCtrl instance;
    public Transform body;
    public Camera cam;
    public Text debugText;
    Vector3 startPos;
    float xDist, yDist;
    public float rotateSpeed = 5f, zoomCap = 10f, zoomSpeed = 20f, zoomMax = 30f, zoomMin = 0.2f;
    private Vector3 rotation = Vector3.zero;
    float angleLimit = 89, currentAngle = 0, zoomAmount = 0, verticalAngle = 0, orthoSize = 2f;
    bool isDrag = false;

    private void Awake() {
        instance = this;
    }
    private void Start() {
        currentAngle = body.localEulerAngles.x;
        cam.orthographicSize = orthoSize;
        ConstMgr.ZoomRate = cam.orthographicSize / zoomMax;
    }
    void Update() {
        rotation = Vector3.zero;
        verticalAngle = 0;
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
            startPos = Input.mousePosition;
            xDist = 0;
            yDist = 0;
            isDrag = true;
        }
        if (Input.GetMouseButton(0) && isDrag) {
            verticalAngle = Input.mousePosition.y - startPos.y - yDist;
            rotation = new Vector3(0, Input.mousePosition.x - startPos.x - xDist, 0);
            xDist = Input.mousePosition.x - startPos.x;
            yDist = Input.mousePosition.y - startPos.y;
        }
        if (Input.GetMouseButtonUp(0))
            isDrag = false;
        //확대
        zoomAmount = 0;
        if (!SearchMgr.instance.isOpen) {
            zoomAmount = zoomSpeed * Input.GetAxisRaw("Mouse ScrollWheel") * (2f * ConstMgr.ZoomRate);
        }
        orthoSize -= zoomAmount;
        if (orthoSize > zoomMax)
            orthoSize = zoomMax;
        else if (orthoSize < zoomMin)
            orthoSize = zoomMin;
    }
    void FixedUpdate(){ //Movement Rotation
        PreformRotation();
        body.localPosition = Vector3.Lerp(body.localPosition, Vector3.zero, 5f * Time.fixedDeltaTime);
    }
    void PreformRotation(){ //X, Y회전
        //회전
        currentAngle -= verticalAngle * rotateSpeed * Time.deltaTime;
        currentAngle = Mathf.Clamp(currentAngle, -angleLimit, angleLimit);
        body.localEulerAngles = Vector3.Lerp(body.localEulerAngles, body.localEulerAngles + rotation, rotateSpeed * Time.deltaTime);
        if(!AstroMgr.instance.currentMode.Equals(ViewMode.ellipse_plane))
            body.localRotation = Quaternion.Euler(currentAngle, body.localEulerAngles.y, 0);
        else
            body.localRotation = Quaternion.Euler(Vector3.zero);
        //줌
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthoSize, zoomCap * Time.deltaTime);
        ConstMgr.ZoomRate = cam.orthographicSize / zoomMax;
    }
    public void SetZoomSize(float amount, bool isInstantlyhMove) {
        orthoSize = amount;
        if (isInstantlyhMove) {
            cam.orthographicSize = orthoSize;
            ConstMgr.ZoomRate = cam.orthographicSize / zoomMax;
            if (AstroMgr.instance.currentMode.Equals(ViewMode.ellipse_plane)) {
                body.localPosition = Vector3.zero;
                body.localRotation = Quaternion.Euler(Vector3.zero);
            }
        }
    }
}
