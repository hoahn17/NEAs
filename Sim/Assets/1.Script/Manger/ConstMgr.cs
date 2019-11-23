using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ConstMgr {
    public static float ZoomRate = 0.1f;
    public static float LineScale = 2f;
    public static int SpeedLevel = 0;
    public static int ShowSpeed = 1;
    public static DateTime J2000 = new DateTime(2000, 1, 1);

    //상수
    public static DateTime MIN_DATE = new DateTime(1800, 1, 1);
    public static DateTime MAX_DATE = new DateTime(2200, 12, 31);
    public const int INTERVAL_COUNT = 1000;
    //태양계
    public static float[][] SOLAR_INFO = {
                    //J2000( 2000년 1월 1일 0시)
                    //a(장반경)    e(이심률)    i,           M0,         Ω,         ω,         mdm
        new float[] {0, 0, 0, 0, 0, 0,0},//태양
        new float[] {0.38709927f,  0.20563593f, 7.00497902f, 174.796f,    48.331f,    29.124f,    4.09237706f},//수성
        new float[] {0.72333566f,  0.00677672f, 3.39467605f, 50.115f,     76.680f,    54.884f,    1.60216874f},//금성
        new float[] {1.00000261f,  0.01671123f, 0.00005f,    358.617f,    348.73936f, 114.20783f, 0.98564736f},//지구
        new float[] {1.52371034f,  0.09339410f, 1.84969142f, 19.3870f,    49.559f,    286.502f,   0.52407109f},//화성
        new float[] {5.20288700f,  0.04838624f, 1.30439695f, 20.020f,     100.464f,   273.867f,   0.08312944f},//목성
        new float[] {9.53667594f,  0.05386179f, 2.48599187f, 317.020f,    113.665f,   339.392f,   0.03349791f},//토성
        new float[] {19.18916464f, 0.04725744f, 0.77263783f, 142.238600f, 74.006f,    96.998857f, 0.01176904f},//천왕성
        new float[] {30.06992276f, 0.00859048f, 1.77004347f, 256.228f,    131.784f,   276.336f,   0.006020076f},//해왕성
    };
    public static string[] SOLOR_NAME = { "Sun", "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };
    //천체 정보
    public static int[] SPEED_COEFFICIENT = { 1, 7, 30, 90, 180, 360 };
    public static float[][] ASTRO_INFO;
    public static OrbitalElements[] ASTROS;
    public static List<String> ASTRO_TYPE = new List<string>(new string[] { "All", "Atira", "Aten", "Apollo", "Amor", "etc" });
    public static float[] CUSTOM_MIN = { 0.1f,     0f,   0f,   0f,    0f,   0f, 0.0001f, 0f};
    public static float[] CUSTOM_MAX = { 400f,   0.99f, 180f, 360f,  360f, 360f, 5f, 100};
}
