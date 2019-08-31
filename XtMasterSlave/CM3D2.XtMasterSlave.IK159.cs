#define IK159

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ExtensionMethods.MyExtensions;
using CM3D2.XtMasterSlave.Plugin;
using UnityEngine;

//XtMasterSlave_IK159.Ik159Inst
namespace XtMasterSlave_IK159
{
#if IK159
    public class Ik159Inst : IkInst
    {
        public override bool IsNewPointIK(Maid m, string hand = "右手")
        {
            var ikP = m.body0.StrIKDataPair[hand].GetIKParam(IKMgrData.IKAttachType.Point);
            return (ikP.MyType == IKMgrData.IKAttachType.NewPoint);
        }

        public override object GetIkPoint(TBody body, string hand = "右手")
        {
            var obj = body.StrIKDataPair[hand].GetIKParam(IKMgrData.IKAttachType.Point);
            if (obj == null)
                obj = body.StrIKDataPair[hand].GetIKParam(IKMgrData.IKAttachType.NewPoint);

            return obj;
        }

        private IKMgrData.IKAttachType GetDefType(XtMasterSlave.MsLinkConfig mscfg)
        {
            if (mscfg.doIK159NewPointToDef)
            {
                return IKMgrData.IKAttachType.NewPoint;
            }
            else
            {
                return IKMgrData.IKAttachType.Point;
            }
        }

        public override void IkClear(Maid tgt, XtMasterSlave.MsLinkConfig mscfg)
        {
            List<string> listHand = new List<string> { "右手", "左手" };
            IkClear(tgt, listHand, mscfg);
        }

        //public override void IkClear(Maid tgt, List<string> listHand, XtMasterSlave.MsLinkConfig mscfg, IKMgrData.IKAttachType IkType = (IKMgrData.IKAttachType)(-1))
        public override void IkClear(Maid tgt, List<string> listHand, XtMasterSlave.MsLinkConfig mscfg, int IkType = (-1))
        {
            List<IKMgrData.IKAttachType> listTypes = new List<IKMgrData.IKAttachType>
                                    { IKMgrData.IKAttachType.NewPoint, IKMgrData.IKAttachType.Rotate };

            listHand.ToList().ForEach(h =>
            {
                listTypes.ForEach(t =>
                {
                    var iks = tgt.body0.StrIKDataPair[h].GetIKParam(t);

                    iks.TgtMaid = null;
                    iks.Tgt_AttachSlot = -1;
                    iks.Tgt_AttachName = string.Empty;
                    iks.Target = null;
                    iks.AxisTgt = null;
                    iks.TgtOffset = Vector3.zero;
                    //iks.IsTgtAxis

                    if (iks.MyType != IKMgrData.IKAttachType.Rotate)
                    {
                        if (IkType >= 0 && IkType != (int)IKMgrData.IKAttachType.Rotate
                                && Enum.IsDefined(typeof(IKMgrData.IKAttachType), IkType))
                        {
                            iks.ChangePointType((IKMgrData.IKAttachType)IkType);
                        }
                        else
                        {
                            if (mscfg != null)
                                iks.ChangePointType(GetDefType(mscfg));
                            else
                                iks.ChangePointType(IKMgrData.IKAttachType.NewPoint);
                        }
                    }
                });
            });
        }

        public override void CopyHandIK(Maid master, Maid slave, XtMasterSlave.v3Offsets[] v3ofs, int num_)
        {
            List<string> listHand = new List<string> { "右手", "左手" };
            List<IKMgrData.IKAttachType> listTypes = new List<IKMgrData.IKAttachType>
                                    { IKMgrData.IKAttachType.NewPoint, IKMgrData.IKAttachType.Rotate };

            listHand.ToList().ForEach(h =>
            {
                listTypes.ForEach(t =>
                {
                    var ikm = master.body0.StrIKDataPair[h].GetIKParam(t);
                    var iks = slave.body0.StrIKDataPair[h].GetIKParam(t);

                    if (!(string.IsNullOrEmpty(ikm.Tgt_AttachName) && ikm.Target == null))
                    {
                        //Console.WriteLine("{0} {1} -> {2} {3} {4}", h, t, ikm.MyType, ikm.Tgt_AttachName, ikm.Target);

                        if (iks.MyType != IKMgrData.IKAttachType.Rotate)
                        {
                            if (ikm.MyType != IKMgrData.IKAttachType.Rotate)
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

                        iks.TgtMaid = ikm.TgtMaid;
                        iks.Tgt_AttachSlot = ikm.Tgt_AttachSlot;
                        iks.Tgt_AttachName = ikm.Tgt_AttachName;
                        iks.Target = ikm.Target;
                        iks.AxisTgt = ikm.AxisTgt;

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

        public override void SetHandIKRotate(string handName, Maid master, Maid slave, string boneTgtname, Vector3 v3HandLOffsetRot)
        {
            slave.IKTargetToBone(handName, master, boneTgtname, v3HandLOffsetRot, IKMgrData.IKAttachType.Rotate, false);
        }

        public override object GetIKCmo(TBody body, string hand = "右手")
        {
            if (hand == "右手")
                return body.IKHandR.IKCmo;
            else
                return body.IKHandL.IKCmo;
        }

        public override bool GetIKCmoPosRot(TBody body, out Vector3 pos, out Quaternion rot, string hand = "右手")
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;

            bool proc = false;
            var data = GetIkPoint(body, hand) as IKMgrData.IKParam;

            if (data.Target != null)
            {
                pos = data.Target.position;
                rot = data.Target.rotation;
                proc = true;
            }
            else if (data.Tgt_AttachName != string.Empty)
            {
                if (data.TgtMaid != null && data.TgtMaid.body0 != null && data.TgtMaid.body0.goSlot[data.Tgt_AttachSlot].morph != null)
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
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            bool proc = GetIKCmoPosRot(body, out pos, out rot, hand);

            var mgr = body.IKHandR;
            if (hand != "右手")
                mgr = body.IKHandL;

            if (proc)
            {
                mgr.IKCmo.Porc(trh.parent.parent, trh.parent, trh, pos, rot * offset);
                return true;
            }
            return false;
        }
    }
#endif
}

