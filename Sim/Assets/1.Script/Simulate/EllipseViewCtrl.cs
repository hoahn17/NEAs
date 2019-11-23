using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EllipseViewCtrl : MonoBehaviour {
    //고정 변수
    public Transform[] models, images, zeroPoints;
    public LineRenderer[] lines;
    float draw_interval = 0;
    LineRenderer orbitalLine;
    public Image[] DegImages;
    public Text[] info_Texts;
    //카메라 위치
    public Transform CamPosition;
    //표시 천체 정보
    AstroCtrl focused;
    public OrbitalElements element;
    Vector3 p, r, v;
    Vector3[] RM = new Vector3[3];
    float EA, M, f, b;

    private void Start() {
        for (int i = 0; i < lines.Length; i++) {
            lines[i].positionCount = 2;
        }
        orbitalLine = GetComponent<LineRenderer>();
        orbitalLine.positionCount = ConstMgr.INTERVAL_COUNT;
        draw_interval = 360f / ConstMgr.INTERVAL_COUNT;
        gameObject.SetActive(false);
    }
    public void InitInfo(AstroCtrl astro) {
        focused = astro;
        element = astro.el;
        CalcRM();
        //위치 이동
        SetPosition();
        element.e = 0;
        CalcRM();
        float zoomAmount = Mathf.Clamp(1.75f * element.a, CameraCtrl.instance.zoomMin, CameraCtrl.instance.zoomMax);
        CameraCtrl.instance.SetZoomSize(zoomAmount, AstroMgr.instance.currentMode.Equals(ViewMode.ellipse_plane));
        //궤도 그리기
        zeroPoints[1].position = Vector3.zero;
        for(int i=0;i < DegImages.Length; i++) {
            DegImages[i].transform.localScale = element.a * 0.5f * Vector2.one;
        }
        DegImages[2].transform.position = Vector3.zero;
        b = element.a * Mathf.Sqrt(1 - focused.el.e * focused.el.e);
        info_Texts[0].text = focused.el.e.ToString();
        info_Texts[1].text = focused.el.a.ToString();
        info_Texts[2].text = b.ToString();
        info_Texts[3].text = focused.p.magnitude.ToString();
        info_Texts[4].text = focused.el.peri_dist.ToString();
        info_Texts[5].text = focused.el.aphel_dist.ToString();
        OrbitalDraw();
        MeanAnomalyDraw();
        EccentricAnomalyDraw();
        DrawRefresh();
    }
    void SetPosition() {
        //PerihelionDirection;
        transform.position = -element.a * element.e * (Mathf.Cos(0) * RM[0] + Mathf.Sin(0) * RM[1]);
        Vector3 vec1 = p.magnitude * Mathf.Cos(0) * RM[0] + Mathf.Sin(0) * RM[1];
        Vector3 vec2 = p.magnitude * Mathf.Cos(0.5f * Mathf.PI) * RM[0] + Mathf.Sin(0.5f * Mathf.PI) * RM[1];
        Vector3 vec = Vector3.Cross(vec2, vec1);
        CamPosition.rotation = Quaternion.LookRotation(-vec);
        CamPosition.Rotate(Vector3.forward, element.ω + (element.i < 0.0001f ? element.Ω : (element.i > 179.999f ? -element.Ω : 0)));
        //CamPosition.localEulerAngles = new Vector3(CamPosition.localEulerAngles.x,0,0);
        CameraCtrl.instance.body.SetParent(CamPosition);
    }
    void CalcRM() {
        //단위 변환(디그리 -> 라디안)
        float Ω_rd = element.Ω * Mathf.Deg2Rad;
        float ω_rd = element.ω * Mathf.Deg2Rad;
        float i_rd = element.i * Mathf.Deg2Rad;
        //RM계산_시뮬레이터상 y와 z를 바꾸어서 계산
        RM[0].x = Mathf.Cos(Ω_rd) * Mathf.Cos(ω_rd) - Mathf.Sin(Ω_rd) * Mathf.Sin(ω_rd) * Mathf.Cos(i_rd);
        RM[0].z = Mathf.Sin(Ω_rd) * Mathf.Cos(ω_rd) + Mathf.Cos(Ω_rd) * Mathf.Sin(ω_rd) * Mathf.Cos(i_rd);
        RM[0].y = Mathf.Sin(ω_rd) * Mathf.Sin(i_rd);
        RM[1].x = -Mathf.Cos(Ω_rd) * Mathf.Sin(ω_rd) - Mathf.Sin(Ω_rd) * Mathf.Cos(ω_rd) * Mathf.Cos(i_rd);
        RM[1].z = -Mathf.Sin(Ω_rd) * Mathf.Sin(ω_rd) + Mathf.Cos(Ω_rd) * Mathf.Cos(ω_rd) * Mathf.Cos(i_rd);
        RM[1].y = Mathf.Cos(ω_rd) * Mathf.Sin(i_rd);
        RM[2].x = Mathf.Sin(Ω_rd) * Mathf.Sin(i_rd);
        RM[2].z = -Mathf.Cos(Ω_rd) * Mathf.Sin(i_rd);
        RM[2].y = Mathf.Cos(i_rd);
        p = element.a * (1 - element.e * element.e) * RM[1];
    }
    void OrbitalDraw() {
        int index = 0;
        float r_mag;
        float f, interval = 0;
        Vector3 r;
        for (int i = 0; i < ConstMgr.INTERVAL_COUNT; i++) {
            interval += draw_interval;
            EA = EccAnom(interval, element);
            f = EA * Mathf.Deg2Rad;
            r_mag = (p / (1 + element.e * Mathf.Cos(f))).magnitude;
            r = r_mag * Mathf.Cos(f) * RM[0] + r_mag * Mathf.Sin(f) * RM[1];
            orbitalLine.SetPosition(index++, r);
        }
    }
    float EccAnom(float M, OrbitalElements element) {
        float pi = Mathf.PI, K = Mathf.Deg2Rad;
        float maxIter = 30, i = 0;
        float dp = 5;
        float delta = Mathf.Pow(10, -dp);
        float E, F;
        M *= 0.00277777777f;//1/360
        M = 2.0f * pi * (M - Mathf.Floor(M));
        if (element.e < 0.8)
            E = M;
        else
            E = pi;
        F = E - element.e * Mathf.Sin(M) - M;
        while ((Mathf.Abs(F) > delta) && (i < maxIter)) {
            E -= F / (1.0f - element.e * Mathf.Cos(E));
            F = E - element.e * Mathf.Sin(E) - M;
            i++;
        }
        E /= K;
        return Mathf.Round(E * Mathf.Pow(10, dp)) * Mathf.Pow(10, -dp);
    }
    private void FixedUpdate() {
        MeanAnomalyDraw();
        EccentricAnomalyDraw();
        DrawRefresh();
        TruAnomalyDraw();
        InfoRefresh();
    }
    void InfoRefresh() {
    }
    void DrawRefresh() {
        float scale = ConstMgr.LineScale * ConstMgr.ZoomRate;
        orbitalLine.widthMultiplier = ConstMgr.LineScale * ConstMgr.ZoomRate;
        for (int i = 0; i < images.Length; i++) {
            Vector3 vec = CameraCtrl.instance.cam.transform.position - models[i].position;
            images[i].rotation = Quaternion.LookRotation(vec);
            images[i].localScale = 2f * scale * Vector3.one;
        }
        for (int i = 0; i < zeroPoints.Length; i++) {
            Vector3 vec = CameraCtrl.instance.cam.transform.position - zeroPoints[i].position;
            zeroPoints[i].rotation = Quaternion.LookRotation(vec);
            zeroPoints[i].localScale = 2f * scale * Vector3.one;
        }
        for (int i = 0; i < lines.Length; i++) {
            lines[i].widthMultiplier = ConstMgr.LineScale * ConstMgr.ZoomRate;
            if (i >= 4)
                lines[i].widthMultiplier *= 1.5f;
        }
        lines[0].SetPosition(1, models[0].localPosition);
        lines[1].SetPosition(1, models[1].localPosition);
        lines[2].SetPosition(1, focused.model.position);
        lines[3].SetPosition(0, focused.model.position);
        lines[3].SetPosition(1, models[1].position);
        lines[4].SetPosition(1, -b * (Mathf.Cos(90f * Mathf.Deg2Rad) * RM[0] + Mathf.Sin(90f * Mathf.Deg2Rad) * RM[1]));
        lines[5].SetPosition(1, focused.p.magnitude * (Mathf.Cos(90f * Mathf.Deg2Rad) * RM[0] + Mathf.Sin(90f * Mathf.Deg2Rad) * RM[1]));
        lines[6].SetPosition(0, -transform.position);
        lines[6].SetPosition(1, element.a * (Mathf.Cos(Mathf.PI) * RM[0] + Mathf.Sin(Mathf.PI) * RM[1]));
        lines[7].SetPosition(0, -transform.position);
        lines[7].SetPosition(1, element.a * (Mathf.Cos(0f) * RM[0] + Mathf.Sin(0f) * RM[1]));
        //임시1
    }
    void MeanAnomalyDraw() {
        M = element.M0 + element.mdm * CalendarCtrl.instance.GetDayGap(element.epoch);
        M %= 360f;
        f = M * Mathf.Deg2Rad;
        DegImages[0].fillAmount = M * 0.00277777f;
        info_Texts[6].text = string.Format("{0}", M.ToString("N4"));
        r = element.a * Mathf.Cos(f) * RM[0] + element.a * Mathf.Sin(f) * RM[1];
        models[0].localPosition = r;
    }
    void EccentricAnomalyDraw() {
        Vector3[] RM = focused.RM;
        M = element.M0 + element.mdm * CalendarCtrl.instance.GetDayGap(element.epoch);
        M %= 360f;
        f = EccAnom(M, focused.el);
        DegImages[1].fillAmount = f * 0.00277777f;
        info_Texts[7].text = string.Format("{0}", f.ToString("N4"));
        f *= Mathf.Deg2Rad;
        r = element.a * (Mathf.Cos(f) * RM[0] + Mathf.Sin(f) * RM[1]);
        models[1].localPosition = r;
    }
    void TruAnomalyDraw() {
        DegImages[2].fillAmount = focused.f * 0.00277777f * Mathf.Rad2Deg;
        info_Texts[8].text = string.Format("{0}", (focused.f * Mathf.Rad2Deg).ToString("N4"));
    }
}
