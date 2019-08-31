using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.IO;
//
using UnityEngine;
using PluginExt;
using CM3D2.XtMasterSlave.Plugin;

namespace CM3D2.XtMasterSlave.Plugin
{
    class XtMs2ndWnd
    {
        public static bool boShow = false;

        public static Vector2 EditScroll_cfg = Vector2.zero;
        static int EditScroll_cfg_sizeY = 0;

        public static Vector2 EditScroll_fn = Vector2.zero;
        static int EditScroll_fn_sizeX = 0;

        const int ItemX = 5;
        const int ItemWidth = XtMasterSlave.GUI_WIDTH - 16 - 5;//- 10;
        const int ItemHeight = 20;

        static UserConfig _UserCfg;
        static string _EditIniFN = string.Empty;
        static string _LoadIniFN = string.Empty;
        static string _memo = string.Empty;
        public static List<string> _UserIniFN = new List<string>();

        //名前で読み出し、表示も試みる
        //メインのcfgへ static bool boNameSelectAndLoad = false;

        public static void Init()
        {
            _EditIniFN = string.Empty;
            _LoadIniFN = string.Empty;
            _memo = string.Empty;

            //ファイル一覧取得
            GetUsersConfigs();
        }

        static string maidorman(Maid m)
        {
            if (m.boMAN)
                return "男 ";

            return "ﾒｲﾄﾞ";
        }

