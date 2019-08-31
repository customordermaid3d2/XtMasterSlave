#define IKO117

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ExtensionMethods.MyExtensions;
using CM3D2.XtMasterSlave.Plugin;
using UnityEngine;

namespace XtMasterSlave_IKO117
{
#if IKO117
    public class Iko117Inst : IkInst
    {
        public override bool IsNewPointIK(Maid m, string hand = "右手")
        {
            var ikP = m.body0.IKCtrl.GetIKData(hand).GetIKParam(IKCtrlData.IKAttachType.Point);
            return (ikP.MyType == IKCtrlData.IKAttachType.NewPoint);
        }

        public override object GetIkPoint(TBody body, string hand = "右手")
        {
            var obj = body.IKCtrl.GetIKData(hand).GetIKParam(IKCtrlData.IKAttachType.Point);
            if (obj == null)
                obj = body.IKCtrl.GetIKData(hand).GetIKParam(IKCtrlData.IKAttachType.NewPoint);

            return obj;
        }

        public override object GetIkCtrl(Maid maid)
        {
            return maid.IKCtrl;
        }

        public override object GetIkCtrlPoint(TBody body, string hand = "右手")
        {
            var obj = body.IKCtrl.GetIKData(hand);
            return obj;
        }

        private IKCtrlData.IKAttachType GetDefType(XtMasterSlave.MsLinkConfig mscfg)
        {
            if (mscfg.doIK159NewPointToDef)
            {
                return IKCtrlData.IKAttachType.NewPoint;
            }
            else
            {
                return IKCtrlData.IKAttachType.Point;
            }
        }

        public override void IkClear(Maid tgt, XtMasterSlave.MsLinkConfig mscfg)
        {
            List<string> listHand = new List<string> { "右手", "左手" };
            IkClear(tgt, listHand, mscfg);
        }

        //public override void IkClear(Maid tgt, List<string> listHand, XtMasterSlave.MsLinkConfig mscfg, IKCtrlData.IKAttachType IkType = (IKCtrlData.IKAttachType)(-1))
        public override void IkClear(Maid tgt, List<string> listHand, XtMasterSlave.MsLinkConfig mscfg, int IkType = (-1))
        {
            List<IKCtrlData.IKAttachType> listTypes = new List<IKCtrlData.IKAttachType>
                                    { IKCtrlData.IKAttachType.NewPoint, IKCtrlData.IKAttachType.Rotate };

            listHand.ToList().ForEach(h =>
            {
                var ctrl = tgt.body0.IKCtrl.GetIKData(h);
                listTypes.ForEach(t =>
                {
                    var iks = ctrl.GetIKParam(t);

                    if (IkXT.IsIkCtrlO117)
                    {
                        ctrl.SetIKSetting(t, false, null, -1, string.Empty, null, null, Vector3.zero, false, 0f);
                        //iks.SetIKSetting(null, -1, string.Empty, null, null, Vector3.zero, false, 0f);
                        ctrl.Detach(t, 0f);
                    }
                    else
                    {
                        iks.TgtMaid = null;
                        iks.Tgt_AttachSlot = -1;
                        iks.Tgt_AttachName = string.Empty;
                        iks.Target = null;
                        iks.AxisTgt = null;
                        iks.TgtOffset = Vector3.zero;
                        //iks.IsTgtAxis
                    }

                    if (iks.MyType != IKCtrlData.IKAttachType.Rotate)
                    {
                        if (IkType >= 0 && IkType != (int)IKCtrlData.IKAttachType.Rotate
                                && Enum.IsDefined(typeof(IKCtrlData.IKAttachType), IkType))
                        {
                            iks.ChangePointType((IKCtrlData.IKAttachType)IkType);
                        }
                        else
                        {
                            if (mscfg != null)
                                iks.ChangePointType(GetDefType(mscfg));/*fix v5.0
                            else
                                iks.ChangePointType(IKCtrlData.IKAttachType.NewPoint);*/
                        }
                    }
                });
            });
        }

