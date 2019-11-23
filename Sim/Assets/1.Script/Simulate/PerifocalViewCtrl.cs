using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerifocalViewCtrl : MonoBehaviour {
    //고정 변수
    public OrbitalElements el;
    public MeshFilter filter;
    public Transform[] Indices, Indices_Text, Axises_Pos, Axises_Text;
    public Text[] Degrees, infos;
    //public Transform[] Line;
    Mesh mesh;
    LineRenderer orbitalLine;
    public LineRenderer[] lines, Axises;
    public Image[] images;
    float draw_interval = 0, Vel_Min, Vel_Itv;
    Vector3[] dirs = { Vector3.right, Vector3.up, Vector3.forward };
    //유동변수
    Vector3 p;
    Vector3[] RM = new Vector3[3];
    AstroCtrl focused;

    private void Start() {
        mesh = new Mesh();
        mesh.vertices = new Vector3[ConstMgr.INTERVAL_COUNT];
        filter.mesh = mesh;
        orbitalLine = GetComponent<LineRenderer>();
        orbitalLine.positionCount = ConstMgr.INTERVAL_COUNT;
        for (int i = 0; i < lines.Length; i++)
            lines[i].positionCount = 2;
        for (int i = 0; i < Axises.Length; i++)
            Axises[i].positionCount = 2;
        draw_interval = 2 * Mathf.PI / ConstMgr.INTERVAL_COUNT;
        gameObject.SetActive(false);
    }
    public void InitInfo(AstroCtrl astro) {
        el = astro.el;
        focused = astro;
        float zoomAmount = Mathf.Clamp(1.5f * (astro.isSolar ? el.a : el.aphel_dist), CameraCtrl.instance.zoomMin, CameraCtrl.instance.zoomMax);
        CameraCtrl.instance.SetZoomSize(zoomAmount, AstroMgr.instance.currentMode.Equals(ViewMode.perifocal));
        CalcRM(true);
        OrbitalCircleDraw();
        DrawLine();
        //축 갱신은 한 번만
        p = el.a * (1 - el.e * el.e) * RM[1];
        float r_mag = (p / (1 + el.e * Mathf.Cos(el.Ω * Mathf.PI / 180 + el.ω * Mathf.PI / 180))).magnitude;
        for (int i = 0; i < Axises.Length; i++) {
            Axises[i].SetPositions(new Vector3[2] { Vector3.zero, 1.3f * r_mag * dirs[i] });
            Axises_Pos[i].position = 1.3f * r_mag * dirs[i];
        }
        Degrees[0].text = el.Ω + "˚";
        Degrees[1].text = el.ω + "˚";
        Degrees[2].text = el.i + "˚";
        GetVelocityRate();
        DrawRefresh();
    }
    private void FixedUpdate() {
        DrawRefresh();
    }
    void DrawRefresh() {
        orbitalLine.widthMultiplier = ConstMgr.LineScale * ConstMgr.ZoomRate;
        for (int i = 0; i < lines.Length; i++)
            lines[i].widthMultiplier = ConstMgr.LineScale * ConstMgr.ZoomRate;
        //축 갱신
        for (int i = 0; i < Axises.Length; i++) {
            Axises[i].widthMultiplier = 1.2f * ConstMgr.LineScale * ConstMgr.ZoomRate;
            Axises_Pos[i].localScale = 35f * ConstMgr.LineScale * ConstMgr.ZoomRate * Vector3.one;
            Axises_Text[i].position = CameraCtrl.instance.cam.WorldToScreenPoint(Axises_Pos[i].position);
        }
        infos[0].text = AstroMgr.instance.Focused.s_dist.ToString("N6");
        infos[2].text = AstroMgr.instance.Focused.el.mdm.ToString("N6");
        infos[3].text = (360f / AstroMgr.instance.Focused.el.mdm).ToString("N6");
        //Velocity
        lines[4].SetPositions(new Vector3[2] { focused.model.position, focused.model.position + GetVelocityVector() * (focused.isSolar ? el.a : el.aphel_dist) * 0.2f });
        //RadiusVector
        lines[5].SetPositions(new Vector3[2] { Vector3.zero, focused.model.position });
        //텍스트
        for (int i = 0; i < Indices.Length; i++) {
            Indices[i].position = lines[i].GetPosition(1);
            Indices[i].localScale = 35f * ConstMgr.LineScale * ConstMgr.ZoomRate * Vector3.one;
            Indices[i].rotation = Quaternion.LookRotation(lines[i].GetPosition(1) - lines[i].GetPosition(0));
            if (Indices_Text.Length > i)
                Indices_Text[i].position = CameraCtrl.instance.cam.WorldToScreenPoint(lines[i].GetPosition(1));
        }
    }
    Vector3 GetVelocityVector() {
        Vector3 result = 1 / Mathf.Sqrt(focused.p.magnitude) * 29.784852f * (-Mathf.Sin(focused.f) * focused.RM[0] + (focused.el.e + Mathf.Cos(focused.f)) * focused.RM[1]);
        infos[1].text = result.magnitude.ToString("N6");
        return (1 + (result.magnitude - Vel_Min) * Vel_Itv) * result.normalized;
    }
    void CalcRM(bool isPlane) {
        //단위 변환(디그리 -> 라디안)
        float Ω_rd = el.Ω * Mathf.PI / 180;
        float ω_rd = el.ω * Mathf.PI / 180;
        float i_rd = isPlane ? 0 : el.i * Mathf.PI / 180;
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
    void OrbitalCircleDraw() {
        float r_mag;
        float f = 0;
        Vector3 r;
        List<Vector3> verticiesList = new List<Vector3> { };
        for (; f <= 2 * Mathf.PI; f += draw_interval) {
            r_mag = (p / (1 + el.e * Mathf.Cos(f))).magnitude;
            r = r_mag * (Mathf.Cos(f) * RM[0] + Mathf.Sin(f) * RM[1]);
            verticiesList.Add(r);
        }
        Vector3[] verticies = verticiesList.ToArray();
        //triangles
        List<int> trianglesList = new List<int> { };
        for (int i = 0; i < (ConstMgr.INTERVAL_COUNT - 2); i++) {
            trianglesList.Add(0);
            trianglesList.Add(i + 1);
            trianglesList.Add(i + 2);
        }
        int[] triangles = trianglesList.ToArray();
        //initialise
        mesh.vertices = verticies;
        mesh.triangles = triangles;
    }
    void DrawLine() {
        float r_mag = (p / (1 + el.e * Mathf.Cos(-el.ω * Mathf.PI / 180))).magnitude;
        //LineOfNodes
        lines[0].SetPositions(new Vector3[2] { Vector3.zero, 1.3f * r_mag * new Vector3(Mathf.Cos(el.Ω * Mathf.PI / 180), 0, Mathf.Sin(el.Ω * Mathf.PI / 180)) });
        images[0].fillAmount = el.Ω / 360f;
        //PerihelionDirection
        CalcRM(false);
        r_mag = (p / (1 + el.e * Mathf.Cos(0))).magnitude;
        lines[1].SetPositions(new Vector3[2] { Vector3.zero, 1.3f * p.magnitude * (Mathf.Cos(0) * RM[0] + Mathf.Sin(0) * RM[1]) });
        images[1].fillAmount = el.ω / 360f;
        images[1].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        images[1].transform.Rotate(Vector3.forward, el.Ω);
        images[1].transform.Rotate(Vector3.right, -el.i);
        //SemiLatusRectum
        lines[2].SetPositions(new Vector3[2] { Vector3.zero, 1.3f * p.magnitude * (Mathf.Cos(0.5f * Mathf.PI) * RM[0] + Mathf.Sin(0.5f * Mathf.PI) * RM[1]) });
        //AngularMomentumVector
        lines[3].SetPositions(new Vector3[2] { Vector3.zero, Vector3.Cross(lines[2].GetPosition(1), lines[1].GetPosition(1)) / p.magnitude });
        images[2].transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Vector3 v = new Vector3(lines[3].GetPosition(1).x, 0, lines[3].GetPosition(1).z);
        float angle = Vector3.SignedAngle(-Vector3.right, v, Vector3.up);
        images[2].transform.Rotate(Vector3.right, angle);
        images[2].fillAmount = el.i / 360f;
        //범위 반경
        images[0].transform.localScale = r_mag * 1f * Vector2.one;
        images[1].transform.localScale = r_mag * 1f * Vector2.one;
        images[2].transform.localScale = r_mag * 1f * Vector2.one;
    }
    void GetVelocityRate() {
        float max = (1 / Mathf.Sqrt(focused.p.magnitude) * 29.784852f * (-Mathf.Sin(0) * focused.RM[0] + (focused.el.e + Mathf.Cos(0)) * focused.RM[1])).magnitude;
        Vel_Min = (1 / Mathf.Sqrt(focused.p.magnitude) * 29.784852f * (-Mathf.Sin(Mathf.PI) * focused.RM[0] + (focused.el.e + Mathf.Cos(Mathf.PI)) * focused.RM[1])).magnitude;
        Vel_Itv = focused.el.e.Equals(0) ? 0 : (1 / (max - Vel_Min));
    }
    public static float GetAngle(Vector3 vStart, Vector3 vEnd) {
        Vector3 v = vEnd - vStart;
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }
}