        public static void SaveWindowCallback_proc(int id, XtMasterSlave XtMS, ref int showWndMode, ref int _pageNum, XtMasterSlave.MsLinks[] p_MSlinks, XtMasterSlave.MsLinkConfig[] cfgs, XtMasterSlave.v3Offsets[] v3ofs, out string _WinprocPhase)
        {
            //XtMasterSlave.MsLinks ms_ = p_MSlinks[_pageNum];
            //bool boChrCng = false;

            _WinprocPhase = "[init]";
            GUIStyle gsLabel = new GUIStyle("label");
            gsLabel.fontSize = 12;
            gsLabel.alignment = TextAnchor.MiddleLeft;

            GUIStyle gsButton = new GUIStyle("button");
            gsButton.fontSize = 12;
            gsButton.alignment = TextAnchor.MiddleCenter;

            GUIStyle gsToggle = new GUIStyle("toggle");
            gsToggle.fontSize = 12;
            gsToggle.alignment = TextAnchor.MiddleLeft;

            GUIStyle gsText = new GUIStyle("textfield");
            gsText.fontSize = 12;
            gsText.alignment = TextAnchor.UpperLeft;

            GUIStyle gsTextAr = new GUIStyle("textArea");
            gsTextAr.fontSize = 12;
            gsTextAr.alignment = TextAnchor.UpperLeft;
            gsTextAr.wordWrap = true;

            GUIStyle gsCombo = new GUIStyle("button");
            gsCombo.fontSize = 12;
            gsCombo.alignment = TextAnchor.MiddleLeft;
            gsCombo.hover.textColor = Color.cyan;
            gsCombo.onHover.textColor = Color.cyan;
            gsCombo.onActive.textColor = Color.cyan;

            //GUIStyle gsScView = new GUIStyle("scrollView");
            _WinprocPhase = "[ctrl-1]";

            //GUI.enabled = false; //セーブ画面では閉じない
            if (GUI.Button(new Rect(240, 0, 20, 20), "x", gsButton))
            {
                //GizmoVisible(false);
                //GizmoHsVisible(false);
                //GuiFlag = false;
                boShow = false;
            }
            GUI.enabled = true;

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

            EditScroll_cfg = GUI.BeginScrollView(new Rect(0, 20, XtMasterSlave.GUI_WIDTH, XtMasterSlave.GUI_HIGHT - 30), EditScroll_cfg, 
                                new Rect(0, 0, XtMasterSlave.GUI_WIDTH - 16, EditScroll_cfg_sizeY));
            try
            {
                //XtMasterSlave.MsLinkConfig p_mscfg = cfgs[_pageNum];
                //XtMasterSlave.v3Offsets p_v3of = v3ofs[_pageNum];
                int pos_y = 0;

                GUI.Label(new Rect(ItemX, pos_y, 200, ItemHeight), "【設定保存・読込】", gsLabel);
                pos_y += ItemHeight;
                if (GUI.Button(new Rect(ItemX, pos_y, ItemWidth, ItemHeight), "戻る", gsButton))
                {
                    boShow = false;
                    Init();
                }
                pos_y += ItemHeight;
                pos_y += ItemHeight / 2;

                GUI.Label(new Rect(ItemX, pos_y, 200, ItemHeight), "【デフォルト＆一般設定】", gsLabel);

                if (GUI.Button(new Rect(ItemX, pos_y += ItemHeight, ItemWidth / 2, ItemHeight), "保存", gsButton))
                {
                    // Iniファイル書き出し
                    XtMS.SaveMyConfigs();
                }
                if (GUI.Button(new Rect(ItemX + ItemWidth / 2, pos_y, ItemWidth / 2, ItemHeight), "読込", gsButton))
                {
                    // Iniファイル読み出し
                    XtMS.LoadMyConfigs();
                }
                /*
                GUI.Label(new Rect(5 + ItemWidth - 25 - 35 - 35, pos_y, 25, ItemHeight), "設定", gsLabel);

                if (GUI.Button(new Rect(5 + ItemWidth - 35 - 35, pos_y, 35, 20), "保存", gsButton))
                {
                    // Iniファイル書き出し
                    XtMS.SaveMyConfigs();
                }
                if (GUI.Button(new Rect(5 + ItemWidth - 35, pos_y, 35, 20), "読込", gsButton))
                {
                    // Iniファイル読み出し
                    XtMS.LoadMyConfigs();
                }*/

                pos_y += ItemHeight;
                pos_y += ItemHeight/2;
                // 男0でもプリセ保存できてもいいかな？v0025 if (XtMasterSlave._MensList.Count <= 0)
                //    GUI.enabled = false;
                try
                {
                    GUI.Label(new Rect(ItemX, pos_y, 200, ItemHeight), "【プリセットの保存】", gsLabel);
                    pos_y += ItemHeight;

                    GUI.Label(new Rect(ItemX, pos_y, ItemWidth - 110, ItemHeight), "ファイル名（Save）", gsLabel);
                    if (GUI.Button(new Rect(ItemWidth - 110, pos_y, 110, ItemHeight), "日時で付ける", gsButton))
                    {
                        _memo = string.Empty;
                        _EditIniFN = "XtMS-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".ini";
                    }
                    _EditIniFN = GUI.TextField(new Rect(5, pos_y += ItemHeight, ItemWidth, ItemHeight), _EditIniFN, gsText);

                    if (GUI.Button(new Rect(ItemX, pos_y += ItemHeight, ItemWidth, ItemHeight), "保存", gsButton))
                    {
                        // Iniファイル書き出し
                        _WinprocPhase = "[save-uIni]";

                        if (string.IsNullOrEmpty(_EditIniFN))
                        {
                            NUty.WinMessageBox(NUty.GetWindowHandle(), "ファイル名を入力して下さい", "( ! )", NUty.MSGBOX.MB_OK | NUty.MSGBOX.MB_ICONERROR);
                        }
                        else
                        {
                            if (Path.GetExtension(_EditIniFN) != ".ini")
                            {
                                _EditIniFN = _EditIniFN + ".ini";
                            }
                            UserConfig ucfg = new UserConfig();
                            string strmemo = "";


                            for (int i = 0; i < cfgs.Length; i++)
                            {
                                XtMasterSlave.v3OffsetsToIniCfgs(ref cfgs[i], v3ofs[i]);
                            }

                            for (int i = 0; i < XtMasterSlave.MAX_PAGENUM; i++)
                            {
                                var ms = p_MSlinks[i];

                                ucfg.cfgs_ms[i] = cfgs[i];
                                ucfg.cfgs_p[i] = new UserConfigPage(ms.doMasterSlave, ms.Scc1_MasterMaid, ms);

                                /*ucfg.cfgs_p[i] = new UserConfigPage(ms.doMasterSlave, ms.Scc1_MasterMaid,
                                    new XtMasterSlave.ManInfo(ms.mdMasters[ms.mdMaster_No].mem, ms.mdMaster_No), 
                                    new XtMasterSlave.ManInfo(ms.mdSlaves[ms.mdSlave_No].mem, ms.mdSlave_No));
                                    */
                                if (ms.doMasterSlave)
                                {
                                    strmemo = strmemo + i+ "Master" + ": [" + maidorman(ms.mdMasters[0].mem) + ms.mdMaster_No + "]   " + XtMasterSlave.GetMaidName(ms.mdMasters[ms.mdMaster_No]) + "  [M⇔S]\r\n";
                                    strmemo = strmemo + "  Slave" + ": [" + maidorman(ms.mdSlaves[0].mem) + ms.mdSlave_No + "]   " + XtMasterSlave.GetMaidName(ms.mdSlaves[ms.mdSlave_No]) + "  [M⇔S]\r\n";
                                }
                                else if (XtMasterSlave._MensList.Count <= 0)
                                {
                                    // メイドのみモード
                                    strmemo = strmemo + i + "Master" + ": 不在" + "\r\n";

                                    if (ms.mdSlave_No < 0)
                                        strmemo = strmemo + "  Slave" + ": [" + maidorman(ms.mdSlaves[0].mem) + "_]   " + "未選択" + "\r\n";
                                    else
                                        strmemo = strmemo + "  Slave" + ": [" + maidorman(ms.mdSlaves[0].mem) + ms.mdSlave_No + "]   " + XtMasterSlave.GetMaidName(ms.mdSlaves[ms.mdSlave_No]) + "\r\n";
                                }
                                else
                                {
                                    if (ms.mdMaster_No < 0)
                                        strmemo = strmemo + i + "Master" + ": [" + maidorman(ms.mdMasters[0].mem) + "_]   " + "未選択" + "\r\n";
                                    else
                                        strmemo = strmemo + i + "Master" + ": [" + maidorman(ms.mdMasters[0].mem) + ms.mdMaster_No + "]   " + XtMasterSlave.GetMaidName(ms.mdMasters[ms.mdMaster_No]) + "\r\n";

                                    if (ms.mdSlave_No < 0)
                                        strmemo = strmemo + "  Slave" + ": [" + maidorman(ms.mdSlaves[0].mem) + "_]   " + "未選択" + "\r\n";
                                    else
                                        strmemo = strmemo + "  Slave" + ": [" + maidorman(ms.mdSlaves[0].mem) + ms.mdSlave_No + "]   " + XtMasterSlave.GetMaidName(ms.mdSlaves[ms.mdSlave_No]) + "\r\n";
                                }
                            }
                            if (strmemo.Length > 0)
                            {
                                //strmemo = "保存時のリンク状態\r\n" + strmemo;
                            }

                            _WinprocPhase = "[save-uIni2]";
                            ucfg.cfg_h.CurPageNum = _pageNum;
                            ucfg.cfg_h.Memo = strmemo;
                            // ジッサイに保存
                            SaveIni_UserCfg(_EditIniFN, ucfg);
                            GetUsersConfigs();
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    GUI.enabled = true;
                }

                pos_y += ItemHeight;
                pos_y += ItemHeight;
                GUI.Label(new Rect(ItemX, pos_y, 200, ItemHeight), "【プリセットの読込】", gsLabel);
                pos_y += ItemHeight;

                List<string> slist = _UserIniFN;
                Rect boxrect = new Rect(ItemX, pos_y, ItemWidth, ItemHeight * 5);
                pos_y += ItemHeight * 5;
                GUI.Box(boxrect, "");
                EditScroll_fn = GUI.BeginScrollView(boxrect, EditScroll_fn, new Rect(0, 0, EditScroll_fn_sizeX, (ItemHeight * slist.Count())), false, true);
                EditScroll_fn_sizeX = (int)boxrect.width - 16;
                try
                {
                    int pos_sy = 0;
                    foreach (string str in slist)
                    {
                        bool chk = (_LoadIniFN == str);
                        int pos_sx = 2;

                        //ファイル名
                        Vector2 v2Toggle = gsToggle.CalcSize(new GUIContent(str));
                        v2Toggle.x += pos_sx + 10;
                        if (EditScroll_fn_sizeX < v2Toggle.x)
                            EditScroll_fn_sizeX = (int)v2Toggle.x;
                        bool chk2 = GUI.Toggle(new Rect(pos_sx, pos_sy, /*boxrect.width*/v2Toggle.x, ItemHeight), chk, str, gsToggle);
                        if (chk != chk2)
                        {
                            if (chk2)
                            {
                                _LoadIniFN = str;
                                _UserCfg = LoadIni_UserCfg(str);
                                _memo = _UserCfg.cfg_h.Memo;
                            }/*
                            else
                            {
                                _LoadIniFN = string.Empty;
                            }*/
                        }
                        pos_sy += ItemHeight;
                    }
                }
                catch (Exception)
                {
                    _WinprocPhase = "スクロール1";
                    throw;
                }
                finally
                {
                    GUI.EndScrollView();
                }

                //pos_y += ItemHeight / 2;

                //ファイル名
                GUI.Label(new Rect(ItemX, pos_y, 200, ItemHeight), "ファイル名（Load）", gsLabel);
                GUI.TextField(new Rect(5, pos_y += ItemHeight, ItemWidth, ItemHeight), _LoadIniFN, gsText);

                GUI.Label(new Rect(ItemX, pos_y += ItemHeight, ItemWidth, ItemHeight), "保存時の情報", gsLabel);
                GUI.TextArea(new Rect(ItemX, pos_y += ItemHeight, ItemWidth, ItemHeight * 8), _memo, gsTextAr);
                pos_y += ItemHeight * 8;
                //pos_y += ItemHeight / 2;

                if (GUI.Button(new Rect(ItemX, pos_y, ItemWidth, ItemHeight), "読込", gsButton))
                {
                    // 元のカーソルを保持
                    System.Windows.Forms.Cursor preCursor = System.Windows.Forms.Cursor.Current;
                    // カーソルを待機カーソルに変更
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                    
                    try {
                        // Iniファイル読み出し
                        _UserCfg = LoadIni_UserCfg(_LoadIniFN);

                        //ページ選択の復元
                        _pageNum = _UserCfg.cfg_h.CurPageNum;

                        //プリセットデータの反映を行う
                        procUserCfg(p_MSlinks, cfgs, v3ofs);
                    }
                    catch
                    {
                        _WinprocPhase = "[load-uIni2]";
                        throw;
                    }
                    finally
                    {
                        // カーソルを元に戻す
                        System.Windows.Forms.Cursor.Current = preCursor;
                    }

                    //画面を閉じる
                    boShow = false;
                    Init();
                }//ボタン

                pos_y += ItemHeight;
                XtMasterSlave.cfg.boNameSelectAndLoad = GUI.Toggle(new Rect(ItemX, pos_y, ItemWidth, ItemHeight), XtMasterSlave.cfg.boNameSelectAndLoad, "キャラ名指定＆呼び出し有効", gsToggle);
                pos_y += ItemHeight;

                GUI.Label(new Rect(ItemX, pos_y, ItemWidth, ItemHeight), "注:呼び出す人数が多いと時間が掛かります", gsLabel);
                pos_y += ItemHeight;
                GUI.Label(new Rect(ItemX, pos_y, ItemWidth, ItemHeight), "(※OFF時は表示キャラ順の番号指定)", gsLabel);
                pos_y += ItemHeight;

                pos_y += ItemHeight / 2;
                pos_y += ItemHeight * 2;
                EditScroll_cfg_sizeY = pos_y;
            }
            catch
            {
                throw;
            }
            finally
            {
                GUI.EndScrollView();
            }

        }

        static void procUserCfg(XtMasterSlave.MsLinks[] p_MSlinks, XtMasterSlave.MsLinkConfig[] cfgs, XtMasterSlave.v3Offsets[] v3ofs)
        {
            for (int i = 0; i < XtMasterSlave.MAX_PAGENUM; i++)
            {
                var ms = p_MSlinks[i];

                //v0031 fix
                ms.mdMaster_No = -1;
                ms.FixMaster();
                ms.mdSlave_No = -1;
                ms.FixSlave();

                cfgs[i] = _UserCfg.cfgs_ms[i];

                UserConfigPage cfgp = _UserCfg.cfgs_p[i];
                ms.Scc1_MasterMaid = cfgp.isMasterMaid;
                ms.mdMaster_No = cfgp.Master_ID;
                ms.mdSlave_No = cfgp.Slave_ID;

                if (!ms.Scc1_MasterMaid)
                {
                    ms.mdMasters = XtMasterSlave._MensList;
                    ms.mdSlaves = XtMasterSlave._MaidList;
                }
                else
                {
                    ms.mdSlaves = XtMasterSlave._MensList;
                    ms.mdMasters = XtMasterSlave._MaidList;
                }

                //名前指定＆呼び出し
                if (XtMasterSlave.cfg.boNameSelectAndLoad)
                {
                    //master
                    int num = -1;
                    if (!string.IsNullOrEmpty(cfgp.Master_Name))
                    {
                        if (!ms.Scc1_MasterMaid)
                        {
                            num = XtMasterSlave.SelectOrLoadMan(cfgp.Master_Name);
                            if (num >= 0)
                                ms.mdMasters = XtMasterSlave._MensList; //リストが更新された可能性ある
                        }
                        else
                        {
                            num = XtMasterSlave.SelectOrLoadMaid(cfgp.Master_Name);
                            if (num >= 0)
                                ms.mdMasters = XtMasterSlave._MaidList;
                        }

                        if (num >= 0)
                        {
                            ms.mdMaster_No = num;

                            //公式撮影の非表示解除
                            if(ms.mdMasters[ms.mdMaster_No].mem.gameObject.transform.GetChild(0).localScale == Vector3.zero)
                                ms.mdMasters[ms.mdMaster_No].mem.gameObject.transform.GetChild(0).localScale = Vector3.one;
                        }
                        else
                        {
                            Console.WriteLine("指定キャラクターが見つかりません。" + (cfgp.doMasterSlave ? "リンク中止：" : "：") + cfgp.Master_Name);
                            cfgp.doMasterSlave = false;
                        }
                    }

                    //slave
                    num = -1;
                    if (!string.IsNullOrEmpty(cfgp.Slave_Name))
                    {
                        if (!ms.Scc1_MasterMaid)
                        {
                            num = XtMasterSlave.SelectOrLoadMaid(cfgp.Slave_Name);
                            if (num >= 0)
                                ms.mdSlaves = XtMasterSlave._MaidList; //リストが更新された可能性ある
                        }
                        else
                        {
                            num = XtMasterSlave.SelectOrLoadMan(cfgp.Slave_Name);
                            if (num >= 0)
                                ms.mdSlaves = XtMasterSlave._MensList;
                        }

                        if (num >= 0)
                        {
                            ms.mdSlave_No = num;

                            //公式撮影の非表示解除
                            if (ms.mdSlaves[ms.mdSlave_No].mem.gameObject.transform.GetChild(0).localScale == Vector3.zero)
                                ms.mdSlaves[ms.mdSlave_No].mem.gameObject.transform.GetChild(0).localScale = Vector3.one;
                        }
                        else
                        {
                            Console.WriteLine("指定キャラクターが見つかりません。" + (cfgp.doMasterSlave ? "リンク中止：" : "：") + cfgp.Slave_Name);
                            cfgp.doMasterSlave = false;
                        }
                    }
                }

                ms.doMasterSlave = false;
                ms.maidKeepSlaveYotogi = null;

                if (ms.mdMasters.Count > ms.mdMaster_No && ms.mdMaster_No >= 0)
                {
                    Maid master = ms.mdMasters[ms.mdMaster_No].mem;

                    if (ms.mdSlaves.Count > ms.mdSlave_No && ms.mdSlave_No >= 0)
                    {
                        Maid slave = ms.mdSlaves[ms.mdSlave_No].mem;
                        Maid maid0 = null;
                        if (!slave.boMAN && ms.mdSlave_No > 0 && ms.mdSlaves[0].mem)
                            maid0 = ms.mdSlaves[0].mem;

                        //設定の反映
                        debugPrintConsole(i + "：設定の反映");
                        cfgp.ProcParam(master, slave, maid0, cfgs[i]);

                        //リンク開始
                        if (master && slave && master.body0 && slave.body0)
                        {
                            ms.doMasterSlave = cfgp.doMasterSlave;

                            //キープスレイブ反映
                            if (cfgs[i].doKeepSlaveYotogi && !slave.boMAN && XtMasterSlave.IsKeepScene())
                                ms.maidKeepSlaveYotogi = slave;

                            //v0031 fix
                            ms.FixMaster();
                            ms.FixSlave();
                        }
                    }
                }
                else
                {
                    if (ms.mdSlaves.Count > ms.mdSlave_No && ms.mdSlave_No >= 0)
                    {
                        // メイドのみでも設定反映 v0025
                        Maid slave = ms.mdSlaves[ms.mdSlave_No].mem;
                        Maid maid0 = null;
                        if (!slave.boMAN && ms.mdSlave_No > 0 && ms.mdSlaves[0].mem)
                            maid0 = ms.mdSlaves[0].mem;

                        //設定の反映
                        debugPrintConsole(i + "：設定の反映m/o");
                        cfgp.ProcParam(null, slave, maid0, cfgs[i]);

                        //v0031 fix
                        ms.mdMaster_No = -1;
                        ms.FixMaster();
                        ms.FixSlave();
                    }
                }

                if (ms.mdSlaves.Count > ms.mdSlave_No && ms.mdSlave_No >= 0)
                {
                    Maid slave = ms.mdSlaves[ms.mdSlave_No].mem;
                    cfgp.ProcParam2(slave, ms);
                }
            }

            //オフセットテーブルの更新
            for (int i = 0; i < cfgs.Length; i++)
            {
                XtMasterSlave.IniCfgsTov3Offsets(ref v3ofs[i], cfgs[i]);
            }
        }

#if COM3D2
        const string dirIni = @"\Config\XtMasterSlave\User_SaveData\"; //UnityInjector
#else
        const string dirIni = @"\Config\XtMsterSlave\User_SaveData\"; //UnityInjector
#endif
        static string getPlginDir()
        {
#if COM3D2
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#else
            {
                string fullpath = Path.GetFullPath(".\\");
                string plginDirPath = Path.Combine(fullpath, @"Sybaris\Plugins\UnityInjector");
                return plginDirPath;
            }
#endif
        }

        //プリセット用INIファイルリスト取得
        public static void GetUsersConfigs()
        {
            try
            {
                string plginDirPath = getPlginDir();

                //ディレクトリがなければ作成
                string dir = plginDirPath + Path.GetDirectoryName(dirIni);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                _UserIniFN.Clear();
                HashSet<string> tmpset = new HashSet<string>();

                string[] files = Directory.GetFiles(plginDirPath + dirIni, "*.ini");
                foreach (string fn in files)
                {
                    //debugPrintConsole(fn);
                    //GetFilesは.ini?で検索してしまうのでチェックする
                    if (Path.GetExtension(fn) == ".ini")
                    {
                        tmpset.Add(Path.GetFileName(fn));
                    }
                }
                _UserIniFN = new List<string>(tmpset.ToArray());
                _UserIniFN.Sort();
            }
            catch (Exception e)
            {
                Console.WriteLine("GetUsersConfigs例外; " + e);
            }
        }

        //PluginExt用の相対パスに変更
        public static string extpath(string inipath)
        {
#if COM3D2
            return getPlginDir() + @"\Config\XtMasterSlave\User_SaveData\" + System.IO.Path.GetFileName(inipath);
#endif
            return @"XtMsterSlave\User_SaveData\" + System.IO.Path.GetFileName(inipath);
        }

        //プリセット用INIファイル読み書き
        public static string LoadIni_Memo(string inifn)
        {
            //ファイル名
            //string fileIni = @"UnityInjector\Config\XtMsterSlave\User_SaveData\" + inifn;
            string fileIni = @"User_SaveData\" + inifn;
            //bool isExist = false;

            try
            {
                string xfn = extpath(fileIni);
                Console.WriteLine("ユーザーセーブini設定読み込み :" + xfn);

                var tmpc = PluginExt.SharedConfig.ReadConfig<UserConfigHeader>("Config", xfn);
                //isExist = true;

                tmpc.Memo = tmpc.Memo.Replace("＜改行＞", "\r\n");
                //tmpc.Memo = tmpc.Memo.Replace("＜改行＞", "\n");

                return tmpc.Memo;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("XtMs+INI Error:" + e);
            }

            return null;
        }

        public static UserConfig LoadIni_UserCfg(string inifn)
        {
            //ファイル名
            //string fileIni = @"UnityInjector\Config\XtMsterSlave\User_SaveData\" + inifn;
            string fileIni = @"User_SaveData\" + inifn;
            //bool isExist = false;

            try
            {
                string xfn = extpath(fileIni);
                Console.WriteLine("ユーザーセーブini設定読み込み :" + xfn);

                UserConfig uc = new UserConfig();
                uc.cfg_h = SharedConfig.ReadConfig<UserConfigHeader>("Config", xfn);
                uc.cfg_h.Memo = uc.cfg_h.Memo.Replace("＜改行＞", "\r\n");
                //tmpc.Memo = tmpc.Memo.Replace("＜改行＞", "\n");

                for (int i = 0; i<XtMasterSlave.MAX_PAGENUM; i++)
                {
                    uc.cfgs_p[i] = SharedConfig.ReadConfig<UserConfigPage>("Page-" + (i+1).ToString(), xfn);

                    //debugPrintConsole(uc.cfgs_p[i].Master_Name);

                    //勝手に取ってくれるっぽい…
                    //uc.cfgs_p[i].Master_Name = uc.cfgs_p[i].Master_Name._dq();
                    //uc.cfgs_p[i].Slave_Name = uc.cfgs_p[i].Slave_Name._dq();
                }
                //isExist = true;

                for (int i = 0; i < uc.cfgs_ms.Length; i++)
                    uc.cfgs_ms[i] = SharedConfig.ReadConfig<XtMasterSlave.MsLinkConfig>("Config-" + (i + 1).ToString(), xfn);
                /*
                for (int i = 0; i < cfgs.Length; i++)
                {
                    v3ofs[i].v3StackOffset = faTov3(cfgs[i].v3StackOffsetFA);
                    v3ofs[i].v3StackOffsetRot = faTov3(cfgs[i].v3StackOffsetRotFA);
                    v3ofs[i].v3HandLOffset = faTov3(cfgs[i].v3HandLOffsetFA);
                    v3ofs[i].v3HandROffset = faTov3(cfgs[i].v3HandROffsetFA);
                }*/

                return uc;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("XtMs+INI Error:" + e);
            }

            return null;
        }

#if false
        public static UserConfig SaveIni_UserCfg(string inifn)
        {
            //ファイル名
            string fileIni = @"UnityInjector\Config\XtMsterSlave\User_SaveData\" + inifn;
            //bool isExist = false;

            try
            {
                string xfn = extpath(fileIni);
                Console.WriteLine("ユーザーセーブini設定読み込み :" + xfn);

                UserConfig uc = new UserConfig();
                uc.cfg_h = SharedConfig.ReadConfig<UserConfigHeader>("Config", xfn);
                uc.cfg_h.Memo = uc.cfg_h.Memo.Replace("＜改行＞", "\r\n");
                //tmpc.Memo = tmpc.Memo.Replace("＜改行＞", "\n");

                for (int i = 0; i < XtMasterSlave.MAX_PAGENUM; i++)
                {
                    uc.cfgs_p[i] = SharedConfig.ReadConfig<UserConfigPage>("Page-" + (i + 1).ToString(), xfn);
                }

                for (int i = 0; i < uc.cfgs_ms.Length; i++)
                    SharedConfig.SaveConfig("Config-" + (i + 1).ToString(), xfn, uc.cfgs_ms[i]);
                /*
                for (int i = 0; i < cfgs.Length; i++)
                {
                    v3ofs[i].v3StackOffset = faTov3(cfgs[i].v3StackOffsetFA);
                    v3ofs[i].v3StackOffsetRot = faTov3(cfgs[i].v3StackOffsetRotFA);
                    v3ofs[i].v3HandLOffset = faTov3(cfgs[i].v3HandLOffsetFA);
                    v3ofs[i].v3HandROffset = faTov3(cfgs[i].v3HandROffsetFA);
                }*/

                return uc;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("XtMs+INI Error:" + e);
            }

            return null;
        }
#endif

        //プリセット用INIファイル読み書き
        public static bool SaveIni_UserCfg(string inifn, UserConfig uc)
        {
#if COM3D2
            string saveDirPath = Path.Combine(getPlginDir(), @"Config\");
            //ファイル名
            string fileIni = saveDirPath + @"XtMasterSlave\User_SaveData\" + inifn;
#else
            string fullpath = Path.GetFullPath(".\\");
            string saveDirPath = Path.Combine(fullpath, @"Sybaris\Plugins\UnityInjector\Config\");
            //ファイル名
            //string fileIni = @"UnityInjector\Config\XtMsterSlave\User_SaveData\" + inifn;
            string fileIni = saveDirPath + @"XtMsterSlave\User_SaveData\" + inifn;
#endif
            string xfn = extpath(fileIni);
            bool isSuccess = false;
            string strfn_d = null;
            try
            {
                if (File.Exists(fileIni))
                {
                    var msgo = "上書きしますか？\r\nファイル名: " + inifn;
                    var reto = NUty.WinMessageBox(NUty.GetWindowHandle(), msgo, "( ! )", NUty.MSGBOX.MB_OKCANCEL | NUty.MSGBOX.MB_ICONQUESTION);
                    if (reto != (int)System.Windows.Forms.DialogResult.OK)
                    {
                        msgo = "保存はキャンセルされました";
                        NUty.WinMessageBox(NUty.GetWindowHandle(), msgo, "( ! )", NUty.MSGBOX.MB_OK);
                        return false;
                    }
                    else {
                        strfn_d = fileIni + ".bak";
                        File.Move(fileIni, strfn_d);
                    }
                }

                if (!File.Exists(fileIni))
                {
                    Console.WriteLine("ユーザーセーブini設定書き込み :" + xfn);

                    uc.cfg_h.Memo = uc.cfg_h.Memo.Replace("\n", "＜改行＞");
                    uc.cfg_h.Memo = uc.cfg_h.Memo.Replace("\r", "");
                    SharedConfig.SaveConfig("Config", xfn, uc.cfg_h);

                    for (int i = 0; i < XtMasterSlave.MAX_PAGENUM; i++)
                    {
                        //前後のスペースがトリムされるようなので""でエスケープする
                        uc.cfgs_p[i].Master_Name = uc.cfgs_p[i].Master_Name.dq_();
                        uc.cfgs_p[i].Slave_Name = uc.cfgs_p[i].Slave_Name.dq_();

                        SharedConfig.SaveConfig("Page-" + (i + 1).ToString(), xfn, uc.cfgs_p[i]);
                    }

                    for (int i = 0; i < uc.cfgs_ms.Length; i++)
                        SharedConfig.SaveConfig("Config-" + (i + 1).ToString(), xfn, uc.cfgs_ms[i]);
                }
                if (strfn_d != null && File.Exists(strfn_d))
                {
                    File.Delete(strfn_d);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("api+INI SaveError:" + e);

                var msg = "ファイル保存に失敗しました\r\nファイル名：" + inifn + "\r\n" + e.Message;
                NUty.WinMessageBox(NUty.GetWindowHandle(), msg, "( ! )", NUty.MSGBOX.MB_OK | NUty.MSGBOX.MB_ICONERROR);
            }
            return isSuccess;
        }

        //　デバッグ用コンソール出力メソッド
        [Conditional("DEBUG")]
        private static void debugPrintConsole(string s)
        {
            Console.WriteLine(s);
        }
    }


    class UserConfig
    {
        public UserConfigHeader cfg_h = new UserConfigHeader();
        public UserConfigPage[] cfgs_p = new UserConfigPage[XtMasterSlave.MAX_PAGENUM];
        public XtMasterSlave.MsLinkConfig[] cfgs_ms = new XtMasterSlave.MsLinkConfig[XtMasterSlave.MAX_PAGENUM];

        public UserConfig()
        {
            for (int i = 0; i < cfgs_p.Length; i++)
                cfgs_p[i] = new UserConfigPage();

            for (int i = 0; i < cfgs_ms.Length; i++)
                cfgs_ms[i] = new XtMasterSlave.MsLinkConfig();
        }
    }

    class UserConfigHeader
    {
        public string Memo = string.Empty;
        public int CurPageNum = 0;
    }

    class UserConfigPage
    {
        public bool doMasterSlave = false;
        public bool isMasterMaid = false;

        public string Master_Name = string.Empty;
        public int Master_ID = 0;
        public bool Master_Hide = false;

        public string Slave_Name = string.Empty;
        public int Slave_ID = 0;

        public float Scale_Master = 1f;
        public float Scale_Slave = 1f;
        //public float Scale_HitCheck = 1f;

        public float manAlpha;
        public bool Cnk_Visible = true;
        public float Cnk_Scale = 1f;
        public float[] Cnk_OffsetPos = new float[3] { 0, 0, 0 };

        public bool Stgt_boHeadToCam = false;
        public bool Stgt_boEyeToCam = false;
        public int Stgt_hashTarget = 0;
        public string Stgt_sBoneName = string.Empty;

        public string[] Hold_SlaveMaskItems = new string[0];

        public UserConfigPage()
        {
        }

        /*public UserConfigPage(bool linking, bool isMasterMaid, XtMasterSlave.ManInfo master, XtMasterSlave.ManInfo slave)
        {
            SetParam(linking, isMasterMaid, master, slave);
        }

        public void SetParam(bool linking, bool isMasterMaid, XtMasterSlave.ManInfo master, XtMasterSlave.ManInfo slave)
        */

        public UserConfigPage(bool linking, bool isMasterMaid, XtMasterSlave.MsLinks ms)
        {
            SetParam(linking, isMasterMaid, ms);
        }


        //この保存用クラスに読込
        public void SetParam(bool linking, bool isMasterMaid, XtMasterSlave.MsLinks ms)
        {
            doMasterSlave = linking;
            this.isMasterMaid = isMasterMaid;

            XtMasterSlave.ManInfo master = null;
            XtMasterSlave.ManInfo slave = null;

            if (ms.mdMaster_No >= 0)
                master = new XtMasterSlave.ManInfo(ms.mdMasters[ms.mdMaster_No].mem, ms.mdMaster_No);
            if (ms.mdSlave_No >= 0)
                slave = new XtMasterSlave.ManInfo(ms.mdSlaves[ms.mdSlave_No].mem, ms.mdSlave_No);

            if (master != null)
            {
                if (master.mem.boMAN)
                    Master_Name = XtMasterSlave.GetMaidName(master);
                else
                    Master_Name = XtMasterSlave.GetMaidName(master.mem);
                Master_ID = master.mem_id;
            }
            else
            {
                Master_Name = string.Empty;
                Master_ID = ms.mdMaster_No;
            }

            if (slave != null)
            {
                if (slave.mem.boMAN)
                    Slave_Name = XtMasterSlave.GetMaidName(slave);
                else
                    Slave_Name = XtMasterSlave.GetMaidName(slave.mem);
                Slave_ID = slave.mem_id;
            }
            else
            {
                Slave_Name = string.Empty;
                Slave_ID = ms.mdSlave_No;
            }

            if (master != null)
                Scale_Master = (master.mem.gameObject.transform.localScale.x + master.mem.gameObject.transform.localScale.y + master.mem.gameObject.transform.localScale.z) / 3;
            if (slave != null)
                Scale_Slave = (slave.mem.gameObject.transform.localScale.x + slave.mem.gameObject.transform.localScale.y + slave.mem.gameObject.transform.localScale.z) / 3;

            if (master != null)
            {
                if (master.mem.boMAN)
                {
                    Master_Hide = !XtMasterSlave.GetManVisible(master.mem);

                    Cnk_Visible = XtMasterSlave.GetChinkoVisible(master.mem.body0);
                    Cnk_Scale = XtMasterSlave.GetChinkoScale(master.mem.body0).x;
                    manAlpha = XtMasterSlave.GetManAlpha(master.mem);

                    Cnk_OffsetPos = XtMasterSlave.v3Tofa(XtMasterSlave.GetChinkoPos(master.mem.body0));
                }
                else
                {
                    Master_Hide = XtMasterSlave.GetStateMaskItemsAll(master.mem);

                    if (slave.mem.boMAN)
                    {
                        Cnk_Visible = XtMasterSlave.GetChinkoVisible(slave.mem.body0);
                        Cnk_Scale = XtMasterSlave.GetChinkoScale(slave.mem.body0).x;
                        manAlpha = XtMasterSlave.GetManAlpha(slave.mem);

                        Cnk_OffsetPos = XtMasterSlave.v3Tofa(XtMasterSlave.GetChinkoPos(slave.mem.body0));
                    }
                }
            }

            if (slave != null)
            {
                if (!slave.mem.boMAN)
                {
                    Stgt_boHeadToCam = slave.mem.body0.boHeadToCam;
                    Stgt_boEyeToCam = slave.mem.body0.boEyeToCam;
                    //Stgt_hashTarget = slave.mem.body0.trsLookTarget.GetHashCode();
                    Stgt_sBoneName = slave.mem.body0.trsLookTarget.name;

                    switch (Stgt_sBoneName)
                    {
                        case "Bip01 Head":
                            break;
                        case "_IK_vagina":
                            break;
                        case "ManBip Head":
                            break;
                        case "chinko2":
                            break;
                        default:
                            Stgt_sBoneName = string.Empty;
                            break;
                    }
                }
            }

            if (slave != null)
            {
                if (!slave.mem.boMAN)
                {
                    //アイテムマスク
                    if (ms.holdSlvMask && ms.holdSlvMaskMaid == slave.mem)
                        Hold_SlaveMaskItems = ms.holdSlvMaskItems.ToArray();
                    else
                        Hold_SlaveMaskItems = new string[0];
                }
            }
        }

        public void ProcParam(Maid master, Maid slave, Maid maid0, XtMasterSlave.MsLinkConfig p_mscfg)
        {
            //v0025 masterの不在を許可 if (!master && !master.body0 && !slave && !slave.body0)
            if (!slave && !slave.body0)
                return;

            //サイズ変更
            if (master && master.body0)
            {
                master.gameObject.transform.localScale = new Vector3(Scale_Master, Scale_Master, Scale_Master);
                if (!master.boMAN)
                    XtMasterSlave.UpdateHitScale(master, Scale_Master, p_mscfg);
                   //  XtMasterSlave.UpdateHitScale(master, Scale_Master * p_mscfg.Scale_HitCheckEffect);
            }
            slave.gameObject.transform.localScale = new Vector3(Scale_Slave, Scale_Slave, Scale_Slave);
            if (!slave.boMAN)
                XtMasterSlave.UpdateHitScale(slave, Scale_Slave, p_mscfg);
            //  XtMasterSlave.UpdateHitScale(slave, Scale_Slave * p_mscfg.Scale_HitCheckEffect);

            if (master && master.body0)
            {
                // 男設定 chinkoサイズ・位置など
                if (master.boMAN)
                {
                    XtMasterSlave.SetManVisible(master, !Master_Hide);

                    //master.body0.SetChinkoVisible(Cnk_Visible);
                    XtMasterSlave.SetChinkoVisible(master.body0, Cnk_Visible); //v0030 fix
                    if (Cnk_Visible)
                    {
                        XtMasterSlave.SetChinkoScale(master.body0, Cnk_Scale);
                        XtMasterSlave.SetChinkoPos(master.body0, XtMasterSlave.faTov3(Cnk_OffsetPos));
                    }
                    XtMasterSlave.SetManAlpha(master, manAlpha);
                }
                else
                {
                    XtMasterSlave.SetStateMaskItemsAll(master, Master_Hide);

                    if (slave.boMAN)
                    {
                        //slave.body0.SetChinkoVisible(Cnk_Visible);
                        XtMasterSlave.SetChinkoVisible(master.body0, Cnk_Visible); //v0030 fix
                        if (Cnk_Visible)
                        {
                            XtMasterSlave.SetChinkoScale(slave.body0, Cnk_Scale);
                            XtMasterSlave.SetChinkoPos(slave.body0, XtMasterSlave.faTov3(Cnk_OffsetPos));
                        }
                        XtMasterSlave.SetManAlpha(slave, manAlpha);
                    }
                }
            }
#if DEBUG
            Console.WriteLine(string.Format("+Stgt_boHeadToCam={0} Stgt_boEyeToCam={1}", Stgt_boHeadToCam, Stgt_boEyeToCam));
#endif

            if (!slave.boMAN)
            {
#if DEBUG
                Console.WriteLine(string.Format("-Stgt_boHeadToCam={0} Stgt_boEyeToCam={1}", Stgt_boHeadToCam, Stgt_boEyeToCam));
#endif
                slave.body0.boHeadToCam = Stgt_boHeadToCam;
                slave.body0.boEyeToCam = Stgt_boEyeToCam;
                Maid tgt = null;
                switch (Stgt_sBoneName)
                {
                    case "Bip01 Head":
                        if (maid0 && maid0.body0 && slave != maid0)
                        {
                            tgt = maid0;
                        }
                        break;
                    case "_IK_vagina":
                        if (maid0 && maid0.body0 && slave != maid0)
                        {
                            tgt = maid0;
                        }
                        break;
                    case "ManBip Head":
                        if (master && master.body0)
                        {
                            tgt = master;
                        }
                        break;
                    case "chinko2":
                        if (master && master.body0)
                        {
                            tgt = master;
                        }
                        break;
                    default:
                        Stgt_sBoneName = string.Empty;
                        break;
                }

                if (tgt)
                {
                    Transform tgt_tr = BoneLink.BoneLink.SearchObjName(tgt.body0.m_Bones.transform, Stgt_sBoneName, true);
                    slave.EyeToTarget(tgt, GameUty.MillisecondToSecond(0), Stgt_sBoneName);
                }
                else
                {
                    slave.EyeToReset(GameUty.MillisecondToSecond(0));
                }
                slave.body0.boHeadToCam = Stgt_boHeadToCam;
                slave.body0.boEyeToCam = Stgt_boEyeToCam;
            }
        }

        public void ProcParam2(Maid slave, XtMasterSlave.MsLinks ms)
        {
            if (slave && !slave.boMAN && this.Hold_SlaveMaskItems.Length > 0)
            {
                //衣装マスク設定あり
                ms.holdSlvMask = true;
                ms.holdSlvMaskMaid = slave;

                ms.holdSlvMaskItems.Clear();
                foreach (var s in this.Hold_SlaveMaskItems)
                    ms.holdSlvMaskItems.Add(s);
            }
        }
    }

    static class Extensions
    {
        public static string dq_(this string str)
        {
            return @"""" + str + @"""";
        }

        public static string _dq(this string str)
        {
            if (str.Length <= 2)
                return string.Empty;

            return str.Substring(1, str.Length - 2);
        }
    }
}
