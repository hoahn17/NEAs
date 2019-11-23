using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CalendarCtrl : MonoBehaviour {
    public static CalendarCtrl instance;
    //고정 변수
    Animator anim;
    public Animator TCAnim;
    DateTime dateTime = DateTime.Now, selectDate = DateTime.Now;
    public Button[] dateItems;
    public InputField yearText, monthText;
    public Text y_m_dText, speedText;
    public Image[] Marker;//0: 투데이, 1: 셀렉트
    public Slider speedSlider;
    //유동 변수
    public bool isOpen = false;
    int showSpeedCnt = 0, timeCnt = 1, simLevel = 0;
    public float daysInterval = 0;

    private void Awake() {
        instance = this;
    }
    void Start() {
        anim = GetComponent<Animator>();
        CreateCalendar();
        TimeUpdate();
        simLevel = ConstMgr.SpeedLevel;
    }

    public void IsOpen() {
        isOpen = !isOpen;
        IsOpen(isOpen);
    }
    public void IsOpen(bool isOpen) {
        this.isOpen = isOpen;
        anim.SetBool("IsOpen", isOpen);
        if (isOpen) {
            dateTime = selectDate;
            CreateCalendar();
            AstroMgr.instance.AnimCtrl(false);
        }
    }
    private void FixedUpdate() {
        if (showSpeedCnt > 0) {
            showSpeedCnt--;
            if(showSpeedCnt.Equals(0))
                TCAnim.SetBool("IsOpen", false);
        }
        if (timeCnt > 0) {
            timeCnt--;
            if (timeCnt.Equals(0)) {
                timeCnt = ConstMgr.ShowSpeed;
                if (AstroMgr.instance.isPlay) {
                    TimeUpdate();
                    y_m_dText.text = string.Format("{0}-{1:D2}-{2:D2}", selectDate.Year, selectDate.Month, selectDate.Day);
                }
            }
        }
    }
    void TimeUpdate() {
        DateTime addDate = selectDate;
        int sign = 1;
        simLevel = ConstMgr.SpeedLevel;
        if (simLevel < 0) sign = -1;
        switch (Mathf.Abs(simLevel)) {
            case 1: addDate = selectDate.AddDays(sign * 1); break;
            case 2: addDate = selectDate.AddDays(sign * 7); break;
            case 3: addDate = selectDate.AddMonths(sign * 1); break;
            case 4: addDate = selectDate.AddMonths(sign * 3); break;
            case 5: addDate = selectDate.AddMonths(sign * 6); break;
            case 6: addDate = selectDate.AddYears(sign * 1); break;
        }
        daysInterval = (addDate.Date - selectDate.Date).Days;
        selectDate = selectDate.AddSeconds(864 * daysInterval);
        if (selectDate > ConstMgr.MAX_DATE) {
            selectDate = ConstMgr.MAX_DATE;
            AstroMgr.instance.AnimCtrl(false);
        }
        else if(selectDate < ConstMgr.MIN_DATE){
            selectDate = ConstMgr.MIN_DATE;
            AstroMgr.instance.AnimCtrl(false);
        }
    }
    void CreateCalendar() {
        if (dateTime > ConstMgr.MAX_DATE) {
            dateTime = ConstMgr.MAX_DATE;
        }
        else if (dateTime < ConstMgr.MIN_DATE) {
            dateTime = ConstMgr.MIN_DATE;
        }
        int thisYear = dateTime.Year;
        int thisMonth = dateTime.Month;
        DateTime ShowDay = dateTime.AddDays(-(dateTime.Day - 1));
        int index = (int)ShowDay.DayOfWeek;
        ShowDay = ShowDay.AddDays(-index);

        Marker[0].gameObject.SetActive(false);
        Marker[1].gameObject.SetActive(false);

        for (int i = 0; i < dateItems.Length; i++) {
            DateTime thatDay = ShowDay.AddDays(i);
            dateItems[i].interactable = thatDay.Month.Equals(thisMonth);
            dateItems[i].GetComponent<Text>().text = (thatDay.Day).ToString();
            if (SelectCheck(thatDay)) {
                Marker[1].gameObject.SetActive(true);
                Marker[1].transform.position = dateItems[i].transform.position;
            } else if (TodayCheck(thatDay)) {
                Marker[0].gameObject.SetActive(true);
                Marker[0].transform.position = dateItems[i].transform.position;
            }
        }
        yearText.text = thisYear.ToString();
        monthText.text = thisMonth.ToString();
        y_m_dText.text = string.Format("{0}-{1:D2}-{2:D2}", selectDate.Year, selectDate.Month, selectDate.Day);
    }
    bool TodayCheck(DateTime date) {
        DateTime today = DateTime.Now;
        return date.Year.Equals(today.Year) && date.Month.Equals(today.Month) && date.Day.Equals(today.Day);
    }
    bool SelectCheck(DateTime date) {
        return date.Year.Equals(selectDate.Year) && date.Month.Equals(selectDate.Month) && date.Day.Equals(selectDate.Day);
    }
    public void ChangeYear(int dir) {
        dateTime = dateTime.AddYears(dir);
        CreateCalendar();
    }
    public void ChangeMonth(int dir) {
        dateTime = dateTime.AddMonths(dir);
        CreateCalendar();
    }
    public void SelectDay(int idx) {
        DateTime ShowDay = dateTime.AddDays(-(dateTime.Day - 1));
        int index = (int)ShowDay.DayOfWeek;
        ShowDay = ShowDay.AddDays(-index + idx);
        selectDate = ShowDay;
        CreateCalendar();
    }
    public void YearInput(string str) {
        bool result = int.TryParse(str, out int value);
        if (result) {
            value = Mathf.Clamp(value, ConstMgr.MIN_DATE.Year, ConstMgr.MAX_DATE.Year);
            dateTime = new DateTime(value, dateTime.Month, dateTime.Day);
        }
        CreateCalendar();
    }
    public void MonthInput(string str) {
        bool result = int.TryParse(str, out int value);
        if (result) {
            value = Mathf.Clamp(value, 1, 12);
            dateTime = new DateTime(dateTime.Year, value, dateTime.Day);
        }
        CreateCalendar();
    }
    public void SpeedInput(Single level) {
        SetSpeedLevel((int)level);
    }
    public void ChangeSpeed(int dir) {
        SetSpeedLevel(ConstMgr.SpeedLevel + dir);
        speedSlider.value = ConstMgr.SpeedLevel;
    }
    public void SetSpeedLevel(int level) {
        ConstMgr.SpeedLevel = level;
        ConstMgr.SpeedLevel = Mathf.Clamp(ConstMgr.SpeedLevel, -6, 6);
        string str = "";
        if (ConstMgr.SpeedLevel > 0) str = "+";
        else if (ConstMgr.SpeedLevel < 0) str = "-";
        switch (Mathf.Abs(ConstMgr.SpeedLevel)) {
            case 0: str += "pause"; break;
            case 1: str += "1 day"; break;
            case 2: str += "1 week"; break;
            case 3: str += "1 month"; break;
            case 4: str += "3 month"; break;
            case 5: str += "6 month"; break;
            case 6: str += "1 year"; break;
        }
        speedText.text = str;
        showSpeedCnt = 300;
        TCAnim.SetBool("IsOpen", true);
    }
    public float GetDayGap(DateTime epochDate) {
        float originDays = (float)(selectDate - epochDate).TotalDays;
        return originDays;
    }
    public void SetToday() {
        selectDate = DateTime.Now;
        dateTime = selectDate;
        CreateCalendar();
    }
}
