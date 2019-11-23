using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FocusListCtrl : MonoBehaviour {
    public static FocusListCtrl instance;
    //고정변수
    List<Text> itemList = new List<Text>();
    public GameObject prepab;
    public ScrollRect scroll;
    public RectTransform contents;
    Vector2 size;
    //유동변수
    public Text[] InfoTexts;
    Text FocusedText;
    public bool isOpen = false;
    int selected = 0;

    private void Start() {
        instance = this;
        size = prepab.GetComponent<RectTransform>().sizeDelta;
        InstantiateObject();
        itemList[0].fontStyle = FontStyle.Bold;
        itemList[0].fontSize = 40;
        FocusedText = itemList[0];
    }
    public void IsOpen() {
        isOpen = !isOpen;
        GetComponent<Animator>().SetBool("IsOpen", isOpen);
        RefreshList();
    }
    public void Close() {
        isOpen = false;
        GetComponent<Animator>().SetBool("IsOpen", isOpen);
    }
    public void RefreshList() {
        int solarLength = AstroMgr.instance.solars.Count;
        int astroLength = AstroMgr.instance.ploted_Astro.Count;
        contents.sizeDelta = new Vector2(0, size.y * (solarLength + (astroLength.Equals(0) ? 0 : (astroLength + 1))));
        while (itemList.Count <= solarLength + astroLength + 1)
            InstantiateObject();
        for (int i = 0; i < itemList.Count; i++) {
            itemList[i].GetComponent<Button>().interactable = true;
            itemList[i].transform.SetSiblingIndex(i+1);
            if (i < solarLength) {
                itemList[i].text = GetLable(AstroMgr.instance.solars[i]);
                itemList[i].GetComponent<FocusListItemCtrl>().idx = i;
                itemList[i].gameObject.SetActive(true);
            }
            else if (i < solarLength + astroLength) {
                itemList[i].text = GetLable(AstroMgr.instance.ploted_Astro[i - solarLength]);
                itemList[i].GetComponent<FocusListItemCtrl>().idx = i;
                itemList[i].gameObject.SetActive(true);
            }
            else {
                itemList[i].gameObject.SetActive(false);
            }
        }
        DeactiveSunBtn(AstroMgr.instance.currentMode.Equals(ViewMode.heliocentric));
        if (!astroLength.Equals(0)) {
            itemList[itemList.Count - 1].gameObject.SetActive(true);
            itemList[itemList.Count - 1].text = "-Asteroid-";
            itemList[itemList.Count - 1].GetComponent<Button>().interactable = false;
            itemList[itemList.Count - 1].transform.SetSiblingIndex(solarLength + 1);
        }
        else {
            itemList[itemList.Count - 1].gameObject.SetActive(false);
        }
    }
    void InstantiateObject() {
        for (int i = 0; i < 10; i++) {
            GameObject newItem = Instantiate(prepab, prepab.transform.parent);
            itemList.Add(newItem.GetComponent<Text>());
        }
    }
    void TextFocus(Text text) {
        FocusedText.fontStyle = FontStyle.Normal;
        FocusedText.fontSize = 35;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 40;
        FocusedText = text;
    }
    public void OnSelected(int index) {
        int solarLength = AstroMgr.instance.solars.Count;
        TextFocus(itemList[index]);
        if (index < solarLength) {
            AstroMgr.instance.SetFocus(AstroMgr.instance.solars[index]);
        }
        else {
            AstroMgr.instance.SetFocus(AstroMgr.instance.ploted_Astro[index - solarLength]);
        }
        selected = index;
        for (int i = 0; i < InfoTexts.Length; i++)
            InfoTexts[i].text = GetLable(AstroMgr.instance.Focused);
    }
    public void ChangeSelect(int dir) {
        int solarLength = AstroMgr.instance.solars.Count;
        int astroLength = AstroMgr.instance.ploted_Astro.Count;
        int newSelected = Mathf.Clamp(selected + dir, 0, solarLength + astroLength - 1);
        if (!AstroMgr.instance.currentMode.Equals(ViewMode.heliocentric) ? !newSelected.Equals(0) : true)
            OnSelected(newSelected);
    }
    public void DeactiveSunBtn(bool isActive) {
        itemList[0].GetComponent<Button>().interactable = isActive;
    }
    string GetLable(AstroCtrl astro) {
        string str = "";
        if (astro.el.name != null) {
            if (!astro.el.number.Equals("")) {
                str += astro.el.number + " ";
            }
            str += astro.el.name;
        }
        else {
            str += astro.el.principal_desig;
        }
        return str;
    }
}
