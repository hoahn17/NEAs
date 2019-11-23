using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using System;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Net;
using HtmlAgilityPack;


public enum ViewMode { heliocentric, perifocal, ellipse_plane}
[Serializable]
public struct OrbitalElements {
    public int ID;
    //고정값
    public DateTime epoch;
    //public Vector3 rp;//근일점 벡터
    //public Vector3 ra;//원일점 벡터
    public float e;//이심률: 장반경(a)과 단반경(b)의 비율.
    public float a;//장반경
    public float M0;//초기(epoch 시점) 평균 근점 이각
    public float mdm;//일일 평균 근점 이각 변화량
    //public Vector3 P, Q;//perifocal coordinate system상의 x,y 단위 벡터
    public float Ω;//승교점 경도(longitude of the ascending node)
    public float ω;//근일점 편각(argument of periapsis)
    public float i;//궤도 경사(inclination)
    //표시 정보
    public float period;
    public float peri_dist;
    public float aphel_dist;
    public float H;//절대 등급
    public string name;
    public string number;
    public string principal_desig;
    public string orbitType;
    public string last_Obs;
    public string semi_rectum;
}

public class AstroMgr : MonoBehaviour {
    public static AstroMgr instance;
    //고정 변수
    public PerifocalViewCtrl IV;
    public EllipseViewCtrl EV;
    public GameObject textPrepab, astroPrepab, IVCanvas, EVCanvas, intro, UpdateBtn;
    public Transform CameraTr;
    public Transform planetTextparent, astroParent;
    public LineRenderer[] axis;
    public Image playBtnImage;
    public Sprite[] btnImages;
    public Text FrameText, LastUpdate;
    public Dropdown ModeDropdown;
    string path, CurrentUpdate;
    //천체
    public LayerMask LM_astro;
    public List<AstroCtrl> solars, astros, ploted_Astro;
    List<PlanetText> planetTexts = new List<PlanetText>();
    //프레임 체커
    float deltaTime = 0.0f, msec, fps, worstFps = 100f;
    //유동변수
    public ViewMode currentMode;
    public AstroCtrl Focused;
    public bool isPlay = false, isDataLoaded = false, isDataDownLoaded = false, isInit = true;
    public Gradient[] colors, astro_Colors;
    WebClient webClient;

