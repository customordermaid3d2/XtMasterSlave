//#define COM3D2
#define IK159
#define COM3D2only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
//
using UnityEngine;
using UnityInjector.Attributes;

using PluginExt;
using CM3D2.XtMasterSlave.Plugin;
using VYMModule;

using ExtensionMethods;
using static ExtensionMethods.MyExtensions;
using static ExtensionMethods.ComExt;
using System.IO;
using UnityEngine.SceneManagement;

// コンパイル用コマンド　同梱のbat参照　※要VS2017(C#7.0)

namespace CM3D2.XtMasterSlave.Plugin
{
#if COM3D2
    [PluginFilter("COM3D2x64"), PluginFilter("COM3D2OHx64"),
    PluginName("COM3D2.XtMasterSlave.Plugin"), PluginVersion("0.0.5.1")]
#else
    [PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginFilter("CM3D2VRx64"),
    PluginFilter("CM3D2OHx64"), PluginFilter("CM3D2OHx86"), PluginFilter("CM3D2OHVRx64"),
    PluginName("CM3D2.XtMasterSlave.Plugin"), PluginVersion("0.0.5.0")]
#endif
    public class XtMasterSlave : ExPluginBase
    {
        #region 定数宣言
        public readonly static string PLUGIN_NAME = "XtMasterSlave";

        public readonly static string PLUGIN_VERSION = "0.0.5.1";
        private const int WINID_COFIG = 99101;
        const string PluginCfgFN = "XtMasterSlave.ini";
        const string YotogiCfgFN = "XtMasterSlave_Yotogi.ini";
        public const int MAX_PAGENUM = 5;

        //private readonly float WaitForInitialize = 5.0f;                             // 初期化遅延秒数
        //private readonly float WaitMaidInfoFind = 3.0f;                             // メイド検索開始遅延秒数

        // 処理を行わないシーン
        //  1   メイド選択
        //  6   メーカーロゴ
        //  7   メイド管理
        //  9   タイトル
        // 13   起動時警告
        // 16   日付
        // 17   タイトルに戻る
        // 19   スタッフロール
        // 23   デスクトップカスタム
        private readonly static int[] cIgnoreSceneLevel = new int[] { 1, 6, 7, 9, 13, 16, 17, 19, 23 };

        private readonly static string[] MaskItems =
        {
            "accashi", "bra",
            "chikubi",
            "head",
            "accanl",
            "accsenaka",
            "accha",
            "acchana",
            "acchat",
            "acchead",
            "accheso",
            "accKami_1_",
            "accKami_2_",
            "accKami_3_",
            "accKamiSubR",
            "accKamiSubL",
            "acckubi",
            "acckubiwa",
            "accMiMiR",
            "accMiMiL",
            "accNipR",
            "accNipL",
            "onepiece",
            "accshippo",
            "acctatoo",
            "accude",
            "accvag",
            "accxxx",
            "bra",
            "glove",
            "headset",
            "kousoku",
            "megane",
            "mizugi",
            "moza",
            "wear",
            "panz",
            "shoes",
            "skirt",
            "stkg",
            "hairaho",
            "hairf",
            "hairr",
            "hairp",
            "hairs",
            "hairt",
            "underhair",
        };

        private readonly static string[] HiddenNode =
        {
            "Spine",
            "Clavicle",
            "Mune",
            "Pelvis",
        };
        #endregion

        #region 変数宣言・シーン

        //VYMより
        private static int vSceneLevel = 0;
        public static bool SceneLevelEnable = false;
        static bool bIsYotogiScene = false;
        public static bool maidActive = false;
        //private Maid maid;

        static bool bIsVymPlg = false;
        //static bool bVoicePlaying = false;
        //static bool bHitChkResized = false; 

        //回想モード
        static bool vIsKaisouScene = false;
        static bool vacationEnabled = false;

        //脱衣設定
        public readonly static Dictionary<string, TBody.SlotID[]> dicMaskItems = new Dictionary<string, TBody.SlotID[]>
        {
            {"トップス/ワンピ", new TBody.SlotID[] { TBody.SlotID.wear,TBody.SlotID.onepiece } },
            {"水着", new TBody.SlotID[] { TBody.SlotID.mizugi } },
            {"スカート", new TBody.SlotID[] { TBody.SlotID.skirt } },
            {"ストッキング", new TBody.SlotID[] { TBody.SlotID.stkg } },
            {"ブラ", new TBody.SlotID[] { TBody.SlotID.bra } },
            {"パンツ", new TBody.SlotID[] { TBody.SlotID.panz } },
            {"グローブ", new TBody.SlotID[] { TBody.SlotID.glove } },
            {"シューズ", new TBody.SlotID[] { TBody.SlotID.shoes } },
            {"帽子/ヘッドセット", new TBody.SlotID[] { TBody.SlotID.accHat, TBody.SlotID.headset } },
            {"メガネ", new TBody.SlotID[] { TBody.SlotID.megane } },
            {"チョーカー", new TBody.SlotID[] { TBody.SlotID.accKubi } },
            {"首輪", new TBody.SlotID[] { TBody.SlotID.accKubiwa } },
            {"アクセ鼻", new TBody.SlotID[] { TBody.SlotID.accHana } },
            {"アクセ耳", new TBody.SlotID[] { TBody.SlotID.accMiMiL, TBody.SlotID.accMiMiR } },
            {"アクセ腕", new TBody.SlotID[] { TBody.SlotID.accUde } },
            {"アクセ足", new TBody.SlotID[] { TBody.SlotID.accAshi } },
            {"へそ", new TBody.SlotID[] { TBody.SlotID.accHeso } },
            {"前穴", new TBody.SlotID[] { TBody.SlotID.accXXX } },
            {"背中", new TBody.SlotID[] { TBody.SlotID.accSenaka } },
            {"しっぽ", new TBody.SlotID[] { TBody.SlotID.accShippo } },
        };

        #endregion

        #region ギズモ関係

        /// <summary>
        /// 公式ギズモを使いやすくするためのクラス
        /// </summary>
        class OhMyGizmo : GizmoRender
        {
            public GameObject gameObject_ = null;

            public bool eDragUndo = false;

            FieldInfo _fi = null;
            FieldInfo _fi_beSelectedType = null;
            bool _isdrag_bk = false;

            //差分計算用
            public Vector3 _backup_pos = Vector3.zero;
            public Quaternion _backup_rot = Quaternion.identity;

            public Vector3 _backup_pos_u1 = Vector3.zero;
            public Quaternion _backup_rot_u1 = Quaternion.identity;

            public Vector3 _backup_posLocal_u1 = Vector3.zero;
            public Quaternion _backup_rotLocal_u1 = Quaternion.identity;


            //ギズモ位置
            public Vector3 position
            {
                get { return this.transform.position; }
                set { this.transform.position = value; }
            }

            //ギズモ回転
            public Quaternion rotation
            {
                get { return this.transform.rotation; }
                set { this.transform.rotation = value; }
            }

            //差分計算用
            public void BkupPos() { _backup_pos = this.position; }
            public void BkupRot() { _backup_rot = this.rotation; }
            public void BkupPosAndRot() { this.BkupPos(); this.BkupRot(); }

            //public void BkupPosAndRotLocal() { _backup_pos = this.transform.localPosition; _backup_rot = this.transform.localRotation; }

            public void BkupPosAndRotU1() { _backup_pos_u1 = this.transform.position; _backup_rot_u1 = this.transform.rotation; }
            public void BkupPosAndRotLocalU1() { _backup_posLocal_u1 = this.transform.localPosition; _backup_rotLocal_u1 = this.transform.localRotation; }

            //差分計算用
            public Vector3 _predrag_pos = Vector3.zero;
            public Quaternion _predrag_rot = Quaternion.identity;
            public bool _predrag_state = false;
            public override void Update()
            {
                bool dragnow = this.isDrag;

                if (dragnow != _predrag_state)
                {
                    if (eDragUndo && !_predrag_state)
                    {
                        _predrag_pos = this.position;
                        _predrag_rot = this.rotation;
                    }
                    _predrag_state = dragnow;
                }
                if (eDragUndo && dragnow)
                {
                    if (this.isDragUndo)
                    {
                        //右クリックかESCでポジション復帰
                        this.position = _predrag_pos;
                        this.rotation = _predrag_rot;
                    }
                }

                base.Update();
            }

            public bool isDragUndo
            {
                get
                {
                    if (!_predrag_state)
                        return false;
                    return Input.GetMouseButton(1) || Input.GetKey(KeyCode.Escape);
                }
            }

            //ドラッグ判定、複数ギズモを表示中でも個別判定できるようにした
            public bool isDrag
            {
                get
                {
                    if (!this.Visible)
                        return false;

                    if (_fi != null && _fi_beSelectedType != null)
                    {
                        object obj = _fi.GetValue(this);
                        if (obj is bool && (bool)obj)
                        {
                            //ギズモをドラッグ中（どれかは不明）
                            /*上手く行かないので没
                            RaycastHit hit = new RaycastHit();
                            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                            if (Physics.Raycast(ray, out hit))
                            {
                                debugPrintConsole("hit: name:" + hit.collider.gameObject.name + " tag:" + hit.collider.gameObject.tag);
                                if (hit.collider.gameObject.name == this.name)
                                {
                                    
                                }
                            }*/
                            object obj2 = _fi_beSelectedType.GetValue(this);
                            if (obj2 is Enum && (int)obj2 != 0)
                            {
                                //GizmoRender.MOVETYPE.NONE以外ならこのギズモのどこかをドラッグ中
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }

            public void ClearSelectedType()
            {
                _fi_beSelectedType.SetValue(this, 0);
            }

            //ドラッグエンド判定用（変化を見るだけなので毎フレーム呼び出す必要あり）
            public bool isDragEnd
            {
                get
                {
                    bool drag = this.isDrag;
                    if (drag != _isdrag_bk)
                    {
                        _isdrag_bk = drag;
                        if (drag == false)
                            return true;
                    }
                    return false;
                }
            }

            public void DragBkup()
            {
                {
                    _isdrag_bk = this.isDrag;
                }
            }

            public OhMyGizmo()
            {
                //beSelectedType
                if (_fi_beSelectedType == null)
                    _fi_beSelectedType = typeof(GizmoRender).GetField("beSelectedType", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (_fi == null)
                    _fi = typeof(GizmoRender).GetField("is_drag_", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            }
            /*
            private Vector3 lastpos_orobj_ = Vector3.zero;
            private Quaternion lastrot_orobj_ = Quaternion.identity;
            public override void OnRenderObject()
            {
                if (_predrag_state && Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                {
                    //shift+ドラッグで微調整
                    Vector3 dv = this.position - lastpos_orobj_;
                    dv *= 0.1f;
                    this.position = lastpos_orobj_ + dv;

                    this.rotation = Quaternion.Slerp(lastrot_orobj_, this.rotation, 0.1f);
                }
                lastpos_orobj_ = this.position;
                lastrot_orobj_ = this.rotation;
                base.OnRenderObject();
            }*/

            //ギズモ作成補助
            static public List<GameObject> _gameObjects_ = new List<GameObject>();
            static public OhMyGizmo AddGizmo(Transform parent_tr, string gizmo_name)
            {
                GameObject go = new GameObject();
                _gameObjects_.Add(go);
                go.transform.SetParent(parent_tr, true);
                go.name = gizmo_name;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;

                GameObject go2 = new GameObject();
                go2.transform.SetParent(go.transform, true);
                go2.transform.localPosition = Vector3.zero;
                go2.transform.localRotation = Quaternion.identity;

                OhMyGizmo mg = go2.AddComponent<OhMyGizmo>();
                mg.gameObject_ = go2;
                //debugPrintConsole("test: " + (go != mg.gameObject));
                //mg.transform.SetParent(go.transform, false);
                mg.transform.parent = go2.transform;

                mg.name = gizmo_name + "_GR";
                return mg;
            }
        }
        static OhMyGizmo _Gizmo;
        //static OhMyGizmo _GizmoRot;
        static OhMyGizmo _Gizmo_HandR;
        static OhMyGizmo _Gizmo_HandL;

#if DEBUG
        static OhMyGizmo _Gizmo_dbg;
#endif

        Vector3 _gizmo_predrag_atcpos = Vector3.zero;
        Vector3 _gizmo_predrag_atcrot = Vector3.zero;

        static void GizmoVisible(bool visible)
        {
            _Gizmo.Visible = visible;
        }

        static void GizmoHsVisible(bool visible)
        {
            _Gizmo_HandR.Visible = visible;
            _Gizmo_HandL.Visible = visible;
        }

        static bool GetGizmoHsVisible()
        {
            if (_Gizmo_HandR.Visible || _Gizmo_HandL.Visible)
                return true;

            return false;
        }

        const float GIZMO_SLOWRATE = 0.2f;

        #endregion

        #region 設定ファイル定義
        //　設定クラス（Iniファイルで読み書きしたい変数はここに格納する）
        public class PluginConfig
        {
            // 一般設定
            public KeyCode hotkey_GUI = KeyCode.M;        //　プラグインの有効無効の切替キー
            public InputEx.ModifierKey hotkey_GUI_Modifier = InputEx.ModifierKey.Alt;        //　プラグインの有効無効の切替キー

            public float Scale_Min = 0.3f;
            public float Scale_Max = 2.0f;

            public bool boNameSelectAndLoad = true;
            public bool DlgShow_Hint001 = true;

#if DEBUG
            public bool boMasterMotionLog = true;
#else
            public bool boMasterMotionLog = false;
#endif
            //無効シーン
            public int[] IgnoreSceneLevel = cIgnoreSceneLevel;

            public bool hideHitScaleDef = true;
            public bool doHitScaleDef = false;
            public float[] HitScaleDef = new float[(int)HitCheckTgt.Bip01 + 1] {
                1f, 1f, 1f, 1f, 1f,
            };

            public bool AdjustBoneHitHeightY = true;

            // v0030
            public string[][] customNames = new string[][]
            {
                new string[] { VymModule.VoiceMode.カスタム1.ToString(), "カスタム1" },
                new string[] { VymModule.VoiceMode.カスタム2.ToString(), "カスタム2" },
                new string[] { VymModule.VoiceMode.カスタム3.ToString(), "カスタム3" },
                new string[] { VymModule.VoiceMode.カスタム4.ToString(), "カスタム4" },
            };

            //public bool HandIKsp_UseVechand = false; //IKフィードバック有無
        }
        public static PluginConfig cfg = new PluginConfig();

        public enum ATgtChar
        {
            None = 0,
            Self = 1,
            Master = 2,
            Maid0 = 3,
            Maid1 = 4,
            Maid2 = 5,
            Maid3 = 6,
            Maid4 = 7, // とりあえずスロット数と同じ5人まで
        }

        public bool ATgtStr_IsNullOrEmpty(string s)
        {
            if (string.IsNullOrEmpty(s))
                return true;

            if (s == "無し")
                return true;

            return false;
        }


        public class MsLinkConfig
        {
            //位置合わせ
            public bool doStackSlave = true;
            public bool doStackSlave_Pelvis = false;
            public bool doStackSlave_CliCnk = true;
            public float[] v3StackOffsetFA = new float[] { 0, 0, 0 };
            public float[] v3StackOffsetRotFA = new float[] { 0, 0, 0 };
            public float[] v3HandROffsetFA = new float[] { 0, 0, 0 };
            public float[] v3HandLOffsetFA = new float[] { 0, 0, 0 };
            public float[] v3HandROffsetRotFA = new float[] { 0, 0, 0 };
            public float[] v3HandLOffsetRotFA = new float[] { 0, 0, 0 };

            public float[][] customHandRfa = new float[2][] { new float[] { 0, 0, 0 }, new float[] { 0, 0, 0 } };
            public float[][] customHandLfa = new float[2][] { new float[] { 0, 0, 0 }, new float[] { 0, 0, 0 } };

            //位置調整のみ
            public bool doStackSlave_PosSyncMode = false;
            public bool doStackSlave_PosSyncModeV2 = false; //基準を原点に
            public bool doStackSlave_PosSyncModeSp = false; //アタッチ先を任意ボーン
            public string doStackSlave_PosSyncModeSp_TgtBone = string.Empty; //アタッチ先を任意ボーン

            //IK
            public bool doCopyIKTarget = true;
            public bool doIKTargetMHand = true;

            public bool doIKTargetMHandSpCustom = false;    // アタッチ先変更
            public bool doIKTargetMHandSpCustom_v2 = true;   // v5.0 アタッチ先変更2（角度指定バージョン2）
            public bool chkIkSpCustomR_v2()
            {
                return doIKTargetMHandSpCustom && doIKTargetMHandSpCustom_v2 && doIKTargetMHandSpR_TgtChar != ATgtChar.None;
            }
            public bool chkIkSpCustomL_v2()
            {
                return doIKTargetMHandSpCustom && doIKTargetMHandSpCustom_v2 && doIKTargetMHandSpL_TgtChar != ATgtChar.None;
            }

            public ATgtChar doIKTargetMHandSpR_TgtChar = ATgtChar.None;
            public ATgtChar doIKTargetMHandSpL_TgtChar = ATgtChar.None;
            public string doIKTargetMHandSpR_TgtBone = string.Empty;
            public string doIKTargetMHandSpL_TgtBone = string.Empty;
            public bool doIKTargetMHandSpCustomAltRotR = true; // アタッチポイントとの相対角度 v5.0でtrueに
            public bool doIKTargetMHandSpCustomAltRotL = true; // アタッチポイントとの相対角度 v5.0でtrueに
            // v0026
            public bool doIK159NewPointToDef = false; //v5.0ギリギリ使えるレベルに //v4.0β※com1.17で不具合が出る true; // 159以降の新IKをデフォルトに使うか
            public bool doIK159RotateToHands = true; // 159以降のRotateIKを両手にアタッチに使うか
            // v5.0
            public bool doFinalIKShoulderMove = false; // 肩位置への影響
            public bool doFinalIKThighMove = false; // もも位置への影響
            public float fFinalIKLegWeight = 1f; // 足への影響

            // 手のブレンド v0030
            public bool doBlendHandR = false;
            public bool doBlendHandL = false;
            public float fBlendHandROpen = 0f;
            public float fBlendHandLOpen = 0f;
            public float fBlendHandRGrip = 0f;
            public float fBlendHandLGrip = 0f;
            public float fBlendHandR = 0f;
            public float fBlendHandL = 0f;
            public bool doAnimeHandR = false;
            public bool doAnimeHandL = false;
            public float fAnimeHandRMove = 0f;
            public float fAnimeHandLMove = 0f;
            public float fAnimeHandRSpeed = 0f;
            public float fAnimeHandLSpeed = 0f;

            // 絶頂痙攣β v0030
            public bool doZecchoKeiren = true;
            public float fZecchoKeirenParam = 0.06f;

            //表情同期
            public bool doFaceSync = false;
            public bool doVoiceAndFacePlay = true;
#if COM3D2
            public bool doVoiceDisabled = false; // 新性格に未対応 -> v5.0で変更
#else
            public bool doVoiceDisabled = false;
#endif

            //VYM連動設定
            public bool doVoiceAndFacePlayOnVYM = true;
            public bool doVoiceAndFacePlayOnVYM_Zeccho = false; //絶頂同期

            //マニュアルモード
            public bool doManualVfPlay = false;
            //マニュアルモード用
            public int manuVf_iExcite = 0;
            public int manuVf_mState = 10;
            public int manuVf_mOrgcmb = -1;

            //夜伽モードでSlaveを維持
            public bool doKeepSlaveYotogi = true;

            //ヒットチェックスケール
            public float Scale_HitCheckEffect = 1f;
            public bool Scale_HitCheckDetail = true;
            public float Scale_HitCheckDetail_Momo = 1f;
            public float Scale_HitCheckDetail_Thigh = 1f;
            public float Scale_HitCheckDetail_Hip = 1f;
            public float Scale_HitCheckDetail_Spine = 1f;
            public float Scale_HitCheckDetail_Bip01 = 1f;

            public bool Adjust_doHitHeightYOffset = false;
            public float Adjust_HitHeightYOffset = 0f;

            public ref float GetHitDetail(HitCheckTgt h)
            {
                switch (h)
                {
                    case HitCheckTgt.Hip:
                        return ref this.Scale_HitCheckDetail_Hip;
                    case HitCheckTgt.Momo:
                        return ref this.Scale_HitCheckDetail_Momo;
                    case HitCheckTgt.Spine:
                        return ref this.Scale_HitCheckDetail_Spine;
                    case HitCheckTgt.Thigh:
                        return ref this.Scale_HitCheckDetail_Thigh;
                    default:
                        return ref this.Scale_HitCheckDetail_Bip01;
                }
            }
        }
        public static MsLinkConfig[] cfgs = new MsLinkConfig[MAX_PAGENUM] { new MsLinkConfig(), new MsLinkConfig(), new MsLinkConfig(), new MsLinkConfig(), new MsLinkConfig() };

        public class PosRot
        {
            public Vector3 pos;
            public Vector3 rot;

            public PosRot()
            {
                pos = Vector3.zero;
                rot = Vector3.zero;
            }

            public PosRot(Vector3 position, Quaternion rotation)
            {
                pos = position;
                rot = rotation.eulerAngles;
            }

            public PosRot(float[][] faa) : this()
            {
                if (faa.Length == 2)
                {
                    pos = faTov3(faa[0]);
                    rot = faTov3(faa[1]);
                }
            }

            public float[][] ToFloatArray()
            {
                float[][] faa = new float[2][];
                faa[0] = v3Tofa(pos);
                faa[1] = v3Tofa(rot);
                return faa;
            }

            public float[] PosFA()
            {
                return v3Tofa(pos);
            }

            public float[] RotFA()
            {
                return v3Tofa(rot);
            }
        }

        public class v3OffsetsV2
        {
            v3Offsets v3o;
            public bool isV2 { get; private set; }

            public v3OffsetsV2(v3Offsets v3o, MsLinkConfig cfg)
            {
                this.v3o = v3o;
                this.isV2 = cfg.doIKTargetMHandSpCustom_v2;
            }

            public v3OffsetsV2(v3Offsets v3o, bool useV2)
            {
                this.v3o = v3o;
                this.isV2 = useV2;
            }

            public Vector3 v3StackOffset { get { return v3o.v3StackOffset; } set { v3o.v3StackOffset = value; } }
            public Vector3 v3StackOffsetRot { get { return v3o.v3StackOffsetRot; } set { v3o.v3StackOffsetRot = value; } }

            public Vector3 v3HandROffset
            {
                get { return !isV2 ? v3o.v3HandROffset : v3o.customHandR.pos; }
                set {
                    if (!isV2) v3o.v3HandROffset = value;
                    else v3o.customHandR.pos = value;
                }
            }

            public Vector3 v3HandLOffset
            {
                get { return !isV2 ? v3o.v3HandLOffset : v3o.customHandL.pos; }
                set {
                    if (!isV2) v3o.v3HandLOffset = value;
                    else v3o.customHandL.pos = value;
                }
            }

            public Vector3 v3HandROffsetRot
            {
                get { return !isV2 ? v3o.v3HandROffsetRot : v3o.customHandR.rot; }
                set {
                    if (!isV2) v3o.v3HandROffsetRot = value;
                    else v3o.customHandR.rot = value;
                }
            }

            public Vector3 v3HandLOffsetRot
            {
                get { return !isV2 ? v3o.v3HandLOffsetRot : v3o.customHandL.rot; }
                set {
                    if (!isV2) v3o.v3HandLOffsetRot = value;
                    else v3o.customHandL.rot = value;
                }
            }
        }

        public class v3Offsets
        {
            public Vector3 v3StackOffset = Vector3.zero;
            public Vector3 v3StackOffsetRot = Vector3.zero;
            public Vector3 v3HandROffset = Vector3.zero;
            public Vector3 v3HandLOffset = Vector3.zero;

            public Vector3 v3HandROffsetRot = Vector3.zero;
            public Vector3 v3HandLOffsetRot = Vector3.zero;

            public PosRot customHandR = new PosRot();
            public PosRot customHandL = new PosRot();
            /*
            public ref Vector3 refCustomRHpos(MsLinkConfig cfg)
            {
                if (cfg.doIKTargetMHandSpCustom_v2)
                {
                    return ref customHandR.pos; 
                }
                return ref v3HandROffset;
            }

            public ref Vector3 refCustomLHpos(MsLinkConfig cfg)
            {
                if (cfg.doIKTargetMHandSpCustom_v2)
                {
                    return ref customHandL.pos;
                }
                return ref v3HandLOffset;
            }

            public ref Vector3 refCustomRHrot(MsLinkConfig cfg)
            {
                if (cfg.doIKTargetMHandSpCustom_v2)
                {
                    return ref customHandR.rot;
                }
                return ref v3HandROffsetRot;
            }

            public ref Vector3 refCustomLHrot(MsLinkConfig cfg)
            {
                if (cfg.doIKTargetMHandSpCustom_v2)
                {
                    return ref customHandL.rot;
                }
                return ref v3HandLOffsetRot;
            }
            */
#if DEBUG
            private Vector3 old = Vector3.zero;
#endif
#if test
            public Vector3 v3StackOffset2Bip(Maid slave, bool trans)
            {
                //return Quaternion.Inverse(Quaternion.Euler(0, -90, -90)) * v3StackOffset;

                if (!trans)
                    return v3StackOffset;

                var bp = _Gizmo.transform.position;
                var br = _Gizmo.transform.rotation;

                var sTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);
                _Gizmo.transform.position = sTr.position;
                _Gizmo.transform.rotation = sTr.rotation;
                _Gizmo.transform.rotation *= Quaternion.Euler(0, -90, -90);
                //var gv = _Gizmo.transform.TransformDirection(_Gizmo.transform.localRotation * v3StackOffset);

                //_Gizmo.transform.localPosition += _Gizmo.transform.localRotation * v3StackOffset;
                _Gizmo.transform.position += _Gizmo.transform.TransformDirection(v3StackOffset);
#if false//DEBUG
                if (old != _Gizmo.transform.position)
                {
                    old = _Gizmo.transform.position;
                    debugPrintConsole("g_ pos: " + _Gizmo.transform.position + " dv: " + v3StackOffset + " lr: " + _Gizmo.transform.localRotation.eulerAngles.ToString());
                }
#endif
                var dv = _Gizmo.transform.position - sTr.position;
                _Gizmo.transform.position = bp;
                _Gizmo.transform.rotation = br;

                //return sTr.InverseTransformDirection(dv);
                return dv;
            }
#endif
        }
        public static v3Offsets[] v3ofs = new v3Offsets[MAX_PAGENUM] { new v3Offsets(), new v3Offsets(), new v3Offsets(), new v3Offsets(), new v3Offsets() };

        public class YotogiConfig
        {
            //絶頂判定用
            public string[] sMensZeccyouMotion = { /*"_zeccyou_m_once_",*/ "_shasei_naka_m_once_", "_shasei_soto_m_once_", "_shasei_kao_", "_shasei_kuti_", "2ana_shasei_.*_m2" }; /*"_seikantaizeccyou_m_once_",*/
            public string[] sMensZeccyouAfterMotion = { "_zeccyougo_m", "_shaseigo_naka_m", "_shaseigo_soto_m", "shaseigo" };

            public string[] sMensSexMotion = new string[] { "seijyoui_", "kouhaii_", "sokui_", "kijyoui_", "haimen_", "ekiben_", "sex_", "manguri_", "ritui_",
                "seijyouia_", "kouhaiia_", "sokuia_", "kijyouia_", "haimena_", "ekibena_", "sexa_", "manguria_", "rituia_", "ran3p_2ana", "ran4p_", "taimenkijyoui" };

            public string[] sMensKissMotion = new string[] { "sixnine", "^(?!.*fera).*kiss.*(?!.*fera)", "kunni", };

            public string[] sMensUkeMotion = new string[] { "fera", "_ir_", "siriname", "tikubiname", "tekoki", "paizuri", "kijyoui", "ran3p_housi", "harem_housi", "mp_arai_", "asikoki_" }; //_ir_イラマ

            public string[] sMensTaikiMotion = new string[] { "_taiki", "tekoki_nade" };

            public string[] sMensKousokuMotion = new string[] { "muri_3p_.*_m2", };

            public string[] sMensSemeMotion = new string[] { "_aibu_", };

            //モーションカテゴリ別レベルシフター
            public Dictionary<string, int> MotionEffect_ExciteLevelSift = new Dictionary<string, int>
            {
                {XtMasterSlave.AnimeState.State.none.ToString(), -2 },
                {XtMasterSlave.AnimeState.State.taiki.ToString(), -3 },
                {XtMasterSlave.AnimeState.State.kiss.ToString(), 0 },
                {XtMasterSlave.AnimeState.State.uke.ToString(), 0 },
                {XtMasterSlave.AnimeState.State.sex.ToString(), -1 },
                {XtMasterSlave.AnimeState.State.zeccho.ToString(), 0 },
                {XtMasterSlave.AnimeState.State.kousoku.ToString(), -3 },
                {XtMasterSlave.AnimeState.State.seme.ToString(), -2 },
            };

            //モーションカテゴリ別状態スイッチ 0で変更なし=興奮度によって切換え
            public Dictionary<string, int> MotionEffect_StateMajorSwitch = new Dictionary<string, int>
            {
                {XtMasterSlave.AnimeState.State.none.ToString(), 40 },
                {XtMasterSlave.AnimeState.State.taiki.ToString(), 10 },
                {XtMasterSlave.AnimeState.State.kiss.ToString(), 0 },
                {XtMasterSlave.AnimeState.State.uke.ToString(), 30 },
                {XtMasterSlave.AnimeState.State.sex.ToString(), 0 },
                {XtMasterSlave.AnimeState.State.zeccho.ToString(), 30 },
                {XtMasterSlave.AnimeState.State.yoin.ToString(), 40 },
                {XtMasterSlave.AnimeState.State.kousoku.ToString(), 10 },
                {XtMasterSlave.AnimeState.State.seme.ToString(), 40 },
            };
        }
        public static YotogiConfig ycfg = new YotogiConfig();

        #endregion

        #region 汎用クラス＆メソッド

        static Vector3 v3limit(Vector3 v, float limit)
        {
            v.x = v.x > limit ? limit : v.x;
            v.y = v.y > limit ? limit : v.y;
            v.z = v.z > limit ? limit : v.z;

            v.x = v.x < -limit ? -limit : v.x;
            v.y = v.y < -limit ? -limit : v.y;
            v.z = v.z < -limit ? -limit : v.z;
            return v;
        }

        static int obj2int(object obj)
        {
            try
            {
                if (obj is int) return (int)obj;
            }
            catch { }
            return -1;
        }


        // TBody
        //public void ManColorUpdate(Maid man, int manAlpha)
        public static void SetManAlpha(Maid man, float manAlpha)
        {
            if (man.boMAN)
            {
                for (int i = 0; i < man.body0.goSlot.Count; i++)
                {
                    TBodySkin tBodySkin = man.body0.goSlot[i];
                    if (tBodySkin != null)
                    {
                        ManColorUpdate(tBodySkin, manAlpha);
                    }
                }
            }
        }

        static FieldInfo fiml_ = typeof(TBodySkin).GetField("m_listManAlphaMat", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        // TBodySkin
        public static void ManColorUpdate(TBodySkin tbs, float manAlpha)
        {
            var m_listManAlphaMat = fiml_.GetValue(tbs) as List<Material>;
            if (m_listManAlphaMat == null)
            {
                Console.WriteLine("m_listManAlphaMat取得エラー");
                return;
            }

            for (int i = 0; i < m_listManAlphaMat.Count; i++)
            {
                Material material = m_listManAlphaMat[i];
                //material.SetFloat("_FloatValue2", (float)GameMain.Instance.CMSystem.ManAlpha / 100f);
                //material.SetFloat("_FloatValue2", (float)manAlpha / 100f);
                material.SetFloat("_FloatValue2", manAlpha / 100f);
                //material.SetColor("_Color", tbs.body.maid.ManColor);
            }
        }

        public static float GetManAlpha(Maid man)
        {
            if (man.boMAN)
            {
                for (int i = 0; i < man.body0.goSlot.Count; i++)
                {
                    TBodySkin tBodySkin = man.body0.goSlot[i];
                    if (tBodySkin != null)
                    {
                        float a = GetManAlpha(tBodySkin);
                        if (a >= 0)
                            return a;
                    }
                }
            }
            return GameMain.Instance.CMSystem.ManAlpha;
        }
        public static float GetManAlpha(TBodySkin tbs)
        {
            List<Material> m_listManAlphaMat = fiml_.GetValue(tbs) as List<Material>;
            if (m_listManAlphaMat == null)
            {
                Console.WriteLine("m_listManAlphaMat取得エラー");
                return -1;
            }

            for (int j = 0; j < m_listManAlphaMat.Count; j++)
            {
                Material material = m_listManAlphaMat[j];
                //material.SetFloat("_FloatValue2", (float)GameMain.Instance.CMSystem.ManAlpha / 100f);
                //material.SetFloat("_FloatValue2", (float)manAlpha / 100f);
                //material.SetColor("_Color", tbs.body.maid.ManColor);
                float a = (material.GetFloat("_FloatValue2") * 100f);
                if (a < 0)
                    continue;

                return a;
            }
            return -1;
        }

        // TBodyより改造
        //static Dictionary<Maid, bool[]> boVisibleBkup = new Dictionary<Maid, bool[]>();
        static HashSet<Maid> maskedMaids = new HashSet<Maid>();
        static void MaskItemsAll(Maid m)
        {
            if (m.boMAN || m.IsBusy)
                return;

            //足首スロットで操作するために一時アイテムセット
            m.SetProp(MPN.accashi, "_I_accashi_del.menu", "_I_accashi_del.menu".ToLower().GetHashCode(), true);
            m.AllProcProp();

            maskedMaids.Add(m);
            //boVisibleBkup[m] = new bool[m.body0.goSlot.Count+]
            for (int i = 0; i < m.body0.goSlot.Count; i++)
            {
                m.body0.goSlot[i].boVisible = false;
            }
            m.body0.boVisible_NIP = false;
            m.body0.boVisible_HESO = false;
            m.body0.boVisible_XXX = false;
            m.body0.boVisible_BRA = false;
            m.body0.boVisible_PANZU = false;
            m.body0.boVisible_SKIRT = false;
            m.body0.boVisible_WEAR = false;
            m.body0.boMizugi_panz = false;
            //m.body0.slotno_accXXX = (int)TBody.hashSlotName["accXXX"];

            //消去ノード
            foreach (var s in HiddenNode)
                m.body0.goSlot[(int)TBody.SlotID.accAshi].SetVisibleFlag(false, s, m.body0.goSlot[0].obj_tr, false);

            m.body0.FixVisibleFlag(false);
            m.AllProcProp();
        }
        static void ResetMaskItemsAll(Maid m)
        {
            if (m.boMAN || m.IsBusy)
                return;

            MaidProp mp = m.GetProp(MPN.accashi);
            if (mp.boTempDut)
            {
                mp.boDut = true;
                mp.boTempDut = false;
            }
            m.ResetProp(MPN.accashi, true);

            if (maskedMaids.Contains(m))
                maskedMaids.Remove(m);
            m.body0.FixMaskFlag();

            m.body0.FixVisibleFlag(false);
            m.AllProcProp();
        }

        public static void SetStateMaskItemsAll(Maid m, bool mask)
        {
            if (mask)
            {
                MaskItemsAll(m);
            }
            else
            {
                ResetMaskItemsAll(m);
            }
        }

        public static bool GetStateMaskItemsAll(Maid m)
        {
            return maskedMaids.Contains(m);
        }

        // ヒットチェックのスケーリングが必要かの判定（本体1.54未満？）
        static bool NeedHitScaleCalc = typeof(TBodySkin).GetField("m_trMaid", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null;
        public static void UpdateHitScale(Maid maid, float bodyScale, float scale)
        {
            if (!NeedHitScaleCalc)
            {
                bodyScale = 1f; //公式側対応済み
            }

            UpdateHitScale_(maid, scale * bodyScale);
        }

        static HashSet<Maid> HitScaleChangedMaids = new HashSet<Maid>();
        public static void UpdateHitScale_(Maid maid, float scale)
        {
            TBody body = maid.body0;
            if (scale != 1)
            {
                HitScaleChangedMaids.Add(maid);
            }
            else //if (HitScaleChangedMaids.Contains(maid))
            {
                HitScaleChangedMaids.Remove(maid);
            }

            for (int n = 0; n < body.goSlot.Count; n++)
            {
                var s = body.goSlot[n];
                if (s != null)
                {
                    if (s.bonehair != null && s.bonehair.bodyhit != null)
                    {
                        PreUpdateSc(s.bonehair.bodyhit, scale);
                    }
                }
            }
            UpdateHitScale_boneh2(maid, scale, scale);
            UpdateHitScale_boneh3(maid, scale, scale, scale);
        }

        // CM3D2.hitCheckResize.Managed.hitCheckResizeManagedより改造
        public static void PreUpdateSc(TBodyHit tbh, float scale)
        {
            for (int i = 0; i < tbh.spherelist.Count; i++)
            {
                //tbh.spherelist[i].wv_old = tbh.spherelist[i].wv;
                //Vector3 wv = tbh.spherelist[i].t.TransformPoint(tbh.spherelist[i].vs);
                //tbh.spherelist[i].wv = wv;
                Transform t = tbh.spherelist[i].t;
                //float num = Mathf.Pow(t.lossyScale.x * t.lossyScale.y * t.lossyScale.z, 0.333333343f);
                /*float num = (t.lossyScale.x + t.lossyScale.y + t.lossyScale.z) / 3f;
                bool flag = tbh.spherelist[i].pname == "Bip01 Spine0a";
                if (flag)
                {
                    tbh.spherelist[i].len = tbh.spherelist[i].len_ * 0.1f;
                }
                else
                {
                    tbh.spherelist[i].len = tbh.spherelist[i].len_ * num;
                }*/
#if DEBUG
                if (UnityEngine.Input.GetKey(KeyCode.H) && true)
                {
                    Console.WriteLine(tbh.spherelist[i].pname + " " + tbh.spherelist[i].len / tbh.spherelist[i].len_);
                }
#endif
                tbh.spherelist[i].len = tbh.spherelist[i].len_ * scale;
                tbh.spherelist[i].lenxlen = tbh.spherelist[i].len * tbh.spherelist[i].len;
#if COM3D2
                if (tbh.spherelist[i].tPtr != null)
                    tbh.spherelist[i].tPtr.localScale = new Vector3(tbh.spherelist[i].len, tbh.spherelist[i].tPtr.localScale.y, tbh.spherelist[i].tPtr.localScale.z);
#endif

#if false
                if (UnityEngine.Input.GetKey(KeyCode.H))
                {
                    Console.WriteLine(tbh.spherelist[i].pname);
                }
#endif
            }
        }

        /*デフォルト
            momotwist2_R 1
            momotwist2_L 1
            momotwist2_R 1
            momotwist2_L 1
            momotwist2_R 1
            momotwist2_L 1
            Bip01 R Thigh 1
            Bip01 L Thigh 1
            Bip01 R Thigh 1
            Bip01 L Thigh 1
            Hip_R 1
            Hip_L 1
            Bip01 Spine0a 1
            Bip01 1
         */
        public enum HitCheckTgt
        {
            Momo = 0,
            Thigh = 1,
            Hip = 2,
            Spine = 3,
            Bip01 = 4, //最後にすること
        }
        public readonly static Dictionary<HitCheckTgt, string> HitCheckTgtStr = new Dictionary<HitCheckTgt, string>
        {
            { HitCheckTgt.Momo, "momotwist" },
            { HitCheckTgt.Thigh, "Thigh" },
            { HitCheckTgt.Hip, "Hip" },
            { HitCheckTgt.Spine, "Spine" },
            { HitCheckTgt.Bip01, "絶対ヒットさせない文字列はこちら" },
        };

        public static void UpdateHitScale(Maid maid, float bodyScale, MsLinkConfig mcfg)
        {
            if (!NeedHitScaleCalc)
            {
                bodyScale = 1f; //公式側対応済み
            }

            TBody body = maid.body0;
            if (!mcfg.Scale_HitCheckDetail)
            {
                UpdateHitScale_(maid, mcfg.Scale_HitCheckEffect * bodyScale);
                return;
            }

            Dictionary<HitCheckTgt, float> hcscs = new Dictionary<HitCheckTgt, float>
            {
                { HitCheckTgt.Momo, mcfg.Scale_HitCheckDetail_Momo * bodyScale },
                { HitCheckTgt.Thigh, mcfg.Scale_HitCheckDetail_Thigh * bodyScale },
                { HitCheckTgt.Hip, mcfg.Scale_HitCheckDetail_Hip * bodyScale },
                { HitCheckTgt.Spine, mcfg.Scale_HitCheckDetail_Spine * bodyScale },
                { HitCheckTgt.Bip01, mcfg.Scale_HitCheckDetail_Bip01 * bodyScale },
            };

            UpdateHitScale_2(maid, hcscs);
            UpdateHitScale_boneh2(maid, mcfg.Scale_HitCheckDetail_Bip01, mcfg.Scale_HitCheckDetail_Spine);
            UpdateHitScale_boneh3(maid, mcfg.Scale_HitCheckDetail_Thigh, mcfg.Scale_HitCheckDetail_Hip, mcfg.Scale_HitCheckDetail_Momo);
        }

        public static void UpdateHitScaleDef(Maid maid, float bodyScale, float[] scales, bool needUpdate)
        {
            float scale = 1f;
            foreach (var v in scales)
            {
                scale *= v;
            }
            if (scale == 1)
            {
                return; //不要
            }

            if (HitScaleChangedMaids.Contains(maid) && !needUpdate)
            {
                return; //変更済み
            }

            Dictionary<HitCheckTgt, float> hcscs = new Dictionary<HitCheckTgt, float>();
            foreach (var v in HitCheckTgtStr)
            {
                hcscs.Add(v.Key, scales[(int)v.Key]);
            }

            UpdateHitScale_2(maid, hcscs);
            UpdateHitScale_boneh2(maid, 1f, 1f);
            UpdateHitScale_boneh3(maid, 1f, 1f, 1f);
        }

        private static Dictionary<object, float> listScaleBkup = new Dictionary<object, float>();
        public static void UpdateHitScale_boneh2(Maid maid, float scale, float scaleMune)
        {
#if COM3D2
            TBody body = maid.body0;
            for (int n = 0; n < body.goSlot.Count; n++)
            {
                var s = body.goSlot[n];
                if (s != null)
                {
                    if (s.bonehair2 != null)
                    {
                        List<DynamicBoneColliderBase> list = s.bonehair2.GetType().GetField("m_listCollders", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(s.bonehair2) as List<DynamicBoneColliderBase>;
                        if (list != null)
                        {
                            foreach (var c in list)
                            {
                                if (c.TypeName == "dbc")
                                {
                                    //var dbc = (DynamicBoneCollider)c;
                                    var sc = c.transform.localScale;
                                    if (!listScaleBkup.ContainsKey(c))
                                    {
                                        //listScaleBkup.Add(c, dbc.m_Radius);
                                        listScaleBkup.Add(c, sc.x);
                                    }
                                    //dbc.m_Radius = listScaleBkup[dbc] * scale;
                                    c.transform.localScale = new Vector3(listScaleBkup[c] * scale, sc.y, sc.z);

                                    if (scale == 1)
                                    {
                                        listScaleBkup.Remove(c);
                                    }
                                }
                                else if (c.TypeName == "dbm")
                                {
                                    var dbm = (DynamicBoneMuneCollider)c;
                                    var sc = c.transform.localScale;
                                    if (!listScaleBkup.ContainsKey(c))
                                    {
                                        //listScaleBkup.Add(c, dbm.m_Radius);
                                        listScaleBkup.Add(c, sc.x);
                                    }
                                    //dbm.m_Radius = listScaleBkup[dbm] * scaleMune;
                                    c.transform.localScale = new Vector3(listScaleBkup[c] * scaleMune, sc.y, sc.z);

                                    if (scaleMune == 1)
                                    {
                                        listScaleBkup.Remove(c);
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif
        }

        public static void UpdateHitScale_boneh3(Maid maid, float scaleReg, float scaleHip, float scaleVag)
        {
#if COM3D2
            TBody body = maid.body0;
            for (int n = 0; n < body.goSlot.Count; n++)
            {
                var s = body.goSlot[n];
                if (s != null)
                {
                    if (s.bonehair3 != null)
                    {
                        DynamicSkirtBone bone = s.bonehair3.GetType().GetField("m_SkirtBone", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(s.bonehair3) as DynamicSkirtBone;
                        if (bone)
                            bone.m_fRegDefaultRadius = 0.1f * scaleReg;
                    }
                }
            }

            var objHit = FindChild(maid.body0.Pelvis.gameObject, "Hit_HipL");
            if (objHit)
            {
                objHit.GetComponentInChildren<DynamicBoneCollider>().m_Radius = 0.09f * scaleHip;
            }
            objHit = FindChild(maid.body0.Pelvis.gameObject, "Hit_HipR");
            if (objHit)
            {
                objHit.GetComponentInChildren<DynamicBoneCollider>().m_Radius = 0.09f * scaleHip;
            }
            objHit = FindChild(maid.body0.Pelvis.gameObject, "Hit_Vag");
            if (objHit)
            {
                objHit.GetComponentInChildren<DynamicBoneCollider>().m_Radius = 0.09f * scaleVag;
            }
#endif
        }

        public static GameObject FindChild(GameObject obj, string name)
        {
            var childrens = obj.GetComponentsInChildren<Transform>(false);
            foreach (var tr in childrens)
            {
                if (tr.name == name)
                {
                    return tr.gameObject;
                }
            }
            return null;
        }


        public static void UpdateHitScale_2(Maid maid, Dictionary<HitCheckTgt, float> hcscs)
        {
            TBody body = maid.body0;

            /*問題があるので廃止
            float scale = 1f;

            foreach (var v in hcscs)
            {
                scale *= v.Value;
            }

            if (scale != 1)
            */
            {
                HitScaleChangedMaids.Add(maid);
            }
            /*else //if (HitScaleChangedMaids.Contains(maid))
            {
                HitScaleChangedMaids.Remove(maid);
            }*/

            for (int n = 0; n < body.goSlot.Count; n++)
            {
                var s = body.goSlot[n];
                if (s != null)
                {
                    if (s.bonehair != null && s.bonehair.bodyhit != null)
                    {
                        PreUpdateScDetail(s.bonehair.bodyhit, hcscs);
                    }
                }
            }
        }

        // CM3D2.hitCheckResize.Managed.hitCheckResizeManagedより改造
        public static void PreUpdateScDetail(TBodyHit tbh, IDictionary<HitCheckTgt, float> scales)
        {
            for (int i = 0; i < tbh.spherelist.Count; i++)
            {
                //tbh.spherelist[i].wv_old = tbh.spherelist[i].wv;
                //Vector3 wv = tbh.spherelist[i].t.TransformPoint(tbh.spherelist[i].vs);
                //tbh.spherelist[i].wv = wv;
                Transform t = tbh.spherelist[i].t;
                //float num = Mathf.Pow(t.lossyScale.x * t.lossyScale.y * t.lossyScale.z, 0.333333343f);
                /*float num = (t.lossyScale.x + t.lossyScale.y + t.lossyScale.z) / 3f;
                bool flag = tbh.spherelist[i].pname == "Bip01 Spine0a";
                if (flag)
                {
                    tbh.spherelist[i].len = tbh.spherelist[i].len_ * 0.1f;
                }
                else
                {
                    tbh.spherelist[i].len = tbh.spherelist[i].len_ * num;
                }*/

#if DEBUG
                if (UnityEngine.Input.GetKey(KeyCode.H) && true)
                {
                    Console.WriteLine(tbh.spherelist[i].pname + " " + tbh.spherelist[i].len / tbh.spherelist[i].len_);
                }
#endif

                if (tbh.spherelist[i].len_ <= 0 || string.IsNullOrEmpty(tbh.spherelist[i].pname))
                    continue; //0は無視

                bool hit = false;
                foreach (var s in scales)
                {
                    if (tbh.spherelist[i].pname.Contains(HitCheckTgtStr[s.Key]))
                    {
                        tbh.spherelist[i].len = tbh.spherelist[i].len_ * s.Value;
                        hit = true;
                        break;
                    }
                }
                if (!hit)
                {
                    tbh.spherelist[i].len = tbh.spherelist[i].len_ * scales[HitCheckTgt.Bip01];
                }
                //tbh.spherelist[i].len = tbh.spherelist[i].len_ * scale;
                tbh.spherelist[i].lenxlen = tbh.spherelist[i].len * tbh.spherelist[i].len;
#if COM3D2
                if (tbh.spherelist[i].tPtr != null)
                    tbh.spherelist[i].tPtr.localScale = new Vector3(tbh.spherelist[i].len, tbh.spherelist[i].tPtr.localScale.y, tbh.spherelist[i].tPtr.localScale.z);
#endif
            }
        }

        #region 保留
#if false//保留
        // TBody node消去/node表示
        public void SetVisibleNodeSlotEx(TBody tb, string slotname, bool boSetFlag, string name)
        {
            /*if (!tb.boMaid)
            {
                return;
            }*/
            if (!TBody.hashSlotName.ContainsKey(slotname))
            {
                NDebug.Assert("SetVisibleNodeSlot: not found slot name " + slotname);
                return;
            }
            int index = (int)TBody.hashSlotName[slotname];

            m_dicDelNodeBody_bkup.Clear();
            SetVisibleFlagEx(tb.goSlot[index], boSetFlag, name, tb.goSlot[0].obj_tr, false);
        }
        Dictionary<string, bool> m_dicDelNodeBody_bkup = new Dictionary<string, bool>();

        // TBodySkinより
        public void SetVisibleFlagEx(TBodySkin tbs, bool boSetFlag, string name, Transform t = null, bool boTgt = false)
        {
            if (t.name.IndexOf(name) >= 0)
            {
                boTgt = true;
            }
            if (name == "_ALL_")
            {
                boTgt = true;
            }
            if (boTgt)
            {
                m_dicDelNodeBody_bkup[t.name] = tbs.m_dicDelNodeBody[t.name];
                tbs.m_dicDelNodeBody[t.name] = boSetFlag;
            }
            foreach (Transform t2 in t)
            {
                SetVisibleFlagEx(tbs, boSetFlag, name, t2, boTgt);
            }
        }
        //AccExプラグインより
        public void FixFlag(Maid maid)
        {
            maid.body0.FixMaskFlag();
            maid.body0.FixVisibleFlag(false);
            maid.AllProcPropSeqStart();
        }
#endif
        #endregion
        // TBodyよりmanでも動くように
        static HashSet<Maid> hiddenMens_ = new HashSet<Maid>();
        public static void FixVisibleFlagMan(Maid man, bool visible)
        {
            TBody body = man.body0;
            if (!man.boMAN || body.goSlot[0].morph == null)
            {
                return;
            }

            if (!visible)
            {
                hiddenMens_.Add(man);
            }
            else //if(hiddenMens_.Contains(man))
            {
                hiddenMens_.Remove(man);
            }

            bool boTama = GetTamabkrVisible(man);
            for (int i = 0; i < body.goSlot.Count; i++)
            {
                TBodySkin tBodySkin = body.goSlot[i];
                if (tBodySkin.morph != null)
                {
                    tBodySkin.morph.ClearAllVisibleFlag(visible);

                    for (int j = 0; j < tBodySkin.morph.BoneNames.Count; j++)
                    {
                        if (tBodySkin.morph.BoneNames[j].ToLower().Contains("chinko"))
                            tBodySkin.morph.BoneVisible[j] = true;

                        if (tBodySkin.morph.BoneNames[j].ToLower().Contains("tamabukuro"))
                            tBodySkin.morph.BoneVisible[j] = boTama;
                    }
                }
            }
            for (int n = 0; n < body.goSlot.Count; n++)
            {
                TBodySkin tBodySkin4 = body.goSlot[n];
                if (tBodySkin4.morph != null)
                {
                    tBodySkin4.morph.FixVisibleFlag();
                }
            }
        }

        public static void SetManVisible(Maid man, bool visible)
        {
            FixVisibleFlagMan(man, visible);
            man.AllProcProp();
        }

        public static bool GetManVisible(Maid man)
        {
            return !hiddenMens_.Contains(man);
        }

        //Cnkの表示非表示状態
        static public void SetChinkoVisible(TBody body, bool visible)
        {
            body.SetChinkoVisible(visible);

            // ボーンのスケーリングも直す
            if (visible)
                SetChinkoScale(body, 1f);
        }

        //Cnkの表示非表示状態取得
        static public bool GetChinkoVisible(TBody body)
        {
            bool f_bVisibleGet = true;
            Vector3 localScale = Vector3.zero; //new Vector3(0f, 0f, 0f);

            /* v0030 公式の処理が変わって判定できなくなったので
            if (body.trManChinko != null)
            {
                if (body.trManChinko.localScale == localScale)
                {
                    f_bVisibleGet = false;
                }
                return f_bVisibleGet;
            }

            for (int i = 0; i < body.goSlot.Count; i++)
            {
                GameObject obj = body.goSlot[i].obj;
                if (obj != null)
                {
                    //v0030
                    //body.trManChinko = BoneLink.BoneLink.SearchObjName(obj.transform, "chinkoCenter", false);
                    var chinko = BoneLink.BoneLink.SearchObjName(obj.transform, "chinkoCenter", false);
                    if (chinko != null)
                    {
                        if (chinko.localScale == localScale)
                        {
                            f_bVisibleGet = false;
                            break; // v0030
                        }
                    }
                }
            }*/

            for (int i = 0; i < body.goSlot.Count; i++)
            {
                GameObject obj = body.goSlot[i].obj;
                if (obj != null)
                {
                    Transform chinko = CMT.SearchObjName(obj.transform, "chinkoCenter", false);
                    if (chinko != null && chinko.localScale == Vector3.zero)
                    {
                        return false;
                    }
                }
            }

            return f_bVisibleGet;
        }

        static public Vector3 GetChinkoScale(TBody body)
        {
            Vector3 localScale = Vector3.zero;

            if (SetChinkoScaleMens.ContainsKey(body))
            {
                //v0030 スケーリング変更中はボーンのスケール表示を優先
                Transform man_bone_tr = body.m_Bones.transform;
                var size = BoneLink.BoneLink.SearchObjName(man_bone_tr, "chinkoCenter", true).localScale;
                if (size != Vector3.one)
                    return size;
            }

            /* v0030
            if (body.trManChinko != null)
            {
                return body.trManChinko.localScale;
            }
            */
            for (int i = 0; i < body.goSlot.Count; i++)
            {
                GameObject obj = body.goSlot[i].obj;
                if (obj != null)
                {
                    //v0030
                    //body.trManChinko = BoneLink.BoneLink.SearchObjName(obj.transform, "chinkoCenter", false);
                    var chinko = CMT.SearchObjName(obj.transform, "chinkoCenter", false);
                    if (chinko != null)
                    {
                        localScale = chinko.localScale;
                        break; //v0030
                    }
                }
            }
            return localScale;
        }

        /// <summary>
        /// 後で戻すための書き換え男bodyリスト
        /// ボーンのスケーリングはシーン変更などで勝手に戻らないのでメッシュと差異がでないように書き戻す必要がある
        /// </summary>
        static public Dictionary<TBody, float> SetChinkoScaleMens = new Dictionary<TBody, float>();
        static public Dictionary<TBody, Vector3> SetChinkoPosMens = new Dictionary<TBody, Vector3>();
        static public Dictionary<TBody, Vector3> LastMySetChinkoPosMens = new Dictionary<TBody, Vector3>();

        //Cnkのサイズ調整
        static public void SetChinkoScale(TBody body, float f/*, Vector3 dpos*/)
        {
            if (!body.boMAN)
                return;

            if (f != 1f)
                SetChinkoScaleMens[body] = f;
            else
                SetChinkoScaleMens.Remove(body);

            Vector3 scale = new Vector3(f, f, f);

            for (int i = 0; i < body.goSlot.Count; i++)
            {
                GameObject obj = body.goSlot[i].obj;
                if (obj != null)
                {
                    //fix v0027 var//1.59で公式処理でも代入が消えたので   chinko = body.trManChinko = CMT.SearchObjName(obj.transform, "chinkoCenter", false);
                    var chinko = CMT.SearchObjName(obj.transform, "chinkoCenter", false);
                    if (chinko != null)
                    {
                        chinko.localScale = scale;
                        //body.trManChinko.localPosition = dpos;
                    }
                }
            }

            //ボーンもスケーリング
            Transform man_bone_tr = body.m_Bones.transform;

            // CMT.SearchObjNameが再帰メソッドで遅かったのでボーン情報をDictionaryでキャッシュするラッパーメソッド
            BoneLink.BoneLink.SearchObjName(man_bone_tr, "chinkoCenter", true).localScale = scale;
        }


        //Cnkのサイズ調整を維持（ボーンスケールとのずれを防ぐ）
        static public void FixChinkoScaleInUpdate(TBody body)
        {
            if (!body.boMAN)
                return;

            if (Time.frameCount % 6 != 0) // 6フレーム毎にチェック
                return;

            //Vector3 localScale = Vector3.zero;

            if (SetChinkoScaleMens.ContainsKey(body))
            {
                //v0030 スケーリング変更中
                Transform man_bone_tr = body.m_Bones.transform;
                var size = BoneLink.BoneLink.SearchObjName(man_bone_tr, "chinkoCenter", true).localScale;
                if (size != Vector3.one)
                {
                    float f = size.x;
                    Vector3 scale = size;//new Vector3(f, f, f);

                    for (int i = 0; i < body.goSlot.Count; i++)
                    {
                        GameObject obj = body.goSlot[i].obj;
                        if (obj != null)
                        {
                            var chinko = CMT.SearchObjName(obj.transform, "chinkoCenter", false);
                            if (chinko != null)
                            {
                                if (chinko.localScale == scale)
                                    return;

                                if (chinko.localScale == Vector3.zero)
                                    return;

                                // 非表示でなければ適用
                                chinko.localScale = scale;
                            }
                        }
                    }
                }
                else
                {
                    SetChinkoScaleMens.Remove(body);
                }
            }
        }

        //static FieldInfo fiChinkoOffsetOrg = typeof(TBody).GetField("vecChinkoOffset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //Cnkのサイズ調整
        static public void SetChinkoPos(TBody body, Vector3 dpos)
        {
            if (!body.boMAN)
                return;

            //ボーン
            Transform man_bone_tr = body.m_Bones.transform;
            var cctr = BoneLink.BoneLink.SearchObjName(man_bone_tr, "chinkoCenter", true);

            /* trManChinkoだと何故か動かない
            var cctr = body.trManChinko;
            if (!cctr)
                cctr = BoneLink.BoneLink.SearchObjName(man_bone_tr, "chinkoCenter", true);
                */

            if (!SetChinkoPosMens.ContainsKey(body))
            {
                SetChinkoPosMens[body] = cctr.localPosition;
            }
            else if (LastMySetChinkoPosMens.ContainsKey(body)) //v0027 fix
            {
                if (cctr.localPosition != (LastMySetChinkoPosMens[body])
                    && body.vecChinkoOffset == cctr.localPosition)
                { // 他で位置が変更されたら初期値をセットし直す（SetManOffsetPosなど）
                    SetChinkoPosMens[body] = cctr.localPosition;
                }
            }
            //else if (SetChinkoPosMens[body] == dpos)
            //    SetChinkoPosMens.Remove(body);

            cctr.localPosition = dpos + SetChinkoPosMens[body];

            //v0027 fix body.vecChinkoOffset対応
            LastMySetChinkoPosMens[body] = cctr.localPosition;
            body.vecChinkoOffset = cctr.localPosition;
            //if (fiChinkoOffsetOrg != null)
            //    fiChinkoOffsetOrg.SetValue(body, cctr.localPosition);

            if (Vector3.zero == dpos)
            {
                SetChinkoPosMens.Remove(body);
                LastMySetChinkoPosMens.Remove(body);
            }
        }

        static public Vector3 GetInitChinkoPos(TBody body)
        {
            Vector3 v3 = Vector3.zero;
            /*if (SetChinkoPosMens.TryGetValue(body, out v3))
            {
                return v3;
            }*/
            return Vector3.zero;
        }

        static public Vector3 GetChinkoPos(TBody body)
        {
            //ボーン
            Transform man_bone_tr = body.m_Bones.transform;
            var cctr = BoneLink.BoneLink.SearchObjName(man_bone_tr, "chinkoCenter", true);

            if (!SetChinkoPosMens.ContainsKey(body))
                SetChinkoPosMens[body] = cctr.localPosition;

            return cctr.localPosition - SetChinkoPosMens[body];
        }

        static public void ResetChinkoAll()
        {
            //サイズ
            bool chk = SetChinkoScaleMens.Count > 0;
            foreach (var mandata in SetChinkoScaleMens.ToArray())
            {
                if (mandata.Key)
                    SetChinkoScale(mandata.Key, 1f);
            }

            if (chk && SetChinkoScaleMens.Count > 0)
                Console.WriteLine("◆◆注意◆◆：Chinkoのスケーリング復元が不完全。動きや射精などがズレる場合は、男局部のスケーリングを一度1以外に設定後、1に戻してください");
            else
                SetChinkoScaleMens.Clear();
#if DEBUG
            if (chk && SetChinkoScaleMens.Count == 0)
                debugPrintConsole("◆Chinkoのスケーリング復元が正常に完了");
#endif

            //位置
            chk = SetChinkoPosMens.Count > 0;
            foreach (var mandata in SetChinkoPosMens.ToArray())
            {
                if (mandata.Key)
                    SetChinkoPos(mandata.Key, Vector3.zero);
                //  SetChinkoPos(mandata.Key, mandata.Value);
            }
            if (chk && SetChinkoPosMens.Count > 0)
                Console.WriteLine("◆◆注意◆◆：Chinkoの位置復元が不完全。動きや射精などがズレる場合は、男局部の位置調整を一度0以外に設定後、0に戻してください");
            else
            {
                SetChinkoPosMens.Clear();
                LastMySetChinkoPosMens.Clear();
            }
        }

#if false
        static private bool GetTamabkrVisible(TBody body)
        {
            bool f_bVisibleGet = true;
            Vector3 localScale = new Vector3(0f, 0f, 0f);
            for (int i = 0; i < body.goSlot.Count; i++)
            {
                GameObject obj = body.goSlot[i].obj;
                if (obj != null)
                {
                    var tama = BoneLink.BoneLink.SearchObjName(obj.transform, "tamabukuro", false);
                    if (tama != null)
                    {
                        if (tama.localScale == localScale)
                        {
                            f_bVisibleGet = false;
                        }
                    }
                }
            }
            return f_bVisibleGet;
        }

        static private bool SetTamabkrVisible(TBody body, bool visible)
        {
            bool f_bVisibleGet = true;
            Vector3 localScale = visible ? new Vector3(1f, 1f, 1f) : new Vector3(0f, 0f, 0f);
            for (int i = 0; i < body.goSlot.Count; i++)
            {
                GameObject obj = body.goSlot[i].obj;
                if (obj != null)
                {
                    var tama = BoneLink.BoneLink.SearchObjName(obj.transform, "tamabukuro", false);
                    if (tama != null)
                    {
                        tama.localScale = localScale;
                    }
                }
            }
            return f_bVisibleGet;
        }
#else
        static private bool GetTamabkrVisible(Maid man)
        {
            bool f_bVisibleGet = true;
            TBody body = man.body0;
            if (!man.boMAN || body.goSlot[0].morph == null)
            {
                return f_bVisibleGet;
            }

            for (int i = 0; i < body.goSlot.Count; i++)
            {
                TBodySkin tBodySkin = body.goSlot[i];
                if (tBodySkin.morph != null)
                {
                    for (int j = 0; j < tBodySkin.morph.BoneNames.Count; j++)
                    {
                        if (tBodySkin.morph.BoneNames[j].ToLower().Contains("tamabukuro"))
                            f_bVisibleGet = tBodySkin.morph.BoneVisible[j];
                    }
                }
            }
            for (int n = 0; n < body.goSlot.Count; n++)
            {
                TBodySkin tBodySkin4 = body.goSlot[n];
                if (tBodySkin4.morph != null)
                {
                    tBodySkin4.morph.FixVisibleFlag();
                }
            }
            return f_bVisibleGet;
        }

        static private void SetTamabkrVisible(Maid man, bool visible)
        {
            TBody body = man.body0;
            if (!man.boMAN || body.goSlot[0].morph == null)
            {
                return;
            }

            for (int i = 0; i < body.goSlot.Count; i++)
            {
                TBodySkin tBodySkin = body.goSlot[i];
                if (tBodySkin.morph != null)
                {
                    for (int j = 0; j < tBodySkin.morph.BoneNames.Count; j++)
                    {
                        if (tBodySkin.morph.BoneNames[j].ToLower().Contains("tamabukuro"))
                        {
                            tBodySkin.morph.BoneVisible[j] = visible;
                            tBodySkin.morph.GetType().InvokeMember("m_bDut", BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.SetField,
                                                                    null, tBodySkin.morph, new object[] { true });
                        }
                    }
                }
            }
            for (int n = 0; n < body.goSlot.Count; n++)
            {
                TBodySkin tBodySkin4 = body.goSlot[n];
                if (tBodySkin4.morph != null)
                {
                    tBodySkin4.morph.FixVisibleFlag();
                }
            }
            man.AllProcProp();
        }
#endif
        public static string V3Text(Vector3 v3)
        {
            //return "\t" + angle180d(v3.x) + "\r\n\t" + angle180d(v3.y) + "\r\n\t" + angle180d(v3.z) + "\r\n";
            return string.Format("\t{0:0.#####}\r\n\t{1:0.#####}\r\n\t{2:0.#####}\r\n", v3v(v3.x), v3v(v3.y), v3v(v3.z));
        }
        public static string V3TextA(Vector3 v3)
        {
            //return "\t" + angle180d(v3.x) + "\r\n\t" + angle180d(v3.y) + "\r\n\t" + angle180d(v3.z) + "\r\n";
            return string.Format("\t{0:0.####}\r\n\t{1:0.####}\r\n\t{2:0.####}\r\n", angle180d(v3.x), angle180d(v3.y), angle180d(v3.z));
        }

        static string V3S(Vector3 v3)
        {
            return "X:" + angle180d(v3.x) + " Y:" + angle180d(v3.y) + " Z:" + angle180d(v3.z);
        }

        static float v3v(float f)
        {
            f = (float)Math.Round(f, 5);
            if (Math.Abs(f) < 0.00001)
                f = 0;

            return f;
        }

        static float angle180d(float f)
        {
            f = (float)Math.Round(f, 4);
            if (Math.Abs(f) < 0.0001)
                f = 0;

            if (f > 180)
            {
                f -= 360;
            }
            else if (f < -180)
            {
                f += 360;
            }
            return f;
        }

        public static Vector3 va180(Vector3 v)
        {
            v.x = angle180d(v.x);
            v.y = angle180d(v.y);
            v.z = angle180d(v.z);

            return v;
        }

        public static float[] v3Tofa(Vector3 v)
        {
            float[] fa = new float[3];
            fa[0] = v.x;
            fa[1] = v.y;
            fa[2] = v.z;

            return fa;
        }

        public static Vector3 faTov3(float[] fa)
        {
            if (fa.Length != 3)
            {
                Console.WriteLine("XtMS: インデックスエラー。float[]→Vector3の変換に失敗");
                return Vector3.zero;
            }

            return new Vector3(fa[0], fa[1], fa[2]);
        }

        public static class InputEx
        {

            [FlagsAttribute]
            public enum ModifierKey
            {
                None = 0x00,
                Control = 0x01,
                Alt = 0x02,
                Shift = 0x04
            }

            //static int fCnt_last = -1;
            static ModifierKey m_getMdfKeys = ModifierKey.None;
            /*static public void GetModifierKeys(ModifierKey mdfkey)
            {
                GetModifierKeys(true, mdfkey);
            }*/
            static public void GetModifierKeys(/*bool ForceUpdate, */ModifierKey mdfkey)
            {
                ModifierKey getmdfkey = ModifierKey.None;

                /*//基本的には毎フレームに一度だけチェック
                if (Time.frameCount == fCnt_last && !ForceUpdate)
                    return;
                fCnt_last = Time.frameCount;
                */
                if ((mdfkey & ModifierKey.Control) != 0)
                {
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        getmdfkey = getmdfkey | ModifierKey.Control;
                }
                if ((mdfkey & ModifierKey.Alt) != 0)
                {
                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                        getmdfkey = getmdfkey | ModifierKey.Alt;
                }
                if ((mdfkey & ModifierKey.Shift) != 0)
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        getmdfkey = getmdfkey | ModifierKey.Shift;
                }
                m_getMdfKeys = getmdfkey;
            }

            static public bool GetKeyDownEx(KeyCode key, ModifierKey mdfkey)
            {
                GetModifierKeys(mdfkey);
                if (m_getMdfKeys != mdfkey)
                    return false;

                return Input.GetKeyDown(key);
            }

            static public bool GetModifierKey(ModifierKey mdfkey)
            {
                GetModifierKeys(mdfkey);
                return m_getMdfKeys == mdfkey;
            } 
        }
        #endregion
        public static bool IsKeepScene()
        {
            return vIsKaisouScene || bIsYotogiScene;
        }

        public class keepSlaveInfo
        {
            public string lastMotionFN = string.Empty;
            public Vector3 lastPos = Vector3.zero;
            private int pageNum;

            public keepSlaveInfo(int index)
            {
                this.pageNum = index;
            }

            public void SaveLastInfo(Maid m)
            {
                lastMotionFN = m.body0.LastAnimeFN;
                lastPos = m.gameObject.transform.position;
            }

            public bool CheckMoved(Maid m, int pageN)
            {
                if (lastMotionFN != m.body0.LastAnimeFN/* || lastPos != m.gameObject.transform.position*/)
                {
                    debugPrintConsole(lastMotionFN + lastPos + " != " + m.body0.LastAnimeFN + m.gameObject.transform.position);
                    Console.WriteLine("[!] ゲームシステムによるキャラクター操作を検知したためM-Sリンク<" + this.pageNum + ">を中断");

                    //アタッチの解除
                    if (cfgs[pageN].doIKTargetMHand || cfgs[pageN].doCopyIKTarget)
                    {
                        m.IKTargetClear();
                    }
                    //ハーレムプレイなどで問題がでることがあるのでリンク中止
                    _MSlinks[pageN].doMasterSlave = false;
                    _MSlinks[pageN].maidKeepSlaveYotogi = null;

                    //モーションが変更されたらシステム側のキャラクター操作があったと判断
                    return true;
                }
                /* 中断されすぎるのでなし
                Animation animation = m.body0.m_Bones.GetComponent<Animation>();
                if (animation != null)
                {
                    var anime_state = animation[m.body0.LastAnimeFN.ToLower()];
                    if (anime_state != null && anime_state.length != 0f)
                    {
                        //debugPrintConsole("anime_state = " + anime_state.enabled);

                        //アニメが再開されていたらtrue
                        return anime_state.enabled;
                    }
                }*/
                return false;
            }
        }

        public static string pathConfig(string path)
        {
            {
                if (IsCom3d2)
                {
                    //if (!path.Contains("Config"))
                    {
                        path = Path.Combine("Config", path);
                    }
                    return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
                }
                return path;
            }
        }

        #region Config
        // ExPluginBase (Copyright (c) 2015 asm__) より改造
        public T ReadConfig<T>(string section, string dirPath) where T : new()
        {
            return SharedConfig.ReadConfig<T>("Config", Path.Combine(dirPath, GetType().Name + ".ini"));
        }
        public void SaveConfig<T>(T data, string section, string dirPath)
        {
            SharedConfig.SaveConfig("Config", Path.Combine(dirPath, GetType().Name + ".ini"), data);
        }
        #endregion


        public void LoadMyConfigs()
        {
            // Iniファイル読み出し
            //cfg = ReadConfig<PluginConfig>("Config");
            /*
            v3StackOffset = faTov3(cfg.v3StackOffsetFA);
            v3StackOffsetRot = faTov3(cfg.v3StackOffsetRotFA);
            v3HandLOffset = faTov3(cfg.v3HandLOffsetFA);
            v3HandROffset = faTov3(cfg.v3HandROffsetFA);
            */

            cfg = ReadConfig<PluginConfig>("Config", pathConfig(""));

            for (int i = 0; i < cfgs.Length; i++)
            {
                if (cfg.doHitScaleDef)
                {
                    cfgs[i].Scale_HitCheckDetail = true;

                    foreach (var v in HitCheckTgtStr)
                        cfgs[i].GetHitDetail(v.Key) = cfg.HitScaleDef[(int)v.Key];
                }

                if (i == 0)
                    cfgs[i] = ReadConfig<MsLinkConfig>("Config", pathConfig(""));
                else
                    cfgs[i] = SharedConfig.ReadConfig<MsLinkConfig>("Config-" + (i + 1).ToString(), pathConfig(PluginCfgFN));
            }

            for (int i = 0; i < cfgs.Length; i++)
            {
                IniCfgsTov3Offsets(ref v3ofs[i], cfgs[i]);
            }
            ycfg = PluginExt.SharedConfig.ReadConfig<YotogiConfig>("Config", pathConfig(YotogiCfgFN));
            VYMModule.VymModule.cfg = PluginExt.SharedConfig.ReadConfig<VYMModule.VymModule.VibeYourMaidConfig>("VYMAsset", pathConfig(YotogiCfgFN));

            // v0027
            // ボイステーブルをini→CSVに
            VymModule.voiceLegacy = SharedConfig.ReadConfig<VymModule.VoiceTableLegacy>("VYMAsset", pathConfig(YotogiCfgFN));
            cnv2csv.SaveVoiceCsvFile();
            // CSVロード
            NewVoiceTable.LoadCsv();
        }

        public static void IniCfgsTov3Offsets(ref v3Offsets v3ofs, MsLinkConfig cfgs)
        {
            v3ofs.v3StackOffset = faTov3(cfgs.v3StackOffsetFA);
            v3ofs.v3StackOffsetRot = faTov3(cfgs.v3StackOffsetRotFA);
            v3ofs.v3HandLOffset = faTov3(cfgs.v3HandLOffsetFA);
            v3ofs.v3HandROffset = faTov3(cfgs.v3HandROffsetFA);
            v3ofs.v3HandLOffsetRot = faTov3(cfgs.v3HandLOffsetRotFA);
            v3ofs.v3HandROffsetRot = faTov3(cfgs.v3HandROffsetRotFA);

            // 5.0
            v3ofs.customHandR = new PosRot(cfgs.customHandRfa);
            v3ofs.customHandL = new PosRot(cfgs.customHandLfa);
        }

        public void SaveMyConfigs()
        {
            // Iniファイル書き出し
            for (int i = 0; i < cfgs.Length; i++)
            {
                v3OffsetsToIniCfgs(ref cfgs[i], v3ofs[i]);
            }
            SaveConfig(cfg, "Config", pathConfig(""));
            SaveConfig(cfgs[0], "Config", pathConfig(""));
            for (int i = 1; i < cfgs.Length; i++)
                SharedConfig.SaveConfig("Config-" + (i + 1).ToString(), pathConfig(PluginCfgFN), cfgs[i]);

            PluginExt.SharedConfig.SaveConfig("Config", pathConfig(YotogiCfgFN), ycfg);
            PluginExt.SharedConfig.SaveConfig("VYMAsset", pathConfig(YotogiCfgFN), VYMModule.VymModule.cfg);
        }

        public static void v3OffsetsToIniCfgs(ref MsLinkConfig cfgs, v3Offsets v3ofs)
        {
            cfgs.v3StackOffsetFA = v3Tofa(v3ofs.v3StackOffset);
            cfgs.v3StackOffsetRotFA = v3Tofa(v3ofs.v3StackOffsetRot);
            cfgs.v3HandLOffsetFA = v3Tofa(v3ofs.v3HandLOffset);
            cfgs.v3HandROffsetFA = v3Tofa(v3ofs.v3HandROffset);
            cfgs.v3HandLOffsetRotFA = v3Tofa(v3ofs.v3HandLOffsetRot);
            cfgs.v3HandROffsetRotFA = v3Tofa(v3ofs.v3HandROffsetRot);

            // v5.0
            cfgs.customHandRfa = v3ofs.customHandR.ToFloatArray();
            cfgs.customHandLfa = v3ofs.customHandL.ToFloatArray();
        }

        void Awake()
        {
            GameObject.DontDestroyOnLoad(this);
#if COM3D2
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
#endif

#if true
            if (NeedHitScaleCalc)
                Console.WriteLine("XtMS: ヒットチェック計算を旧バージョン用に設定");
#endif

            //位置補正座標
            _Gizmo = OhMyGizmo.AddGizmo(base.transform, "XtMS_Gizmo");
            _Gizmo.eRotate = true;
            _Gizmo.eAxis = true;
            _Gizmo.eScal = false;
            _Gizmo.offsetScale = 1.0f;
            //_Gizmo.lineRSelectedThick = 0.2f;
            _Gizmo.Visible = false;

            _Gizmo_HandR = OhMyGizmo.AddGizmo(base.transform, "XtMS_Gizmo_HR");
            _Gizmo_HandR.eRotate = true;
            _Gizmo_HandR.eAxis = true;
            _Gizmo_HandR.eScal = false;
            _Gizmo_HandR.offsetScale = 0.5f;
            _Gizmo_HandR.Visible = false;
            _Gizmo_HandR.lineSelectedThick = 0.2f;

            _Gizmo_HandL = OhMyGizmo.AddGizmo(base.transform, "XtMS_Gizmo_HL");
            _Gizmo_HandL.eRotate = true;
            _Gizmo_HandL.eAxis = true;
            _Gizmo_HandL.eScal = false;
            _Gizmo_HandL.offsetScale = 0.5f;
            _Gizmo_HandL.Visible = false;
            _Gizmo_HandL.lineSelectedThick = 0.2f;

#if DEBUG
            //位置補正座標
            _Gizmo_dbg = OhMyGizmo.AddGizmo(base.transform, "XtMS_Gizmo_Dbg");
            _Gizmo_dbg.eRotate = true;
            _Gizmo_dbg.eAxis = true;
            _Gizmo_dbg.eScal = false;
            _Gizmo_dbg.offsetScale = 1.0f;
            //_Gizmo.lineRSelectedThick = 0.2f;
            _Gizmo_dbg.Visible = false;

            cnv2csv.SaveVoiceCsvFile();
            VYMModule.NewVoiceTable.LoadCsv();

            /*if (Ik159.IsIkMgr159)
            {

            }*/
#endif
#if !DEBUG
            // Iniファイル読み出し
            LoadMyConfigs();
#endif
            // Iniファイル書き出し
            SaveMyConfigs();
        }

        public void Start()
        {

        }

        public void OnApplicationQuit()
        {
        }

        public void OnDestroy()
        {
        }

#if !COM3D2
        void OnLevelWasLoaded(int level)
        {
#else
        void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            int level = scene.buildIndex;
#endif

#if DEBUG
            Console.WriteLine("Xtms: OnScene" + level);
            new GameObject("xtms-onescene").AddComponent<OneScene>();
#endif

            //　レベルの取得
            vSceneLevel = level;

            SceneLevelEnable = true;
            bIsYotogiScene = checkYotogiScene();

            //以下のシーンにある場合、プラグインを有効化
            //4：ドキドキ Fallin' Love   5：エディット
            //15：コミュニケーション等   
            //20：entrance to you   22：scarlet leap
            //26：stellar my tears   24：回想   27：撮影モード
            if (0 <= Array.IndexOf(cfg.IgnoreSceneLevel, vSceneLevel) && !bIsYotogiScene)
                SceneLevelEnable = false;

            //回想モードの夜伽判定用
            if (vSceneLevel == 24)
            {
                //if (!boIsKaisouScene)
                //    Console.WriteLine("VYM+夜伽リンク：回想モードに切り替えました");

                vIsKaisouScene = true;
            }
            else if (vSceneLevel == 1 || vSceneLevel == 3)
            { //1=夜伽、回想のメイド選択、3=昼/夜の自室メニュー
                //if (boIsKaisouScene)
                //    Console.WriteLine("VYM+夜伽リンク：通常モードに切り替えました");

                vIsKaisouScene = false;
            }

            //バケーションモードの切替チェック(vymより)
            if (vSceneLevel == 43 && !vacationEnabled) vacationEnabled = true;
            if (vSceneLevel == 3 && vacationEnabled) vacationEnabled = false;

            //初期化
            if (!vIsKaisouScene)
            {
                GuiFlag = false;
                _pageNum = 0;
            }
            else
            {
                if (GuiFlag)
                    showWndMin = true;
            }
            XtMs2ndWnd.boShow = false;

            //設定項目
            showPosSliderHand = false;
            showPosSliderHandR = true;
            showChinkoSlider = false;

            showSlaveEyeToTgt = false;
            showSubMens = false;

            showSlvMask = false;
            showHandTTPosSlider = false;

            GizmoVisible(false);
            GizmoHsVisible(false);
            //doMasterSlave = false;
            BoneLink.BoneLink.ClearCache();

            //ComboMaster.boPop = false;
            //ComboSlave.boPop = false;
            //ComboSubMaid.boPop = false;
            CloseAllCombos();

            _StockMaids.Clear();
            _StockMaids_Visible.Clear();

            VYMModule.VymModule.Reset();

            if (!(vIsKaisouScene || bIsYotogiScene))
            {
                //maidKeepSlaveYotogi = null;
                MsLinks.AllReset(true);
            }
            else
            {
                MsLinks.AllReset(false);
            }
            //lastSlaveStacked = null;

            //モーション取得情報クリア
            AnimeState.AllReset();

            //男局部のスケーリングリセット
            //（リセットしないとボーンだけ維持される）
            ResetChinkoAll();

            //リンク共通設定
            CommonEdit.ResetAll();

            //表示設定関係
            try
            {
                //多分なくても平気
                /*foreach (var m in maskedMaids.ToArray())
                {
                    if (m && m.body0) ResetMaskItemsAll(m);
                }*/
            }
            catch (Exception e)
            {
                debugPrintConsole("ResetMaskItemsAll例外：" + e);
            }
            maskedMaids.Clear();

            try
            {
                foreach (var m in HitScaleChangedMaids.ToArray())
                {
                    if (m && m.body0) UpdateHitScale_(m, 1f);
                }
            }
            catch (Exception e)
            {
                debugPrintConsole("UpdateHitScale例外：" + e);
            }
            HitScaleChangedMaids.Clear();

            try
            {
                foreach (var m in hiddenMens_.ToArray())
                {
                    if (m && m.body0) SetManVisible(m, true);
                }
            }
            catch (Exception e)
            {
                debugPrintConsole("SetManVisible例外：" + e);
            }
            hiddenMens_.Clear();
        }

        //　夜伽シーンにいるかをチェック
        private bool checkYotogiScene()
        {
            // OH版は夜伽シーンでもSceneが14にならない(10のまま)ので、YotogiManagerの有無で判別する
            int iYotogiManagerCount = FindObjectsOfType<YotogiManager>().Length;
            if (iYotogiManagerCount > 0) { return true; }
            return false;
        }

        //
        // GUI用
        //
        private bool GuiFlag = false;
        public const int GUI_WIDTH = 260;
        public static int GUI_HIGHT = 640;
        const int WIDTH_DPOS = -GUI_WIDTH - 30;
        const int HEIGHT_DPOS = -685 - 35;
        static Rect rc_stgw = new Rect(UnityEngine.Screen.width + WIDTH_DPOS, /*UnityEngine.Screen.height + HEIGHT_DPOS*/UnityEngine.Screen.height / 720 * 35, GUI_WIDTH, GUI_HIGHT);
        static Rect rc_stgw_caption = new Rect(0, 0, rc_stgw.width, 20);
        int ScWidth = 0, ScHeight = 0;
        private static bool bGuiOnMouse = true;

        void OnGUI()
        {
            // シーン有効チェック
            if (!SceneLevelEnable)
            {
                return;
            }

            //if (_MaidList.Count <= 0 || _MensList.Count <= 0)
            if (_MaidList.Count <= 0 /*|| _MensList.Count <= 0*/) //メイド単体稼働可に
            {
                return;
            }

            if (GuiFlag)
            {
                if (ScHeight != UnityEngine.Screen.height)
                {
                    if (UnityEngine.Screen.height >= 720)
                        rc_stgw.y = (UnityEngine.Screen.height - 720) * 0.1f + 30;
                    else
                        rc_stgw.y = 0;
                    ScHeight = UnityEngine.Screen.height;
                }
                if (ScWidth != UnityEngine.Screen.width)
                {
                    if (UnityEngine.Screen.width > 800)
                        rc_stgw.x = UnityEngine.Screen.width + WIDTH_DPOS;
                    else
                        rc_stgw.x = UnityEngine.Screen.width - rc_stgw.width;
                    ScWidth = UnityEngine.Screen.width;
                }

                GUIStyle gsWin = new GUIStyle("box")
                {
                    fontSize = 12,
                    alignment = TextAnchor.UpperLeft
                };

                if (showWndMode == 1)
                    GUI_HIGHT = 110;
                else if (showWndMode == 2)
                    GUI_HIGHT = 320;
                else
                    GUI_HIGHT = 640;
                rc_stgw.height = GUI_HIGHT;

                if (showWndMin)
                {
                    bool bdrag = false;
                    if (showWndMinHide)
                    {
                        rc_stgw.height = 20;
                    }
                    else
                    {
                        if (UnityEngine.Input.GetMouseButton(0))
                        {
                            bdrag = true;
                        }
                    }

                    if (rc_stgw.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                    {
                        showWndMinHide = false;
                    }
                    else if (!bdrag)
                    {
                        showWndMinHide = true;
                        rc_stgw.height = 20;
                    }
                }

                //メイン画面
                rc_stgw = GUI.Window(WINID_COFIG, rc_stgw, WindowCallback, PLUGIN_NAME + " " + PLUGIN_VERSION, gsWin);

                if (rc_stgw.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    GameMain.Instance.MainCamera.SetControl(false);
                    bGuiOnMouse = true;
                }
                else
                {
                    if (bGuiOnMouse)
                        GameMain.Instance.MainCamera.SetControl(true);
                    bGuiOnMouse = false;
                }
            }
            else if (bGuiOnMouse)
            {
                GameMain.Instance.MainCamera.SetControl(true);
                bGuiOnMouse = false;
            }
        }//OnGUI()

        float btnset_LR(Rect rc, int bw, float f, float fd, GUIStyle gsButton, float min, float max)
        {
            f = btnset_LR(rc, bw, f, fd, gsButton);

            if (f < min)
                f = min;
            if (f > max)
                f = max;

            return f;
        }

        float btnset_LR(Rect rc, int bw, float f, GUIStyle gsButton, float min, float max)
        {
            f = btnset_LR(rc, bw, f, gsButton);

            if (f < min)
                f = min;
            if (f > max)
                f = max;

            return f;
        }

        float btnset_LR(Rect rc, int bw, float f, GUIStyle gsButton)
        {
            float fd = 0.001f;
            f = btnset_LR(rc, bw, f, fd, gsButton);

            return f;
        }
        float btnset_LR(Rect rc, int bw, float f, float fd, GUIStyle gsButton)
        {
            if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                fd *= 0.1f;

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                fd *= 100f;

            if (GUI.Button(rc, "<", gsButton))
            {
                f -= fd;
            }
            rc.x += bw;
            if (GUI.Button(rc, ">", gsButton))
            {
                f += fd;
            }

            return f;
        }


        void WindowCallback(int id)
        {
            try
            {
                foreach (var v in _MSlinks)
                {
                    v.MsUpdate(true, true);

                    if (v.maidKeepSlaveYotogi)
                    {
                        //リンク中断判定
                        v.keepSI.CheckMoved(v.maidKeepSlaveYotogi, v.num_);
                    }
                }

                if (!XtMs2ndWnd.boShow)
                {
                    WindowCallback_proc(id);
                }
                else
                {
                    XtMs2ndWnd.SaveWindowCallback_proc(id, this, ref showWndMode, ref _pageNum, _MSlinks, cfgs, v3ofs, out _WinprocPhase);

                    GetMens();      //キャラ数が変わってる可能性があるので
                    GetMaids();     //キャラ数が変わってる可能性があるので
                }

                foreach (var v in _MSlinks)
                {
                    if (v.maidKeepSlaveYotogi)
                        v.keepSI.SaveLastInfo(v.maidKeepSlaveYotogi);

                    //キャラ選択を保持
                    //v.SaveMsLast();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("XtMS: 設定画面での例外エラー　状況：{0}\r\n" + e, _WinprocPhase);
            }

            GUI.DragWindow(rc_stgw_caption);
        }


        const int ItemX = 5;
        const int ItemWidth = GUI_WIDTH - 16 - 5;//- 10;
        const int ItemHeight = 20;
        const int ItemDw = 20;

        static string _WinprocPhase = "";

        public static Vector2 EditScroll_cfg = Vector2.zero;
        static int EditScroll_cfg_sizeY = 0;

        static EasyCombo ComboMaster = new EasyCombo("マスターを選択", 0);
        static EasyCombo ComboSlave = new EasyCombo("スレイブを選択", 0);
        static EasyCombo ComboSubMaid = new EasyCombo("サブメイドを選択", -1);
        static EasyCombo ComboSubMaidV = new EasyCombo("サブメイドを選択", -1);
        static EasyCombo ComboVoiceMode = new EasyCombo(VYMModule.VymModule.VoiceMode.オートモード.ToString(), 0);
        static EasyCombo ComboPosLinkBone = new EasyCombo("アタッチ先ボーン", -1);
        static EasyCombo ComboHandTgtBoneR = new EasyCombo("Handアタッチ先ボーン", -1);
        static EasyCombo ComboHandTgtBoneL = new EasyCombo("Handアタッチ先ボーン", -1);

        //static List<ManInfo> mdMasters = new List<ManInfo>();
        //static List<ManInfo> mdSlaves = new List<ManInfo>();
        static List<ManInfo> mdDummyMaidl = new List<ManInfo>();

        //リンク共通設定
        static public class CommonEdit
        {
            private static Dictionary<Maid, float> dicManAlpha = new Dictionary<Maid, float>();
            public static void SaveManAlpha(Maid man, float f)
            {
                if (f >= 0)
                    dicManAlpha[man] = f;
                else
                    dicManAlpha.Remove(man);
            }

            public static bool LoadManAlpha(Maid man, out float val)
            {
                return dicManAlpha.TryGetValue(man, out val);
            }

            public static void ProcManAlpha(bool needUpdate)
            {
                if (needUpdate || Time.frameCount % 24 == 0)    //更新は24フレーム毎
                {
                    foreach (var v in dicManAlpha.ToArray())
                    {
                        if (_MSlinks.Any(link => link.curMaster_ == v.Key
                            || (link.maidKeepSlaveYotogi && link.do_master == v.Key))) //どこかのページでマスターしてるか
                        {
                            if (v.Key && v.Key.Visible)
                                SetManAlpha(v.Key, v.Value);
                        }
                        else
                        {
                            dicManAlpha.Remove(v.Key);
                        }
                    }
                }
            }

            public static void ResetAll()
            {
                dicManAlpha.Clear();
            }
        }

        public class MsLinks
        {
            static List<MsLinks> list_ = new List<MsLinks>();
            public int mdMaster_No = -1;
            public int mdSlave_No = -1;

            public bool Scc1_MasterMaid = false;
            public List<ManInfo> mdMasters = new List<ManInfo>();
            public List<ManInfo> mdSlaves = new List<ManInfo>();

            //選択中のキャラクターインスタンス（msリンクがされているかは問わない）
            public Maid curMaster_ { get; private set; } = null;
            public Maid curSlave_ { get; private set; } = null;

            public void FixMaster() { curMaster_ = (mdMaster_No >= 0) ? mdMasters[mdMaster_No].mem : null; }
            public void FixSlave() { curSlave_ = (mdSlave_No >= 0) ? mdSlaves[mdSlave_No].mem : null; }

            public Maid GetMaster()
            {
                return (mdMaster_No >= 0 && mdMasters.Count() > mdMaster_No) ? mdMasters[mdMaster_No].mem : null;
            }
            public Maid GetSlave()
            {
                return (mdSlave_No >= 0 && mdSlaves.Count() > mdSlave_No) ? mdSlaves[mdSlave_No].mem : null;
            }

            public Maid maidKeepSlaveYotogi = null;
            public Maid lastSlaveStacked = null;
            public int num_;

            private BoneLink.BoneLink boneLink;

            //マスク保持用
            public bool holdSlvMask = false;
            public Maid holdSlvMaskMaid = null;
            public HashSet<string> holdSlvMaskItems = new HashSet<string>();

            //透明度保持
            //public float holdManAlpha = -1f; 

            public bool CheckSlvMaskSlave(Maid m)
            {
                if (!holdSlvMask)
                    return false;

                if (holdSlvMaskMaid != m || holdSlvMaskMaid == null || !m)
                {
                    holdSlvMask = false;
                    holdSlvMaskMaid = null;
                    holdSlvMaskItems.Clear();
                    return false;
                }
                return true;
            }

            // アタッチバックアップ用
            public BkupHandsAtc bkupHandTgt = null;

            //public Vector3 chinko_dpos = Vector3.zero;

            private bool doMasterSlave_ = false;
            public bool doMasterSlave
            {
                set
                {
                    if (value)
                    {
                        debugPrintConsole("doMasterSlave set: " + mdMaster_No + " / " + mdSlave_No);

                        //情報の保持
                        do_master = mdMasters[mdMaster_No].mem;
                        do_slave = mdSlaves[mdSlave_No].mem;

                        do_masterName = GetMaidName(mdMasters[mdMaster_No]);
                        do_slaveName = GetMaidName(mdSlaves[mdSlave_No].mem);
                    }
                    else if (doMasterSlave_ && do_slave && lastSlaveStacked == do_slave)
                    {
                        if (cfgs[num_].doIKTargetMHand || cfgs[num_].doCopyIKTarget)
                        {
                            do_slave.IKTargetClear();
                        }
                        lastSlaveStacked = null;
                    }
                    doMasterSlave_ = value;
                }
                get { return doMasterSlave_; }
            }
            public Maid do_master { get; private set; } = null;
            public Maid do_slave { get; private set; } = null;
            public string do_masterName { get; private set; } = string.Empty;
            public string do_slaveName { get; private set; } = string.Empty;

            public keepSlaveInfo keepSI;

            private int MouthMode = 0;
            VymMouthAnime mouthAnime = new VymMouthAnime();

            AnimeState animeState = new AnimeState();
            public bool bVoicePlaying = false;

            public int manuKyoseiZeccho = 0;

            public XtHandMgr.BlendMgr blendHandL = null;
            public XtHandMgr.BlendMgr blendHandR = null;

            public MsLinks()
            {
                num_ = list_.Count;
                list_.Add(this);
                keepSI = new keepSlaveInfo(num_);

                if (num_ == 0)
                {
                    //1ページ目だけ選択状態にしておく
                    mdMaster_No = 0;
                    mdSlave_No = 0;
                }

                boneLink = new BoneLink.BoneLink(num_);
            }

            ~MsLinks()
            {
                //終了時のみのはず
                if (list_.Contains(this))
                    list_.Remove(this);
            }

            //表情演出に必要なメイドが揃っているか確認(lateupdate用)
            public bool CheckPlayMaids()
            {
                return (mdSlaves.Count > mdSlave_No) && (mdSlave_No > 0 || !bIsYotogiScene)
                    && mdSlaves[0].mem && !mdSlaves[0].mem.boMAN && mdSlaves[mdSlave_No].mem;
            }

            public void Init()
            {
                mdMaster_No = -1;
                mdSlave_No = -1;

                if (num_ == 0)
                {
                    //1ページ目だけ選択状態にしておく
                    mdMaster_No = 0;
                    mdSlave_No = 0;
                }

                curMaster_ = null;
                curSlave_ = null;
                //holdManAlpha = -1f;
            }

            public static void AllReset(bool yotogiReset)
            {
                foreach (var v in list_)
                {
                    v.doMasterSlave = false;
                    v.lastSlaveStacked = null;
                    if (yotogiReset)
                    {
                        v.maidKeepSlaveYotogi = null;
                    }

                    v.Init();
                    v.holdSlvMask = false;
                    v.holdSlvMaskMaid = null;
                    v.holdSlvMaskItems.Clear();

                    // シーン移動で解除
                    cfgs[v.num_].doIKTargetMHandSpCustom = false;
                }
            }

            //キャラクタの増減に追従させる
            //戻り値：継続可？
            public static bool chkMemNo(List<ManInfo> list, ref int list_no, Maid tgt)
            {
                bool bContinue = true;

                if (list_no >= list.Count)
                {
                    list_no = -1;
                    if (list.Count <= 0)
                        return false;
                }

                if (list_no < 0 || tgt != list[list_no].mem)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (tgt == list[i].mem)
                        {
                            list_no = i;
                            break;
                        }
                    }

                    if (list_no < 0 || tgt != list[list_no].mem)
                    {
                        list_no = -1;
#if DEBUG
                        debugPrintConsole(GetMaidName(tgt) + " 追跡に失敗");
#endif
                        bContinue = false;
                    }
                }
                return bContinue;
            }

            /*
            public void SaveMsLast()
            {
                if (mdMaster_No >= 0 && mdMasters.Count > mdMaster_No)
                {
                    preMaster_ = mdMasters[mdMaster_No].mem;
                }
                else
                {
                    mdMaster_No = -1;
                    preMaster_ = null;
                }

                if (mdSlave_No >= 0 && mdSlaves.Count > mdSlave_No)
                {
                    preSlave_ = mdSlaves[mdSlave_No].mem;
                }
                else
                {
                    mdSlave_No = -1;
                    preSlave_ = null;
                }
            }*/
            /*
            //キャラクター増減時の後処理用 
            public static void AllMsUpdateListChanged()
            {
                GetMens();
                GetMaids();

                foreach (var v in _MSlinks)
                {
                    v.MsUpdateListChanged(v.preMaster_, v.preSlave_, false);
                }
            }*/

            //cur=選択を維持したいキャラ
            public void MsUpdateListChanged(bool boMasterMaid, Maid man, Maid maid, bool getlist = true)
            {
                Maid master, slave;
                if (boMasterMaid)
                {
                    master = maid;
                    slave = man;
                }
                else
                {
                    master = man;
                    slave = maid;
                }

                if (master)
                {
                    f(master);
                }

                if (slave)
                {
                    f(slave);
                }

                void f(Maid cur)
                {
                    if (cur.boMAN)
                    {
                        if (getlist)
                            GetMens();

                        if (Scc1_MasterMaid)
                            MsLinks.chkMemNo(_MensList, ref this.mdSlave_No, cur);
                        else
                            MsLinks.chkMemNo(_MensList, ref this.mdMaster_No, cur);
                    }
                    else
                    {
                        if (getlist)
                            GetMaids();

                        if (!Scc1_MasterMaid)
                            MsLinks.chkMemNo(_MaidList, ref this.mdSlave_No, cur);
                        else
                            MsLinks.chkMemNo(_MaidList, ref this.mdMaster_No, cur);
                    }
                }

                if (getlist)
                {
                    if (Scc1_MasterMaid)
                    {
                        mdMasters = _MaidList;
                        mdSlaves = _MensList;
                    }
                    else
                    {
                        mdMasters = _MensList;
                        mdSlaves = _MaidList;
                    }
                }

                if (master)
                {
                    FixMaster();
                }

                if (slave)
                {
                    FixSlave();
                }
                //SaveMsLast();
                //MsUpdate();
            }

            public bool MsUpdate(bool midcheck, bool fixMS)
            {
                bool linkok = true;

                if (Scc1_MasterMaid)
                {
                    mdMasters = _MaidList;
                    mdSlaves = _MensList;
                }
                else
                {
                    mdMasters = _MensList;
                    mdSlaves = _MaidList;
                }

                if (mdMaster_No < 0)
                {
                    doMasterSlave = false;
                    //return;
                }
                else if (mdMaster_No >= mdMasters.Count)
                {
                    mdMaster_No = mdMasters.Count - 1;
                    if (mdMaster_No < 0)
                    {
                        mdMaster_No = -1;
                        doMasterSlave = false;
                    }
                    else
                    {
                        MsUpdate2();
                    }
                    //doMasterSlave = false;
                }

                if (mdSlave_No < 0)
                {
                    doMasterSlave = false;
                    //return;
                }
                else if (mdSlave_No >= mdSlaves.Count)
                {
                    mdSlave_No = mdSlaves.Count - 1;
                    if (mdSlave_No < 0)
                    {
                        mdSlave_No = -1;
                        doMasterSlave = false;
                    }
                    else
                    {
                        MsUpdate2();
                    }
                    //doMasterSlave = false;
                }

                /*未選択許容にしたので不要に
                 * if (mdMaster_No < 0)
                    mdMaster_No = 0;
                if (mdSlave_No < 0)
                    mdSlave_No = 0;*/

                if (mdMaster_No < 0 || mdSlave_No < 0)
                {
                    linkok = false;
                }

                //キャラクター変更時の選択解除
                if (midcheck)
                {
                    if (mdMaster_No >= 0)
                    {
                        if (curMaster_ != null && mdMasters[mdMaster_No].mem != curMaster_)
                        {
                            //もう一度探す
                            if (!MsLinks.chkMemNo(mdMasters, ref this.mdMaster_No, curMaster_))
                            {
                                linkok = false;
                                mdMaster_No = -1;
                                curMaster_ = null;
                            }
                        }
                    }

                    if (mdSlave_No >= 0)
                    {
                        if (curSlave_ != null && mdSlaves[mdSlave_No].mem != curSlave_)
                        {
                            //もう一度探す
                            if (!MsLinks.chkMemNo(mdSlaves, ref this.mdSlave_No, curSlave_))
                            {
                                linkok = false;
                                mdSlave_No = -1;
                                curSlave_ = null;
                            }
                        }
                    }
                }

                //変更を保持
                //SaveMsLast();

                if (fixMS)
                {
                    FixMaster();
                    FixSlave();
                }

                return linkok;
            }

            public void MsUpdate2(bool fixMS = false)
            {
                if (doMasterSlave)
                {
                    //選択キャラクターの追従チェック
                    bool doflg = true;
                    int slaveno_bk = mdSlave_No;

                    doflg = chkMemNo(mdMasters, ref mdMaster_No, do_master) && doflg;
                    doflg = chkMemNo(mdSlaves, ref mdSlave_No, do_slave) && doflg;
                    if (doMasterSlave != doflg)
                    {
                        doMasterSlave = doflg;

                        if (fixMS)
                        {
                            FixMaster();
                            FixSlave();
                        }
                    }

                    if (doMasterSlave && (bIsYotogiScene || vIsKaisouScene) && slaveno_bk != mdSlave_No)
                    {
                        //繰り上げでカレントメイドになったら止める
                        if (mdSlave_No == 0)
                            doMasterSlave = false;
                    }
                }

                //変更を保持
                //SaveMsLast();
            }

            public void lateUpdate()
            {
                if (!this.doMasterSlave || this.mdSlave_No < 0 || !this.CheckPlayMaids())
                    return;
                //debugPrintConsole("LateUpdate2");

                if (cfgs[this.num_].doFaceSync && !this.mdSlaves[0].mem.boMAN && this.mdSlave_No > 0)
                {
                    if (this.mdSlaves[0].mem.FaceName3.Contains("オリジナル"))
                    {
                        //表情同期２
                        FaceBlend2Sync(this.mdSlaves[0].mem.body0.Face.morph, this.mdSlaves[this.mdSlave_No].mem.body0.Face.morph, true);
                    }
                }

                if (cfgs[this.num_].doVoiceAndFacePlay)
                {
                    if (MouthMode != 0) //メイド以外は0以外にならない
                    {
                        //debugPrintConsole("MouthChange");
                        mouthAnime.MouthChange(this.mdSlaves[this.mdSlave_No].mem, MouthMode);
                    }
                }
            }

            //メイド単独処理など毎回必ず実行される
            public void inUpdate_HoldMask()
            {
                //#if false //動きがわかりにくくなるのでとりあえず止め?

                if (!this.holdSlvMask)
                    return;

                //this.MsUpdate();

                Maid slave = null;
                if (!this.Scc1_MasterMaid && this.mdSlaves.Count > 0 && this.mdSlave_No >= 0)
                    slave = this.mdSlaves[this.mdSlave_No].mem;

                if (!slave || slave.boMAN) //null
                    return;

                if (slave.IsBusy) //着替え中
                    return;

                //マスク保持
                if (this.CheckSlvMaskSlave(slave))
                {
                    //debugPrintConsole("holdSlvMask");

                    foreach (var item in dicMaskItems)
                    {
                        if (!this.holdSlvMaskItems.Contains(item.Key))
                        {
                            continue;
                        }

                        bool nflg = false;
                        bool vflg = false;
                        foreach (var v in item.Value)
                        {
                            vflg |= slave.body0.GetMask(v);
                        }

                        if (vflg != nflg)
                        {
                            foreach (var v in item.Value)
                            {
                                slave.body0.SetMask(v, nflg);
                            }
                        }
                    }
                }// if
                //#endif
            }

            /*
            public void keepManAlpha(Maid tgtman)
            {
                if (this.holdManAlpha >= 0 && (Time.frameCount % 24 == 0 || fioMgr.bUpdateRequest))    //更新は24フレーム毎
                    SetManAlpha(tgtman, this.holdManAlpha);
            }
            */

            // 手のアタッチ上書き用
            public void handsAtcpProc()
            {
                var p_mscfg = cfgs[num_];
                if (!p_mscfg.doIKTargetMHandSpCustom)
                    return;

                Maid master = (mdMaster_No >= 0 && mdMasters.Count() > mdMaster_No) ? mdMasters[mdMaster_No].mem : null;
                Maid slave = (mdSlave_No >= 0 && mdSlaves.Count() > mdSlave_No) ? mdSlaves[mdSlave_No].mem : null;
                if (!slave || !slave.body0)
                    return;

                var p_v3of2 = new v3OffsetsV2(v3ofs[num_], p_mscfg);

                if (p_mscfg.doIKTargetMHandSpR_TgtChar != ATgtChar.None)
                {
                    if (IkXT.IsNewIK)
                        IkXT.IkClear(slave, new List<string> { "右手" }, p_mscfg/*null, Ik159.GetDefType(p_mscfg)*/);

                    Maid tgt = GetHandAtcTgt(true, slave, master, p_mscfg);
                    AtccHand1R(slave, tgt, p_mscfg.doIKTargetMHandSpR_TgtBone, p_v3of2.v3HandROffset);
                }
                if (p_mscfg.doIKTargetMHandSpL_TgtChar != ATgtChar.None)
                {
                    if (IkXT.IsNewIK)
                        IkXT.IkClear(slave, new List<string> { "左手" }, p_mscfg/*null, Ik159.GetDefType(p_mscfg)*/);

                    Maid tgt = GetHandAtcTgt(false, slave, master, p_mscfg);
                    AtccHand1L(slave, tgt, p_mscfg.doIKTargetMHandSpL_TgtBone, p_v3of2.v3HandLOffset);
                }
            }

#if false
            bool bkupHandRotR, bkupHandRotL = false;
            Quaternion bkupHandRotRq, bkupHandRotLq;

            IkMini _ikR = new IkMini();
            IkMini _ikL = new IkMini();
            public void setupIkMIni()
            {
                var p_mscfg = cfgs[num_];
                if (!p_mscfg.doIKTargetMHandSpCustom)
                    return;

                Maid slave = (mdSlave_No >= 0 && mdSlaves.Count() > mdSlave_No) ? mdSlaves[mdSlave_No].mem : null;
                if (!slave || !slave.body0)
                    return;

                string str = "Bip01";
                if (slave.boMAN)
                {
                    str = "ManBip";
                }

                var body = slave.body0;
                var trb = slave.body0.m_Bones.transform;
                _ikR.UpperArm = BoneLink.BoneLink.SearchObjName(trb, str + " R UpperArm", true);
                _ikL.UpperArm = BoneLink.BoneLink.SearchObjName(trb, str + " L UpperArm", true);
                _ikR.Forearm = BoneLink.BoneLink.SearchObjName(trb, str + " R Forearm", true);
                _ikL.Forearm = BoneLink.BoneLink.SearchObjName(trb, str + " L Forearm", true);
                _ikR.Hand = BoneLink.BoneLink.SearchObjName(trb, str + " L Hand", true);
                _ikL.Hand = BoneLink.BoneLink.SearchObjName(trb, str + " L Hand", true);

                trb = slave.body0.m_Bones2.transform; // 元の角度
                _ikR.UpperArm2 = BoneLink.BoneLink.SearchObjName(trb, str + " R UpperArm", true);
                _ikL.UpperArm2 = BoneLink.BoneLink.SearchObjName(trb, str + " L UpperArm", true);
                _ikR.Forearm2 = BoneLink.BoneLink.SearchObjName(trb, str + " R Forearm", true);
                _ikL.Forearm2 = BoneLink.BoneLink.SearchObjName(trb, str + " L Forearm", true);
                _ikR.Hand2 = BoneLink.BoneLink.SearchObjName(trb, str + " L Hand", true);
                _ikL.Hand2 = BoneLink.BoneLink.SearchObjName(trb, str + " L Hand", true);

                _ikR.IkPreProc();
                _ikL.IkPreProc();
            }

            class IkMini
            {
                public Transform UpperArm;
                public Transform Forearm;
                public Transform Hand;

                public Transform UpperArm2;
                public Transform Forearm2;
                public Transform Hand2;

                class boneProp
                {
                    public Vector3 position;
                    public Vector3 localPosision;
                    public Quaternion rotation;
                    public Quaternion localRotation;

                    public void Set(Transform t)
                    {
                        this.position = t.position;
                        this.localPosision = t.localPosition;
                        this.rotation = t.rotation;
                        this.localRotation = t.localRotation;
                    }
                }
                boneProp _kata = new boneProp();
                //boneProp _hiji = new boneProp();
                //boneProp _tekubi = new boneProp();

                public void IkPreProc()
                {
                    IkPreProc(this.UpperArm, this.Forearm, this.Hand);
                }

                public void IkPreProc(Transform shoulder, Transform elbow, Transform hand)
                {
                    _kata.Set(shoulder);
                    //_hiji.Set(elbow);
                    //_tekubi.Set(hand);
                }

                public void IkProc(Vector3 tgt, Vector3 vechand_offset)
                {
                    IkProc2(this.UpperArm, this.Forearm, this.Hand, tgt, vechand_offset);
                }

                public void IkProc1(Transform shoulder, Transform elbow, Transform hand, Vector3 tgt, Vector3 vechand_offset)
                {
                    tgt += hand.rotation * vechand_offset;
                    Vector3 tgtv = tgt - _kata.position;
                    float tgtd2 = tgtv.sqrMagnitude;
                    var b2 = (elbow.position - shoulder.position).sqrMagnitude;
                    var c2 = (elbow.position - hand.position).sqrMagnitude;

                    if ((hand.position - shoulder.position).sqrMagnitude >= tgtd2)
                    {
                        // 手が届かない
                        elbow.transform.rotation = Quaternion.identity;
                        
                    }
                    else {
                        //float tgtd = Mathf.Sqrt( tgtd2 );
                        var b = Mathf.Sqrt(b2);
                        var c = Mathf.Sqrt(c2);

                        // 肘の角度を求める
                        var A = Mathf.Rad2Deg * Mathf.Acos((b2 + c2 - tgtd2) / (2 * b * c));
                        elbow.localRotation = Quaternion.Euler(0f, elbow.localRotation.y, A);
                    }

                    shoulder.transform.rotation = _kata.rotation;
                    var vectgt = tgt - hand.position;
                    shoulder.rotation = Quaternion.FromToRotation(vectgt.normalized, tgt.normalized) * shoulder.rotation;
                    return;
                }

                public void IkProc2(Transform shoulder, Transform elbow, Transform hand, Vector3 tgt, Vector3 vechand_offset)
                {
                    //tgt += hand.rotation * vechand_offset;
                    tgt += vechand_offset;
                    Vector3 tgtv = tgt - shoulder.position;
                    float tgtd2 = tgtv.sqrMagnitude;
                    float tgtd = Mathf.Sqrt(tgtv.sqrMagnitude);
                    var b2 = (elbow.position - shoulder.position).sqrMagnitude;
                    var c2 = (elbow.position - hand.position).sqrMagnitude;

                    // 手首に合わせて前腕を捻る
                    var hiji = Forearm2.localRotation.eulerAngles;
                    /*var hiji2 = Forearm2.localRotation.eulerAngles;
                    var te = hand.localRotation.eulerAngles;
                    var te2 = Hand2.localRotation.eulerAngles;
                    hiji.y = (hiji.y - hiji2.y) * 0.5f + (te.y - te2.y) * 0.5f + hiji2.y;
                    */
                    // 肘ピンから計算
                    elbow.localRotation = Quaternion.Euler(hiji.x, hiji.y, 0f);
                    if (((hand.position - shoulder.position).sqrMagnitude) <= tgtd2)
                    {
                        // 手が届かない
                        //elbow.transform.rotation = Quaternion.identity;
                        //elbow.localRotation = Quaternion.Euler(0f, elbow.localRotation.y, 0f);
                    }
                    else
                    {
                        //float tgtd = Mathf.Sqrt( tgtd2 );
                        var b = Mathf.Sqrt(b2);
                        var c = Mathf.Sqrt(c2);

                        // 肘の角度を求める
                        var A = Mathf.Rad2Deg * Mathf.Acos((b2 + c2 - tgtd2) / (2 * b * c));
                        elbow.localRotation = Quaternion.Euler(hiji.x, hiji.y, A);
                    }

                    shoulder.localRotation = UpperArm2.localRotation;
                    var vechnd = hand.position - shoulder.position;
                    shoulder.rotation = Quaternion.FromToRotation(vechnd.normalized, tgtv.normalized) * shoulder.rotation;


                    var q = Quaternion.FromToRotation((elbow.position - shoulder.position).normalized, vechnd.normalized) * shoulder.rotation;
                    var q1 = Quaternion.Inverse( shoulder.parent.rotation ) * q;
                    var q2 = Quaternion.Inverse( shoulder.rotation ) * q;

#if DEBUG
                    _Gizmo_dbg.Visible = true;

                    _Gizmo_dbg.transform.rotation = shoulder.rotation;
                    _Gizmo_dbg.transform.localRotation = q2;
                    _Gizmo_dbg.transform.position = shoulder.position + (vechnd / 2);
                    //_Gizmo_dbg.transform.LookAt(elbow, _Gizmo_dbg.transform.up);
                    debugPrintConsoleSp(string.Format("q2: {0}, {1}, {2}", q1.eulerAngles.x, q1.eulerAngles.y, q1.eulerAngles.z));
#endif

                    return;
                }
            }
#endif
            // 手指のブレンド v0030
            public void lateBlendHand()
            {
                Maid slave = (mdSlave_No >= 0 && mdSlaves.Count() > mdSlave_No) ? mdSlaves[mdSlave_No].mem : null;
                if (!slave || !slave.body0)
                    return;

                var p_mscfg = cfgs[num_];
                if (p_mscfg.doBlendHandR)
                {
                    if (blendHandR == null || blendHandR.maid != slave)
                    {
                        blendHandR = new XtHandMgr.BlendMgr(slave, true);
                    }

                    blendHandR.fBlend = p_mscfg.fBlendHandR;
                    blendHandR.fOpen = p_mscfg.fBlendHandROpen;
                    blendHandR.fGrip = p_mscfg.fBlendHandRGrip;
                    blendHandR.animOn = p_mscfg.doAnimeHandR;
                    blendHandR.animRange = p_mscfg.fAnimeHandRMove;
                    blendHandR.animSpeed = p_mscfg.fAnimeHandRSpeed;

                    blendHandR.Apply(p_mscfg.doBlendHandR);
                }
                if (p_mscfg.doBlendHandL)
                {
                    if (blendHandL == null || blendHandL.maid != slave)
                    {
                        blendHandL = new XtHandMgr.BlendMgr(slave, false);
                    }

                    blendHandL.fBlend = p_mscfg.fBlendHandL;
                    blendHandL.fOpen = p_mscfg.fBlendHandLOpen;
                    blendHandL.fGrip = p_mscfg.fBlendHandLGrip;
                    blendHandL.animOn = p_mscfg.doAnimeHandL;
                    blendHandL.animRange = p_mscfg.fAnimeHandLMove;
                    blendHandL.animSpeed = p_mscfg.fAnimeHandLSpeed;

                    blendHandL.Apply(p_mscfg.doBlendHandL);
                }

                /*足の痙攣テスト
                if (p_mscfg.doAnimeHandR)
                {
                    var trTh = BoneLink.BoneLink.SearchObjName(slave.body0.m_trBones.transform, "Bip01 R Thigh", true);
                    if (trTh)
                    {
                        var rot_ = rot.eulerAngles;
                        var dx = Mathf.SmoothStep(0, Mathf.Clamp01(p_mscfg.fBlendHandLOpen), UnityEngine.Random.Range(0f, 1f)) * UnityEngine.Random.Range(0, 120f);
                        var dy = Mathf.SmoothStep(0, Mathf.Clamp01(p_mscfg.fBlendHandLGrip), UnityEngine.Random.Range(0f, 1f)) * UnityEngine.Random.Range(0, 120f);
                        trTh.localRotation = Quaternion.Euler(rot_.x + dx, rot_.y + dy, rot_.z);

                        trTh = BoneLink.BoneLink.SearchObjName(slave.body0.m_trBones.transform, "Bip01 L Thigh", true);
                        rot_ = trTh.localRotation.eulerAngles;
                        trTh.localRotation = Quaternion.Euler(rot_.x - dx, rot_.y - dy, rot_.z);
                        slave.body0.AutoTwist();
                    }
                }

                if (p_mscfg.doAnimeHandR)
                {
                    float paramX = 0; // 痙攣の大きさX
                    float paramY = 0; // 痙攣の大きさY

                    paramX = p_mscfg.fBlendHandLOpen; // 痙攣の大きさX
                    paramY = p_mscfg.fBlendHandLGrip; // 痙攣の大きさY

                    var dx = Mathf.SmoothStep(0, Mathf.Clamp01(p_mscfg.fBlendHandLOpen), UnityEngine.Random.Range(0f, 1f)) * UnityEngine.Random.Range(0, 90f);
                    var dy = Mathf.SmoothStep(0, Mathf.Clamp01(p_mscfg.fBlendHandLGrip), UnityEngine.Random.Range(0f, 1f)) * UnityEngine.Random.Range(0, 90f);


                    var trTh = CMT.SearchObjName(slave.body0.m_trBones.transform, "Bip01 R Thigh", true);
                    var rot = trTh.localRotation.eulerAngles;
                    trTh.localRotation = Quaternion.Euler(rot.x + dx, rot.y + dy, rot.z);

                    trTh = CMT.SearchObjName(slave.body0.m_trBones.transform, "Bip01 L Thigh", true);
                    rot = trTh.localRotation.eulerAngles;
                    trTh.localRotation = Quaternion.Euler(rot.x - dx, rot.y - dy, rot.z);
                    slave.body0.AutoTwist();
                }*/
            }

            public void lateHandsAtcpProc()
            {
                Maid slave = this.GetSlave();
                if (!slave || !slave.body0)
                    return;

                if (lateHandsAtcpProc2(slave) && !IsHookAutoTwist)
                {
#if DEBUG
                    if (Input.GetKey(KeyCode.RightAlt))
                        slave.body0.AutoTwist();
#endif
                }
            }

            // 戻り値: ツイスト必要か
            public bool lateHandsAtcpProc2(Maid slave)
            {
                bool needTwist = false;
                //bkupHandRotR = false;
                //bkupHandRotL = false;
                
                var p_mscfg = cfgs[num_];
                var p_v3of = v3ofs[num_];
                var p_v3of2 = new v3OffsetsV2(v3ofs[num_], p_mscfg);

                Maid master = null;
                if (doMasterSlave)
                    master = this.GetMaster();

                if (!p_mscfg.doIKTargetMHandSpCustom)
                {
                    if (master && p_mscfg.doIKTargetMHand) // v5.0
                    {
                        if (!p_mscfg.doIK159NewPointToDef && p_mscfg.doIK159RotateToHands) 
                        {
                            // v5.0 SetHandIKRotate同様のグローバル角度での調整
                            Transform trh0 = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, GetHandBnR(master), true);
                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                            if (!IsHookActive && !IkXT.IsNewPointIK(slave, "右手") && IkXT._inst.IKCmoUpdate(slave.body0, trh, p_v3of.v3HandROffset, "右手"))
                            {
                                trh.rotation = trh0.rotation * Quaternion.Euler(p_v3of.v3HandROffsetRot);
                                slave.body0.setR_vechand(trh0.rotation * p_v3of.v3HandROffset);

                                // IK適用済みのはずなので解除
                                slave.body0._ikp().tgtMaidR = null;
                                slave.body0._ikp().tgtHandR = null;
                            }
                            else if (IsHookActive)
                            {
                                trh.rotation = trh0.rotation * Quaternion.Euler(p_v3of.v3HandROffsetRot);
                                slave.body0.setR_vechand(trh0.rotation * p_v3of.v3HandROffset);
                            }

                            trh0 = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, GetHandBnL(master), true);
                            trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                            if (!IsHookActive && !IkXT.IsNewPointIK(slave, "左手") && IkXT._inst.IKCmoUpdate(slave.body0, trh, p_v3of.v3HandLOffset, "左手"))
                            {
                                trh.rotation = trh0.rotation * Quaternion.Euler(p_v3of.v3HandLOffsetRot);
                                slave.body0.setL_vechand(trh0.rotation * p_v3of.v3HandLOffset);

                                // IK適用済みのはずなので解除
                                slave.body0._ikp().tgtMaidL = null;
                                slave.body0._ikp().tgtHandL = null;
                            }
                            else if (IsHookActive)
                            {
                                trh.rotation = trh0.rotation * Quaternion.Euler(p_v3of.v3HandLOffsetRot);
                                slave.body0.setL_vechand(trh0.rotation * p_v3of.v3HandLOffset);
                            }

                            //slave.body0.AutoTwist();
                            needTwist = true;
                        }
                    }
                    return needTwist;
                }

                // ここからカスタムアタッチ
                //debugPrintConsole("cus-1");

                //if (!doMasterSlave || cfgs[num_].doStackSlave_PosSyncMode)
                {
                    // v0025 手の角度調整(アタッチ版)
                    //if (p_v3of.v3HandROffsetRot != Vector3.zero)
                    {
                        var ikp = slave.body0._ikp();

                        if (p_mscfg.doIKTargetMHandSpR_TgtChar != ATgtChar.None
                        && ikp.tgtMaidR && ikp.tgtHandR_AttachSlot >= 0 && !string.IsNullOrEmpty(ikp.tgtHandR_AttachName))
                        {
                            //debugPrintConsole("cus-2 " + ikp.tgtHandR_AttachSlot + " " + ikp.tgtHandR_AttachName);

                            // 右手
                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                            //bkupHandRotR = true;
                            //bkupHandRotRq = trh.rotation;

                            if (p_mscfg.doIKTargetMHandSpCustomAltRotR)
                            {
                                var iks = slave.body0._ikp();
                                //if (true/*iks.tgtHandR_AttachName != string.Empty && slave.body0.goSlot[iks.tgtHandR_AttachSlot].morph != null*/)
                                if (iks != null && iks.tgtHandR_AttachSlot >= 0)
                                {
                                    if (iks.tgtMaidR != null && iks.tgtMaidR.body0 != null && iks.tgtMaidR.body0.goSlot[iks.tgtHandR_AttachSlot].morph != null)
                                    {
                                        Vector3 vector2;
                                        Quaternion rotation2;
                                        iks.tgtMaidR.body0.goSlot[iks.tgtHandR_AttachSlot].morph.GetAttachPoint(iks.tgtHandR_AttachName, out vector2, out rotation2);
                                        if (p_mscfg.doIKTargetMHandSpCustom_v2 && !p_mscfg.doIK159NewPointToDef)
                                        {
                                            if (!IsHookActive && IkXT._inst.IKCmoUpdate(slave.body0, trh, p_v3of2.v3HandROffset, "右手"))
                                            {
                                                // IK適用済みのはずなので解除
                                                slave.body0._ikp().tgtMaidR = null;
                                                slave.body0._ikp().tgtHandR = null;
                                                AtccHand1R(slave, null, null, Vector3.zero);
                                            }
                                        }

                                        // アタッチポイント基準角度
                                        trh.rotation = rotation2 * Quaternion.Euler(p_v3of2.v3HandROffsetRot);

                                        /*// アタッチポイント基準
                                        var q0 = rotation2;
                                        var q1 = q0 * Quaternion.Euler(p_v3of.v3HandROffsetRot);
                                        q0 = Quaternion.Inverse(trh.parent.rotation) * q0;
                                        // 目的の角度に
                                        trh.rotation = q1 * Quaternion.Inverse(q0);
                                        */
                                        // IKリミットチェック
                                        var rm = trh.gameObject.transform.gameObject.GetComponent<RootMotion.FinalIK.RotationLimit>();
                                        if (rm)
                                        {
                                            bool flag;
                                            var q = rm.GetLimitedLocalRotation(trh.localRotation, out flag);
                                            if (flag)
                                            {
                                                trh.localRotation = q;
                                            }
                                            //slave.body0.setR_vechand(trh.rotation * p_v3of.v3HandROffset);
                                            //slave.body0.setR_vechand(rotation2 * p_v3of.v3HandROffset);
                                        }
                                        /*else
                                        {
                                            //モーションチェック                                            
                                            bool motion_stop = true;
                                            Animation animation = slave.body0.m_Bones.GetComponent<Animation>();
                                            if (animation != null)
                                            {
                                                var anime_state = animation[slave.body0.LastAnimeFN.ToLower()];
                                                if (anime_state != null && anime_state.length != 0f)
                                                {
                                                    motion_stop = !anime_state.enabled;
                                                }
                                            }

                                            if (!motion_stop)
                                            {
                                                _ikR.IkProc(vector2, rotation2 * p_v3of.v3HandROffset);
                                                // パラメータ上書き
                                                //slave.body0.owIkParam(true, trh.rotation * p_v3of.v3HandROffset, _ikR.Forearm.position);
                                                slave.body0.owIkParam(true, rotation2 * p_v3of.v3HandROffset, _ikR.Forearm.position);
                                            }
                                            else
                                            {
                                                // モーション停止中は使えない
                                                //slave.body0.setR_vechand(trh.rotation * p_v3of.v3HandROffset);
                                                slave.body0.setR_vechand(rotation2 * p_v3of.v3HandROffset);
                                            }
                                        }*/
                                        //slave.body0.setR_vechand(trh.rotation * p_v3of.v3HandROffset);

                                        // IK補正上書き
                                        slave.body0.setR_vechand(rotation2 * p_v3of2.v3HandROffset);
                                        //slave.body0.setR_vechand(rotation2 * p_v3of.v3HandROffset);

                                        needTwist = true;

                                        //slave.body0.AutoTwist();
                                        //if (cfg.HandIKsp_UseVechand)
                                        //    slave.body0.setR_vechand(trh.rotation * p_v3of.v3HandROffset);
                                    }
                                }
                            }
                            else if (p_mscfg.doIKTargetMHandSpCustom_v2) // v5.0
                            {
                                var iks = slave.body0._ikp();
                                if (iks.tgtMaidR != null && iks.tgtMaidR.body0 != null && iks.tgtMaidR.body0.goSlot[iks.tgtHandR_AttachSlot].morph != null)
                                {
                                    Vector3 vector;
                                    Quaternion rotation;
                                    iks.tgtMaidR.body0.goSlot[iks.tgtHandR_AttachSlot].morph.GetAttachPoint(iks.tgtHandR_AttachName, out vector, out rotation);

                                    if (!p_mscfg.doIK159NewPointToDef)
                                    {
                                        if (!IsHookActive && IkXT._inst.IKCmoUpdate(slave.body0, trh, p_v3of2.v3HandROffset, "右手"))
                                        {
                                            // IK適用済みのはずなので解除
                                            slave.body0._ikp().tgtMaidR = null;
                                            slave.body0._ikp().tgtHandR = null;
                                            AtccHand1R(slave, null, null, Vector3.zero);
                                        }
                                    }

                                    trh.LookAt(vector, trh.transform.parent.up);
                                    //trh.LookAt(vector, trh.transform.parent.position - trh.transform.position);
                                    trh.rotation *= Quaternion.Euler(p_v3of2.v3HandROffsetRot);

                                    /*if (!p_mscfg.doIK159NewPointToDef)
                                        slave.body0.setR_vechand(trh.rotation * p_v3of.v3HandROffset);*/
                                    //slave.body0.AutoTwist();
                                    needTwist = true;
                                }
                            }
                            else if (doMasterSlave && p_mscfg.doIK159NewPointToDef && p_mscfg.doIK159RotateToHands) // v5.0
                            {
                                trh.localRotation *= Quaternion.Euler(p_v3of2.v3HandROffsetRot);
                                //slave.body0.AutoTwist();
                                needTwist = true;
                            }
                            else if (!doMasterSlave || !cfgs[num_].doStackSlave || !cfgs[num_].doIKTargetMHand) // ボーンリンク中は二重補正になるのでしない
                            {
                                slave.body0.setR_vechand(trh.rotation * p_v3of2.v3HandROffset);
                                trh.localRotation *= Quaternion.Euler(p_v3of2.v3HandROffsetRot);
                                // IK補正上書き
                                //if (cfg.HandIKsp_UseVechand)
                                needTwist = true;
                            }
                        }
                        else if (ikp.tgtHandR && p_mscfg.doIKTargetMHandSpR_TgtChar != ATgtChar.None) // v5.0
                        {
                            //debugPrintConsole("cus-3");

                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);

                            bool reset = !IsHookActive && IkXT._inst.IKCmoUpdate(slave.body0, trh, p_v3of2.v3HandROffset, "右手");

                            if (p_mscfg.doIKTargetMHandSpCustomAltRotR)
                            {
                                //debugPrintConsole("cus-3a");
                                Transform trh0 = ikp.tgtHandR;
                                trh.rotation = trh0.rotation * Quaternion.Euler(p_v3of2.v3HandROffsetRot);
                                slave.body0.setR_vechand(trh0.rotation * p_v3of2.v3HandROffset);
                            }
                            else
                            {
                                slave.body0.setR_vechand(trh.rotation * p_v3of2.v3HandROffset);
                                trh.localRotation *= Quaternion.Euler(p_v3of2.v3HandROffsetRot);
                            }
                            needTwist = true;

                            if (reset)
                            {
                                // IK適用済みのはずなので解除
                                AtccHand1R(slave, null, null, Vector3.zero);
                            }
                        }
                        else if (IsHookActive && master && p_mscfg.doIKTargetMHand) // v5.0
                        {
                            Transform trh0 = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, GetHandBnR(master), true);
                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                            trh.rotation = trh0.rotation * Quaternion.Euler(p_v3of.v3HandROffsetRot);
                            slave.body0.setR_vechand(trh0.rotation * p_v3of.v3HandROffset);
                            needTwist = true;
                        }
                    }

                    //if (p_v3of.v3HandLOffsetRot != Vector3.zero)
                    {
                        var ikp = slave.body0._ikp();

                        if (p_mscfg.doIKTargetMHandSpL_TgtChar != ATgtChar.None
                        && ikp.tgtMaidL && ikp.tgtHandL_AttachSlot >= 0 && !string.IsNullOrEmpty(ikp.tgtHandL_AttachName))
                        {
                            // 左手
                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                            //bkupHandRotL = true;
                            //bkupHandRotLq = trh.rotation;

                            if (p_mscfg.doIKTargetMHandSpCustomAltRotL)
                            {
                                var iks = slave.body0._ikp();
                                //if (true/*iks.tgtHandL_AttachName != string.Empty && slave.body0.goSlot[iks.tgtHandL_AttachSlot].morph != null*/)
                                if (iks != null && iks.tgtHandL_AttachSlot >= 0)
                                {
                                    if (iks.tgtMaidL != null && iks.tgtMaidL.body0 != null && iks.tgtMaidL.body0.goSlot[iks.tgtHandL_AttachSlot].morph != null)
                                    {
                                        Vector3 vector;
                                        Quaternion rotation;
                                        iks.tgtMaidL.body0.goSlot[iks.tgtHandL_AttachSlot].morph.GetAttachPoint(iks.tgtHandL_AttachName, out vector, out rotation);
                                        if (p_mscfg.doIKTargetMHandSpCustom_v2 && !p_mscfg.doIK159NewPointToDef)
                                        {
                                            if (!IsHookActive && IkXT._inst.IKCmoUpdate(slave.body0, trh, p_v3of2.v3HandLOffset, "左手"))
                                            {
                                                // IK適用済みのはずなので解除
                                                slave.body0._ikp().tgtMaidL = null;
                                                slave.body0._ikp().tgtHandL = null;
                                                AtccHand1L(slave, null, null, Vector3.zero);
                                            }
                                        }

                                        // アタッチポイント基準角度
                                        trh.rotation = rotation * Quaternion.Euler(p_v3of2.v3HandLOffsetRot);

                                        // IKリミットチェック
                                        var rm = trh.gameObject.transform.gameObject.GetComponent<RootMotion.FinalIK.RotationLimit>();
                                        if (rm)
                                        {
                                            bool flag;
                                            var q = rm.GetLimitedLocalRotation(trh.localRotation, out flag);
                                            if (flag)
                                            {
                                                trh.localRotation = q;
                                            }
                                        }

                                        // IK補正上書き
                                        slave.body0.setL_vechand(rotation * p_v3of2.v3HandLOffset);
                                        //slave.body0.AutoTwist();
                                        needTwist = true;

                                        //if (cfg.HandIKsp_UseVechand)
                                        //    slave.body0.setL_vechand(trh.rotation * p_v3of.v3HandLOffset);
                                    }
                                }
                            }
                            else if (p_mscfg.doIKTargetMHandSpCustom_v2) // v5.0
                            {
                                var iks = slave.body0._ikp();
                                if (iks.tgtMaidL != null && iks.tgtMaidL.body0 != null && iks.tgtMaidL.body0.goSlot[iks.tgtHandL_AttachSlot].morph != null)
                                {
                                    Vector3 vector;
                                    Quaternion rotation;
                                    iks.tgtMaidL.body0.goSlot[iks.tgtHandL_AttachSlot].morph.GetAttachPoint(iks.tgtHandL_AttachName, out vector, out rotation);

                                    if (!p_mscfg.doIK159NewPointToDef)
                                    {
                                        if (!IsHookActive && IkXT._inst.IKCmoUpdate(slave.body0, trh, p_v3of2.v3HandLOffset, "左手"))
                                        {
                                            // IK適用済みのはずなので解除
                                            slave.body0._ikp().tgtMaidL = null;
                                            slave.body0._ikp().tgtHandL = null;
                                            AtccHand1L(slave, null, null, Vector3.zero);
                                        }
                                    }

                                    trh.LookAt(vector, trh.transform.parent.up);
                                    //trh.LookAt(vector, trh.transform.parent.position - trh.transform.position);
                                    trh.rotation *= Quaternion.Euler(p_v3of2.v3HandLOffsetRot);
                                    /*
                                    if (!p_mscfg.doIK159NewPointToDef)
                                        slave.body0.setL_vechand(trh.rotation * p_v3of.v3HandLOffset);*/
                                    //slave.body0.AutoTwist();
                                    needTwist = true;
                                }
                            }
                            else if (doMasterSlave && p_mscfg.doIK159NewPointToDef && p_mscfg.doIK159RotateToHands) // v5.0
                            {
                                trh.localRotation *= Quaternion.Euler(p_v3of2.v3HandLOffsetRot);
                                //slave.body0.AutoTwist();
                                needTwist = true;
                            }
                            else if (!doMasterSlave || !cfgs[num_].doStackSlave || !cfgs[num_].doIKTargetMHand) // ボーンリンク中は二重補正になるのでしない
                            {
                                slave.body0.setL_vechand(trh.rotation * p_v3of2.v3HandLOffset);
                                trh.localRotation *= Quaternion.Euler(p_v3of2.v3HandLOffsetRot);
                                // IK補正上書き
                                //if (cfg.HandIKsp_UseVechand)
                                needTwist = true;
                            }
                        }
                        else if (ikp.tgtHandL && p_mscfg.doIKTargetMHandSpL_TgtChar != ATgtChar.None) // v5.0
                        {
                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                            bool reset = !IsHookActive && IkXT._inst.IKCmoUpdate(slave.body0, trh, p_v3of2.v3HandLOffset, "左手");

                            if (p_mscfg.doIKTargetMHandSpCustomAltRotL)
                            {
                                Transform trh0 = ikp.tgtHandL;
                                /*
                                Maid tgt = GetHandAtcTgt(true, slave, master, p_mscfg);
                                if (tgt && !string.IsNullOrEmpty(p_mscfg.doIKTargetMHandSpL_TgtBone))
                                {
                                    var tgtm = tgt;
                                    var atcpTgt = p_mscfg.doIKTargetMHandSpL_TgtBone;
                                    trh0 = BoneLink.BoneLink.SearchObjName(tgtm.body0.m_Bones.transform, atcpTgt.Remove(0, Defines.data.comboBonePrefix.Length), true);
                                }*/

                                trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                                trh.rotation = trh0.rotation * Quaternion.Euler(p_v3of2.v3HandLOffsetRot);
                                slave.body0.setL_vechand(trh0.rotation * p_v3of2.v3HandLOffset);
                            }
                            else
                            {
                                slave.body0.setL_vechand(trh.rotation * p_v3of2.v3HandLOffset);
                                trh.localRotation *= Quaternion.Euler(p_v3of2.v3HandLOffsetRot);
                            }
                            needTwist = true;

                            if (reset)
                            {
                                // IK適用済みのはずなので解除
                                AtccHand1L(slave, null, null, Vector3.zero);
                            }
                        }
                        else if (IsHookActive && master && p_mscfg.doIKTargetMHand)
                        {
                            Transform trh0 = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, GetHandBnL(master), true);
                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                            trh.rotation = trh0.rotation * Quaternion.Euler(p_v3of.v3HandLOffsetRot);
                            slave.body0.setL_vechand(trh0.rotation * p_v3of.v3HandLOffset);
                            needTwist = true;
                        }
                    }
                }
                return needTwist;
            }

            /*
            internal void postHandsAtcpProc()
            {
                Maid slave = (mdSlave_No >= 0 && mdSlaves.Count() > mdSlave_No) ? mdSlaves[mdSlave_No].mem : null;
                if (!slave || !slave.body0)
                    return;

                debugPrintConsoleSp("復元");

                // 右手
                if (bkupHandRotR)
                {
                    Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                    trh.rotation = bkupHandRotRq;
                }

                // 左手
                if (bkupHandRotL)
                {
                    Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                    trh.rotation = bkupHandRotLq;
                }
            }*/

            //リンク可能な場面のみ
            public void linkProc()
            {
                //更新
                MsUpdate(true, true);

                //現ページのMSコンフィグ
                var cfg_ = cfgs[num_];

                //回想・夜伽モード中のサブメイドキープ
                if (XtMasterSlave.IsKeepScene()/*(vIsKaisouScene || bIsYotogiScene)*/ && maidKeepSlaveYotogi && !Scc1_MasterMaid && cfg_.doKeepSlaveYotogi
                    && GameMain.Instance.CharacterMgr.GetMaid(0) != maidKeepSlaveYotogi)
                {
                    if (/*maidKeepSlaveKaisou != null && */!doMasterSlave && do_master && do_master.Visible)
                    {
                        if (mdSlave_No < 0 || mdSlaves[mdSlave_No].mem != maidKeepSlaveYotogi)
                        {
                            //指定メイドがロードされてなければ読み込み
                            bool loaded = false;
                            int num = 0;
                            foreach (var mt in mdSlaves)
                            {
                                if (mt.mem == maidKeepSlaveYotogi && mt.mem.Visible)
                                {
                                    loaded = true;
                                    break;
                                }
                                num++;
                            }

                            if (!loaded)
                            {
                                if (!GameMain.Instance.CharacterMgr.IsBusy() && GameMain.Instance.CharacterMgr.GetMaid(0) != null)
                                {
                                    if (LoadMaid(maidKeepSlaveYotogi))
                                    {
                                        debugPrintConsole("きーぷめいどろーど完了・・・@link " + num_ + " mid:" + do_master.GetInstanceID());

                                        //表情、モーションの初期化（モーション変化検出用）
                                        if (!bIsYotogiScene) //回想モード対応(夜伽はLoadMaidで変更される）
                                            maidKeepSlaveYotogi.FaceAnime(VymModule.cfg.sFaceAnimeYotogiDefault, 0f);
                                        //maidKeepSlaveYotogi.CrossFadeAbsolute("maid_stand01.anm", false, true, false, 0f, 1f); //一応
                                        this.keepSI.SaveLastInfo(maidKeepSlaveYotogi);

                                        _StockMaids_Visible.Add(maidKeepSlaveYotogi);
                                        _YtgKeepMaids_Visible.Add(maidKeepSlaveYotogi);
                                    }
                                    else
                                    {
                                        Console.WriteLine("XtMasterSlave : サブメイドのロードに失敗しました");
                                        maidKeepSlaveYotogi = null;
                                    }
                                    /*
                                    GetMaids();
                                    num = 0;
                                    foreach (var m in MaidList)
                                    {
                                        if (m.mem == maidKeepSlaveKaisou)
                                        {
                                            mdSlave_No = num;
                                            doMasterSlave = true;
                                        }
                                        num++;
                                    }*/
                                }
                                else
                                {
                                    if (Time.frameCount % 30 == 0) //30フレームに1回表示
                                        Console.WriteLine("XtMasterSlave : CharacterMgrがBusyのため待機中");
                                }
                                return;
                            }
                            else
                            {
                                if (do_master && do_master.Visible)
                                {
                                    if (mdMaster_No < 0)
                                    { //マスターを探す
                                        debugPrintConsole("マスター捜索: " + num_);
                                        if (chkMemNo(mdMasters, ref mdMaster_No, do_master))
                                            FixMaster();
                                    }
                                    if (mdMaster_No >= 0)
                                    {
                                        debugPrintConsole("スレイブセット: " + num_);
                                        mdSlave_No = num;
                                        doMasterSlave = true;

                                        //変更を保持
                                        //SaveMsLast();
                                        FixSlave();
                                    }
                                }
                            }
                        }
#if DEBUG
                        else if (!doMasterSlave)
                        {
                            if (do_master && !do_master.Visible)
                            {
                                debugPrintConsole("ますたーみえない・・・@link " + num_ + " mid:" + do_master.GetInstanceID());
                            }
                            if (_MensList.Count > 1)
                            {
                                debugPrintConsole("これはだれ？・・・@link " + num_ + " mid1:" + _MensList[1].mem.GetInstanceID());
                            }
                            if (_MensList.Count > 2)
                            {
                                debugPrintConsole("これはだれ？・・・@link " + num_ + " mid2:" + _MensList[2].mem.GetInstanceID());
                            }
                        }
#endif
                    }
                    else if (do_master && !do_master.Visible)
                    {
                        //maidKeepSlaveYotogi.Visible = false;
                        if (doMasterSlave || maidKeepSlaveYotogi.Visible)
                        {
                            debugPrintConsole("ますたーいなくなった・・・@link " + num_ + " mid:" + do_master.GetInstanceID());
                            if (maidKeepSlaveYotogi.Visible && !keepSI.CheckMoved(maidKeepSlaveYotogi, num_))
                            {
                                if (_YtgKeepMaids_Visible.Contains(maidKeepSlaveYotogi))
                                {
                                    Console.WriteLine("Link:{0} Masterが非表示になったため、Slaveを表示解除", num_);
                                    maidKeepSlaveYotogi.Visible = false;    //スレイブキーパーが表示させた場合
                                }
                                else
                                {
                                    Console.WriteLine("[!] ゲームシステムによる表示変更を検知したためM-Sリンク<" + num_ + ">を中断");
                                    maidKeepSlaveYotogi = null;
                                }
                            }
                            doMasterSlave = false;
                        }
                        else if (keepSI.CheckMoved(maidKeepSlaveYotogi, num_))
                        {
                            //リンク中断?
                            if (!maidKeepSlaveYotogi.Visible)
                            {
                                debugPrintConsole("もどらないと・・・@link " + num_);
                                maidKeepSlaveYotogi.Visible = true;
                            }
                        }

                        //mdSlave_No = -1;
                        //FixSlave();
                        //このまま進むとキープ解除されるので戻る
                        return;
                    }

                    if (keepSI.CheckMoved(maidKeepSlaveYotogi, num_))
                    {
                        //リンク中断?
                    }
                }
                else
                {
                    if (maidKeepSlaveYotogi != null)
                        maidKeepSlaveYotogi = null;
                }

                //マスタースレイブの更新
                MsUpdate2(true);

                //未選択状態のチェック
                if (mdMaster_No < 0 || mdSlave_No < 0)
                {
                    if (doMasterSlave)
                    {
                        debugPrintConsole("未選択状態 Link解消 " + num_);
                        doMasterSlave = false;
                    }

                    if (maidKeepSlaveYotogi != null)
                    {
                        debugPrintConsole("未選択状態 Keep解消 " + num_);
                        maidKeepSlaveYotogi = null;
                    }
                    return;
                }

                Maid master = mdMasters[mdMaster_No].mem;
                //if (master)
                //    keepManAlpha(master);

                Maid slave = mdSlaves[mdSlave_No].mem;
                if (!master || !slave || !master.body0 || !slave.body0 || !master.body0.m_Bones || !slave.body0.m_Bones)
                    return;

                //v0031 局部サイズチェック
                if (master.boMAN)
                    FixChinkoScaleInUpdate(master.body0);
                else if (slave.boMAN)
                    FixChinkoScaleInUpdate(slave.body0);

                //ボーンリンク実行
#if DEBUG
                if (!cfg_.doStackSlave_PosSyncMode && !Input.GetKey(KeyCode.U))
#else
                if (!cfg_.doStackSlave_PosSyncMode)
#endif
                {
                    boneLink.Try(master, slave, doMasterSlave,
                        (cfg_.doStackSlave && cfg_.doStackSlave_Pelvis),    //骨盤補正
                        (cfg_.doStackSlave && cfg_.doStackSlave_CliCnk),    //局部補正
                        Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffset,      //補正座標 
                        v3ofs[num_], cfg_);                         //手のアタッチ調整用
                    //v3ofs[num_].v3StackOffset2Bip(slave, cfg_.doStackSlave && (cfg_.doStackSlave_Pelvis || cfg_.doStackSlave_CliCnk)));                         //補正座標
                }
                //else
                //    BoneLink.BoneLink.TryPos(master, slave, doMasterSlave, (cfg_.doStackSlave && cfg_.doStackSlave_Pelvis));

                //後処理もあるのでtry後に戻る
                if (!doMasterSlave)
                    return;

                //ボイスプレイ
                if (cfg_.doVoiceAndFacePlay && (bIsYotogiScene || (bIsVymPlg && cfg_.doVoiceAndFacePlayOnVYM) || cfg_.doManualVfPlay) && CheckPlayMaids())
                {
                    if (!slave.boMAN)
                    {
                        //Maid vm = mdSlaves[0].mem;
                        bool motionChanged;
                        var stat = animeState.chk_motion_state(master.body0, false, out motionChanged);

                        int iExcite = GetExciteCurMaid();

                        if (cfg_.doManualVfPlay)
                        {
                            iExcite = cfg_.manuVf_iExcite;

                            if (this.manuKyoseiZeccho > 0)
                            {
                                if (slave.AudioMan.audiosource.loop || (!slave.AudioMan.audiosource.loop && !slave.AudioMan.audiosource.isPlaying))
                                {
                                    //音声割り込みタイミングで絶頂させる
                                    stat = AnimeState.State.zeccho;
                                    motionChanged = true;
                                    this.manuKyoseiZeccho--;
                                }
                            }
                        }
                        //VYMModule.VymModule.MaidVoicePlay(slave, vm.Param.status.cur_excite, stat, motionChanged, !cfg_.doFaceSync);
                        VYMModule.VymModule.MaidVoicePlay(slave, iExcite, stat, motionChanged, !cfg_.doFaceSync, !cfg_.doVoiceDisabled, (!bIsYotogiScene && bIsVymPlg),
                                                      cfg_.doManualVfPlay, cfg_, ref bVoicePlaying);

                        MouthMode = 0;
                        if (stat == AnimeState.State.kiss)
                        {
                            MouthMode = 1;//kiss
                                          //debugPrintConsole("stat = kiss");
                        }

                        // 絶頂痙攣β
                        if (cfg_.doZecchoKeiren && (stat == AnimeState.State.zeccho || this.manuKyoseiZeccho > 0))
                        {
                            float paramX = cfg_.fZecchoKeirenParam; // 痙攣の大きさX
                                                                    //float paramY = cfg_.fZecchoKeirenParam; // 痙攣の大きさY

                            var dy = Mathf.SmoothStep(0, Mathf.Clamp01(paramX), UnityEngine.Random.Range(0f, 1f)) * UnityEngine.Random.Range(0, 30f);
                            //var dy = Mathf.SmoothStep(0, Mathf.Clamp01(paramY), UnityEngine.Random.Range(0f, 1f)) * UnityEngine.Random.Range(0, 90f);
                            var dx = dy / 2;

                            var trTh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, "Bip01 R Thigh", true);
                            var rot = trTh.localRotation.eulerAngles;
                            trTh.localRotation = Quaternion.Euler(rot.x + dx, rot.y + dy, rot.z);

                            trTh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, "Bip01 L Thigh", true);
                            rot = trTh.localRotation.eulerAngles;
                            trTh.localRotation = Quaternion.Euler(rot.x - dx, rot.y - dy, rot.z);

                            //不要 v5.0 slave.body0.AutoTwist();
                        }
                    }
                }

                //表情同期
                if (cfg_.doFaceSync && !slave.boMAN && mdSlave_No > 0)
                {
                    slave.FaceAnime(mdSlaves[0].mem.ActiveFace, 1, 0);
                    slave.FaceBlend(mdSlaves[0].mem.FaceName3);
                    if (mdSlaves[0].mem.FaceName3.Contains("オリジナル"))
                    {
                        FaceBlend2Sync(mdSlaves[0].mem.body0.Face.morph, mdSlaves[mdSlave_No].mem.body0.Face.morph, false);
                    }
                }

                //位置合わせ
                if (cfg_.doStackSlave)
                {
                    if (lastSlaveStacked != slave)
                    {
                        //アタッチの解除
                        if (lastSlaveStacked && (cfg_.doIKTargetMHand || cfg_.doCopyIKTarget))
                        {
                            lastSlaveStacked.IKTargetClear();
                        }
                        lastSlaveStacked = slave;
                    }

                    //地面高さ
                    if (ComExt.IsCom3d2)
                    {
#if COM3D2
                        if (cfg.AdjustBoneHitHeightY)
                        {
                            //オフセット設定有り

                            Transform trFloor;
                            if ((vSceneLevel == 15 || vIsKaisouScene || bIsYotogiScene) && (mdSlave_No > 0 && mdSlaves[0].mem && !mdSlaves[0].mem.boMAN))
                            {
                                //イベントシーンかつカレントメイドが別にいる
                                trFloor = mdSlaves[0].mem.body0.m_trFloorPlane;
                            }
                            else
                            {
                                trFloor = master.body0.m_trFloorPlane;
                            }
                            float posY = 0f;
                            if (trFloor)
                            {
                                // COMでのBoneHitHeightY取得処理
                                // MANではm_trFloorPlaneがnullの場合もあるぽいのでgetが使えないことがある
                                posY = trFloor.position.y;
                            }

                            if (posY > (master.gameObject.transform.position.y))
                            {
                                //ソファー対策
                                posY = master.gameObject.transform.position.y;
                            }

                            if (cfg_.Adjust_doHitHeightYOffset)
                            {
                                //オフセット設定有り
                                posY += cfg_.Adjust_HitHeightYOffset;
                            }
                            slave.body0.SetBoneHitHeightY(posY);
                        }
#endif
                    }
                    else if (cfg.AdjustBoneHitHeightY)
                    {
                        slave.body0.SetBoneHitHeightY(master.body0.BoneHitHeightY);
                        if ((vSceneLevel == 15 || vIsKaisouScene || bIsYotogiScene) && (mdSlave_No > 0 && mdSlaves[0].mem && !mdSlaves[0].mem.boMAN))
                        {
                            //イベントシーンかつカレントメイドが別にいる
                            slave.body0.BoneHitHeightY = mdSlaves[0].mem.body0.BoneHitHeightY;
                        }
                        if (slave.body0.BoneHitHeightY > (master.gameObject.transform.position.y))
                        {
                            //ソファー対策
                            //debugPrintConsole("slave.body0.BoneHitHeightY:" + slave.body0.BoneHitHeightY + " > (master.gameObject.transform.position.y:" + master.gameObject.transform.position.y + ")");
                            slave.body0.SetBoneHitHeightY(master.gameObject.transform.position.y);
                        }

                        if (cfg_.Adjust_doHitHeightYOffset)
                        {
                            //オフセット設定有り
                            slave.body0.SetBoneHitHeightY(master.gameObject.transform.position.y + cfg_.Adjust_HitHeightYOffset);
                            //debugPrintConsole("slave.body0.BoneHitHeightY:" + slave.body0.BoneHitHeightY + " / (master.gameObject.transform.position.y:" + master.gameObject.transform.position.y + ")");
                        }
                    }

                    // 角度オフセットを位置位置オフセット前に持ってきてみる ver0025
                    if (cfg_.doStackSlave_PosSyncMode && cfg_.doStackSlave_PosSyncModeSp && !string.IsNullOrEmpty(cfg_.doStackSlave_PosSyncModeSp_TgtBone))
                    {
                        //if (cfg_.doStackSlave_PosSyncModeSp && !string.IsNullOrEmpty(cfg_.doStackSlave_PosSyncModeSp_TgtBone))
                        {
                            // マスターの性転換チェック用
                            string[] bnames = master.boMAN ? Defines.data.ManBones : Defines.data.MaidBones;
                            if (!bnames.Contains(cfg_.doStackSlave_PosSyncModeSp_TgtBone))
                            {
                                cfg_.doStackSlave_PosSyncModeSp_TgtBone = string.Empty;
                                cfg_.doStackSlave_PosSyncModeSp = false;
                            }
                        }

                        if (cfg_.doStackSlave_PosSyncModeSp)
                        {
                            //角度オフセット
                            Transform slvTr;
                            slvTr = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, (cfg_.doStackSlave_PosSyncModeSp_TgtBone), true);

                            /*if (cfg_.doStackSlave_CliCnk || cfg_.doStackSlave_Pelvis)
                                slave.transform.rotation = slvTr.rotation * Quaternion.Euler(0, -90, -90);
                            else*/
                            slave.transform.rotation = slvTr.rotation;
                            slave.transform.localRotation *= Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);
                        }
                    }
                    else if (cfg_.doStackSlave_PosSyncMode && (cfg_.doStackSlave_CliCnk || cfg_.doStackSlave_Pelvis))
                    {
                        //slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation;

                        //角度オフセット
                        Transform slvTr;
                        slvTr = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, (!master.boMAN ? "Bip01" : "ManBip"), true);
                        //var q = Quaternion.Inverse(master.gameObject.transform.rotation) * slvTr.rotation;
                        //slave.gameObject.transform.rotation *= Quaternion.Euler(q * (Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot));

                        slave.transform.rotation = slvTr.rotation * Quaternion.Euler(0, -90, -90);
                        slave.transform.localRotation *= Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);
                    }
                    else if (/*!cfg_.doStackSlave_PosSyncMode && */(cfg_.doStackSlave_CliCnk || cfg_.doStackSlave_Pelvis))
                    {
                        slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation;

                        //角度オフセット
                        Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);

                        // bip基準
                        //var q = Quaternion.Inverse(slave.transform.rotation) * slvTr.rotation;
                        //var q1 = q * Quaternion.Euler((Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot));


#if true
                        // gizmo基準
                        var q = slvTr.rotation * Quaternion.Euler(0, -90, -90);
                        var q1 = q * Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);
                        q = Quaternion.Inverse(slave.transform.rotation) * q;

                        // 目的の角度に
                        slave.transform.rotation = q1 * Quaternion.Inverse(q);
#else
                        var q = Quaternion.Inverse(slave.transform.rotation) * slvTr.rotation * Quaternion.Euler(0, -90, -90);
                        var q1 = q * Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);

                        // 目的の角度に
                        slave.transform.localRotation *= q1 * Quaternion.Inverse(q);
#endif
                        //slave.transform.rotation *= Quaternion.Euler(q * (Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot));
                        //slave.transform.localRotation *= Quaternion.Euler(slvTr.localRotation * Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot);
                        //slave.transform.localRotation *= Quaternion.Euler(slave.transform.InverseTransformVector(((slvTr.rotation * Quaternion.Euler(0, -90, -90)) * v3ofs[num_].v3StackOffsetRot)));
                    }
                    else
                    {
                        slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation * Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);
                    }

                    // 位置オフセット
                    if (cfg_.doStackSlave_PosSyncMode)
                    {
                        //位置のみ
                        if (!cfg_.doStackSlave_Pelvis && !cfg_.doStackSlave_CliCnk && !cfg_.doStackSlave_PosSyncModeSp)
                        {
                            //slave.gameObject.transform.localPosition += slave.gameObject.transform.InverseTransformDirection(v3ofs[num_].v3StackOffset);

                            slave.SetPos(master.gameObject.transform.localPosition
                                + (slave.gameObject.transform.localRotation * v3ofs[num_].v3StackOffset));
                        }
#if true
                        else
                        {
                            Vector3 v3d = Vector3.zero;
                            v3d = BoneLink.BoneLink.TryPosSp(master, slave, doMasterSlave,
                                            (cfg_.doStackSlave && cfg_.doStackSlave_Pelvis),    //骨盤補正
                                            (cfg_.doStackSlave && cfg_.doStackSlave_CliCnk),    //局部補正
                                            cfg_.doStackSlave_PosSyncModeV2,
                                            cfg_.doStackSlave_PosSyncModeSp ? cfg_.doStackSlave_PosSyncModeSp_TgtBone : null,
                                            v3ofs[num_].v3StackOffset, v3ofs[num_].v3StackOffsetRot);    //補正座標

                            //Quaternion.Euler(0, -90, -90) * Quaternion.Euler(v3ofs[num_].v3StackOffsetRot) * v3ofs[num_].v3StackOffset);                         //補正座標

                            //slave.SetPos(master.gameObject.transform.localPosition
                            //    + slave.gameObject.transform.InverseTransformDirection(v3d));
                            slave.SetPos(master.gameObject.transform.localPosition);
                            slave.gameObject.transform.position += v3d;
                            //slave.SetPos(slave.gameObject.transform.localPosition
                            //    + (slave.gameObject.transform.localRotation * v3ofs[num_].v3StackOffset));
                            /*
                            if (v3ofs[num_].v3StackOffset != Vector3.zero)
                            {
                                Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);
                                slave.gameObject.transform.position += slvTr.TransformDirection(Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffset);
                            }*/
                        }
#else
                        else
                        {
                            Vector3 v3d = Vector3.zero;
                            v3d = BoneLink.BoneLink.TryPos(master, slave, doMasterSlave,
                                            (cfg_.doStackSlave && cfg_.doStackSlave_Pelvis),    //骨盤補正
                                            (cfg_.doStackSlave && cfg_.doStackSlave_CliCnk),    //局部補正
                                            Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffset);                         //補正座標

                            //slave.SetPos(master.gameObject.transform.localPosition
                            //    + slave.gameObject.transform.InverseTransformDirection(v3d));
                            slave.SetPos(master.gameObject.transform.localPosition);
                            slave.gameObject.transform.position += v3d;

                            /*
                            if (v3ofs[num_].v3StackOffset != Vector3.zero)
                            {
                                Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);
                                slave.gameObject.transform.position += slvTr.TransformDirection(Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffset);
                            }*/
                        }
#endif
                    }
                    else
                    {
                        //通常
                        //fix? slave.SetPos(master.gameObject.transform.localPosition + v3ofs[num_].v3StackOffset + v3d);
                        if (!cfg_.doStackSlave_Pelvis && !cfg_.doStackSlave_CliCnk)
                        {
                            //slave.SetPos(master.gameObject.transform.localPosition
                            //    + slave.gameObject.transform.InverseTransformDirection(v3ofs[num_].v3StackOffset));

                            slave.SetPos(master.gameObject.transform.localPosition
                                + (slave.gameObject.transform.localRotation * v3ofs[num_].v3StackOffset));
                        }
                        else
                        {
                            slave.SetPos(master.gameObject.transform.localPosition);
                        }
                    }

                    //slave.SetRot((master.gameObject.transform.localRotation * Quaternion.Euler(v3StackOffsetRot)).eulerAngles);
#if OldVer
                    if (cfg_.doStackSlave_CliCnk)
                    {
                        slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation;

                        //Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01 Pelvis" : "chinkoCenter"), true);
                        Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);
                        slave.gameObject.transform.rotation *= (Quaternion.Euler(slvTr.TransformDirection(Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot)));
#if DEBUG
                        //debugPrintConsole("Q: " + slvTr.TransformDirection(v3ofs[num_].v3StackOffsetRot));
#endif
                    }
                    else if (cfg_.doStackSlave_Pelvis)
                    {
                        slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation;

                        //Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " Pelvis", true);
                        Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);
                        slave.gameObject.transform.rotation *= (Quaternion.Euler(slvTr.TransformDirection(Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot)));
                    }
                    else
#endif //#else
                    /*if (cfg_.doStackSlave_CliCnk)
                    {
                        slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation;

                        //Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " Pelvis", true);
                        Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);
                        slave.gameObject.transform.rotation *= Quaternion.Euler(slave.gameObject.transform.InverseTransformDirection(slvTr.TransformDirection(v3ofs[num_].v3StackOffsetRot)));
                    }
                    else*/
#if pre0024
                    if (cfg_.doStackSlave_PosSyncMode && (cfg_.doStackSlave_CliCnk || cfg_.doStackSlave_Pelvis))
                    {
                        //slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation;

                        //角度オフセット
                        Transform slvTr;
                        slvTr = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, (!master.boMAN ? "Bip01" : "ManBip"), true);
                        //var q = Quaternion.Inverse(master.gameObject.transform.rotation) * slvTr.rotation;
                        //slave.gameObject.transform.rotation *= Quaternion.Euler(q * (Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot));

                        slave.transform.rotation = slvTr.rotation * Quaternion.Euler(0, -90, -90);
                        slave.transform.localRotation *= Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);
                    }
                    else if (/*!cfg_.doStackSlave_PosSyncMode && */(cfg_.doStackSlave_CliCnk || cfg_.doStackSlave_Pelvis))
                    {
                        slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation;

                        //角度オフセット
                        Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);

                        // bip基準
                        //var q = Quaternion.Inverse(slave.transform.rotation) * slvTr.rotation;
                        //var q1 = q * Quaternion.Euler((Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot));


#if true
                        // gizmo基準
                        var q = slvTr.rotation * Quaternion.Euler(0, -90, -90);
                        var q1 = q * Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);
                        q = Quaternion.Inverse(slave.transform.rotation) * q;

                        // 目的の角度に
                        slave.transform.rotation = q1 * Quaternion.Inverse(q);
#else
                        var q = Quaternion.Inverse(slave.transform.rotation) * slvTr.rotation * Quaternion.Euler(0, -90, -90);
                        var q1 = q * Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);

                        // 目的の角度に
                        slave.transform.localRotation *= q1 * Quaternion.Inverse(q);
#endif
                        //slave.transform.rotation *= Quaternion.Euler(q * (Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot));
                        //slave.transform.localRotation *= Quaternion.Euler(slvTr.localRotation * Quaternion.Euler(0, -90, -90) * v3ofs[num_].v3StackOffsetRot);
                        //slave.transform.localRotation *= Quaternion.Euler(slave.transform.InverseTransformVector(((slvTr.rotation * Quaternion.Euler(0, -90, -90)) * v3ofs[num_].v3StackOffsetRot)));
                    }
                    else
//#endif
                    {
                        slave.gameObject.transform.localRotation = master.gameObject.transform.localRotation * Quaternion.Euler(v3ofs[num_].v3StackOffsetRot);
                    }
#endif
                    // IK初期化
                    if (IkXT.IsNewIK)
                    {
                        IkXT.IkClear(slave, cfg_);
                    }

                    if (doMasterSlave && cfg_.doCopyIKTarget)
                    {
                        // 1.59 IKコピー
                        if (IkXT.IsNewIK)
                        {
                            IkXT.CopyHandIK(master, slave, v3ofs, num_);
                        }
                        else
                        {
                            //Maid.IKTargetToAttachPointより
                            //if (slave.body0.tgtHandL_AttachName != master.body0.tgtHandL_AttachName)
                            {
                                slave.body0._ikp().tgtMaidL = master.body0._ikp().tgtMaidL;
                                slave.body0._ikp().tgtHandL_AttachSlot = master.body0._ikp().tgtHandL_AttachSlot;
                                slave.body0._ikp().tgtHandL_AttachName = master.body0._ikp().tgtHandL_AttachName;
                                slave.body0._ikp().tgtHandL = master.body0._ikp().tgtHandL;
                                slave.body0._ikp().tgtHandL_offset = master.body0._ikp().tgtHandL_offset;
                            }
                            //if (slave.body0.tgtHandR_AttachName != master.body0.tgtHandR_AttachName)
                            {
                                slave.body0._ikp().tgtMaidR = master.body0._ikp().tgtMaidR;
                                slave.body0._ikp().tgtHandR_AttachSlot = master.body0._ikp().tgtHandR_AttachSlot;
                                slave.body0._ikp().tgtHandR_AttachName = master.body0._ikp().tgtHandR_AttachName;
                                slave.body0._ikp().tgtHandR = master.body0._ikp().tgtHandR;
                                slave.body0._ikp().tgtHandR_offset = master.body0._ikp().tgtHandR_offset;
                            }
                        }

                        if (cfg_.doIKTargetMHand)
                        {
                            if (string.IsNullOrEmpty(slave.body0._ikp().tgtHandR_AttachName) && slave.body0._ikp().tgtHandR == null)
                            {
                                if (!cfg_.chkIkSpCustomR_v2())
                                    AtccHand2HandR2(master, slave, v3ofs[num_].v3HandROffset, v3ofs[num_].v3HandROffsetRot, cfg_);
                            }

                            if (string.IsNullOrEmpty(slave.body0._ikp().tgtHandL_AttachName) && slave.body0._ikp().tgtHandL == null)
                            {
                                if (!cfg_.chkIkSpCustomL_v2())
                                    AtccHand2HandL2(master, slave, v3ofs[num_].v3HandLOffset, v3ofs[num_].v3HandLOffsetRot, cfg_);
                            }
                        }
                    }
                    else if (doMasterSlave && cfg_.doIKTargetMHand)
                    {
                        if (!cfg_.chkIkSpCustomL_v2())
                            AtccHand2HandL2(master, slave, v3ofs[num_].v3HandLOffset, v3ofs[num_].v3HandLOffsetRot, cfg_);
                        if (!cfg_.chkIkSpCustomR_v2())
                            AtccHand2HandR2(master, slave, v3ofs[num_].v3HandROffset, v3ofs[num_].v3HandROffsetRot, cfg_);
                    }
                    else
                    {
                        slave.body0._ikp().tgtHandL_AttachName = string.Empty;
                        slave.body0._ikp().tgtHandR_AttachName = string.Empty;
                        slave.body0._ikp().tgtHandL = null;
                        slave.body0._ikp().tgtHandR = null;
                    }
                }
                else
                {
                    if (lastSlaveStacked)
                    {
                        //アタッチの解除
                        if (lastSlaveStacked && (cfg_.doIKTargetMHand || cfg_.doCopyIKTarget))
                        {
                            lastSlaveStacked.IKTargetClear();
                        }
                        lastSlaveStacked = null;
                    }
                }

                //最終値の保持
                if (maidKeepSlaveYotogi)
                    keepSI.SaveLastInfo(maidKeepSlaveYotogi);

                //SaveMsLast();
            }

            private int GetExciteCurMaid()
            {
                if (bIsYotogiScene)
                {
                    Maid vm = mdSlaves[0].mem;
                    return vm.XtParam().status.cur_excite;
                }
                else if (bIsVymPlg)
                {
                    int elv = obj2int(VYM.API.GetVYM_Value(VYM.API.VYM_IO_ID.i_vExciteLevel));

                    //　興奮度の判定（しきい値を合わせるためにLv→興奮度に変換する）
                    int ext = 0;
                    switch (elv)
                    {
                        case 1:
                            ext = 0;
                            break;
                        case 2:
                            ext = VYMModule.VymModule.cfg.vExciteLevelThresholdV1 + 1;
                            break;
                        case 3:
                            ext = VYMModule.VymModule.cfg.vExciteLevelThresholdV2 + 1;
                            break;
                        case 4:
                            ext = VYMModule.VymModule.cfg.vExciteLevelThresholdV3 + 1;
                            break;
                    }
                    if (ext > 300)
                        ext = 300;

                    return ext;
                }
                return 0;
            }

            private bool msCheck(Maid master, Maid slave)
            {
                return msCheck(master, slave, 0);
            }

            private bool msCheck(Maid master, Maid me, int count)
            {
                if (count > MAX_PAGENUM * 2)
                {
                    debugPrintConsole("ターン" + count + " もう…ダメなのね…");
                    return true;
                }

                for (int i = 0; i < _MSlinks.Length; i++)
                {
                    var m = _MSlinks[i];
                    if (m == this)
                        continue; //自分は無視

                    if (m.doMasterSlave)
                    {
                        //別のカップルを発見
                        //debugPrintConsole("ターン" + count + " カップル発見 Link" + i);
                        if (m.mdSlaves[m.mdSlave_No].mem == master)
                        {
                            //よそではマスターがスレイブだった…
                            debugPrintConsole("ターン" + count + " ！？マスターがそんな…" + i);

                            if (m.mdMasters[m.mdMaster_No].mem == me)
                            {
                                //主従逆転…ワタシがマスター？
                                debugPrintConsole("orz...");
                                return true;
                            }
                            else
                            {
                                //調査続行
                                if (msCheck(m.mdMasters[m.mdMaster_No].mem, me, count + 1))
                                    return true;
                            }
                        }
                    }
                }
                //イベント不発
                return false;
            }

            //ループになるリンクをチェックする
            public bool testLoopLink()
            {
                if (doMasterSlave)
                    return false; //実行中なら無視

                return msCheck(mdMasters[mdMaster_No].mem, mdSlaves[mdSlave_No].mem);
            }

            //重複リンクをチェックする
            public bool testSlaved(Maid test, out int slot)
            {
                slot = -1;

                if (test == null)
                {
                    test = mdSlaves[mdSlave_No].mem;
                }

                for (int i = 0; i < _MSlinks.Length; i++)
                {
                    var m = _MSlinks[i];
                    if (m == this)
                        continue; //自分は無視

                    if (m.mdSlave_No >= 0 && m.mdSlaves[m.mdSlave_No].mem == test)
                    {
                        slot = m.num_ + 1;
                        return true;
                    }
                }
                return false;
            }

            //重複リンクをチェックする
            public bool testOverlapedLink(Maid me = null)
            {
                //リンク実行前チェック
                if (doMasterSlave)
                    return false;

                if (me == null)
                {
                    me = mdSlaves[mdSlave_No].mem;
                }

                for (int i = 0; i < _MSlinks.Length; i++)
                {
                    var m = _MSlinks[i];
                    if (m == this)
                        continue; //自分は無視

                    if (m.doMasterSlave)
                    {
                        //別のカップルを発見
                        if (m.mdSlaves[m.mdSlave_No].mem == me)
                        {
                            //Slaveの重複を検出
                            return true;
                            /*
                            if (m.mdMasters[m.mdMaster_No].mem == mdMasters[mdMaster_No].mem)
                            {
                                //重複を検出
                                return true;
                            }*/
                        }
                    }
                }
                return false;
            }

        }
        static MsLinks[] _MSlinks = new MsLinks[MAX_PAGENUM] { new MsLinks(), new MsLinks(), new MsLinks(), new MsLinks(), new MsLinks() };


        string getMSState(Maid MaidOrMan)
        {
            string state = string.Empty;
            for (int i = 0; i < _MSlinks.Length; i++)
            {
                var m = _MSlinks[i];
                if (m.doMasterSlave)
                {
                    if (m.mdMasters[m.mdMaster_No].mem == MaidOrMan)
                    {
                        state = state + "*m" + (i + 1);
                    }
                    if (m.mdSlaves[m.mdSlave_No].mem == MaidOrMan)
                    {
                        state = state + "*s" + (i + 1);
                    }
                }
            }
            if (state.Length > 0)
                state = " " + state;

            return state;
        }

        void showSlidersHint()
        {
            if (cfg.DlgShow_Hint001)
            {
                var msg = "【ヒント】\r\nスライダー上の「＜」「＞」ボタンは、キーボードのCtrlを押しながらで100倍、" +
                "Shiftを押しながらで0.1倍、Ctrl＋Shift同時押しで10倍に変化量が変わります。\r\n" +
                "微調整などにご使用下さい。\r\n" +
                "一部、スライダーの限界値を超えて設定可能な項目もあります。\r\n\r\n" +
                "次回以降、このヒントを表示しない？\r\n" +
                "(次回起動時からも非表示にするには、設定保存が必要です)";

                var ret = NUty.WinMessageBox(NUty.GetWindowHandle(), msg, "( i )", NUty.MSGBOX.MB_YESNO | NUty.MSGBOX.MB_ICONINFORMATION);
                if (ret == (int)System.Windows.Forms.DialogResult.Yes)
                {
                    cfg.DlgShow_Hint001 = false;
                }
            }
        }

        void showHintMsg(string msg)
        {
            NUty.WinMessageBox(NUty.GetWindowHandle(), msg, "( i )", NUty.MSGBOX.MB_OK | NUty.MSGBOX.MB_ICONINFORMATION);
        }

        //ページ
        static int _pageNum = 0;

        //表示関係
        bool showPosSlider = false;
        bool showPosSliderHand = false;
        bool showPosSliderHandR = true;

        bool showChinkoSlider = false;

        bool showSlaveEyeToTgt = false;
        bool showVymPlaySet = false;
        int showWndMode = 0;
        bool showWndMin = false;
        bool showWndMinHide = false;
        bool showSubMens = false;
        bool showSlvMask = false;

        // 手のアタッチカスタム
        //bool showHandsToTgt = false;
        bool showHandTTPosSlider = false;

        float hsldPreHeight = 0f;

        GUIStyle gsLabel = new GUIStyle("label");
        GUIStyle gsButton = new GUIStyle("button");
        GUIStyle gsToggle = new GUIStyle("toggle");
        GUIStyle gsText = new GUIStyle("textfield");
        GUIStyle gsTextAr = new GUIStyle("textArea");
        GUIStyle gsCombo = new GUIStyle("button");

        void WindowCallback_proc(int id)
        {
            _WinprocPhase = "[start]";

            bool SlaveRusubanMode = false; //マスター待機モード
            //bool boChrCng = false;

            MsLinks ms_ = _MSlinks[_pageNum];
            //ms_.MsUpdate(true, true);     //リンクをチェック->前メソッドに移動

            _WinprocPhase = "[init]";
            //GUIStyle gsLabel = new GUIStyle("label");
            gsLabel.fontSize = 12;
            gsLabel.alignment = TextAnchor.MiddleLeft;

            //GUIStyle gsButton = new GUIStyle("button");
            gsButton.fontSize = 12;
            gsButton.alignment = TextAnchor.MiddleCenter;

            //GUIStyle gsToggle = new GUIStyle("toggle");
            gsToggle.fontSize = 12;
            gsToggle.alignment = TextAnchor.MiddleLeft;

            //GUIStyle gsText = new GUIStyle("textfield");
            gsText.fontSize = 12;
            gsText.alignment = TextAnchor.UpperLeft;

            //GUIStyle gsTextAr = new GUIStyle("textArea");
            gsTextAr.fontSize = 12;
            gsTextAr.alignment = TextAnchor.UpperLeft;
            gsTextAr.wordWrap = true;

            //GUIStyle gsCombo = new GUIStyle("button");
            gsCombo.fontSize = 12;
            gsCombo.alignment = TextAnchor.MiddleLeft;
            gsCombo.hover.textColor = Color.cyan;
            gsCombo.onHover.textColor = Color.cyan;
            gsCombo.onActive.textColor = Color.cyan;

            //GUIStyle gsScView = new GUIStyle("scrollView");
            _WinprocPhase = "[ctrl-1]";

            GUI.enabled = true;
            if (GUI.Button(new Rect(240, 0, 20, 20), "x", gsButton))
            {
                GizmoVisible(false);
                GizmoHsVisible(false);

                CloseAllCombos();
                GuiFlag = false;
                return;
            }

            if (GUI.Button(new Rect(240 - 90, 0, 20, 20), (showWndMin ? "▼" : "▽"), gsButton))
            {
                showWndMin = !showWndMin;

                if (showWndMin)
                {
                    //GizmoVisible(false);
                    GizmoHsVisible(false);
                }
            }
            //GUI.enabled = true;

            if (showWndMode == 1)
                GUI.enabled = false;
            if (GUI.Button(new Rect(240 - 70, 0, 20, 20), "-", gsButton))
            {
                showWndMode = 1;
            }
            GUI.enabled = true;
            if (showWndMode == 2)
                GUI.enabled = false;
            if (GUI.Button(new Rect(240 - 50, 0, 20, 20), "=", gsButton))
            {
                showWndMode = 2;
            }
            GUI.enabled = true;
            if (showWndMode == 0)
                GUI.enabled = false;
            if (GUI.Button(new Rect(240 - 30, 0, 20, 20), "□", gsButton))
            {
                showWndMode = 0;
            }
            GUI.enabled = true;

            //最小化時
            if (showWndMin && showWndMinHide)
            {
                if (showWndMin)
                {
                    //GizmoVisible(false);
                    GizmoHsVisible(false); //手ギズモは危険物なので消す
                }

                //位置ギズモだけは処理
                if (ms_.doMasterSlave)
                    PosGizmoProc(ms_);
                else
                    GizmoVisible(false);
                return;
            }

            EditScroll_cfg = GUI.BeginScrollView(new Rect(0, 20, GUI_WIDTH, GUI_HIGHT - 30), EditScroll_cfg, new Rect(0, 0, GUI_WIDTH - 16, EditScroll_cfg_sizeY));
            try
            {
                MsLinkConfig p_mscfg = cfgs[_pageNum];
                v3Offsets p_v3of = v3ofs[_pageNum];
                int pos_y = 0;
                if (_MensList.Count <= 0)
                    GUI.Label(new Rect(5, pos_y, 200, ItemHeight), "【Master-Slave無効中】", gsLabel);
                else
                    GUI.Label(new Rect(5, pos_y, 200, ItemHeight), "【Master-Slave切替】", gsLabel);

                GUI.Label(new Rect(5 + ItemWidth - 25 - 35 - 35, pos_y, 25, ItemHeight), "設定", gsLabel);

                if (GUI.Button(new Rect(5 + ItemWidth - 35 - 35, pos_y, 35 * 2, 20), "保存/読込", gsButton))
                {
                    // Iniファイル設定画面へ
                    XtMs2ndWnd.Init();
                    XtMs2ndWnd.boShow = true;

                    GizmoVisible(false);
                    GizmoHsVisible(false);
                }
                /*
                if (GUI.Button(new Rect(5 + ItemWidth - 35 - 35, pos_y, 35, 20), "保存", gsButton))
                {
                    // Iniファイル書き出し
                    SaveMyConfigs();
                }
                if (GUI.Button(new Rect(5 + ItemWidth - 35, pos_y, 35, 20), "読込", gsButton))
                {
                    // Iniファイル読み出し
                    LoadMyConfigs();
                }*/

                if (ms_.maidKeepSlaveYotogi)
                {
                    GUI.enabled = false;
                    GUI.Label(new Rect(ItemX, pos_y += ItemHeight, ItemWidth, ItemHeight), "夜伽Slave-Keeper 作動中", gsButton);
                    GUI.enabled = true;
                }
                else
                {
                    if (_MensList.Count <= 0)
                    {   //男性不在
                        ms_.Scc1_MasterMaid = false;
                        GizmoVisible(false);
                        GizmoHsVisible(false);
                    }
                    else
                    {
                        //マスターモード選択
                        if (!ms_.Scc1_MasterMaid)
                        {
                            GUI.enabled = false;
                        }
                        if (GUI.Button(new Rect(ItemX, pos_y += ItemHeight, ItemWidth / 2, ItemHeight), "Man⇒Maid", gsButton))
                        {
                            // モード変更
                            MsUtil.ChangeMsMode(_pageNum, false);
                        }
                        GUI.enabled = true;

                        if (ms_.Scc1_MasterMaid)
                        {
                            GUI.enabled = false;
                        }
                        if (GUI.Button(new Rect(ItemX + ItemWidth / 2, pos_y, ItemWidth / 2, ItemHeight), "Maid⇒Man", gsButton))
                        {
                            // モード変更
                            MsUtil.ChangeMsMode(_pageNum, true);
                        }
                        GUI.enabled = true;
                    }

                    //更新
                    ms_.MsUpdate(false, true);

                }

                //ページ選択
                _WinprocPhase = "[page-sel]";
                pos_y += ItemHeight;
                int oldPageNum = _pageNum;
                //GUI.Label(new Rect(5 + ItemWidth - 25 - 35 - 35, pos_y, 25, ItemHeight), "Slot", gsLabel);
                for (int i = 0; i < MAX_PAGENUM; i++)
                {
                    if (_pageNum == i)
                        GUI.enabled = false;

                    Color c = GUI.color;
                    if (_MSlinks[i].doMasterSlave)
                        GUI.color = Color.yellow;
                    else if (_MSlinks[i].maidKeepSlaveYotogi)
                        GUI.color = Color.cyan;

                    if (GUI.Button(new Rect(ItemX + ItemWidth - (5 + 20 * (MAX_PAGENUM - i)), pos_y, 20, 20), (i + 1).ToString(), gsButton))
                    {
                        _pageNum = i;
                    }

                    GUI.color = c;
                    GUI.enabled = true;
                }
                /*
                if (GUI.Button(new Rect(ItemX + ItemWidth - (10 + 20 * 5), pos_y, 20, 20), "1", gsButton))
                {
                    _pageNum = 0;
                }
                */
                GUI.enabled = true;
                if (_pageNum != oldPageNum)
                {
                    //ページ変更有
                    p_mscfg = cfgs[_pageNum];
                    ms_ = _MSlinks[_pageNum];

                    //コンボ閉じる
                    CloseAllCombos();
                    return;
                }

                if (_MensList.Count <= 0)
                {//男性不在
                    ComboMaster.boPop = false;
                    ms_.doMasterSlave = false;
                    ms_.Scc1_MasterMaid = false;
                }

                Rect rcItem = new Rect(ItemX, pos_y/* += ItemHeight*/, ItemWidth, ItemHeight);

                if (ms_.maidKeepSlaveYotogi)
                {
                    _WinprocPhase = "[keeperview]";
                    GUI.Label(rcItem, "【Master】 (リンク元)", gsLabel);
                    rcItem.y += ItemHeight;
                    GUI.Label(rcItem, (!ms_.do_master || !ms_.do_master.Visible ? " [待機中]" : " [⇔]") + "  " + ms_.do_masterName, gsText);
                    rcItem.y += ItemHeight;
                    GUI.Label(rcItem, "【Slave】 (リンク先)", gsLabel);
                    rcItem.y += ItemHeight;
                    GUI.Label(rcItem, (!ms_.do_slave || !ms_.do_slave.Visible ? " [待機中]" : " [⇔]") + "  " + ms_.do_slaveName, gsText);
                    rcItem.y += ItemHeight;

                    GUI.enabled = true;
                    if (!ms_.doMasterSlave)
                    {
                        Color cbk = GUI.color;
                        if (ms_.doMasterSlave)
                            GUI.color = Color.yellow;
                        else
                            GUI.color = Color.cyan;
                        if (GUI.Button(rcItem, "夜伽Slave-Keeper 解除", gsButton))
                        {
                            ms_.maidKeepSlaveYotogi = null;
                        }
                        GUI.color = cbk;
                        rcItem.y += ItemHeight;
                    }
                }
                else
                {
                    _WinprocPhase = "[chr-sel]";

                    List<string> m_names;
                    if (ms_.mdMasters.Count > 0)
                    {
                        GUI.Label(rcItem, "【Master】 (リンク元)", gsLabel);
                        rcItem.y += ItemHeight;

                        string master_name = "未選択";
                        if (ms_.mdMaster_No >= 0)
                            master_name = GetMaidName(ms_.mdMasters[ms_.mdMaster_No], true) + getMSState(ms_.mdMasters[ms_.mdMaster_No].mem);
                        m_names = new List<string>();
                        if (ms_.doMasterSlave)
                        {
                            ComboMaster.boPop = false;
                            GUI.enabled = false;
                        }
                        else
                        {
                            foreach (var vm in ms_.mdMasters)
                                m_names.Add(GetMaidName(vm, true) + getMSState(vm.mem));

                            m_names.Add("*** 選択解除 ***");
                        }
                        if (ComboMaster.Show(rcItem, ItemHeight, ItemHeight * 4, m_names, master_name, gsButton, gsButton))
                        {
                            int memNum = ms_.mdMaster_No;
                            if (ComboMaster.sIndex >= 0)
                            {
                                memNum = ComboMaster.sIndex;
                            }

                            MsUtil.SelectMaster(_pageNum, memNum);
                        }
                        GUI.enabled = true;
                        pos_y = (int)rcItem.y + ItemHeight;
                        if (ComboMaster.boPop)
                        {
                            pos_y += ItemHeight * 4;
                        }
                    }

                    _WinprocPhase = "[chr-sel2]";

                    rcItem.y = pos_y;
                    if (_MensList.Count > 0)
                        GUI.Label(rcItem, "【Slave】 (リンク先)", gsLabel);
                    else
                        GUI.Label(rcItem, "【Slave】(Maid)", gsLabel);
                    rcItem.y += ItemHeight;

                    string slave_name = "未選択";
                    if (ms_.mdSlave_No >= 0)
                        slave_name = GetMaidName(ms_.mdSlaves[ms_.mdSlave_No], true) + getMSState(ms_.mdSlaves[ms_.mdSlave_No].mem);
                    m_names = new List<string>();
                    bool[] disables = new bool[ms_.mdSlaves.Count + 2];

                    if (ms_.doMasterSlave)
                    {
                        ComboSlave.boPop = false;
                        GUI.enabled = false;
                    }
                    else
                    {
                        int i = 0;
                        foreach (var vm in ms_.mdSlaves)
                        {
                            string name = GetMaidName(vm, true);
                            if (ms_.testSlaved(vm.mem, out int slot))
                            {
                                if (!vm.mem.boMAN)
                                    name = name.Substring(0, 7);
                                name = name + " <Slave " + slot.ToString() + ">";
                                disables[i] = true;
                            }
                            m_names.Add(name + getMSState(vm.mem));
                            i++;
                        }

                        m_names.Add("*** 選択解除 ***");
                    }

                    // Slave選択
                    if (ComboSlave.Show(rcItem, ItemHeight, ItemHeight * 4, m_names, slave_name, gsButton, gsButton, disables))
                    {
                        if (ComboSlave.sIndex >= 0)
                        {
                            // Slave変更
                            MsUtil.SelectSlave(_pageNum, ComboSlave.sIndex);
                        }
                    }
                    GUI.enabled = true;
                    pos_y = (int)rcItem.y + ItemHeight;
                    if (ComboSlave.boPop)
                    {
                        pos_y += ItemHeight * 4;
                    }
                    rcItem.y = pos_y;
                }

                //留守番モードチェック
                if (_MensList.Count > 0 && ms_.mdMaster_No < 0 && !ms_.Scc1_MasterMaid && ms_.mdSlave_No >= 0
                    && ms_.mdSlaves[ms_.mdSlave_No].mem && !ms_.mdSlaves[ms_.mdSlave_No].mem.boMAN)
                {
                    //マスター待機モード
                    ms_.Scc1_MasterMaid = false;
                    SlaveRusubanMode = true;
                    ms_.doMasterSlave = false;

                    GizmoVisible(false);
                    GizmoHsVisible(false);
                }

                //未選択チェック
                if (ms_.mdSlave_No < 0 || (ms_.mdMaster_No < 0 && _MensList.Count > 0 && !SlaveRusubanMode))
                {
                    //未選択状態
                    //EditScroll_cfg_sizeY = pos_y;
                    //return;

                    if (ms_.mdSlaves.Count > 0)
                    {
                        Maid slave0 = ms_.mdSlaves[0].mem;
                        //サブメンバーコントロール
                        if (ProcSubMemberCtrls(ref pos_y, ref rcItem, ms_, slave0, p_mscfg))
                        {
                            //メンバー変更有
                            //この後なにもないのでスルー//return;
                        }
                    }

                    pos_y = (int)(rcItem.y += ItemHeight / 2);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    EditScroll_cfg_sizeY = pos_y + ItemHeight;
                    return;
                }

                _WinprocPhase = "[ms-set]";

                Maid master = (ms_.mdMasters.Count > 0 && !SlaveRusubanMode) ? ms_.mdMasters[ms_.mdMaster_No].mem : null;
                Maid slave = ms_.mdSlaves[ms_.mdSlave_No].mem;
                //if (!master || !slave || !master.body0 || !slave.body0)
                if (!slave || !slave.body0 || (master && !master.body0))　//メイド単体稼働可に
                {
                    //ここは基本来ないはず（bodyロード中くらい？）
                    EditScroll_cfg_sizeY = pos_y;
                    debugPrintConsole("エラー：slave = null");
                    return;
                }

                if (master != null)
                {
                    Color cbk = GUI.color;
                    if (ms_.doMasterSlave)
                        GUI.color = Color.yellow;

                    //リンク実行ボタン
#if false
                    if (ms_.maidKeepSlaveYotogi)
                    {
                        /*移設
                         * GUI.enabled = true;
                        if (GUI.Button(rcItem, "夜伽Slave-Keeper 解除", gsButton) )
                        {
                            ms_.maidKeepSlaveYotogi = null;
                        }*/
                    }
                    else
#endif
                    if (ms_.testLoopLink())
                    {
                        //リンクの無限ループを防止
                        GUI.enabled = false;
                        GUI.Button(rcItem, "× リンクに無限ループを検出 ×", gsButton);
                    }
                    else if (ms_.testOverlapedLink())
                    {
                        //リンクのダブりを防止
                        GUI.enabled = false;
                        GUI.Button(rcItem, "× 既存リンクに同Slaveを検出 ×", gsButton);
                    }
                    else if (GUI.Button(rcItem, "Master-Slave *" + (_pageNum + 1) + " リンク" + (!ms_.doMasterSlave ? "実行" : "停止"), gsButton))
                    {
                        // リンクスタートか停止
                        MsUtil.StartMsLink(_pageNum, ms_.doMasterSlave, false, slave);
                    }
                    GUI.color = cbk;
                    GUI.enabled = true;

                    pos_y = (int)(rcItem.y += ItemHeight);
                    pos_y = (int)(rcItem.y += ItemHeight / 2);

                    GUI.Label(rcItem, "【配置設定】", gsLabel);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    p_mscfg.doStackSlave = GUI.Toggle(rcItem, p_mscfg.doStackSlave, "Master座標にSlaveを重ねる", gsToggle);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    _WinprocPhase = "[sliders]";

                    if (p_mscfg.doStackSlave)
                    {
                        const int SLDW = 220;
                        const int LX = ItemX + 10;
                        const int BW = 25;
                        const int LW = SLDW - BW * 3 - 5;
                        const int EDTW = LW + BW * 2 + 5;

                        //pos_y += ItemHeight;
                        //ギズモ表示設定
                        if (!ms_.doMasterSlave)
                        {
                            GUI.enabled = false;
                            _Gizmo.Visible = false;
                        }
                        _Gizmo.Visible = GUI.Toggle(new Rect(LX, (pos_y), ItemWidth - 70, ItemHeight), _Gizmo.Visible, "Slave重ね位置調整(Gizmo)", gsToggle);
                        GUI.enabled = true;
                        /*if (GUI.Button(new Rect(rcItem.x + rcItem.width -20, rcItem.y, 20, ItemHeight), "C", gsButton))
                        {
                            v3StackOffset = Vector3.zero;
                        }*/
                        //pos_y = (int)(rcItem.y += ItemHeight);

                        GizmoVisible(_Gizmo.Visible);

                        GUI.Label(new Rect(LX + ItemWidth - 70 + 15, pos_y, 25, ItemHeight), "詳細", gsLabel);

                        if (GUI.Button(new Rect(LX + ItemWidth - 70 + 40, (pos_y), 20, 20), (showPosSlider ? "-" : "+"), gsButton))
                        {
                            showPosSlider = !showPosSlider;

                            if (showPosSlider)
                            {
                                showSlidersHint();
                            }
                        }

                        if (showPosSlider)
                        {
                            Vector3 _v = p_v3of.v3StackOffset;
                            GUI.Label(new Rect(LX, (pos_y += ItemHeight), 122, ItemHeight), " +X: " + Math.Round(_v.x, 4), gsLabel);
                            _v.x = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.x, gsButton);
                            if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                            {
                                _v.x = 0;
                            }
                            _v.x = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.x, -1f, 1f);

                            GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +Y: " + Math.Round(_v.y, 4), gsLabel);
                            _v.y = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.y, gsButton);
                            if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                            {
                                _v.y = 0;
                            }
                            _v.y = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.y, -1f, 1f);

                            GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +Z: " + Math.Round(_v.z, 4), gsLabel);
                            _v.z = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.z, gsButton);
                            if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                            {
                                _v.z = 0;
                            }
                            _v.z = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.z, -1f, 1f);

                            p_v3of.v3StackOffset = _v;
                        }


                        if (showPosSlider)
                        {
                            Vector3 _v = va180(p_v3of.v3StackOffsetRot);
                            GUI.Label(new Rect(LX, (pos_y += ItemHeight), 122, ItemHeight), " +回転X: " + Math.Round(_v.x, 4), gsLabel);
                            _v.x = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.x, gsButton);
                            if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                            {
                                _v.x = 0;
                            }
                            _v.x = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.x, -180f, 180f);

                            GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +回転Y: " + Math.Round(_v.y, 4), gsLabel);
                            _v.y = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.y, gsButton);
                            if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                            {
                                _v.y = 0;
                            }
                            _v.y = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.y, -180f, 180f);

                            GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +回転Z: " + Math.Round(_v.z, 4), gsLabel);
                            _v.z = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.z, gsButton);
                            if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                            {
                                _v.z = 0;
                            }
                            _v.z = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.z, -180f, 180f);

                            p_v3of.v3StackOffsetRot = _v;


                            rcItem.y = (pos_y += 20);
                            p_mscfg.doStackSlave_Pelvis = GUI.Toggle(new Rect(LX, (pos_y), ItemWidth - LX, ItemHeight), p_mscfg.doStackSlave_Pelvis, "骨盤ボーンでの位置補正を行う", gsToggle);
                            if (p_mscfg.doStackSlave_Pelvis) p_mscfg.doStackSlave_CliCnk = false;

                            rcItem.y = (pos_y += 20);
                            p_mscfg.doStackSlave_CliCnk = GUI.Toggle(new Rect(LX, (pos_y), ItemWidth - LX, ItemHeight), p_mscfg.doStackSlave_CliCnk, "局部で位置補正(chinkoCenter)", gsToggle);
                            if (p_mscfg.doStackSlave_CliCnk) p_mscfg.doStackSlave_Pelvis = false;


                            //地面の当たり判定位置オフセット
                            if (!cfg.AdjustBoneHitHeightY)
                            {
                                GUI.enabled = false;
                                p_mscfg.Adjust_doHitHeightYOffset = false;
                            }
                            rcItem.y = (pos_y += 20);
                            p_mscfg.Adjust_doHitHeightYOffset = GUI.Toggle(new Rect(LX, (pos_y), ItemWidth - LX, ItemHeight), p_mscfg.Adjust_doHitHeightYOffset, "Slave HitHeight調整(接地判定高さ)", gsToggle);
                            GUI.enabled = true;
                            if (p_mscfg.Adjust_doHitHeightYOffset)
                            {
                                rcItem.y = (pos_y += 20);
                                ref float refsc = ref p_mscfg.Adjust_HitHeightYOffset; //オフセット値
                                float newsc_h = refsc;


                                GUI.Label(new Rect(LX, (pos_y), LW, ItemHeight), "+Y Offset: " + Math.Round(refsc, 3), gsLabel);

                                if (master && GUI.Button(new Rect(LX + LW - 30, (pos_y), 28, 20), "=M", gsButton))
                                {
                                    newsc_h = master.body0.BoneHitHeightY - master.gameObject.transform.position.y;
                                }
                                newsc_h = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, newsc_h, 0.01f, gsButton, -2000f, 2000f);
                                if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "|", gsButton))
                                {
                                    newsc_h = 0;
                                }

                                if (!(Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)))
                                    hsldPreHeight = newsc_h;
                                newsc_h = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), newsc_h, -2f + hsldPreHeight, 2f + hsldPreHeight);
                                if (newsc_h != refsc)
                                {
                                    refsc = newsc_h;
                                }

                                //rcItem.y = (pos_y += 15);
                            }
                            rcItem.y = (pos_y += 10);
                        }

                        rcItem.y = (pos_y += 20);
                        p_mscfg.doIKTargetMHand = GUI.Toggle(new Rect(LX, (pos_y), ItemWidth - LX - 70, ItemHeight), p_mscfg.doIKTargetMHand, "Masterの両手にアタッチ", gsToggle);
                        //pos_y = (int)(rcItem.y += ItemHeight);

                        if (ms_.doMasterSlave)
                        {
                            GUI.Label(new Rect(LX + ItemWidth - 70 + 15, pos_y, 25, ItemHeight), "調整", gsLabel);

                            if (GUI.Button(new Rect(LX + ItemWidth - 70 + 40, (pos_y), 20, 20), (showPosSliderHand ? "-" : "+"), gsButton))
                            {
                                showPosSliderHand = !showPosSliderHand;

                                GizmoHsVisible(showPosSliderHand);

                                if (showPosSliderHand)
                                {
                                    showSlidersHint();
                                }

                                if (showHandTTPosSlider)
                                {
                                    // どちらもONなら切り替え
                                    showHandTTPosSlider = false;
                                }
                            }
                        }
                        else
                        {
                            if (showPosSliderHand)
                            {
                                showPosSliderHand = false;
                                GizmoHsVisible(false);
                            }
                        }

                        if (showPosSliderHand)
                        {
                            //ギズモとスライダー調整
                            ProcHandGizmo(slave, ref rcItem, ref pos_y, p_mscfg, p_v3of);
                        }

                        rcItem.y = (pos_y += 20);
                        p_mscfg.doCopyIKTarget = GUI.Toggle(new Rect(LX, (pos_y), ItemWidth - LX, ItemHeight), p_mscfg.doCopyIKTarget, "Masterの手のIKターゲットを複製", gsToggle);

                        rcItem.y = (pos_y += 20);
                        p_mscfg.doStackSlave_PosSyncMode = GUI.Toggle(new Rect(LX, (pos_y), ItemWidth - LX - 30, ItemHeight), p_mscfg.doStackSlave_PosSyncMode, "位置のみリンク(ﾎﾟｰｽﾞ同期OFF)", gsToggle);
                        if (p_mscfg.doStackSlave_PosSyncMode)
                        {
                            p_mscfg.doStackSlave_PosSyncModeV2 = GUI.Toggle(new Rect(LX + ItemWidth - LX - 30, (pos_y), 40, ItemHeight), p_mscfg.doStackSlave_PosSyncModeV2, "v2", gsToggle);

                            rcItem.y = (pos_y += 20);
                            p_mscfg.doStackSlave_PosSyncModeSp = GUI.Toggle(new Rect(LX, (pos_y), ItemWidth - LX - 30, ItemHeight), p_mscfg.doStackSlave_PosSyncModeSp, "リンク先ボーン指定変更", gsToggle);
                            if (p_mscfg.doStackSlave_PosSyncModeSp)
                            {
                                rcItem.y = (pos_y += 20);

                                string[] e_names = master.boMAN ? Defines.data.ManBones : Defines.data.MaidBones;
                                Rect rctmp = new Rect(LX, pos_y, ItemWidth - LX, ItemHeight);
                                if (ComboPosLinkBone.Show(rctmp, ItemHeight, ItemHeight * 4, e_names, p_mscfg.doStackSlave_PosSyncModeSp_TgtBone, gsButton, gsButton))
                                {
                                    if (ComboPosLinkBone.sIndex >= 0)
                                    {
                                        p_mscfg.doStackSlave_PosSyncModeSp_TgtBone = ComboPosLinkBone.sSelected;
                                    }
                                }
                                pos_y = (int)(rcItem.y += ItemHeight);
                                if (ComboPosLinkBone.boPop)
                                {
                                    pos_y = (int)(rcItem.y += (ItemHeight * 4));
                                }
                            }

                        }

                        pos_y = (int)(rcItem.y += ItemHeight);
                    }
                    else
                    {
                        GizmoVisible(false);
                        GizmoHsVisible(false);
                    }
                }//master!=null
                else
                { //master == null
                    ms_.doMasterSlave = false;
                    ms_.maidKeepSlaveYotogi = null;
                }

                const int SSLDW = ItemWidth;
                const int SLX = ItemX;
                const int SBW = 25;
                const int SLW = SSLDW - SBW * 3 - 5;
                const int SEDTW = SLW + SBW * 2 + 5;

                //v0011追加
                float sc_m = 1f;
                float newsc_m = 1f;
                if (master != null)
                {
                    sc_m = (master.gameObject.transform.localScale.x + master.gameObject.transform.localScale.y + master.gameObject.transform.localScale.z) / 3;
                    newsc_m = sc_m;

                    rcItem.y = (pos_y += 15);
                    GUI.Label(new Rect(SLX, (pos_y), SLW, ItemHeight), "Masterサイズ調整: " + Math.Round(newsc_m, 3), gsLabel);
                    newsc_m = btnset_LR(new Rect(SLX + SLW, (pos_y), SBW, 20), SBW, newsc_m, gsButton/*, cfg.Scale_Min, cfg.Scale_Max*/, 0.01f, 100f);
                    if (GUI.Button(new Rect(SLX + SEDTW, (pos_y), 28, 20), "|", gsButton))
                    {
                        newsc_m = 1f;
                    }
                    newsc_m = GUI.HorizontalSlider(new Rect(SLX, (pos_y += 20), SSLDW, 15), newsc_m, cfg.Scale_Min, cfg.Scale_Max);
                    if (newsc_m != sc_m)
                    {
                        master.gameObject.transform.localScale = new Vector3(newsc_m, newsc_m, newsc_m);
                        if (!master.boMAN)
                            UpdateHitScale(master, newsc_m, p_mscfg);
                        //UpdateHitScale(master, newsc_m * p_mscfg.Scale_HitCheckEffect);
                    }
                    //rcItem.y = (pos_y += 20);
                }//master!=null

                float sc = (slave.gameObject.transform.localScale.x + slave.gameObject.transform.localScale.y + slave.gameObject.transform.localScale.z) / 3;
                float newsc = sc;
                rcItem.y = (pos_y += 15);
                //GUI.Label(rcItem, "Slaveサイズ調整", gsLabel);
                GUI.Label(new Rect(SLX, (pos_y), SLW, ItemHeight), "Slaveサイズ調整: " + Math.Round(newsc, 3), gsLabel);
                newsc = btnset_LR(new Rect(SLX + SLW, (pos_y), SBW, 20), SBW, newsc, gsButton/*, cfg.Scale_Min, cfg.Scale_Max*/, 0.01f, 100f);
                if (GUI.Button(new Rect(SLX + SEDTW, (pos_y), 28, 20), "|", gsButton))
                {
                    newsc = 1f;
                }
                newsc = GUI.HorizontalSlider(new Rect(SLX, (pos_y += 20), SSLDW, 15), newsc, cfg.Scale_Min, cfg.Scale_Max);
                if (newsc != sc)
                {
                    slave.gameObject.transform.localScale = new Vector3(newsc, newsc, newsc);
                    if (!slave.boMAN)
                        UpdateHitScale(slave, newsc, p_mscfg);
                    //  UpdateHitScale(slave, newsc * p_mscfg.Scale_HitCheckEffect);
                }
                rcItem.y = (pos_y += 15);

                //GUI.Label(new Rect(SLX, (pos_y), SLW, ItemHeight), "ヒットチェック倍率: " + Math.Round(p_mscfg.Scale_HitCheckEffect, 2), gsLabel);
                if (!p_mscfg.Scale_HitCheckDetail)
                    GUI.Label(new Rect(SLX, (pos_y), SLW - 45, ItemHeight), "HitCheck倍率: " + Math.Round(p_mscfg.Scale_HitCheckEffect, 2), gsLabel);
                else
                    GUI.Label(new Rect(SLX, (pos_y), SLW - 45, ItemHeight), "HitCheck倍率: ", gsLabel);

                if (GUI.Button(new Rect(SLX + SLW - 52, (pos_y), 48, ItemHeight), "再適用", gsButton))
                {
                    if (master && !master.boMAN)
                        UpdateHitScale(master, newsc_m, p_mscfg);
                    //UpdateHitScale(master, newsc_m * p_mscfg.Scale_HitCheckEffect);
                    if (!slave.boMAN)
                        UpdateHitScale(slave, newsc, p_mscfg);
                    //UpdateHitScale(slave, newsc * p_mscfg.Scale_HitCheckEffect);
                }
                if (!p_mscfg.Scale_HitCheckDetail)
                {
                    float newsc_h = p_mscfg.Scale_HitCheckEffect;
                    newsc_h = btnset_LR(new Rect(SLX + SLW, (pos_y), SBW, 20), SBW, newsc_h, 0.01f, gsButton, 0f, 2.0f);
                    if (GUI.Button(new Rect(SLX + SEDTW, (pos_y), 28, 20), "|", gsButton))
                    {
                        newsc_h = 1f;
                    }
                    newsc_h = GUI.HorizontalSlider(new Rect(SLX, (pos_y += 20), SSLDW, 15), newsc_h, 0f, 2.0f);
                    if (newsc_h != p_mscfg.Scale_HitCheckEffect)
                    {
                        p_mscfg.Scale_HitCheckEffect = newsc_h;

                        if (master && !master.boMAN)
                            UpdateHitScale(master, newsc_m, newsc_h);
                        if (!slave.boMAN)
                            UpdateHitScale(slave, newsc, newsc_h);
                    }
                    rcItem.y = (pos_y += 15);
                }
                else
                {
                    if (GUI.Button(new Rect(SLX + SLW, (pos_y), 50, ItemHeight), (cfg.hideHitScaleDef ? "+ 開く" : "- 隠す"), gsButton))
                    {
                        cfg.hideHitScaleDef = !cfg.hideHitScaleDef;
                    }
                    rcItem.y = (pos_y += 20);

                    if (!cfg.hideHitScaleDef)
                    {
                        ref float f(HitCheckTgt h)
                        {
                            switch (h)
                            {
                                case HitCheckTgt.Hip:
                                    return ref p_mscfg.Scale_HitCheckDetail_Hip;
                                case HitCheckTgt.Momo:
                                    return ref p_mscfg.Scale_HitCheckDetail_Momo;
                                case HitCheckTgt.Spine:
                                    return ref p_mscfg.Scale_HitCheckDetail_Spine;
                                case HitCheckTgt.Thigh:
                                    return ref p_mscfg.Scale_HitCheckDetail_Thigh;
                                default:
                                    return ref p_mscfg.Scale_HitCheckDetail_Bip01;
                            }
                        }

                        string fixHitNames(string str)
                        {
#if COM3D2
                            if (str == "Momo")
                                return "Momo+dVag";
                            if (str == "Thigh")
                                return "Thigh+dReg";
                            if (str == "Spine")
                                return "Spine+dMune";
                            if (str == "Bip01")
                                return "Bip01+dbc";
                            if (str == "Hip")
                                return "Hip+dHipLR";
#endif
                            return str;
                        }

                        foreach (var h in HitCheckTgtStr)
                        {
                            ref float refsc = ref f(h.Key);
                            float newsc_h = refsc;

                            GUI.Label(new Rect(SLX, (pos_y), SLW - 45, ItemHeight), fixHitNames(h.Key.ToString()) + ": " + Math.Round(refsc, 3), gsLabel);
                            newsc_h = btnset_LR(new Rect(SLX + SLW, (pos_y), SBW, 20), SBW, newsc_h, 0.01f, gsButton, 0f, 6.0f);
                            if (GUI.Button(new Rect(SLX + SEDTW, (pos_y), 28, 20), "|", gsButton))
                            {
                                if (cfg.doHitScaleDef)
                                {
                                    newsc_h = cfg.HitScaleDef[(int)h.Key];
                                }
                                else
                                {
                                    newsc_h = 1f;
                                }
                            }
                            newsc_h = GUI.HorizontalSlider(new Rect(SLX, (pos_y += 20), SSLDW, 15), newsc_h, 0f, 3.5f);
                            if (newsc_h != refsc)
                            {
                                refsc = newsc_h;

                                if (master && !master.boMAN)
                                    UpdateHitScale(master, newsc_m, p_mscfg);
                                if (!slave.boMAN)
                                    UpdateHitScale(slave, newsc, p_mscfg);
                            }
                            rcItem.y = (pos_y += 15);
                        }

                        if (p_mscfg.Scale_HitCheckDetail)
                        {
                            Color cbk = GUI.color;
                            if (cfg.doHitScaleDef)
                            {
                                GUI.color = Color.yellow;
                            }
                            if (GUI.Button(new Rect(SLX, (pos_y), SSLDW - 80, 20), "全ﾒｲﾄﾞの基本値に" + (cfg.doHitScaleDef ? "上書き" : "登録"), gsButton))
                            {
                                cfg.doHitScaleDef = true;
                                foreach (var h in HitCheckTgtStr)
                                {
                                    cfg.HitScaleDef[(int)h.Key] = f(h.Key);
                                }
                            }
                            GUI.color = cbk;

                            if (!cfg.doHitScaleDef)
                            {
                                GUI.enabled = false;
                            }
                            if (GUI.Button(new Rect(SLX + (SSLDW - 80), (pos_y), 40, 20), "取得", gsButton))
                            {
                                foreach (var h in HitCheckTgtStr)
                                {
                                    f(h.Key) = cfg.HitScaleDef[(int)h.Key];
                                }
                            }
                            if (GUI.Button(new Rect(SLX + (SSLDW - 40), (pos_y), 40, 20), "解除", gsButton))
                            {
                                cfg.doHitScaleDef = false;
                            }
                            GUI.enabled = true;
                        }
                        rcItem.y = (pos_y += 20);
                    }
                }
                p_mscfg.Scale_HitCheckDetail = GUI.Toggle(new Rect(SLX, (pos_y), ItemWidth - 120, ItemHeight), p_mscfg.Scale_HitCheckDetail, "HitCheck詳細設定", gsToggle);
                if (cfg.doHitScaleDef)
                {
                    Color cbk = GUI.color;
                    GUI.color = Color.yellow;
                    GUIStyle gsLabel2 = new GUIStyle(gsLabel);
                    gsLabel2.alignment = TextAnchor.LowerLeft;
                    GUI.Label(new Rect(SLX + (ItemWidth - 125), (pos_y + 2), 120, ItemHeight), "《登録有り》", gsLabel2);

                    if (GUI.Button(new Rect(SLX + (ItemWidth - 55), (pos_y + 2), 50, ItemHeight), "→適用", gsButton))
                    {
                        foreach (var h in HitCheckTgtStr)
                        {
                            p_mscfg.GetHitDetail(h.Key) = cfg.HitScaleDef[(int)h.Key];
                        }

                        if (master && !master.boMAN)
                            UpdateHitScaleDef(master, newsc_m, cfg.HitScaleDef, true);
                        if (!slave.boMAN)
                            UpdateHitScaleDef(slave, newsc, cfg.HitScaleDef, true);
                    }
                    GUI.color = cbk;
                }



#if false //操作時のみに変更
                else if (p_mscfg.Scale_HitCheckEffect != 1f)
                {
                    //初回適用
                    if (master && !master.boMAN /*&& !HitScaleChangedMaids.Contains(master)*/)
                        UpdateHitScale(master, newsc_m * newsc_h);
                    if (!slave.boMAN /*&& !HitScaleChangedMaids.Contains(slave)*/)
                        UpdateHitScale(slave, newsc * newsc_h);
                }
#endif
                rcItem.y = (pos_y += 20);
                rcItem.y = (pos_y += 20);
#if true
                if (master != null)
                {
                    GUI.Label(rcItem, "【表示設定】", gsLabel);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    Maid tgtman = (master.boMAN ? master : slave);
                    FieldInfo fiml = typeof(TBodySkin).GetField("m_listManAlphaMat", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (fiml != null)
                    {
                        float manAlpha = GetManAlpha(tgtman);
                        bool holdAlphaBk = CommonEdit.LoadManAlpha(master, out float f);
                        if (holdAlphaBk)
                            manAlpha = f;

                        GUI.Label(rcItem, (master.boMAN ? "Master" : "Slave") + "の透明度: " + Math.Round(manAlpha, 1), gsLabel);

                        Color cbk = GUI.color;
                        if (holdAlphaBk)
                            GUI.color = Color.yellow;
                        bool holdAlpha = GUI.Toggle(new Rect(SLX + SEDTW - 60, (pos_y), 60, ItemHeight), holdAlphaBk, "Hold", gsToggle);
                        if (holdAlpha != holdAlphaBk)
                        {
                            if (holdAlpha)
                                CommonEdit.SaveManAlpha(master, manAlpha);
                            else
                                CommonEdit.SaveManAlpha(master, -1f);
                        }
                        GUI.color = cbk;

                        if (GUI.Button(new Rect(SLX + SEDTW, (pos_y), 28, 20), "|", gsButton))
                        {
                            manAlpha = GameMain.Instance.CMSystem.ManAlpha;
                            SetManAlpha(tgtman, manAlpha);

                            if (holdAlpha)
                                CommonEdit.SaveManAlpha(master, manAlpha);
                            //ms_.holdManAlpha = manAlpha;
                        }
                        pos_y = (int)(rcItem.y += ItemHeight);

                        int newA = (int)GUI.HorizontalSlider(new Rect(rcItem.x, rcItem.y, rcItem.width, 15), manAlpha, 0, 100);
                        if (newA != manAlpha)
                        {
                            SetManAlpha(tgtman, newA);

                            if (holdAlpha)
                                CommonEdit.SaveManAlpha(master, newA);
                            //ms_.holdManAlpha = manAlpha;
                        }
                        pos_y = (int)(rcItem.y += ItemHeight / 4 * 3);
                    }
                }//master!=null
#else
                GUI.Label(rcItem, "男モデルの透明度: " + GameMain.Instance.CMSystem.ManAlpha, gsLabel);
                pos_y = (int)(rcItem.y += ItemHeight);

                var manAlpha = (int)GUI.HorizontalSlider(rcItem, GameMain.Instance.CMSystem.ManAlpha, 0, 100);
                if (manAlpha != GameMain.Instance.CMSystem.ManAlpha)
                {
                    GameMain.Instance.CMSystem.ManAlpha = manAlpha;
                    //GameMain.Instance.CMSystem.ConfigScreenApply(); //bugfix v0011
                    if (GameMain.Instance.CharacterMgr != null) //bugfix v0011
                    {
                        GameMain.Instance.CharacterMgr.ManAlphaUpdate();
                    }
                }
                pos_y = (int)(rcItem.y += ItemHeight);
#endif
                if (master != null)
                {
                    if (!master.boMAN)
                    {
                        bool bochk = GetStateMaskItemsAll(master);
                        bool bonew = GUI.Toggle(rcItem, bochk, "Masterのメイドを隠す(Node消去&Mask)", gsToggle);
                        pos_y = (int)(rcItem.y += ItemHeight);
                        if (bochk != bonew)
                        {
                            if (bonew)
                                MaskItemsAll(master);
                            else
                                ResetMaskItemsAll(master);
                        }
                    }
                    else if (master.boMAN)
                    {
                        bool bochk = !GetManVisible(master);
                        bool bonew = GUI.Toggle(rcItem, bochk, "Masterの男を隠す(Node消去&Mask)", gsToggle);
                        pos_y = (int)(rcItem.y += ItemHeight);
                        if (bochk != bonew)
                        {
                            SetManVisible(master, !bonew);
                        }
                    }

                    //if (master.boMAN) m/s共通に
                    {
                        Maid tgtman = null;
                        bool bochk = false;
                        bool bocnk = false;
                        if (master.boMAN)
                        {
                            tgtman = master;
                            bochk = GetChinkoVisible(master.body0);

                            // v0031
                            Color cbk = GUI.color;
                            if (SetChinkoScaleMens.ContainsKey(master.body0))
                            {
                                GUI.color = Color.yellow;
                            }
                            bocnk = GUI.Toggle(new Rect(rcItem.x, rcItem.y, rcItem.width - 80, rcItem.height), bochk, "Masterの局部を表示", gsToggle);
                            pos_y = (int)(rcItem.y += ItemHeight);
                            GUI.color = cbk;
                        }
                        else if (slave.boMAN)
                        {
                            tgtman = slave;
                            bochk = GetChinkoVisible(slave.body0);

                            // v0031
                            Color cbk = GUI.color;
                            if (SetChinkoScaleMens.ContainsKey(slave.body0))
                            {
                                GUI.color = Color.yellow;
                            }
                            bocnk = GUI.Toggle(new Rect(rcItem.x, rcItem.y, rcItem.width - 80, rcItem.height), bochk, "Slaveの局部を表示", gsToggle);
                            pos_y = (int)(rcItem.y += ItemHeight);
                            GUI.color = cbk;
                        }

                        if (bochk != bocnk)
                        {
                            //SetTamabkrVisible(master, true);
                            //tgtman.body0.SetChinkoVisible(bocnk);
                            XtMasterSlave.SetChinkoVisible(tgtman.body0, bocnk); //v0030 fix
                        }

                        if (tgtman && /*master.body0.trManChinko != null && */(bocnk || showChinkoSlider))
                        {
                            //chkサイズスライダー
                            const int SLDW = 220;
                            const int LX = ItemX + 10;
                            const int BW = 25;
                            const int LW = SLDW - BW * 3 - 5;
                            const int EDTW = LW + BW * 2 + 5;
                            pos_y = (int)(rcItem.y -= ItemHeight);
                            {
                                GUI.Label(new Rect(LX + ItemWidth - 70 + 15, pos_y, 25, ItemHeight), "調整", gsLabel);

                                if (GUI.Button(new Rect(LX + ItemWidth - 70 + 40, (pos_y), 20, 20), (showChinkoSlider ? "-" : "+"), gsButton))
                                {
                                    showChinkoSlider = !showChinkoSlider;

                                    if (showChinkoSlider)
                                    {
                                        showSlidersHint();
                                    }
                                }
                            }
                            pos_y = (int)(rcItem.y += ItemHeight);

                            if (showChinkoSlider)
                            {
                                float s = GetChinkoScale(tgtman.body0).x;
                                float _s = s;
                                GUI.Label(new Rect(LX, (pos_y), 122, ItemHeight), " サイズ: " + Math.Round(_s, 3), gsLabel);
                                _s = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _s, gsButton);
                                if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "|", gsButton))
                                {
                                    _s = 1f;
                                }
                                pos_y = (int)(rcItem.y += ItemHeight);
                                _s = GUI.HorizontalSlider(new Rect(LX, (pos_y), SLDW, 18), _s, 0f, 2f);

#if false
                                if (s != _s/* || _v != ms_.chinko_dpos*/)
                                {
                                    /*if (_s != 1)
                                        ms_.chinkoSizeChangedMan = tgtman;
                                    else
                                        ms_.chinkoSizeChangedMan = null;
                                        */
                                    SetChinkoScale(tgtman.body0, _s/*, _v*/);
                                    //ms_.chinko_dpos = _v;
                                }
#else
                                Vector3 _v = GetChinkoPos(tgtman.body0);//ms_.chinko_dpos;
                                Vector3 v0 = _v;
                                {
                                    GUI.Label(new Rect(LX, (pos_y += ItemHeight), 122, ItemHeight), " +X: " + Math.Round(_v.x, 4), gsLabel);
                                    _v.x = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.x, gsButton);
                                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                                    {
                                        _v.x = GetInitChinkoPos(tgtman.body0).x;
                                    }
                                    _v.x = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.x, -0.15f, 0.15f);

                                    GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +Y: " + Math.Round(_v.y, 4), gsLabel);
                                    _v.y = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.y, gsButton);
                                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                                    {
                                        _v.y = GetInitChinkoPos(tgtman.body0).y;
                                    }
                                    _v.y = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.y, -0.15f, 0.15f);

                                    GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +Z: " + Math.Round(_v.z, 4), gsLabel);
                                    _v.z = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.z, gsButton);
                                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                                    {
                                        _v.z = GetInitChinkoPos(tgtman.body0).z;
                                    }
                                    _v.z = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.z, -0.15f, 0.15f);

                                    rcItem.y = (pos_y);
                                    //_v = v3limit(_v, 0.15f);
                                }

                                if (s != _s)
                                {
                                    SetChinkoScale(tgtman.body0, _s);
                                }

                                if (_v != v0)
                                {
                                    SetChinkoScale(tgtman.body0, _s);
                                    SetChinkoPos(tgtman.body0, _v);
                                    //ms_.chinko_dpos = _v;
                                }
#endif
                                pos_y = (int)(rcItem.y += ItemHeight);
                            }

                            /*モザイク対応が面倒なのでやめ
                            bool bochk2 = !GetTamabkrVisible(master);
                            bool bonew = GUI.Toggle(new Rect(rcItem.x + (rcItem.width - 80), rcItem.y - ItemHeight, 80, rcItem.height), bochk2, "玉を隠す", gsToggle);
                            if (bochk2 != bonew)
                            {
                                SetTamabkrVisible(master, !bonew);
                            }*/
                        }
                    }

                    /*共通処理化
                     * if (slave.boMAN)
                    {
                        bool bochk = GetChinkoVisible(slave.body0);
                        bool bocnk = GUI.Toggle(rcItem, bochk, "Slaveの局部を表示", gsToggle);
                        pos_y = (int)(rcItem.y += ItemHeight);
                        if (bochk != bocnk)
                        {
                            slave.body0.SetChinkoVisible(bocnk);
                        }
                    }*/
                    //pos_y = (int)(rcItem.y += ItemHeight / 2);
                }

                _WinprocPhase = "[masks]";

                if (!slave.boMAN)
                {
                    //pos_y = (int)(rcItem.y += ItemHeight / 2);

                    if (GUI.Button(new Rect(ItemX, (pos_y), 20, 20), (showSlvMask ? "-" : "+"), gsButton))
                    {
                        showSlvMask = !showSlvMask;
                    }
                    GUI.Label(new Rect(ItemX + 20, pos_y, ItemWidth - 20 - 90, ItemHeight), "Slave衣装表示設定", gsLabel);

                    ms_.CheckSlvMaskSlave(slave);

                    Color cbklt = GUI.color;
                    if (ms_.holdSlvMask && ms_.holdSlvMaskItems.Count > 0)
                    {
                        GUI.color = Color.yellow;
                    }
                    bool holdSlvMask_t = GUI.Toggle(new Rect(ItemX + 20 + (ItemWidth - 20 - 90), pos_y, 90, ItemHeight), ms_.holdSlvMask, "マスクを保持", gsToggle);
                    GUI.color = cbklt;

                    if (holdSlvMask_t != ms_.holdSlvMask)
                    {
                        ms_.holdSlvMask = holdSlvMask_t;
                        if (!ms_.holdSlvMask)
                        {
                            ms_.holdSlvMaskMaid = null;
                            ms_.holdSlvMaskItems.Clear();
                        }
                        else
                        {
                            ms_.holdSlvMaskMaid = slave;
                            foreach (var item in dicMaskItems)
                            {
                                bool vflg = false;
                                foreach (var v in item.Value)
                                {
                                    vflg |= slave.body0.GetMask(v);
                                }
                                if (!vflg)
                                    ms_.holdSlvMaskItems.Add(item.Key);
                            }
                        }
                    }
                    pos_y = (int)(rcItem.y += ItemHeight);

                    if (showSlvMask)
                    {
                        Rect rcbox = new Rect(rcItem);
                        rcbox.height = ((float)Math.Ceiling(dicMaskItems.Count / 2.0) + 0.5f) * ItemHeight;
                        GUI.Box(rcbox, "");

                        Rect rcman = new Rect(rcItem);
                        rcman.x = 0;

                        int i = 0;
                        foreach (var item in dicMaskItems)
                        {
                            bool vflg = false;
                            foreach (var v in item.Value)
                            {
                                vflg |= slave.body0.GetMask(v);
                            }

                            rcman.x = ((i % 2 == 0) ? rcItem.x : (rcItem.x + rcman.width)) + 10;
                            rcman.width = (i % 2 == 0) ? (rcItem.width / 2) : (rcItem.width / 2 - 10);

                            bool nflg0 = vflg;
                            Color cbkl = GUI.color;
                            if (ms_.holdSlvMask && ms_.holdSlvMaskItems.Contains(item.Key))
                            {
                                GUI.color = Color.yellow;
                                nflg0 = false;
                            }

                            bool nflg = GUI.Toggle(rcman, nflg0, item.Key, gsToggle);
                            GUI.color = cbkl;

                            if ((i % 2 == 1))
                                rcman.y += ItemHeight;
                            //  rcman.y = pos_y = (int)(rcItem.y += ItemHeight);

                            if (ms_.holdSlvMask && nflg != nflg0)
                            {
                                if (nflg)
                                    ms_.holdSlvMaskItems.Remove(item.Key);
                                else
                                    ms_.holdSlvMaskItems.Add(item.Key);
                            }

                            if (vflg != nflg)
                            {
                                foreach (var v in item.Value)
                                {
                                    slave.body0.SetMask(v, nflg);
                                }
                            }
                            i++;
                        }
                        pos_y = (int)(rcItem.y += rcbox.height);
                    }
                }

                if (master != null)
                {
                    //pos_y = (int)(rcItem.y += ItemHeight /4*3);
                    pos_y = (int)(rcItem.y += ItemHeight);
                    _WinprocPhase = "[voice]";

                    if (!slave.boMAN)
                    {
                        GUI.Label(rcItem, "【演出設定】", gsLabel);
                        pos_y = (int)(rcItem.y += ItemHeight);


                        // if (ms_.mdSlave_No > 0)
                        if (ms_.mdSlave_No > 0 || !bIsYotogiScene) //v5.0
                            GUI.enabled = true;
                        else
                            GUI.enabled = false;
                        /*Color cbk = GUI.contentColor;
                        if (ms_.mdSlave_No > 0)
                            GUI.enabled = true;
                        else
                            GUI.contentColor = Color.gray;*/

                        Rect rctmp0 = new Rect(rcItem.x, rcItem.y, (rcItem.width - 65), rcItem.height);
                        p_mscfg.doVoiceAndFacePlay = GUI.Toggle(rctmp0, p_mscfg.doVoiceAndFacePlay, "Slaveサブメイド夜伽演出", gsToggle);
                        pos_y = (int)(rcItem.y += ItemHeight);
                        if (p_mscfg.doVoiceAndFacePlay)
                        {
                            rctmp0.x += rctmp0.width;
                            rctmp0.width = rcItem.width - rctmp0.width;
                            bool voiceStop = !GUI.Toggle(rctmp0, !p_mscfg.doVoiceDisabled, "ボイス", gsToggle);
                            if (voiceStop != p_mscfg.doVoiceDisabled)
                            {
                                p_mscfg.doVoiceDisabled = voiceStop;
                                if (voiceStop && ms_.bVoicePlaying)
                                {
                                    slave.AudioMan.Stop(0f);
                                    ms_.bVoicePlaying = false;
                                }
                            }

                            if (GUI.Button(new Rect(ItemX + 10, (pos_y), 20, 20), (showVymPlaySet ? "-" : "+"), gsButton))
                            {
                                showVymPlaySet = !showVymPlaySet;
                            }
                            GUI.Label(new Rect(ItemX + 20 + 10, pos_y, ItemWidth - 20 - 10, ItemHeight), "夜伽演出オプション", gsLabel);
                            pos_y = (int)(rcItem.y += ItemHeight);

                            if (showVymPlaySet)
                            {
                                Rect rctmp = new Rect(rcItem.x + 10, rcItem.y, (rcItem.width - 10) / 3, rcItem.height);
                                VYMModule.VymModule.cfg.HohoEnabled = GUI.Toggle(rctmp, VYMModule.VymModule.cfg.HohoEnabled, "頬染め", gsToggle);
                                rctmp.x += rctmp.width;
                                VYMModule.VymModule.cfg.YodareEnabled = GUI.Toggle(rctmp, VYMModule.VymModule.cfg.YodareEnabled, "ヨダレ", gsToggle);
                                rctmp.x += rctmp.width;
                                VYMModule.VymModule.cfg.NamidaEnabled = GUI.Toggle(rctmp, VYMModule.VymModule.cfg.NamidaEnabled, "ナミダ", gsToggle);
                                pos_y = (int)(rcItem.y += ItemHeight);

                                GUI.Label(new Rect(ItemX + 10, pos_y, ItemWidth - 20, ItemHeight), "【ボイスモード選択】", gsLabel);
                                pos_y = (int)(rcItem.y += ItemHeight);
                                string[] e_names = Enum.GetNames(typeof(VYMModule.VymModule.VoiceMode));
                                for (int j = 0; j < e_names.Length; j++)
                                {
                                    // iniにカスタム名の指定があれば反映
                                    var custom = cfg.customNames.FirstOrDefault(x => x[0] == e_names[j]);
                                    if (custom != null)
                                        e_names[j] = custom[1];
                                }
                                rctmp = new Rect(ItemX + 10, pos_y, ItemWidth - 10, ItemHeight);
                                if (ComboVoiceMode.Show(rctmp, ItemHeight, ItemHeight * 4, e_names, null, gsButton, gsButton))
                                {
                                    if (ComboVoiceMode.sIndex >= 0)
                                    {
                                        VYMModule.VymModule.cfg.eVoiceMode = (VYMModule.VymModule.VoiceMode)ComboVoiceMode.sIndex;
                                    }
                                }
                                pos_y = (int)(rcItem.y += ItemHeight);
                                if (ComboVoiceMode.boPop)
                                {
                                    pos_y = (int)(rcItem.y += (ItemHeight * 4));
                                }

                                bool manu_e = GUI.Toggle(new Rect(ItemX + 10, pos_y, ItemWidth - 20, ItemHeight), p_mscfg.doManualVfPlay,
                                                            "マニュアルプレイモード", gsToggle);
                                pos_y = (int)(rcItem.y += ItemHeight);

                                if (manu_e != p_mscfg.doManualVfPlay)
                                {
                                    //音声停止判定
                                    p_mscfg.doManualVfPlay = manu_e;
                                    if (!manu_e && ms_.bVoicePlaying)
                                    {
                                        slave.AudioMan.Stop(0f);
                                        ms_.bVoicePlaying = false;
                                    }
                                }

                                if (p_mscfg.doManualVfPlay)
                                {
                                    GUI.Box(new Rect(ItemX + 10, pos_y, ItemWidth - 10, ItemHeight * 6), "");

                                    const int MvpX = ItemX + 20;
                                    if (p_mscfg.manuVf_iExcite >= 1000)
                                    {
                                        //拡張Lvスタン用
                                        GUI.Label(new Rect(MvpX, pos_y, ItemWidth, ItemHeight), "興奮: - スタン状態 -", gsLabel);
                                    }
                                    else
                                    {
                                        var tsize = gsLabel.CalcSize(new GUIContent("興奮: "));
                                        GUI.Label(new Rect(MvpX, pos_y, tsize.x, ItemHeight), "興奮: ", gsLabel);
                                        Color cbk_mn = GUI.color;

                                        if (p_mscfg.manuVf_iExcite < VymModule.cfg.vExciteLevelThresholdV1)
                                        {
                                            //vExciteLevel = 1;
                                            //GUI.color = Color.white;
                                        }
                                        else if (p_mscfg.manuVf_iExcite < VymModule.cfg.vExciteLevelThresholdV2)
                                        {
                                            //vExciteLevel = 2;
                                            GUI.color = Color.cyan;
                                        }
                                        else if (p_mscfg.manuVf_iExcite < VymModule.cfg.vExciteLevelThresholdV3)
                                        {
                                            //vExciteLevel = 3;
                                            GUI.color = Color.yellow;
                                        }
                                        else if (VymModule.cfg.vExciteLevelThresholdV3 <= p_mscfg.manuVf_iExcite)
                                        {
                                            //vExciteLevel = 4;
                                            GUI.color = Color.magenta;
                                        }
                                        GUI.Label(new Rect(MvpX + tsize.x, pos_y, ItemDw * 3, ItemHeight), "" + p_mscfg.manuVf_iExcite, gsLabel);

                                        p_mscfg.manuVf_iExcite = (int)GUI.HorizontalSlider(new Rect(ItemX + 10 + ItemDw * 4 - 10, pos_y + 5, ItemWidth - ItemDw * 4 - 10 - 45, ItemHeight - 5), p_mscfg.manuVf_iExcite, 0, 300);
                                        GUI.color = cbk_mn;
                                    }

                                    if (GUI.Toggle(new Rect(ItemX + 10 + ItemDw * 4 + (ItemWidth - ItemDw * 4 - 10 - 45), pos_y, 45, ItemHeight), (p_mscfg.manuVf_iExcite >= 1000), "Stun", gsToggle)/* && (p_mscfg.manuVf_iExcite < 1000)*/)
                                        p_mscfg.manuVf_iExcite = 1000;
                                    else if (p_mscfg.manuVf_iExcite >= 1000)
                                        p_mscfg.manuVf_iExcite = 300;
                                    pos_y = (int)(rcItem.y += ItemHeight);

                                    const int MNTGL4 = (ItemWidth - 20) / 4;
                                    Rect rctgl = new Rect(MvpX, pos_y, MNTGL4, ItemHeight);
                                    if (GUI.Toggle(rctgl, (p_mscfg.manuVf_mState == 10), "止", gsToggle) && (p_mscfg.manuVf_mState != 10))
                                        p_mscfg.manuVf_mState = 10;
                                    rctgl.x += rctgl.width;
                                    if (GUI.Toggle(rctgl, (p_mscfg.manuVf_mState == 20), "弱", gsToggle) && (p_mscfg.manuVf_mState != 20))
                                        p_mscfg.manuVf_mState = 20;
                                    rctgl.x += rctgl.width;
                                    if (GUI.Toggle(rctgl, (p_mscfg.manuVf_mState == 30), "強", gsToggle) && (p_mscfg.manuVf_mState != 30))
                                        p_mscfg.manuVf_mState = 30;
                                    rctgl.x += rctgl.width;
                                    if (GUI.Toggle(rctgl, (p_mscfg.manuVf_mState == 40), "余韻", gsToggle) && (p_mscfg.manuVf_mState != 40))
                                        p_mscfg.manuVf_mState = 40;
                                    rctgl.x += rctgl.width;

                                    pos_y = (int)(rcItem.y += ItemHeight);
                                    if (GUI.Button(new Rect(MvpX + 5, pos_y, ItemWidth - 30, ItemHeight), "強制絶頂の実行"
                                                            + (ms_.manuKyoseiZeccho <= 0 ? "" : " +" + ms_.manuKyoseiZeccho.ToString()), gsButton))
                                    {
                                        ms_.manuKyoseiZeccho++;
                                    }
                                    pos_y = (int)(rcItem.y += ItemHeight);

                                    GUI.Label(new Rect(MvpX, pos_y, ItemWidth - 20, ItemHeight * 3), "※表情、ボイスは設定変更時やMasterのモーション変化時にini設定テーブルからランダム選択", gsLabel);
                                    pos_y = (int)(rcItem.y += ItemHeight * 3);
                                }

                                VymModule.vMaidParam mp;
                                if (!VymModule.maidParam.TryGetValue(slave, out mp))
                                    mp = new VymModule.vMaidParam();
                                bool mz = GUI.Toggle(new Rect(ItemX + 10, pos_y, ItemDw * 6, ItemHeight), p_mscfg.manuVf_mOrgcmb >= 0, "絶頂数ロック: " + mp.vOrgasmCmb.ToString(), gsToggle);
                                if (!mz)
                                {
                                    GUI.enabled = false;
                                    GUI.HorizontalSlider(new Rect(ItemX + 10 + ItemDw * 6, pos_y + 5, ItemWidth - ItemDw * 6 - 10, ItemHeight - 5), mp.vOrgasmCmb, 0, 5);
                                    p_mscfg.manuVf_mOrgcmb = -1;
                                    GUI.enabled = true;
                                }
                                else
                                {
                                    if (p_mscfg.manuVf_mOrgcmb < 0)
                                        p_mscfg.manuVf_mOrgcmb = mp.vOrgasmCmb;
                                    p_mscfg.manuVf_mOrgcmb = (int)GUI.HorizontalSlider(new Rect(ItemX + 10 + ItemDw * 6, pos_y + 5, ItemWidth - ItemDw * 6 - 10, ItemHeight - 5), p_mscfg.manuVf_mOrgcmb, 0, 5);
                                }
                                pos_y = (int)(rcItem.y += ItemHeight);

                                p_mscfg.doZecchoKeiren = GUI.Toggle(new Rect(ItemX + 10, pos_y, ItemDw * 6, ItemHeight), p_mscfg.doZecchoKeiren, "絶頂痙攣 β: " + Math.Round(p_mscfg.fZecchoKeirenParam, 3), gsToggle);
                                if (p_mscfg.doZecchoKeiren)
                                    p_mscfg.fZecchoKeirenParam = GUI.HorizontalSlider(new Rect(ItemX + 10 + ItemDw * 6, pos_y + 5, ItemWidth - ItemDw * 6 - 10, ItemHeight - 5), p_mscfg.fZecchoKeirenParam, 0, 0.5f);
                                pos_y = (int)(rcItem.y += ItemHeight);
                            }

                        }

                        bool guibk_01 = GUI.enabled; 
                        if (ms_.mdSlave_No == 0 && !bIsYotogiScene) //v5.0
                            GUI.enabled = false;
                        p_mscfg.doFaceSync = GUI.Toggle(rcItem, p_mscfg.doFaceSync, "Slaveの表情をMaid0から複製", gsToggle);
                        if (ms_.mdSlave_No == 0 && !bIsYotogiScene) //v5.0
                            GUI.enabled = guibk_01;
                        pos_y = (int)(rcItem.y += ItemHeight);

                        if (bIsVymPlg && p_mscfg.doVoiceAndFacePlay)
                        {
                            Rect rcV = new Rect(rcItem);
                            rcV.width -= 56;
                            bool bo = GUI.Toggle(rcV, p_mscfg.doVoiceAndFacePlayOnVYM, "夜伽以外VibeYourMaidに連動", gsToggle);
                            if (bo != p_mscfg.doVoiceAndFacePlayOnVYM)
                            {
                                p_mscfg.doVoiceAndFacePlayOnVYM = bo;
                                //音声停止判定
                                if (!bo && ms_.bVoicePlaying)
                                {
                                    slave.AudioMan.Stop(0f);
                                    ms_.bVoicePlaying = false;
                                }
                            }
                            rcV.x += rcV.width;
                            rcV.width = 56;
                            p_mscfg.doVoiceAndFacePlayOnVYM_Zeccho = GUI.Toggle(rcV, p_mscfg.doVoiceAndFacePlayOnVYM_Zeccho, "⇔絶頂", gsToggle);

                            pos_y = (int)(rcItem.y += ItemHeight);
                            //GUI.enabled = true;
                        }

                        //pos_y = (int)(rcItem.y += ItemHeight);
                        GUI.enabled = true;
                        //GUI.contentColor = cbk;
                    }
                    else if (slave.boMAN)
                    {
                        GUI.Label(rcItem, "【演出設定】", gsLabel);
                        pos_y = (int)(rcItem.y += ItemHeight);
                    }
                }//master!=null

                if (!slave.boMAN)
                {
                    if (GUI.Button(new Rect(ItemX, (pos_y), 20, 20), (showSlaveEyeToTgt ? "-" : "+"), gsButton))
                    {
                        showSlaveEyeToTgt = !showSlaveEyeToTgt;
                    }
                    GUI.Label(new Rect(ItemX + 20, pos_y, ItemWidth - 20, ItemHeight), "Slave視線制御 (標準⇒カメラ)", gsLabel);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    if (showSlaveEyeToTgt)
                    {
                        const int LX = ItemX + 10;

                        {//Slave汎用
                            //Transform tgt_tr = GameMain.Instance.MainCamera.transform;
                            bool bochk = slave.body0.boHeadToCam;//(tgt_tr == slave.body0.trsLookTarget);

                            Rect rcb = new Rect(rcItem);
                            rcb.x = LX;
                            rcb.width -= 90;
                            bool bonew = GUI.Toggle(rcb, bochk, "Slaveの顔を向ける", gsToggle);
                            if (bochk != bonew)
                            {
                                slave.body0.boHeadToCam = bonew;
                                /*
                                if (bocnk)
                                    slave.EyeToCamera(Maid.EyeMoveType.目と顔を向ける, GameUty.MillisecondToSecond(500));
                                else
                                    slave.EyeToReset(GameUty.MillisecondToSecond(500));
                            */
                            }

                            bochk = slave.body0.boEyeToCam;//(tgt_tr == slave.body0.trsLookTarget);
                            rcb.x += rcb.width;
                            rcb.width = 90;
                            bonew = GUI.Toggle(rcb, bochk, "目を向ける", gsToggle);
                            if (bochk != bonew)
                            {
                                slave.body0.boEyeToCam = bonew;

                                /*if (bonew)
                                    slave.EyeToCamera(Maid.EyeMoveType.目と顔を向ける, GameUty.MillisecondToSecond(500));
                                else
                                    slave.EyeToReset(GameUty.MillisecondToSecond(500));*/
                            }
                        }

                        Rect rcItem2 = new Rect(rcItem);
                        rcItem2.x = LX;
                        rcItem2.width -= LX;

                        //Slave二人目以降オプション
                        if (!slave.boMAN && ms_.mdSlave_No > 0 && ms_.mdSlaves[0].mem)
                        {
                            pos_y = (int)(rcItem.y += ItemHeight);
                            rcItem2.y = pos_y;

                            Maid tgt = ms_.mdSlaves[0].mem;
                            string tgtBoneName = "Bip01 Head";
                            Transform tgt_tr = BoneLink.BoneLink.SearchObjName(tgt.body0.m_Bones.transform, tgtBoneName, true);
                            bool bochk = (tgt_tr == slave.body0.trsLookTarget);

                            bool bocnk = GUI.Toggle(rcItem2, bochk, "Slaveの視線をMaid0の顔に向ける", gsToggle);
                            if (bochk != bocnk)
                            {
                                if (bocnk)
                                    slave.EyeToTarget(tgt, GameUty.MillisecondToSecond(500), tgtBoneName);
                                else
                                    slave.EyeToReset(GameUty.MillisecondToSecond(500));
                            }
                        }
                        //pos_y = (int)(rcItem.y += ItemHeight);


                        if (!slave.boMAN && ms_.mdSlave_No != 0 && ms_.mdSlaves[0].mem)
                        {
                            pos_y = (int)(rcItem.y += ItemHeight);
                            rcItem2.y = pos_y;

                            Maid tgt = ms_.mdSlaves[0].mem;
                            string tgtBoneName = "_IK_vagina";
                            Transform tgt_tr = BoneLink.BoneLink.SearchObjName(tgt.body0.m_Bones.transform, tgtBoneName, true);
                            bool bochk = (tgt_tr == slave.body0.trsLookTarget);

                            bool bocnk = GUI.Toggle(rcItem2, bochk, "Slaveの視線をMaid0の秘部に向ける", gsToggle);
                            if (bochk != bocnk)
                            {
                                if (bocnk)
                                    slave.EyeToTarget(tgt, GameUty.MillisecondToSecond(500), tgtBoneName);
                                else
                                    slave.EyeToReset(GameUty.MillisecondToSecond(500));
                            }

                            pos_y = (int)(rcItem.y += ItemHeight / 2);
                        }

                        //Slaveメイド時オプション
                        if (!slave.boMAN && master)
                        {
                            pos_y = (int)(rcItem.y += ItemHeight);
                            rcItem2.y = pos_y;

                            Maid tgt = master;
                            string tgtBoneName = "ManBip Head";
                            Transform tgt_tr = BoneLink.BoneLink.SearchObjName(tgt.body0.m_Bones.transform, tgtBoneName, true);
                            bool bochk = (tgt_tr == slave.body0.trsLookTarget);

                            bool bocnk = GUI.Toggle(rcItem2, bochk, "Slaveの視線をMasterの顔に向ける", gsToggle);
                            if (bochk != bocnk)
                            {
                                if (bocnk)
                                    slave.EyeToTarget(tgt, GameUty.MillisecondToSecond(500), tgtBoneName);
                                else
                                    slave.EyeToReset(GameUty.MillisecondToSecond(500));
                            }
                        }
                        if (!slave.boMAN && master)
                        {
                            pos_y = (int)(rcItem.y += ItemHeight);
                            rcItem2.y = pos_y;

                            Maid tgt = master;
                            string tgtBoneName = "chinko2";
                            Transform tgt_tr = BoneLink.BoneLink.SearchObjName(tgt.body0.m_Bones.transform, tgtBoneName, true);
                            bool bochk = (tgt_tr == slave.body0.trsLookTarget);

                            bool bocnk = GUI.Toggle(rcItem2, bochk, "Slaveの視線をMasterの局部に向ける", gsToggle);
                            if (bochk != bocnk)
                            {
                                if (bocnk)
                                    slave.EyeToTarget(tgt, GameUty.MillisecondToSecond(500), tgtBoneName);
                                else
                                    slave.EyeToReset(GameUty.MillisecondToSecond(500));
                            }
                        }

                        pos_y = (int)(rcItem.y += ItemHeight);
                    }
                }

                // ハンドアタッチカスタム
                var bkflg = p_mscfg.doIKTargetMHandSpCustom;
                p_mscfg.doIKTargetMHandSpCustom =
                    GUI.Toggle(new Rect(ItemX, pos_y, ItemWidth -60, ItemHeight), p_mscfg.doIKTargetMHandSpCustom,
                    "Slave 手のアタッチ先変更", gsToggle);

                if (p_mscfg.doIKTargetMHandSpCustom)
                {
                    p_mscfg.doIKTargetMHandSpCustom_v2 =
                    GUI.Toggle(new Rect(ItemX + (ItemWidth - 60), pos_y, 60, ItemHeight), p_mscfg.doIKTargetMHandSpCustom_v2,
                    "v2", gsToggle);
                }

                // アタッチ設定の変更チェック
                if (p_mscfg.doIKTargetMHandSpCustom != bkflg)
                {
                    if (p_mscfg.doIKTargetMHandSpCustom)
                    {
                        // 元の値をバックアップ
                        ms_.bkupHandTgt = new BkupHandsAtc(slave.body0);
                    }
                    else if (ms_.bkupHandTgt != null)
                    {
                        // アタッチの復元
                        if (!ms_.bkupHandTgt.RestoreAtc(ref slave.body0))
                        {
                            // 失敗ならアタッチの解除
                            slave.IKTargetClear();
                        }
                        ms_.bkupHandTgt = null;
                    }
                }

                if (p_mscfg.doIKTargetMHandSpCustom && ms_.bkupHandTgt != null
                    && ms_.bkupHandTgt.bkupbody != slave.body0)
                {
                    // メイド変更あり、元の値を復元
                    ms_.bkupHandTgt.RestoreAtc(ref ms_.bkupHandTgt.bkupbody);

                    // メイド変更あり、元の値を再バックアップ
                    ms_.bkupHandTgt = new BkupHandsAtc(slave.body0);
                }

                pos_y = (int)(rcItem.y += ItemHeight);

                if (!p_mscfg.doIKTargetMHandSpCustom)
                {
                    // サブ項目無効
                    if (showHandTTPosSlider)
                        showHandTTPosSlider = false;
                }
                //if (p_mscfg.doIKTargetMHandSpCustom)
                else
                {
                    void change_tgt(bool boR, ref ATgtChar atgt, int inc)
                    {
                        atgt += inc;
                        chkatgt(ref atgt);

                        void chkatgt(ref ATgtChar atgt_)
                        {
                            if ((int)atgt_ < 0)
                                atgt_ = ATgtChar.None;

                            if ((int)atgt_ >= Enum.GetNames(typeof(ATgtChar)).Length)
                                atgt_ = ATgtChar.None;
                        }

                        while (GetHandAtcTgt(boR, slave, master, p_mscfg) == null) //居ないのは無視
                        {
                            if (atgt == ATgtChar.None)
                                break;
                            atgt += inc;
                            chkatgt(ref atgt);
                        }
                        chkatgt(ref atgt);

                        if (boR)
                        {
                            p_mscfg.doIKTargetMHandSpR_TgtBone = string.Empty;
                            if (atgt == ATgtChar.None)
                            {
                                //解除
                                AtccHand1R(slave, null, string.Empty, Vector3.zero);
                            }
                        }
                        else
                        {
                            p_mscfg.doIKTargetMHandSpL_TgtBone = string.Empty;
                            if (p_mscfg.doIKTargetMHandSpL_TgtChar == ATgtChar.None)
                            {
                                //解除
                                AtccHand1L(slave, null, string.Empty, Vector3.zero);
                            }
                        }
                    }

                    //rcItem.y = (pos_y += 20);
                    GUI.Label(new Rect(ItemX + 20, pos_y, 80, ItemHeight), "右手 対象:", gsLabel);
                    if (p_mscfg.doIKTargetMHandSpR_TgtChar != ATgtChar.None
                        && GUI.Button(new Rect(ItemX + 80, (pos_y), 20, 20), "<", gsButton))
                    {
                        change_tgt(true, ref p_mscfg.doIKTargetMHandSpR_TgtChar, -1);
                    }
                    if (GUI.Button(new Rect(ItemX + 100, (pos_y), ItemWidth - 100, 20), p_mscfg.doIKTargetMHandSpR_TgtChar.ToString(), gsButton))
                    {
                        change_tgt(true, ref p_mscfg.doIKTargetMHandSpR_TgtChar, +1);
                    }
                    rcItem.y = (pos_y += 20);

                    if (p_mscfg.doIKTargetMHandSpR_TgtChar != ATgtChar.None)
                    {
                        seth(true);
                    }

                    GUI.Label(new Rect(ItemX + 20, pos_y, 80, ItemHeight), "左手 対象:", gsLabel);
                    if (p_mscfg.doIKTargetMHandSpL_TgtChar != ATgtChar.None
                        && GUI.Button(new Rect(ItemX + 80, (pos_y), 20, 20), "<", gsButton))
                    {
                        change_tgt(false, ref p_mscfg.doIKTargetMHandSpL_TgtChar, -1);
                    }
                    if (GUI.Button(new Rect(ItemX + 100, (pos_y), ItemWidth - 100, 20), p_mscfg.doIKTargetMHandSpL_TgtChar.ToString(), gsButton))
                    {
                        change_tgt(false, ref p_mscfg.doIKTargetMHandSpL_TgtChar, +1);
                    }
                    rcItem.y = (pos_y += 20);

                    if (p_mscfg.doIKTargetMHandSpL_TgtChar != ATgtChar.None)
                    {
                        seth(false);
                    }

                    // 手のアタッチ変更と調整
                    void seth(bool boR)
                    {
                        Maid tgt = GetHandAtcTgt(boR, slave, master, p_mscfg);
                        if (!tgt)
                        {
                            return;
                        }

                        int LX = ItemX + 20;
                        string[] e_names; // = tgt.boMAN ? Defines.data.ManBones : Defines.data.MaidBones;

                        List<string> slist = new List<string>();
                        /*
                        TMorph t = tgt.body0.goSlot[(int)TBody.SlotID.body].morph;
                        if (t != null)
                        {
                            foreach (var p in t.dicAttachPoint)
                            {
                                slist.Add(p.Key);
                            }
                        }*/

                        // アタッチポイント一覧取得
                        slist.Add("無し");
                        for (int i = 0; i < tgt.body0.goSlot.Count; i++)
                        {
                            TMorph t = tgt.body0.goSlot[i].morph;
                            if (t == null)
                            {
                                continue;
                            }

                            foreach (var p in t.dicAttachPoint)
                            {
                                slist.Add(((TBody.SlotID)i).ToString() + "⇒" + p.Key);
                            }

                            // v5.0
                            string[] bone_names = tgt.boMAN ? Defines.data.ManBones : Defines.data.MaidBones;
                            foreach (var p in bone_names)
                            {
                                slist.Add(Defines.data.comboBonePrefix + p);
                            }
                        }
                        e_names = slist.ToArray();

                        // コンボボックス
                        var combo = boR ? ComboHandTgtBoneR : ComboHandTgtBoneL;
                        var selected = boR ? p_mscfg.doIKTargetMHandSpR_TgtBone : p_mscfg.doIKTargetMHandSpL_TgtBone;
                        if (string.IsNullOrEmpty(selected))
                            selected = "アタッチポイント選択";

                        Rect rctmp = new Rect(LX, pos_y, ItemWidth - 20 - 40, ItemHeight);
                        Rect rect_list = new Rect(rctmp);
                        rect_list.width = ItemWidth - 20;
                        rect_list.y += rctmp.height;
                        rect_list.height = ItemHeight * 4;
                        if (combo.Show(rctmp, ItemHeight, rect_list, e_names, selected, gsButton, gsButton))
                        {
                            v3OffsetsV2 v3of2 = new v3OffsetsV2(p_v3of, p_mscfg);
                            if (combo.sIndex >= 0)
                            {
                                if (boR)
                                {
                                    p_mscfg.doIKTargetMHandSpR_TgtBone = combo.sSelected;
                                    //AtccHand1R(slave, tgt, p_mscfg.doIKTargetMHandSpR_TgtBone, p_v3of.v3HandROffset);
                                    AtccHand1R(slave, tgt, p_mscfg.doIKTargetMHandSpR_TgtBone, v3of2.v3HandROffset);
                                }
                                else
                                {
                                    p_mscfg.doIKTargetMHandSpL_TgtBone = combo.sSelected;
                                    //AtccHand1L(slave, tgt, p_mscfg.doIKTargetMHandSpL_TgtBone, p_v3of.v3HandLOffset);
                                    AtccHand1L(slave, tgt, p_mscfg.doIKTargetMHandSpL_TgtBone, v3of2.v3HandLOffset);
                                }
                            }
                        }

                        var tgtstr = boR ? p_mscfg.doIKTargetMHandSpR_TgtBone : p_mscfg.doIKTargetMHandSpL_TgtBone;
                        bool open = showHandTTPosSlider && showPosSliderHandR == boR;

                        if (combo.boPop || (open && ATgtStr_IsNullOrEmpty(tgtstr)))
                        {
                            // 調整を一度閉じる
                            if (showHandTTPosSlider)
                            {
                                showHandTTPosSlider = false;
                                // Gizmo消す
                                GizmoHsVisible(showHandTTPosSlider);
                            }
                        }

                        if (ATgtStr_IsNullOrEmpty(tgtstr))
                        {
                            //if (showHandTTPosSlider)
                            //    showHandTTPosSlider = false;

                            GUI.enabled = false;
                            if (GUI.Button(new Rect(LX + (ItemWidth - 20 - 40), pos_y, 40, ItemHeight), "調整", gsButton))
                            { }
                            GUI.enabled = true;
                        }
                        else
                        {
                            if (GUI.Button(new Rect(LX + (ItemWidth - 20 - 40), pos_y, 40, ItemHeight), (open ? "閉" : "調整"), gsButton))
                            {
                                showHandTTPosSlider = !open;
                                showPosSliderHandR = boR;

                                GizmoHsVisible(showHandTTPosSlider);

                                // コンボを一度閉じる
                                if (combo.boPop)
                                {
                                    combo.boPop = false;
                                }
                            }
                            if (showHandTTPosSlider && showPosSliderHand)
                            {
                                // どちらもONならこっちを優先
                                showPosSliderHand = false;
                            }
                        }

                        if (showHandTTPosSlider && showPosSliderHandR == boR && !ATgtStr_IsNullOrEmpty(tgtstr))
                        {
                            v3OffsetsV2 v3of2 = new v3OffsetsV2(p_v3of, p_mscfg);

                            // 調整Gizmo&スライダー
                            if (ProcHandGizmo(slave, ref rcItem, ref pos_y, p_mscfg, p_v3of, false))
                            {
                                // 変化ありで反映
                                if (boR)
                                    AtccHand1R(slave, tgt, p_mscfg.doIKTargetMHandSpR_TgtBone, v3of2.v3HandROffset);
                                else
                                    AtccHand1L(slave, tgt, p_mscfg.doIKTargetMHandSpL_TgtBone, v3of2.v3HandLOffset);
                            }

                            //if (showHandTTPosSlider)
                            {
                                bool changeR = false, changeL = false;
                                pos_y = (int)(rcItem.y += ItemHeight);
                                if (boR)
                                {
                                    changeR = p_mscfg.doIKTargetMHandSpCustomAltRotR;

                                    p_mscfg.doIKTargetMHandSpCustomAltRotR =
                                        GUI.Toggle(new Rect(ItemX + 10, pos_y, ItemWidth, ItemHeight), p_mscfg.doIKTargetMHandSpCustomAltRotR,
                                        "アタッチ箇所からの角度指定(右手)", gsToggle);

                                    changeR = changeR != p_mscfg.doIKTargetMHandSpCustomAltRotR;
                                }
                                else
                                {
                                    changeL = p_mscfg.doIKTargetMHandSpCustomAltRotL;

                                    p_mscfg.doIKTargetMHandSpCustomAltRotL =
                                        GUI.Toggle(new Rect(ItemX + 10, pos_y, ItemWidth, ItemHeight), p_mscfg.doIKTargetMHandSpCustomAltRotL,
                                        "アタッチ箇所からの角度指定(左手)", gsToggle);

                                    changeL = changeL != p_mscfg.doIKTargetMHandSpCustomAltRotL;
                                }
#if DEBUG2
                                // MSリンク中にAltキー押しながら変更すると座標変換する v5.0
                                if (changeR && ms_.doMasterSlave && InputEx.GetModifierKey(InputEx.ModifierKey.Alt))
                                {
                                    // 右手
                                    //Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                                    Transform trh = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, GetHandBnR(master), true);
                                    var iks = slave.body0._ikp();
                                    var tgtMaid = iks.tgtMaidR;
                                    if (tgtMaid != null && tgtMaid.body0 != null && tgtMaid.body0.goSlot[iks.tgtHandR_AttachSlot].morph != null)
                                    {
                                        Vector3 vector2;
                                        Quaternion rotation2;
                                        tgtMaid.body0.goSlot[iks.tgtHandR_AttachSlot].morph.GetAttachPoint(iks.tgtHandR_AttachName, out vector2, out rotation2);

                                        // アタッチポイント基準角度
                                        if (p_mscfg.doIKTargetMHandSpCustomAltRotR)
                                            p_v3of.v3HandROffsetRot = (Quaternion.Inverse(rotation2) * trh.rotation * Quaternion.Euler(p_v3of.v3HandROffsetRot)).eulerAngles;
                                        else
                                            p_v3of.v3HandROffsetRot = (Quaternion.Inverse(trh.rotation) * rotation2 * Quaternion.Euler(p_v3of.v3HandROffsetRot)).eulerAngles;
                                    }
                                }
                                if (changeL && ms_.doMasterSlave && InputEx.GetModifierKey(InputEx.ModifierKey.Alt))
                                {
                                    // 左手
                                    //Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                                    Transform trh = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, GetHandBnL(master), true);
                                    var iks = slave.body0._ikp();
                                    var tgtMaid = iks.tgtMaidL;
                                    if (tgtMaid != null && tgtMaid.body0 != null && tgtMaid.body0.goSlot[iks.tgtHandL_AttachSlot].morph != null)
                                    {
                                        Vector3 vector2;
                                        Quaternion rotation2;
                                        tgtMaid.body0.goSlot[iks.tgtHandL_AttachSlot].morph.GetAttachPoint(iks.tgtHandL_AttachName, out vector2, out rotation2);

                                        // アタッチポイント基準角度
                                        if (p_mscfg.doIKTargetMHandSpCustomAltRotL)
                                            p_v3of.v3HandLOffsetRot = (Quaternion.Inverse(rotation2) * trh.rotation * Quaternion.Euler(p_v3of.v3HandLOffsetRot)).eulerAngles;
                                        else
                                            p_v3of.v3HandLOffsetRot = (Quaternion.Inverse(trh.rotation) * rotation2 * Quaternion.Euler(p_v3of.v3HandLOffsetRot)).eulerAngles;
                                    }
                                }
#endif
                            }
                        }

                        pos_y = (int)(rcItem.y += ItemHeight);
                        if (combo.boPop)
                        {
                            pos_y = (int)(rcItem.y += (ItemHeight * 4));
                        }
                        return; //seth
                    }
                }

                if (IkXT.IsNewIK)
                {
                    // 新IKを使うか？
                    var bkflg2 = p_mscfg.doIK159NewPointToDef;
                    p_mscfg.doIK159NewPointToDef =
                        GUI.Toggle(new Rect(ItemX, pos_y, ItemWidth - 95, ItemHeight), p_mscfg.doIK159NewPointToDef,
                        "新IK有効(IKの複製以外)", gsToggle);

                    if (IkXT.IsIkCtrlO118 || IkXT.IsIkCtrlO120)
                    {
                        var enabled = GUI.enabled;
                        if (!p_mscfg.doIK159NewPointToDef)
                        {
                            GUI.enabled = false;
                        }
                        p_mscfg.doFinalIKShoulderMove =
                            GUI.Toggle(new Rect(ItemX + (ItemWidth - 95), pos_y, 30, ItemHeight), p_mscfg.doFinalIKShoulderMove,
                            "肩", gsToggle);

                        p_mscfg.doFinalIKThighMove =
                            GUI.Toggle(new Rect(ItemX + (ItemWidth - 65), pos_y, 30, ItemHeight), p_mscfg.doFinalIKThighMove,
                            "腿", gsToggle);

                        p_mscfg.fFinalIKLegWeight =
                            GUI.Toggle(new Rect(ItemX + (ItemWidth - 35), pos_y, 30, ItemHeight), p_mscfg.fFinalIKLegWeight > 0f,
                            "足", gsToggle) ? 1.0f : 0.0f;

                        GUI.enabled = enabled;
                    }
                    pos_y = (int)(rcItem.y += ItemHeight);

                    p_mscfg.doIK159RotateToHands =
                        GUI.Toggle(new Rect(ItemX, pos_y, ItemWidth, ItemHeight), p_mscfg.doIK159RotateToHands,
                        "両手にｱﾀｯﾁでRotateIK有効(新IK2)", gsToggle);
                    pos_y = (int)(rcItem.y += ItemHeight);


                    // アタッチ設定の変更チェック
                    if (p_mscfg.doIK159NewPointToDef != bkflg2)
                    {
                        // 変更処理が必要になったら追加
                        // 毎フレームIKをセットしてるので現状無くても大丈夫なはず
                    }
                }

                p_mscfg.doBlendHandR =
                        GUI.Toggle(new Rect(ItemX, pos_y, ItemWidth, ItemHeight), p_mscfg.doBlendHandR,
                        "手指のブレンド（右手）", gsToggle);
                pos_y = (int)(rcItem.y += ItemHeight);
                if (p_mscfg.doBlendHandR)
                {
                    const int SLDS = ItemX + 10 + 90 + 5;
                    const int SLDW = 220 - 10 - 90;
                    GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "適用: " + Math.Round(p_mscfg.fBlendHandR, 2), gsLabel);
                    p_mscfg.fBlendHandR = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), p_mscfg.fBlendHandR, 0f, 1f);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "開度: " + Math.Round(p_mscfg.fBlendHandROpen, 2), gsLabel);
                    p_mscfg.fBlendHandROpen = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), p_mscfg.fBlendHandROpen, 0f, 1f);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "握り: " + Math.Round(p_mscfg.fBlendHandRGrip, 2), gsLabel);
                    p_mscfg.fBlendHandRGrip = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), p_mscfg.fBlendHandRGrip, 0f, 1f);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    p_mscfg.doAnimeHandR =
                        GUI.Toggle(new Rect(ItemX + 10, pos_y, ItemWidth, ItemHeight), p_mscfg.doAnimeHandR,
                        "アニメーション", gsToggle);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    if (p_mscfg.doAnimeHandR)
                    {
                        {
                            ref var val = ref p_mscfg.fAnimeHandRSpeed;
                            GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "速さ: " + Math.Round(val, 2), gsLabel);
                            val = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), val, 0f, 15f);
                            pos_y = (int)(rcItem.y += ItemHeight);
                        }

                        {
                            ref var val = ref p_mscfg.fAnimeHandRMove;
                            GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "握力: " + Math.Round(val, 2), gsLabel);
                            val = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), val, 0f, 1f);
                            pos_y = (int)(rcItem.y += ItemHeight);
                        }
                    }
                }

                p_mscfg.doBlendHandL =
                        GUI.Toggle(new Rect(ItemX, pos_y, ItemWidth, ItemHeight), p_mscfg.doBlendHandL,
                        "手指のブレンド（左手）", gsToggle);
                pos_y = (int)(rcItem.y += ItemHeight);
                if (p_mscfg.doBlendHandL)
                {
                    const int SLDS = ItemX + 10 + 90 + 5;
                    const int SLDW = 220 - 10 - 90;
                    GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "適用: " + Math.Round(p_mscfg.fBlendHandL, 2), gsLabel);
                    p_mscfg.fBlendHandL = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), p_mscfg.fBlendHandL, 0f, 1f);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "開度: " + Math.Round(p_mscfg.fBlendHandLOpen, 2), gsLabel);
                    p_mscfg.fBlendHandLOpen = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), p_mscfg.fBlendHandLOpen, 0f, 1f);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "握り: " + Math.Round(p_mscfg.fBlendHandLGrip, 2), gsLabel);
                    p_mscfg.fBlendHandLGrip = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), p_mscfg.fBlendHandLGrip, 0f, 1f);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    p_mscfg.doAnimeHandL =
                        GUI.Toggle(new Rect(ItemX + 10, pos_y, ItemWidth, ItemHeight), p_mscfg.doAnimeHandL,
                        "アニメーション", gsToggle);
                    pos_y = (int)(rcItem.y += ItemHeight);

                    if (p_mscfg.doAnimeHandL)
                    {
                        {
                            ref var val = ref p_mscfg.fAnimeHandLSpeed;
                            GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "速さ: " + Math.Round(val, 2), gsLabel);
                            val = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), val, 0f, 15f);
                            pos_y = (int)(rcItem.y += ItemHeight);
                        }

                        {
                            ref var val = ref p_mscfg.fAnimeHandLMove;
                            GUI.Label(new Rect(ItemX + 10, pos_y, 90, ItemHeight), "握力: " + Math.Round(val, 2), gsLabel);
                            val = GUI.HorizontalSlider(new Rect(SLDS, (pos_y), SLDW, ItemHeight), val, 0f, 1f);
                            pos_y = (int)(rcItem.y += ItemHeight);
                        }
                    }
                }

                //サブメンバーコントロール
                if (ProcSubMemberCtrls(ref pos_y, ref rcItem, ms_, slave, p_mscfg))
                {
                    //メンバー変更有
                    return;
                }


                //手のGizmo無効チェック
                if (GetGizmoHsVisible())
                {
                    // 位置設定で使ってなければ消す
                    if (!showPosSliderHand && !showHandTTPosSlider)
                        GizmoHsVisible(false);
                }

                _WinprocPhase = "[last]";

                pos_y = (int)(rcItem.y += ItemHeight / 2);
                pos_y = (int)(rcItem.y += ItemHeight);

                EditScroll_cfg_sizeY = pos_y + ItemHeight;
            }
            catch
            {
                throw;
            }
            finally
            {
                GUI.EndScrollView();
            }

            //ギズモ移動処理
            PosGizmoProc(ms_);
        }

        // アタッチターゲット取得
        static Maid GetHandAtcTgt(bool boR, Maid slave, Maid master, MsLinkConfig p_mscfg)
        {
            Maid tgt = null;

            var chk = boR ? p_mscfg.doIKTargetMHandSpR_TgtChar
                : p_mscfg.doIKTargetMHandSpL_TgtChar;
            switch (chk)
            {
                case ATgtChar.Self:
                    tgt = slave;
                    break;
                case ATgtChar.Master:
                    tgt = master;
                    break;
                case ATgtChar.Maid0:
                    if (slave != _MaidList[0].mem && _MaidList[0].mem)
                        tgt = _MaidList[0].mem;
                    break;
            }

            if (chk >= ATgtChar.Maid1)
            {
                int i = chk - ATgtChar.Maid0;
                if (i < _MaidList.Count())
                {
                    if (slave != _MaidList[i].mem && _MaidList[i].mem)
                        tgt = _MaidList[i].mem;
                }
            }

            return tgt;
        }

        // Gizmoの回転差分Vector3を得る
        Vector3 CalcGizmoDvRot(OhMyGizmo gz)
        {
            if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
            {
                gz.transform.rotation = Quaternion.Slerp(gz._backup_rot, gz.rotation, GIZMO_SLOWRATE);
            }

            var q = (Quaternion.Inverse(gz._backup_rotLocal_u1) * gz.transform.localRotation);
            var v3 = q.eulerAngles;

            return va180(v3);
        }

        // 回転初期値
        void BkupGizmoU1(OhMyGizmo gz, Vector3 dv)
        {
            // 開始角度を保持
            gz.BkupPosAndRotLocalU1();
            gz._backup_rotLocal_u1 = gz._backup_rotLocal_u1 * Quaternion.Inverse(Quaternion.Euler(dv));
        }

        // 手のアタッチ調整ギズモ＆スライダー
        private bool ProcHandGizmo(Maid slave, ref Rect rcItem, ref int pos_y, MsLinkConfig p_mscfg, v3Offsets p_v3of_org, bool boSelectLR = true)
        {
            bool bochg = false;
            v3OffsetsV2 p_v3of2 = boSelectLR ? new v3OffsetsV2(p_v3of_org, false) : new v3OffsetsV2(p_v3of_org, p_mscfg); // v5.0

            if (slave)
            {
                const int SLDW = 220;
                const int LX = ItemX + 10;
                const int BW = 25;
                const int LW = SLDW - BW * 3 - 5;
                const int EDTW = LW + BW * 2 + 5;

                // v5.0 カスタムアタッチの座標ズレを防ぐために反映
                bool cusR = false, cusL = false;
                Quaternion qCusR = Quaternion.identity, qCusL = Quaternion.identity;
                if (p_mscfg.doIKTargetMHandSpCustom && p_mscfg.doIKTargetMHandSpCustom_v2)
                {
                    _MSlinks[_pageNum].handsAtcpProc();

                    // カスタムアタッチ
                    if (p_mscfg.doIKTargetMHandSpCustomAltRotR && p_mscfg.doIKTargetMHandSpR_TgtChar != ATgtChar.None)
                    {
                        // アタッチ先座標
                        var iks = slave.body0._ikp();
                        if (iks != null && iks.tgtMaidR != null && iks.tgtHandR_AttachSlot >= 0 && !string.IsNullOrEmpty(iks.tgtHandR_AttachName))
                        {
                            if (iks.tgtMaidR.body0 != null && iks.tgtMaidR.body0.goSlot[iks.tgtHandR_AttachSlot].morph != null)
                            {
                                Vector3 vector2;
                                Quaternion rotation2;
                                iks.tgtMaidR.body0.goSlot[iks.tgtHandR_AttachSlot].morph.GetAttachPoint(iks.tgtHandR_AttachName, out vector2, out rotation2);
                                //Console.WriteLine("g-r");

                                // アタッチポイント基準角度
                                Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " R Hand", true);
                                //Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                                qCusR = trh.rotation = rotation2 * Quaternion.Euler(p_v3of2.v3HandROffsetRot);
                                cusR = true;
                            }
                        }
                        else if (iks != null && iks.tgtHandR)
                        {
                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " R Hand", true);
                            //Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                            qCusR = trh.rotation = iks.tgtHandR.rotation * Quaternion.Euler(p_v3of2.v3HandROffsetRot);
                            cusR = true;
                        }
                    }

                    if (p_mscfg.doIKTargetMHandSpCustomAltRotL && p_mscfg.doIKTargetMHandSpL_TgtChar != ATgtChar.None)
                    {
                        // アタッチ先座標
                        var iks = slave.body0._ikp();
                        if (iks != null && iks.tgtMaidL != null && iks.tgtHandL_AttachSlot >= 0 && !string.IsNullOrEmpty(iks.tgtHandL_AttachName))
                        {
                            if (iks.tgtMaidL.body0 != null && iks.tgtMaidL.body0.goSlot[iks.tgtHandL_AttachSlot].morph != null)
                            {
                                //Console.WriteLine("g-l");
                                Vector3 vector2;
                                Quaternion rotation2;
                                iks.tgtMaidR.body0.goSlot[iks.tgtHandL_AttachSlot].morph.GetAttachPoint(iks.tgtHandL_AttachName, out vector2, out rotation2);

                                // アタッチポイント基準角度
                                Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " L Hand", true);
                                //Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                                qCusL = trh.rotation = rotation2 * Quaternion.Euler(p_v3of2.v3HandLOffsetRot);
                                cusL = true;
                            }
                        }
                        else if (iks != null && iks.tgtHandL)
                        {
                            Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " L Hand", true);
                            //Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                            qCusL = trh.rotation = iks.tgtHandL.rotation * Quaternion.Euler(p_v3of2.v3HandLOffsetRot);
                            cusL = true;
                        }
                    }
                }


                if (showPosSliderHandR)
                {
                    _Gizmo_HandR.Visible = true;
                    _Gizmo_HandL.Visible = false;
                }
                else
                {
                    _Gizmo_HandR.Visible = false;
                    _Gizmo_HandL.Visible = true;
                }

#if false
                        if (showPosSliderHandR && _Gizmo_HandR.isDrag)
                        {
                            Vector3 dv = _Gizmo_HandR.transform.localPosition - _Gizmo_HandR._backup_pos;
                            if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                            {
                                dv *= GIZMO_SLOWRATE;
                                _Gizmo_HandR.transform.localPosition = _Gizmo_HandR._backup_pos + dv;
                            }
                            p_v3of.v3HandROffset += dv;
                            p_v3of.v3HandROffset = v3limit(p_v3of.v3HandROffset, 0.15f);
                            _Gizmo_HandR.BkupPosAndRotLocal();
                        }
                        if (!showPosSliderHandR && _Gizmo_HandL.isDrag)
                        {
                            Vector3 dv = _Gizmo_HandL.transform.localPosition - _Gizmo_HandL._backup_pos;
                            if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                            {
                                dv *= GIZMO_SLOWRATE;
                                _Gizmo_HandL.transform.localPosition = _Gizmo_HandL._backup_pos + dv;
                            }
                            p_v3of.v3HandLOffset += dv;
                            p_v3of.v3HandLOffset = v3limit(p_v3of.v3HandLOffset, 0.15f);
                            _Gizmo_HandL.BkupPosAndRotLocal();
                        }
#else
                if (showPosSliderHandR && _Gizmo_HandR.isDrag)
                {
                    Vector3 dv = _Gizmo_HandR.position - _Gizmo_HandR._backup_pos;
                    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                    {
                        dv *= GIZMO_SLOWRATE;
                        _Gizmo_HandR.position = _Gizmo_HandR._backup_pos + dv;
                    }
                    Transform slvR = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " R Hand", true);
                    //座標変換して加算
                    p_v3of2.v3HandROffset += Quaternion.Euler(p_v3of2.v3HandROffsetRot) * slvR.InverseTransformDirection(dv);
                    p_v3of2.v3HandROffset = v3limit(p_v3of2.v3HandROffset, 0.15f);

                    if (_Gizmo_HandR._backup_rot != _Gizmo_HandR.transform.rotation)
                    {
                        // 角度差分取得
                        p_v3of2.v3HandROffsetRot = CalcGizmoDvRot(_Gizmo_HandR);
                    }

                    _Gizmo_HandR.BkupPosAndRot();
                    bochg = true;
                }
                if (!showPosSliderHandR && _Gizmo_HandL.isDrag)
                {
                    Vector3 dv = _Gizmo_HandL.position - _Gizmo_HandL._backup_pos;
                    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                    {
                        dv *= GIZMO_SLOWRATE;
                        _Gizmo_HandL.position = _Gizmo_HandL._backup_pos + dv;
                    }
                    Transform slvL = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " L Hand", true);

                    //座標変換して加算
                    p_v3of2.v3HandLOffset += Quaternion.Euler(p_v3of2.v3HandLOffsetRot) * slvL.InverseTransformDirection(dv);
                    p_v3of2.v3HandLOffset = v3limit(p_v3of2.v3HandLOffset, 0.15f);

                    if (_Gizmo_HandL._backup_rot != _Gizmo_HandL.transform.rotation)
                    {
                        // 角度差分取得
                        p_v3of2.v3HandLOffsetRot = CalcGizmoDvRot(_Gizmo_HandL);
                    }

                    _Gizmo_HandL.BkupPosAndRot();
                    bochg = true;
                }
#endif
                if (boSelectLR)
                {
                    //Rect rctmp = new Rect(rcItem.x + 10, rcItem.y, (rcItem.width + 10) / 2, rcItem.height);
                    Rect rctmp = new Rect(LX, (pos_y += ItemHeight), SLDW / 2, ItemHeight);
                    showPosSliderHandR = GUI.Toggle(rctmp, showPosSliderHandR, "R(右手位置)", gsToggle);
                    rctmp.x += rctmp.width;
                    showPosSliderHandR = !GUI.Toggle(rctmp, !showPosSliderHandR, "L(左手位置)", gsToggle);
                }

                var cbk = GUI.contentColor;
                if (p_v3of2.isV2)
                {
                    GUI.contentColor = Color.yellow;
                }
                else if (p_mscfg.doIKTargetMHandSpCustom_v2)
                {
                    GUI.contentColor = Color.cyan;
                }

                // 位置
                {
                    Vector3 _v = p_v3of2.v3HandROffset;
                    if (!showPosSliderHandR)
                        _v = p_v3of2.v3HandLOffset;

                    var v_bk = _v;

                    GUI.Label(new Rect(LX, (pos_y += ItemHeight), 122, ItemHeight), " +X: " + Math.Round(_v.x, 4), gsLabel);
                    _v.x = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.x, gsButton);
                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                    {
                        _v.x = 0;
                    }
                    _v.x = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.x, -0.15f, 0.15f);

                    GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +Y: " + Math.Round(_v.y, 4), gsLabel);
                    _v.y = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.y, gsButton);
                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                    {
                        _v.y = 0;
                    }
                    _v.y = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.y, -0.15f, 0.15f);

                    GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +Z: " + Math.Round(_v.z, 4), gsLabel);
                    _v.z = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.z, gsButton);
                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                    {
                        _v.z = 0;
                    }
                    _v.z = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.z, -0.15f, 0.15f);

                    _v = v3limit(_v, 0.15f);

                    if (v_bk != _v)
                        bochg = true;

                    if (showPosSliderHandR)
                        p_v3of2.v3HandROffset = _v;
                    else
                        p_v3of2.v3HandLOffset = _v;
                }

                // 回転
                {
                    Vector3 _v = showPosSliderHandR ? va180(p_v3of2.v3HandROffsetRot)
                        : va180(p_v3of2.v3HandLOffsetRot);

                    GUI.Label(new Rect(LX, (pos_y += ItemHeight), 122, ItemHeight), " +回転X: " + Math.Round(_v.x, 4), gsLabel);
                    _v.x = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.x, gsButton);
                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                    {
                        _v.x = 0;
                    }
                    _v.x = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.x, -180f, 180f);

                    GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +回転Y: " + Math.Round(_v.y, 4), gsLabel);
                    _v.y = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.y, gsButton);
                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                    {
                        _v.y = 0;
                    }
                    _v.y = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.y, -180f, 180f);

                    GUI.Label(new Rect(LX, (pos_y += ItemHeight - 5), 122, ItemHeight), " +回転Z: " + Math.Round(_v.z, 4), gsLabel);
                    _v.z = btnset_LR(new Rect(LX + LW, (pos_y), BW, 20), BW, _v.z, gsButton);
                    if (GUI.Button(new Rect(LX + EDTW, (pos_y), 28, 20), "CL", gsButton))
                    {
                        _v.z = 0;
                    }
                    _v.z = GUI.HorizontalSlider(new Rect(LX, (pos_y += 20), SLDW, 15), _v.z, -180f, 180f);

                    if (showPosSliderHandR)
                        p_v3of2.v3HandROffsetRot = _v;
                    else
                        p_v3of2.v3HandLOffsetRot = _v;
                }

                GUI.contentColor = cbk;

#if true
                if (!_Gizmo_HandR.isDrag)
                {
                    Transform slvR = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " R Hand", true);
                    _Gizmo_HandR.position = slvR.position;
                    _Gizmo_HandR.rotation = slvR.rotation; //* Quaternion.Euler(180, 0, 0);
                    if (cusR)
                        _Gizmo_HandR.rotation = qCusR;
                    _Gizmo_HandR.BkupPosAndRot();

                    //回転保持
                    BkupGizmoU1(_Gizmo_HandR, p_v3of2.v3HandROffsetRot);
                }
                if (!_Gizmo_HandL.isDrag)
                {
                    Transform slvL = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " L Hand", true);
                    _Gizmo_HandL.position = slvL.position;
                    _Gizmo_HandL.rotation = slvL.rotation; //* Quaternion.Euler(0, 0, 180);
                    if (cusL)
                        _Gizmo_HandL.rotation = qCusL;
                    _Gizmo_HandL.BkupPosAndRot();

                    //回転保持
                    BkupGizmoU1(_Gizmo_HandL, p_v3of2.v3HandLOffsetRot);
                }
#else
                            if (!_Gizmo_HandR.isDrag)
                            {
                                Transform slvR = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " R Hand", true);
                                _Gizmo_HandR.transform.parent.transform.position = slvR.position;
                                _Gizmo_HandR.transform.parent.transform.rotation = slvR.rotation; //* Quaternion.Euler(180, 0, 0);
                                //_Gizmo_HandR.transform.localPosition = v3HandROffset;
                                _Gizmo_HandR.position = slvR.position;
                                _Gizmo_HandR.BkupPosAndRotLocal();
                            }
                            if (!_Gizmo_HandL.isDrag)
                            {
                                Transform slvL = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " L Hand", true);
                                _Gizmo_HandL.transform.parent.transform.position = slvL.position;
                                _Gizmo_HandL.transform.parent.transform.rotation = slvL.rotation; //* Quaternion.Euler(0, 0, 180);
                                //_Gizmo_HandL.transform.localPosition = v3HandLOffset;
                                _Gizmo_HandL.position = slvL.position;
                                _Gizmo_HandL.BkupPosAndRotLocal();
                                //_Gizmo_HandL.BkupPosAndRot();
                            }
#endif

                rcItem.y = pos_y;
            }
            return bochg;
        }


        //サブメイド＆男の表示/非表示コントロール達、メンバー数に変更が出たらtrue
        private bool ProcSubMemberCtrls(ref int pos_y, ref Rect rcItem, MsLinks ms_, Maid slave, MsLinkConfig p_mscfg)
        {
            _WinprocPhase = "[subs]";

            //サブメイド
            GetStockMaids();
            if (_StockMaids.Count > 0)
            {
                if (_MaidList.Count >= 20)
                {
                    GUI.enabled = false;
                    ComboSubMaid.boPop = false;
                }

                //pos_y = (int)(rcItem.y += ItemHeight/2);
                //pos_y = (int)(rcItem.y += ItemHeight / 4*3);
                pos_y = (int)(rcItem.y += ItemHeight);

                GUI.Label(rcItem, "【サブメイド呼び出し】", gsLabel);
                pos_y = (int)(rcItem.y += ItemHeight);

                List<string> s_names = new List<string>();
                foreach (var vm in _StockMaids)
                    s_names.Add(GetMaidName(vm));
                if (ComboSubMaid.Show(rcItem, ItemHeight, ItemHeight * 5, s_names.ToArray(), "サブメイドを選択", gsButton, gsButton))
                {
                    if (ComboSubMaid.sIndex >= 0)
                    {
                        // 元のカーソルを保持
                        System.Windows.Forms.Cursor preCursor = System.Windows.Forms.Cursor.Current;
                        // カーソルを待機カーソルに変更
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                        try
                        {
                            //表示サブメイドリストに追加
                            _StockMaids_Visible.Add(_StockMaids[ComboSubMaid.sIndex].mem);

                            //if (GameMain.Instance.CharacterMgr.GetMaidCount() < 21) →　常に21になるっぽい//(vSceneLevel != 5)
                            if (LoadMaid(_StockMaids[ComboSubMaid.sIndex].mem))//(vSceneLevel != 5)
                            {
                                //Maid getmaid = _StockMaids[ComboSubMaid.sIndex].mem;
                                //LoadMaid(getmaid);
                            }
                            else
                            {
                                //アクティブメイドに登録不可 ボイスとかは再生できないかも
                                if (!_StockMaids[ComboSubMaid.sIndex].mem.body0.isLoadedBody)
                                {
                                    _StockMaids[ComboSubMaid.sIndex].mem.DutPropAll();
                                    _StockMaids[ComboSubMaid.sIndex].mem.AllProcPropSeqStart();
                                }
                                _StockMaids[ComboSubMaid.sIndex].mem.Visible = true;
                            }

                            Maid nm = _StockMaids[ComboSubMaid.sIndex].mem;
                            ComboSubMaid.sSelected = "サブメイドを選択";
                            ComboSubMaid.sIndex = -1;

                            //選択を変更
                            if (!ms_.doMasterSlave)
                            {
                                ms_.MsUpdateListChanged(ms_.Scc1_MasterMaid, null, nm);
                            }
                            /*
                            //スレイブ選択を変更
                            if (!slave.boMAN && !ms_.doMasterSlave)
                            {
                                //ms_.mdSlave_No = ms_.mdSlaves.Count();
                                //MsLinks.AllMsUpdateListChanged();
                            }*/
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            // カーソルを元に戻す
                            System.Windows.Forms.Cursor.Current = preCursor;
                        }

                        //メイドが変更されているので帰る
                        return true;
                    }
                }
                pos_y = (int)(rcItem.y += ItemHeight);
                if (ComboSubMaid.boPop)
                {
                    pos_y += ItemHeight * 5;
                }

                GUI.enabled = true;
            }

            _WinprocPhase = "[subs-hide]";

            if (_StockMaids_Visible.Count > 0)
            {
                rcItem.y = (pos_y);
                GUI.Label(rcItem, "【サブメイドを帰す】", gsLabel);
                pos_y = (int)(rcItem.y += ItemHeight);

                List<string> s_names = new List<string>();
                foreach (var vm in _StockMaids_Visible.ToArray())
                {
                    if (!vm.Visible)
                        _StockMaids_Visible.Remove(vm);
                    else
                        s_names.Add(GetMaidName(vm));
                }
                if (ComboSubMaidV.Show(rcItem, ItemHeight, ItemHeight * 3, s_names.ToArray(), "サブメイドを選択", gsButton, gsButton))
                {
                    if (ComboSubMaidV.sIndex >= 0)
                    {
                        //選択中ならリンク中断
                        if (slave == _StockMaids_Visible[ComboSubMaidV.sIndex])
                            ms_.doMasterSlave = false;
                        if (ms_.maidKeepSlaveYotogi == _StockMaids_Visible[ComboSubMaidV.sIndex])
                            ms_.maidKeepSlaveYotogi = null;

                        //非表示
                        if (GetMaidName(_StockMaids_Visible[ComboSubMaidV.sIndex]) == ComboSubMaidV.sSelected)
                            _StockMaids_Visible[ComboSubMaidV.sIndex].Visible = false;
                        else
                            Console.WriteLine("メイドが一致しないため非表示をキャンセル。選択:" + GetMaidName(_StockMaids_Visible[ComboSubMaidV.sIndex]) + "/" + ComboSubMaidV.sSelected);

                        //if (_StockMaids_Visible[ComboSubMaidV.sIndex].Visible)
                        //  return;

                        ComboSubMaidV.sSelected = "サブメイドを選択";
                        ComboSubMaidV.sIndex = -1;

                        //更新＆チェック
                        //MsLinks.AllMsUpdateListChanged();

                        return true; //  メイド数が変わってるので一度戻る
                    }
                }
                pos_y = (int)(rcItem.y += ItemHeight);
                if (ComboSubMaidV.boPop)
                {
                    pos_y += ItemHeight * 3;
                }
            }

            rcItem.y = (pos_y);
            {
                //Color cbk = GUI.contentColor;
                //if (!ms_.doMasterSlave)
                //  GUI.contentColor = Color.gray;
                p_mscfg.doKeepSlaveYotogi = GUI.Toggle(rcItem, p_mscfg.doKeepSlaveYotogi, "夜伽中,サブメイドのSlaveリンク維持", gsToggle);
                //GUI.contentColor = cbk;
            }

            pos_y = (int)(rcItem.y += ItemHeight);
            pos_y = (int)(rcItem.y += ItemHeight / 2);

            _WinprocPhase = "[mens]";

            if (GameMain.Instance.CharacterMgr.GetManCount() > 0)
            {
                if (GUI.Button(new Rect(ItemX, (pos_y), 20, 20), (showSubMens ? "-" : "+"), gsButton))
                {
                    showSubMens = !showSubMens;
                }
                GUI.Label(new Rect(ItemX + 20, pos_y, ItemWidth - 20, ItemHeight), "【男の表示設定/呼び出し】", gsLabel);
                pos_y = (int)(rcItem.y += ItemHeight);
                if (showSubMens)
                {
                    Rect rcbox = new Rect(rcItem);
                    int mancnt = 0;
                    for (int i = 0; i < GameMain.Instance.CharacterMgr.GetManCount(); i++)
                    {
                        if (GameMain.Instance.CharacterMgr.GetMan(i) != null)
                            mancnt += 1;
                    }
                    rcbox.height = (mancnt + 0.5f) * ItemHeight;
                    GUI.Box(rcbox, "");

                    Rect rcman = new Rect(rcItem);
                    rcman.x += 10;
                    rcman.width -= 20;
                    for (int i = 0; i < GameMain.Instance.CharacterMgr.GetManCount(); i++)
                    {
                        Maid man = GameMain.Instance.CharacterMgr.GetMan(i);
                        if (man)
                        {
                            rcman.y = pos_y;
                            bool vflg = man.Visible;
                            if (vflg && !_StockMens_Called.Contains(man))
                                GUI.enabled = false;
                            bool nflg = GUI.Toggle(rcItem, vflg, GetMaidName(new ManInfo(man, i)), gsToggle);
                            GUI.enabled = true;

                            if (vflg != nflg)
                            {
                                if (nflg)
                                {
                                    man.Visible = true;
                                    _StockMens_Called.Add(man);

                                    //選択変更
                                    if (!ms_.doMasterSlave)
                                        ms_.MsUpdateListChanged(ms_.Scc1_MasterMaid, man, null);
                                }
                                else
                                {
                                    if (_StockMens_Called.Contains(man))
                                    {
                                        _StockMens_Called.Remove(man);
                                        man.Visible = false;
                                    }
                                    else
                                    {
                                        Console.WriteLine("呼び出し済みリストにいない男のため非表示をキャンセル");
                                    }
                                }

                                return true;
                            }
                            pos_y = (int)(rcItem.y += ItemHeight);
                        }
                    }
                }
            }

            return false;
        }

        static void CloseAllCombos()
        {
            ComboMaster.boPop = false;
            ComboSlave.boPop = false;
            ComboSubMaid.boPop = false;
            ComboSubMaidV.boPop = false;
            ComboVoiceMode.boPop = false;
            ComboPosLinkBone.boPop = false;
        }

        static void PosGizmoProc(MsLinks ms_)
        {
            if (/*_Gizmo.Visible && */_Gizmo.isDrag)
            {
                //debugPrintConsole("_Gizmo.isDrag");

                if (_Gizmo._backup_pos != _Gizmo.transform.position)
                {
                    Vector3 dv = _Gizmo.transform.position - _Gizmo._backup_pos;
                    //debugPrintConsole("_g pos: " + _Gizmo.transform.position);
                    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                    {
                        dv *= GIZMO_SLOWRATE;
                        _Gizmo.transform.position = _Gizmo._backup_pos + dv;
                    }

                    //アタッチポイント設定にギズモの移動を反映
                    if (cfgs[ms_.num_].doStackSlave_CliCnk)
                    {
                        //Maid slave = ms_.mdSlaves[ms_.mdSlave_No].mem;
                        //Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01 Pelvis" : "chinkoCenter"), true);
                        Transform slvTr = _Gizmo.transform;
                        v3ofs[_pageNum].v3StackOffset += slvTr.InverseTransformDirection(dv);
                        //v3ofs[_pageNum].v3StackOffset = slvTr.InverseTransformDirection(_Gizmo.transform.position - _Gizmo._backup_pos_u1);

                    }
                    else if (cfgs[ms_.num_].doStackSlave_Pelvis)
                    {
                        //Maid slave = ms_.mdSlaves[ms_.mdSlave_No].mem;
                        //Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " Pelvis", true);
                        //Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);
                        Transform slvTr = _Gizmo.transform;
                        v3ofs[_pageNum].v3StackOffset += slvTr.InverseTransformDirection(dv);
                        //v3ofs[_pageNum].v3StackOffset += _Gizmo.transform.InverseTransformDirection(dv);
                    }
                    else
                    {
                        v3ofs[_pageNum].v3StackOffset += _Gizmo.transform.InverseTransformDirection(dv);
                    }
                }
                else if (_Gizmo._backup_rot != _Gizmo.transform.rotation)
                {
                    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                    {
                        _Gizmo.transform.rotation = Quaternion.Slerp(_Gizmo._backup_rot, _Gizmo.rotation, GIZMO_SLOWRATE);
                    }

#if false
                    if (cfgs[ms_.num_].doStackSlave_CliCnk)
                    {
                        //Maid slave = ms_.mdSlaves[ms_.mdSlave_No].mem;
                        //Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01 Pelvis" : "chinkoCenter"), true);
                        v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(_Gizmo._backup_rot_u1) * _Gizmo.transform.rotation).eulerAngles;
                    }
                    else if(cfgs[ms_.num_].doStackSlave_Pelvis)
                    {
                        //Maid slave = ms_.mdSlaves[ms_.mdSlave_No].mem;
                        //Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " Pelvis", true);
                        //v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(slvTr.rotation) * _Gizmo.transform.rotation).eulerAngles;
                        v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(_Gizmo._backup_rot_u1) * _Gizmo.transform.rotation).eulerAngles;
                    }
                    else
                    {
                        v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(ms_.mdMasters[ms_.mdMaster_No].mem.gameObject.transform.rotation) * _Gizmo.transform.rotation).eulerAngles;
                    }
//#else
                    if (cfgs[ms_.num_].doStackSlave_Pelvis || cfgs[ms_.num_].doStackSlave_CliCnk)
                    {
                        //v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(_Gizmo._backup_rot_u1) * (Quaternion.Inverse(Quaternion.Euler(0, -90, -90)) * _Gizmo.transform.rotation)).eulerAngles;
                        v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(_Gizmo._backup_rot_u1) * _Gizmo.transform.rotation).eulerAngles;

                        Maid slave = ms_.mdSlaves[ms_.mdSlave_No].mem;
                        Transform slvTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);

                        debugPrintConsole("bip rot: " + slvTr.localRotation.eulerAngles + " ofs: " + (Quaternion.Inverse(slave.gameObject.transform.rotation)* slvTr.rotation).eulerAngles);

                        if (cfgs[ms_.num_].doStackSlave_Pelvis)
                        {
                            //v3ofs[_pageNum].v3StackOffsetRot = Quaternion.Euler((Quaternion.Inverse(slave.gameObject.transform.rotation) * slvTr.rotation).eulerAngles - new Vector3(270.0f, 90.0f, 0.0f)) * v3ofs[_pageNum].v3StackOffsetRot;
                            debugPrintConsole("-ofs rot: " + ((Quaternion.Inverse(slave.gameObject.transform.rotation) * slvTr.rotation).eulerAngles - new Vector3(270.0f, 90.0f, 0.0f)));
                            v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(_Gizmo._backup_rot_u1) * _Gizmo.transform.rotation).eulerAngles;
                        }
                        else
                        {
                            v3ofs[_pageNum].v3StackOffsetRot = _Gizmo.transform.TransformDirection(v3ofs[_pageNum].v3StackOffsetRot);
                            slvTr = slave.gameObject.transform;
                            v3ofs[_pageNum].v3StackOffsetRot = slvTr.InverseTransformDirection(Quaternion.Euler(0, -90, -90)*v3ofs[_pageNum].v3StackOffsetRot);
                        }
                    }
                    else
                    {
                        v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(_Gizmo._backup_rot_u1) * _Gizmo.transform.rotation).eulerAngles;
                    }
#endif
                    //v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(_Gizmo._backup_rot_u1) * _Gizmo.transform.rotation).eulerAngles;

                    v3ofs[_pageNum].v3StackOffsetRot = (Quaternion.Inverse(_Gizmo._backup_rotLocal_u1) * _Gizmo.transform.localRotation).eulerAngles;
                    //_Gizmo.BkupPosAndRotLocalU1();

                    v3ofs[_pageNum].v3StackOffsetRot = va180(v3ofs[_pageNum].v3StackOffsetRot);
                    debugPrintConsole("_g rot: " + _Gizmo.transform.rotation.eulerAngles + " ofs: " + v3ofs[_pageNum].v3StackOffsetRot);
                }

                _Gizmo.BkupPosAndRot();
            }
            else if (_Gizmo.Visible)
            {
                Transform sTr = ms_.mdSlaves[ms_.mdSlave_No].mem.gameObject.transform;
                Maid slave = ms_.mdSlaves[ms_.mdSlave_No].mem;

                if (cfgs[ms_.num_].doStackSlave_CliCnk)
                {
                    sTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01 Pelvis" : "chinkoCenter"), true);
                }
                else if (cfgs[ms_.num_].doStackSlave_Pelvis)
                {
                    sTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip") + " Pelvis", true);
                }

                _Gizmo.transform.rotation = sTr.rotation;
                _Gizmo.transform.position = sTr.position;


                if (cfgs[ms_.num_].doStackSlave_PosSyncMode && cfgs[ms_.num_].doStackSlave_PosSyncModeSp && !string.IsNullOrEmpty(cfgs[ms_.num_].doStackSlave_PosSyncModeSp_TgtBone))
                {
                    Maid master = ms_.mdMasters[ms_.mdMaster_No].mem;

                    sTr = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, (cfgs[ms_.num_].doStackSlave_PosSyncModeSp_TgtBone), true);
                    if (sTr)
                    {
                        _Gizmo.transform.rotation = sTr.rotation;
                        //if ((cfgs[ms_.num_].doStackSlave_Pelvis || cfgs[ms_.num_].doStackSlave_CliCnk))
                        //    _Gizmo.transform.rotation *= Quaternion.Euler(0, -90, -90);
                        _Gizmo.transform.rotation *= Quaternion.Euler(v3ofs[_pageNum].v3StackOffsetRot);
                    }

                }
                else if (cfgs[ms_.num_].doStackSlave_PosSyncMode && (cfgs[ms_.num_].doStackSlave_Pelvis || cfgs[ms_.num_].doStackSlave_CliCnk))
                {
                    Maid master = ms_.mdMasters[ms_.mdMaster_No].mem;

                    // 位置のみの場合はアタッチ先キャラクター座標を基準に
                    sTr = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, (!master.boMAN ? "Bip01" : "ManBip"), true);
                    //_Gizmo.transform.position = sTr.position;
                    _Gizmo.transform.rotation = sTr.rotation;
                    _Gizmo.transform.rotation *= Quaternion.Euler(0, -90, -90);
                    _Gizmo.transform.rotation *= Quaternion.Euler(v3ofs[_pageNum].v3StackOffsetRot);
                }
                else if (cfgs[ms_.num_].doStackSlave_Pelvis || cfgs[ms_.num_].doStackSlave_CliCnk)
                {
                    sTr = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, (!slave.boMAN ? "Bip01" : "ManBip"), true);
                    //_Gizmo.transform.position = sTr.position;
                    _Gizmo.transform.rotation = sTr.rotation;
                    _Gizmo.transform.rotation *= Quaternion.Euler(0, -90, -90);
                }
                _Gizmo.BkupPos();
                _Gizmo.BkupPosAndRotU1();
                //_Gizmo._backup_rot_u1 = _Gizmo._backup_rot_u1 * Quaternion.Inverse(Quaternion.Euler(v3ofs[_pageNum].v3StackOffsetRot));

                _Gizmo.BkupPosAndRotLocalU1();
                _Gizmo._backup_rotLocal_u1 = _Gizmo._backup_rotLocal_u1 * Quaternion.Inverse(Quaternion.Euler(v3ofs[_pageNum].v3StackOffsetRot));
            }
        }

        //メイド読み出し（夜伽のサブメイド読み込み&PlacementWindowのActiveMaidを参考にした）
        static bool LoadMaid(Maid newmaid)
        {
            //空きスロットか登録済みスロットを探す
            int k = 0;
            while (k < GameMain.Instance.CharacterMgr.GetMaidCount())
            {
                if (GameMain.Instance.CharacterMgr.GetMaid(k) == null || GameMain.Instance.CharacterMgr.GetMaid(k) == newmaid)
                {
                    break;
                }
                k++;
            }
            if (k > 20) //アクティブメイドの最大数は21
            {
                Console.WriteLine("アクティブメイド登録 インデックスエラー: " + k);
                return false;
            }


            if (IkXT.IsIkCtrlO117 || IkXT.IsIkCtrlO118) //v5.0
            {
                // 撮影モードチェック → 撮影モードを抜ける時のエラー対策
                var plw = GameObject.FindObjectOfType<PlacementWindow>();
                if (plw)
                {
                    plw.InvokeNonPublicMethod("ActiveMaid", new object[] { newmaid });
                    newmaid.Visible = true;
                    newmaid.AllProcProp();
                    return true;
                }
            }

            //アクティブメイド登録
            Console.WriteLine("アクティブメイド登録: " + GetMaidName(newmaid) + "⇒" + k + " / " + GameMain.Instance.CharacterMgr.GetMaidCount());
            GameMain.Instance.CharacterMgr.SetActiveMaid(newmaid, k);
            newmaid.Visible = true;
            newmaid.AllProcProp();

            //表情やポーズの設定
            newmaid.boMabataki = true;
            newmaid.CrossFadeAbsolute("maid_stand01.anm", false, true, false, 0.5f, 1f);

            if (bIsYotogiScene)
                newmaid.FaceAnime(VymModule.cfg.sFaceAnimeYotogiDefault, 0f);

            return true;
        }


        //男の選択または読み込み(ユーザー設定ファイル読み出し時)
        public static int SelectOrLoadMan(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;

            for (int i = 0; i < _MensList.Count; i++)
            {
                if (name == GetMaidName(_MensList[i]))
                    return i;
            }

            for (int i = 0; i < GameMain.Instance.CharacterMgr.GetManCount(); i++)
            {
                Maid man = GameMain.Instance.CharacterMgr.GetMan(i);
                if (man && name == GetMaidName(new ManInfo(man, i)))
                {
                    if (!man.Visible)
                        _StockMens_Called.Add(man);
                    man.Visible = true;

                    GetMens();
                    break;
                }
            }

            int vmaid_cnt = 0;
            for (int i = 0; i < GameMain.Instance.CharacterMgr.GetManCount(); i++)
            {
                Maid man = GameMain.Instance.CharacterMgr.GetMan(i);
                if (!ChkMaid(man, true))
                    continue;

                if (name == GetMaidName(new ManInfo(man, i)))
                    return vmaid_cnt;
                vmaid_cnt++;
            }
            return -1;
        }

        //メイドの選択または読み込み(ユーザー設定ファイル読み出し時)
        public static int SelectOrLoadMaid(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;

            for (int i = 0; i < _MaidList.Count; i++)
            {
                if (name == GetMaidName(_MaidList[i].mem))
                    return i;
            }

            debugPrintConsole("hit - i");

            Maid hiddenMaid = null;
            GetStockMaids();
            foreach (var vm in _StockMaids)
            {
                debugPrintConsole(name + " / " + GetMaidName(vm.mem));

                if (name == GetMaidName(vm.mem))
                {
                    hiddenMaid = vm.mem;
                    debugPrintConsole("hit - s " + hiddenMaid);

                    break;
                }
            }

            if (hiddenMaid != null)
            {
                //debugPrintConsole("hit - h0 " + !(hiddenMaid));

                //if (GameMain.Instance.CharacterMgr.GetMaidCount() < 20)//(vSceneLevel != 5)
                //表示サブメイドリストに追加
                if (LoadMaid(hiddenMaid))//(vSceneLevel != 5)
                {
                    debugPrintConsole("hit - h1");
                    _StockMaids_Visible.Add(hiddenMaid);
                    //LoadMaid(hiddenMaid);
                    GetMaids();
                }

                int vmaid_cnt = 0;
                for (int i = 0; i < GameMain.Instance.CharacterMgr.GetMaidCount(); i++)
                {
                    Maid maidt = GameMain.Instance.CharacterMgr.GetMaid(i);
                    if (!ChkMaid(maidt, true))
                        continue;

                    debugPrintConsole(name + " / " + GetMaidName(maidt));

                    if (name == GetMaidName(maidt))
                        return vmaid_cnt;
                    vmaid_cnt++;
                }
            }
            return -1;
        }

#region コンボボックス
        //超簡単コンボボックス、範囲外を選択されたときに消すなんて便利機能もできた
        class EasyCombo
        {
            static EasyCombo openCombo = null;

            public Vector2 scrollPosition = Vector2.zero;
            private bool boPop_ = false;
            public bool boPop
            {
                get { return boPop_; }
                set
                {
                    if (value)
                    {   //一つに抑制する
                        if (openCombo != null)
                            openCombo.boPop = false;
                        openCombo = this;
                    }
                    boPop_ = value;
                }
            }
            public string sSelected = String.Empty;
            public int sIndex { get; set; }
            public bool boChanged { get; private set; }

            //アウトカーソルクリッククローズ用
            private Vector2 scrollPosition_bk = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            private int closeCntdwn = 0;    //マウスのボタンUPの次フレームあたりにボタンイベントが起きるようなので

            public EasyCombo(string s, int i)
            {
                sSelected = s;
                sIndex = i;
            }

            public bool Show(Rect rect, int itemH, int maxH, List<string> slist, string sBtn, GUIStyle gsBtn, GUIStyle gsLst, bool[] disables = null)
            {
                return Show(rect, itemH, maxH, slist.ToArray(), sBtn, gsBtn, gsLst, disables);
            }

            public bool Show(Rect rect, int itemH, int maxH, int Numbers, string sBtn, GUIStyle gsBtn, GUIStyle gsLst, bool[] disables = null)
            {
                List<string> list = new List<string>();
                for (int i = 0; i < Numbers; i++)
                    list.Add(i.ToString());

                return Show(rect, itemH, maxH, list.ToArray(), sBtn, gsBtn, gsLst, disables);
            }

            public bool Show(Rect rect_btn, int itemH, int maxH, string[] slist, string sBtn, GUIStyle gsBtn, GUIStyle gsLst, bool[] disables = null)
            {
                Rect rect_list = new Rect(rect_btn);
                rect_list.y += rect_btn.height;
                rect_list.height = maxH;

                //スクロールバー分幅を足す
                //rect_list.width += 16;

                return Show(rect_btn, itemH, rect_list, slist, sBtn, gsBtn, gsLst, disables);
            }


            public bool Show(Rect rect_btn, int itemH, Rect rect_list, string[] slist, string sBtn, GUIStyle gsBtn, GUIStyle gsLst, bool[] disables = null)
            {
                int maxlen = 0;

                boChanged = false;

                Color cbk = GUI.color;
                if (boPop)
                    GUI.color = Color.cyan;

                string sText = sBtn;
                if (sText == null)
                    sText = sSelected;
                else
                    sSelected = sText;

                if (GUI.Button(rect_btn, sText, gsBtn))
                {
                    boPop = !boPop;
                }
                GUI.color = cbk;

                if (boPop)
                {
                    rect_btn = rect_list;

                    foreach (string s in slist)
                    {
                        /*if (maxlen < s.Length)
                            maxlen = s.Length;*/
                        int len = (int)gsLst.CalcSize(new GUIContent(s)).x;

                        if (maxlen < len)
                            maxlen = len;
                    }
                    int iw = /*(gsLst.fontSize+2) * */maxlen;
                    if (iw < (rect_btn.width - 16))
                        iw = (int)rect_btn.width - 16;//スクロールバー分幅を引く

                    GUI.Box(new Rect(rect_btn.x, rect_btn.y, rect_btn.width - 15, rect_btn.height), "");
                    scrollPosition = GUI.BeginScrollView(rect_btn, scrollPosition, new Rect(0, 0, iw, itemH * slist.Length), false, true);

                    try
                    {
                        int pos_y = 0;
                        int i = 0;
                        foreach (string s in slist)
                        {
                            if (disables != null && disables[i])
                                GUI.enabled = false;

                            if (GUI.Button(new Rect(0, pos_y, iw, itemH), s, gsLst))
                            {
                                if (sSelected != s)
                                    boChanged = true;

                                sSelected = s;
                                sIndex = i;
                                boPop = false;
                            }
                            GUI.enabled = true;

                            i++;
                            pos_y += itemH;
                        }
                    }
                    finally
                    {
                        GUI.enabled = true;
                        GUI.EndScrollView();
                    }

                    if (boPop && !boChanged)
                    {
                        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonUp(1))
                        {
                            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                                scrollPosition_bk = scrollPosition;
                            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
                            {
                                if (scrollPosition_bk == scrollPosition)
                                {
                                    //スクロールドラッグなし
                                    //マウスのボタンUPの次フレームあたりにボタンイベントが起きるようなので
                                    closeCntdwn = 3;
                                }
                            }
                        }
                        else if (closeCntdwn > 0)
                        {
                            closeCntdwn--;
                            if (closeCntdwn == 0)
                                boPop = false;
                        }
                    }
                }
                else
                {
                    if (scrollPosition_bk == scrollPosition)    //開くときのクリックで閉じないように
                        scrollPosition_bk = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    if (closeCntdwn > 0)
                        closeCntdwn = 0;
                }

                return boChanged;
            }

        }
#endregion

        //　デバッグ用コンソール出力メソッド
        [Conditional("DEBUG")]
        private static void debugPrintConsole(string s)
        {
            Console.WriteLine(s);
        }
        //　デバッグ用コンソール出力メソッド
        [Conditional("DEBUG")]
        private static void debugPrintConsoleSp(string s)
        {
            if (Input.GetKey(KeyCode.Space))
                Console.WriteLine(s);
        }

#region メンズ＆メイド一覧
        public class ManInfo
        {
            public ManInfo(Maid m, int n)
            {
                mem = m;
                mem_id = n;
            }

            public ManInfo(Maid m, int n, bool newman, string motion_bkup, bool chinkov_bkup)
            {
                mem = m;
                mem_id = n;
            }

            public Maid mem = null;
            public int mem_id = 0;
        }

        public static List<ManInfo> _MensList = new List<ManInfo>();

        Maid GetM(List<ManInfo> list, ref int num)
        {
            if (list.Count <= 0)
                return null;

            if (list.Count <= num || num < 0)
                num = 0;

            return list[num].mem;
        }

        static int GetMens()
        {
            return GetMens(true);
        }

        static int GetMens(bool boChkVsbl)
        {
            int cnt = GameMain.Instance.CharacterMgr.GetManCount();
            if (cnt <= 0)
            {
                if (_MensList.Count > 0)
                    _MensList.Clear();
                return 0;
            }

            List<ManInfo> _maidList = new List<ManInfo>();

            int vmaid_cnt = 0;
            for (int i = 0; i < cnt; i++)
            {
                Maid maids = GameMain.Instance.CharacterMgr.GetMan(i);
                if (!ChkMaid(maids, boChkVsbl))
                    continue;

                vmaid_cnt++;

                _maidList.Add(new ManInfo(maids, i));
            }
            _MensList = _maidList;

            return vmaid_cnt;
        }

        public static List<ManInfo> _MaidList = new List<ManInfo>();

        static int GetMaids()
        {
            return GetMaid(true);
        }

        static int GetMaid(bool boChkVsbl)
        {
            int cnt = GameMain.Instance.CharacterMgr.GetMaidCount();
            if (cnt <= 0)
            {
                if (_MaidList.Count > 0)
                    _MaidList.Clear();
                return 0;
            }

            List<ManInfo> _maidList = new List<ManInfo>();

            int vmaid_cnt = 0;
            for (int i = 0; i < cnt; i++)
            {
                Maid maids = GameMain.Instance.CharacterMgr.GetMaid(i);
                if (!ChkMaid(maids, boChkVsbl))
                    continue;

                vmaid_cnt++;

                _maidList.Add(new ManInfo(maids, i));
            }
            _MaidList = _maidList;

            return vmaid_cnt;
        }

        private static bool ChkMaid(Maid m, bool boChkVsbl)
        {
            if (boChkVsbl)
                return m != null && m.Visible && m.body0 != null && m.body0.goSlot != null;
            else
                return m != null && m.GetProp(MPN.head) != null;
        }

        public static string GetMaidName(Maid m)
        {
            if (m.boMAN)
            {
                return "男 No.不明";
            }
            else
            {
                string maidname = (m.XtParam().status.last_name + " " + m.XtParam().status.first_name);
                return maidname;
            }
        }

        public static string GetMaidName(ManInfo m)
        {
            return GetMaidName(m, false);
        }

        public static string GetMaidName(ManInfo m, bool withMaidID)
        {
            if (m.mem.boMAN)
            {
                return "男 No." + m.mem_id;
            }
            else
            {
                string maidname = (m.mem.XtParam().status.last_name + " " + m.mem.XtParam().status.first_name);
                if (withMaidID)
                    maidname = "Maid" + m.mem_id + ": " + maidname;
                return maidname;
            }
        }

        //サブメイド
        static void GetStockMaids()
        {
            _StockMaids.Clear();
            for (int j = 0; j < GameMain.Instance.CharacterMgr.GetStockMaidCount(); j++)
            {
                Maid ms = GameMain.Instance.CharacterMgr.GetStockMaid(j);
                if (ms != null && !ms.Visible)
                {
                    _StockMaids.Add(new ManInfo(ms, j));
                }
            }
        }
        static List<ManInfo> _StockMaids = new List<ManInfo>();
        static List<Maid> _StockMaids_Visible = new List<Maid>();
        static List<Maid> _StockMens_Called = new List<Maid>();
        static HashSet<Maid> _YtgKeepMaids_Visible = new HashSet<Maid>();

#endregion

        class FIOmgr
        {
            bool bFade = false;
            public bool bFadeIn { get; private set; }
            public bool bUpdateRequest { get; private set; }

            public FIOmgr()
            {
                bUpdateRequest = false;
            }

            public void inUpdate()
            {
                bUpdateRequest = false;
                bFadeIn = false;

                if (GameMain.Instance.MainCamera.IsFadeProc())
                {
                    if (!bFade)
                        bFadeIn = true;

                    bFade = true;
                }
                if (bFade && GameMain.Instance.MainCamera.IsFadeStateNon())
                {
                    debugPrintConsole("Fade: UpdateRequest = true");
                    bFade = false;
                    bUpdateRequest = true;
                }
            }
        }
        static FIOmgr fioMgr = new FIOmgr();

        void LateUpdate()
        {
            //_camera_num = Camera.allCamerasCount;
            foreach (var m in _MSlinks)
            {
                m.lateUpdate();
            }
            VertexMorph_FixBlendValues();

            // 手のアタッチ角度調整
            foreach (var ms in _MSlinks)
            {
                if (!IsHookActive)
                    ms.lateHandsAtcpProc();
                /*
#if DEBUG
                if (ms.do_slave && Input.GetKey(KeyCode.Y))
                    IkXT._inst.NewIKReset(ms.do_slave);
#endif
*/
                // 手指のブレンド v0030
                ms.lateBlendHand();
            }
        }

        static bool IsHookActive = false;
        static bool IsHookAutoTwist = false;

        // AutoTwist前にフック
        public void preTBodyAutoTwist(TBody tbody)
        {
            IsHookAutoTwist = true;
            //Console.WriteLine("preTBodyAutoTwist " + (tbody.boMAN ? "男" : tbody.maid.XtParam().status.last_name));

            // 手のアタッチ角度調整
            foreach (var ms in _MSlinks)
            {
                var slave = ms.GetSlave();

                if (slave && slave == tbody.maid)
                {
                    IsHookActive = true;

                    //Console.WriteLine("latehook " + (slave.boMAN ? "男" : slave.XtParam().status.last_name));
                    ms.lateHandsAtcpProc();
                }
            }
        }

        //v5.1 SolverUpdate前にフック
        public void preSolverUpdate(object objFullBodyIKCtrl)
        {
            if (!IkXT.IsIkCtrlO120) //v5.1 
                return;

            // FinalIK割り込み設定
            foreach (var ms in _MSlinks)
            {
                var slave = ms.GetSlave();
                if (slave && IkXT._inst.GetIkCtrl(slave) == objFullBodyIKCtrl)
                {
                    //v5.1
                    IkXT._inst.UpdateFinalIK(ms.GetSlave(), ms, cfgs[ms.num_]);
                }
            }
        }

        /*
        // IKCtrlのアップデート前にフックされる
        public void postIKUpdate(object objIKCtrl)
        {
            // 手のアタッチ角度調整
            foreach (var ms in _MSlinks)
            {
                var slave = ms.GetSlave();

                if (slave && IkXT._inst.GetIkCtrl(slave) == objIKCtrl)
                {
                    IsHookActive = true;

                    //Console.WriteLine("latehook " + (slave.boMAN ? "男" : slave.XtParam().status.last_name));
                    ms.lateHandsAtcpProc();
                }
            }
        }*/

        /*
        int _camera_num = 0;
        private void OnRenderObject()
        {
            _camera_num--;
            if (_camera_num > 0)
                return;

            //if (Camera.current != GameMain.Instance.MainCamera.camera)
            //    return;

            //カメラのレンダリング完了

            // 手の角度復元
            foreach (var ms in _MSlinks)
            {
                ms.postHandsAtcpProc();
            }
        }
        */
        void Update()
        {
            //maid = null;
            foreach (var m in _MSlinks)
            {
                m.mdMasters = mdDummyMaidl;
                m.mdSlaves = mdDummyMaidl;
            }

            if (!SceneLevelEnable /*|| VYM.API.GetPluginEnabled() != 1*/)
            {
                return;
            }

            //フェードアウトチェック
            fioMgr.inUpdate();

            //有効チェック
            bool boEnabled = true;

            // メイドさんの取得
            if (GameMain.Instance.CharacterMgr.GetMaidCount() <= 0 || GameMain.Instance.CharacterMgr.GetManCount() <= 0)
            {
                boEnabled = false;
                //return;
            }
            else
            {
                GetMaids();

                if (cfg.doHitScaleDef)
                {
                    //ヒットチェック初期値設定
                    foreach (var m in _MaidList)
                    {
                        if (m.mem.IsBusy)
                            continue;

                        //バケーションパック対応は想像で適当にやってるので問題あるかも
                        UpdateHitScaleDef(m.mem, 1f, cfg.HitScaleDef, fioMgr.bUpdateRequest && vacationEnabled);
                    }
                    foreach (var m in HitScaleChangedMaids.ToArray())
                    {
                        if (!ChkMaid(m, true))
                            HitScaleChangedMaids.Remove(m);
                    }
                }

                //if (GetMaid() > 0)
                //    maid = MaidList[0].mem;
                GetMens();

                if (_MaidList.Count <= 0 || _MensList.Count <= 0)
                {
                    boEnabled = false;
                    //return;
                }
            }

            if (!boEnabled)
            {
                //ご主人様かメイドが居ない
                foreach (var ms in _MSlinks)
                {
                    if (ms.maidKeepSlaveYotogi && ms.maidKeepSlaveYotogi.Visible)
                    {
                        //夜伽キープ中のメイドが残ってしまうのを防止
                        if (!ms.keepSI.CheckMoved(ms.maidKeepSlaveYotogi, ms.num_) && GameMain.Instance.CharacterMgr.GetMaidCount() > 0
                            && GameMain.Instance.CharacterMgr.GetMaid(0) != ms.maidKeepSlaveYotogi)
                        {
                            ms.maidKeepSlaveYotogi.Visible = false;
                        }
                        else
                        {
                            ms.maidKeepSlaveYotogi = null;
                        }
                    }
                }

                //メイド単体稼働可に// return;
                if (_MaidList.Count <= 0)
                    return;
            }

            //フェードアウトによるクリア
            if (fioMgr.bUpdateRequest)
            {
                if (vacationEnabled || bIsYotogiScene)
                    VymModule.Reset();
            }

            //ストックメイドから表示したメイドのチェック
            if (_StockMaids_Visible.Count > 0)
            {
                foreach (var vm in _StockMaids_Visible.ToArray())
                {
                    if (!vm.Visible)
                        _StockMaids_Visible.Remove(vm);
                }

                if (bIsYotogiScene && fioMgr.bUpdateRequest)
                {
                    //夜伽中はフェードインごとにメイドがリセットされる
                    _YtgKeepMaids_Visible.Clear();
                }
            }

            //キーチェック
            if (InputEx.GetKeyDownEx(cfg.hotkey_GUI, cfg.hotkey_GUI_Modifier))
            {
                //GUIの切り替え
                GuiFlag = !GuiFlag;
                XtMs2ndWnd.boShow = false;

                if (!GuiFlag && _Gizmo.Visible)
                {
                    //_Gizmo.Visible = false;
                    GizmoVisible(false);
                }
                if (!GuiFlag)
                {
                    GizmoHsVisible(false);
                    CloseAllCombos();
                }
                else
                {
                    showWndMin = false;
                }
            }
            if (GuiFlag)
            {
                //onguiだけだと無効化しきれない場合がある
                if (rc_stgw.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    GameMain.Instance.MainCamera.SetControl(false);
                    bGuiOnMouse = true;
                }
            }

            //Slave単独処理など
            foreach (var ms in _MSlinks)
            {
                ms.MsUpdate(true, true);
                ms.inUpdate_HoldMask();
            }

            if (boEnabled)
            {
                //マスターが居なければリンク処理はない

                //マスター透明度の設定
                CommonEdit.ProcManAlpha(fioMgr.bUpdateRequest);

                //VibeYourMaidチェック
                bIsVymPlg = (VYM.API.GetPluginEnabled() == 1);

                //リンク処理
                foreach (var ms in _MSlinks)
                {
                    ms.linkProc();
                }

                VertexMorph_FixBlendValues();
            }

            // 手のアタッチ変更
            foreach (var ms in _MSlinks)
            {
                ms.handsAtcpProc();
                //ms.setupIkMIni();

                //v5.0
                if (!IkXT.IsIkCtrlO120 && IkXT.IsIkCtrlO118) //v5.1 
                    IkXT._inst.UpdateFinalIK(ms.GetSlave(), ms, cfgs[ms.num_]);
            }
            return;
        }

#region アタッチ
        /*void AtccHand2HandR(Maid master, Maid slave)
        {
            string bone_handR = "ManBip R Hand";
            if (!master.boMAN)
            {
                bone_handR = "Bip01 R Hand";
            }
            //Maid.IKTargetToAttachPointより

            if (bone2Atcp(master, bone_handR) &&
                        slave.body0.tgtHandR_AttachName != bone_handR)
            {
                slave.body0.tgtMaidR = master;
                slave.body0.tgtHandR_AttachSlot = (int)TBody.SlotID.body;
                slave.body0.tgtHandR_AttachName = bone_handR;
                slave.body0.tgtHandR = null;
                slave.body0.tgtHandR_offset = v3HandROffset;//Vector3.zero;
            }
        }*/

        static string GetHandBnR(Maid m)
        {
            if (!m.boMAN)
            {
                return "Bip01 R Hand";
            }
            return "ManBip R Hand";
        }

        // 手と手のアタッチ
        static void AtccHand2HandR2(Maid master, Maid slave, Vector3 v3HandROffset, Vector3 v3HandROffsetRot, MsLinkConfig mscfg)
        {
            string bone_handR = GetHandBnR(master);

            Transform tgt = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, bone_handR, true);
            if (tgt)
            {
                slave.body0._ikp().tgtMaidR = master;
                slave.body0._ikp().tgtHandR_AttachSlot = (int)TBody.SlotID.body;
                slave.body0._ikp().tgtHandR_AttachName = string.Empty;
                slave.body0._ikp().tgtHandR = tgt;
                slave.body0._ikp().tgtHandR_offset = v3HandROffset;//Vector3.zero;

                if (IkXT.IsIkCtrlO117)
                {
                    IkXT.SetHandIKTarget(mscfg, "右手", master, slave, (int)TBody.SlotID.body, string.Empty, tgt, v3HandROffset);
                }

                if (!mscfg.doIK159NewPointToDef && mscfg.doIK159RotateToHands)
                {
                    // v5.0 SetHandIKRotate同様のグローバル角度での調整
                    Transform trh0 = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, GetHandBnR(master), true);
                    Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                    trh.rotation = trh0.rotation * Quaternion.Euler(v3HandROffsetRot);

                }
                else 
                if (IkXT.IsNewIK && mscfg.doIK159RotateToHands/*Ik159.IsNewPointIK(slave, "右手")*/) // 手首角度のアタッチ v0027
                {
                    //slave.IKTargetToBone("右手", master, bone_handR, v3HandROffsetRot, IKMgrData.IKAttachType.Rotate, false);
                    IkXT.SetHandIKRotate("右手", master, slave, bone_handR, v3HandROffsetRot);
                }
                // v0025 手の角度調整
                else if (v3HandROffsetRot != Vector3.zero)
                {
                    Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnR(slave), true);
                    trh.localRotation *= Quaternion.Euler(v3HandROffsetRot);
                }
            }
        }

        /*void AtccHand2HandL(Maid master, Maid slave)
        {
            string bone_handL = "ManBip L Hand";
            if (!master.boMAN)
            {
                bone_handL = "Bip01 L Hand";
            }
            //Maid.IKTargetToAttachPointより

            if (bone2Atcp(master, bone_handL) &&
                        slave.body0.tgtHandL_AttachName != bone_handL)
            {
                slave.body0.tgtMaidL = master;
                slave.body0.tgtHandL_AttachSlot = (int)TBody.SlotID.body;
                slave.body0.tgtHandL_AttachName = bone_handL;
                slave.body0.tgtHandL = null;
                slave.body0.tgtHandL_offset = v3HandLOffset;//Vector3.zero;
            }
        }*/

        static string GetHandBnL(Maid m)
        {
            if (!m.boMAN)
            {
                return "Bip01 L Hand";
            }
            return "ManBip L Hand";
        }

        static string GetForearmBn(Maid m, bool boR)
        {
            if (boR)
            {
                if (!m.boMAN)
                {
                    return "Bip01 R Forearm";
                }
                return "ManBip R Forearm";
            }

            if (!m.boMAN)
            {
                return "Bip01 L Forearm";
            }
            return "ManBip L Forearm";
        }

        static void AtccHand2HandL2(Maid master, Maid slave, Vector3 v3HandLOffset, Vector3 v3HandLOffsetRot, MsLinkConfig mscfg)
        {
            string bone_handL = GetHandBnL(master);

            //Maid.IKTargetToAttachPointより
            Transform tgt = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, bone_handL, true);
            if (tgt)
            {
                slave.body0._ikp().tgtMaidL = master;
                slave.body0._ikp().tgtHandL_AttachSlot = (int)TBody.SlotID.body;
                slave.body0._ikp().tgtHandL_AttachName = string.Empty;
                slave.body0._ikp().tgtHandL = tgt;
                slave.body0._ikp().tgtHandL_offset = v3HandLOffset;//Vector3.zero;

                if (IkXT.IsIkCtrlO117)
                {
                    IkXT.SetHandIKTarget(mscfg, "左手", master, slave, (int)TBody.SlotID.body, string.Empty, tgt, v3HandLOffset);
                }

                if (!mscfg.doIK159NewPointToDef && mscfg.doIK159RotateToHands)
                {
                    // v5.0 SetHandIKRotate同様のグローバル角度での調整
                    Transform trh0 = BoneLink.BoneLink.SearchObjName(master.body0.m_Bones.transform, GetHandBnL(master), true);
                    Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                    trh.rotation = Quaternion.Inverse(trh0.rotation) * Quaternion.Euler(v3HandLOffsetRot);
                }
                else
                if (IkXT.IsNewIK && mscfg.doIK159RotateToHands/*Ik159.IsNewPointIK(slave, "左手")*/) // 手首角度のアタッチ v0027
                {
                    //slave.IKTargetToBone("左手", master, bone_handL, v3HandLOffsetRot, IKMgrData.IKAttachType.Rotate, false);
                    IkXT.SetHandIKRotate("左手", master, slave, bone_handL, v3HandLOffsetRot);
                }
                // v0025 手の角度調整
                else if (v3HandLOffsetRot != Vector3.zero)
                {
                    Transform trh = BoneLink.BoneLink.SearchObjName(slave.body0.m_Bones.transform, GetHandBnL(slave), true);
                    trh.localRotation *= Quaternion.Euler(v3HandLOffsetRot);
                }
            }
        }

        // 手のアタッチのバックアップ用
        public class BkupHandsAtc
        {
            public class Param
            {
                public Maid TgtMaid;
                public int Tgt_AttachSlot = -1;
                public string Tgt_AttachName = string.Empty;
                public Transform TgtTr;
                public Vector3 TgtOffset;
            }
            public Param pR = new Param();
            public Param pL = new Param();
            public TBody bkupbody = null;

            public BkupHandsAtc(TBody body0)
            {
                this.bkupbody = body0;

                pL.TgtMaid = body0._ikp().tgtMaidL;
                pL.Tgt_AttachSlot = body0._ikp().tgtHandL_AttachSlot;
                pL.Tgt_AttachName = body0._ikp().tgtHandL_AttachName;
                pL.TgtTr = body0._ikp().tgtHandL;
                pL.TgtOffset = body0._ikp().tgtHandL_offset;

                pR.TgtMaid = body0._ikp().tgtMaidR;
                pR.Tgt_AttachSlot = body0._ikp().tgtHandR_AttachSlot;
                pR.Tgt_AttachName = body0._ikp().tgtHandR_AttachName;
                pR.TgtTr = body0._ikp().tgtHandR;
                pR.TgtOffset = body0._ikp().tgtHandR_offset;
            }

            public bool RestoreAtc(ref TBody body0)
            {
                if (!body0)
                    return true; // 居なかったら復元不要なので成功とする

                if (this.bkupbody != body0)
                    return false; // 別人なら

                body0._ikp().tgtMaidL = pL.TgtMaid;
                body0._ikp().tgtHandL_AttachSlot = pL.Tgt_AttachSlot;
                body0._ikp().tgtHandL_AttachName = pL.Tgt_AttachName;
                body0._ikp().tgtHandL = pL.TgtTr;
                body0._ikp().tgtHandL_offset = pL.TgtOffset;

                body0._ikp().tgtMaidR = pR.TgtMaid;
                body0._ikp().tgtHandR_AttachSlot = pR.Tgt_AttachSlot;
                body0._ikp().tgtHandR_AttachName = pR.Tgt_AttachName;
                body0._ikp().tgtHandR = pR.TgtTr;
                body0._ikp().tgtHandR_offset = pR.TgtOffset;

                return true;
            }
        }

        static void AtccHand1R(Maid slave, Maid tgtm, string atcpTgt, Vector3 v3HandOffset)
        {
            //Maid.IKTargetToAttachPointより
            int slotid = (int)TBody.SlotID.body;
            string atcpName = string.Empty;

            if (tgtm && !string.IsNullOrEmpty(atcpTgt))
            {
                // v5.0 ボーンにアタッチ
                if (atcpTgt.StartsWith(Defines.data.comboBonePrefix, StringComparison.Ordinal))
                {
                    var bonetgt = BoneLink.BoneLink.SearchObjName(tgtm.body0.m_Bones.transform, atcpTgt.Remove(0, Defines.data.comboBonePrefix.Length), true);

                    slave.body0._ikp().tgtMaidR = tgtm;
                    slave.body0._ikp().tgtHandR_AttachSlot = -1;
                    slave.body0._ikp().tgtHandR_AttachName = string.Empty;
                    slave.body0._ikp().tgtHandR = bonetgt;
                    slave.body0._ikp().tgtHandR_offset = v3HandOffset;//Vector3.zero;
                    return;
                }

                string[] sa = atcpTgt.Split('⇒');
                if (sa.Length == 2)
                {
                    try
                    {
                        slotid = (int)Enum.Parse(typeof(TBody.SlotID), sa[0]);
                        atcpName = sa[1];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("XtMS-AtccHand1R：" + e);
                    }
                }
            }

            slave.body0._ikp().tgtMaidR = tgtm ? tgtm : null;
            slave.body0._ikp().tgtHandR_AttachSlot = slotid;
            slave.body0._ikp().tgtHandR_AttachName = atcpName;
            slave.body0._ikp().tgtHandR = null;
            slave.body0._ikp().tgtHandR_offset = v3HandOffset;//Vector3.zero;
        }

        static void AtccHand1L(Maid slave, Maid tgtm, string atcpTgt, Vector3 v3HandLOffset)
        {
            //Maid.IKTargetToAttachPointより
            int slotid = (int)TBody.SlotID.body;
            string atcpName = string.Empty;

            if (tgtm && !string.IsNullOrEmpty(atcpTgt))
            {
                // v5.0 ボーンにアタッチ
                if (atcpTgt.StartsWith(Defines.data.comboBonePrefix, StringComparison.Ordinal))
                {
                    var bonetgt = BoneLink.BoneLink.SearchObjName(tgtm.body0.m_Bones.transform, atcpTgt.Remove(0, Defines.data.comboBonePrefix.Length), true);

                    slave.body0._ikp().tgtMaidL = tgtm;
                    slave.body0._ikp().tgtHandL_AttachSlot = -1;
                    slave.body0._ikp().tgtHandL_AttachName = string.Empty;
                    slave.body0._ikp().tgtHandL = bonetgt;
                    slave.body0._ikp().tgtHandL_offset = v3HandLOffset;//Vector3.zero;
                    return;
                }

                string[] sa = atcpTgt.Split('⇒');
                if (sa.Length == 2)
                {
                    try
                    {
                        slotid = (int)Enum.Parse(typeof(TBody.SlotID), sa[0]);
                        atcpName = sa[1];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("XtMS-AtccHand1L：" + e);
                    }
                }
            }

            slave.body0._ikp().tgtMaidL = tgtm ? tgtm : null;
            slave.body0._ikp().tgtHandL_AttachSlot = slotid;
            slave.body0._ikp().tgtHandL_AttachName = atcpName;
            slave.body0._ikp().tgtHandL = null;
            slave.body0._ikp().tgtHandL_offset = v3HandLOffset;//Vector3.zero;
        }

        static void AtccHandR(Maid slave, Maid tgtm, string boneName, Vector3 v3HandLOffset)
        {
            //Maid.IKTargetToAttachPointより
            //Transform tgt = BoneLink.BoneLink.SearchObjName(tgtm.body0.goSlot[(int)TBody.SlotID.body].obj.transform, boneName, true);
            Transform tgt = BoneLink.BoneLink.SearchObjName(tgtm.body0.m_Bones.transform, boneName, true);
            if (tgt)
            {
                slave.body0._ikp().tgtMaidR = tgtm;
                slave.body0._ikp().tgtHandR_AttachSlot = (int)TBody.SlotID.body;
                slave.body0._ikp().tgtHandR_AttachName = string.Empty;
                slave.body0._ikp().tgtHandR = tgt;
                slave.body0._ikp().tgtHandR_offset = v3HandLOffset;//Vector3.zero;
            }
        }

        static void AtccHandL(Maid slave, Maid tgtm, string boneName, Vector3 v3HandLOffset)
        {
            //Maid.IKTargetToAttachPointより
            //Transform tgt = BoneLink.BoneLink.SearchObjName(tgtm.body0.goSlot[(int)TBody.SlotID.body].obj.transform, boneName, true);
            Transform tgt = BoneLink.BoneLink.SearchObjName(tgtm.body0.m_Bones.transform, boneName, true);
            if (tgt)
            {
                slave.body0._ikp().tgtMaidL = tgtm;
                slave.body0._ikp().tgtHandL_AttachSlot = (int)TBody.SlotID.body;
                slave.body0._ikp().tgtHandL_AttachName = string.Empty;
                slave.body0._ikp().tgtHandL = tgt;
                slave.body0._ikp().tgtHandL_offset = v3HandLOffset;//Vector3.zero;
            }
        }

#if test_cm3d2
        bool bone2Atcp(Maid maid, string bonename)
        {
            // とりあえずbody限定
            int slotid = (int)TBody.SlotID.body; //Enum.Parse(typeof(TBody.SlotID), sa[0]);
            string atcp_sName = bonename;

            var bodytmph = maid.body0.goSlot[(int)TBody.SlotID.body].morph;
            var bodyBindVert = maid.body0.goSlot[(int)TBody.SlotID.body].morph.BindVert;

            if (bodytmph.dicAttachPoint.ContainsKey(atcp_sName))
                return true;

            var trs = CMT.SearchObjName(maid.body0.m_Bones.transform, bonename, true);
            if (!trs)
                return false;

            var vc = trs.position;
            var rot = trs.rotation;
            
            Transform[] t_bones = bodytmph.GetType().InvokeMember("m_bones", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, null, bodytmph, null) as Transform[];
            if (t_bones == null)
                return false;

            //Transform transform = this.m_bones[tmph.BindBone[vidx]].transform;
            Transform transform = t_bones[bodytmph.BindBone[0]].transform;
            //vector = transform.TransformPoint(vector);
            float num = (vc - transform.TransformPoint(bodyBindVert[0])).sqrMagnitude;
            int num2 = 0;
            for (int i = 0; i < bodytmph.m_vOriVert.Length; i++)
            {
                //float sqrMagnitude = (vc - tmph.DefVert[i]).sqrMagnitude;
                transform = t_bones[bodytmph.BindBone[i]].transform;
                float sqrMagnitude = (vc - transform.TransformPoint(bodyBindVert[i])).sqrMagnitude;
                if (num > sqrMagnitude)
                {
                    num = sqrMagnitude;
                    num2 = i;
                }
            }
            int vidx = num2;
            var pos = bodytmph.DefVert[vidx];

            Transform bone_tr = t_bones[bodytmph.BindBone[vidx]].transform;
            Quaternion q = (Quaternion.Inverse(bone_tr.rotation) * rot);

            SetAttachPoint(bodytmph, atcp_sName, pos, q, (TBody.SlotID)slotid);

            return true;
        }

        // TMorphよりループ向けにするラッパー
        public void SetAttachPoint(TMorph tm, string apname, Vector3 vc, Quaternion q, TBody.SlotID slot)
        {
            if (tm.dicAttachPoint.ContainsKey(apname))
            {
                TAttachPoint tAttachPoint = tm.dicAttachPoint[apname];
                int vidx = tAttachPoint.vidx;

                if (tm.DefVert[vidx] == vc)
                {
                    tAttachPoint.q = q; //角度のみ書き込み
                    tm.dicAttachPoint[apname] = tAttachPoint;
                    return; //変更なしなら更新不要（全頂点検索しにいくっぽいので毎回は重そう）
                }
            }
            tm.SetAttachPoint(apname, vc, q);

            //BindVert更新
            if (!RenewBindVert(slot, tm, apname))
            {
                Console.WriteLine("BindVertの更新に失敗しました。アタッチが正常に表示されない可能性があります");
            }
        }
#endif

        class gaoo
        {
            static public Matrix4x4[] m_bindposes = null;
            static public Vector3[] m_vTmpVert = null;

            static public void reset()
            {
                m_bindposes = null;
                m_vTmpVert = null;
            }
        }

        public bool RenewBindVert(TBody.SlotID slotID, TMorph tm, string apname)
        {
            if (!tm.dicAttachPoint.ContainsKey(apname))
            {
                return false;
            }
            TAttachPoint tAttachPoint = tm.dicAttachPoint[apname];
            //Vector3 vector = Vector3.zero;
            int vidx = tAttachPoint.vidx;

            //Transform[] _bones = tm.GetType().InvokeMember("m_bones", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, null, tm, null) as Transform[];
            gaoo.m_bindposes = tm.GetType().InvokeMember("m_bindposes", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, null, tm, null) as Matrix4x4[];
            gaoo.m_vTmpVert = tm.GetType().InvokeMember("m_vTmpVert", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, null, tm, null) as Vector3[];

            if (/*_bones == null ||*/ gaoo.m_bindposes == null || gaoo.m_vTmpVert == null)
            {
                return false;
            }

            if (slotID == TBody.SlotID.head)
            {
                //FixBlendValues_Faceより
                Vector3 vector = Vector3.zero;
                vector += gaoo.m_bindposes[tAttachPoint.bw.boneIndex0].MultiplyPoint3x4(gaoo.m_vTmpVert[vidx]) * tAttachPoint.bw.weight0;
                vector += gaoo.m_bindposes[tAttachPoint.bw.boneIndex1].MultiplyPoint3x4(gaoo.m_vTmpVert[vidx]) * tAttachPoint.bw.weight1;
                vector += gaoo.m_bindposes[tAttachPoint.bw.boneIndex2].MultiplyPoint3x4(gaoo.m_vTmpVert[vidx]) * tAttachPoint.bw.weight2;
                vector += gaoo.m_bindposes[tAttachPoint.bw.boneIndex3].MultiplyPoint3x4(gaoo.m_vTmpVert[vidx]) * tAttachPoint.bw.weight3;
                tm.BindVert[vidx] = vector;
            }
            else
            {
                //FixBlendValueより
                tm.BindVert[vidx] = gaoo.m_bindposes[tAttachPoint.bw.boneIndex0].MultiplyPoint3x4(gaoo.m_vTmpVert[vidx]);
            }

            gaoo.reset();
            return true;
        }
#endregion

        public class MsUtil
        {
            /// <summary>
            ///     スレイブの選択
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <param name="memNum">Slaveリストのキャラ番号</param>
            /// <returns>成功ならTrue</returns>
            public static bool SelectSlave(int pageNum, int memNum)
            {
                if (memNum < 0 || pageNum < 0)
                    return false;

                MsLinks ms_ = _MSlinks[pageNum];
                MsLinkConfig p_mscfg = cfgs[pageNum];
                v3Offsets p_v3of = v3ofs[pageNum];

                if (ms_.mdSlave_No >= 0)
                {
                    //アタッチの解除
                    if (p_mscfg.doIKTargetMHand || p_mscfg.doCopyIKTarget)
                    {
                        ms_.mdSlaves[ms_.mdSlave_No].mem.IKTargetClear();
                    }
                    else if (p_mscfg.doIKTargetMHandSpCustom)
                    {
                        //ms_.bkupHandTgt.RestoreAtc(ref ms_.mdSlaves[ms_.mdSlave_No].mem.body0);
                        // 両手をアタッチ⇒カスタムアタッチ⇒両手をアタッチ解除→こことか、条件を考えるのがめんどうなので全解除
                        ms_.mdSlaves[ms_.mdSlave_No].mem.IKTargetClear();
                        ms_.bkupHandTgt = null;
                    }
                }

                //選択変更
                ms_.mdSlave_No = memNum;

                if (ms_.mdSlave_No >= ms_.mdSlaves.Count)
                {
                    ms_.mdSlave_No = -1; //選択解除
                    ms_.maidKeepSlaveYotogi = null;
                }
                else
                {
                    //キープメイド更新
                    if (ms_.doMasterSlave && ms_.mdSlave_No > 0 && !ms_.mdSlaves[ms_.mdSlave_No].mem.boMAN)
                    {
                        if (p_mscfg.doKeepSlaveYotogi && XtMasterSlave.IsKeepScene()/*(vIsKaisouScene || bIsYotogiScene)*/)
                            ms_.maidKeepSlaveYotogi = ms_.mdSlaves[ms_.mdSlave_No].mem;
                    }
                    else
                    {
                        ms_.maidKeepSlaveYotogi = null;
                    }
                }

                ms_.FixSlave();
                return true;
            }

            /// <summary>
            ///     マスターの選択
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <param name="memNum">Masterリストのキャラ番号</param>
            /// <returns>成功ならTrue</returns>
            public static bool SelectMaster(int pageNum, int memNum)
            {
                if (memNum < 0 || pageNum < 0)
                    return false;

                MsLinks ms_ = _MSlinks[pageNum];
                //MsLinkConfig p_mscfg = cfgs[pageNum];
                //v3Offsets p_v3of = v3ofs[pageNum];

                ms_.mdMaster_No = memNum;

                if (ms_.mdMaster_No >= ms_.mdMasters.Count)
                    ms_.mdMaster_No = -1; //選択解除

                ms_.FixMaster();
                return true;
            }

            /// <summary>
            ///     M/Sモード変更
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <param name="isMaidMaster">メイドがマスター</param>
            public static void ChangeMsMode(int pageNum, bool isMaidMaster)
            {
                MsLinks ms_ = _MSlinks[pageNum];
                MsLinkConfig p_mscfg = cfgs[pageNum];
                //v3Offsets p_v3of = v3ofs[pageNum];

                if (!isMaidMaster)
                {
                    if (ms_.Scc1_MasterMaid)
                    {
                        // "Man⇒Maid"
                        ms_.Scc1_MasterMaid = false;
                        ms_.doMasterSlave = false;

                        int n = ms_.mdMaster_No;
                        ms_.mdMaster_No = ms_.mdSlave_No;

                        if (n >= 0 && ms_.testSlaved(_MaidList[n].mem, out int iran))
                        { //スレイブが重複しないかチェックして重複なら解除
                            n = -1;
                        }
                        ms_.mdSlave_No = n;
                        //コンボ初期化
                        CloseAllCombos();
                    }
                }
                else
                {
                    if (!ms_.Scc1_MasterMaid)
                    {
                        ms_.Scc1_MasterMaid = true;
                        ms_.doMasterSlave = false;

                        int n = ms_.mdMaster_No;
                        ms_.mdMaster_No = ms_.mdSlave_No;

                        if (n >= 0 && ms_.testSlaved(_MensList[n].mem, out int iran))
                        { //スレイブが重複しないかチェックして重複なら解除
                            n = -1;
                        }
                        ms_.mdSlave_No = n;
                        //コンボ初期化
                        CloseAllCombos();
                    }
                }
                return;
            }

            /// <summary>
            ///     指定ページのSlaveまたはMaster候補者リストを返す
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <param name="GetMasters">マスターを取得したい場合True（FalseでSlave）</param>
            /// <returns>メイドまたは男リスト、エラーでNULL</returns>
            public static Maid[] GetMembersList(int pageNum, bool GetMasters)
            {
                if (pageNum < 0)
                    return null;

                MsLinks ms_ = _MSlinks[pageNum];
                //MsLinkConfig p_mscfg = cfgs[pageNum];
                //v3Offsets p_v3of = v3ofs[pageNum];

                var m = ms_.mdSlaves;
                if (GetMasters)
                {
                    m = ms_.mdSlaves;
                }

                Maid[] ret = null;
                if (m != null)
                {
                    List<Maid> lm = new List<Maid>();
                    foreach (var p in m)
                    {
                        lm.Add(p.mem);
                    }
                    ret = lm.ToArray();
                }

                return ret;
            }

            /// <summary>
            ///     指定ページで選択中のキャラクター番号
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <param name="GetMasters">マスターを取得したい場合True（FalseでSlave）</param>
            /// <returns>エラーまたは未選択なら-1</returns>
            public static int GetMemberNum(int pageNum, bool GetMaster)
            {
                if (pageNum < 0)
                    return -1;

                MsLinks ms_ = _MSlinks[pageNum];
                //MsLinkConfig p_mscfg = cfgs[pageNum];
                //v3Offsets p_v3of = v3ofs[pageNum];

                var m = ms_.mdSlave_No;
                if (GetMaster)
                {
                    m = ms_.mdMaster_No;
                }

                return m;
            }

            /// <summary>
            ///     Msリンクの開始か停止状態取得
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <returns>実行中ならTrue</returns>
            public static bool IsStartMsLink(int pageNum)
            {
                MsLinks ms_ = _MSlinks[pageNum];

                return ms_.doMasterSlave;
            }

            /// <summary>
            ///     Msリンクの開始か停止
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <param name="Stop">停止したい時はTrue</param>
            /// <returns>成功ならTrue</returns>
            public static bool StartMsLink(int pageNum, bool Stop)
            {
                if (pageNum < 0)
                    return false;

                MsLinks ms_ = _MSlinks[pageNum];
                Maid slave = (ms_.mdSlave_No >= 0 && ms_.mdSlaves.Count() > ms_.mdSlave_No) ? ms_.mdSlaves[ms_.mdSlave_No].mem : null;

                if (!slave || !slave.body0)
                {
                    return false;
                }

                return StartMsLink(pageNum, Stop, true, slave);
            }

            // 内部用
            /// <summary>
            ///     Msリンクの開始か停止
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <param name="Stop">停止したい時はTrue</param>
            /// <param name="LinkCheck">GUIではFalse</param>
            /// <param name="slave">GUI用</param>
            /// <returns>成功ならTrue</returns>
            internal static bool StartMsLink(int pageNum, bool Stop, bool LinkCheck, Maid slave)
            {
                MsLinks ms_ = _MSlinks[pageNum];
                MsLinkConfig p_mscfg = cfgs[pageNum];
                //v3Offsets p_v3of = v3ofs[pageNum];

                if (LinkCheck)
                {
                    if (ms_.testLoopLink())
                    {
                        //リンクの無限ループを防止
                        return false;
                    }
                    else if (ms_.testOverlapedLink())
                    {
                        //リンクのダブりを防止
                        return false;
                    }
                }

                if (Stop == !ms_.doMasterSlave)
                    return true; //変化なし

                //if (GUI.Button(rcItem, "Master-Slave *" + (_pageNum + 1) + " リンク" + (!ms_.doMasterSlave ? "実行" : "停止"), gsButton))
                ms_.doMasterSlave = !ms_.doMasterSlave;
                if (ms_.doMasterSlave)
                {
                    if (p_mscfg.doKeepSlaveYotogi && XtMasterSlave.IsKeepScene() && !slave.boMAN)
                        ms_.maidKeepSlaveYotogi = slave;
                }
                else
                {
                    ms_.maidKeepSlaveYotogi = null;

                    //アタッチの解除
                    if (p_mscfg.doIKTargetMHand || p_mscfg.doCopyIKTarget)
                    {
                        slave.IKTargetClear();
                    }
                }
                return true;
            }

            /// <summary>
            ///     上級者向けリンク状態設定。アタッチ解除とかしないので後始末出来る人用
            /// </summary>
            /// <param name="pageNum">ページ番号</param>
            /// <param name="StackPos">位置の重ね有無</param>
            /// <param name="StackPos">局部で位置合わせ有無</param>
            /// <param name="AtcMsHands">両手をマスターにアタッチ</param>
            /// <param name="CopyIkHands">両手のIKコピー</param>
            /// <param name="PosSync">位置のみリンク有無</param>
            /// <param name="VoiceAndFacePlay">ボイス＆フェイス変更</param>
            /// <returns>成功ならTrue</returns>
            public static bool ConfigMsLink(int pageNum, bool StackPos, bool AutoCnkPos, bool AtcMsHands, bool CopyIkHands, bool PosSync, bool VoiceAndFacePlay)
            {
                if (pageNum < 0)
                    return false;

                MsLinks ms_ = _MSlinks[pageNum];
                MsLinkConfig p_mscfg = cfgs[pageNum];

                p_mscfg.doStackSlave = StackPos;
                p_mscfg.doStackSlave_CliCnk = AutoCnkPos;
                p_mscfg.doIKTargetMHand = AtcMsHands;
                p_mscfg.doCopyIKTarget = CopyIkHands;
                p_mscfg.doStackSlave_PosSyncMode = PosSync;
                p_mscfg.doVoiceAndFacePlay = VoiceAndFacePlay;

                return true;
            }

        }

#region FaceSync
        public static void FaceBlend2Sync(XtTMorph tm, XtTMorph tm_tgt, bool isLateUpdate)
        {
            if (!isLateUpdate)
            {
                tm_tgt.BlendValues[(int)tm_tgt.hash["hohol"]] = tm.BlendValues[(int)tm.hash["hohol"]];
                tm_tgt.BlendValues[(int)tm_tgt.hash["hohos"]] = tm.BlendValues[(int)tm.hash["hohos"]];
                tm_tgt.BlendValues[(int)tm_tgt.hash["hohos"]] = tm.BlendValues[(int)tm.hash["hohos"]];

                tm_tgt.BlendValues[(int)tm_tgt.hash["hoho2"]] = tm.BlendValues[(int)tm.hash["tear3"]];
                tm_tgt.BlendValues[(int)tm_tgt.hash["hoho2"]] = tm.BlendValues[(int)tm.hash["tear2"]];
                tm_tgt.BlendValues[(int)tm_tgt.hash["hoho2"]] = tm.BlendValues[(int)tm.hash["tear1"]];

                tm_tgt.BlendValues[(int)tm_tgt.hash["yodare"]] = tm.BlendValues[(int)tm.hash["yodare"]];
            }
            else
            {
                tm_tgt.BlendValues[(int)tm_tgt.hash["hoho2"]] = tm.BlendValues[(int)tm.hash["hoho2"]];
                tm_tgt.BlendValues[(int)tm_tgt.hash["namida"]] = tm.BlendValues[(int)tm.hash["namida"]];
                tm_tgt.BlendValues[(int)tm_tgt.hash["shock"]] = tm.BlendValues[(int)tm.hash["shock"]];
            }
            tm_tgt.morph.FixBlendValues_Face();
        }

        class VymMouthAnime
        {
            //VibeYourMaidプラグインより頂きました

            //メイドの口元変更
            private float MouthHoldTime = 0f;
            //private int MouthMode = 0;
            private int OldMode = 0;
            private float MaValue;
            private float MiValue;
            //private float McValue;
            private float MdwValue;
            private float TupValue = 0f;
            private float ToutValue = 0f;
            private float TopenValue = 0f;
            private float TupValue2 = 0.3f;
            private float ToutValue2 = 0.3f;
            private float TopenValue2 = 0.4f;

            public void MouthChange(Maid maid, int mode)
            {
                float timerRate = Time.deltaTime * 60;

                float maV; //口あ
                float miV; //口い
                float mcV; //口う
                float msV; //笑顔
                float mdwV; //口角上げ
                float mupV; //口角下げ

                if (mode != OldMode)
                {
                    MouthHoldTime = 0;
                    OldMode = mode;
                }

                if (MouthHoldTime <= 0)
                {
                    MouthHoldTime = UnityEngine.Random.Range(180f, 360f);

                    if (mode == 0)
                    {  //通常時
                        MaValue = UnityEngine.Random.Range(0f, 30f) / 100f;
                        MdwValue = UnityEngine.Random.Range(0f, 30f) / 100f;
                    }
                    if (mode == 1)
                    {  //キス時
                        MaValue = UnityEngine.Random.Range(20f, 60f) / 100f;
                        MdwValue = UnityEngine.Random.Range(0f, 50f) / 100f;
                    }
                    if (mode == 2)
                    {  //フェラ時
                        MaValue = UnityEngine.Random.Range(80f, 100f) / 100f;
                    }
                    if (mode == 3)
                    {  //連続絶頂時１
                        MaValue = UnityEngine.Random.Range(70f, 90f) / 100f;
                        MdwValue = UnityEngine.Random.Range(30f, 90f) / 100f;
                    }
                    if (mode == 4)
                    {  //連続絶頂時２
                        MiValue = UnityEngine.Random.Range(30f, 50f) / 100f;
                        MdwValue = UnityEngine.Random.Range(20f, 40f) / 100f;
                    }
                    if (mode == 5)
                    {  //余韻時
                        MaValue = UnityEngine.Random.Range(10f, 40f) / 100f;
                        MdwValue = UnityEngine.Random.Range(0f, 30f) / 100f;
                    }

                }

                MouthHoldTime -= timerRate;

                var morph = maid.body0.Face.XtMorph();
                maV = morph.BlendValues[(int)maid.body0.Face.morph.hash[(object)"moutha"]] + MaValue;
                miV = morph.BlendValues[(int)maid.body0.Face.morph.hash[(object)"mouthi"]] + MiValue;
                mcV = morph.BlendValues[(int)maid.body0.Face.morph.hash[(object)"mouthc"]];
                msV = morph.BlendValues[(int)maid.body0.Face.morph.hash[(object)"mouths"]];
                mdwV = morph.BlendValues[(int)maid.body0.Face.morph.hash[(object)"mouthdw"]] + MdwValue;
                mupV = morph.BlendValues[(int)maid.body0.Face.morph.hash[(object)"mouthup"]];


                //舌の動き処理
                //キス時とフェラ時
                if (mode == 1 || mode == 2)
                {
                    if (TupValue < TupValue2)
                    {
                        TupValue += Time.deltaTime * 0.5f;
                        if (TupValue >= TupValue2) { TupValue2 = UnityEngine.Random.Range(0f, 60f) / 100f; }
                    }
                    else
                    {
                        TupValue -= Time.deltaTime * 0.5f;
                        if (TupValue <= TupValue2) { TupValue2 = UnityEngine.Random.Range(0f, 60f) / 100f; }
                    }

                    if (ToutValue < ToutValue2)
                    {
                        ToutValue += Time.deltaTime * 0.5f;
                        if (ToutValue >= ToutValue2) { ToutValue2 = UnityEngine.Random.Range(0f, 50f) / 100f; }
                    }
                    else
                    {
                        ToutValue -= Time.deltaTime * 0.5f;
                        if (ToutValue <= ToutValue2) { ToutValue2 = UnityEngine.Random.Range(0f, 50f) / 100f; }
                    }

                    if (TopenValue < TopenValue2)
                    {
                        TopenValue += Time.deltaTime * 0.5f;
                        if (TopenValue >= TopenValue2) { TopenValue2 = UnityEngine.Random.Range(0f, 40f) / 100f; }
                    }
                    else
                    {
                        TopenValue -= Time.deltaTime * 0.5f;
                        if (TopenValue <= TopenValue2) { TopenValue2 = UnityEngine.Random.Range(0f, 40f) / 100f; }
                    }
                }
                //連続絶頂時
                if (mode == 3)
                {
                    if (TupValue < TupValue2)
                    {
                        TupValue += Time.deltaTime * 0.5f;
                        if (TupValue >= TupValue2) { TupValue2 = UnityEngine.Random.Range(0f, 40f) / 100f; }
                    }
                    else
                    {
                        TupValue -= Time.deltaTime * 0.5f;
                        if (TupValue <= TupValue2) { TupValue2 = UnityEngine.Random.Range(0f, 40f) / 100f; }
                    }

                    if (ToutValue < ToutValue2)
                    {
                        ToutValue += Time.deltaTime * 0.5f;
                        if (ToutValue >= ToutValue2) { ToutValue2 = UnityEngine.Random.Range(60f, 100f) / 100f; }
                    }
                    else
                    {
                        ToutValue -= Time.deltaTime * 0.5f;
                        if (ToutValue <= ToutValue2) { ToutValue2 = UnityEngine.Random.Range(60f, 100f) / 100f; }
                    }

                    if (TopenValue < TopenValue2)
                    {
                        TopenValue += Time.deltaTime * 0.5f;
                        if (TopenValue >= TopenValue2) { TopenValue2 = UnityEngine.Random.Range(0f, 60f) / 100f; }
                    }
                    else
                    {
                        TopenValue -= Time.deltaTime * 0.5f;
                        if (TopenValue <= TopenValue2) { TopenValue2 = UnityEngine.Random.Range(0f, 60f) / 100f; }
                    }
                }


                //口元破綻の抑制とシェイプキー操作
                if (mode == 0)
                {  //通常時
                    try
                    {
                        VertexMorph_FromProcItem(maid.body0, "moutha", maV);
                        VertexMorph_FromProcItem(maid.body0, "mouthdw", mdwV);
                    }
                    catch { /*LogError(ex);*/ }
                }
                if (mode == 1)
                {  //キス時
                    if (miV > 0.1f) VertexMorph_FromProcItem(maid.body0, "mouthi", 0.1f);
                    if (maV > 0.6f) maV = 0.6f;
                    try
                    {
                        VertexMorph_FromProcItem(maid.body0, "moutha", maV);
                        VertexMorph_FromProcItem(maid.body0, "mouthdw", mdwV);
                        VertexMorph_FromProcItem(maid.body0, "tangup", TupValue);
                        VertexMorph_FromProcItem(maid.body0, "tangout", ToutValue);
                        VertexMorph_FromProcItem(maid.body0, "tangopen", TopenValue);
                    }
                    catch { /*LogError(ex);*/ }
                }
                if (mode == 2)
                {  //フェラ時
                    if (miV > 0.1f) VertexMorph_FromProcItem(maid.body0, "mouthi", 0.1f);
                    if (mcV > 0.2f) VertexMorph_FromProcItem(maid.body0, "mouthc", 0.2f);
                    if (msV > 0.1f) VertexMorph_FromProcItem(maid.body0, "mouths", 0.1f);
                    if (mupV > 0.1f) VertexMorph_FromProcItem(maid.body0, "mouthup", 0.1f);
                    if (maV > 1.0f) maV = 1.0f;
                    try
                    {
                        VertexMorph_FromProcItem(maid.body0, "moutha", maV);
                        VertexMorph_FromProcItem(maid.body0, "mouthdw", mdwV);
                        VertexMorph_FromProcItem(maid.body0, "tangup", TupValue);
                        VertexMorph_FromProcItem(maid.body0, "tangout", ToutValue);
                        VertexMorph_FromProcItem(maid.body0, "tangopen", TopenValue);
                    }
                    catch { /*LogError(ex);*/ }
                }
                if (mode == 3)
                {  //連続絶頂時１
                    if (miV > 0.1f) VertexMorph_FromProcItem(maid.body0, "mouthi", 0.1f);
                    if (mcV > 0.2f) VertexMorph_FromProcItem(maid.body0, "mouthc", 0.2f);
                    if (msV > 0.1f) VertexMorph_FromProcItem(maid.body0, "mouths", 0.1f);
                    if (mupV > 0.1f) VertexMorph_FromProcItem(maid.body0, "mouthup", 0.1f);
                    if (maV > 1.0f) maV = 1.0f;
                    try
                    {
                        VertexMorph_FromProcItem(maid.body0, "moutha", maV);
                        VertexMorph_FromProcItem(maid.body0, "mouthdw", mdwV);
                        VertexMorph_FromProcItem(maid.body0, "tangup", TupValue);
                        VertexMorph_FromProcItem(maid.body0, "tangout", ToutValue);
                        VertexMorph_FromProcItem(maid.body0, "tangopen", TopenValue);
                    }
                    catch { /*LogError(ex);*/ }
                }
                if (mode == 4)
                {  //連続絶頂時２
                    if (mupV > 0f) VertexMorph_FromProcItem(maid.body0, "mouthup", 0f);
                    if (msV > 0.1f) VertexMorph_FromProcItem(maid.body0, "mouths", 0.1f);
                    try
                    {
                        VertexMorph_FromProcItem(maid.body0, "mouthi", miV);
                        VertexMorph_FromProcItem(maid.body0, "mouthdw", mdwV);
                        VertexMorph_FromProcItem(maid.body0, "toothoff", 0f);
                    }
                    catch { /*LogError(ex);*/ }
                }
                if (mode == 5)
                {  //余韻時
                    try
                    {
                        VertexMorph_FromProcItem(maid.body0, "moutha", maV);
                        VertexMorph_FromProcItem(maid.body0, "mouthdw", mdwV);
                    }
                    catch { /*LogError(ex);*/ }
                }
            }
        }


        static List<TMorph> m_NeedFixTMorphs = new List<TMorph>();
        //シェイプキー操作
        //戻り値はsTagの存在有無にしているので必要に応じて変更してください
        static public bool VertexMorph_FromProcItem(TBody body, string sTag, float f)
        {
            bool bRes = false;

            if (!body || sTag == null || sTag == "")
                return false;

            for (int i = 0; i < body.goSlot.Count; i++)
            {
                TMorph morph = body.goSlot[i].morph;
                if (morph != null)
                {
                    if (morph.Contains(sTag))
                    {
                        /*if (i == 1)
                        {
                            bFace = true;
                        }*/
                        bRes = true;
                        int h = (int)morph.hash[sTag];
                        var morphV = body.goSlot[i].XtMorph();
                        morphV.BlendValues[h] = f;

                        //後でまとめて更新する
                        //body.goSlot[i].morph.FixBlendValues();

                        //更新リストに追加
                        if (!m_NeedFixTMorphs.Contains(morph))
                            m_NeedFixTMorphs.Add(morph);
                    }
                }
            }
            return bRes;
        }


        //シェイプキー操作Fix(基本はUpdate等の最後に一度呼ぶだけで良いはず）
        static public void VertexMorph_FixBlendValues()
        {
            foreach (TMorph tm in m_NeedFixTMorphs)
            {
                if (tm != null) // bugfix
                {
                    tm.FixBlendValues();
                }
            }

            m_NeedFixTMorphs.Clear();
        }
#endregion


        public class AnimeState
        {
            static HashSet<AnimeState> listInst_ = new HashSet<AnimeState>();
            string ytg_Pre_sAnm = "";
            State ytg_bOrg = State.none;

            float last_anitime = 0f;

            [FlagsAttribute]
            public enum State
            {
                none = 0,
                zeccho = 1,
                yoin = 1 << 1,
                kiss = 1 << 2,
                uke = 1 << 3,
                sex = 1 << 4,
                taiki = 1 << 5,
                kousoku = 1 << 6,
                seme = 1 << 7,
            }

            public AnimeState()
            {
                listInst_.Add(this);
            }

            ~AnimeState()
            {
                if (listInst_.Contains(this))
                    listInst_.Remove(this);
            }

            public static void AllReset()
            {
                foreach (var m in listInst_)
                {
                    m.chk_motion_reset();
                }
            }

            public void chk_motion_reset()
            {
                {
                    ytg_Pre_sAnm = "";
                    ytg_bOrg = State.none;
                    last_anitime = 0f;
                }
            }
            public State chk_motion_state(TBody body, bool reset, out bool boMotionChanged)
            {
                if (reset)
                {
                    ytg_Pre_sAnm = "";
                    ytg_bOrg = State.none;
                }
                boMotionChanged = false;

                //モーション判定ここから
                //string anim = body.LastAnimeFN;

                //vym、回想対応版
                var anim_state = GetPlayingFN_withTime(body);
                string anim = anim_state.Key;


                if (anim == null || anim.Length <= 0)
                {
                    ytg_bOrg = State.none;
                    return State.none;
                }

                //モーション名取得して絶頂時のものなら絶頂処理
                if (anim != ytg_Pre_sAnm)//前回からモーション変化時のみ
                {
                    boMotionChanged = true;

                    ytg_bOrg = State.none;
                    string anim_lr = anim.ToLower();

                    foreach (string s in ycfg.sMensKousokuMotion)
                    {
                        //拘束役
                        if (Regex.IsMatch(anim_lr, s))
                        {
                            ytg_bOrg = State.kousoku;
                            break;
                        }
                    }

                    if (ytg_bOrg == State.none)
                    {
                        foreach (string s in ycfg.sMensZeccyouMotion)
                        {
                            if (Regex.IsMatch(anim_lr, s))
                            {
                                ytg_bOrg = State.zeccho;
                                break;
                            }
                        }
                    }

                    if (ytg_bOrg == State.none)
                    {
                        foreach (string s in ycfg.sMensZeccyouAfterMotion)
                        {
                            if (Regex.IsMatch(anim_lr, s))
                            {
                                ytg_bOrg = State.yoin;
                                break;
                            }
                        }
                    }

                    if (ytg_bOrg == State.none)
                    {
                        foreach (string s in ycfg.sMensTaikiMotion)
                        {
                            if (Regex.IsMatch(anim_lr, s))
                            {
                                ytg_bOrg = State.taiki;
                                break;
                            }
                        }
                    }

                    if (ytg_bOrg == State.none || ytg_bOrg == State.zeccho || ytg_bOrg == State.yoin)
                    {
                        foreach (string s in ycfg.sMensKissMotion)
                        {
                            if (Regex.IsMatch(anim_lr, s))
                            {
                                ytg_bOrg = ytg_bOrg | State.kiss;
                                break;
                            }
                        }
                    }

                    if (ytg_bOrg == State.none)
                    {
                        foreach (string s in ycfg.sMensUkeMotion)
                        {
                            if (Regex.IsMatch(anim_lr, s))
                            {
                                ytg_bOrg = State.uke;
                                break;
                            }
                        }
                    }

                    if (ytg_bOrg == State.none)
                    {
                        foreach (string s in ycfg.sMensSemeMotion)
                        {
                            //責め役
                            if (Regex.IsMatch(anim_lr, s))
                            {
                                ytg_bOrg = State.seme;
                                break;
                            }
                        }
                    }

                    if (ytg_bOrg == State.none)
                    {
                        foreach (string s in ycfg.sMensSexMotion)
                        {
                            if (Regex.IsMatch(anim_lr, s))
                            {
                                ytg_bOrg = State.sex;
                                break;
                            }
                        }
                    }
                    //debugPrintConsole("Masterモーション変更あり " + ytg_bOrg + " / " + anim);
                    if (cfg.boMasterMotionLog)
                        Console.WriteLine("Masterモーション変更あり 登録カテゴリ:" + ytg_bOrg + " / モーション名:" + anim);

                    ytg_Pre_sAnm = anim;
                }
                else
                {
                    if (anim_state.Value < last_anitime && (last_anitime % 1f) < 0.7f) //切りの悪いところでリセットが掛かった場合のみにする
                    {
                        boMotionChanged = true; //アニメリセット時(同一コマンド選択など)
                        debugPrintConsole("masterモーション変更あり " + anim + " " + anim_state.Value + " / " + last_anitime);
                    }
                }

                last_anitime = anim_state.Value;
                return ytg_bOrg;
            }

            //実際にプレイ中のモーション名を取得する
            string m_LastGetMotion = "";
            static readonly KeyValuePair<string, float> errFNwithTime = new KeyValuePair<string, float>("", 0);
            public KeyValuePair<string, float> GetPlayingFN_withTime(TBody body0)
            {
                Animation anim = body0.m_Bones.GetComponent<Animation>();

                if (!anim.isPlaying)
                    return errFNwithTime; //モーション停止中

                //LastAnimeFNの再生中チェック
                if (anim.IsPlaying(body0.LastAnimeFN))
                    return new KeyValuePair<string, float>(body0.LastAnimeFN, anim[body0.LastAnimeFN].normalizedTime/*.normalizedTime*/);  //再生中なら

                //debugPrintConsole("LastAnimeFNは再生待ち " + maid.body0.LastAnimeFN);
                if (!anim.IsPlaying(m_LastGetMotion))
                {
                    string LastOnceFN = null, LastLoopFN = null;
                    m_LastGetMotion = "";

                    WrapMode CurMode = WrapMode.Default;
                    foreach (AnimationState state in anim)
                    {
                        if (state.enabled) //再生中の物のみチェック
                        {
                            CurMode = state.wrapMode;   //最後の値が残る
                            if (CurMode == WrapMode.Once)
                                LastOnceFN = state.name;
                            if (CurMode == WrapMode.Loop)
                                LastLoopFN = state.name;
                        }
                    }
                    if (CurMode == WrapMode.Once)
                    {
                        //ループなしアニメの場合の処理
                        m_LastGetMotion = LastOnceFN;
                    }
                    else
                    {
                        //ループありアニメの場合の処理
                        m_LastGetMotion = LastLoopFN;
                    }
                }
                return new KeyValuePair<string, float>(m_LastGetMotion, anim[m_LastGetMotion].normalizedTime/*.normalizedTime*/);
            }

        }
    }

    public class OneScene : MonoBehaviour
    {
        private void OnDestroy()
        {
            Console.WriteLine("One Scene OnDestroy");
        }

    }
}

namespace ExtensionMethods
{
    public static class ComExt
    {
#if COM3D2
        public static bool IsCom3d2 { get { return true; } }

        public class XtTMorph
        {
            public TMorph morph;
            FieldInfo fiBlendValues = typeof(TMorph).GetField("BlendValues", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            public XtTMorph(TMorph tm)
            {
                morph = tm;
                BlendValues = fiBlendValues.GetValue(tm) as float[];
            }

            public System.Collections.Hashtable hash
            {
                get { return morph.hash; }
            }

            public float[] BlendValues;

            public static implicit operator XtTMorph(TMorph tm)
            {
                return new XtTMorph(tm);
            }
        }

        public static XtTMorph XtMorph(this TBodySkin tbs)
        {
            return new XtTMorph(tbs.morph);
        }

        public static void GetAttachPoint(this TMorph tm, string apname, out Vector3 vWorldPos, out Quaternion qWorldRot)
        {
            tm.GetAttachPoint(apname, out vWorldPos, out qWorldRot, out Vector3 dummy_Scale, false);
        }

        public class XtMaidParam
        {
            public XtMaidStatus status;

            public XtMaidParam(Maid m)
            {
                status = new XtMaidStatus(m);
            }
        }

        public class XtMaidStatus
        {
            public Maid maid;
            MaidStatus.Status s_;

            public XtMaidStatus(Maid m)
            {
                maid = m;
                s_ = maid.status;
            }

            public string last_name
            {
                get { return s_.lastName; }
            }

            public string first_name
            {
                get { return s_.firstName; }
            }

            public int cur_excite
            {
                get { return s_.currentExcite; }
            }

            public string personal
            {
                get { return s_.personal.uniqueName; }
            }
        }

        public static XtMaidParam XtParam(this Maid m)
        {
            return new XtMaidParam(m);
        }
#else
        public static bool IsCom3d2 { get { return false; } }

        public class XtTMorph
        {
            public TMorph morph;

            public XtTMorph(TMorph tm)
            {
                morph = tm;
            }

            public System.Collections.Hashtable hash
            {
                get { return morph.hash; }
            }

            public float[] BlendValues
            {
                get { return morph.BlendValues; }
            }

            public static implicit operator XtTMorph(TMorph tm)
            {
                return new XtTMorph(tm);
            }
        }

        public static XtTMorph XtMorph(this TBodySkin tbs)
        {
            return new XtTMorph(tbs.morph);
        }
        public static MaidParam XtParam(this Maid m)
        {
            return m.Param;
        }
#endif

    }

    public static class MyExtensions
    {
        static Dictionary<string, string> dicIkL = new Dictionary<string, string>
        {
            { "HandTgt", "tgtHandL" },
            { "TgtMaid", "tgtMaidL" },
            { "Tgt_AttachSlot", "tgtHandL_AttachSlot" },
            { "Tgt_AttachName", "tgtHandL_AttachName" },
            { "TgtOffset", "tgtHandL_offset" },
        };
        static Dictionary<string, string> dicIkR = new Dictionary<string, string>
        {
            { "HandTgt", "tgtHandR" },
            { "TgtMaid", "tgtMaidR" },
            { "Tgt_AttachSlot", "tgtHandR_AttachSlot" },
            { "Tgt_AttachName", "tgtHandR_AttachName" },
            { "TgtOffset", "tgtHandR_offset" },
        };

        static Dictionary<string, Type> dicType = new Dictionary<string, Type>
        {
            { "HandTgt", typeof(Transform) },
            { "TgtMaid", typeof(Maid) },
            { "Tgt_AttachSlot", typeof(int) },
            { "Tgt_AttachName", typeof(string) },
            { "TgtOffset", typeof(Vector3) },
        };

        class FiHand
        {
            public FieldInfo fiTgt, fiMaid, fiAslot, fiAname, fiOffset;
        }
        static FiHand _handL = new FiHand();
        static FiHand _handR = new FiHand();
        static FiHand _handNew = new FiHand();
        static FieldInfo fiPointL;
        static FieldInfo fiPointR;
        static bool NeedInit = true;
        static bool boLegacy = false;

        public static void IKTargetClear(this Maid m)
        {
            if (IkXT.IsNewIK)
            {
                IkXT.IkClear(m, null);
                return; // v3.2 fix
            }
            else
            {
                IKTargetClearOld(m);
            }
        }

        static void IKTargetClearOld(this Maid m)
        {
            m.IKTargetToAttachPoint("左手", null, "body", string.Empty, Vector3.zero);
            m.IKTargetToAttachPoint("右手", null, "body", string.Empty, Vector3.zero);
        }

        static Assembly LoadIkDll(string dllname)
        {
            // Sybarisのバージョンによってリダイレクトが変わるのでいくつか試す…
            Console.WriteLine("XtMS: Loading... " + dllname);
            //var asm = Assembly.Load("CM3D2.XtMasterSlave.IK159");
            Assembly asm = null;
            var dll = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + dllname;
            //Console.WriteLine(dll);
            //if (!File.Exists(dll))すら欺かれる…?
            try
            {
                asm = Assembly.LoadFile(dll);
            }
            catch
            {
            }

            if (asm == null)
            {
                dll = Path.GetFullPath(@".") + @"\Sybaris\Plugins\UnityInjector\" + dllname;
                Console.WriteLine(dll);
                try
                {
                    asm = Assembly.LoadFile(dll);
                }
                catch
                {
                }
            }

            if (asm == null)
            {
                dll = Directory.GetCurrentDirectory() + @"\Sybaris\Plugins\UnityInjector\" + dllname;
                Console.WriteLine(dll);
                try
                {
                    asm = Assembly.LoadFile(dll);
                }
                catch
                {
                }
            }

            if (asm == null)
            {
                dll = Path.GetDirectoryName(typeof(IkXT).Module.Assembly.Location) + @"\" + dllname;
                //Console.WriteLine(dll);
                asm = Assembly.LoadFile(dll);
            }

            return asm;
        }

        // 1.55以降用
        public class IkXT
        {
            public static IkInst _inst;

            // cm3d2 1.59~
            public static Type _typIKM159 = Assembly.Load("Assembly-CSharp").GetType("IKMgrData");
            public static bool IsIkMgr159 { get { return _typIKM159 != null; } }

            // com3d2 1.17~
            public static Type _typIKO117 = Assembly.Load("Assembly-CSharp").GetType("IKCtrlData");
            public static bool IsIkCtrlO117 { get { return _typIKO117 != null; } }

            // com3d2 1.18~
            public static Type _typFLIK = Assembly.Load("Assembly-CSharp").GetType("FullBodyIKCtrl");
            //public static MethodInfo _methIKG118 = Assembly.Load("Assembly-CSharp").GetType("FullBodyIKCtrl")
            //                                            .GetMethod("GetIKData", BindingFlags.Instance | BindingFlags.Public ,null, new Type[] { typeof(string), typeof(bool) }, null); 
            static int flagIkCtrlO118 = -1;
            public static bool IsIkCtrlO118 {
                get {
                    if (_typFLIK != null)
                    {
                        if (flagIkCtrlO118 == -1)
                        {
                            var m = _typFLIK.GetMethod("GetIKData", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(bool) }, null);
                            //return m != null;
                            flagIkCtrlO118 = (m != null) ? 1 : 0; 
                        }

                        if (flagIkCtrlO118 == 1)
                            return true;
                    }
                    return false;
                } }

            // com3d2 1.20.1~
            static int flagIkCtrlO120 = -1;
            public static bool IsIkCtrlO120
            {
                get
                {
                    if (_typFLIK != null)
                    {
                        if (flagIkCtrlO120 == -1)
                        {
                            // public virtual void Detach(IKAttachType attachType); 1.20で引数減った
                            var m = _typIKO117.GetMethod("Detach", BindingFlags.Instance | BindingFlags.Public);
                            if (m != null)
                            {
                                var p = m.GetParameters();
                                flagIkCtrlO120 = (p.Length == 1) ? 1 : 0;
                            }
                            else
                            {
                                flagIkCtrlO120 = 0;
                            }
                        }

                        if (flagIkCtrlO120 == 1)
                            return true;
                    }
                    return false;
                }
            }

            public static bool IsNewIK { get { return IsIkCtrlO117 || IsIkMgr159; } }

            static IkXT()
            {
#if false//COM3D2 //com117以降で無理に
                _inst = new XtMasterSlave_IK159.Ik159Inst();
#else
                if (IsIkCtrlO117)
                {
                    if (IsIkCtrlO120)
                    {
                        Console.WriteLine("XtMS: 新IKモードで動作開始(IKCtrl v1.20)");
#if COM3D2only
                        Assembly asm = LoadIkDll("COM3D2.XtMasterSlave.IKO120.xdll");
#else
                        Assembly asm = LoadIkDll("CM3D2.XtMasterSlave.IKO120.xdll");
#endif
                        var type = asm.GetType("XtMasterSlave_IK_XDLL.IkpInst");
                        _inst = Activator.CreateInstance(type) as IkInst;
                    }
                    else if (IsIkCtrlO118)
                    {
                        Console.WriteLine("XtMS: 新IKモードで動作開始(IKCtrl v1.18)");
#if COM3D2only
                        Assembly asm = LoadIkDll("COM3D2.XtMasterSlave.IKO118.xdll");
#else
                        Assembly asm = LoadIkDll("CM3D2.XtMasterSlave.IKO118.xdll");
#endif
                        var type = asm.GetType("XtMasterSlave_IK_XDLL.IkpInst");
                        _inst = Activator.CreateInstance(type) as IkInst;
                    }
                    else
                    {
                        Console.WriteLine("XtMS: 新IKモードで動作開始(IKCtrl v1.17)");
#if COM3D2only
                        Assembly asm = LoadIkDll("COM3D2.XtMasterSlave.IKO117.xdll");
#else
                        Assembly asm = LoadIkDll("CM3D2.XtMasterSlave.IKO117.xdll");
#endif
                        var type = asm.GetType("XtMasterSlave_IKO117.Iko117Inst");
                        _inst = Activator.CreateInstance(type) as IkInst;
                    }
                }
                else if (IsIkMgr159)
                {
                    Console.WriteLine("XtMS: 新IKモードで動作開始(IKMgr v1.59)");

                    //var asm = Assembly.Load("CM3D2.XtMasterSlave.IK159");
#if COM3D2
                    Assembly asm = LoadIkDll("COM3D2.XtMasterSlave.IK159.xdll");
#else
                    Assembly asm = LoadIkDll("CM3D2.XtMasterSlave.IK159.xdll");
#endif
                    var type = asm.GetType("XtMasterSlave_IK159.Ik159Inst");
                    _inst = Activator.CreateInstance(type) as IkInst;
                }
                else
                {
                    Console.WriteLine("XtMS: 旧IKモードで動作開始(IKMgr v1.59未満)");

                    _inst = new IkInst();
                }
#endif
                }

            public static bool IsNewPointIK(Maid m, string hand = "右手")
            {
                if (IsNewIK)
                    return _inst.IsNewPointIK(m, hand);
                return false;
            }

            public static object GetIkPoint(TBody body, string hand = "右手")
            {
                if (IsNewIK)
                    return _inst.GetIkPoint(body, hand);
                return null;
            }

            public static void IkClear(Maid tgt, XtMasterSlave.MsLinkConfig mscfg)
            {
                List<string> listHand = new List<string> { "右手", "左手" };
                IkClear(tgt, listHand, mscfg);
            }
            public static void IkClear(Maid tgt, List<string> listHand, XtMasterSlave.MsLinkConfig mscfg)
            {
                if (IsNewIK)
                    _inst.IkClear(tgt, listHand, mscfg);
            }

            public static void CopyHandIK(Maid master, Maid slave, XtMasterSlave.v3Offsets[] v3ofs, int num_)
            {
                if (IsNewIK)
                    _inst.CopyHandIK(master, slave, v3ofs, num_);
            }

            public static void SetHandIKRotate(string handName, Maid master, Maid slave, string boneTgtname, Vector3 v3HandLOffsetRot)
            {
                if (IsNewIK)
                    _inst.SetHandIKRotate(handName, master, slave, boneTgtname, v3HandLOffsetRot);
            }

            public static void SetHandIKTarget(XtMasterSlave.MsLinkConfig mscfg, string handName, Maid master, Maid slave, int slot_no, string attach_name, Transform target, Vector3 v3HandLOffset)
            {
                if (IsNewIK)
                    _inst.SetHandIKTarget(mscfg, handName, master, slave, slot_no, attach_name, target, v3HandLOffset);
            }

            public static object GetIKCmo(TBody body, string hand = "右手")
            {
                return _inst.GetIKCmo(body, hand);
            }
        }

        public class IkInst
        {
            public virtual bool IsNewPointIK(Maid m, string hand = "右手")
            {
                return false;
            }

            public virtual object GetIkPoint(TBody body, string hand = "右手")
            {
                return null;
            }

            public virtual object GetIkCtrl(Maid maid)
            {
                return null;
            }

            public virtual object GetIkCtrlPoint(TBody body, string hand = "右手")
            {
                return null;
            }

            public virtual void IkClear(Maid tgt, XtMasterSlave.MsLinkConfig mscfg)
            {
            }
            public virtual void IkClear(Maid tgt, List<string> listHand, XtMasterSlave.MsLinkConfig mscfg, int IkType = (-1))
            {
            }

            public virtual void CopyHandIK(Maid master, Maid slave, XtMasterSlave.v3Offsets[] v3ofs, int num_)
            {
            }

            public virtual void SetHandIKRotate(string handName, Maid master, Maid slave, string boneTgtname, Vector3 v3HandLOffsetRot)
            {
            }

            public virtual object GetIKCmo(TBody body, string hand = "右手")
            {
                return null;
            }

            public virtual void SetHandIKTarget(XtMasterSlave.MsLinkConfig mscfg, string handName, Maid master, Maid slave, int slot_no, string attach_name, Transform target, Vector3 v3HandLOffset)
            {
            }

            public virtual bool IKUpdate(TBody body)
            {
                return false; // 実行できたか
            }

            public virtual bool GetIKCmoPosRot(TBody body, out Vector3 pos, out Quaternion rot, string hand = "右手")
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                return false;
            }

            public virtual bool IKCmoUpdate(TBody body, Transform trh, Vector3 offset, string hand = "右手")
            {
                return false;
            }

            public virtual bool UpdateFinalIK(Maid maid, XtMasterSlave.MsLinks ms, XtMasterSlave.MsLinkConfig mscfg)
            {
                return false; // 実行できたか
            }
        }

        static void SetFI()
        {
            fiPointL = typeof(TBody).GetField("IkPointL", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            fiPointR = typeof(TBody).GetField("IkPointR", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (IkXT.IsIkCtrlO117)
            {
                Console.WriteLine("XtMS: COM3D2 Ver1.17以降相当のIKを検出");
                Type typL = IkXT._typIKO117.GetNestedType("IKParam", BindingFlags.Public);

                _handNew.fiTgt = typL.GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiMaid = typL.GetField("TgtMaid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiAslot = typL.GetField("Tgt_AttachSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiAname = typL.GetField("Tgt_AttachName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiOffset = typL.GetField("TgtOffset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            }
            else if (IkXT.IsIkMgr159)
            {
                Console.WriteLine("XtMS: CM3D2 Ver1.59以降相当のIKを検出");
                Type typL = IkXT._typIKM159.GetNestedType("IKParam", BindingFlags.Public);

                _handNew.fiTgt = typL.GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiMaid = typL.GetField("TgtMaid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiAslot = typL.GetField("Tgt_AttachSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiAname = typL.GetField("Tgt_AttachName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiOffset = typL.GetField("TgtOffset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            }
            else if (fiPointL == null && fiPointR == null)
            {
                Console.WriteLine("XtMS: CM3D2 Ver1.54以前互換モード");

                boLegacy = true;
                _handR.fiTgt = typeof(TBody).GetField("tgtHandR", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handR.fiMaid = typeof(TBody).GetField("tgtMaidR", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handR.fiAslot = typeof(TBody).GetField("tgtHandR_AttachSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handR.fiAname = typeof(TBody).GetField("tgtHandR_AttachName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handR.fiOffset = typeof(TBody).GetField("tgtHandR_offset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                _handL.fiTgt = typeof(TBody).GetField("tgtHandL", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handL.fiMaid = typeof(TBody).GetField("tgtMaidL", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handL.fiAslot = typeof(TBody).GetField("tgtHandL_AttachSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handL.fiAname = typeof(TBody).GetField("tgtHandL_AttachName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handL.fiOffset = typeof(TBody).GetField("tgtHandL_offset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            }
            else
            {
                Console.WriteLine("XtMS: CM3D2 Ver1.55以降を検出");
                Type typL = typeof(TBody).GetNestedType("IKParamData", BindingFlags.Public);

                _handNew.fiTgt = typL.GetField("HandTgt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiMaid = typL.GetField("TgtMaid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiAslot = typL.GetField("Tgt_AttachSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiAname = typL.GetField("Tgt_AttachName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                _handNew.fiOffset = typL.GetField("TgtOffset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            }
        }

        public class SetIK
        {
            TBody body;
            object objL;
            object objR;
            FiHand fiR, fiL;

            public Transform tgtHandR
            {
                get
                {
                    return (Transform)fiR.fiTgt.GetValue(objR);
                }
                set
                {
                    fiR.fiTgt.SetValue(objR, value);
                }
            }
            public Transform tgtHandL
            {
                get
                {
                    return (Transform)fiL.fiTgt.GetValue(objL);
                }
                set
                {
                    fiL.fiTgt.SetValue(objL, value);
                }
            }

            public Maid tgtMaidR
            {
                get
                {
                    return fiR.fiMaid.GetValue(objR) as Maid;
                }
                set
                {
                    fiR.fiMaid.SetValue(objR, value);
                }
            }
            public Maid tgtMaidL
            {
                get
                {
                    return fiL.fiMaid.GetValue(objL) as Maid;
                }
                set
                {
                    fiL.fiMaid.SetValue(objL, value);
                }
            }

            public Vector3 tgtHandR_offset
            {
                get
                {
                    return (Vector3)fiR.fiOffset.GetValue(objR);
                }
                set
                {
                    fiR.fiOffset.SetValue(objR, value);
                }
            }
            public Vector3 tgtHandL_offset
            {
                get
                {
                    return (Vector3)fiL.fiOffset.GetValue(objL);
                }
                set
                {
                    fiL.fiOffset.SetValue(objL, value);
                }
            }

            public int tgtHandR_AttachSlot
            {
                get
                {
                    return (int)fiR.fiAslot.GetValue(objR);
                }
                set
                {
                    fiR.fiAslot.SetValue(objR, value);
                }
            }
            public int tgtHandL_AttachSlot
            {
                get
                {
                    return (int)fiL.fiAslot.GetValue(objL);
                }
                set
                {
                    fiL.fiAslot.SetValue(objL, value);
                }
            }

            public string tgtHandR_AttachName
            {
                get
                {
                    return fiR.fiAname.GetValue(objR) as string;
                }
                set
                {
                    fiR.fiAname.SetValue(objR, value);
                }
            }
            public string tgtHandL_AttachName
            {
                get
                {
                    return fiL.fiAname.GetValue(objL) as string;
                }
                set
                {
                    fiL.fiAname.SetValue(objL, value);
                }
            }

            void Init()
            {
                if (NeedInit)
                {
                    SetFI();
                    NeedInit = false;
                }

                if (IkXT.IsIkMgr159 || IkXT.IsIkCtrlO117)
                {
                    /*
                    objR = body.StrIKDataPair["右手"].GetIKParam(IKMgrData.IKAttachType.Point);
                    if (objR == null)
                        objR = body.StrIKDataPair["右手"].GetIKParam(IKMgrData.IKAttachType.NewPoint);

                    objL = body.StrIKDataPair["左手"].GetIKParam(IKMgrData.IKAttachType.Point);
                    if (objL==null)
                        objL = body.StrIKDataPair["左手"].GetIKParam(IKMgrData.IKAttachType.NewPoint);
                        */
                    objR = IkXT.GetIkPoint(body, "右手");
                    objL = IkXT.GetIkPoint(body, "左手");
                    fiR = _handNew;
                    fiL = _handNew;
                }
                else if (boLegacy)
                {
                    objR = body;
                    objL = body;
                    fiR = _handR;
                    fiL = _handL;
                }
                else
                {
                    objR = fiPointR.GetValue(body);
                    objL = fiPointL.GetValue(body);
                    fiR = _handNew;
                    fiL = _handNew;
                }
            }

            public SetIK(TBody body)
            {
                this.body = body;
                Init();
            }
        }

        // フレーム内キャッシュ (フレーム内の参照回数が少なければ不要）
        static Dictionary<TBody, SetIK> dicIKset = new Dictionary<TBody, SetIK>();
        static int FrameCnt = 0;

        /// <summary>
        ///   Maid.body0.tgtHandR/L→Maid.body0._ikp().tgtHandR/Lとするだけで～1.54/1.55両対応できる拡張メソッド
        /// </summary>
        /// <param name="body"> Maid.body0 </param>
        /// <returns></returns>
        public static SetIK _ikp(this TBody body)
        {
            if (UnityEngine.Time.frameCount != FrameCnt)
            {
                dicIKset.Clear();   // 毎フレームクリア
                FrameCnt = Time.frameCount;
            }
            else if (dicIKset.TryGetValue(body, out SetIK val))
            {
                return val;         // キャッシュを返す
            }

            var set = new SetIK(body);
            dicIKset.Add(body, set);

            return set;
        }

        class IKM
        {
            public TBody.IKCMO m_IkMgrR;
            public TBody.IKCMO m_IkMgrL;

            public IKM(TBody.IKCMO m_IkMgrR, TBody.IKCMO m_IkMgrL)
            {
                this.m_IkMgrR = m_IkMgrR;
                this.m_IkMgrL = m_IkMgrL;
            }
        }

        // 1.55以降用 ネストが深いのでキャッシュする
        static Type typeIKP = typeof(TBody).GetNestedType("IKParamData");
        static FieldInfo fim_IkMgr = typeIKP != null ? typeIKP.GetField("m_IkMgr", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) : null;
        static FieldInfo fiIkpointR = typeof(TBody).GetField("IkPointR", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        static FieldInfo fiIkpointL = typeof(TBody).GetField("IkPointL", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        static FieldInfo fiVechand = typeof(TBody.IKCMO).GetField("vechand", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        static FieldInfo fiKnee_old = typeof(TBody.IKCMO).GetField("knee_old", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        public static void owIkParam(this TBody tb, bool boR, Vector3 vechand, Vector3 knee_old)
        {
            object ikm;
            if (typeIKP != null)
            {
                var objL = boR ? fiIkpointR.GetValue(tb) : fiIkpointL.GetValue(tb);
                //if (objL != null)
                {
                    ikm = fim_IkMgr.GetValue(objL);
                }
            }
            else
            {
                if (boR)
                    ikm = typeof(TBody).InvokeMember("ikRightArm", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, tb, null);
                else
                    ikm = typeof(TBody).InvokeMember("ikLeftArm", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, tb, null);
            }

            fiVechand.SetValue(ikm, vechand);
            fiKnee_old.SetValue(ikm, knee_old);
        }


        public static void setR_vechand(this TBody tb, Vector3 v3)
        {
            if (IkXT.IsIkMgr159 || IkXT.IsIkCtrlO117)
            {
                //fiVechand.SetValue(tb.IKHandR.IKCmo, v3);
                fiVechand.SetValue(IkXT.GetIKCmo(tb, "右手"), v3);
                return;
            }

            object ikm;
            if (typeIKP != null)
            {
                var objL = fiIkpointR.GetValue(tb);
                //if (objL != null)
                {
                    ikm = fim_IkMgr.GetValue(objL);
                }
            }
            else
                ikm = typeof(TBody).InvokeMember("ikRightArm", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, tb, null);

            fiVechand.SetValue(ikm, v3);
        }

        public static void setL_vechand(this TBody tb, Vector3 v3)
        {
            if (IkXT.IsIkMgr159 || IkXT.IsIkCtrlO117)
            {
                //fiVechand.SetValue(tb.IKHandL.IKCmo, v3);
                fiVechand.SetValue(IkXT.GetIKCmo(tb, "左手"), v3);
                return;
            }

            object ikm;
            if (typeIKP != null)
            {
                var objL = fiIkpointL.GetValue(tb);
                //if (objL != null)
                {
                    ikm = fim_IkMgr.GetValue(objL);
                }
            }
            else
                ikm = typeof(TBody).InvokeMember("ikLeftArm", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, tb, null);

            fiVechand.SetValue(ikm, v3);
        }

        static MethodInfo miIkcmoProc1 = typeof(TBody.IKCMO).GetMethod("Proc", BindingFlags.Public | BindingFlags.Instance, null, 
            new Type[] { typeof(Transform), typeof(Transform), typeof(Transform), typeof(Vector3), typeof(Vector3) }, null);
        static MethodInfo miIkcmoProc = typeof(TBody.IKCMO).GetMethod("Proc", BindingFlags.Public | BindingFlags.Instance);
        public static void IKCmoProc(this TBody body, bool handRight, Transform bone, Vector3 tgt, Vector3 offset)
        {
            var cmo = IkXT.GetIKCmo(body, handRight ? "右手" : "左手");
            var ikctrl = IkXT.GetIkPoint(body, handRight ? "右手" : "左手");
            switch (miIkcmoProc.GetParameters().Length)
            {
                case 5:
                    miIkcmoProc.Invoke(cmo, new object[] { bone.parent.parent, bone.parent, bone, tgt, offset });
                    break;
                case 6:
                    miIkcmoProc.Invoke(cmo, new object[] { bone.parent.parent, bone.parent, bone, tgt, offset, ikctrl});
                    break;
            }
        }

        public static void InvokeNonPublicMethod(this object obj, string name, object[] values)
        {
            obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, values);
        }
    }
}



namespace BoneLink
{
    public class BoneLink
    {
        //メンバ変数
        static int maxInstId = 0;
        int linkId;　//0～21を想定

        Maid slave_last_ = null;
        Maid stopmaid_last_ = null;
        Maid stopman_last_ = null;

        //bool bk_eAnimeMan = true, bk_eAnimeMaid = true;
        FlgStop _flgStopped = FlgStop.none;

        //メイド、ストップカウント(ビットフラグ)、オリジナル値
        static Dictionary<Maid, StopAnimeState> stopAnimeMngr = new Dictionary<Maid, StopAnimeState>();

        enum FlgStop
        {
            none,
            maid2man,
            man2maid,
        }

        class StopAnimeState
        {
            public int linkIdBits = 0;
            public bool valBkup = true;
        }

        public BoneLink(int id)
        {
            linkId = id;

            if (maxInstId < id)
                maxInstId = id;

            if (maxInstId > 21) //cm3d2 最大アクティブメイド登録数
            {
                //5程度のはず…
                Console.WriteLine("BoneLinkのカウントが想定値を超えています :" + maxInstId);
            }
        }

        public void Reset()
        {
            stopAnime(null, null, FlgStop.none);

            slave_last_ = null;
            stopmaid_last_ = null;
            stopman_last_ = null;

            if (linkId == maxInstId) //最後のリンクでクリア
                stopAnimeMngr.Clear();
        }

        //linkIDは0～32まで指定可能(そこまで使わないけど)
        void stopAnime(Maid maid, Maid man, FlgStop flg)
        {
            if (stopmaid_last_ != maid)
            {
                stopAnime(stopmaid_last_, false, linkId);
                stopmaid_last_ = maid;

                _flgStopped = FlgStop.none;
            }

            if (stopman_last_ != maid)
            {
                stopAnime(stopman_last_, false, linkId);
                stopman_last_ = man;

                _flgStopped = FlgStop.none;
            }

            if (!maid || !man)
                return;

            if (flg == _flgStopped)
                return;

            //モーション停止
            switch (flg)
            {
                case FlgStop.maid2man:
                    stopAnime(man, true, linkId);
                    stopAnime(maid, false, linkId);
                    break;
                case FlgStop.man2maid:
                    stopAnime(man, false, linkId);
                    stopAnime(maid, true, linkId);
                    break;
                case FlgStop.none:
                    stopAnime(maid, false, linkId);
                    stopAnime(man, false, linkId);
                    break;
            }
            _flgStopped = flg;
        }

        static void stopAnime(Maid maid, bool stop, int linkId)
        {
            if (maid == null)
                return;

            if (stop)
                stopAnime(maid, linkId);
            else
                restoreAnime(maid, linkId);
        }

        static bool stopAnime(Maid maid, int linkId)
        {
            bool needBkup = false;
            if (!stopAnimeMngr.ContainsKey(maid))
            {
                stopAnimeMngr.Add(maid, new StopAnimeState());
            }
            if (stopAnimeMngr[maid].linkIdBits == 0)
            {
                needBkup = true;
            }

            //ID対応のビットをセットする
            stopAnimeMngr[maid].linkIdBits |= (1 << linkId);

            bool motion_stop = true;
            bool old = true;
            if (maid.body0.m_Bones != null && !string.IsNullOrEmpty(maid.body0.LastAnimeFN))
            {
                Animation animation = maid.body0.m_Bones.GetComponent<Animation>();
                if (animation != null)
                {
                    var anime_state = animation[maid.body0.LastAnimeFN.ToLower()];
                    if (anime_state != null && anime_state.length != 0f)
                    {
                        old = !anime_state.enabled;
                        if (motion_stop != !anime_state.enabled)
                        {
                            anime_state.enabled = !motion_stop;
                        }
                    }
                    else
                    {
                        //アニメ再生なし
                        old = true;
                    }
                }
            }

            if (needBkup)
            {
                stopAnimeMngr[maid].valBkup = old;
            }
            return old;
        }

        //戻り値：成功ならtrue
        static bool restoreAnime(Maid maid, int linkId)
        {
            bool motion_stop = false;
            bool old = true;

            if (!stopAnimeMngr.ContainsKey(maid))
                return false;

            //ID対応のビットを取り除く
            stopAnimeMngr[maid].linkIdBits &= (~(1 << linkId));

            if (stopAnimeMngr[maid].linkIdBits != 0)
                return true;    //残っていたら帰る
            //else
            {
                //バックアップの取得
                motion_stop = stopAnimeMngr[maid].valBkup;
                stopAnimeMngr.Remove(maid); //登録解除

                //呼ばれるタイミングが後処理のため一応チェック
                if (!maid || !maid.body0)
                    return false;
            }

            if (maid.body0.m_Bones != null && !string.IsNullOrEmpty(maid.body0.LastAnimeFN))
            {
                Animation animation = maid.body0.m_Bones.GetComponent<Animation>();
                if (animation != null)
                {
                    var anime_state = animation[maid.body0.LastAnimeFN.ToLower()];
                    if (anime_state != null && anime_state.length != 0f)
                    {
                        old = !anime_state.enabled;
                        if (motion_stop != !anime_state.enabled)
                        {
                            anime_state.enabled = !motion_stop;
                        }
                    }
                }
            }
            return true;
        }

#if false//単一リンク時
        static FlgStop _flgStopped = FlgStop.none;
        static bool bk_eAnimeMan = true, bk_eAnimeMaid = true;
        static void stopAnime(Maid maid, Maid man, FlgStop flg)
        {
            if (flg == _flgStopped)
                return;

            bool olds_maid = bk_eAnimeMaid, olds_man = bk_eAnimeMan;

            //モーション停止
            switch (flg)
            {
                case FlgStop.maid2man:
                    olds_man = stopAnime(man, true);
                    olds_maid = stopAnime(maid, false);
                    break;
                case FlgStop.man2maid:
                    olds_man = stopAnime(man, false);
                    olds_maid = stopAnime(maid, true);
                    break;
                case FlgStop.none:
                    stopAnime(maid, olds_maid);
                    stopAnime(man, olds_man);
                    break;
            }

            if (_flgStopped == FlgStop.none)
            {
                bk_eAnimeMaid = olds_maid;
                bk_eAnimeMan = olds_man;
            }
            _flgStopped = flg;
        }

        static bool stopAnime(Maid maid, bool motion_stop)
        {
            bool old = true;
            if (maid.body0.m_Bones != null && !string.IsNullOrEmpty(maid.body0.LastAnimeFN))
            {
                Animation animation = maid.body0.m_Bones.GetComponent<Animation>();
                if (animation != null)
                {
                    var anime_state = animation[maid.body0.LastAnimeFN.ToLower()];
                    if (anime_state != null && anime_state.length != 0f)
                    {
                        old = !anime_state.enabled;
                        if (motion_stop != !anime_state.enabled)
                        {
                            anime_state.enabled = !motion_stop;
                        }
                    }
                }
            }
            return old;
        }
#endif

        public static void ClearCache()
        {
            SearchObjCache.Clear();
        }

        static Dictionary<Transform, Dictionary<string, Transform>> SearchObjCache = new Dictionary<Transform, Dictionary<string, Transform>>();
        public static Transform SearchObjName(Transform t, string name, bool boSMPass)
        {
            Dictionary<string, Transform> cache;
            Transform tout;
            if (SearchObjCache.TryGetValue(t, out cache))
            {
                if (cache.TryGetValue(name, out tout))
                {
                    return tout;
                }
            }
            else
            {
                SearchObjCache.Add(t, new Dictionary<string, Transform>());
            }
            tout = CMT.SearchObjName(t, name, true);
            SearchObjCache[t][name] = tout;
            return tout;
        }

        //static Maid slave_last_ = null;
        //public static void Try(Maid master, Maid slave, bool enabled, bool org_pelvis)
        public void Try(Maid master, Maid slave, bool enabled, bool org_pelvis, bool org_clit, Vector3 v3StackOffset, XtMasterSlave.v3Offsets v3ofs, XtMasterSlave.MsLinkConfig mcfg)
        {
            if (!master || !slave)
                return;

            Transform gotr_mas = master.gameObject.transform;
            Transform gotr_slv = slave.gameObject.transform;

            if (slave_last_ != slave)
            {
                //スレイブが変更されたらフラグリセット
                //ここではチェックしない _flgStopped = FlgStop.none;
            }

            Maid man = master;
            Maid maid = slave;
            if (!master.boMAN)
            {
                man = slave;
                maid = master;
            }

            var dicMaidPose = new Dictionary<string, Quaternion>();
            var dicManPose = new Dictionary<string, Quaternion>();
            Transform maid_bone_tr = maid.body0.m_Bones.transform;
            Transform man_bone_tr = man.body0.m_Bones.transform;

            if (!enabled)
            {
                stopAnime(maid, man, FlgStop.none);
            }
            else if (!master.boMAN)
            {
                //if (_flgStopped != FlgStop.maid2man)
                {
                    stopAnime(maid, man, FlgStop.maid2man);
                }

                foreach (var bone in Defines.data.MaidBones)
                {
                    try
                    {
                        Quaternion q = SearchObjName(maid_bone_tr, bone, true).localRotation;
                        dicMaidPose[bone] = q;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("PoseGetMaid: " + e);
                    }
                }

                dicManPose = maid2man(dicMaidPose);

                foreach (var bone in Defines.data.ManBones)
                {
                    try
                    {
                        Quaternion q;
                        float[] q0;
                        if (dicManPose.TryGetValue(bone, out q))
                        {
                            SearchObjName(man_bone_tr, bone, true).localRotation = q;//(q.x, q.y, q.z, q.w);
                        }
                        else if (ipData.dicManInitPose.TryGetValue(bone, out q0))
                        {
                            SearchObjName(man_bone_tr, bone, true).localRotation = new Quaternion(q0[0], q0[1], q0[2], q0[3]);//(q.x, q.y, q.z, q.w);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("PoseInitMild: " + e);
                    }
                }

                //ボーン基準位置合わせ
                try
                {
                    Transform trManBip = SearchObjName(man_bone_tr, "ManBip", true);
                    Transform trBip01 = SearchObjName(maid_bone_tr, "Bip01", true);
                    trManBip.localPosition = trBip01.localPosition;

                    if (org_clit)
                    {
                        trManBip.position += (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position)
                            - (SearchObjName(man_bone_tr, "chinkoCenter", true).position - gotr_slv.position);

                        //trManBip.position += SearchObjName(man_bone_tr, "chinkoCenter", true).TransformDirection(v3StackOffset);
                        //trManBip.position += trBip01.TransformDirection(Quaternion.Inverse(Quaternion.Euler(0, -90, -90)) * v3StackOffset);
                        trManBip.position += trManBip.TransformDirection(v3StackOffset);

                        /*
                        if ( !maid.body0.goSlot[(int)TBody.SlotID.body].morph.GetAttachPoint("クリトリス", out Vector3 vout, out Quaternion qout) )
                        {
                            SearchObjName(man_bone_tr, "ManBip", true).position += (vout - gotr_mas.position)
                                                - (SearchObjName(man_bone_tr, "chinko1", true).position - gotr_slv.position);
                        }*/
                    }
                    else if (org_pelvis)
                    {
                        trManBip.position += (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position)
                            - (SearchObjName(man_bone_tr, "ManBip Pelvis", true).position - gotr_slv.position);

                        //trManBip.position += SearchObjName(man_bone_tr, "ManBip Pelvis", true).TransformDirection(v3StackOffset);
                        //trManBip.localPosition += Quaternion.Inverse(Quaternion.Euler(0, -90, -90)) * v3StackOffset;
                        trManBip.position += trManBip.TransformDirection(v3StackOffset);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("PoseInitMild2: " + e);
                }
            }
            else
            {
                //if (_flgStopped != FlgStop.man2maid)
                {
                    stopAnime(maid, man, FlgStop.man2maid);
                }

                foreach (var bone in Defines.data.ManBones)
                {
                    try
                    {
                        Quaternion q = SearchObjName(man_bone_tr, bone, true).localRotation;
                        dicManPose[bone] = q;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("PoseGetMan: " + e);
                    }
                }

                dicMaidPose = man2maid(dicManPose);

                foreach (var bone in Defines.data.MaidBones)
                {
                    try
                    {
                        if (bone == "Mune_R" || bone == "Mune_L") //胸ボーンbugfix v0011
                            continue;

                        /* test v5.0
                        if (mcfg.doIKTargetMHand && !mcfg.doIK159NewPointToDef && (bone.Contains("UpperArm") || bone.Contains("Forearm") || bone.Contains("Hand")))
                            continue;*/
#if DEBUG
                        /*// v5.0
                        if (Input.GetKey(KeyCode.Space) && mcfg.doIK159NewPointToDef && bone.Contains("Hand"))
                            continue;
                        if (Input.GetKey(KeyCode.LeftControl) && mcfg.doIK159NewPointToDef && (bone.Contains("UpperArm") || bone.Contains("Forearm") || bone.Contains("Hand")))
                            continue;*/
#endif
                        Quaternion q;
                        float[] q0;
                        if (dicMaidPose.TryGetValue(bone, out q))
                        {
                            SearchObjName(maid_bone_tr, bone, true).localRotation = q;//(q.x, q.y, q.z, q.w);
                        }
                        else if (ipData.dicMaidInitPose.TryGetValue(bone, out q0))
                        {
                            SearchObjName(maid_bone_tr, bone, true).localRotation = new Quaternion(q0[0], q0[1], q0[2], q0[3]);//(q.x, q.y, q.z, q.w);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("PoseInitMild: " + e);
                    }
                }

                //ボーン基準位置合わせ
                try
                {
                    Transform trManBip = SearchObjName(man_bone_tr, "ManBip", true);
                    Transform trBip01 = SearchObjName(maid_bone_tr, "Bip01", true);
                    trBip01.localPosition = trManBip.localPosition;

                    //忘れてたので追加
                    Transform trHuta = SearchObjName(maid_bone_tr, "_IK_hutanari", true);
                    if (trHuta && dicMaidPose.TryGetValue("_IK_hutanari", out Quaternion q)) //chinko1rot
                    {
#if DEBUG
                        if (Input.GetKey(KeyCode.H))
                        {
                            Console.WriteLine("{0}, prt={1}, c={2}, {3}", trHuta.name, trHuta.parent.name, trHuta.childCount, trHuta.hideFlags);
                            Console.WriteLine("x{0}, y{1}, z{2}, {3}", trHuta.localPosition.x, trHuta.localPosition.y, trHuta.localPosition.z, trHuta.localRotation);
                            Console.WriteLine("x{0}, y{1}, z{2}, w{3}", trHuta.localRotation.x, trHuta.localRotation.y, trHuta.localRotation.z, trHuta.localRotation.w);
                            foreach (Transform t in trHuta)
                            {
                                Console.WriteLine("├{0}, prt={1}, c={2}, {3}", t.name, t.parent.name, t.childCount, t.hideFlags);
                            }
                        }
#endif
                        trHuta.localRotation = SearchObjName(man_bone_tr, "chinkoCenter", true).localRotation * q * Quaternion.Euler(-90, 0, 0);
                    }

                    /*位置変更
                    // v0025 手の角度調整
                    if (v3ofs.v3HandROffsetRot != Vector3.zero)
                    {
                        Transform trh = SearchObjName(maid_bone_tr, "Bip01 R Hand", true);
                        trh.localRotation *= Quaternion.Euler(v3ofs.v3HandROffsetRot);
                    }
                    if (v3ofs.v3HandLOffsetRot != Vector3.zero)
                    {
                        Transform trh = SearchObjName(maid_bone_tr, "Bip01 L Hand", true);
                        trh.localRotation *= Quaternion.Euler(v3ofs.v3HandLOffsetRot);
                    }*/

#if true // ボーンを追加（別のプラグインにする -> 5.0一応有効にしてみる
                    else
                    {
                        if (!trHuta)
                        {
                            // _IK_hutanariが無ければ追加
                            GameObject go = UnityEngine.Object.Instantiate(Resources.Load("seed")) as GameObject;
                            go.layer = 10;
                            go.name = "_IK_hutanari";
                            go.transform.SetParent(SearchObjName(maid_bone_tr, "Bip01 Pelvis", true));
                            go.transform.localPosition= new Vector3(0.02106727f, 0.04978831f, -1.620466E-07f);
                            go.transform.localRotation = new Quaternion(0.5323541f, -0.4653954f, -0.4654013f, 0.5323607f);
                            GameObject go2 = UnityEngine.Object.Instantiate(Resources.Load("seed")) as GameObject;
                            go2.transform.SetParent(SearchObjName(maid.body0.m_Bones2.transform, "Bip01 Pelvis", true));
                            go2.transform.localPosition = new Vector3(0.02106727f, 0.04978831f, -1.620466E-07f);
                            go2.transform.localRotation = new Quaternion(0.5323541f, -0.4653954f, -0.4654013f, 0.5323607f);
#if DEBUG
                            Console.WriteLine("不足ボーンを追加：{0}, prt={1}, c={2}, {3}", go.name, go.transform.parent.name, go.transform.childCount, go.transform.hideFlags);
#endif
                            // ボーンを追加したのでキャッシュを破棄
                            ClearCache();
                        }
                    }
#endif
                    if (org_clit)
                    {
                        trBip01.position += (SearchObjName(man_bone_tr, "chinkoCenter", true).position - gotr_mas.position)
                                     - (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_slv.position);

                        //trBip01.position += SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).TransformDirection(v3StackOffset);
                        trBip01.position += trBip01.TransformDirection(v3StackOffset);
                    }
                    else if (org_pelvis)
                    {
#if DEBUG
                        //Console.WriteLine("{0}, {1}, {2}, {3}", SearchObjName(man_bone_tr, "ManBip Pelvis", true).position, gotr_mas.position,
                        //    SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position, gotr_slv.position);
#endif
                        trBip01.position += (SearchObjName(man_bone_tr, "ManBip Pelvis", true).position - gotr_mas.position)
                                     - (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_slv.position);

                        //trBip01.position += SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).TransformDirection(v3StackOffset);
                        trBip01.position += trBip01.TransformDirection(v3StackOffset);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("PoseInitMild2: " + e);
                }
            }

        }

        // 位置のみリンク用 ボーンアタッチ座標計算
        public static Vector3 TryPosSp(Maid master, Maid slave, bool enabled, bool org_pelvis, bool org_clit, bool PosSyncModeV2, string tgtBonename,
            Vector3 v3StackOffset, Vector3 v3StackOffsetRot)
        {
            if (!master || !slave || !enabled)
                return Vector3.zero;

            if (!org_pelvis && !org_clit && string.IsNullOrEmpty(tgtBonename))
                return slave.transform.rotation * v3StackOffset;

            if (string.IsNullOrEmpty(tgtBonename) && (org_clit || org_pelvis))
            {   // ギズモ回転角度に補正
                v3StackOffset = Quaternion.Euler(0, -90, -90) * Quaternion.Euler(v3StackOffsetRot) * v3StackOffset;
            }
            else
            {
                v3StackOffset = Quaternion.Euler(v3StackOffsetRot) * v3StackOffset;
            }

            Transform gotr_mas = master.gameObject.transform;
            Transform gotr_slv = slave.gameObject.transform;

            Maid man = master;
            Maid maid = slave;
            if (!master.boMAN)
            {
                man = slave;
                maid = master;
            }

            string tgtbone = tgtBonename;
            bool boTgtBone = !string.IsNullOrEmpty(tgtBonename);

            var dicMaidPose = new Dictionary<string, Quaternion>();
            var dicManPose = new Dictionary<string, Quaternion>();
            Transform maid_bone_tr = maid.body0.m_Bones.transform;
            Transform man_bone_tr = man.body0.m_Bones.transform;

            //Vector3 dpos = master.transform.position - slave.transform.position;
            Vector3 dpos = master.gameObject.transform.position - slave.gameObject.transform.position;
            Vector3 res = Vector3.zero;
            if (!master.boMAN)
            {
                //ボーン基準位置合わせ
                try
                {
                    if (!boTgtBone)
                    {
                        tgtbone = "Bip01 Pelvis";
                    }

                    //slave.transform.localPosition += SearchObjName(man_bone_tr, "ManBip", true).position - SearchObjName(maid_bone_tr, "Bip01", true).position - dpos;
                    //Bip01とpelvisは同座標っぽい…  res = SearchObjName(maid_bone_tr, "Bip01", true).position - SearchObjName(man_bone_tr, "ManBip", true).position - dpos;
                    //SearchObjName(man_bone_tr, "ManBip", true).localPosition = SearchObjName(maid_bone_tr, "Bip01", true).localPosition;
                    if (PosSyncModeV2)
                    {
                        res = (SearchObjName(maid_bone_tr, tgtbone, true).position - gotr_mas.position);
                    }
                    else if (org_clit)
                    {
                        res = (SearchObjName(maid_bone_tr, tgtbone, true).position - gotr_mas.position)
                            - (SearchObjName(man_bone_tr, "chinkoCenter", true).position - gotr_slv.position);
                    }
                    else if (org_pelvis)
                    {
                        res = (SearchObjName(maid_bone_tr, tgtbone, true).position - gotr_mas.position)
                            - (SearchObjName(man_bone_tr, "ManBip Pelvis", true).position - gotr_slv.position);
                    }
                    else if (boTgtBone)
                    {
                        res = (SearchObjName(maid_bone_tr, tgtbone, true).position - gotr_mas.position);
                    }

                    if (v3StackOffset != Vector3.zero)
                    {
                        if (!boTgtBone)
                            res += SearchObjName(maid_bone_tr, "Bip01", true).TransformDirection(v3StackOffset);
                        else
                            res += SearchObjName(maid_bone_tr, tgtbone, true).TransformDirection(v3StackOffset);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("TryPosSp: " + e);
                }
            }
            else
            {
                //ボーン基準位置合わせ
                try
                {
                    if (!boTgtBone)
                    {
                        if (org_clit)
                            tgtbone = "chinkoCenter";
                        else //if (org_pelvis)
                            tgtbone = "ManBip Pelvis";
                    }

                    //SearchObjName(maid_bone_tr, "Bip01", true).localPosition = SearchObjName(man_bone_tr, "ManBip", true).localPosition;
                    //Bip01とpelvisは同座標っぽい…res = SearchObjName(man_bone_tr, "ManBip", true).position - SearchObjName(maid_bone_tr, "Bip01", true).position - dpos;
                    if (PosSyncModeV2)
                    {
                        res = (SearchObjName(man_bone_tr, tgtbone, true).position - gotr_mas.position);
                    }
                    else if (org_clit || org_pelvis)
                    {
                        res = (SearchObjName(man_bone_tr, tgtbone, true).position - gotr_mas.position)
                            - (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_slv.position);
                    }
                    else if (boTgtBone)
                    {
                        res = (SearchObjName(man_bone_tr, tgtbone, true).position - gotr_mas.position);
                    }

                    if (v3StackOffset != Vector3.zero)
                    {
                        if (!boTgtBone)
                            res += SearchObjName(man_bone_tr, "ManBip", true).TransformDirection(v3StackOffset);
                        else
                            res += SearchObjName(man_bone_tr, tgtbone, true).TransformDirection(v3StackOffset);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("PoseInitMild2: " + e);
                }
            }
            return res;
        }

#if pre0025
        public static Vector3 TryPos(Maid master, Maid slave, bool enabled, bool org_pelvis, bool org_clit, bool PosSyncModeV2, Vector3 v3StackOffset)
        {
            if (!master || !slave || !enabled)
                return Vector3.zero;

            Transform gotr_mas = master.gameObject.transform;
            Transform gotr_slv = slave.gameObject.transform;
            
            Maid man = master;
            Maid maid = slave;
            if (!master.boMAN)
            {
                man = slave;
                maid = master;
            }

            var dicMaidPose = new Dictionary<string, Quaternion>();
            var dicManPose = new Dictionary<string, Quaternion>();
            Transform maid_bone_tr = maid.body0.m_Bones.transform;
            Transform man_bone_tr = man.body0.m_Bones.transform;

            //Vector3 dpos = master.transform.position - slave.transform.position;
            Vector3 dpos = master.gameObject.transform.position - slave.gameObject.transform.position;
            Vector3 res = Vector3.zero;
            if (!master.boMAN)
            {
                //ボーン基準位置合わせ
                try
                {
                    //slave.transform.localPosition += SearchObjName(man_bone_tr, "ManBip", true).position - SearchObjName(maid_bone_tr, "Bip01", true).position - dpos;
                    //Bip01とpelvisは同座標っぽい…  res = SearchObjName(maid_bone_tr, "Bip01", true).position - SearchObjName(man_bone_tr, "ManBip", true).position - dpos;
                    //SearchObjName(man_bone_tr, "ManBip", true).localPosition = SearchObjName(maid_bone_tr, "Bip01", true).localPosition;
                    if (org_clit)
                    {
                        //slave.transform.localPosition += (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position)
                        if (PosSyncModeV2)
                        {
                            res = (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position);
                        }
                        else
                        {
                            res = (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position)
                            - (SearchObjName(man_bone_tr, "chinkoCenter", true).position - gotr_slv.position);
                        }

                        //res += SearchObjName(man_bone_tr, "chinkoCenter", true).TransformDirection(v3StackOffset);
                        if (v3StackOffset != Vector3.zero)
                            res += SearchObjName(maid_bone_tr, "Bip01", true).TransformDirection(v3StackOffset);
                            //res += SearchObjName(man_bone_tr, "ManBip", true).TransformDirection(v3StackOffset);
                    }
                    else if (org_pelvis)
                    {
                        //slave.transform.localPosition += (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position)
                        if (PosSyncModeV2)
                        {
                            res = (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position);
                        }
                        else
                        {
                            res = (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position)
                            - (SearchObjName(man_bone_tr, "ManBip Pelvis", true).position - gotr_slv.position);
                        }

                        //res += SearchObjName(man_bone_tr, "ManBip Pelvis", true).TransformDirection(v3StackOffset);
                        if (v3StackOffset != Vector3.zero)
                            res += SearchObjName(maid_bone_tr, "Bip01", true).TransformDirection(v3StackOffset);
                            //res += SearchObjName(man_bone_tr, "ManBip", true).TransformDirection(v3StackOffset);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("PoseInitMild2: " + e);
                }
            }
            else
            {
                //ボーン基準位置合わせ
                try
                {
                    //SearchObjName(maid_bone_tr, "Bip01", true).localPosition = SearchObjName(man_bone_tr, "ManBip", true).localPosition;
                    //Bip01とpelvisは同座標っぽい…res = SearchObjName(man_bone_tr, "ManBip", true).position - SearchObjName(maid_bone_tr, "Bip01", true).position - dpos;
                    if (org_clit)
                    {
                        //slave.transform.localPosition += (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_mas.position)
                        if (PosSyncModeV2)
                        {
                            res = (SearchObjName(man_bone_tr, "chinkoCenter", true).position - gotr_mas.position);
                        }
                        else
                        {
                            res = (SearchObjName(man_bone_tr, "chinkoCenter", true).position - gotr_mas.position)
                            - (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_slv.position);
                        }

                        //res += SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).TransformDirection(v3StackOffset);
                        //res += SearchObjName(maid_bone_tr, "Bip01", true).TransformDirection(v3StackOffset);
                        if (v3StackOffset != Vector3.zero)
                            res += SearchObjName(man_bone_tr, "ManBip", true).TransformDirection(v3StackOffset);
                    }
                    else if (org_pelvis)
                    {
#if DEBUG
                        //Console.WriteLine("{0}, {1}, {2}, {3}", SearchObjName(man_bone_tr, "ManBip Pelvis", true).position, gotr_mas.position,
                        //    SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position, gotr_slv.position);
#endif
                        //slave.transform.position += (SearchObjName(man_bone_tr, "ManBip Pelvis", true).position - gotr_mas.position)
                        if (PosSyncModeV2)
                        {
                            res = (SearchObjName(man_bone_tr, "ManBip Pelvis", true).position - gotr_mas.position);
                        }
                        else
                        {
                            res = (SearchObjName(man_bone_tr, "ManBip Pelvis", true).position - gotr_mas.position)
                            - (SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).position - gotr_slv.position);
                        }

                        //res += SearchObjName(maid_bone_tr, "Bip01 Pelvis", true).TransformDirection(v3StackOffset);
                        //res += SearchObjName(maid_bone_tr, "Bip01", true).TransformDirection(v3StackOffset);
                        if (v3StackOffset != Vector3.zero)
                            res += SearchObjName(man_bone_tr, "ManBip", true).TransformDirection(v3StackOffset);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("PoseInitMild2: " + e);
                }
            }
            return res;
        }
#endif

        static Dictionary<string, Quaternion> maid2man(Dictionary<string, Quaternion> dicBones)
        {
            var dest = new Dictionary<string, Quaternion>();
            foreach (var bn in dicBones)
            {
                string name = bn.Key;
                if (!name.Contains("Bip01")) name = ""; //v0020 不要なボーンを除く

                //# ボーン/ウェイト名を Maid → Man
                //name = Regex.Replace(name, @"Mune_.*", @"ManBip Spine2");
                //name = Regex.Replace(name, @"Uppertwist.*_([rRlL]).*$", @"ManBip $1 UpperArm");
                //name = Regex.Replace(name, @"Kata_([rRlL]).*$", @"ManBip $1 UpperArm");

                //name = Regex.Replace(name, @"Foretwist.*_([rRlL]).*$", @"ManBip $1 Forearm");
                //name = Regex.Replace(name, @"Hip_([rRlL]).*$", @"ManBip $1 Thigh");

                //name = Regex.Replace(name, @"momotwist.*_([rRlL]).*$", @"ManBip $1 Thigh");
                //name = Regex.Replace(name, @"momoniku.*_([rRlL]).*$", @"ManBip $1 Thigh");

                name = Regex.Replace(name, @"Bip01 ([rRlL]) Toe1$", @"ManBip $1 Toe0");
                //name = Regex.Replace(name, @"(Bip01.*) Toe11$", @"$1 Toe0");
                //name = Regex.Replace(name, @"(Bip01.*) Toe01$", @"$1 Toe0");

                name = Regex.Replace(name, @"Bip01 ([rRlL]) Toe2$", @"ManBip $1 Toe1");
                //name = Regex.Replace(name, @"(Bip01.*) Toe21$", @"$1 Toe1");

                if (name.Contains(" Toe") && name.Contains("Bip01 ")) name = ""; //v0020 不要なボーンを除く

                name = Regex.Replace(name, @"Spine0a", @"Spine1");
                name = Regex.Replace(name, @"Spine1a", @"Spine2");
                //name = Regex.Replace(name, @"Bip01", @"ManBip");
                name = name.Replace(@"Bip01", @"ManBip");

                if (name != "")
                    dest[name] = bn.Value;
            }
            return dest;
        }

        static Dictionary<string, Quaternion> man2maid(Dictionary<string, Quaternion> dicBones)
        {
            var dest = new Dictionary<string, Quaternion>();
            foreach (var bn in dicBones)
            {
                string name = bn.Key;

                //# ボーン/ウェイト名を Man → Maid
                //name = Regex.Replace(name, @"chinko.*$", @"Bip01 Pelvis");
                //name = Regex.Replace(name, @"tamabukuro$", @"Bip01 Pelvis");
                name = Regex.Replace(name, @"chinko1$", @"_IK_hutanari");
                name = Regex.Replace(name, @"Spine1$", @"Spine0a");
                name = Regex.Replace(name, @"Spine2$", @"Spine1a");
                //name = Regex.Replace(name, @"(ManBip)(.*)$", @"Bip01$2");
                name = Regex.Replace(name, @"ManBip ([rRlL]) Toe1$", @"Bip01 $1 Toe2");
                name = Regex.Replace(name, @"ManBip ([rRlL]) Toe0$", @"Bip01 $1 Toe1");
                if (name.Contains(" Toe") && name.Contains("ManBip")) name = ""; //v0020 不要なボーンを除く

                name = name.Replace(@"ManBip", @"Bip01");
                if (name != "")
                    dest[name] = bn.Value;

                //足指のボーンを複製
                float[] q0 = new float[4];
                if ((name == "Bip01 R Toe1") && ipData.dicMaidInitPose.TryGetValue("Bip01 R Toe0", out q0))
                {
                    dest["Bip01 R Toe0"] = Quaternion.Lerp(new Quaternion(q0[0], q0[1], q0[2], q0[3]), bn.Value, 0.5f);
                }
                if ((name == "Bip01 L Toe1") && ipData.dicMaidInitPose.TryGetValue("Bip01 L Toe0", out q0))
                {
                    dest["Bip01 L Toe0"] = Quaternion.Lerp(new Quaternion(q0[0], q0[1], q0[2], q0[3]), bn.Value, 0.5f);
                }

            }
            return dest;
        }

        //初期ポーズデータ
        static InitPose ipData = new InitPose(true);
        class InitPose
        {
            public InitPose()
            {
            }

            public InitPose(bool load)
            {
                if (load)
                {
                    Load();
                }
            }

            public void Load()
            {
                var ipData = JsonFx.Json.JsonReader.Deserialize<InitPose>(@"{""dicMaidInitPose"":{""Bip01"":[-0.500000358,0.499999642,0.499999642,0.500000358],""Bip01 Head"":[5.10125346E-08,-1.01156218E-07,0.08458735,0.9964161],""Bip01 Neck"":[-4.095299E-14,4.27135149E-07,-0.154000267,0.988070846],""Bip01 Spine"":[-0.5224006,0.476547748,0.4765491,0.522399068],""Bip01 Spine0a"":[-7.289694E-08,-2.48852643E-07,0.08234593,0.996603847],""Bip01 Spine1"":[2.84859958E-09,-2.56992337E-07,0.05952706,0.9982267],""Bip01 Spine1a"":[2.54004089E-08,5.56248E-09,-0.0262033567,0.9996567],""Mune_R"":[-0.130401641,-0.116427459,-0.6552144,0.734938145],""Bip01 R Clavicle"":[0.707011163,-0.0116283,0.7070017,-0.0121913319],""Bip01 R UpperArm"":[-0.127119452,0.347238928,0.280861944,0.8856536],""Bip01 R Forearm"":[3.04732928E-09,2.669005E-08,0.456733763,0.889603436],""Bip01 R Hand"":[0.9619744,0.035165213,0.0032886113,0.270846665],""Mune_L"":[0.735012531,-0.655130446,-0.116414241,-0.130416155],""Bip01 L Clavicle"":[0.707011163,-0.0116263488,-0.707001746,0.0121893613],""Bip01 L UpperArm"":[0.1271193,-0.347239166,0.280861765,0.8856536],""Bip01 L Forearm"":[3.40293149E-09,-6.628062E-09,0.456733733,0.889603436],""Bip01 L Hand"":[0.9619744,0.03516523,-0.00328857778,-0.270846665],""Bip01 R Thigh"":[-0.5271557,0.825693846,-0.131378531,-0.151908576],""Bip01 R Calf"":[1.5333729E-08,7.856927E-09,0.456016839,0.8899712],""Bip01 R Foot"":[0.0429165475,-0.0405659154,0.0193312634,0.998067558],""Bip01 L Thigh"":[-0.5271557,0.825694263,0.131378159,0.151906654],""Bip01 L Calf"":[-1.57551006E-09,-9.178994E-09,0.456016868,0.889971137],""Bip01 L Foot"":[-0.042916514,0.0405658446,0.0193312317,0.9980676],""Bip01 R Finger0"":[-0.334180564,0.285874128,-0.035913486,0.8973904],""Bip01 R Finger01"":[-1.55828523E-08,1.41867531E-08,0.0468453579,0.9989022],""Bip01 R Finger02"":[-2.98023188E-08,7.450581E-09,7.450581E-09,1],""Bip01 R Finger1"":[0.00194044434,0.0588727221,0.0261753127,0.9979204],""Bip01 R Finger11"":[3.7252903E-09,7.450581E-09,-2.32830644E-09,1],""Bip01 R Finger12"":[3.7252903E-09,7.450581E-09,-2.32830644E-09,1],""Bip01 R Finger2"":[0.000407734333,0.002978354,0.00321666966,0.999990344],""Bip01 R Finger21"":[3.72315334E-09,1.26156932E-10,0.0338649936,0.9994264],""Bip01 R Finger22"":[3.7252903E-09,-1.49011594E-08,5.587935E-09,1],""Bip01 R Finger3"":[-0.000201299859,-0.07369577,0.00395552255,0.9972729],""Bip01 R Finger31"":[4.000103E-09,-1.48297543E-08,0.0184850525,0.9998292],""Bip01 R Finger32"":[-7.71108E-09,2.97359826E-08,0.008750517,0.999961734],""Bip01 R Finger4"":[-0.00130899251,-0.1456734,0.01685153,0.9891884],""Bip01 R Finger41"":[-1.86264493E-09,1.49011594E-08,2.7755569E-17,1],""Bip01 R Finger42"":[-2.44913223E-09,4.46460575E-08,0.03396485,0.999423],""Bip01 L Finger0"":[0.334180564,-0.285874128,-0.0359135047,0.8973904],""Bip01 L Finger01"":[1.62809037E-08,-2.90715541E-08,0.04684536,0.9989022],""Bip01 L Finger02"":[-2.220446E-16,2.98023224E-08,7.450581E-09,1],""Bip01 L Finger1"":[-0.001940444,-0.0588727035,0.0261753183,0.9979204],""Bip01 L Finger11"":[0,0,-4.19095159E-09,1],""Bip01 L Finger12"":[0,0,-4.19095159E-09,1],""Bip01 L Finger2"":[-0.0004077344,-0.002978339,0.00321665849,0.999990344],""Bip01 L Finger21"":[-3.21852567E-09,-1.501877E-08,0.0338649936,0.9994264],""Bip01 L Finger22"":[0,0,7.45057971E-09,1],""Bip01 L Finger3"":[0.0002012996,0.07369578,0.00395552441,0.9972729],""Bip01 L Finger31"":[5.862429E-09,-1.47953205E-08,0.0184850488,0.9998292],""Bip01 L Finger32"":[1.3039303E-10,-1.49005892E-08,0.008750529,0.999961734],""Bip01 L Finger4"":[0.00130899344,0.1456734,0.016851522,0.9891884],""Bip01 L Finger41"":[0,0,-1.11758709E-08,1],""Bip01 L Finger42"":[-1.355455E-09,-1.49558268E-08,0.03396483,0.999423],""Bip01 R Toe2"":[-0.0308998842,-0.0107883792,-0.5952882,0.8028455],""Bip01 R Toe21"":[-4.386279E-09,1.94202219E-08,0.0339062028,0.999425054],""Bip01 R Toe1"":[0.0493279733,0.0129661234,-0.617873847,0.784621358],""Bip01 R Toe11"":[-1.86264537E-09,1.11758718E-08,2.0816685E-17,1],""Bip01 R Toe0"":[0.00419396255,-0.00206413679,-0.6421687,0.76654917],""Bip01 R Toe01"":[7.960058E-09,-5.8863705E-09,0.0819981843,0.9966325],""Bip01 L Toe2"":[0.0308998823,0.010788383,-0.595288157,0.8028455],""Bip01 L Toe21"":[7.5094535E-09,-1.60895353E-09,0.03390623,0.999425054],""Bip01 L Toe1"":[-0.0493279621,-0.0129661178,-0.6178737,0.784621358],""Bip01 L Toe11"":[2.7939675E-09,2.25845724E-08,-6.310056E-17,1],""Bip01 L Toe0"":[-0.00419394253,0.00206416636,-0.642168641,0.7665492],""Bip01 L Toe01"":[-4.476413E-09,8.97639651E-09,0.08199818,0.9966325],""Bip01 Pelvis"":[-0.5323567,0.4653983,0.465398431,0.53235805],""Bip01 Footsteps"":[0,0,-0.707106352,0.7071073]},""dicManInitPose"":{""ManBip"":[-0.500000358,0.499999642,0.499999642,0.500000358],""ManBip Head"":[4.17850264E-14,2.48618228E-08,-0.008963051,0.9999598],""ManBip Neck"":[5.575591E-14,4.20879815E-07,-0.151744932,0.9884197],""ManBip Spine"":[0.5000007,-0.4999993,-0.5000007,-0.4999993],""ManBip Spine1"":[-6.12429131E-15,-1.07341428E-07,0.038701117,0.9992508],""ManBip Spine2"":[-3.8841E-14,-1.31670959E-07,0.0474729538,0.9988725],""ManBip R Clavicle"":[0.6467047,0.0002564384,0.7627404,-0.000304589048],""ManBip R UpperArm"":[-0.103138067,0.3984069,-0.0506248623,0.9099844],""ManBip R Forearm"":[-1.96698768E-10,-2.98344E-08,0.0402369276,0.9991902],""ManBip R Hand"":[0.706165135,0.0305421874,0.0123255923,0.707280755],""ManBip L Clavicle"":[-0.6467047,-0.000258554064,0.7627404,-0.000302795466],""ManBip L UpperArm"":[0.103138067,-0.398406923,-0.05062485,0.9099844],""ManBip L Forearm"":[-3.23052762E-09,7.443611E-08,0.04023694,0.9991902],""ManBip L Hand"":[-0.706165135,-0.0305421837,0.01232559,0.707280755],""ManBip R Thigh"":[0.00263441727,0.9982243,-0.00517705875,-0.0592836924],""ManBip R Calf"":[2.48443544E-10,7.454624E-09,0.01352519,0.999908566],""ManBip R Foot"":[0.0313028,-0.0597688779,-0.0139128156,0.997624338],""ManBip L Thigh"":[0.002634342,0.9982244,0.00517567061,0.0592822172],""ManBip L Calf"":[1.16404685E-10,1.57453933E-12,0.0135251889,0.999908566],""ManBip L Foot"":[-0.03130279,0.0597687848,-0.0139128147,0.997624338],""ManBip R Finger0"":[-0.4217479,0.283663929,-0.0152533976,0.861063838],""ManBip R Finger01"":[2.0470706E-08,-8.678663E-10,-0.0423574746,0.999102533],""ManBip R Finger02"":[2.04890949E-08,-3.05311252E-16,-1.49011594E-08,1],""ManBip R Finger1"":[0.0008758125,0.0596490949,0.008028525,0.998186767],""ManBip R Finger11"":[-3.72529E-09,-1.49011612E-08,-1.49011612E-08,1],""ManBip R Finger12"":[-3.72529E-09,-1.49011612E-08,-1.49011612E-08,1],""ManBip R Finger2"":[0.000398111,0.0156074585,6.18502054E-06,0.999878168],""ManBip R Finger21"":[3.7252903E-09,-7.450581E-09,-1.49011612E-08,1],""ManBip R Finger22"":[3.7252903E-09,-7.450581E-09,-1.49011612E-08,1],""ManBip R Finger3"":[0.000398110424,-0.0156222573,-6.223847E-06,0.9998779],""ManBip R Finger31"":[0,1.86264515E-09,0,1],""ManBip R Finger32"":[0,1.86264515E-09,0,1],""ManBip R Finger4"":[0.0003967817,-0.08307511,-3.30893345E-05,0.9965432],""ManBip R Finger41"":[-1.86264515E-09,0,0,1],""ManBip R Finger42"":[-1.86264515E-09,0,0,1],""ManBip L Finger0"":[0.421747833,-0.2836639,-0.0152533874,0.861063838],""ManBip L Finger01"":[-1.33424019E-08,-6.891615E-09,-0.0423574746,0.999102533],""ManBip L Finger02"":[-1.11758709E-08,3.7252903E-09,4.16333634E-17,1],""ManBip L Finger1"":[-0.000875810045,-0.0596490875,0.00802857,0.998186767],""ManBip L Finger11"":[3.7252903E-09,0,0,1],""ManBip L Finger12"":[3.7252903E-09,0,0,1],""ManBip L Finger2"":[-0.0003981141,-0.0156074539,6.22968264E-06,0.999878168],""ManBip L Finger21"":[-1.86264537E-09,3.7252903E-09,6.938895E-18,1],""ManBip L Finger22"":[-1.86264537E-09,3.7252903E-09,6.938895E-18,1],""ManBip L Finger3"":[-0.0003981144,0.0156222573,-6.208881E-06,0.9998779],""ManBip L Finger31"":[9.313226E-10,2.79396772E-09,-1.49011612E-08,1],""ManBip L Finger32"":[9.313226E-10,2.79396772E-09,-1.49011612E-08,1],""ManBip L Finger4"":[-0.0003967855,0.0830751061,-3.30890143E-05,0.9965432],""ManBip L Finger41"":[0,0,0,1],""ManBip L Finger42"":[0,0,0,1],""ManBip R Toe1"":[1.808849E-08,-1.28201343E-08,-0.7071068,0.7071068],""ManBip R Toe0"":[1.808849E-08,-1.28201343E-08,-0.7071068,0.7071068],""ManBip L Toe1"":[-1.54543134E-08,1.54543134E-08,-0.7071068,0.7071068],""ManBip L Toe0"":[-1.54543134E-08,1.54543134E-08,-0.7071068,0.7071068],""ManBip Pelvis"":[-0.499999642,0.500000358,0.499999642,0.500000358],""chinkoCenter"":[0.7071068,-0.7071068,5.577444E-07,1.50745143E-06],""chinko1"":[-2.71605011E-14,-8.23369151E-14,0.336606741,0.9416453],""chinko2"":[-1.00942432E-09,-4.852763E-08,-0.0207966343,0.999783754],""tamabukuro"":[-3.374307E-17,-4.86970168E-15,0.6788698,0.7342587],""ManBip Footsteps"":[0,0,-0.707106352,0.7071073]},""isDataInit"":true}") as InitPose;
                this.dicMaidInitPose = ipData.dicMaidInitPose;
                this.dicManInitPose = ipData.dicManInitPose;
            }

            public InitPose(Dictionary<string, float[]> dicMaidInitPose, Dictionary<string, float[]> dicManInitPose)
            {
                this.dicMaidInitPose = dicMaidInitPose;
                this.dicManInitPose = dicManInitPose;
            }

            public Dictionary<string, float[]> dicMaidInitPose = new Dictionary<string, float[]>();
            public Dictionary<string, float[]> dicManInitPose = new Dictionary<string, float[]>();

            public bool isDataInit = false;
        }
    }
}

namespace XtHandMgr
{
    // 公式のArmFinger : FingerBlend.BaseFingerより改造 
    public class BlendMgr
    {
        bool isR = true;
        public Maid maid;
        public float fBlend;
        public float fOpen;
        public float fGrip;
        IKManager.BoneType[][] fingers = new IKManager.BoneType[][] { };
        Dictionary<IKManager.BoneType, KeyValuePair<IKManager.BoneSetType, GameObject>> fingersBoneDic;
        public bool animOn = false;
        //float animVal = 0;
        public float animRange = 0;
        public float animSpeed = 0;
        public int retry = 100;
        bool needInit = true;

        public void Apply(bool bo)
        {
            if (maid && bo)
            {
                try
                {
                    if (!maid.body0.m_Bones)
                        return;

                    if (needInit)
                        init(maid, isR);

                    Apply(this.fingers);
                    retry = 100;
                }
                catch (NullReferenceException ex)
                {
                    if (retry > 0)
                    {
                        needInit = true;
                        UnityEngine.Debug.LogWarningFormat("XtMS: 指のブレンドマネージャを再起動(リトライ=残{0})", retry);
                        
                        retry--;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(ex);
                    }
                }
            }
        }

        public void Apply(IKManager.BoneType[][] fingers)
        {
            var valO = Mathf.Clamp01(fOpen);
            var valG = Mathf.Clamp01(fGrip);
            if (animOn)
            {
                //valG = Mathf.SmoothStep(Mathf.Clamp01(valG), Mathf.Clamp01(valG + animRange), Mathf.PingPong(Time.time * animSpeed, 1f));

                // v5.0 非対称
                var time = Time.time * animSpeed % 3f;
                if (time > 1f)
                {
                    time = (time - 1f) / 2f + 1f; 
                }
                valG = Mathf.SmoothStep(Mathf.Clamp01(valG), Mathf.Clamp01(valG + animRange), Mathf.PingPong(time, 1f));
            }

            if (FingerBlend.open_dic == null || FingerBlend.close_dic == null || FingerBlend.fist_dic == null)
            {
                // FingerBlend初期化まだ
                var gofb = GameObject.Find("xtMsInit_FingerBlend");
                if (!gofb)
                {
                    Console.WriteLine("XtMs: FingerBlendの初期化");
                    var go = new GameObject("xtMsInit_FingerBlend");
                    go.AddComponent<FingerBlend>();
                }
                else
                {
                    GameObject.DestroyImmediate(gofb);
                    Console.WriteLine("XtMs: FingerBlendの再初期化");
                    var go = new GameObject("xtMsInit_FingerBlend");
                    go.AddComponent<FingerBlend>();
                }
            }

            for (int i = 0; i < fingers.Length; i++)
            {
                foreach (IKManager.BoneType boneType in this.fingers[i])
                {
                    var trBone = getBone(boneType).transform;

                    trBone.localRotation
                        = Quaternion.Lerp(
                            trBone.localRotation,
                            Quaternion.Lerp(
                                Quaternion.Lerp(
                                    FingerBlend.close_dic[boneType],
                                    FingerBlend.open_dic[boneType],
                                    valO),
                                FingerBlend.fist_dic[boneType],
                                valG),
                            Mathf.Clamp01(fBlend));
                }
            }
        }

        private void init(Maid maid, bool isR)
        {
            this.isR = isR;
            this.maid = maid;
            this.fingers = new IKManager.BoneType[5][];
            for (int i = 0; i < 5; i++)
            {
                this.fingers[i] = new IKManager.BoneType[3];
                for (int j = 0; j < 3; j++)
                {
                    if (isR)
                        this.fingers[i][j] = i * this.fingers[i].Length + IKManager.BoneType.Finger0_Root_R + j;
                    else
                        this.fingers[i][j] = i * this.fingers[i].Length + IKManager.BoneType.Finger0_Root_L + j;
                }
            }
            setDic(maid);
            needInit = false;
        }

        public BlendMgr(Maid maid, bool isR)
        {
            init(maid, isR);
        }

        GameObject getBone(IKManager.BoneType type)
        {
            return fingersBoneDic[type].Value;
        }

        void setDic(Maid maid)
        {
            //IKManagerより 旧カスメでも使えるように切り出し
            fingersBoneDic = new Dictionary<IKManager.BoneType, KeyValuePair<IKManager.BoneSetType, GameObject>>
            {
                {
                    IKManager.BoneType.Finger0_Root_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger0").gameObject)
                },
                {
                    IKManager.BoneType.Finger0_0_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger01").gameObject)
                },
                {
                    IKManager.BoneType.Finger0_1_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger02").gameObject)
                },
                {
                    IKManager.BoneType.Finger1_Root_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger1").gameObject)
                },
                {
                    IKManager.BoneType.Finger1_0_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger11").gameObject)
                },
                {
                    IKManager.BoneType.Finger1_1_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger12").gameObject)
                },
                {
                    IKManager.BoneType.Finger2_Root_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger2").gameObject)
                },
                {
                    IKManager.BoneType.Finger2_0_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger21").gameObject)
                },
                {
                    IKManager.BoneType.Finger2_1_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger22").gameObject)
                },
                {
                    IKManager.BoneType.Finger3_Root_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger3").gameObject)
                },
                {
                    IKManager.BoneType.Finger3_0_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger31").gameObject)
                },
                {
                    IKManager.BoneType.Finger3_1_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger32").gameObject)
                },
                {
                    IKManager.BoneType.Finger4_Root_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger4").gameObject)
                },
                {
                    IKManager.BoneType.Finger4_0_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger41").gameObject)
                },
                {
                    IKManager.BoneType.Finger4_1_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightArmFinger, maid.body0.GetBone("Bip01 R Finger42").gameObject)
                },
                {
                    IKManager.BoneType.Finger0_Root_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger0").gameObject)
                },
                {
                    IKManager.BoneType.Finger0_0_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger01").gameObject)
                },
                {
                    IKManager.BoneType.Finger0_1_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger02").gameObject)
                },
                {
                    IKManager.BoneType.Finger1_Root_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger1").gameObject)
                },
                {
                    IKManager.BoneType.Finger1_0_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger11").gameObject)
                },
                {
                    IKManager.BoneType.Finger1_1_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger12").gameObject)
                },
                {
                    IKManager.BoneType.Finger2_Root_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger2").gameObject)
                },
                {
                    IKManager.BoneType.Finger2_0_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger21").gameObject)
                },
                {
                    IKManager.BoneType.Finger2_1_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger22").gameObject)
                },
                {
                    IKManager.BoneType.Finger3_Root_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger3").gameObject)
                },
                {
                    IKManager.BoneType.Finger3_0_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger31").gameObject)
                },
                {
                    IKManager.BoneType.Finger3_1_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger32").gameObject)
                },
                {
                    IKManager.BoneType.Finger4_Root_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger4").gameObject)
                },
                {
                    IKManager.BoneType.Finger4_0_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger41").gameObject)
                },
                {
                    IKManager.BoneType.Finger4_1_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftArmFinger, maid.body0.GetBone("Bip01 L Finger42").gameObject)
                },
                {
                    IKManager.BoneType.Toe0_Root_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightLegFinger, maid.body0.GetBone("Bip01 R Toe2").gameObject)
                },
                {
                    IKManager.BoneType.Toe0_0_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightLegFinger, maid.body0.GetBone("Bip01 R Toe21").gameObject)
                },
                {
                    IKManager.BoneType.Toe1_Root_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightLegFinger, maid.body0.GetBone("Bip01 R Toe1").gameObject)
                },
                {
                    IKManager.BoneType.Toe1_0_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightLegFinger, maid.body0.GetBone("Bip01 R Toe11").gameObject)
                },
                {
                    IKManager.BoneType.Toe2_Root_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightLegFinger, maid.body0.GetBone("Bip01 R Toe0").gameObject)
                },
                {
                    IKManager.BoneType.Toe2_0_R,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightLegFinger, maid.body0.GetBone("Bip01 R Toe01").gameObject)
                },
                {
                    IKManager.BoneType.Toe0_Root_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.RightLegFinger, maid.body0.GetBone("Bip01 L Toe2").gameObject)
                },
                {
                    IKManager.BoneType.Toe0_0_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftLegFinger, maid.body0.GetBone("Bip01 L Toe21").gameObject)
                },
                {
                    IKManager.BoneType.Toe1_Root_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftLegFinger, maid.body0.GetBone("Bip01 L Toe1").gameObject)
                },
                {
                    IKManager.BoneType.Toe1_0_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftLegFinger, maid.body0.GetBone("Bip01 L Toe11").gameObject)
                },
                {
                    IKManager.BoneType.Toe2_Root_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftLegFinger, maid.body0.GetBone("Bip01 L Toe0").gameObject)
                },
                {
                    IKManager.BoneType.Toe2_0_L,
                    new KeyValuePair<IKManager.BoneSetType, GameObject>(IKManager.BoneSetType.LeftLegFinger, maid.body0.GetBone("Bip01 L Toe01").gameObject)
                }
            };

        }


    }
}


namespace Defines
{
    class data
    {
        public readonly static string[] MaidBones =
        {
            "Bip01",
            "Bip01 Head",
            "Bip01 Neck",
            "Bip01 Spine",
            "Bip01 Spine0a",
            "Bip01 Spine1",
            "Bip01 Spine1a",
            "Mune_R",
            "Bip01 R Clavicle",
            "Bip01 R UpperArm",
            "Bip01 R Forearm",
            "Bip01 R Hand",
            "Mune_L",
            "Bip01 L Clavicle",
            "Bip01 L UpperArm",
            "Bip01 L Forearm",
            "Bip01 L Hand",
            "Bip01 R Thigh",
            "Bip01 R Calf",
            "Bip01 R Foot",
            "Bip01 L Thigh",
            "Bip01 L Calf",
            "Bip01 L Foot",
            "Bip01 R Finger0",
            "Bip01 R Finger01",
            "Bip01 R Finger02",
            "Bip01 R Finger1",
            "Bip01 R Finger11",
            "Bip01 R Finger12",
            "Bip01 R Finger2",
            "Bip01 R Finger21",
            "Bip01 R Finger22",
            "Bip01 R Finger3",
            "Bip01 R Finger31",
            "Bip01 R Finger32",
            "Bip01 R Finger4",
            "Bip01 R Finger41",
            "Bip01 R Finger42",
            "Bip01 L Finger0",
            "Bip01 L Finger01",
            "Bip01 L Finger02",
            "Bip01 L Finger1",
            "Bip01 L Finger11",
            "Bip01 L Finger12",
            "Bip01 L Finger2",
            "Bip01 L Finger21",
            "Bip01 L Finger22",
            "Bip01 L Finger3",
            "Bip01 L Finger31",
            "Bip01 L Finger32",
            "Bip01 L Finger4",
            "Bip01 L Finger41",
            "Bip01 L Finger42",
            "Bip01 R Toe2",
            "Bip01 R Toe21",
            "Bip01 R Toe1",
            "Bip01 R Toe11",
            "Bip01 R Toe0",
            "Bip01 R Toe01",
            "Bip01 L Toe2",
            "Bip01 L Toe21",
            "Bip01 L Toe1",
            "Bip01 L Toe11",
            "Bip01 L Toe0",
            "Bip01 L Toe01",
            "Bip01 Pelvis",
            "Bip01 Footsteps",
        };

        public readonly static string[] ManBones =
        {
            "ManBip",
            "ManBip Head",
            "ManBip Neck",
            "ManBip Spine",
            "ManBip Spine1",
            "ManBip Spine2",
            "ManBip R Clavicle",
            "ManBip R UpperArm",
            "ManBip R Forearm",
            "ManBip R Hand",
            "ManBip L Clavicle",
            "ManBip L UpperArm",
            "ManBip L Forearm",
            "ManBip L Hand",
            "ManBip R Thigh",
            "ManBip R Calf",
            "ManBip R Foot",
            "ManBip L Thigh",
            "ManBip L Calf",
            "ManBip L Foot",
            "ManBip R Finger0",
            "ManBip R Finger01",
            "ManBip R Finger02",
            "ManBip R Finger1",
            "ManBip R Finger11",
            "ManBip R Finger12",
            "ManBip R Finger2",
            "ManBip R Finger21",
            "ManBip R Finger22",
            "ManBip R Finger3",
            "ManBip R Finger31",
            "ManBip R Finger32",
            "ManBip R Finger4",
            "ManBip R Finger41",
            "ManBip R Finger42",
            "ManBip L Finger0",
            "ManBip L Finger01",
            "ManBip L Finger02",
            "ManBip L Finger1",
            "ManBip L Finger11",
            "ManBip L Finger12",
            "ManBip L Finger2",
            "ManBip L Finger21",
            "ManBip L Finger22",
            "ManBip L Finger3",
            "ManBip L Finger31",
            "ManBip L Finger32",
            "ManBip L Finger4",
            "ManBip L Finger41",
            "ManBip L Finger42",
            "ManBip R Toe1",
            "ManBip R Toe0",
            "ManBip L Toe1",
            "ManBip L Toe0",
            "ManBip Pelvis",
            "chinkoCenter",
            "chinko1",
            "chinko2",
            "tamabukuro",
            "ManBip Footsteps",
        };

        public readonly static string comboBonePrefix = "* ";
    }
}

#if true

namespace VYMModule
{

#region VoiceTableToCsv
    // csvテーブル移行用 v0027
    class cnv2csv
    {
        static readonly string[] levelTypes = new string[] { /*"10",*/ "20", "30", "40" };
#if COM3D2
        static readonly string[] maidTypes = new string[] { "Pure", "Pride", "Cool", "Yandere", "Anesan", "Genki", "Sadist", "Muku", "Majime", "Rindere", };
#else
        static readonly string[] maidTypes = new string[] { "Pure", "Pride", "Cool", "Yandere", "Anesan", "Genki", "Sadist", };
#endif
        static readonly string[] voiceTypes = new string[] { "sLoopVoice{0}{1}Vibe", "sLoopVoice{0}{1}Fera", "sOrgasmVoice{0}{1}Vibe", "sOrgasmVoice{0}{1}Fera", };
        static readonly string[] customTypes = new string[] { "1", "2", "3", "4" };
        static readonly string[] voiceCustoms = new string[] { "sLoopVoice{0}Custom{1}", "sOrgasmVoice{0}Custom{1}", };

        public static void SaveVoiceCsvFile()
        {
            string fileName = NewVoiceTable.fileName;
            if (System.IO.File.Exists(fileName)/* && !vs_Overwrite*/)
            {  //上書きのチェック
#if !DEBUG
                return;
#endif
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(fileName, false, new System.Text.UTF8Encoding(true)))
                {
                    sw.WriteLine("VoiceType,State(20=Low|30=High|40=Rest),Personal,Level(0~3=ExciteLv|4=Stun|-1=N/A),Files...");
                    toCsv(sw);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("XtMS: CSV生成エラー " + e);
            }

        }

        public static void toCsv(StreamWriter sw)
        {
            var vcfg = VYMModule.VymModule.voiceLegacy;

            maidTypes.ToList().ForEach(y =>
            {
                levelTypes.ToList().ForEach(x =>
                {
                    voiceTypes.ToList().ForEach(z =>
                    {
                        var str = string.Format(z, x, y);
                        try
                        {
                            var obj = vcfg.GetType().GetField(str).GetValue(vcfg);
                            if (obj is string[][])
                            {
                                var spp = obj as string[][];

                                for (int i = 0; i < spp.Length; i++)
                                {
                                    outputLine(spp[i], i);
                                }
                            }
                            else
                            {
                                string[] sl = obj as string[];
                                if (z.Contains("sLoopVoice") && x == "40")
                                {
                                    for (int i = 0; i < sl.Length; i++)
                                    {
                                        string[] newsl = new string[1] { sl[i] };
                                        outputLine(newsl, i);
                                    }
                                }
                                else
                                    outputLine(sl, -1);
                            }

                            void outputLine(string[] vList, int state)
                            {
                                List<string> line = new List<string> { string.Format(z, "", "").Substring(1).Replace("Vibe", ""), x, y, state.ToString() };
                                line.AddRange(vList);
                                sw.WriteLine(string.Join(",", line.ToArray()));
                            }
                        }
                        catch
                        {
                            if (!(z.Contains("sOrgasmVoice") && x != "30") && !(z.Contains("Fera") && x == "40"))
                                Console.WriteLine(str + "は見つかりません");
                        }
                    });
                });
            });

            customTypes.ToList().ForEach(y =>
            {
                levelTypes.ToList().ForEach(x =>
                {
                    voiceTypes.ToList().ForEach(z =>
                    {
                        var str = string.Format(voiceCustoms[0], x, y);
                        try
                        {
                            if (z.Contains("Fera") && x == "40")
                                return; // continue

                            if (!z.Contains("sLoopVoice"))
                            {
                                str = string.Format(voiceCustoms[1], x, y);
                            }

                            var obj = vcfg.GetType().GetField(str).GetValue(vcfg);
                            if (obj is string[][])
                            {
                                var spp = obj as string[][];

                                for (int i = 0; i < spp.Length; i++)
                                {
                                    outputLine(spp[i], i);
                                }
                            }
                            else
                            {
                                string[] sl = obj as string[];
                                if (z.Contains("sLoopVoice") && x == "40")
                                {
                                    for (int i = 0; i < sl.Length; i++)
                                    {
                                        string[] newsl = new string[1] { sl[i] };
                                        outputLine(newsl, i);
                                    }
                                }
                                else
                                    outputLine(sl, -1);
                            }

                            void outputLine(string[] vList, int state)
                            {
                                List<string> line = new List<string> { string.Format(z, "", "").Substring(1).Replace("Vibe", ""), x, "Custom" + y, state.ToString() };
                                line.AddRange(vList);
                                sw.WriteLine(string.Join(",", line.ToArray()));
                            }
                        }
                        catch
                        {
                            if (!(z.Contains("sOrgasmVoice") && x != "30") && !(z.Contains("Fera") && x == "40"))
                                Console.WriteLine(str + "は見つかりません");
                        }
                    });
                });
            });
        }
    }
#endregion

#region NewVoiceTable

    class NewVoiceTable
    {
        public static readonly string fileName
            = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            @"Config\xtYotogiVoiceTable.csv");

        public class VoiceSet
        {
            public enum VoiceType
            {
                LoopVoice,
                LoopVoiceFera,
                OrgasmVoice,
                OrgasmVoiceFera,
            }

            public VoiceType MyType;   // 音声タイプ
            public int State;          // VYMでのバイブ強弱(20=Low|30=High|40=Rest)
            public string Personal;    // 性格
            public int Level;          // 興奮Lv(0~3=ExciteLv|4=Stun|-1=N/A)
            public string[] Files;     // 音声リスト

            public VoiceSet(VoiceType type, int state, string personal, int level, string[] files)
            {
                this.MyType = type;
                this.State = state;
                this.Personal = personal;
                this.Level = level;
                this.Files = files;
            }

            public VoiceSet(string type, string state, string personal, string level, string[] files)
                : this((VoiceType)Enum.Parse(typeof(VoiceType), type, true), int.Parse(state), personal, int.Parse(level), files)
            {
            }

            public VoiceSet(string type, string state, string personal, string level)
                : this((VoiceType)Enum.Parse(typeof(VoiceType), type, true), int.Parse(state), personal, int.Parse(level), new string[0])
            {
            }
        }
        public static List<VoiceSet> voiceTable = new List<VoiceSet>();

        public static void LoadCsv()
        {
            // 他プロセスが開いていても読み込む
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fs, new System.Text.UTF8Encoding(true)))
                {
                    voiceTable = new List<VoiceSet>();

                    int i = 0;
                    while (sr.Peek() > -1)
                    {
                        i++;
                        List<string> lineData = new List<string>();
                        string m = sr.ReadLine();

                        if (i <= 1)
                            continue;   // ヘッダーを飛ばす

                        string[] values = m.Split(new char[] { ',', '\t' });
                        try
                        {
                            // パラメータ
                            var set = new VoiceSet(values[0], values[1], values[2], values[3]);
                            // ファイルリスト
                            if (values.Length > 4)
                            {
                                int emplen = 0;
                                for (int j = values.Length - 1; j > 4; j--)
                                {
                                    if (string.IsNullOrEmpty(values[j]))
                                        emplen++;
                                    else
                                        break;
                                }

                                set.Files = new string[values.Length - 4 - emplen];
                                Array.Copy(values, 4, set.Files, 0, set.Files.Length);
                            }
                            voiceTable.Add(set);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("CSV読込エラー 行:{0} " + m, i);
                            Console.WriteLine("                : " + e.Message);
                        }
                    }

                    sr.Close();
                }
            }
        }

        public static VoiceSet GetVoiceSet(NewVoiceTable.VoiceSet.VoiceType VoiceType, int State, string Personal, int Level)
        {
            var get = voiceTable.FirstOrDefault(x =>
                            x.MyType == VoiceType &&
                            x.State == State &&
                            x.Personal == Personal &&
                            x.Level == Level);

            if (get == null)
            {
                Console.WriteLine("ボイスセットが見つかりません {0}, {1}, {2}, {3}", VoiceType, State, Personal, Level);
            }
#if DEBUG
            else
            {
                Console.WriteLine("ボイスセット {0}, {1}, {2}, {3}, " + string.Join(",", get.Files), VoiceType, State, Personal, Level);
            }
#endif
            return get;
        }
    }


#endregion

    public class VymModule
    {
        //　興奮度管理
        private static int vExciteLevel = 1;                       //　０～３００の興奮度を、１～４の興奮レベルに変換した値
        private static double iCurrentExcite = 0;                  //　現在興奮値
        private static int OrgasmVoice = 0;                        //　絶頂時音声フラグ
        private static int vStateMajor = 10;                       //　強弱によるステート
                                                                   //private int vOrgasmCount = 0;                       //　絶頂回数
        public class vMaidParam
        {
            public int vOrgasmCmb = 0;                         //　連続絶頂回数
            public int iExcite_Old = 0;
            public int vStateMajor_Old = 10;
            public bool faceanimeChanged = true; //最初に表情を切り替えるようにtrue
        }
        public static Dictionary<Maid, vMaidParam> maidParam = new Dictionary<Maid, vMaidParam>();


        //改変　表情管理（バイブ）
        public static int vStateAltTime1VBase = 120;                 //　フェイスアニメの変化時間１（秒）（20→21の遷移、40→41の遷移）
        public static int vStateAltTime2VBase = 180;                 //　フェイスアニメの変化時間２（秒）（30におけるランダム再遷移）
        public static int vStateAltTime1VRandomExtend = 120;         //　変化時間１へのランダム加算（秒）
        public static int vStateAltTime2VRandomExtend = 180;         //　変化時間２へのランダム加算（秒）
        public static float fAnimeFadeTimeV = 1.0f;                //　バイブモードのフェイスアニメ等のフェード時間（秒）
        private static bool vMaidStun = false;

        //ランダムボイス被り防止
        //private static int[] iRandomVoiceBackup = new int[] { -1, -1, -1, -1, -1 };
        //複数メイド対応型
        private static Dictionary<int, Dictionary<Maid, int>> iRandomVoiceBackup = new Dictionary<int, Dictionary<Maid, int>>
        {
            { 0, new Dictionary<Maid , int>() },
            { 1, new Dictionary<Maid , int>() },
            { 2, new Dictionary<Maid , int>() },
            { 3, new Dictionary<Maid , int>() },
            { 4, new Dictionary<Maid , int>() },
        };

        //表情バックアップ
        private static Dictionary<Maid, string> sFaceBackup = new Dictionary<Maid, string>();
        private static Dictionary<Maid, string> sFace3Backup = new Dictionary<Maid, string>();

        public enum VoiceMode
        {
            オートモード,
            通常固定,
            舐め固定,
            カスタム1,
            カスタム2,
            カスタム3,
            カスタム4,
        }

#region ini設定用
        //　設定クラス（Iniファイルで読み書きしたい変数はここに格納する）
        public class VibeYourMaidConfig
        { //@API実装//→API用にpublicに変更

            public int vExciteLevelThresholdV1 = 100;           //　興奮レベル１→２閾値
            public int vExciteLevelThresholdV2 = 200; //180           //　興奮レベル２→３閾値
            public int vExciteLevelThresholdV3 = 250;           //　興奮レベル３→４閾値
            public int iYodareAppearLevelV = 3;                 //　所定の興奮レベル以上でよだれをつける（１～４のどれかを入れる、０で無効） 

            public int vStateMajor30Threshold = 200;           //　Voice20→30しきい値

            public bool NamidaEnabled = false;
            public bool HohoEnabled = true;
            public bool YodareEnabled = false;
            public float fAnimeFadeTimeV = 1.0f;                //　バイブモードのフェイスアニメ等のフェード時間（秒）

            public VoiceMode eVoiceMode = VoiceMode.オートモード;

            public string sFaceAnimeYotogiDefault = "エロ好感１";

            //　表情テーブル　（バイブ）
            public string[][] sFaceAnime20Vibe = new string[][] {
            new string[] { "困った" , "ダンス困り顔" , "恥ずかしい" , "苦笑い" , "引きつり笑顔" , "まぶたギュ" },
            new string[] { "困った" , "ダンス困り顔" , "恥ずかしい" , "苦笑い" , "引きつり笑顔" , "まぶたギュ" },
            new string[] { "怒り" , "興奮射精後１" , "発情" , "エロ痛み２" , "引きつり笑顔" , "エロ我慢３" },
            new string[] { "怒り" , "興奮射精後１" , "発情" , "エロ痛み２" , "引きつり笑顔" , "エロ我慢３" }
            };
            public string[][] sFaceAnime30Vibe = new string[][] {
            new string[] { "エロ痛み１" , "エロ痛み２" , "エロ我慢１" , "エロ我慢２" , "泣き" , "怒り" },
            new string[] { "エロ痛み１" , "エロ痛み２" , "エロ我慢１" , "エロ我慢２" , "泣き" , "怒り" },
            new string[] { "エロ痛み我慢" , "エロ痛み我慢２" , "エロ痛み我慢３" , "エロメソ泣き" , "エロ羞恥３" , "エロ我慢３" },
            new string[] { "エロ痛み我慢" , "エロ痛み我慢２" , "エロ痛み我慢３" , "エロメソ泣き" , "エロ羞恥３" , "エロ我慢３" }
            };
            public string[] sFaceAnime40Vibe = new string[] { "少し怒り", "思案伏せ目", "まぶたギュ", "エロメソ泣き" };

            public string[] sFaceAnimeStun = new string[] { "絶頂射精後１", "興奮射精後１", "エロメソ泣き", "エロ痛み２", "エロ我慢３", "引きつり笑顔", "エロ通常３", "泣き" };
        }

#region Legacy
        // v0027でiniよりボイステーブルを分離
        public static VoiceTableLegacy voiceLegacy = new VoiceTableLegacy();
        // 旧ボイステーブル、移行＆CSV生成用
        public class VoiceTableLegacy
        {
            //　性格別声テーブル　弱バイブ版---------------------------------------------------------------
            //通常
            public string[][] sLoopVoice20PrideVibe = new string[][] {
              new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" },
              new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" },
              new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" },
              new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" },
              new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" }
              };
            public string[][] sLoopVoice20CoolVibe = new string[][] {
              new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" },
              new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" },
              new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" },
              new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" },
              new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" }
              };
            public string[][] sLoopVoice20PureVibe = new string[][] {
              new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" },
              new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" },
              new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" },
              new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" },
              new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" }
              };
            public string[][] sLoopVoice20YandereVibe = new string[][] {
              new string[] { "s3_02767.ogg" , "s3_02768.ogg" , "s3_02769.ogg" , "s3_02770.ogg" },
              new string[] { "s3_02767.ogg" , "s3_02768.ogg" , "s3_02769.ogg" , "s3_02770.ogg" },
              new string[] { "s3_02767.ogg" , "s3_02768.ogg" , "s3_02769.ogg" , "s3_02770.ogg" },
              new string[] { "s3_02767.ogg" , "s3_02768.ogg" , "s3_02769.ogg" , "s3_02770.ogg" },
              new string[] { "s3_02767.ogg" , "s3_02768.ogg" , "s3_02769.ogg" , "s3_02770.ogg" }
              };
            public string[][] sLoopVoice20AnesanVibe = new string[][] {
              new string[] { "s4_08211.ogg" , "s4_08212.ogg" , "s4_08213.ogg" , "s4_08214.ogg" },
              new string[] { "s4_08211.ogg" , "s4_08212.ogg" , "s4_08213.ogg" , "s4_08214.ogg" },
              new string[] { "s4_08211.ogg" , "s4_08212.ogg" , "s4_08213.ogg" , "s4_08214.ogg" },
              new string[] { "s4_08211.ogg" , "s4_08212.ogg" , "s4_08213.ogg" , "s4_08214.ogg" },
              new string[] { "s4_08211.ogg" , "s4_08212.ogg" , "s4_08213.ogg" , "s4_08214.ogg" }
              };
            public string[][] sLoopVoice20GenkiVibe = new string[][] {
              new string[] { "s5_04127.ogg" , "s5_04129.ogg" , "s5_04130.ogg" , "s5_04131.ogg" },
              new string[] { "s5_04127.ogg" , "s5_04048.ogg" , "s5_04130.ogg" , "s5_04048.ogg" },
              new string[] { "s5_04133.ogg" , "s5_04134.ogg" , "s5_04047.ogg" , "s5_04048.ogg" },
              new string[] { "s5_04133.ogg" , "s5_04134.ogg" , "s5_04047.ogg" , "s5_04131.ogg" },
              new string[] { "s5_04133.ogg" , "s5_04134.ogg" , "s5_04047.ogg" , "s5_04131.ogg" }
              };
            public string[][] sLoopVoice20SadistVibe = new string[][] {
              new string[] { "S6_02244.ogg" , "S6_02180.ogg" , "S6_02181.ogg" , "S6_02245.ogg" },
              new string[] { "S6_02179.ogg" , "S6_02243.ogg" , "S6_02246.ogg" , "S6_02182.ogg" },
              new string[] { "S6_02179.ogg" , "S6_02183.ogg" , "S6_02246.ogg" , "S6_02247.ogg" },
              new string[] { "S6_02183.ogg" , "S6_02184.ogg" , "S6_02246.ogg" , "S6_02247.ogg" },
              new string[] { "S6_02179.ogg" , "S6_02180.ogg" , "S6_02181.ogg" , "S6_02182.ogg" }
              };

#if COM3D2
            // オダメ追加分
            public string[][] sLoopVoice20MukuVibe = new string[][] {
                new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" },
                new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" },
                new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" },
                new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" },
                new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" }
                };
            public string[][] sLoopVoice20MajimeVibe = new string[][] {
                new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" },
                new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" },
                new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" },
                new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" },
                new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" }
                };
            public string[][] sLoopVoice20RindereVibe = new string[][] {
                new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" },
                new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" },
                new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" },
                new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" },
                new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" }
                };
#endif

            //-----------------------------------------------------------------------------------
            //フェラ
            public string[][] sLoopVoice20PrideFera = new string[][] {
              new string[] { "S0_01383.ogg" , "S0_01367.ogg" , "S0_01384.ogg" , "S0_01369.ogg" },
              new string[] { "S0_01383.ogg" , "S0_01367.ogg" , "S0_01384.ogg" , "S0_01369.ogg" },
              new string[] { "S0_01383.ogg" , "S0_01367.ogg" , "S0_01384.ogg" , "S0_01369.ogg" },
              new string[] { "S0_01383.ogg" , "S0_01367.ogg" , "S0_01384.ogg" , "S0_01369.ogg" },
              new string[] { "S0_01383.ogg" , "S0_01367.ogg" , "S0_01384.ogg" , "S0_01369.ogg" }
              };
            public string[][] sLoopVoice20CoolFera = new string[][] {
              new string[] { "S1_02455.ogg" , "S1_02440.ogg" , "S1_02457.ogg" , "S1_02442.ogg" },
              new string[] { "S1_02455.ogg" , "S1_02440.ogg" , "S1_02457.ogg" , "S1_02442.ogg" },
              new string[] { "S1_02455.ogg" , "S1_02440.ogg" , "S1_02457.ogg" , "S1_02442.ogg" },
              new string[] { "S1_02455.ogg" , "S1_02440.ogg" , "S1_02457.ogg" , "S1_02442.ogg" },
              new string[] { "S1_02455.ogg" , "S1_02440.ogg" , "S1_02457.ogg" , "S1_02442.ogg" }
              };
            public string[][] sLoopVoice20PureFera = new string[][] {
              new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
              new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
              new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
              new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
              new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" }
              };
            public string[][] sLoopVoice20YandereFera = new string[][] {
              new string[] { "S3_02833.ogg" , "S3_02818.ogg" , "S3_02835.ogg" , "S3_02820.ogg" },
              new string[] { "S3_02833.ogg" , "S3_02818.ogg" , "S3_02835.ogg" , "S3_02820.ogg" },
              new string[] { "S3_02833.ogg" , "S3_02818.ogg" , "S3_02835.ogg" , "S3_02820.ogg" },
              new string[] { "S3_02833.ogg" , "S3_02818.ogg" , "S3_02835.ogg" , "S3_02820.ogg" },
              new string[] { "S3_02833.ogg" , "S3_02818.ogg" , "S3_02835.ogg" , "S3_02820.ogg" }
              };
            public string[][] sLoopVoice20AnesanFera = new string[][] {
              new string[] { "S4_08241.ogg" , "S4_08258.ogg" , "S4_08243.ogg" , "S4_08259.ogg" },
              new string[] { "S4_08241.ogg" , "S4_08258.ogg" , "S4_08243.ogg" , "S4_08259.ogg" },
              new string[] { "S4_08241.ogg" , "S4_08258.ogg" , "S4_08243.ogg" , "S4_08259.ogg" },
              new string[] { "S4_08241.ogg" , "S4_08258.ogg" , "S4_08243.ogg" , "S4_08259.ogg" },
              new string[] { "S4_08241.ogg" , "S4_08258.ogg" , "S4_08243.ogg" , "S4_08259.ogg" }
              };
            public string[][] sLoopVoice20GenkiFera = new string[][] {
              new string[] { "S5_04163.ogg" , "S5_04162.ogg" , "S5_04179.ogg" , "S5_04181.ogg" },
              new string[] { "S5_04163.ogg" , "S5_04162.ogg" , "S5_04179.ogg" , "S5_04181.ogg" },
              new string[] { "S5_04163.ogg" , "S5_04162.ogg" , "S5_04179.ogg" , "s5_04174.ogg" },
              new string[] { "S5_04163.ogg" , "S5_04162.ogg" , "S5_04179.ogg" , "s5_04174.ogg" },
              new string[] { "S5_04163.ogg" , "S5_04162.ogg" , "S5_04179.ogg" , "s5_04174.ogg" }
              };
            public string[][] sLoopVoice20SadistFera = new string[][] {
              new string[] { "S6_02219.ogg" , "S6_02220.ogg" , "S6_02221.ogg" , "S6_02222.ogg" },
              new string[] { "S6_02219.ogg" , "S6_02220.ogg" , "S6_02221.ogg" , "S6_02222.ogg" },
              new string[] { "S6_02219.ogg" , "S6_02220.ogg" , "S6_02221.ogg" , "S6_02222.ogg" },
              new string[] { "S6_02219.ogg" , "S6_02220.ogg" , "S6_02221.ogg" , "S6_02222.ogg" },
              new string[] { "S6_02219.ogg" , "S6_02220.ogg" , "S6_02221.ogg" , "S6_02222.ogg" }
              };
#if COM3D2
            // オダメ追加分
            public string[][] sLoopVoice20MukuFera = new string[][] {
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" }
                };
            public string[][] sLoopVoice20MajimeFera = new string[][] {
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" }
                };
            public string[][] sLoopVoice20RindereFera = new string[][] {
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" }
                };
#endif

            //-----------------------------------------------------------------------------------
            //カスタムボイス１
            public string[][] sLoopVoice20Custom1 = new string[][] {
              new string[] { "N0_00435.ogg" , "N0_00449.ogg" },
              new string[] { "N0_00435.ogg" , "N0_00449.ogg" },
              new string[] { "N0_00435.ogg" , "N0_00449.ogg" },
              new string[] { "N0_00435.ogg" , "N0_00449.ogg" },
              new string[] { "N0_00435.ogg" , "N0_00449.ogg" }
              };
            //カスタムボイス２
            public string[][] sLoopVoice20Custom2 = new string[][] {
              new string[] { "N7_00262.ogg" , "N7_00267.ogg" , "N7_00269.ogg" , "N7_00272.ogg" },
              new string[] { "N7_00262.ogg" , "N7_00267.ogg" , "N7_00269.ogg" , "N7_00272.ogg" },
              new string[] { "N7_00262.ogg" , "N7_00267.ogg" , "N7_00269.ogg" , "N7_00272.ogg" },
              new string[] { "N7_00262.ogg" , "N7_00267.ogg" , "N7_00269.ogg" , "N7_00272.ogg" },
              new string[] { "N7_00262.ogg" , "N7_00267.ogg" , "N7_00269.ogg" , "N7_00272.ogg" }
              };
            //カスタムボイス３
            public string[][] sLoopVoice20Custom3 = new string[][] {
              new string[] { "N1_00170.ogg" , "N1_00191.ogg" , "N1_00192.ogg" , "N1_00194.ogg" },
              new string[] { "N1_00170.ogg" , "N1_00191.ogg" , "N1_00192.ogg" , "N1_00194.ogg" },
              new string[] { "N1_00170.ogg" , "N1_00191.ogg" , "N1_00192.ogg" , "N1_00194.ogg" },
              new string[] { "N1_00170.ogg" , "N1_00191.ogg" , "N1_00192.ogg" , "N1_00194.ogg" },
              new string[] { "N1_00170.ogg" , "N1_00191.ogg" , "N1_00192.ogg" , "N1_00194.ogg" }
              };
            //カスタムボイス４
            public string[][] sLoopVoice20Custom4 = new string[][] {
              new string[] { "N3_00157.ogg" , "N3_00370.ogg" },
              new string[] { "N3_00157.ogg" , "N3_00370.ogg" },
              new string[] { "N3_00157.ogg" , "N3_00370.ogg" },
              new string[] { "N3_00157.ogg" , "N3_00370.ogg" },
              new string[] { "N3_00157.ogg" , "N3_00370.ogg" }
              };



            //　性格別声テーブル　強バイブ版---------------------------------------------------------------
            //通常
            public string[][] sLoopVoice30PrideVibe = new string[][] {
              new string[] { "s0_01326.ogg" , "s0_01327.ogg" , "s0_01330.ogg" , "s0_01331.ogg" },
              new string[] { "s0_01326.ogg" , "s0_01327.ogg" , "s0_01330.ogg" , "s0_01331.ogg" },
              new string[] { "s0_01326.ogg" , "s0_01327.ogg" , "s0_01330.ogg" , "s0_01331.ogg" },
              new string[] { "s0_01326.ogg" , "s0_01327.ogg" , "s0_01330.ogg" , "s0_01331.ogg" },
              new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" }
              };
            public string[][] sLoopVoice30CoolVibe = new string[][] {
              new string[] { "s1_02401.ogg" , "s1_02400.ogg" , "s1_02402.ogg" , "s1_02404.ogg" },
              new string[] { "s1_02401.ogg" , "s1_02400.ogg" , "s1_02402.ogg" , "s1_02404.ogg" },
              new string[] { "s1_02401.ogg" , "s1_02400.ogg" , "s1_02402.ogg" , "s1_02404.ogg" },
              new string[] { "s1_02401.ogg" , "s1_02400.ogg" , "s1_02402.ogg" , "s1_02404.ogg" },
              new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" }
              };
            public string[][] sLoopVoice30PureVibe = new string[][] {
              new string[] { "s2_01185.ogg" , "s2_01186.ogg" , "s2_01187.ogg" , "s2_01188.ogg" },
              new string[] { "s2_01185.ogg" , "s2_01186.ogg" , "s2_01187.ogg" , "s2_01188.ogg" },
              new string[] { "s2_01185.ogg" , "s2_01186.ogg" , "s2_01187.ogg" , "s2_01188.ogg" },
              new string[] { "s2_01185.ogg" , "s2_01186.ogg" , "s2_01187.ogg" , "s2_01188.ogg" },
              new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" }
              };
            public string[][] sLoopVoice30YandereVibe = new string[][] {
              new string[] { "s3_02797.ogg" , "s3_02798.ogg" , "s3_02691.ogg" , "s3_02796.ogg" },
              new string[] { "s3_02797.ogg" , "s3_02798.ogg" , "s3_02691.ogg" , "s3_02796.ogg" },
              new string[] { "s3_02797.ogg" , "s3_02798.ogg" , "s3_02691.ogg" , "s3_02796.ogg" },
              new string[] { "s3_02797.ogg" , "s3_02798.ogg" , "s3_02691.ogg" , "s3_02796.ogg" },
              new string[] { "s3_02767.ogg" , "s3_02768.ogg" , "s3_02769.ogg" , "s3_02770.ogg" }
              };
            public string[][] sLoopVoice30AnesanVibe = new string[][] {
              new string[] { "s4_08140.ogg" , "s4_08141.ogg" , "s4_08142.ogg" , "s4_08145.ogg" },
              new string[] { "s4_08140.ogg" , "s4_08141.ogg" , "s4_08142.ogg" , "s4_08145.ogg" },
              new string[] { "s4_08140.ogg" , "s4_08141.ogg" , "s4_08149.ogg" , "s4_08150.ogg" },
              new string[] { "s4_08140.ogg" , "s4_08134.ogg" , "s4_08149.ogg" , "s4_08150.ogg" },
              new string[] { "s4_08211.ogg" , "s4_08212.ogg" , "s4_08213.ogg" , "s4_08214.ogg" }
              };
            public string[][] sLoopVoice30GenkiVibe = new string[][] {
              new string[] { "s5_04133.ogg" , "s5_04058.ogg" , "s5_04055.ogg" , "s5_04050.ogg" },
              new string[] { "s5_04133.ogg" , "s5_04058.ogg" , "s5_04055.ogg" , "s5_04050.ogg" },
              new string[] { "s5_04051.ogg" , "s5_04055.ogg" , "s5_04054.ogg" , "s5_04052.ogg" },
              new string[] { "s5_04055.ogg" , "s5_04061.ogg" , "s5_04054.ogg" , "s5_04052.ogg" },
              new string[] { "s5_04133.ogg" , "s5_04134.ogg" , "s5_04047.ogg" , "s5_04131.ogg" }
              };
            public string[][] sLoopVoice30SadistVibe = new string[][] {
              new string[] { "S6_02183.ogg" , "S6_02184.ogg" , "S6_02246.ogg" , "S6_02247.ogg" },
              new string[] { "S6_02183.ogg" , "S6_02184.ogg" , "S6_02246.ogg" , "S6_02247.ogg" },
              new string[] { "S6_02248.ogg" , "S6_02184.ogg" , "S6_02185.ogg" , "S6_02249.ogg" },
              new string[] { "S6_02249.ogg" , "S6_02250.ogg" , "S6_02185.ogg" , "S6_02186.ogg" },
              new string[] { "S6_02243.ogg" , "S6_02244.ogg" , "S6_02245.ogg" , "S6_02246.ogg" }
              };
#if COM3D2
            // オダメ追加分
            public string[][] sLoopVoice30MukuVibe = new string[][] {
                new string[] { "s2_01185.ogg" , "s2_01186.ogg" , "s2_01187.ogg" , "s2_01188.ogg" },
                new string[] { "s2_01185.ogg" , "s2_01186.ogg" , "s2_01187.ogg" , "s2_01188.ogg" },
                new string[] { "s2_01185.ogg" , "s2_01186.ogg" , "s2_01187.ogg" , "s2_01188.ogg" },
                new string[] { "s2_01185.ogg" , "s2_01186.ogg" , "s2_01187.ogg" , "s2_01188.ogg" },
                new string[] { "s2_01235.ogg" , "s2_01236.ogg" , "s2_01237.ogg" , "s2_01238.ogg" }
                };
            public string[][] sLoopVoice30MajimeVibe = new string[][] {
                new string[] { "s1_02401.ogg" , "s1_02400.ogg" , "s1_02402.ogg" , "s1_02404.ogg" },
                new string[] { "s1_02401.ogg" , "s1_02400.ogg" , "s1_02402.ogg" , "s1_02404.ogg" },
                new string[] { "s1_02401.ogg" , "s1_02400.ogg" , "s1_02402.ogg" , "s1_02404.ogg" },
                new string[] { "s1_02401.ogg" , "s1_02400.ogg" , "s1_02402.ogg" , "s1_02404.ogg" },
                new string[] { "s1_02396.ogg" , "s1_02390.ogg" , "s1_02391.ogg" , "s1_02392.ogg" }
                };
            public string[][] sLoopVoice30RindereVibe = new string[][] {
                new string[] { "s0_01326.ogg" , "s0_01327.ogg" , "s0_01330.ogg" , "s0_01331.ogg" },
                new string[] { "s0_01326.ogg" , "s0_01327.ogg" , "s0_01330.ogg" , "s0_01331.ogg" },
                new string[] { "s0_01326.ogg" , "s0_01327.ogg" , "s0_01330.ogg" , "s0_01331.ogg" },
                new string[] { "s0_01326.ogg" , "s0_01327.ogg" , "s0_01330.ogg" , "s0_01331.ogg" },
                new string[] { "s0_01236.ogg" , "s0_01237.ogg" , "s0_01238.ogg" , "s0_01239.ogg" }
                };
#endif

            //-----------------------------------------------------------------------------------
            //フェラ
            public string[][] sLoopVoice30PrideFera = new string[][] {
              new string[] { "S0_01385.ogg" , "S0_01371.ogg" , "S0_01386.ogg" , "S0_01387.ogg" },
              new string[] { "S0_01385.ogg" , "S0_01371.ogg" , "S0_01386.ogg" , "S0_01387.ogg" },
              new string[] { "S0_01385.ogg" , "S0_01371.ogg" , "S0_01386.ogg" , "S0_01387.ogg" },
              new string[] { "S0_01385.ogg" , "S0_01371.ogg" , "S0_01386.ogg" , "S0_01387.ogg" },
              new string[] { "S0_01383.ogg" , "S0_01367.ogg" , "S0_01384.ogg" , "S0_01369.ogg" }
              };
            public string[][] sLoopVoice30CoolFera = new string[][] {
              new string[] { "S1_02458.ogg" , "S1_02459.ogg" , "S1_02444.ogg" , "S1_02460.ogg" },
              new string[] { "S1_02458.ogg" , "S1_02459.ogg" , "S1_02444.ogg" , "S1_02460.ogg" },
              new string[] { "S1_02458.ogg" , "S1_02459.ogg" , "S1_02444.ogg" , "S1_02460.ogg" },
              new string[] { "S1_02458.ogg" , "S1_02459.ogg" , "S1_02444.ogg" , "S1_02460.ogg" },
              new string[] { "S1_02455.ogg" , "S1_02440.ogg" , "S1_02457.ogg" , "S1_02442.ogg" }
              };
            public string[][] sLoopVoice30PureFera = new string[][] {
              new string[] { "S2_01299.ogg" , "S2_01300.ogg" , "S2_01285.ogg" , "S2_01301.ogg" },
              new string[] { "S2_01299.ogg" , "S2_01300.ogg" , "S2_01285.ogg" , "S2_01301.ogg" },
              new string[] { "S2_01299.ogg" , "S2_01300.ogg" , "S2_01285.ogg" , "S2_01301.ogg" },
              new string[] { "S2_01299.ogg" , "S2_01300.ogg" , "S2_01285.ogg" , "S2_01301.ogg" },
              new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" }
              };
            public string[][] sLoopVoice30YandereFera = new string[][] {
              new string[] { "S3_02836.ogg" , "S3_02837.ogg" , "S3_02822.ogg" , "S3_02838.ogg" },
              new string[] { "S3_02836.ogg" , "S3_02837.ogg" , "S3_02822.ogg" , "S3_02838.ogg" },
              new string[] { "S3_02836.ogg" , "S3_02837.ogg" , "S3_02822.ogg" , "S3_02838.ogg" },
              new string[] { "S3_02836.ogg" , "S3_02837.ogg" , "S3_02822.ogg" , "S3_02838.ogg" },
              new string[] { "S3_02833.ogg" , "S3_02818.ogg" , "S3_02835.ogg" , "S3_02820.ogg" }
              };
            public string[][] sLoopVoice30AnesanFera = new string[][] {
              new string[] { "S4_08244.ogg" , "S4_08245.ogg" , "S4_08262.ogg" , "S4_08246.ogg" },
              new string[] { "S4_08244.ogg" , "S4_08245.ogg" , "S4_08262.ogg" , "S4_08246.ogg" },
              new string[] { "S4_08244.ogg" , "S4_08245.ogg" , "S4_08262.ogg" , "S4_08246.ogg" },
              new string[] { "S4_08244.ogg" , "S4_08245.ogg" , "S4_08262.ogg" , "S4_08246.ogg" },
              new string[] { "S4_08241.ogg" , "S4_08258.ogg" , "S4_08243.ogg" , "S4_08259.ogg" }
              };
            public string[][] sLoopVoice30GenkiFera = new string[][] {
              new string[] { "S5_04093.ogg" , "S5_04094.ogg" , "S5_04102.ogg" , "S5_04100.ogg" },
              new string[] { "S5_04093.ogg" , "S5_04094.ogg" , "S5_04102.ogg" , "S5_04100.ogg" },
              new string[] { "S5_04093.ogg" , "S5_04094.ogg" , "S5_04102.ogg" , "S5_04100.ogg" },
              new string[] { "S5_04093.ogg" , "S5_04094.ogg" , "S5_04102.ogg" , "S5_04100.ogg" },
              new string[] { "S5_04163.ogg" , "S5_04162.ogg" , "S5_04179.ogg" , "s5_04174.ogg" }
              };
            public string[][] sLoopVoice30SadistFera = new string[][] {
              new string[] { "S6_02223.ogg" , "S6_02224.ogg" , "S6_02225.ogg" , "S6_02226.ogg" },
              new string[] { "S6_02223.ogg" , "S6_02224.ogg" , "S6_02225.ogg" , "S6_02226.ogg" },
              new string[] { "S6_02223.ogg" , "S6_02224.ogg" , "S6_02225.ogg" , "S6_02226.ogg" },
              new string[] { "S6_02223.ogg" , "S6_02224.ogg" , "S6_02225.ogg" , "S6_02226.ogg" },
              new string[] { "S6_02219.ogg" , "S6_02220.ogg" , "S6_02221.ogg" , "S6_02222.ogg" }
              };

#if COM3D2
            // オダメ追加分
            public string[][] sLoopVoice30RindereFera = new string[][] {
                new string[] { "S0_01385.ogg" , "S0_01371.ogg" , "S0_01386.ogg" , "S0_01387.ogg" },
                new string[] { "S0_01385.ogg" , "S0_01371.ogg" , "S0_01386.ogg" , "S0_01387.ogg" },
                new string[] { "S0_01385.ogg" , "S0_01371.ogg" , "S0_01386.ogg" , "S0_01387.ogg" },
                new string[] { "S0_01385.ogg" , "S0_01371.ogg" , "S0_01386.ogg" , "S0_01387.ogg" },
                new string[] { "S0_01383.ogg" , "S0_01367.ogg" , "S0_01384.ogg" , "S0_01369.ogg" }
                };
            public string[][] sLoopVoice30MajimeFera = new string[][] {
                new string[] { "S1_02458.ogg" , "S1_02459.ogg" , "S1_02444.ogg" , "S1_02460.ogg" },
                new string[] { "S1_02458.ogg" , "S1_02459.ogg" , "S1_02444.ogg" , "S1_02460.ogg" },
                new string[] { "S1_02458.ogg" , "S1_02459.ogg" , "S1_02444.ogg" , "S1_02460.ogg" },
                new string[] { "S1_02458.ogg" , "S1_02459.ogg" , "S1_02444.ogg" , "S1_02460.ogg" },
                new string[] { "S1_02455.ogg" , "S1_02440.ogg" , "S1_02457.ogg" , "S1_02442.ogg" }
                };
            public string[][] sLoopVoice30MukuFera = new string[][] {
                new string[] { "S2_01299.ogg" , "S2_01300.ogg" , "S2_01285.ogg" , "S2_01301.ogg" },
                new string[] { "S2_01299.ogg" , "S2_01300.ogg" , "S2_01285.ogg" , "S2_01301.ogg" },
                new string[] { "S2_01299.ogg" , "S2_01300.ogg" , "S2_01285.ogg" , "S2_01301.ogg" },
                new string[] { "S2_01299.ogg" , "S2_01300.ogg" , "S2_01285.ogg" , "S2_01301.ogg" },
                new string[] { "S2_01296.ogg" , "S2_01281.ogg" , "S2_01298.ogg" , "S2_01282.ogg" }
                };
#endif

            //-----------------------------------------------------------------------------------
            //カスタムボイス
            public string[][] sLoopVoice30Custom1 = new string[][] {
              new string[] { "N0_00421.ogg" , "N0_00422.ogg" , "N0_00423.ogg" },
              new string[] { "N0_00421.ogg" , "N0_00422.ogg" , "N0_00423.ogg" },
              new string[] { "N0_00421.ogg" , "N0_00422.ogg" , "N0_00423.ogg" },
              new string[] { "N0_00421.ogg" , "N0_00422.ogg" , "N0_00423.ogg" },
              new string[] { "N0_00435.ogg" , "N0_00449.ogg" }
              };
            public string[][] sLoopVoice30Custom2 = new string[][] {
              new string[] { "N7_00252.ogg" , "N7_00255.ogg" , "N7_00267.ogg" , "N7_00261.ogg" },
              new string[] { "N7_00252.ogg" , "N7_00255.ogg" , "N7_00267.ogg" , "N7_00261.ogg" },
              new string[] { "N7_00252.ogg" , "N7_00255.ogg" , "N7_00267.ogg" , "N7_00261.ogg" },
              new string[] { "N7_00252.ogg" , "N7_00255.ogg" , "N7_00267.ogg" , "N7_00261.ogg" },
              new string[] { "N7_00262.ogg" , "N7_00267.ogg" , "N7_00269.ogg" , "N7_00272.ogg" }
              };
            public string[][] sLoopVoice30Custom3 = new string[][] {
              new string[] { "N1_00183.ogg" , "N1_00195.ogg" , "N1_00323.ogg" , "N1_00330.ogg" },
              new string[] { "N1_00183.ogg" , "N1_00195.ogg" , "N1_00323.ogg" , "N1_00330.ogg" },
              new string[] { "N1_00183.ogg" , "N1_00195.ogg" , "N1_00323.ogg" , "N1_00330.ogg" },
              new string[] { "N1_00183.ogg" , "N1_00195.ogg" , "N1_00323.ogg" , "N1_00330.ogg" },
              new string[] { "N1_00170.ogg" , "N1_00191.ogg" , "N1_00192.ogg" , "N1_00194.ogg" }
              };
            public string[][] sLoopVoice30Custom4 = new string[][] {
              new string[] { "N3_00310.ogg" , "N3_00318.ogg" , "N3_00377.ogg" },
              new string[] { "N3_00310.ogg" , "N3_00318.ogg" , "N3_00377.ogg" },
              new string[] { "N3_00310.ogg" , "N3_00318.ogg" , "N3_00377.ogg" },
              new string[] { "N3_00310.ogg" , "N3_00318.ogg" , "N3_00377.ogg" },
              new string[] { "N3_00157.ogg" , "N3_00370.ogg" }
              };


            //　性格別声テーブル　絶頂時---------------------------------------------------------------
            //通常
            public string[][] sOrgasmVoice30PrideVibe = new string[][] {
              new string[] { "s0_01898.ogg" , "s0_01899.ogg" , "s0_01902.ogg" , "s0_01900.ogg" },
              new string[] { "s0_01913.ogg" , "s0_01918.ogg" , "s0_01919.ogg" , "s0_01917.ogg" },
              new string[] { "s0_09072.ogg" , "s0_09070.ogg" , "s0_09099.ogg" , "s0_09059.ogg" },
              new string[] { "s0_09067.ogg" , "s0_09068.ogg" , "s0_09069.ogg" , "s0_09071.ogg" , "s0_09085.ogg" , "s0_09086.ogg" , "s0_09087.ogg" , "s0_09091.ogg" },
              new string[] { "s0_01898.ogg" , "s0_01899.ogg" , "s0_01902.ogg" , "s0_01900.ogg" }
              };
            public string[][] sOrgasmVoice30CoolVibe = new string[][] {
              new string[] { "s1_03223.ogg" , "s1_03246.ogg" , "s1_03247.ogg" , "s1_03210.ogg" },
              new string[] { "s1_03214.ogg" , "s1_03215.ogg" , "s1_03216.ogg" , "s1_03209.ogg" },
              new string[] { "s1_03207.ogg" , "s1_03205.ogg" , "s1_08993.ogg" , "s1_08971.ogg" },
              new string[] { "s1_09344.ogg" , "s1_09370.ogg" , "s1_09371.ogg" , "s1_09372.ogg" , "s1_09374.ogg" , "s1_09398.ogg" , "s1_09392.ogg" , "s1_09365.ogg" },
              new string[] { "s1_03223.ogg" , "s1_03246.ogg" , "s1_03247.ogg" , "s1_03210.ogg" }
              };
            public string[][] sOrgasmVoice30PureVibe = new string[][] {
              new string[] { "s2_01478.ogg" , "s2_01477.ogg" , "s2_01476.ogg" , "s2_01475.ogg" },
              new string[] { "s2_01432.ogg" , "s2_01433.ogg" , "s2_01434.ogg" , "s2_01436.ogg" },
              new string[] { "s2_09039.ogg" , "s2_09067.ogg" , "s2_09052.ogg" , "s2_08502.ogg" },
              new string[] { "s2_09047.ogg" , "s2_09048.ogg" , "s2_09049.ogg" , "s2_09050.ogg" , "s2_09051.ogg" , "s2_09066.ogg" , "s2_09069.ogg" , "s2_09073.ogg" },
              new string[] { "s2_01478.ogg" , "s2_01477.ogg" , "s2_01476.ogg" , "s2_01475.ogg" }
              };
            public string[][] sOrgasmVoice30YandereVibe = new string[][] {
              new string[] { "s3_02908.ogg" , "s3_02950.ogg" , "s3_02923.ogg" , "s3_02932.ogg" },
              new string[] { "s3_02909.ogg" , "s3_02910.ogg" , "s3_02915.ogg" , "s3_02914.ogg" },
              new string[] { "s3_02905.ogg" , "s3_02906.ogg" , "s3_02907.ogg" , "s3_05540.ogg" },
              new string[] { "s3_05657.ogg" , "s3_05658.ogg" , "s3_05659.ogg" , "s3_05660.ogg" , "s3_05661.ogg" , "s3_05678.ogg" , "s3_05651.ogg" , "s3_05656.ogg" },
              new string[] { "s3_02908.ogg" , "s3_02950.ogg" , "s3_02923.ogg" , "s3_02932.ogg" }
              };
            public string[][] sOrgasmVoice30AnesanVibe = new string[][] {
              new string[] { "s4_08348.ogg" , "s4_08354.ogg" , "s4_08365.ogg" , "s4_08374.ogg" },
              new string[] { "s4_08345.ogg" , "s4_08346.ogg" , "s4_08349.ogg" , "s4_08350.ogg" },
              new string[] { "s4_08347.ogg" , "s4_08355.ogg" , "s4_08356.ogg" , "s4_11658.ogg" },
              new string[] { "s4_11684.ogg" , "s4_11677.ogg" , "s4_11680.ogg" , "s4_11683.ogg" , "s4_11661.ogg" , "s4_11659.ogg" , "s4_11654.ogg" , "s4_11660.ogg" },
              new string[] { "s4_08348.ogg" , "s4_08354.ogg" , "s4_08365.ogg" , "s4_08374.ogg" }
              };
            public string[][] sOrgasmVoice30GenkiVibe = new string[][] {
              new string[] { "s5_04264.ogg" , "s5_04258.ogg" , "s5_04256.ogg" , "s5_04255.ogg" },
              new string[] { "s5_04265.ogg" , "s5_04270.ogg" , "s5_04267.ogg" , "s5_04268.ogg" },
              new string[] { "s5_04266.ogg" , "s5_18375.ogg" , "s5_18380.ogg" , "s5_18393.ogg" },
              new string[] { "s5_18379.ogg" , "s5_18380.ogg" , "s5_18382.ogg" , "s5_18384.ogg" , "s5_18385.ogg" , "s5_18400.ogg" , "s5_18402.ogg" , "s5_18119.ogg" },
              new string[] { "s5_04264.ogg" , "s5_04258.ogg" , "s5_04256.ogg" , "s5_04255.ogg" }
              };
            public string[][] sOrgasmVoice30SadistVibe = new string[][] {
              new string[] { "s6_01744.ogg" , "s6_02700.ogg" , "s6_02450.ogg" , "s6_02357.ogg" },
              new string[] { "S6_28847.ogg" , "S6_28853.ogg" , "S6_28814.ogg" , "S6_02397.ogg" },
              new string[] { "S6_28817.ogg" , "S6_02398.ogg" , "S6_02399.ogg" , "s6_02402.ogg" },
              new string[] { "S6_09048.ogg" , "S6_01984.ogg" , "S6_01988.ogg" , "S6_01991.ogg" , "S6_02000.ogg" , "S6_01996.ogg" , "S6_01997.ogg" , "S6_01998.ogg" , "S6_01999.ogg" , "S6_02001.ogg" , "s6_05796.ogg" , "s6_05797.ogg" , "s6_05798.ogg" , "s6_05799.ogg" , "s6_05800.ogg" , "s6_05801.ogg" },
              new string[] { "s6_01744.ogg" , "s6_02700.ogg" , "s6_02450.ogg" , "s6_02357.ogg" }
              };

#if COM3D2
            // オダメ追加分
            public string[][] sOrgasmVoice30RindereVibe = new string[][] {
              new string[] { "s0_01898.ogg" , "s0_01899.ogg" , "s0_01902.ogg" , "s0_01900.ogg" },
              new string[] { "s0_01913.ogg" , "s0_01918.ogg" , "s0_01919.ogg" , "s0_01917.ogg" },
              new string[] { "s0_09072.ogg" , "s0_09070.ogg" , "s0_09099.ogg" , "s0_09059.ogg" },
              new string[] { "s0_09067.ogg" , "s0_09068.ogg" , "s0_09069.ogg" , "s0_09071.ogg" , "s0_09085.ogg" , "s0_09086.ogg" , "s0_09087.ogg" , "s0_09091.ogg" },
              new string[] { "s0_01898.ogg" , "s0_01899.ogg" , "s0_01902.ogg" , "s0_01900.ogg" }
              };
            public string[][] sOrgasmVoice30MajimeVibe = new string[][] {
              new string[] { "s1_03223.ogg" , "s1_03246.ogg" , "s1_03247.ogg" , "s1_03210.ogg" },
              new string[] { "s1_03214.ogg" , "s1_03215.ogg" , "s1_03216.ogg" , "s1_03209.ogg" },
              new string[] { "s1_03207.ogg" , "s1_03205.ogg" , "s1_08993.ogg" , "s1_08971.ogg" },
              new string[] { "s1_09344.ogg" , "s1_09370.ogg" , "s1_09371.ogg" , "s1_09372.ogg" , "s1_09374.ogg" , "s1_09398.ogg" , "s1_09392.ogg" , "s1_09365.ogg" },
              new string[] { "s1_03223.ogg" , "s1_03246.ogg" , "s1_03247.ogg" , "s1_03210.ogg" }
              };
            public string[][] sOrgasmVoice30MukuVibe = new string[][] {
              new string[] { "s2_01478.ogg" , "s2_01477.ogg" , "s2_01476.ogg" , "s2_01475.ogg" },
              new string[] { "s2_01432.ogg" , "s2_01433.ogg" , "s2_01434.ogg" , "s2_01436.ogg" },
              new string[] { "s2_09039.ogg" , "s2_09067.ogg" , "s2_09052.ogg" , "s2_08502.ogg" },
              new string[] { "s2_09047.ogg" , "s2_09048.ogg" , "s2_09049.ogg" , "s2_09050.ogg" , "s2_09051.ogg" , "s2_09066.ogg" , "s2_09069.ogg" , "s2_09073.ogg" },
              new string[] { "s2_01478.ogg" , "s2_01477.ogg" , "s2_01476.ogg" , "s2_01475.ogg" }
              };
#endif

            //-----------------------------------------------------------------------------------
            //フェラ
            public string[][] sOrgasmVoice30PrideFera = new string[][] {
              new string[] { "S0_01922.ogg" , "S0_01920.ogg" , "S0_01921.ogg" },
              new string[] { "S0_01922.ogg" , "S0_01920.ogg" , "S0_01921.ogg" },
              new string[] { "S0_01922.ogg" , "S0_01920.ogg" , "S0_01921.ogg" },
              new string[] { "S0_11361.ogg" , "S0_01931.ogg" , "S0_11350.ogg" , "S0_11349.ogg" },
              new string[] { "S0_01922.ogg" , "S0_01920.ogg" , "S0_01921.ogg" }
              };
            public string[][] sOrgasmVoice30CoolFera = new string[][] {
              new string[] { "S1_03219.ogg" , "S1_03218.ogg" , "S1_03228.ogg" },
              new string[] { "S1_03219.ogg" , "S1_03218.ogg" , "S1_03228.ogg" },
              new string[] { "S1_03219.ogg" , "S1_03218.ogg" , "S1_03228.ogg" },
              new string[] { "S1_11440.ogg" , "S1_11429.ogg" , "S1_11952.ogg" , "S1_19221.ogg" },
              new string[] { "S1_03219.ogg" , "S1_03218.ogg" , "S1_03228.ogg" }
              };
            public string[][] sOrgasmVoice30PureFera = new string[][] {
              new string[] { "S2_01446.ogg" , "S2_01445.ogg" , "S2_01495.ogg" },
              new string[] { "S2_01446.ogg" , "S2_01445.ogg" , "S2_01495.ogg" },
              new string[] { "S2_01446.ogg" , "S2_01445.ogg" , "S2_01495.ogg" },
              new string[] { "S2_11371.ogg" , "S2_11370.ogg" , "S2_11358.ogg" , "S2_11347.ogg" },
              new string[] { "S2_01446.ogg" , "S2_01445.ogg" , "S2_01495.ogg" }
              };
            public string[][] sOrgasmVoice30YandereFera = new string[][] {
              new string[] { "S3_02919.ogg" , "S3_02918.ogg" , "S3_02928.ogg" },
              new string[] { "S3_02919.ogg" , "S3_02918.ogg" , "S3_02928.ogg" },
              new string[] { "S3_02919.ogg" , "S3_02918.ogg" , "S3_02928.ogg" },
              new string[] { "S3_03084.ogg" , "S3_03184.ogg" , "S3_03162.ogg" , "S3_18748.ogg" },
              new string[] { "S3_02919.ogg" , "S3_02918.ogg" , "S3_02928.ogg" }
              };
            public string[][] sOrgasmVoice30AnesanFera = new string[][] {
              new string[] { "S4_08359.ogg" , "S4_08358.ogg" , "S4_08368.ogg" },
              new string[] { "S4_08359.ogg" , "S4_08358.ogg" , "S4_08368.ogg" },
              new string[] { "S4_08359.ogg" , "S4_08358.ogg" , "S4_08368.ogg" },
              new string[] { "S4_05728.ogg" , "S4_05726.ogg" , "S4_05680.ogg" , "S4_05668.ogg" },
              new string[] { "S4_08359.ogg" , "S4_08358.ogg" , "S4_08368.ogg" }
              };
            public string[][] sOrgasmVoice30GenkiFera = new string[][] {
              new string[] { "s5_04271.ogg" , "s5_04272.ogg" , "s5_04273.ogg" },
              new string[] { "s5_04271.ogg" , "s5_04272.ogg" , "s5_04273.ogg" },
              new string[] { "s5_04271.ogg" , "s5_04272.ogg" , "s5_04273.ogg" },
              new string[] { "S5_07752.ogg" , "S5_07753.ogg" , "s5_04273.ogg" , "s5_04271.ogg" },
              new string[] { "s5_04271.ogg" , "s5_04272.ogg" , "s5_04273.ogg" }
              };
            public string[][] sOrgasmVoice30SadistFera = new string[][] {
              new string[] { "S6_28832.ogg" , "s6_02403.ogg" , "S6_28835.ogg" },
              new string[] { "S6_28835.ogg" , "s6_02403.ogg" , "s6_02404.ogg" },
              new string[] { "S6_28838.ogg" , "s6_02404.ogg" , "s6_02405.ogg" },
              new string[] { "S6_02420.ogg" , "S6_08109.ogg" , "S6_08112.ogg" , "S6_08114.ogg" , "s6_02404.ogg" , "s6_02405.ogg"  },
              new string[] { "S6_28832.ogg" , "s6_02403.ogg" , "S6_28835.ogg" }
              };

#if COM3D2
            // オダメ追加分
            public string[][] sOrgasmVoice30RindereFera = new string[][] {
              new string[] { "S0_01922.ogg" , "S0_01920.ogg" , "S0_01921.ogg" },
              new string[] { "S0_01922.ogg" , "S0_01920.ogg" , "S0_01921.ogg" },
              new string[] { "S0_01922.ogg" , "S0_01920.ogg" , "S0_01921.ogg" },
              new string[] { "S0_11361.ogg" , "S0_01931.ogg" , "S0_11350.ogg" , "S0_11349.ogg" },
              new string[] { "S0_01922.ogg" , "S0_01920.ogg" , "S0_01921.ogg" }
              };
            public string[][] sOrgasmVoice30MajimeFera = new string[][] {
              new string[] { "S1_03219.ogg" , "S1_03218.ogg" , "S1_03228.ogg" },
              new string[] { "S1_03219.ogg" , "S1_03218.ogg" , "S1_03228.ogg" },
              new string[] { "S1_03219.ogg" , "S1_03218.ogg" , "S1_03228.ogg" },
              new string[] { "S1_11440.ogg" , "S1_11429.ogg" , "S1_11952.ogg" , "S1_19221.ogg" },
              new string[] { "S1_03219.ogg" , "S1_03218.ogg" , "S1_03228.ogg" }
              };
            public string[][] sOrgasmVoice30MukuFera = new string[][] {
              new string[] { "S2_01446.ogg" , "S2_01445.ogg" , "S2_01495.ogg" },
              new string[] { "S2_01446.ogg" , "S2_01445.ogg" , "S2_01495.ogg" },
              new string[] { "S2_01446.ogg" , "S2_01445.ogg" , "S2_01495.ogg" },
              new string[] { "S2_11371.ogg" , "S2_11370.ogg" , "S2_11358.ogg" , "S2_11347.ogg" },
              new string[] { "S2_01446.ogg" , "S2_01445.ogg" , "S2_01495.ogg" }
              };
#endif

            //-----------------------------------------------------------------------------------
            //カスタムボイス
            public string[][] sOrgasmVoice30Custom1 = new string[][] {
              new string[] { "N0_00424.ogg" , "N0_00459.ogg" , "N0_00503.ogg" , "N0_00508.ogg" , "N0_00534.ogg" },
              new string[] { "N0_00424.ogg" , "N0_00459.ogg" , "N0_00503.ogg" , "N0_00508.ogg" , "N0_00534.ogg" },
              new string[] { "N0_00424.ogg" , "N0_00457.ogg" , "N0_00503.ogg" , "N0_00508.ogg" , "N0_00534.ogg" },
              new string[] { "N0_00456.ogg" , "N0_00457.ogg" , "N0_00458.ogg" , "N0_00534.ogg" , "N0_00288.ogg" , "N0_00292.ogg" , "N0_00293.ogg" },
              new string[] { "N0_00424.ogg" , "N0_00459.ogg" , "N0_00503.ogg" , "N0_00508.ogg" , "N0_00534.ogg" }
              };
            public string[][] sOrgasmVoice30Custom2 = new string[][] {
              new string[] { "N7_00251.ogg" , "N7_00267.ogg" , "N7_00275.ogg" , "N7_00276.ogg" , "N7_00280.ogg" },
              new string[] { "N7_00251.ogg" , "N7_00267.ogg" , "N7_00275.ogg" , "N7_00276.ogg" , "N7_00280.ogg" },
              new string[] { "N7_00251.ogg" , "N7_00267.ogg" , "N7_00275.ogg" , "N7_00276.ogg" , "N7_00280.ogg" },
              new string[] { "N7_00284.ogg" , "N7_00291.ogg" , "N7_00293.ogg" , "N7_00294.ogg" , "N7_00295.ogg" , "N7_00275.ogg" , "n7_00295.ogg" },
              new string[] { "N7_00251.ogg" , "N7_00267.ogg" , "N7_00275.ogg" , "N7_00276.ogg" , "N7_00280.ogg" }
              };
            public string[][] sOrgasmVoice30Custom3 = new string[][] {
              new string[] { "N1_00179.ogg" , "N1_00180.ogg" , "N1_00200.ogg" , "N1_00204.ogg" , "N1_00209.ogg" },
              new string[] { "N1_00179.ogg" , "N1_00180.ogg" , "N1_00200.ogg" , "N1_00204.ogg" , "N1_00209.ogg" },
              new string[] { "N1_00179.ogg" , "N1_00180.ogg" , "N1_00200.ogg" , "N1_00204.ogg" , "N1_00209.ogg" },
              new string[] { "N1_00179.ogg" , "N1_00180.ogg" , "N1_00198.ogg" , "N1_00199.ogg" , "N1_00205.ogg" , "N1_00217.ogg" , "N1_00333.ogg" },
              new string[] { "N1_00179.ogg" , "N1_00180.ogg" , "N1_00200.ogg" , "N1_00204.ogg" , "N1_00209.ogg" }
              };
            public string[][] sOrgasmVoice30Custom4 = new string[][] {
              new string[] { "N3_00193.ogg" , "N3_00194.ogg" , "N3_00195.ogg" , "N3_00330.ogg" , "N3_00378.ogg" },
              new string[] { "N3_00193.ogg" , "N3_00194.ogg" , "N3_00195.ogg" , "N3_00330.ogg" , "N3_00378.ogg" },
              new string[] { "N3_00193.ogg" , "N3_00194.ogg" , "N3_00195.ogg" , "N3_00330.ogg" , "N3_00378.ogg" },
              new string[] { "N3_00376.ogg" , "N3_00194.ogg" , "N3_00195.ogg" , "N3_00197.ogg" , "N3_00203.ogg" , "N3_00328.ogg" , "N3_00330.ogg" , "N3_00379.ogg" },
              new string[] { "N3_00193.ogg" , "N3_00194.ogg" , "N3_00195.ogg" , "N3_00330.ogg" , "N3_00378.ogg" }
              };


            //　性格別声テーブル　停止時
            public string[] sLoopVoice40PrideVibe = new string[] { "S0_01967.ogg", "S0_01967.ogg", "S0_01968.ogg", "S0_01969.ogg", "S0_01969.ogg" };
            public string[] sLoopVoice40CoolVibe = new string[] { "S1_03264.ogg", "S1_03264.ogg", "S1_03265.ogg", "S1_03266.ogg", "S1_03266.ogg" };
            public string[] sLoopVoice40PureVibe = new string[] { "s2_01491.ogg", "s2_01491.ogg", "s2_01492.ogg", "s2_01493.ogg", "s2_01493.ogg" };
            public string[] sLoopVoice40YandereVibe = new string[] { "S3_02964.ogg", "S3_02964.ogg", "S3_02965.ogg", "S3_02966.ogg", "S3_02966.ogg" };
            public string[] sLoopVoice40AnesanVibe = new string[] { "s4_08424.ogg", "s4_08426.ogg", "s4_08427.ogg", "s4_08428.ogg", "s4_08428.ogg" };
            public string[] sLoopVoice40GenkiVibe = new string[] { "s5_04127.ogg", "s5_04129.ogg", "s5_04131.ogg", "s5_04134.ogg", "s5_04134.ogg" };
            public string[] sLoopVoice40SadistVibe = new string[] { "s6_02477.ogg", "s6_02478.ogg", "s6_02479.ogg", "s6_02481.ogg", "s6_02480.ogg" };

#if COM3D2
            // オダメ追加分
            //案1 public string[] sLoopVoice40MukuVibe = new string[] { "H0_00337.ogg", "H0_00352.ogg", "H0_00338.ogg", "H0_00354.ogg", "H0_00339.ogg" };
            public string[] sLoopVoice40MukuVibe = new string[] { "H0_15027.ogg", "H0_00352_vd.ogg", "H0_00338.ogg", "H0_00354.ogg", "H0_00339.ogg" };
            //案1 public string[] sLoopVoice40MajimeVibe = new string[] { "H1_00509.ogg", "H1_00524.ogg", "H1_00510.ogg", "H1_00511.ogg", "H1_00526.ogg" };
            public string[] sLoopVoice40MajimeVibe = new string[] { "H1_00525_vd.ogg"/*"H1_08954.ogg"*/, "H1_00524_vd.ogg", "H1_00510.ogg", "H1_00526.ogg", "H1_00526.ogg" };
            //案1 public string[] sLoopVoice40RindereVibe = new string[] { "H2_00311.ogg", "H2_00326.ogg", "H2_00327.ogg", "H2_00313.ogg", "H2_00328.ogg" };
            public string[] sLoopVoice40RindereVibe = new string[] { "H2_00326_vd.ogg", "H2_00326_vd.ogg", "H2_00327.ogg", "H2_00328.ogg", "H2_00313.ogg" };
#endif

            public string[] sLoopVoice40Custom1 = new string[] { "N0_00460.ogg", "N0_00460.ogg", "N0_00460.ogg", "N0_00460.ogg", "N0_00460.ogg" };
            public string[] sLoopVoice40Custom2 = new string[] { "N7_00277.ogg", "N7_00277.ogg", "N7_00277.ogg", "N7_00277.ogg", "N7_00277.ogg" };
            public string[] sLoopVoice40Custom3 = new string[] { "N1_00382.ogg", "N1_00382.ogg", "N1_00382.ogg", "N1_00382.ogg", "N1_00382.ogg" };
            public string[] sLoopVoice40Custom4 = new string[] { "N3_00205.ogg", "N3_00205.ogg", "N3_00205.ogg", "N3_00205.ogg", "N3_00205.ogg" };
        }
#endregion

#endregion
        public static VibeYourMaidConfig cfg = new VibeYourMaidConfig();

        public static void Reset()
        {
            sFaceBackup.Clear();
            sFace3Backup.Clear();
            maidParam.Clear();
        }

        //メイドの音声再生処理
        public static void MaidVoicePlay(Maid vm, /*int Num, */int iExcite, XtMasterSlave.AnimeState.State motionState,
                            bool boMotionChanged, bool faceAnimeEnabled, bool VoiceEnabled, bool modeVYM, bool modeManual, XtMasterSlave.MsLinkConfig msCfg, ref bool VoicePlaying)
        {
            string sPersonal = vm.XtParam().status.personal.ToString();
            string[] VoiceList = new string[1];
            int vi = 0;

            if (!maidParam.ContainsKey(vm))
                maidParam.Add(vm, new vMaidParam());

            var vOrgasmCmb = maidParam[vm].vOrgasmCmb;
            var iExcite_Old = maidParam[vm].iExcite_Old;

            //Console.WriteLine(sPersonal);
            iCurrentExcite = iExcite * 60;
            OrgasmVoice = 0;
            vMaidStun = false;

            //　興奮度の判定
            if (iCurrentExcite < cfg.vExciteLevelThresholdV1 * 60)
            {
                vExciteLevel = 1;
            }
            else if (cfg.vExciteLevelThresholdV1 * 60 <= iCurrentExcite && iCurrentExcite < cfg.vExciteLevelThresholdV2 * 60)
            {
                vExciteLevel = 2;
            }
            else if (cfg.vExciteLevelThresholdV2 * 60 <= iCurrentExcite && iCurrentExcite < cfg.vExciteLevelThresholdV3 * 60)
            {
                vExciteLevel = 3;
            }
            else if (cfg.vExciteLevelThresholdV3 * 60 <= iCurrentExcite)
            {
                vExciteLevel = 4;
            }

            vStateMajor = 20;
            if (iExcite >= cfg.vStateMajor30Threshold)
            {
                vStateMajor = 30;
            }

            //モーションによるテーブル操作
            int ModeSelect = 0;
            if ((motionState & XtMasterSlave.AnimeState.State.kiss) != 0)
            {
                ModeSelect = 1;
            }
            if ((motionState & XtMasterSlave.AnimeState.State.zeccho) != 0)
            {
                OrgasmVoice = 1;
            }
#if false//設定ファイルに応じて変更するようにした
            /*
                * else if (motionState == XtMasterSlave.AnimeState.State.sex)
            {
                vExciteLevel -= 1;
                if (vExciteLevel < 1)
                    vExciteLevel = 1;
                ModeSelect = 0;
            }
            else if (motionState == XtMasterSlave.AnimeState.State.yoin)
            {
                ModeSelect = 0;
                //vMaidStun = true;
                vStateMajor = 40;
            }
            else if (motionState == XtMasterSlave.AnimeState.State.zeccho)
            {
                ModeSelect = 0;
                OrgasmVoice = 1;
                vStateMajor = 30;
            }
            else if (motionState == XtMasterSlave.AnimeState.State.taiki)
            {
                vExciteLevel = 1;
                vStateMajor = 10;
            }
            else //他
            {
                //責め側なので喘ぎを抑制
                vExciteLevel -= 1;
                if (vExciteLevel < 1)
                    vExciteLevel = 1;
                vStateMajor = 40;
            }*/
#endif
            //モーションカテゴリ別のボイステーブル操作
            int v;
            var mCate = motionState;

            //絶頂系フラグとkissフラグの重複解消
            if ((mCate & XtMasterSlave.AnimeState.State.zeccho) != 0)
                mCate = XtMasterSlave.AnimeState.State.zeccho;
            if ((mCate & XtMasterSlave.AnimeState.State.yoin) != 0)
                mCate = XtMasterSlave.AnimeState.State.yoin;

            int lvCorrect = 0;
            if (XtMasterSlave.ycfg.MotionEffect_ExciteLevelSift.TryGetValue(mCate.ToString(), out v))
            {
                vExciteLevel += v;
                lvCorrect = v;
            }

            int stateLock = 0; // v5.0
            if (XtMasterSlave.ycfg.MotionEffect_StateMajorSwitch.TryGetValue(mCate.ToString(), out v))
            {
                if (v != 0)
                {
                    vStateMajor = v;

                    //if (mCate != XtMasterSlave.AnimeState.State.uke && mCate != XtMasterSlave.AnimeState.State.sex)
                    //    stateLock = v;
                    stateLock = v;
                }
            }

            //vOrgasmCmb = 0;
            if (modeVYM && !modeManual)
            {
                //バイブ状態
                int i_VLevel = VYM.API.obj2int(VYM.API.GetVYM_Value(VYM.API.VYM_IO_ID.i_VLevel));
                //メイド状態
                int i_vb_state_m = VYM.API.obj2int(VYM.API.GetVYM_Value(VYM.API.VYM_IO_ID.i_vStateMajor));
                //if (i_vb_state_m > 0)
                //    vStateMajor = i_vb_state_m;

                vOrgasmCmb = VYM.API.obj2int(VYM.API.GetVYM_Value(VYM.API.VYM_IO_ID.i_vOrgasmCmb));
                if (vOrgasmCmb < 0)
                    vOrgasmCmb = 0;

                if (msCfg.doVoiceAndFacePlayOnVYM_Zeccho)
                {
                    if (mCate == XtMasterSlave.AnimeState.State.sex
                        || mCate == XtMasterSlave.AnimeState.State.uke
                        || mCate == XtMasterSlave.AnimeState.State.kiss)
                    {
                        OrgasmVoice = (VYM.API.obj2int(VYM.API.GetVYM_Value(VYM.API.VYM_IO_ID.i_OrgasmVoice)) >= 1) ? 1 : 0;
                    }

                    if (OrgasmVoice != 0)
                    {
                        debugPrintConsole("xtms+ VYM絶頂あり");
                        if (vm.AudioMan.audiosource.loop || (!vm.AudioMan.audiosource.loop && !vm.AudioMan.audiosource.isPlaying))
                            boMotionChanged = true; //割り込み用
                    }
                }
                /*
                if (mCate == XtMasterSlave.AnimeState.State.sex
                    || mCate == XtMasterSlave.AnimeState.State.uke
                    || mCate == XtMasterSlave.AnimeState.State.kiss)
                {
                    OrgasmVoice = (VYM.API.obj2int(VYM.API.GetVYM_Value(VYM.API.VYM_IO_ID.i_OrgasmVoice)) == 1) ? 1 : 0;
                }*/
                if (stateLock <= 0)
                {
                    if (i_vb_state_m > 0)
                    {
                        vStateMajor = i_vb_state_m;
                        
                        if (mCate != XtMasterSlave.AnimeState.State.uke)
                        {
                            // 受け以外ではmasterはスタンしない
                            vMaidStun = VYM.API.obj2bool(VYM.API.GetVYM_Value(VYM.API.VYM_IO_ID.b_vMaidStun));
                            /*if (vStateMajor == 40)
                            {
                                //　バイブステートの変更
                                if (i_VLevel == 2)
                                { //　「バイブ強」
                                    vStateMajor = 30;
                                }
                                else if (i_VLevel == 1)
                                { //　「バイブ弱」
                                    vStateMajor = 20;
                                }
                                else
                                { //　「バイブ停止」
                                    vStateMajor = 10;
                                }
                            }*/
                        }
                    }
                }

                if (i_VLevel == 0 && i_vb_state_m <= 10)
                {
                    //VYM停止中に他のプラグインなどのモーション変更に反応しないように
                    OrgasmVoice = 0;
                    ModeSelect = 0;
                    vExciteLevel = 1;
                    mCate = XtMasterSlave.AnimeState.State.taiki;
                    vStateMajor = 10;
                }

                if (vStateMajor > 10)
                {
                    //フェイスバックアップ
                    sFaceBackup[vm] = vm.ActiveFace;
                    sFace3Backup[vm] = vm.FaceName3;
                }
                else if (vStateMajor == 10)
                {
                    if (!VoicePlaying)
                    {
                        //フェイス復元
                        if (sFaceBackup.ContainsKey(vm))
                        {
                            vm.FaceAnime(sFaceBackup[vm], 1, 0);
                            sFaceBackup.Remove(vm);
                        }
                        if (sFace3Backup.ContainsKey(vm))
                        {
                            vm.FaceBlend(sFace3Backup[vm]);
                            sFace3Backup.Remove(vm);
                        }
                        return;
                    }
                }
            }//vym_mode
            else
            {
                if (OrgasmVoice == 1 && boMotionChanged)
                {
                    vOrgasmCmb++;
                    if (vOrgasmCmb > 1000)
                        vOrgasmCmb = 1000;
                }
            }

            //バイブ状態マニュアルモード
            if (modeManual)
            {
                vStateMajor = msCfg.manuVf_mState;
                if (OrgasmVoice != 0)
                    vStateMajor = 30;
            }
            //コンボ数マニュアルモード
            if (msCfg.manuVf_mOrgcmb >= 0)
            {
                vOrgasmCmb = msCfg.manuVf_mOrgcmb;
            }

            if (vExciteLevel < 1)
            {
                vExciteLevel = 1;
            }
            else if (vExciteLevel >= 100)
            {
                //裏機能
                vMaidStun = true;
                vExciteLevel = 4;
            }
            else if (vExciteLevel > 4)
            {
                vExciteLevel = 4;
            }

#if DEBUG
            if (Time.frameCount % 180 == 0) //60フレームに1回表示
                debugPrintConsole(string.Format("モーションカテゴリ:{0}, ELv:{1}, SM:{2}, Stun:{3}", motionState, vExciteLevel, vStateMajor, vMaidStun));
#endif
            // v0027用
            string sPersonalEx = sPersonal;
            int modeOrg = ModeSelect;
            NewVoiceTable.VoiceSet voiceSet = null;

            //ユーザー設定によるボイステーブル固定
            switch (cfg.eVoiceMode)
            {
                case VoiceMode.オートモード:
                    // オートモードならスキップ 
                    break;

                case VoiceMode.通常固定:
                    ModeSelect = 0;
                    break;
                case VoiceMode.舐め固定:
                    ModeSelect = 1;
                    break;
                case VoiceMode.カスタム1:
                    ModeSelect = 2;
                    break;
                case VoiceMode.カスタム2:
                    ModeSelect = 3;
                    break;
                case VoiceMode.カスタム3:
                    ModeSelect = 4;
                    break;
                case VoiceMode.カスタム4:
                    ModeSelect = 5;
                    break;
            }

            // v0027拡張
            if (ModeSelect >= 2)
            {
                sPersonalEx = "Custom" + (ModeSelect - 1).ToString();
                ModeSelect = modeOrg;
            }

            // v0027ボイス選定位置移動元

            bool boStateChanged = false;

            //VYM本体より
            bool bAllowVoiceOverrideV = false;
            //　ループ音声を再生中、もしくは一回再生音声が再生済みなら介入してよい
            if (vm.AudioMan.audiosource.loop || (!vm.AudioMan.audiosource.loop && !vm.AudioMan.audiosource.isPlaying))
            {
                //m_sLastLoopFN = vm.AudioMan.FileName;
                //m_sLastMotionFN = vm.body0.LastAnimeFN;
                //debugPrintConsole("書き戻し用のループ音声を保持： " + m_sLastLoopFN + " モーション:" + m_sLastMotionFN);

                //m_iLoopWaitCnt = 0;
                bAllowVoiceOverrideV = true;
            }
            if (!vm.AudioMan.audiosource.loop && !vm.AudioMan.audiosource.isPlaying || (VoiceEnabled && !VoicePlaying)) //→ループ音声停止中も割り込みを入れてみる(VoiceEnabled判定追加v0025)
            { // 一回再生音声が停止中
                /*m_iLoopWaitCnt++;
                if (m_iLoopWaitCnt > 20) //20フレーム待ってもループ音声がなければ介入
                {
                    debugPrintConsole("ループウェイトカウントオーバー m_iLoopWaitCnt = " + m_iLoopWaitCnt);
                    m_iLoopWaitCnt = 0;
                    bAllowVoiceOverrideV = true;
                }*/

                if (VoiceEnabled) // v0027 bugfix
                    boStateChanged = true;//一回音声再生後はモーション変更同様に音声を切換え
            }

            //VYMからの興奮値計算を変更したので不要に if (!modeVYM) //VYMでは興奮度変化が大きいのとモーション変更頻度が多いので除外
            {
                //興奮度がある程度変わった場合もボイス更新する
                if (modeVYM)
                {
                    if (modeManual)
                    {
                        if (maidParam[vm].vStateMajor_Old != vStateMajor || maidParam[vm].vOrgasmCmb != vOrgasmCmb)
                            boStateChanged = true;
                    }
                    else
                    {
                        if (maidParam[vm].vStateMajor_Old != vStateMajor)
                            boStateChanged = true;
                    }
                }
                else if (modeManual)
                {
                    if (Math.Abs(iExcite_Old - iExcite) > 10 || maidParam[vm].vStateMajor_Old != vStateMajor || maidParam[vm].vOrgasmCmb != vOrgasmCmb)
                        boStateChanged = true;
                }
                else
                {
                    if (Math.Abs(iExcite_Old - iExcite) > 10 || maidParam[vm].vStateMajor_Old != vStateMajor)
                        boStateChanged = true;
                }
                //前回値の保持（毎回）
                maidParam[vm].vOrgasmCmb = vOrgasmCmb;
            }

            //絶頂音声以外は基本モーション変更のタイミングでのみ切り替える
            bAllowVoiceOverrideV = (bAllowVoiceOverrideV && (boMotionChanged || boStateChanged))
                                    || (OrgasmVoice > 0 && boMotionChanged); //絶頂音声は割り込み可に
            if (!bAllowVoiceOverrideV)
            {
                //VoicePlaying = false;
                return;
            }

            //bool boExciteChanged = iExcite_Old != iExcite;

            //前回値の保持（ボイス再生毎）
            iExcite_Old = iExcite;
            maidParam[vm].iExcite_Old = iExcite_Old;
            maidParam[vm].vStateMajor_Old = vStateMajor;

            //Console.WriteLine("iExcite {0}, vExciteLevel {1}, ModeSelect {2}, vStateMajor {3}", iExcite, vExciteLevel, ModeSelect, vStateMajor);
            //debugPrintConsole(string.Format("bAllowVoiceOverrideV {0}, boMotionChanged {1}, OrgasmVoice {2}", bAllowVoiceOverrideV, boMotionChanged, OrgasmVoice));

            //音声変更可能なタイミングでフェイスも変更
            if (bAllowVoiceOverrideV && faceAnimeEnabled) //フェイスアニメ有効チェック
            {
                FaceAnime(vm, 0, vStateMajor, vOrgasmCmb);

                //bool force = modeManual || (vStateMajor <= 10 && iExcite == 0 && boExciteChanged); //停止状態で興奮0ならブレンドリセット
                FaceBlend(vm, vOrgasmCmb, true); //常に上書き
            }

            if (!VoiceEnabled)
            {
                if (VoicePlaying)
                {
                    VoicePlaying = false;
                    vm.AudioMan.Stop(0f);
                }
                return;
            }

            //ボイス再生フラグ
            VoicePlaying = true;

            // v0027ボイス選定位置移動
            if (VoiceEnabled)
            {
                //バイブ弱の音声
                if (vStateMajor == 20)
                {
                    if (vMaidStun)
                    {
                        vi = 4;
                    }
                    else
                    {
                        vi = vExciteLevel - 1;
                    }

                    if (ModeSelect == 0)
                    { //通常音声
                        voiceSet = NewVoiceTable.GetVoiceSet(
                            VoiceType: NewVoiceTable.VoiceSet.VoiceType.LoopVoice,
                            State: 20,
                            Personal: sPersonalEx,
                            Level: vi);
                    }
                    else if (ModeSelect == 1)
                    { //フェラ音声
                        voiceSet = NewVoiceTable.GetVoiceSet(
                            VoiceType: NewVoiceTable.VoiceSet.VoiceType.LoopVoiceFera,
                            State: 20,
                            Personal: sPersonalEx,
                            Level: vi);
                    }
                }

                //バイブ強の音声
                if (vStateMajor == 30)
                {
                    if (OrgasmVoice == 0)
                    {
                        if (vMaidStun)
                        {
                            vi = 4;
                        }
                        else
                        {
                            vi = vExciteLevel - 1;
                        }

                        if (ModeSelect == 0)
                        { //通常音声
                            voiceSet = NewVoiceTable.GetVoiceSet(
                                VoiceType: NewVoiceTable.VoiceSet.VoiceType.LoopVoice,
                                State: 30,
                                Personal: sPersonalEx,
                                Level: vi);
                        }
                        else if (ModeSelect == 1)
                        { //フェラ音声
                            voiceSet = NewVoiceTable.GetVoiceSet(
                                VoiceType: NewVoiceTable.VoiceSet.VoiceType.LoopVoiceFera,
                                State: 30,
                                Personal: sPersonalEx,
                                Level: vi);
                        }
                    }
                    else
                    { //絶頂時音声
                        if (vMaidStun)
                        {
                            //放心中の絶頂時音声
                            vi = 4;
                        }
                        else if (vOrgasmCmb < 4)
                        {
                            vi = vExciteLevel - 2;
                            if (vi < 0) vi = 0;
                        }
                        else
                        {
                            vi = 3;
                        }

                        if (ModeSelect == 0)
                        { //通常音声
                            voiceSet = NewVoiceTable.GetVoiceSet(
                                VoiceType: NewVoiceTable.VoiceSet.VoiceType.OrgasmVoice,
                                State: 30,
                                Personal: sPersonalEx,
                                Level: vi);

                        }
                        else if (ModeSelect == 1)
                        { //フェラ音声
                            voiceSet = NewVoiceTable.GetVoiceSet(
                                VoiceType: NewVoiceTable.VoiceSet.VoiceType.OrgasmVoiceFera,
                                State: 30,
                                Personal: sPersonalEx,
                                Level: vi);
                        }
                    }
                }

                // v0027 ボイスセット→ボイスリスト 
                if (voiceSet != null && voiceSet.Files.Length > 0)
                {
                    VoiceList = voiceSet.Files;
                }

            }

            int iRandomVoice = UnityEngine.Random.Range(0, VoiceList.Length);
            if (OrgasmVoice != 0)
            {
                //while (iRandomVoice == iRandomVoiceBackup[vi] && VoiceList.Length > 1)
                while (iRandomVoiceBackup[vi].ContainsKey(vm) && iRandomVoice == iRandomVoiceBackup[vi][vm] && VoiceList.Length > 1)
                {
                    iRandomVoice = UnityEngine.Random.Range(0, VoiceList.Length);
                }
                //iRandomVoiceBackup[vi] = iRandomVoice;
                iRandomVoiceBackup[vi][vm] = iRandomVoice;
            }
            //debugPrintConsole("MaidVoicePlay " + VoiceList[iRandomVoice]);

            //バイブ動作時の音声を実際に再生する
            if (vStateMajor == 20)
            {
                vm.AudioMan.LoadPlay(VoiceList[iRandomVoice], 0f, false, true);
            }
            if (vStateMajor == 30)
            {
                if (OrgasmVoice == 0)
                {
                    vm.AudioMan.LoadPlay(VoiceList[iRandomVoice], 0f, false, true);
                }
                else
                {
                    vm.AudioMan.LoadPlay(VoiceList[iRandomVoice], 0f, false, false);
                    OrgasmVoice = 2;   //絶頂音声再生中のフラグ

                    //int index1 = Array.IndexOf(Edit_MaidsNum, Num);
                    //if (index1 != -1) vsFlag[index1] = 0;
                }
            }

            if (vStateMajor == 10)
            {
                VoicePlaying = false;
                vm.AudioMan.Stop(0.7f);
            }

            //バイブ停止時の音声
            if (vStateMajor == 40)
            {
                int VoiceValue;

                if (vMaidStun)
                {
                    vi = 1;
                }
                else
                {
                    vi = 0;
                }

                if (vOrgasmCmb > 0)
                {
                    VoiceValue = 3 + vi;
                }
                else
                {
                    VoiceValue = vExciteLevel - 1 + vi;
                }

                // v0027
                voiceSet = NewVoiceTable.GetVoiceSet(
                            VoiceType: NewVoiceTable.VoiceSet.VoiceType.LoopVoice,
                            State: 40,
                            Personal: sPersonalEx,
                            Level: VoiceValue);
                if (voiceSet != null)
                {
                    int cnt = voiceSet.Files.Length;
                    if (cnt > 1)
                        cnt = UnityEngine.Random.Range(0, cnt - 1);
                    else
                        cnt = 0;

                    vm.AudioMan.LoadPlay(voiceSet.Files[cnt], 0f, false, true);
                }

                //int index1 = Array.IndexOf(Edit_MaidsNum, Num);
                //if (index1 != -1) vsFlag[index1] = 0;
            }

        }

        public static void FaceAnime(Maid maid, int vStateHoldTime, int vState, int vOrgasmCmb)
        {

            //　バイブフェイスアニメの適用
            bool bAllowChangeFaceAnime = false;

            //　遷移直後かカウンタリセット時のタイミングで適用
            if ((vStateHoldTime <= 0)
                //|| (vStateMajor == 20 && vStateHoldTime >= vStateAltTime1)
                //|| (vStateMajor == 40 && vStateHoldTime >= vStateAltTime1)
                )
            {
                bAllowChangeFaceAnime = true;
            }

            int iRandomFace = 0;
            if (bAllowChangeFaceAnime)
            {
                string sFaceAnimeName = "";

                if (vMaidStun)
                {
                    iRandomFace = UnityEngine.Random.Range(0, cfg.sFaceAnimeStun.Length);
                    sFaceAnimeName = cfg.sFaceAnimeStun[iRandomFace];

                }
                else if (vState == 20)
                {
                    iRandomFace = UnityEngine.Random.Range(0, cfg.sFaceAnime20Vibe[vExciteLevel - 1].Length);
                    sFaceAnimeName = cfg.sFaceAnime20Vibe[vExciteLevel - 1][iRandomFace];

                }
                else if (vState == 40)
                {
                    if (vOrgasmCmb > 0)
                    {
                        sFaceAnimeName = cfg.sFaceAnime40Vibe[3];
                    }
                    else
                    {
                        sFaceAnimeName = cfg.sFaceAnime40Vibe[vExciteLevel - 1];
                    }

                }
                else if (vState == 30)
                {
                    iRandomFace = UnityEngine.Random.Range(0, cfg.sFaceAnime30Vibe[vExciteLevel - 1].Length);
                    sFaceAnimeName = cfg.sFaceAnime30Vibe[vExciteLevel - 1][iRandomFace];
                }
                else if (vState == 10 && maidParam[maid].faceanimeChanged)
                {
                    if (maid.ActiveFace != cfg.sFaceAnimeYotogiDefault)
                        sFaceAnimeName = cfg.sFaceAnimeYotogiDefault;

                    maidParam[maid].faceanimeChanged = false;
                }

                //　""か"変更しない"でなければ、フェイスアニメを適用する
                if (sFaceAnimeName != "" && sFaceAnimeName != "変更しない")
                {
                    maid.FaceAnime(sFaceAnimeName, cfg.fAnimeFadeTimeV, 0);
                    if (vState != 10)
                        maidParam[maid].faceanimeChanged = true;
                }

            }
        }

        public static void FaceBlend(Maid maid, int vOrgasmCmb, bool force)
        {
            //　フェイスブレンドの適用
            //　ステートに応じたフェイスブレンドに上書きする。
            //　ただし、より強いものが適用されるなら、そちらを尊重して上書きしない

            string sFaceBlendCurrent = "";//maid.FaceName3;
            if (!force)
                sFaceBlendCurrent = maid.FaceName3;
            sFaceBlendCurrent = sFaceBlendCurrent.Replace("オリジナル", ""); //取得したフェイスブレンド情報から「オリジナル」の記述を削除


            if (sFaceBlendCurrent == "") sFaceBlendCurrent = "頬０涙０";  // 背景選択時、スキル選択時は、"" が返ってきてエラーが出るため

            string sCurrentCheek = "";
            string sCurrentTears = "";
            int iCurrentCheek = 0;
            int iCurrentTears = 0;
            bool bCurrentYodare = false;

            string sChangeCheek = "";
            string sChangeTears = "";
            int iChangeCheek = 0;
            int iChangeTears = 0;
            string sChangeYodare = "";
            string sChangeBlend = "";

            int iOverrideCheek = 0;
            int iOverrideTears = 0;
            bool bOverrideYodare = false;


            //　興奮度によってフェイスブレンドを変更する
            if (vExciteLevel == 1)
            {
                iOverrideCheek = 1;     //"頬１"
                iOverrideTears = 1;     //"涙１"

            }
            else if (vExciteLevel == 2)
            {
                iOverrideCheek = 2;     //"頬２"
                iOverrideTears = 1;     //"涙１"

            }
            else if (vExciteLevel == 3)
            {
                iOverrideCheek = 3;     //"頬３"
                iOverrideTears = 2;     //"涙２"

            }
            else if (vExciteLevel == 4)
            {
                iOverrideCheek = 3;     //"頬３"
                iOverrideTears = 3;     //"涙３"

            }

            //　よだれ（興奮レベルが一定以上の時にだけよだれをつける）
            if (cfg.iYodareAppearLevelV != 0 && vExciteLevel >= cfg.iYodareAppearLevelV)
            {
                bOverrideYodare = true;
            }
            else if (vOrgasmCmb > 0 || vMaidStun)
            {
                bOverrideYodare = true;
            }
            else
            {
                bOverrideYodare = false;
            }

            //　元々のフェイスブレンドと比較する
            sCurrentCheek = sFaceBlendCurrent.Substring(0, 2);
            sCurrentTears = sFaceBlendCurrent.Substring(2, 2);
            if (sFaceBlendCurrent.Length == 7) bCurrentYodare = true;
            if (force)
            {
                iChangeCheek = iOverrideCheek;
                iChangeTears = iOverrideTears;
                if (bOverrideYodare) sChangeYodare = "よだれ";
            }
            else
            {
                if (sCurrentCheek == "頬０") iCurrentCheek = 0;
                if (sCurrentCheek == "頬１") iCurrentCheek = 1;
                if (sCurrentCheek == "頬２") iCurrentCheek = 2;
                if (sCurrentCheek == "頬３") iCurrentCheek = 3;
                iChangeCheek = iCurrentCheek;
                if (iOverrideCheek > iChangeCheek) iChangeCheek = iOverrideCheek;

                if (sCurrentTears == "涙０") iCurrentTears = 0;
                if (sCurrentTears == "涙１") iCurrentTears = 1;
                if (sCurrentTears == "涙２") iCurrentTears = 2;
                if (sCurrentTears == "涙３") iCurrentTears = 3;
                iChangeTears = iCurrentTears;
                if (iOverrideTears > iChangeTears) iChangeTears = iOverrideTears;

                if (bCurrentYodare || bOverrideYodare) sChangeYodare = "よだれ";
            }

            //頬
            if (iChangeCheek == 0) sChangeCheek = "頬０";
            if (iChangeCheek == 1) sChangeCheek = "頬１";
            if (iChangeCheek == 2) sChangeCheek = "頬２";
            if (iChangeCheek == 3) sChangeCheek = "頬３";
            //涙
            if (iChangeTears == 0) sChangeTears = "涙０";
            if (iChangeTears == 1) sChangeTears = "涙１";
            if (iChangeTears == 2) sChangeTears = "涙２";
            if (iChangeTears == 3) sChangeTears = "涙３";

            //設定により各ブレンドを除外
            if (!cfg.HohoEnabled) sChangeCheek = sCurrentCheek;
            if (!cfg.NamidaEnabled) sChangeTears = sCurrentTears;
            if (!cfg.YodareEnabled)
            {
                if (bCurrentYodare) sChangeYodare = "よだれ";
                if (!bCurrentYodare) sChangeYodare = "";
            }

            sChangeBlend = sChangeCheek + sChangeTears + sChangeYodare;

            //メインメイドにフェイスブレンド適用
            maid.FaceBlend(sChangeBlend);
        }

#region 旧音声ルーチン
#if false
        
        //メイドの音声再生処理
        private void MaidVoicePlay(Maid vm, int Num)
        {

            //フェラしているかのチェック
            checkBlowjobing(vm, Num);
            
            string sPersonal = vm.Param.status.personal.ToString();
            string[] VoiceList = new string[1];
            int vi = 0;

            //Console.WriteLine(sPersonal);

            //バイブ弱の音声
            if (vStateMajor == 20)
            {
                if (vMaidStun)
                {
                    vi = 4;
                }
                else
                {
                    vi = vExciteLevel - 1;
                }

                if (ModeSelect == 0)
                { //通常音声
                    if (sPersonal == "Pure")
                    {
                        VoiceList = cfg.sLoopVoice20PureVibe[vi];
                    }
                    else if (sPersonal == "Cool")
                    {
                        VoiceList = cfg.sLoopVoice20CoolVibe[vi];
                    }
                    else if (sPersonal == "Pride")
                    {
                        VoiceList = cfg.sLoopVoice20PrideVibe[vi];
                    }
                    else if (sPersonal == "Yandere")
                    {
                        VoiceList = cfg.sLoopVoice20YandereVibe[vi];
                    }
                    else if (sPersonal == "Anesan")
                    {
                        VoiceList = cfg.sLoopVoice20AnesanVibe[vi];
                    }
                    else if (sPersonal == "Genki")
                    {
                        VoiceList = cfg.sLoopVoice20GenkiVibe[vi];
                    }
                    else if (sPersonal == "Sadist")
                    {
                        VoiceList = cfg.sLoopVoice20SadistVibe[vi];
                    }
                }
                else if (ModeSelect == 1)
                { //フェラ音声
                    if (sPersonal == "Pure")
                    {
                        VoiceList = cfg.sLoopVoice20PureFera[vi];
                    }
                    else if (sPersonal == "Cool")
                    {
                        VoiceList = cfg.sLoopVoice20CoolFera[vi];
                    }
                    else if (sPersonal == "Pride")
                    {
                        VoiceList = cfg.sLoopVoice20PrideFera[vi];
                    }
                    else if (sPersonal == "Yandere")
                    {
                        VoiceList = cfg.sLoopVoice20YandereFera[vi];
                    }
                    else if (sPersonal == "Anesan")
                    {
                        VoiceList = cfg.sLoopVoice20AnesanFera[vi];
                    }
                    else if (sPersonal == "Genki")
                    {
                        VoiceList = cfg.sLoopVoice20GenkiFera[vi];
                    }
                    else if (sPersonal == "Sadist")
                    {
                        VoiceList = cfg.sLoopVoice20SadistFera[vi];
                    }
                }
                else if (ModeSelect == 2)
                { //カスタム音声１
                    VoiceList = cfg.sLoopVoice20Custom1[vi];
                }
                else if (ModeSelect == 3)
                { //カスタム音声２
                    VoiceList = cfg.sLoopVoice20Custom2[vi];
                }
                else if (ModeSelect == 4)
                { //カスタム音声３
                    VoiceList = cfg.sLoopVoice20Custom3[vi];
                }
                else if (ModeSelect == 5)
                { //カスタム音声４
                    VoiceList = cfg.sLoopVoice20Custom4[vi];
                }

            }

            //バイブ強の音声
            if (vStateMajor == 30)
            {
                if (OrgasmVoice == 0)
                {

                    if (vMaidStun)
                    {
                        vi = 4;
                    }
                    else
                    {
                        vi = vExciteLevel - 1;
                    }

                    if (ModeSelect == 0)
                    { //通常音声
                        if (sPersonal == "Pure")
                        {
                            VoiceList = cfg.sLoopVoice30PureVibe[vi];
                        }
                        else if (sPersonal == "Cool")
                        {
                            VoiceList = cfg.sLoopVoice30CoolVibe[vi];
                        }
                        else if (sPersonal == "Pride")
                        {
                            VoiceList = cfg.sLoopVoice30PrideVibe[vi];
                        }
                        else if (sPersonal == "Yandere")
                        {
                            VoiceList = cfg.sLoopVoice30YandereVibe[vi];
                        }
                        else if (sPersonal == "Anesan")
                        {
                            VoiceList = cfg.sLoopVoice30AnesanVibe[vi];
                        }
                        else if (sPersonal == "Genki")
                        {
                            VoiceList = cfg.sLoopVoice30GenkiVibe[vi];
                        }
                        else if (sPersonal == "Sadist")
                        {
                            VoiceList = cfg.sLoopVoice30SadistVibe[vi];
                        }
                    }
                    else if (ModeSelect == 1)
                    { //フェラ音声
                        if (sPersonal == "Pure")
                        {
                            VoiceList = cfg.sLoopVoice30PureFera[vi];
                        }
                        else if (sPersonal == "Cool")
                        {
                            VoiceList = cfg.sLoopVoice30CoolFera[vi];
                        }
                        else if (sPersonal == "Pride")
                        {
                            VoiceList = cfg.sLoopVoice30PrideFera[vi];
                        }
                        else if (sPersonal == "Yandere")
                        {
                            VoiceList = cfg.sLoopVoice30YandereFera[vi];
                        }
                        else if (sPersonal == "Anesan")
                        {
                            VoiceList = cfg.sLoopVoice30AnesanFera[vi];
                        }
                        else if (sPersonal == "Genki")
                        {
                            VoiceList = cfg.sLoopVoice30GenkiFera[vi];
                        }
                        else if (sPersonal == "Sadist")
                        {
                            VoiceList = cfg.sLoopVoice30SadistFera[vi];
                        }
                    }
                    else if (ModeSelect == 2)
                    { //カスタム音声１
                        VoiceList = cfg.sLoopVoice30Custom1[vi];
                    }
                    else if (ModeSelect == 3)
                    { //カスタム音声２
                        VoiceList = cfg.sLoopVoice30Custom2[vi];
                    }
                    else if (ModeSelect == 4)
                    { //カスタム音声３
                        VoiceList = cfg.sLoopVoice30Custom3[vi];
                    }
                    else if (ModeSelect == 5)
                    { //カスタム音声４
                        VoiceList = cfg.sLoopVoice30Custom4[vi];
                    }

                }
                else
                {  //放心中の絶頂時音声

                    if (vMaidStun)
                    {
                        vi = 4;
                    }
                    else if (vOrgasmCmb < 4)
                    {
                        vi = vExciteLevel - 2;
                    }
                    else
                    {
                        vi = 3;
                    }

                    if (ModeSelect == 0)
                    { //通常音声
                        if (sPersonal == "Pure")
                        {
                            VoiceList = cfg.sOrgasmVoice30PureVibe[vi];
                        }
                        else if (sPersonal == "Cool")
                        {
                            VoiceList = cfg.sOrgasmVoice30CoolVibe[vi];
                        }
                        else if (sPersonal == "Pride")
                        {
                            VoiceList = cfg.sOrgasmVoice30PrideVibe[vi];
                        }
                        else if (sPersonal == "Yandere")
                        {
                            VoiceList = cfg.sOrgasmVoice30YandereVibe[vi];
                        }
                        else if (sPersonal == "Anesan")
                        {
                            VoiceList = cfg.sOrgasmVoice30AnesanVibe[vi];
                        }
                        else if (sPersonal == "Genki")
                        {
                            VoiceList = cfg.sOrgasmVoice30GenkiVibe[vi];
                        }
                        else if (sPersonal == "Sadist")
                        {
                            VoiceList = cfg.sOrgasmVoice30SadistVibe[vi];
                        }
                    }
                    else if (ModeSelect == 1)
                    { //フェラ音声
                        if (sPersonal == "Pure")
                        {
                            VoiceList = cfg.sOrgasmVoice30PureFera[vi];
                        }
                        else if (sPersonal == "Cool")
                        {
                            VoiceList = cfg.sOrgasmVoice30CoolFera[vi];
                        }
                        else if (sPersonal == "Pride")
                        {
                            VoiceList = cfg.sOrgasmVoice30PrideFera[vi];
                        }
                        else if (sPersonal == "Yandere")
                        {
                            VoiceList = cfg.sOrgasmVoice30YandereFera[vi];
                        }
                        else if (sPersonal == "Anesan")
                        {
                            VoiceList = cfg.sOrgasmVoice30AnesanFera[vi];
                        }
                        else if (sPersonal == "Genki")
                        {
                            VoiceList = cfg.sOrgasmVoice30GenkiFera[vi];
                        }
                        else if (sPersonal == "Sadist")
                        {
                            VoiceList = cfg.sOrgasmVoice30SadistFera[vi];
                        }
                    }
                    else if (ModeSelect == 2)
                    { //カスタム音声１
                        VoiceList = cfg.sOrgasmVoice30Custom1[vi];
                    }
                    else if (ModeSelect == 3)
                    { //カスタム音声２
                        VoiceList = cfg.sOrgasmVoice30Custom2[vi];
                    }
                    else if (ModeSelect == 4)
                    { //カスタム音声３
                        VoiceList = cfg.sOrgasmVoice30Custom3[vi];
                    }
                    else if (ModeSelect == 5)
                    { //カスタム音声４
                        VoiceList = cfg.sOrgasmVoice30Custom4[vi];
                    }

                }

            }


            int iRandomVoice = UnityEngine.Random.Range(0, VoiceList.Length);
            if (OrgasmVoice != 0)
            {
                do
                {
                    while (iRandomVoice == iRandomVoiceBackup[vi] && VoiceList.Length > 1)
                    {
                        iRandomVoice = UnityEngine.Random.Range(0, VoiceList.Length);
                    }
                } while (VoiceList[iRandomVoice] == API.mMaidLastZcVoiceFN); //@API連動で追加//夜伽中の音声と重複を避けるため
                iRandomVoiceBackup[vi] = iRandomVoice;
            }

            //バイブ動作時の音声を実際に再生する
            if (vStateMajor == 20)
            {
                int index1 = Array.IndexOf(Edit_MaidsNum, Num);
                if (index1 == -1)
                {
                    vm.AudioMan.LoadPlay(VoiceList[iRandomVoice], 0f, false, true);
                }
                else if (vsFlag[index1] == 0)
                {
                    vm.AudioMan.LoadPlay(VoiceList[iRandomVoice], 0f, false, true);
                }
            }
            if (vStateMajor == 30)
            {
                if (OrgasmVoice == 0)
                {
                    int index1 = Array.IndexOf(Edit_MaidsNum, Num);
                    if (index1 == -1)
                    {
                        vm.AudioMan.LoadPlay(VoiceList[iRandomVoice], 0f, false, true);
                    }
                    else if (vsFlag[index1] == 0)
                    {
                        vm.AudioMan.LoadPlay(VoiceList[iRandomVoice], 0f, false, true);
                    }

                }
                else
                {
                    vm.AudioMan.LoadPlay(VoiceList[iRandomVoice], 0f, false, false);
                    OrgasmVoice = 2;   //絶頂音声再生中のフラグ

                    int index1 = Array.IndexOf(Edit_MaidsNum, Num);
                    if (index1 != -1) vsFlag[index1] = 0;

                }
            }



            //バイブ停止時の音声
            if (vStateMajor == 40)
            {
                int VoiceValue;

                if (vMaidStun)
                {
                    vi = 1;
                }
                else
                {
                    vi = 0;
                }

                if (vOrgasmCmb > 0)
                {
                    VoiceValue = 3 + vi;
                }
                else
                {
                    VoiceValue = vExciteLevel - 1 + vi;
                }

                if (ModeSelect == 2)
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40Custom1[VoiceValue], 0f, false, true);

                }
                else if (ModeSelect == 3)
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40Custom2[VoiceValue], 0f, false, true);

                }
                else if (ModeSelect == 4)
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40Custom3[VoiceValue], 0f, false, true);

                }
                else if (ModeSelect == 5)
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40Custom4[VoiceValue], 0f, false, true);

                }
                else if (sPersonal == "Pure")
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40PureVibe[VoiceValue], 0f, false, true);

                }
                else if (sPersonal == "Cool")
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40CoolVibe[VoiceValue], 0f, false, true);

                }
                else if (sPersonal == "Pride")
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40PrideVibe[VoiceValue], 0f, false, true);

                }
                else if (sPersonal == "Yandere")
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40YandereVibe[VoiceValue], 0f, false, true);

                }
                else if (sPersonal == "Anesan")
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40AnesanVibe[VoiceValue], 0f, false, true);
                }
                else if (sPersonal == "Genki")
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40GenkiVibe[VoiceValue], 0f, false, true);
                }
                else if (sPersonal == "Sadist")
                {
                    vm.AudioMan.LoadPlay(cfg.sLoopVoice40SadistVibe[VoiceValue], 0f, false, true);
                }


                int index1 = Array.IndexOf(Edit_MaidsNum, Num);
                if (index1 != -1) vsFlag[index1] = 0;
            }

        }

        //　フェラ状態チェック
        private int[] bIsBlowjobing = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private string sLastAnimeFileName = "";
        private string sLastAnimeFileNameOld = "";
        private string[] ZAnimeFileName = new string[20];

        //フェラしてるかチェック
        private void checkBlowjobing(Maid m, int Num)
        {

            if (maid)
            {
                //メイドさんのモーションファイル名に含まれる文字列で判別させる
                if (OrgasmVoice == 0)
                {
                    sLastAnimeFileName = m.body0.LastAnimeFN;
                }
                else if (Num == maidDataList[iCurrentMaid] && cfgw.ZeccyouAnimeEnabled)
                {
                    sLastAnimeFileName = ZAnimeFileName[Num];
                }
                else if (Num != maidDataList[iCurrentMaid] && cfgw.ZeccyouAnimeEnabled && cfgw.MaidLinkMotionEnabled)
                {
                    sLastAnimeFileName = ZAnimeFileName[Num];
                }


                if (sLastAnimeFileName != null)
                {

                    bIsBlowjobing[Num] = 0;

                    if (sLastAnimeFileName.Contains("fera")) bIsBlowjobing[Num] = 2; //フェラ
                    if (sLastAnimeFileName.Contains("sixnine")) bIsBlowjobing[Num] = 2;　//シックスナイン
                    if (sLastAnimeFileName.Contains("_ir_")) bIsBlowjobing[Num] = 2;　//イラマ
                    if (sLastAnimeFileName.Contains("_kuti")) bIsBlowjobing[Num] = 2;　//乱交３Ｐ
                    if (sLastAnimeFileName.Contains("housi")) bIsBlowjobing[Num] = 2;　//乱交奉仕
                    if (sLastAnimeFileName.Contains("kiss")) bIsBlowjobing[Num] = 1;　//キス
                    if (sLastAnimeFileName.Contains("ran4p")) bIsBlowjobing[Num] = 2; //乱交４Ｐ

                    if (sLastAnimeFileName.Contains("taiki")) bIsBlowjobing[Num] = 0;　//待機中は含めない
                    if (sLastAnimeFileName.Contains("shaseigo")) bIsBlowjobing[Num] = 0;　//射精後は含めない
                    if (sLastAnimeFileName.Contains("surituke")) bIsBlowjobing[Num] = 1;　//乱交３Ｐ擦り付け時は咥えないのでは含めない
                    if (sLastAnimeFileName.Contains("siriname")) bIsBlowjobing[Num] = 1;　//尻舐めはキス扱い
                    if (sLastAnimeFileName.Contains("asiname")) bIsBlowjobing[Num] = 1;　//足舐めはキス扱い
                    if (sLastAnimeFileName.Contains("tikubiname")) bIsBlowjobing[Num] = 1;　//乳首舐めはキス扱い

                    if (sLastAnimeFileName.Contains("ir_in_taiki")) bIsBlowjobing[Num] = 2;　//咥え始めはフェラに含める
                    if (sLastAnimeFileName.Contains("dt_in_taiki")) bIsBlowjobing[Num] = 2;　//咥え始めはフェラに含める
                    if (sLastAnimeFileName.Contains("kuti_in_taiki")) bIsBlowjobing[Num] = 2;　//咥え始めはフェラに含める
                    if (sLastAnimeFileName.Contains("kutia_in_taiki")) bIsBlowjobing[Num] = 2;　//咥え始めはフェラに含める

                    sLastAnimeFileNameOld = sLastAnimeFileName;

                    //メインメイドの場合はマウスモードを切り替える
                    //if(Num == maidDataList[iCurrentMaid]){
                    if (bIsBlowjobing[Num] == 0 && vOrgasmCmb <= 3)
                    {  //0の時は連続絶頂中じゃなければ切り替える
                        if (vBoostBase > 40)
                        {  //感度が40以上の時はランダムで歯を食いしばる
                            MouthMode[Num] = UnityEngine.Random.Range(2, 5);
                            if (MouthMode[Num] < 4) MouthMode[Num] = 0;
                        }
                        else
                        {
                            MouthMode[Num] = bIsBlowjobing[Num];
                        }
                    }

                    if (bIsBlowjobing[Num] == 1 && cfgw.MouthKissEnabled) MouthMode[Num] = bIsBlowjobing[Num]; //1の時はキスが有効なら切り替える
                    if (bIsBlowjobing[Num] == 2 && cfgw.MouthFeraEnabled) MouthMode[Num] = bIsBlowjobing[Num]; //2の時はフェラが有効なら切り替える

                    if (vMaidStun) MouthMode[Num] = 3;  //放心中は無条件でアヘらせる
                                                        //}
                                                        
                    //フェラの時は顔をカメラに向けないようにする
                    if (bIsBlowjobing[Num] == 2)
                    {
                        m.EyeToCamera((Maid.EyeMoveType)0, 0.8f);
                    }

                }
            }
        }

#endif
#endregion

        //　デバッグ用コンソール出力メソッド
        [Conditional("DEBUG")]
        private static void debugPrintConsole(string s)
        {
            Console.WriteLine(s);
        }

    }
}
#endif


namespace AudioUtil
{
    static class SE
    {

        /// <summary>
        ///  GameMain.Instance.SoundMgr.PlaySe()でセットしたSEの再生情報をコンソールに書き出すサンプル
        ///  
        ///  公式のPlaySeメソッドから呼ばれている、音声ファイルが既に登録済みかのチェックルーチンを参考にしました
        /// </summary>
        public static AudioSourceMgr[] m_aryAudioMan = null;
        public static bool CheckSE(string check_fn)
        {
            try
            {
                if (m_aryAudioMan == null)
                {
                    //オーディオマネージャーの取得、2回目以降はキャッシュを使う
                    object objAudioSeBufSe = GameMain.Instance.SoundMgr.GetType().InvokeMember("m_AudioSeBufSe",
                         BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, null, GameMain.Instance.SoundMgr, null);

                    if (objAudioSeBufSe != null)
                    {
                        object o = objAudioSeBufSe.GetType().InvokeMember("m_aryAudioMan",
                            BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic, null, objAudioSeBufSe, null);
                        m_aryAudioMan = o as AudioSourceMgr[];

                    }
                }

                if (m_aryAudioMan != null)
                {
                    //SEの全チャンネルの書き出し、重複チェックが公式にあるので同じSEが複数チャンネルにあることはないはず
                    int i = 0;
                    foreach (AudioSourceMgr asm in m_aryAudioMan)
                    {
                        i++;
                        //debugPrintConsole("SE[" + i + "]:" + asm.FileName + " isPlay?:" + asm.isPlay() + " isLoop?:" + asm.isLoop());
                        if (check_fn == asm.FileName && asm.isPlay())
                            return true;
                    }
                }
            }
            catch (Exception e)
            {
                debugPrintConsole("e:" + e);
            }
            return false;
        }

        //　デバッグ用コンソール出力メソッド
        [Conditional("DEBUG")]
        static private void debugPrintConsole(string s)
        {
            Console.WriteLine(s);
        }
    }

}


delegate System.Object delegate_API_Entry(int mode, object param1, object param2);
namespace VYM
{
    //@API実装->// API実装用クラス
    public static class API
    {
        //static string sPlugin = @"UnityInjector\CM3D2.VibeYourMaid.Plugin.dll";
        //private static CM3D2.VibeYourMaid.Plugin.VibeYourMaid objVYM = null;
        public static object objVYM = null;
        static int iRetry = 2;
        static MethodInfo mi_api_entry = null;
        static delegate_API_Entry dlg_API_Entry = null;
        //static Type type_VYM_IO_ID = null;

        //
        // 一般関数
        //
        public static double obj2dbl(object obj)
        {
            try
            {
                if (obj is double) return (double)obj;
            }
            catch { }
            return -1;
        }
        public static float obj2float(object obj)
        {
            try
            {
                if (obj is float) return (float)obj;
            }
            catch { }
            return -1;
        }
        public static int obj2int(object obj)
        {
            try
            {
                if (obj is int) return (int)obj;
            }
            catch { }
            return -1;
        }
        public static bool obj2bool(object obj)
        {
            try
            {
                if (obj is bool) return (bool)obj;
            }
            catch { }
            return false;
        }


        private static bool checkVYM()
        {
            if (iRetry <= 0) return false;
            if (mi_api_entry != null)
            { return true; }

            try
            {
                if (objVYM == null)
                {
                    GameObject go = UnityEngine.GameObject.Find("UnityInjector");
#if COM3D2
                    objVYM = go.GetComponent("COM3D2.VibeYourMaid.Plugin.VibeYourMaid");
#else
                    objVYM = go.GetComponent("CM3D2.VibeYourMaid.Plugin.VibeYourMaid");
#endif
                    if (objVYM != null)
                    {
                        //debugPrintConsole("+API.GetMethod API_Entry");
                        mi_api_entry = objVYM.GetType().GetMethod("API_Entry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                        //if (Assembly.LoadFile(sPlugin).GetTypes().Any(t => t == typeof(VYM_IO_ID)))
                        if (mi_api_entry != null)
                        {
                            dlg_API_Entry = (delegate_API_Entry)Delegate.CreateDelegate(typeof(delegate_API_Entry), objVYM, mi_api_entry, false);

                            if (dlg_API_Entry != null)
                            {
                                //Console.WriteLine("くらげ+VYM: API_Entryが見つかりました！連動開始");
                                return true;
                            }
                            else
                            {
                                dlg_API_Entry = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine("XtMS→VYM: API_Entryが見つかりません。対応したCM3D2.VibeYourMaid.Pluginではないかも…");
                            objVYM = null;
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("XtMS→VYM:" + e);
                //return false;
            }

            iRetry--;
            if (iRetry == 0)
            {
                //Console.WriteLine("VYM+APITEST:CM3D2.VibeYourMaid.Pluginが見つかりません。スタンドアローンモードです");
            }
            return false;
        }

        //////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// getVYM_ValueやsetVYM_Value関数でターゲットとするパラメータの指定用
        /// </summary>
        public enum VYM_IO_ID
        { //WindowsMessage的ID値割り当て。一意の数値さえ変えなければ行間に追加可
            b_PluginEnabledV = 100 + 10,     //有効状態
            i_GetCurrentMaidNo = 100 + 20,     //現在操作中のMaidNo//返り値：-1 未定義/null エラー
            i_CurrentMaid = 100 + 30,  //内部配列のメイドNo   //'17.04.07追加

            i_VLevel = 200 + 10,    //　バイブ状態 1=弱 2=強 0=停止
            i_vState = 200 + 20,    //　現状vStateMajorとほぼ同じ
            i_vStateMajor = 200 + 30,    //　強弱によるメイドステート//10 …停止(余韻もなし)//20 …弱//30 …強

            i_vExciteLevel = 300 + 10,    //　０～３００の興奮度を、１～４の興奮レベルに変換した値
            d_iCurrentExcite = 300 + 20,  //　現在興奮値
            d_vResistGet = 400 + 10,   //　現在抵抗値
            d_vResistBase = 400 + 20,   //　抵抗値のベース値
            d_vResistBonus = 400 + 30,   //　抵抗の特別加算値
            d_vBoostGet = 500 + 10,   //　現在感度
            d_vBoostBase = 500 + 20,   //　感度のベース値
            d_vBoostBonus = 500 + 30,   //　感度の特別加算値(今は使われていないみたい)
            d_vMaidStamina = 600 + 10,   //　スタミナ値
            b_vMaidStun = 600 + 20,   //  スタン状態（trueでたたき起こす＝ON）

            d_vOrgasmValue = 700 + 10,   //　現在絶頂値　100になると絶頂
            i_vOrgasmCount = 700 + 20,   //　絶頂回数
            i_vOrgasmCmb = 700 + 30,   //　連続絶頂回数
            i_OrgasmVoice = 700 + 40,   //　絶頂音声再生フラグ(1再生開始、2再生中)
            f_vOrgasmHoldTime = 700 + 50,   //絶頂後ボーナスタイム（残り時間。MAX600）

            b_BreastFlag = 1000 + 10,  //噴乳（胸）開始フラグ
            b_EnemaFlag = 1000 + 20,  //噴乳（尻）開始フラグ
            b_ChinpoFlag = 1000 + 30,  //射精開始フラグ
            b_SioFlag = 1000 + 40,  //潮開始フラグ

            d_AheValue = 1100 + 10,  //　アヘ値＝アヘ有効での瞳の上昇値
            d_AheValue2 = 1100 + 20,

            d_vJirashi = 1300 + 10,  //　焦らし度
            b_ExciteLock = 1500 + 10,  //　興奮度ロック
            b_OrgasmLock = 1500 + 20,  //　絶頂度ロック

            b_RankoEnabled = 1600 + 10,  //　乱交モード



            obj_GetSaveSlot = 90000 + 1,             //セーブスロット項目、一日でリセット(値渡し)

            i_GuiFlag = 98000 + 1,        //　GUIの表示フラグ（0：非表示、1：表示、2：最小化）
            b_GuiFlag2 = 98000 + 2,         //　設定画面の表示フラグ
            b_GuiFlag3 = 98000 + 3,         //　命令画面の表示フラグ

            b_StartFlag = 98100 + 1,         //#1.0.1.2で追加#シーン開始後の有効状態フラグ(通常操作があるまでFalse)

            obj_VibeYourMaidConfig = 99800 + 1,      //設定ファイル項目(荒業/非推奨/参照渡し)
            obj_VibeYourMaidCfgWriting = 99900 + 2,  //GUI設定項目(荒業/非推奨/参照渡し)
        };

        public class SaveSlot
        {
            public bool SaveFlag = false;
            public List<double> vBoostBaseSave = new List<double>();
            public List<int> vOrgasmCountSave = new List<int>();
            public List<string> MaidNameSave = new List<string>();

            public List<int> VLevelSave = new List<int>();
            public List<string> FaceBackupSave = new List<string>();
            public List<string> MotionBackupSave = new List<string>();
        }

        //////////////////////////////////////////////////////////////////////////////
        //
        //プラグインの有効状態を取得　0：無効、1：有効、-1：取得失敗
        //
        /// <summary>
        /// （ViveYourMaid.API関数）プラグインの有効状態を取得
        /// </summary>
        /// <returns>0：無効、1：有効、-1：取得失敗</returns>
        public static int GetPluginEnabled()
        {
            try
            {
                object o = GetVYM_Value(API.VYM_IO_ID.b_PluginEnabledV/*, true*/);
                if (o is bool)
                    return (bool)o ? 1 : 0;
            }
            catch (Exception e) { UnityEngine.Debug.Log("GetPluginEnabled Error:" + e); }
            return -1;
        }

        //////////////////////////////////////////////////////////////////////////////
        //
        //　API.VYM_IO_IDで指定した数値の読み出し
        //　成功ならオブジェクト型で返るので、VYM_IO_IDアイテムの最初の文字を参考にキャストして使用
        //　失敗：null
        //
        /// <summary>
        /// （ViveYourMaid.API関数）プラグインからのパラメータ読み出し用
        ///  成功ならオブジェクト型で返るので、VYM_IO_IDアイテムの最初の文字を参考にキャストして使用
        ///  [頭文字：i=int、d=double、f=float、b=bool、obj=特殊・固有のオブジェクト]
        /// <param name="id">パラメータID(ターゲット指定)</param>
        /// <returns>成功：指定の値(キャストして使用)、失敗：nullまたは数値型なら-1以下</returns>
        /// </summary>
        public static object GetVYM_Value(API.VYM_IO_ID id/*, bool PluginEnabled*/)
        {
            try
            {
                if (checkVYM()/* && PluginEnabled*/)
                    return dlg_API_Entry(1, ((int)id), null);
                //return mi_api_entry.Invoke(objVYM, new System.Object[] {1, ((int)id), null });
                //else
                //    return getVYM_Value_Emu(id);
            }
            catch (Exception e) { UnityEngine.Debug.Log("GetVYM_Value Error:" + e); }

            return null;
        }

        //////////////////////////////////////////////////////////////////////////////
        //
        //　API.VYM_IO_IDで指定した数値の書き込み（処理的に問題なさそうな物だけ実装）
        //
        //  objVarはVYM_IO_IDアイテムの最初の文字を参考に指定（型が違うと失敗します）
        //　成功なら0、メイドの状態などで設定できなかった場合は1
        //　失敗：-1以下（-1は本体クラス側…書き込み拒否含む、-2はAPIクラス側でエラー）
        //
        /// <summary>
        /// （ViveYourMaid.API関数）プラグインへのパラメータ書き込み用
        ///  ※objVarはVYM_IO_IDアイテムの最初の文字を参考に指定（型が違うと失敗します）
        ///  [頭文字：i=int、d=double、f=float、b=bool、obj=特殊・固有のオブジェクト]
        /// </summary>
        /// <param name="id">パラメータID(ターゲット指定)</param>
        /// <param name="objVar">書き込みたい値</param>
        /// <returns>成功：0、メイドやシーン状態などで設定不可:1、エラーや書込不可：-1以下</returns>
        public static int SetVYM_Value(API.VYM_IO_ID id, object objVar, bool PluginEnabled)
        {
            try
            {
                if (checkVYM() && PluginEnabled)
                {
                    object ret = dlg_API_Entry(2, ((int)id), objVar);
                    if (ret is int)
                        return (int)ret;
                }
            }
            catch (Exception e) { UnityEngine.Debug.Log("SetVYM_Value Error:" + e); }

            return -2;
        }
    }
    //<-@VYM.APIクラス追加ここまで
}