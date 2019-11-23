using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClaendarItemCtrl : MonoBehaviour
{
    public void SelectItem() {
        CalendarCtrl.instance.SelectDay(transform.GetSiblingIndex());
    }
}
