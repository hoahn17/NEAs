using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusListItemCtrl : MonoBehaviour{
    public int idx=0;

    public void OnTouch() {
        FocusListCtrl.instance.OnSelected(idx);
    }
    
}
