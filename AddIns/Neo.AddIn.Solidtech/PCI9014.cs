using Neo.AddIn.Contracts.MotionControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.AddIn.Solidtech
{
    static class Extensions
    {
        public static void Verify(this int status)
        {
#if DEBUG
            if (status != 0)
            {
                //throw new Exception(String.Format("PCI9014 ERROR: {0}", status));
                Console.Error.WriteLine("PCI9014 ERROR: {0}", status);
            }
#endif
        }
    }

    public sealed class PCI9014 : MarshalByRefObject, IMotionControllerManager
    {
        public sealed class MotionController : MarshalByRefObject, IMotionController
        {
            public int Id { get; private set; }

            internal MotionController(int id)
            {
                this.Id = id;
            }

            Axis GetAxis(int axis)
            {
                return Axis.Create(this.Id, axis);
            }

            public override object InitializeLifetimeService()
            {
                return null;
            }

            public bool GetDI(int port)
            {
                uint data = 0;
                Api.p9014_get_di_bit(this.Id, (uint)port, ref data).Verify();
                return data == 0;
            }

            public bool GetDO(int port)
            {
                uint data = 0;
                Api.p9014_get_do(this.Id, ref data).Verify();
                return (data & (1u << port)) != 0;
            }

            public double GetPosition(int axis)
            {
                int pos = 0;
                Api.p9014_get_pos(GetAxis(axis), 0, ref pos).Verify();
                return pos;
            }

            public AxisStatus GetStatus(int axis)
            {
                uint raw = 0;
                Api.p9014_get_io_status(GetAxis(axis), ref raw).Verify();
                AxisStatus status = 0;
                Action<int, AxisStatus> CheckBit1 = (bit, mask) =>
                {
                    if ((raw & (1u << bit)) != 0u)
                    {
                        status |= mask;
                    }
                };
                Action<int, AxisStatus> CheckBit0 = (bit, mask) =>
                {
                    if ((raw & (1u << bit)) == 0u)
                    {
                        status |= mask;
                    }
                };
                CheckBit1(0, AxisStatus.PEL);
                CheckBit1(1, AxisStatus.MEL);
                CheckBit1(2, AxisStatus.ORG);
                CheckBit0(4, AxisStatus.EMG);
                if ((status & AxisStatus.PEL) == AxisStatus.PEL)
                    status |= AxisStatus.ALM;
                if ((status & AxisStatus.MEL) == AxisStatus.MEL)
                    status |= AxisStatus.ALM;
                return status;
            }

            public AxisStatus GetStatusMask(int axis)
            {
                return AxisStatus.EMG | AxisStatus.MEL | AxisStatus.ORG | AxisStatus.PEL | AxisStatus.ALM;
            }

            public void Home(int axis, double direction, double speed)
            {
                var a = GetAxis(axis);
                int mode = 0;
                int level = 0;
                int ez_level = 0;
                Api.p9014_home_config(a, mode, level, ez_level).Verify();

                // 设置运动参数
                speed = Math.Abs(speed);
                double acc = (speed - speed / 10) / 0.1;
                Api.p9014_set_t_profile(a, speed / 10, speed, acc, acc).Verify();

                var dir = direction > 0 ? 1 : 0;
                Api.p9014_home_move(a, dir).Verify();
            }

            public void Initialize()
            {
                Api.p9014_set_el_level(GetAxis(0), 1).Verify();
                Api.p9014_set_el_level(GetAxis(1), 1).Verify();
                Api.p9014_set_el_level(GetAxis(2), 0).Verify();
                Api.p9014_set_el_level(GetAxis(3), 1).Verify();
            }

            public bool IsStop(int axis)
            {
                uint status = 0;
                Api.p9014_get_motion_status(GetAxis(axis), ref status).Verify();
                return status == 0;
            }

            public void Locate(int axis, double position, double speed, double tacc_sec, double tdec_sec)
            {
                var a = GetAxis(axis);
                // 设置运动参数
                speed = Math.Abs(speed);
                double acc = (speed - speed / 10) / tacc_sec;
                double dec = (speed - speed / 10) / tdec_sec;
                Api.p9014_set_t_profile(a, speed / 10, speed, acc, dec).Verify();

                // 
                var mode = 1;
                var vel_mode = 2;
                Api.p9014_pmove(a, (int)Math.Round(position), mode, vel_mode).Verify();
            }

            public void Offset(int axis, double offset, double speed, double tacc_sec, double tdec_sec)
            {
                var a = GetAxis(axis);
                // 设置运动参数
                speed = Math.Abs(speed);
                double acc = (speed - speed / 10) / tacc_sec;
                double dec = (speed - speed / 10) / tdec_sec;
                Api.p9014_set_t_profile(a, speed / 10, speed, acc, dec).Verify();

                // 
                var mode = 0;
                var vel_mode = 2;
                Api.p9014_pmove(a, (int)Math.Round(offset), mode, vel_mode).Verify();
            }

            public void SetDO(int port, bool status)
            {
                Api.p9014_set_do_bit(this.Id, (uint)port, status ? 0u : 1u).Verify();
            }

            public void Stop(int axis)
            {
                Api.p9014_stop(GetAxis(axis), 0).Verify();
            }

            public void ToSpeed(int axis, double speed, double tacc_sec, double tdec_sec)
            {
                var a = GetAxis(axis);

                int dir = speed >= 0 ? 1 : 0;
                int mode = 2;

                // 设置运动参数
                speed = Math.Abs(speed);
                double acc = (speed - speed / 10) / tacc_sec;
                double dec = (speed - speed / 10) / tdec_sec;
                Api.p9014_set_t_profile(a, speed / 10, speed, acc, dec).Verify();

                // 开始运动
                Api.p9014_vmove(a, dir, mode);//.Verify();
            }

            public void ZeroPosition(int axis)
            {

            }
        }

        IMotionController[] _motionControllers;

        public PCI9014()
        { }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public IMotionController[] GetMotionControllers()
        {
            if (_motionControllers == null)
            {
                int cnt = 16;
                var ids = new int[cnt];
                Api.p9014_initial(ref cnt, ids).Verify();
                _motionControllers = ids.Take(cnt).Select(id => new MotionController(id) as IMotionController).ToArray();
            }
            return _motionControllers;
        }
    }
}
