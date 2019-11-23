using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstroCtrl : MonoBehaviour {
    //고정 변수
    public Transform model, image;
    LineRenderer line;
    public OrbitalElements el;
    float draw_interval = 0;
    //궤도 요소
    public Vector3 p;//근일점 벡터와 수직되면서 천체의 궤도와 접하는 벡터
    public float f;//진근점 이각(true anomaly): 근일점 벡터와 r벡터 사잇각. (단위: 디그리)
    public float  s_dist;//천체의 공전궤도 주기
    float M;//평균 근점 이각(mean anomly)
    float EA;//편심 이각(eccentric anomaly)
    public Vector3[] RM = new Vector3[3];//좌표 변환 행렬: 천체 궤도 벡터를 태양 중심 벡터로 변환하기 위한 행렬
    Vector3 r;//태양으로부터 천체까지의 거리(단위: AU)
    //유동 변수
    public bool isSolar = false, isVisible = false;

    private void Start() {
        line = GetComponent<LineRenderer>();
        line.positionCount = ConstMgr.INTERVAL_COUNT;
        draw_interval = 360f / ConstMgr.INTERVAL_COUNT;
        //태양계 정보 갱신
        if (transform.parent.name.Equals("SolarSystem")) {
            isSolar = true;
            Init_Solar();
            CalcRM();
            OrbitalDraw();
            AstroMove();
        }
        else
            isSolar = false;
    }
    public void FixedUpdate() {
        //디버깅용
        //CalcRM();
        //OrbitalDraw();
        //본문
        AstroMove();
        DrawRefresh();
    }
    public void VisibleCtrl(bool isVisible) {
        this.isVisible = isVisible;
        image.GetComponent<SpriteRenderer>().enabled = isVisible;
        line.enabled = isVisible;
    }
    void Init_Solar() {
        int index = transform.GetSiblingIndex();
        //a(장반경)    e(이심률)  i(inclination) M0       Ω ω
        el.epoch = ConstMgr.J2000;
        el.a = ConstMgr.SOLAR_INFO[index][0];
        el.e = ConstMgr.SOLAR_INFO[index][1];
        el.i = ConstMgr.SOLAR_INFO[index][2];
        el.M0 = ConstMgr.SOLAR_INFO[index][3];
        el.Ω = ConstMgr.SOLAR_INFO[index][4];
        el.ω = ConstMgr.SOLAR_INFO[index][5];
        el.mdm = ConstMgr.SOLAR_INFO[index][6];
        el.name = ConstMgr.SOLOR_NAME[index];
        el.peri_dist = el.a * (1 - el.e);
        el.aphel_dist= el.a * (1 + el.e);
        line.colorGradient = AstroMgr.instance.colors[index];
        image.GetComponent<SpriteRenderer>().color = AstroMgr.instance.colors[index].colorKeys[0].color;
    }
    public void Init_Astro(OrbitalElements elements) {
        line = GetComponent<LineRenderer>();
        line.positionCount = ConstMgr.INTERVAL_COUNT;
        draw_interval = 360f / ConstMgr.INTERVAL_COUNT;
        el = elements;
        float scale = ConstMgr.LineScale * ConstMgr.ZoomRate;
        image.localScale = 1f * scale * Vector3.one;
        line.widthMultiplier = 1f * scale;
        CalcRM();
        OrbitalDraw();
        AstroMove();
    }
    void AstroMove() {
        M = el.M0 + el.mdm * CalendarCtrl.instance.GetDayGap(el.epoch);
        if (M >= 360)
            M -= 360;
        else if (M <= 0)
            M += 360;
        EA = EccAnom(M);
        f = TrueAnom();
        float r_mag = (p / (1 + el.e * Mathf.Cos(f))).magnitude;
        r = r_mag * (Mathf.Cos(f) * RM[0] + Mathf.Sin(f) * RM[1]);
        s_dist = r_mag;
        model.position = r;
    }
    public void DrawRefresh() {
        bool isFocus = Equals(AstroMgr.instance.Focused);
        float scale = ConstMgr.LineScale * ConstMgr.ZoomRate;
        //이미지 갱신
        Vector3 vec = CameraCtrl.instance.cam.transform.position - model.position;
        image.rotation = Quaternion.LookRotation(vec);
        image.localScale = (isFocus ? 2f : 1.5f) * scale * Vector3.one;
        line.widthMultiplier = (isFocus ? 2f : 1f) * scale;
        if (!isSolar) {
            line.colorGradient = AstroMgr.instance.astro_Colors[!isFocus ? 0 : 1];
            image.GetComponent<SpriteRenderer>().color = AstroMgr.instance.astro_Colors[!isFocus ? 0 : 1].colorKeys[0].color;
        }
        //if (!isSolar)
        //    for (int i = 0; i < line.positionCount; i++)
        //        Debug.DrawLine(Vector3.zero, line.GetPosition(i));
    }
    float EccAnom(float M) {
        float pi = Mathf.PI, K = Mathf.Deg2Rad;
        float maxIter = 30, i = 0;
        float dp = 5;
        float delta = Mathf.Pow(10, -dp);
        float E, F;
        M *= 0.00277777777f;//1/360
        M = 2.0f * pi * (M - Mathf.Floor(M));
        if (el.e < 0.8)
            E = M;
        else
            E = pi;
        F = E - el.e * Mathf.Sin(M) - M;
        while ((Mathf.Abs(F) > delta) && (i < maxIter)) {
            E -= F / (1.0f - el.e * Mathf.Cos(E));
            F = E - el.e * Mathf.Sin(E) - M;
            i++;
        }
        E /= K;
        return Mathf.Round(E * Mathf.Pow(10, dp)) * Mathf.Pow(10, -dp);
    }
    float TrueAnom() {
        float EA = this.EA * Mathf.Deg2Rad;
        return 2f * Mathf.Atan2(Mathf.Sqrt(1 + el.e) * Mathf.Sin(EA * 0.5f), Mathf.Sqrt(1 - el.e) * Mathf.Cos(EA * 0.5f));
    }
    void CalcRM() {
        //단위 변환(디그리 -> 라디안)
        float Ω_rd = el.Ω * Mathf.Deg2Rad;
        float ω_rd = el.ω * Mathf.Deg2Rad;
        float i_rd = el.i * Mathf.Deg2Rad;
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
        p = el.a * (1 - el.e * el.e) * RM[1];
    }
    void OrbitalDraw() {
        int index = 0;
        float r_mag;
        float f, interval = 0;
        Vector3 r;
        for (int i = 0; i < ConstMgr.INTERVAL_COUNT; i++) {
            interval += draw_interval;
            EA = EccAnom(interval);
            f = EA * Mathf.Deg2Rad;
            r_mag = (p / (1 + el.e * Mathf.Cos(f))).magnitude;
            r = r_mag * Mathf.Cos(f) * RM[0] + r_mag * Mathf.Sin(f) * RM[1];
            line.SetPosition(index++, r);
        }
    }
}
