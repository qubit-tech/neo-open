using Neo.AddIn.Contracts.MotionControllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using N = Neo.AddIn.GTS.Api;

namespace Neo.AddIn.GTS
{
    static class Extentions
    {
        public static UInt16 ToUInt16(this bool status)
        {
            if (status)
                return 1;
            else
                return 0;
        }
        public static Int16 ToInt16(this bool status)
        {
            if (status)
                return 1;
            else
                return 0;
        }
        public static int Round(this double x)
        {
            return (int)Math.Round(x);
        }

        public static void Verify(this short status)
        {
#if DEBUG
            if (status == 0)
                return;
            throw new ApplicationException(string.Format("gts error: {0}", status));
#endif
        }
    }

    public sealed class MotionControllerManager : MarshalByRefObject, IMotionControllerManager
    {
        IMotionController[] _motionControllers;

        public MotionControllerManager()
        { }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public IMotionController[] GetMotionControllers()
        {
            if (_motionControllers == null)
            {

                _motionControllers =
                    Enumerable.Range(0, 2)
                    .Select(Card.Of)
                    .Where(c => N.GT_Open(c) == 0)
                    .Select(c => (IMotionController)new MotionController(c))
                    .ToArray();
            }
            return _motionControllers;
        }
    }

    sealed class MotionController : MarshalByRefObject, IMotionController
    {
        readonly Card card;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        internal MotionController(Card card)
        {
            this.card = card;
        }

        int GetRawStatus(int axis)
        {
            int s;
            N.GT_GetSts(card, Axis.Of(axis), out s).Verify();
            return s;
        }

        static bool CheckBit(int s, int bit)
        {
            return (s & (1 << bit)) != 0;
        }

        public AxisStatus GetStatusMask(int axis)
        {
            return AxisStatus.ALM | AxisStatus.INP | AxisStatus.MEL | AxisStatus.PEL | AxisStatus.RDY | AxisStatus.ORG;
        }

        public AxisStatus GetStatus(int axis)
        {
            var raw = GetRawStatus(axis);
            var status = new AxisStatus();
            Action<int, AxisStatus> check_1 = (bit, s) =>
            {
                if (CheckBit(raw, bit))
                {
                    status |= s;
                }
            };
            Action<int, AxisStatus> check_0 = (bit, s) =>
            {
                if (!CheckBit(raw, bit))
                {
                    status |= s;
                }
            };
            check_1(1, AxisStatus.ALM);
            check_1(5, AxisStatus.PEL);
            check_1(6, AxisStatus.MEL);
            check_1(8, AxisStatus.EMG);
            check_1(4, AxisStatus.ORG);
            check_0(10, AxisStatus.INP);
            check_1(9, AxisStatus.RDY);
            return status;
        }


        public bool GetDI(int port)
        {
            Func<short, int> getDI = ty =>
            {
                int s;
                N.GT_GetDi(card, ty, out s).Verify();
                return s;
            };

            var p = port;
            if (p < 0)
                return false;

            if (p < 16)
            {
                return CheckBit(getDI(Api.MC_GPI), p);
            }
            p -= 16;

            if (p < 8)
            {
                return CheckBit(getDI(Api.MC_LIMIT_POSITIVE), p);
            }
            p -= 8;

            if (p < 8)
            {
                return CheckBit(getDI(Api.MC_LIMIT_NEGATIVE), p);
            }
            p -= 8;

            if (p < 8)
            {
                return CheckBit(getDI(Api.MC_ALARM), p);
            }
            p -= 8;

            if (p < 8)
            {
                return CheckBit(getDI(Api.MC_HOME), p);
            }
            p -= 8;

            if (p < 8)
            {
                return CheckBit(getDI(Api.MC_ARRIVE), p);
            }

            return false;
        }

        public bool GetDO(int port)
        {
            Func<short, int> getDO = ty =>
            {
                int s;
                N.GT_GetDo(card, ty, out s).Verify();
                return s;
            };

            var p = port;
            if (p < 0)
                return false;

            if (p < 16)
            {
                return CheckBit(getDO(Api.MC_GPO), p);
            }
            p -= 16;

            if (p < 8)
            {
                return CheckBit(getDO(Api.MC_ENABLE), p);
            }
            p -= 8;

            if (p < 8)
            {
                return CheckBit(getDO(Api.MC_CLEAR), p);
            }

            return false;
        }

        public double GetPosition(int axis)
        {
            double pos;
            N.GT_GetAxisPrfPos(card, Axis.Of(axis), out pos).Verify();
            return pos;
        }

