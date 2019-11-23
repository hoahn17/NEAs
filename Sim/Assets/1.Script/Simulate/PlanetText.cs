using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlanetText : MonoBehaviour {
    public AstroCtrl astro;
    Text text;
    private void Start() {
        text = GetComponentInChildren<Text>();
    }
    private void Update() {
        text.enabled = astro.gameObject.activeSelf;
        if (astro.gameObject.activeSelf && astro.isVisible) {
            text.text = SetLable();
            transform.position = CameraCtrl.instance.cam.WorldToScreenPoint(astro.model.position);
        }
        else {
            text.text = "";
        }
    }
    string SetLable() {
        string str = "";
        if(astro.el.name != null) {
            if (astro.el.number != null) {
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
