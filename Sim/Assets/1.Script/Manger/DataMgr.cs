using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;


public class DataMgr : MonoBehaviour {//변수 저장용
    public static void DataSave() {
        string path = pathForDocumentsFile("custom.sav");
        SavedData file = new SavedData();
        file.DataSave();
        BinaryFormatter formatter = new BinaryFormatter();
        Stream stream = File.Open(path, FileMode.Create);
        formatter.Serialize(stream, file);
        stream.Close();
        file = null;
    }
    public static void DataLoad() {
        string path = pathForDocumentsFile("custom.sav");
        if (File.Exists(path)) {
            Stream stream = File.Open(path, FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();
            SavedData file = (SavedData)formatter.Deserialize(stream);
            file.DataLoad();
            stream.Close();
            file = null;
        }
    }
    public static string pathForDocumentsFile(string filename) {//경로 검색
        if (Application.platform == RuntimePlatform.IPhonePlayer) {
            string path = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
            path = path.Substring(0, path.LastIndexOf('/'));
            return Path.Combine(Path.Combine(path, "Documents"), filename);
        }
        else if (Application.platform == RuntimePlatform.Android) {
            string path = Application.persistentDataPath;
            path = path.Substring(0, path.LastIndexOf('/'));
            return Path.Combine(path, filename);
        }
        else {
            string path = Application.dataPath;
            path = path.Substring(0, path.LastIndexOf('/'));
            return Path.Combine(path, filename);
        }
    }
}
[System.Serializable]
public class SavedData {//저장 데이터
    //저장값
    public List<OrbitalElements> customList = new List<OrbitalElements>();
    public List<OrbitalElements> favoritList = new List<OrbitalElements>();

    public void DataSave() {//저장
        customList = SearchMgr.instance.customList;
        favoritList = SearchMgr.instance.favoritList;
    }
    public void DataLoad() {//불러오기
        SearchMgr.instance.customList = customList;
        SearchMgr.instance.favoritList = favoritList;
    }
}