using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public struct Detail_Item {
    public InputField[] values;
}
public class SearchMgr : MonoBehaviour {
    public static SearchMgr instance;
    //고정변수
    Animator anim;
    public Animator TabAnim;
    public CanvasGroup PlotBtn, CustomBtn, ClearBtn, SearchBar;
    public GameObject LoadingIcon, AllCheckMark, FavoritMark, detailBtn, OpenBtn;
    public SearchItemCtrl[] items;
    public InputField pageInput, searchInput, rndInput;
    public InputField[] customInputs;
    public Text maxText, title, listText;
    public Text[] infoTexts, customValues;
    public Detail_Item[] Detail_Tiems;
    public Dropdown Detail_Type;
    //유동변수
    List<OrbitalElements> source = new List<OrbitalElements>();
    List<OrbitalElements> astroList = new List<OrbitalElements>();
    public List<OrbitalElements> customList = new List<OrbitalElements>();
    public List<OrbitalElements> favoritList = new List<OrbitalElements>();
    List<OrbitalElements> checkList = new List<OrbitalElements>();
    OrbitalElements selectEle;
    bool isDetail = false, isSearchFocus = false, isCustom = false;
    int currentPage = 1, maxPage = 1, sourceIndex = 0;
    public bool isOpen = false;

    private void Awake() {
        instance = this;
        DataMgr.DataLoad();
    }
    private void Start() {
        anim = GetComponent<Animator>();
        for (int i = 0; i < infoTexts.Length; i++)
            infoTexts[i].text = "";
        for (int i = 0; i < items.Length; i++)
            items[i].gameObject.SetActive(false);
        StartCoroutine(WaitForDataLoad());
    }
    IEnumerator WaitForDataLoad() {
        LoadingIcon.SetActive(true);
        while (!AstroMgr.instance.isDataLoaded)
            yield return new WaitForFixedUpdate();
        LoadingIcon.SetActive(false);
        astroList = new List<OrbitalElements>(ConstMgr.ASTROS);
        PlotBtn.interactable = true;
        PlotBtn.alpha = 1f;
        SetSource(sourceIndex);
        PageRefresh();
    }
    private void Update() {
        if (isSearchFocus && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            SearchAsteroid();
        isSearchFocus = searchInput.isFocused;
    }
    public void SetSource(int idx) {
        if (!AstroMgr.instance.isDataLoaded)
            return;
        sourceIndex = idx;
        switch (idx) {
            case 0: source = astroList; break;
            case 1: source = customList; break;
            case 2: source = favoritList; break;
            case 3: checkList.Sort(delegate (OrbitalElements A, OrbitalElements B) {
                    if (A.ID > B.ID) return 1; else return -1;
                }); source = checkList; break;
        }
        for (int i = 0; i < customList.Count; i++) {
            OrbitalElements orbit = customList[i];
            orbit.ID = 100000 + i;
            customList[i] = orbit;
        }
        if(isDetail || isCustom) {
            ClosePanel();
        }
        SearchBar.interactable = !sourceIndex.Equals(3);
        SearchBar.alpha = !sourceIndex.Equals(3) ? 1f : 0.5f;
        TabAnim.SetInteger("SourceIndex", sourceIndex);
        PageInit();
        PageRefresh();
    }
    public void OpenPanel() {
        if (anim.GetBool("IsOpen"))
            return;
        AstroMgr.instance.AnimCtrl(false);
        AstroMgr.instance.CloseAllPanel();
        SetSource(sourceIndex);
        anim.SetBool("IsOpen", true);
        isOpen = true;
        OpenBtn.SetActive(false);
    }
    public void ClosePanel() {
        if (isDetail) {
            isDetail = false;
            anim.SetBool("IsDetail", false);
        }
        else if (isCustom) {
            isCustom = false;
            anim.SetBool("IsCustom", false);
        }
        else {
            anim.SetBool("IsOpen", false);
            isOpen = false;
            SearchBar.interactable = true;
            SearchBar.alpha = 1f;
            OpenBtn.SetActive(true);
        }
        title.text = "Asteorid Search";
        searchInput.text = "";
        detailBtn.SetActive(true);
    }
    public void HidePanel(bool isHide) {
        anim.SetBool("IsDisappear", isHide);
    }
    void PageRefresh() {
        int addition = sourceIndex.Equals(1) ? 1 : 0;
        //업데이트
        maxPage = (source.Count - 1 + addition) / items.Length + 1;
        pageInput.text = currentPage.ToString();
        maxText.text = maxPage.ToString();
        for (int i = 0; i < items.Length; i++) {
            int idx = i + (currentPage - 1) * items.Length;
            if (idx >= source.Count + addition) {
                items[i].gameObject.SetActive(false);
            }
            else {
                items[i].gameObject.SetActive(true);
                if (idx < source.Count) {
                    items[i].SetInfo(idx, source[idx], source[idx].Equals(selectEle), checkList.Contains(source[idx]));
                }
                else {
                    items[i].SetAddition();
                }
            }
        }
        AllCheckMark.SetActive(IsAllChecked());
        listText.text = string.Format("List [{0}]", checkList.Count);
        ClearBtnRefresh();
        InfoRefresh();
    }
    void PageInit() {
        if (source.Count.Equals(0))
            return;
        currentPage = 1;
        selectEle = source[0];
    }
    public void ClearBtnRefresh() {
        ClearBtn.gameObject.SetActive(sourceIndex.Equals(3) || sourceIndex.Equals(1));
        if (sourceIndex.Equals(3)) {//리스트일 때
            ClearBtn.GetComponentInChildren<Text>().text = "Clear";
            ClearBtn.interactable = !checkList.Count.Equals(0);
            ClearBtn.alpha = !checkList.Count.Equals(0) ? 1f : 0.5f;
        }
        else if(sourceIndex.Equals(1)) {//커스텀일 때
            ClearBtn.GetComponentInChildren<Text>().text = "Delete";
            bool isActive = false;
            for (int i = items.Length - 1; i >= 0; i--)
                if (items[i].gameObject.activeSelf && items[i].CheckMark.activeSelf && !items[i].isAdditionBtn) {
                    isActive = true;
                    break;
                }
            ClearBtn.interactable = isActive;
            ClearBtn.alpha = isActive ? 1f : 0.5f;
        }
    }
    public void PageChange(int dir) {
        currentPage += dir;
        currentPage = Mathf.Clamp(currentPage, 1, maxPage);
        PageRefresh();
    }
    public void PageInput(string str) {
        bool result = int.TryParse(str, out int value);
        if (result) {
            value = Mathf.Clamp(value, 1, maxPage);
            currentPage = value;
        }
        PageRefresh();
    }
    //리스트 선택
    public void CheckAllList() {
        if (!AstroMgr.instance.isDataLoaded)
            return;
        bool isAllChecked = true;
        for (int i = 0; i < items.Length; i++) {
            if (items[i].gameObject.activeSelf && !items[i].CheckMark.activeSelf && !items[i].isAdditionBtn) {
                items[i].OnBoxCheck();
                isAllChecked = false;
            }
        }
        if (isAllChecked)
            for (int i = items.Length - 1; i >= 0; i--)
                if (items[i].gameObject.activeSelf && !items[i].isAdditionBtn)
                    items[i].OnBoxCheck();
        //PageRefresh();
    }
    bool IsAllChecked() {
        if (source.Count.Equals(0))
            return false;
        for (int i = 0; i < items.Length; i++)
            if (items[i].gameObject.activeSelf && !items[i].CheckMark.activeSelf && !items[i].isAdditionBtn) {
                return false;
            }
        return true;
    }
    public void ItemSelect(OrbitalElements ele) {
        selectEle = ele;
        PageRefresh();
    }
    public void ItemCheck(OrbitalElements ele) {
        if (checkList.Contains(ele)) {
            checkList.Remove(ele);
        }
        else {
            checkList.Add(ele);
        }
        AllCheckMark.SetActive(IsAllChecked());
        PageRefresh();
    }
    public void RndomSelect(string str) {//테스트
        if (!AstroMgr.instance.isDataLoaded)
            return;
        bool result = int.TryParse(str, out int value);
        if (result) {
            checkList.Clear();
            value = Mathf.Clamp(value, 0, 500);
            rndInput.text = value.ToString();
            List<OrbitalElements> rndList = new List<OrbitalElements>(ConstMgr.ASTROS);
            for (int i = 0; i < value; i++) {
                int rnd = UnityEngine.Random.Range(i, rndList.Count);
                OrbitalElements temp = rndList[i];
                rndList[i] = rndList[rnd];
                rndList[rnd] = temp;
                checkList.Add(rndList[i]);
            }
        }
        checkList.Sort(delegate (OrbitalElements A, OrbitalElements B) {
            if (A.ID > B.ID) return 1; else return -1;
        });
        PageRefresh();
    }
    public void listClear() {
        if (sourceIndex.Equals(3)) {//리스트일 때
            checkList.Clear();
            AstroMgr.instance.PlotAsteroid(checkList);
        }
        else if (sourceIndex.Equals(1)) {//커스텀일 때
            for (int i = items.Length - 1; i >= 0; i--) {
                if (items[i].gameObject.activeSelf && items[i].CheckMark.activeSelf && !items[i].isAdditionBtn) {
                    customList.Remove(items[i].elements);
                    checkList.Remove(items[i].elements);
                    favoritList.Remove(items[i].elements);
                    AstroMgr.instance.RemoveAsteroid(items[i].elements);
                }
            }
            DataMgr.DataSave();
        }
        PageInit();
        PageRefresh();
    }
    //정보 출력
    void InfoRefresh() {
        if (!source.Count.Equals(0)) {
            infoTexts[0].text = GetLabel(selectEle);
            infoTexts[1].text = string.Format("<color=#939393>Epoch:</color> {0}", GetGregoriDate(selectEle.epoch));
            infoTexts[2].text = selectEle.a + "";
            infoTexts[3].text = selectEle.e + "";
            infoTexts[4].text = selectEle.i + "";
            infoTexts[5].text = selectEle.Ω + "";
            infoTexts[6].text = selectEle.ω + "";
            infoTexts[7].text = selectEle.period + "";
            infoTexts[8].text = selectEle.M0 + "";
            infoTexts[9].text = selectEle.mdm + "";
            infoTexts[10].text = selectEle.peri_dist + "";
            infoTexts[11].text = selectEle.aphel_dist + "";
            infoTexts[12].text = selectEle.H + "";
            infoTexts[13].text = selectEle.last_Obs + "";
            FavoritMark.SetActive(favoritList.Contains(selectEle));
        }
        else {
            for (int i = 0; i < infoTexts.Length; i++)
                infoTexts[i].text = "";
            FavoritMark.SetActive(false);
        }
    }
    string GetLabel(OrbitalElements elements) {
        string str = "";
        if (elements.name != null) {
            if (elements.number != null) {
                str += elements.number + " ";
            }
            str += elements.name;
        }
        else
            str += elements.principal_desig;
        return string.Format("{0}<color=#939393>〔{1}〕</color>", str, elements.orbitType);
    }
    string GetGregoriDate(DateTime date) {
        return string.Format("{0}-{1}-{2}", date.Year, date.Month, date.Day);
    }
    public void PlotAsteroid() {
        checkList.Sort(delegate (OrbitalElements A, OrbitalElements B) {
            if (A.ID > B.ID) return 1; else return -1;
        });
        AstroMgr.instance.PlotAsteroid(checkList);
        ClosePanel();
    }
    //검색
    public void SearchAsteroid() {
        if (!AstroMgr.instance.isDataLoaded || sourceIndex.Equals(3))
            return;
        if (isCustom) {
            CalcCustomInfo();
            return;
        }
        string str = searchInput.text;
        source.Clear();
        for (int i = 0; i < ConstMgr.ASTROS.Length; i++) {
            if (CheckKeyword(ConstMgr.ASTROS[i], str.ToLower()))
                source.Add(ConstMgr.ASTROS[i]);
        }
        PageInit();
        PageRefresh();
    }
    bool CheckKeyword(OrbitalElements elements, string keyword) {
        string[] strs = keyword.Split(' ');
        for (int i = 0; i < strs.Length; i++) {
            bool isPass = false;
            if (elements.name != null && elements.name.ToLower().Contains(strs[i]))
                isPass = true;
            else if (elements.number != null && elements.number.ToLower().Contains(strs[i]))
                isPass = true;
            else if (elements.principal_desig != null && elements.principal_desig.ToLower().Contains(strs[i]))
                isPass = true;
            if (!isPass)
                return false;
        }
        return true;
    }
    //상세 검색
    public void DetailOpen() {
        isDetail = !isDetail;
        anim.SetBool("IsDetail", isDetail);
    }
    public void DetailSearch() {
        if (!AstroMgr.instance.isDataLoaded)
            return;
        //이름으로 1차검색
        string str = searchInput.text;
        List<OrbitalElements> temp = new List<OrbitalElements>();
        //상세 검색
        for (int i = 0; i < ConstMgr.ASTROS.Length; i++) {
            bool isPass = true;
            if (!CheckKeyword(ConstMgr.ASTROS[i], str.ToLower()))
                isPass = false;
            else
                for (int j = 0; j < Detail_Tiems.Length; j++) {
                    if (!DetailCheck(j, ConstMgr.ASTROS[i])) {
                        isPass = false;
                        break;
                    }
                }
            if (isPass)
                temp.Add(ConstMgr.ASTROS[i]);
        }
        source = temp;
        PageInit();
        PageRefresh();
        ClosePanel();
    }
    bool DetailCheck(int idx, OrbitalElements elem) {
        float value = 0;
        bool MinExist = float.TryParse(Detail_Tiems[idx].values[0].text, out float min);
        bool MaxExist = float.TryParse(Detail_Tiems[idx].values[1].text, out float max);
        if (!Detail_Type.value.Equals(0)) {
            if (!elem.orbitType.Equals(ConstMgr.ASTRO_TYPE[Detail_Type.value]))
                return false;
        }
        switch (idx) {
            case 0: value = elem.a; break;
            case 1: value = elem.e; break;
            case 2: value = elem.i; break;
            case 3: value = elem.Ω; break;
            case 4: value = elem.ω; break;
            case 5: value = elem.period; break;
            case 6: value = elem.M0; break;
            case 7: value = elem.mdm; break;
            case 8: value = elem.peri_dist; break;
            case 9: value = elem.aphel_dist; break;
            case 10: value = elem.H; break;
        }
        return (!MinExist || value >= min) && (!MaxExist || value <= max);
    }
    //즐겨찾기 추가
    public void FavoritCheck() {
        if (source.Count.Equals(0))
            return;
        if (!favoritList.Contains(selectEle)) {
            favoritList.Add(selectEle);
            FavoritMark.SetActive(true);
        }
        else {
            favoritList.Remove(selectEle);
            FavoritMark.SetActive(false);
        }
        PageRefresh();
        DataMgr.DataSave();
    }
    //커스텀 추가
    public void CreateCustomAsteroid() {
        searchInput.text = "";
        isCustom = true;
        anim.SetBool("IsCustom", true);
        title.text = "Asteorid Name";
        detailBtn.SetActive(false);
        CustomBtn.interactable = false;
        CustomBtn.alpha = 0.5f;
    }
    public void CalcCustomInfo() {
        //입력값 조정
        bool isValid, isActive = true;
        string name = searchInput.text;
        name.Trim();
        if (name.Equals(""))
            isActive = false;
        for (int i = 0; i < 8; i++) {
            isValid = double.TryParse(customInputs[i].text, out double data);
            if (isValid) {
                if (data < ConstMgr.CUSTOM_MIN[i]) customInputs[i].text = ConstMgr.CUSTOM_MIN[i].ToString();
                else if (data > ConstMgr.CUSTOM_MAX[i]) customInputs[i].text = ConstMgr.CUSTOM_MAX[i].ToString();
                else customInputs[i].text = data.ToString();
            }
            else {
                customInputs[i].text = "─";
                isActive = false;
            }
        }
        isValid = DateTime.TryParse(customInputs[8].text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime epoch);
        if (isValid) {
            if (epoch > ConstMgr.MAX_DATE) epoch = ConstMgr.MAX_DATE;
            else if (epoch < ConstMgr.MIN_DATE) epoch = ConstMgr.MIN_DATE;
            customInputs[8].text = GetGregoriDate(epoch);
        }
        else {
            customInputs[8].text = "";
            isActive = false;
        }
        //표시 정보 계산
        bool is_a_Input = float.TryParse(customInputs[0].text, out float a);
        bool is_e_Input = float.TryParse(customInputs[1].text, out float e);
        bool is_n_Input = float.TryParse(customInputs[6].text, out float n);
        if (is_a_Input && is_e_Input) {
            float Q = a * (1 + e);
            float q = a * (1 - e);
            customValues[1].text = q + "";
            customValues[2].text = Q + "";
            if (a < 1.0 && Q < 0.983) {//Atira
                customValues[3].text = ConstMgr.ASTRO_TYPE[1];
            }
            else if (a < 1.0 && Q > 0.983) {//Aten
                customValues[3].text = ConstMgr.ASTRO_TYPE[2];
            }
            else if (a > 1.0 && q < 1.017) {//Apollo
                customValues[3].text = ConstMgr.ASTRO_TYPE[3];
            }
            else if (a > 1.0 && q > 1.017 && q < 1.3) {//Amor
                customValues[3].text = ConstMgr.ASTRO_TYPE[4];
            }
            else {//etc
                customValues[3].text = ConstMgr.ASTRO_TYPE[5];
            }
        }
        else {
            customValues[1].text = "─";
            customValues[2].text = "─";
            customValues[3].text = "─";
        }
        if (is_n_Input)
            customValues[0].text = (360 / (n * 365.2564)).ToString("N4");//1년을 항성년으로 계산
        else {
            customValues[0].text = "─";
        }
        //버튼 활성화
        CustomBtn.interactable = isActive;
        CustomBtn.alpha = isActive ? 1f : 0.5f;
    }
    public void AddNewCustom() {
        OrbitalElements newOrbit = new OrbitalElements();
        newOrbit.ID = 100000 + customList.Count;
        newOrbit.name = searchInput.text;
        newOrbit.a = float.Parse(customInputs[0].text);
        newOrbit.e = float.Parse(customInputs[1].text);
        newOrbit.i = float.Parse(customInputs[2].text);
        newOrbit.Ω = float.Parse(customInputs[3].text);
        newOrbit.ω = float.Parse(customInputs[4].text);
        newOrbit.M0 = float.Parse(customInputs[5].text);
        newOrbit.mdm = float.Parse(customInputs[6].text);
        newOrbit.H = float.Parse(customInputs[7].text);
        newOrbit.epoch = DateTime.Parse(customInputs[8].text);
        newOrbit.period = float.Parse(customValues[0].text);
        newOrbit.peri_dist = float.Parse(customValues[1].text);
        newOrbit.aphel_dist = float.Parse(customValues[2].text);
        newOrbit.orbitType = customValues[3].text;
        newOrbit.number = "";
        customList.Add(newOrbit);
        anim.SetBool("IsCustom", false);
        isCustom = false;
        PageInit();
        PageRefresh();
        DataMgr.DataSave();
    }
    //데이터 업데이트
    public void UpdateData() {
        if (!AstroMgr.instance.isDataLoaded)
            return;
        //초기화
        checkList.Clear();
        AstroMgr.instance.PlotAsteroid(checkList);
        source.Clear();
        PageRefresh();
        //데이터 로드
        AstroMgr.instance.DataUpdate();
        StartCoroutine(WaitForDataLoad());
    }
}