    private void Awake() {
        Application.targetFrameRate = 60;
        //Screen.SetResolution(2048, 2048, false);
        instance = this;
        for (int i = 0; i < solars.Count; i++) {
            GameObject newText = Instantiate(textPrepab, planetTextparent);
            planetTexts.Add(newText.GetComponent<PlanetText>());
            planetTexts[i].astro = solars[i];
        }
        playBtnImage.sprite = btnImages[isPlay ? 1 : 0];
        //데이터 다운
        path = Application.dataPath;
        CurrentUpdate = PlayerPrefs.GetString("CurrentVersion", "");
        DataUpdate();
        //프레임 체커
        StartCoroutine("worstReset");
        intro.SetActive(true);
    }
    public void DataUpdate() {
        StartCoroutine(WaitForDataDownload());
    }
    IEnumerator WaitForDataDownload() {
        Thread t = new Thread(LoadJson);
        t.Start();
        while (!isDataDownLoaded)
            yield return new WaitForFixedUpdate();
        PlayerPrefs.SetString("CurrentVersion", CurrentUpdate.Replace("&nbsp;", ""));
        //PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        LastUpdate.text = "Last Update: " + CurrentUpdate;
        while (!isDataLoaded)
            yield return new WaitForFixedUpdate();
        Debug.Log("DataLoad End");
        t.Join();
        isInit = false;
    }
    //데이터 읽기
    void ReadWebData() {
        //최신 갱신일
        webClient = new WebClient();
        string html = webClient.DownloadString("https://minorplanetcenter.net/data");
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);
        HtmlNode node = doc.DocumentNode.SelectNodes("//table")[1];
        string LastUpdate = node.SelectNodes("tr/td")[10].InnerText;
        //처음 실행시에는 파일 존재만 체크
        if (!isInit || !File.Exists(path + "/nea_extended.json")) {
            webClient.DownloadFile("https://minorplanetcenter.net/Extended_Files/nea_extended.json.gz", path + "/nea_extended.json.gz");
            DirectoryInfo directory = new DirectoryInfo(path);
            Decompress(directory.GetFiles("nea_extended.json.gz")[0]);
            CurrentUpdate = LastUpdate.Replace("&nbsp;", " ");
            Debug.Log(string.Format("File Update: {0}", CurrentUpdate));
        }
        else {
            Debug.Log("File Exist: " + CurrentUpdate);
        }
        isDataDownLoaded = true;
    }
    void LoadJson() {
        isDataLoaded = false;
        ReadWebData();
        string jsonstring = File.ReadAllText(path + "/nea_extended.json");
        JsonData data = JsonMapper.ToObject(jsonstring);
        ConstMgr.ASTROS = new OrbitalElements[data.Count];
        for (int i = 0; i < data.Count; i++) {
            ConstMgr.ASTROS[i].ID = i;
            ConstMgr.ASTROS[i].epoch = GetJulianDateTime(data[i]["Epoch"].ToString());
            ConstMgr.ASTROS[i].a = float.Parse(data[i]["a"].ToString());
            ConstMgr.ASTROS[i].e = float.Parse(data[i]["e"].ToString());
            ConstMgr.ASTROS[i].i = float.Parse(data[i]["i"].ToString());
            ConstMgr.ASTROS[i].M0 = float.Parse(data[i]["M"].ToString());
            ConstMgr.ASTROS[i].Ω = float.Parse(data[i]["Node"].ToString());
            ConstMgr.ASTROS[i].ω = float.Parse(data[i]["Peri"].ToString());
            ConstMgr.ASTROS[i].mdm = float.Parse(data[i]["n"].ToString());
            ConstMgr.ASTROS[i].peri_dist = float.Parse(data[i]["Perihelion_dist"].ToString());
            ConstMgr.ASTROS[i].period = float.Parse(data[i]["Orbital_period"].ToString());
            ConstMgr.ASTROS[i].aphel_dist = float.Parse(data[i]["Aphelion_dist"].ToString());
            try {
                ConstMgr.ASTROS[i].name = data[i]["Name"].ToString();
            }
            catch { }
            try {
                ConstMgr.ASTROS[i].number = data[i]["Number"].ToString();
            }
            catch { }
            try {
                ConstMgr.ASTROS[i].H = float.Parse(data[i]["H"].ToString());
            }
            catch { }
            ConstMgr.ASTROS[i].principal_desig = data[i]["Principal_desig"].ToString();
            ConstMgr.ASTROS[i].orbitType = data[i]["Orbit_type"].ToString();
            ConstMgr.ASTROS[i].last_Obs = data[i]["Last_obs"].ToString();
            ConstMgr.ASTROS[i].semi_rectum = data[i]["Semilatus_rectum"].ToString();
        }
        isDataLoaded = true;
    }
    DateTime GetJulianDateTime(string str) {
        float julianDate = float.Parse(str);
        float unixTime = (julianDate - 2440587.5f) * 86400;
        DateTime result = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        result = result.AddSeconds(unixTime).ToLocalTime();
        return result;
    }
    public static void Decompress(FileInfo fileToDecompress) {
        using (FileStream originalFileStream = fileToDecompress.OpenRead()) {
            string currentFileName = fileToDecompress.FullName;
            string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

            using (FileStream decompressedFileStream = File.Create(newFileName)) {
                using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress)) {
                    decompressionStream.CopyTo(decompressedFileStream);
                }
            }
        }
    }
    public void CreateNewAstro(OrbitalElements elements) {
        AstroCtrl newAstro = Instantiate(astroPrepab, astroParent).GetComponent<AstroCtrl>();
        GameObject newText = Instantiate(textPrepab, planetTextparent);
        newAstro.Init_Astro(elements);
        astros.Add(newAstro);
        ploted_Astro.Add(newAstro);
        newText.GetComponent<PlanetText>().astro = newAstro;
    }
    public void PlotAsteroid(List<OrbitalElements> list) {
        ploted_Astro = new List<AstroCtrl>();
        for (int i = 0; i < astros.Count; i++)
            astros[i].gameObject.SetActive(false);
        for (int i = 0; i < list.Count; i++) {
            if (i < astros.Count) {//확장 조건
                astros[i].gameObject.SetActive(true);
                astros[i].Init_Astro(list[i]);
                ploted_Astro.Add(astros[i]);
            }
            else {
                CreateNewAstro(list[i]);
            }
        }
        //ploted_Astro.Sort(delegate (AstroCtrl A, AstroCtrl B) {
        //    if (A.el.ID > B.el.ID) return 1; else return -1;
        //});
        FocusListCtrl.instance.RefreshList();
        FocusListCtrl.instance.OnSelected(0);
    }
    public void RemoveAsteroid(OrbitalElements elemet) {
        for (int i = ploted_Astro.Count - 1; i >= 0; i--) {
            if (ploted_Astro[i].el.Equals(elemet)) {
                ploted_Astro[i].gameObject.SetActive(false);
                ploted_Astro.RemoveAt(i);
                break;
            }
        }
    }
    //컨트롤
    public void AnimCtrl() {
        isPlay = !isPlay;
        AnimCtrl(isPlay);
        if (isPlay)
            CalendarCtrl.instance.IsOpen(false);
    }
    public void AnimCtrl(bool isPlay) {
        this.isPlay = isPlay;
        playBtnImage.sprite = btnImages[isPlay ? 1 : 0];
    }
    private void Update() {
        //축 갱신
        for (int i = 0; i < axis.Length; i++)
            axis[i].widthMultiplier = ConstMgr.LineScale * ConstMgr.ZoomRate;
        //키 입력
        if (!SearchMgr.instance.isOpen) {
            //행성 터치 검사
            //if (Input.GetMouseButtonDown(0))
            //    AstroTouch();
            //드래그 검사
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                CalendarCtrl.instance.ChangeSpeed(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                CalendarCtrl.instance.ChangeSpeed(1);
            if (Input.GetKeyDown(KeyCode.Space))
                AnimCtrl();
            if (Input.GetKeyDown(KeyCode.DownArrow))
                CalendarCtrl.instance.IsOpen(true);
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                CalendarCtrl.instance.IsOpen(false);
            if (Input.GetKeyDown(KeyCode.PageUp))
                FocusListCtrl.instance.ChangeSelect(-1);
            else if (Input.GetKeyDown(KeyCode.PageDown))
                FocusListCtrl.instance.ChangeSelect(1);
            //perifocal 단축키
            if (Input.GetKeyDown(KeyCode.P))
                ModeDropdown.value = currentMode.Equals(ViewMode.perifocal) ? 0 : 1;
            if (Input.GetKeyDown(KeyCode.E))
                ModeDropdown.value = currentMode.Equals(ViewMode.ellipse_plane) ? 0 : 2;
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
                ConstMgr.LineScale = Mathf.Clamp(ConstMgr.LineScale + 0.2f, 1f, 5f);
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
                ConstMgr.LineScale = Mathf.Clamp(ConstMgr.LineScale - 0.2f, 1f, 5f);
        }
        //프레임 단축키
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        FrameCheckerRefresh();
        if (Input.GetKeyDown(KeyCode.F) && Input.GetKey(KeyCode.LeftControl))
            ShowFrameRate();
    }
    void AstroTouch() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LM_astro)) {
            AstroCtrl target = hit.collider.GetComponentInParent<AstroCtrl>();
            SetFocus(target);
        }
    }
    public void SetFocus(AstroCtrl target) {
        Focused = target;
        switch (currentMode) {
            case ViewMode.heliocentric: CameraTr.SetParent(Focused.model); break;
            case ViewMode.perifocal: OnPerifocalMode(true); break;
            case ViewMode.ellipse_plane: OnEllipseMode(true); break;
        }
    }
    public void CloseAllPanel() {
        CalendarCtrl.instance.IsOpen(false);
        SearchMgr.instance.ClosePanel();
        FocusListCtrl.instance.Close();
    }
    public void ChangeViewMode(int index) {
        currentMode = (ViewMode)index;
        switch (index) {
            case 0: OnMainMode(); break;
            case 1: OnEllipseMode(false); OnPerifocalMode(true); break;
            case 2: OnPerifocalMode(false); OnEllipseMode(true); break;
        }
    }
    void OnMainMode() {
        OnPerifocalMode(false);
        OnEllipseMode(false);
        AxisVisible(true);
        SetFocus(Focused);
    }
    void OnPerifocalMode(bool isActive) {
        if (Focused.Equals(solars[0])) {
            FocusListCtrl.instance.OnSelected(1);
            return;
        }
        SearchMgr.instance.HidePanel(isActive);
        CameraTr.SetParent(solars[0].model);
        //정보 표시
        IV.gameObject.SetActive(isActive);
        IVCanvas.gameObject.SetActive(isActive);
        IV.InitInfo(Focused);
        VisibleSet(isActive);
        AxisVisible(true);
    }
    void OnEllipseMode(bool isActive) {
        if (Focused.Equals(solars[0])) {
            FocusListCtrl.instance.OnSelected(1);
            return;
        }
        SearchMgr.instance.HidePanel(isActive);
        EV.gameObject.SetActive(isActive);
        EVCanvas.gameObject.SetActive(isActive);
        EV.InitInfo(Focused);
        VisibleSet(isActive);
        AxisVisible(false);
    }
    void VisibleSet(bool isSet) {
        FocusListCtrl.instance.RefreshList();
        for (int i = 0; i < solars.Count; i++)
            solars[i].VisibleCtrl(!isSet);
        for (int i = 0; i < astros.Count; i++)
            astros[i].VisibleCtrl(!isSet);
        Focused.VisibleCtrl(true);
        for (int i = 0; i < axis.Length; i++)
            axis[i].widthMultiplier = ConstMgr.LineScale * ConstMgr.ZoomRate;
        Focused.DrawRefresh();
    }
    void AxisVisible(bool isVisible) {
        for (int i = 0; i < axis.Length; i++)
            axis[i].enabled = isVisible;
    }
    //디버그
    void DebugAstro(int idx) {
        Debug.Log(ConstMgr.ASTROS[idx].epoch);
        Debug.Log(ConstMgr.ASTROS[idx].a);
        Debug.Log(ConstMgr.ASTROS[idx].e);
        Debug.Log(ConstMgr.ASTROS[idx].i);
        Debug.Log(ConstMgr.ASTROS[idx].M0);
        Debug.Log(ConstMgr.ASTROS[idx].Ω);
        Debug.Log(ConstMgr.ASTROS[idx].ω);
        Debug.Log(ConstMgr.ASTROS[idx].mdm);
        Debug.Log(ConstMgr.ASTROS[idx].principal_desig);
        Debug.Log(ConstMgr.ASTROS[idx].orbitType);
        Debug.Log(ConstMgr.ASTROS[idx].name);
        Debug.Log(ConstMgr.ASTROS[idx].number);
    }
    void TimerCheck() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        //측정 코드---------------------------------------------------------
        ReadWebData();
        //------------------------------------------------------------------
        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds.ToString() + "ms _ load");
    }
    void ShowFrameRate() {
        FrameText.gameObject.SetActive(!FrameText.gameObject.activeSelf);
        UpdateBtn.SetActive(FrameText.gameObject.activeSelf);
    }
    IEnumerator worstReset() //코루틴으로 15초 간격으로 최저 프레임 리셋해줌.
    {
        while (true) {
            yield return new WaitForSeconds(15f);
            worstFps = 100f;
        }
    }
    void FrameCheckerRefresh() {
        msec = deltaTime * 1000.0f;
        fps = 1.0f / deltaTime;  //초당 프레임 - 1초에
        if (fps < worstFps)  //새로운 최저 fps가 나왔다면 worstFps 바꿔줌.
            worstFps = fps;
        FrameText.text = msec.ToString("F1") + "ms (" + fps.ToString("F1") + ") //worst : " + worstFps.ToString("F1");
    }
}