        void HomeAsync(int axis, double speed)
        {
            var a = Axis.Of(axis);
            var pls_ms = speed / 1000.0;
            var acc_ms = (speed * 10.0) / (1000.0 * 1000.0);
            N.GT_ClrSts(card, a, 1).Verify();
            N.GT_AxisOn(card, a).Verify();
            N.GT_SetCaptureMode(card, a, Api.CAPTURE_HOME).Verify();
            N.GT_ZeroPos(card, a, 1).Verify();
            Offset(axis, -10000000, speed, 0.01, 0.01);

            Task.Run(async () =>
            {
                try
                {
                    short cap = 0;
                    int pos = 0;
                    do
                    {
                        N.GT_GetCaptureStatus(card, a, out cap, out pos).Verify();
                        if (cap != 0)
                        {
                            N.GT_SetPos(card, a, pos).Verify();
                            N.GT_Update(card, a.Mask).Verify();
                            break;
                        }
                        await Task.Delay(1);
                    } while (!IsStop(axis));

                    while (!IsStop(axis))
                    {
                        await Task.Delay(1);
                    }

                    return cap != 0;
                }
                catch(Exception)
                {
                    return false;
                }
            });
        }

        public void Home(int axis, double direction, double speed)
        {
            if (axis < 0 || axis >= 8)
                return;

            HomeAsync(axis, speed);
        }

        public void Locate(int axis, double position, double speed, double taccel_sec, double tdecel_sec)
        {
            var pls_ms = speed / 1000.0;
            var acc_ms = speed / taccel_sec / (1000.0 * 1000.0);
            var dec_ms = speed / tdecel_sec / (1000.0 * 1000.0);
            var a = Axis.Of(axis);
            N.GT_ClrSts(card, a, 1).Verify();
            N.GT_AxisOn(card, a).Verify();
            N.GT_PrfTrap(card, a).Verify();
            Api.TTrapPrm param;
            N.GT_GetTrapPrm(card, a, out param).Verify();
            param.velStart = 0;
            param.acc = acc_ms;
            param.dec = dec_ms;

            N.GT_SetTrapPrm(card, a, ref param).Verify();
            N.GT_SetPos(card, a, position.Round()).Verify();
            N.GT_SetVel(card, a, pls_ms).Verify();
            N.GT_Update(card, a.Mask).Verify();
        }

        public void Offset(int axis, double offset, double speed, double taccel_sec, double tdecel_sec)
        {
            int pos;
            N.GT_GetPos(card, Axis.Of(axis), out pos).Verify();
            Locate(axis, offset + pos, speed, taccel_sec, tdecel_sec);
        }

        public void Initialize()
        {
            var path = typeof(MotionController).Assembly.Location;
            var dir = System.IO.Path.GetDirectoryName(path);
            var cfg = System.IO.Path.Combine(dir, "gts.cfg");
            if (System.IO.File.Exists(cfg))
            {
                N.GT_LoadConfig(card, cfg).Verify();
            }
        }

        public void SetDO(int port, bool status)
        {
            var p = port;
            if (p < 0)
                return;

            if (p < 16)
            {
                N.GT_SetDoBit(card, Api.MC_GPO, (short)(p + 1), status.ToInt16()).Verify();
                return;
            }
            p -= 16;

            if (p < 8)
            {
                N.GT_SetDoBit(card, Api.MC_ENABLE, (short)(p + 1), status.ToInt16()).Verify();
                return;
            }
            p -= 8;

            if (p < 8)
            {
                N.GT_SetDoBit(card, Api.MC_CLEAR, (short)(p + 1), status.ToInt16()).Verify();
                return;
            }
        }

        public void ZeroPosition(int axis)
        {
            N.GT_ZeroPos(card, Axis.Of(axis), 1).Verify();
        }

        public void Stop(int axis)
        {
            N.GT_Stop(card, Axis.Of(axis).Mask, 0).Verify();
        }

        public bool IsStop(int axis)
        {
            var s = GetRawStatus(axis);
            return !CheckBit(s, 10);
        }

        public void ToSpeed(int axis, double speed, double taccel_sec, double tdecel_sec)
        {
            var pls_ms = speed / 1000.0;
            var acc_ms = speed / taccel_sec / (1000.0 * 1000.0);
            var dec_ms = speed / tdecel_sec / (1000.0 * 1000.0);
            var a = Axis.Of(axis);
            N.GT_AxisOn(card, a).Verify();
            N.GT_ClrSts(card, a, 1).Verify();
            N.GT_PrfJog(card, a).Verify();
            Api.TJogPrm param;
            N.GT_GetJogPrm(card, a, out param).Verify();
            param.acc = acc_ms;
            param.dec = acc_ms;
            N.GT_SetJogPrm(card, a, ref param).Verify();
            N.GT_SetVel(card, a, pls_ms).Verify();
            N.GT_Update(card, a.Mask).Verify();
        }
    }
}
