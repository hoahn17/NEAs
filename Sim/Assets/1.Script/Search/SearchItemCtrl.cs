using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SearchItemCtrl : MonoBehaviour {
    public Text id, astro_name;
    public GameObject selectBar, CheckMark;
    public OrbitalElements elements;
    public bool isAdditionBtn = false;

    public void SetInfo(int idx, OrbitalElements elements, bool isSelected, bool isList) {
        this.elements = elements;
        isAdditionBtn = false;
        id.text = idx + 1 + "";
        astro_name.text = GetLabel() + "";
        selectBar.SetActive(isSelected);
        CheckMark.transform.parent.gameObject.SetActive(true);
        CheckMark.SetActive(isList);
    }
    public void SetAddition() {
        isAdditionBtn = true;
        id.text = "";
        astro_name.text = "<size=50><b>＋</b></size>";
        selectBar.SetActive(false);
        CheckMark.transform.parent.gameObject.SetActive(false);
    }
    string GetLabel() {
        string str = "";
        if (elements.name != null) {
            if (elements.number != null) {
                str += elements.number + " ";
            }
            str += elements.name;
        }
        else
            str += elements.principal_desig;
        return str;
    }
    public void ItemSelect() {
        if (!isAdditionBtn) {
            selectBar.SetActive(true);
            SearchMgr.instance.ItemSelect(elements);
        }
        else {
            SearchMgr.instance.CreateCustomAsteroid();
        }
    }
    public void OnBoxCheck() {
        CheckMark.SetActive(!CheckMark.activeSelf);
        SearchMgr.instance.ItemCheck(elements);
    }
}