        public override void CopyHandIK(Maid master, Maid slave, XtMasterSlave.v3Offsets[] v3ofs, int num_)
        {
            List<string> listHand = new List<string> { "右手", "左手" };
            List<IKCtrlData.IKAttachType> listTypes = new List<IKCtrlData.IKAttachType>
                                    { IKCtrlData.IKAttachType.NewPoint, IKCtrlData.IKAttachType.Rotate };

            listHand.ToList().ForEach(h =>
            {
                var ikcm = master.body0.IKCtrl.GetIKData(h);
                var ikcs = slave.body0.IKCtrl.GetIKData(h);
                listTypes.ForEach(t =>
                {
                    var ikm = ikcm.GetIKParam(t);
                    var iks = ikcs.GetIKParam(t);

                    if (!(string.IsNullOrEmpty(ikm.Tgt_AttachName) && ikm.Target == null))
                    {
                        //Console.WriteLine("{0} {1} -> {2} {3} {4}", h, t, ikm.MyType, ikm.Tgt_AttachName, ikm.Target);

                        if (iks.MyType != IKCtrlData.IKAttachType.Rotate)
                        {
                            if (ikm.MyType != IKCtrlData.IKAttachType.Rotate)
                            {
                                iks.ChangePointType(ikm.MyType);
                            }
                        }

                        float fixAngle(float angle)
                        {
                            while (Mathf.Abs(angle) > 360f)
                            {
                                angle = ((!(angle < 0f)) ? (angle - 360f) : (angle + 360f));
                            }
                            return angle;
                        }

                        if (IkXT.IsIkCtrlO117)
                        {
                            ikcs.SetIKSetting(t, false, ikm.TgtMaid, ikm.Tgt_AttachSlot, ikm.Tgt_AttachName, ikm.AxisTgt, ikm.Target, ikm.TgtOffset, ikm.DoAnimation, ikm.BlendTime);
                            //iks.SetIKSetting(ikm.TgtMaid, ikm.Tgt_AttachSlot, ikm.Tgt_AttachName, ikm.AxisTgt, ikm.Target, ikm.TgtOffset, ikm.DoAnimation, ikm.BlendTime);
                        }
                        else
                        {
                            iks.TgtMaid = ikm.TgtMaid;
                            iks.Tgt_AttachSlot = ikm.Tgt_AttachSlot;
                            iks.Tgt_AttachName = ikm.Tgt_AttachName;
                            iks.Target = ikm.Target;
                            iks.AxisTgt = ikm.AxisTgt;
                        }

                        if (iks.IsPointAttach)
                        {
                            iks.TgtOffset = ikm.TgtOffset;
                            if (h == "右手")
                                iks.TgtOffset += v3ofs[num_].v3HandROffset;
                            else
                                iks.TgtOffset += v3ofs[num_].v3HandLOffset;
                        }
                        else
                        {
                            Vector3 v3rot = Vector3.zero;
                            if (h == "右手")
                                v3rot = v3ofs[num_].v3HandROffsetRot;
                            else
                                v3rot = v3ofs[num_].v3HandLOffsetRot;

                            iks.TgtOffset.x = fixAngle(ikm.TgtOffset.x + v3rot.x);
                            iks.TgtOffset.y = fixAngle(ikm.TgtOffset.y + v3rot.y);
                            iks.TgtOffset.z = fixAngle(ikm.TgtOffset.z + v3rot.z);
                        }
                    }

                });
            });
        }

        public static bool boAnime = false; //?

        public override void SetHandIKRotate(string handName, Maid master, Maid slave, string boneTgtname, Vector3 v3HandLOffsetRot)
        {
            slave.IKTargetToBone(handName, master, boneTgtname, v3HandLOffsetRot, IKCtrlData.IKAttachType.Rotate, false, 0f, boAnime, false);
        }

        public override void SetHandIKTarget(XtMasterSlave.MsLinkConfig mscfg, string handName, Maid master, Maid slave, int slot_no, string attach_name, Transform target, Vector3 v3HandLOffset)
        {
            slave.IKCtrl.GetIKData(handName).SetIKSetting(GetDefType(mscfg), false, master, slot_no, attach_name, null, target, v3HandLOffset, boAnime, 0f);
        }

        public override object GetIKCmo(TBody body, string hand = "右手")
        {
            return body.IKCtrl.GetIKData(hand).IKCmo;

            /*
            if (hand == "右手")
                return body.IKCtrl.GetIKData("右手").IKCmo;
            else
                return body.IKCtrl.GetIKData("左手").IKCmo;
                */
        }

        public override bool IKUpdate(TBody body)
        {
            body.IKCtrl.IKUpdate();
            return true;
        }

        public override bool GetIKCmoPosRot(TBody body, out Vector3 pos, out Quaternion rot, string hand = "右手")
        {
            var ctrl = body.IKCtrl.GetIKData(hand);
            bool proc = false;

            pos = Vector3.zero;
            rot = Quaternion.identity;

            var data = ctrl.GetIKParam(IKCtrlData.IKAttachType.Point);
            if (data.Target != null)
            {
                pos = data.Target.position;
                rot = data.Target.rotation;
                proc = true;
            }
            else if (data.Tgt_AttachName != string.Empty)
            {
                if (data.TgtMaid != null && data.TgtMaid.body0 != null && data.Tgt_AttachSlot >= 0 && data.TgtMaid.body0.goSlot[data.Tgt_AttachSlot].morph != null)
                {
                    Vector3 vector;
                    data.TgtMaid.body0.goSlot[data.Tgt_AttachSlot].morph.GetAttachPoint(data.Tgt_AttachName, out pos, out rot, out vector, false);
                    proc = true;
                }
                else
                {
                    data.Tgt_AttachName = string.Empty;
                }
            }

            return proc;
        }

        public override bool IKCmoUpdate(TBody body, Transform trh, Vector3 offset, string hand = "右手")
        {
            var ctrl = body.IKCtrl.GetIKData(hand);

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            bool proc = GetIKCmoPosRot(body, out pos, out rot, hand);

            if (proc)
            {
                ctrl.IKCmo.Porc(trh.parent.parent, trh.parent, trh, pos, rot * offset, ctrl);
                return true;
            }
            return false;
        }
    }
#endif
}

