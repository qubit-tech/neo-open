using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Neo.AddIn.GTS
{
    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct Axis
    {
        public readonly short Id;

        Axis(short id)
        {
            this.Id = id;
        }

        public static Axis Of(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("index");
            return new Axis((short)(index + 1));
        }

        public int Mask { get { return 1 << (this.Id - 1); } }
    }

    [StructLayout(LayoutKind.Sequential, Size = 2)]
    struct Card
    {
        public readonly short Index;

        Card(short index)
        {
            this.Index = index;
        }

        public static Card Of(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("index");
            return new Card((short)index);
        }
    }

    static class Api
    {
        public const string DLL = "gts.dll";

        public const short DLL_VERSION_0 = 2;
        public const short DLL_VERSION_1 = 1;
        public const short DLL_VERSION_2 = 0;

        public const short DLL_VERSION_3 = 1;
        public const short DLL_VERSION_4 = 5;
        public const short DLL_VERSION_5 = 0;
        public const short DLL_VERSION_6 = 6;
        public const short DLL_VERSION_7 = 0;
        public const short DLL_VERSION_8 = 7;

        public const short MC_NONE = -1;

        public const short MC_LIMIT_POSITIVE = 0;
        public const short MC_LIMIT_NEGATIVE = 1;
        public const short MC_ALARM = 2;
        public const short MC_HOME = 3;
        public const short MC_GPI = 4;
        public const short MC_ARRIVE = 5;
        public const short MC_MPG = 6;

        public const short MC_ENABLE = 10;
        public const short MC_CLEAR = 11;
        public const short MC_GPO = 12;

        public const short MC_DAC = 20;
        public const short MC_STEP = 21;
        public const short MC_PULSE = 22;
        public const short MC_ENCODER = 23;
        public const short MC_ADC = 24;

        public const short MC_AXIS = 30;
        public const short MC_PROFILE = 31;
        public const short MC_CONTROL = 32;

        public const short CAPTURE_HOME = 1;
        public const short CAPTURE_INDEX = 2;
        public const short CAPTURE_PROBE = 3;
        public const short CAPTURE_HSIO0 = 6;
        public const short CAPTURE_HSIO1 = 7;
        public const short CAPTURE_HOME_GPI = 8;

        public const short PT_MODE_STATIC = 0;
        public const short PT_MODE_DYNAMIC = 1;

        public const short PT_SEGMENT_NORMAL = 0;
        public const short PT_SEGMENT_EVEN = 1;
        public const short PT_SEGMENT_STOP = 2;

        public const short GEAR_MASTER_ENCODER = 1;
        public const short GEAR_MASTER_PROFILE = 2;
        public const short GEAR_MASTER_AXIS = 3;

        public const short FOLLOW_MASTER_ENCODER = 1;
        public const short FOLLOW_MASTER_PROFILE = 2;
        public const short FOLLOW_MASTER_AXIS = 3;

        public const short FOLLOW_EVENT_START = 1;
        public const short FOLLOW_EVENT_PASS = 2;

        public const short GEAR_EVENT_START = 1;
        public const short GEAR_EVENT_PASS = 2;
        public const short GEAR_EVENT_AREA = 5;

        public const short FOLLOW_SEGMENT_NORMAL = 0;
        public const short FOLLOW_SEGMENT_EVEN = 1;
        public const short FOLLOW_SEGMENT_STOP = 2;
        public const short FOLLOW_SEGMENT_CONTINUE = 3;

        public const short INTERPOLATION_AXIS_MAX = 4;
        public const short CRD_FIFO_MAX = 4096;
        public const short CRD_MAX = 2;
        public const short CRD_OPERATION_DATA_EXT_MAX = 2;

        public const short CRD_OPERATION_TYPE_NONE = 0;
        public const short CRD_OPERATION_TYPE_BUF_IO_DELAY = 1;
        public const short CRD_OPERATION_TYPE_LASER_ON = 2;
        public const short CRD_OPERATION_TYPE_LASER_OFF = 3;
        public const short CRD_OPERATION_TYPE_BUF_DA = 4;
        public const short CRD_OPERATION_TYPE_LASER_CMD = 5;
        public const short CRD_OPERATION_TYPE_LASER_FOLLOW = 6;
        public const short CRD_OPERATION_TYPE_LMTS_ON = 7;
        public const short CRD_OPERATION_TYPE_LMTS_OFF = 8;
        public const short CRD_OPERATION_TYPE_SET_STOP_IO = 9;
        public const short CRD_OPERATION_TYPE_BUF_MOVE = 10;
        public const short CRD_OPERATION_TYPE_BUF_GEAR = 11;
        public const short CRD_OPERATION_TYPE_SET_SEG_NUM = 12;
        public const short CRD_OPERATION_TYPE_STOP_MOTION = 13;
        public const short CRD_OPERATION_TYPE_SET_VAR_VALUE = 14;
        public const short CRD_OPERATION_TYPE_JUMP_NEXT_SEG = 15;
        public const short CRD_OPERATION_TYPE_SYNCH_PRF_POS = 16;
        public const short CRD_OPERATION_TYPE_VIRTUAL_TO_ACTUAL = 17;
        public const short CRD_OPERATION_TYPE_SET_USER_VAR = 18;
        public const short CRD_OPERATION_TYPE_SET_DO_BIT_PULSE = 19;
        public const short CRD_OPERATION_TYPE_BUF_COMPAREPULSE = 20;
        public const short CRD_OPERATION_TYPE_LASER_ON_EX = 21;
        public const short CRD_OPERATION_TYPE_LASER_OFF_EX = 22;
        public const short CRD_OPERATION_TYPE_LASER_CMD_EX = 23;
        public const short CRD_OPERATION_TYPE_LASER_FOLLOW_RATIO_EX = 24;
        public const short CRD_OPERATION_TYPE_LASER_FOLLOW_MODE = 25;

        public const short INTERPOLATION_MOTION_TYPE_LINE = 0;
        public const short INTERPOLATION_MOTION_TYPE_CIRCLE = 1;
        public const short INTERPOLATION_MOTION_TYPE_HELIX = 2;
        public const short INTERPOLATION_MOTION_TYPE_CIRCLE_3D = 3;

        public const short INTERPOLATION_CIRCLE_PLAT_XY = 0;
        public const short INTERPOLATION_CIRCLE_PLAT_YZ = 1;
        public const short INTERPOLATION_CIRCLE_PLAT_ZX = 2;

        public const short INTERPOLATION_HELIX_CIRCLE_XY_LINE_Z = 0;
        public const short INTERPOLATION_HELIX_CIRCLE_YZ_LINE_X = 1;
        public const short INTERPOLATION_HELIX_CIRCLE_ZX_LINE_Y = 2;

        public const short INTERPOLATION_CIRCLE_DIR_CW = 0;
        public const short INTERPOLATION_CIRCLE_DIR_CCW = 1;

        public const short COMPARE_PORT_HSIO = 0;
        public const short COMPARE_PORT_GPO = 1;

        public const short COMPARE2D_MODE_2D = 1;
        public const short COMPARE2D_MODE_1D = 0;

        public const short INTERFACEBOARD20 = 2;
        public const short INTERFACEBOARD30 = 3;

        public const short AXIS_LASER = 7;
        public const short AXIS_LASER_EX = 8;

        public const short LASER_CTRL_MODE_PWM1 = 0;
        public const short LASER_CTRL_FREQUENCY = 1;
        public const short LASER_CTRL_VOLTAGE = 2;
        public const short LASER_CTRL_MODE_PWM2 = 3;

        [StructLayout(LayoutKind.Sequential)]
        public struct TTrapPrm
        {
            public double acc;
            public double dec;
            public double velStart;
            public short smoothTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TJogPrm
        {
            public double acc;
            public double dec;
            public double smooth;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TPid
        {
            public double kp;
            public double ki;
            public double kd;
            public double kvff;
            public double kaff;

            public int integralLimit;
            public int derivativeLimit;
            public short limit;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TThreadSts
        {
            public short run;
            public short error;
            public double result;
            public short line;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TVarInfo
        {
            public short id;
            public short dataType;
            public double dumb0;
            public double dumb1;
            public double dumb2;
            public double dumb3;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCompileInfo
        {
            public string pFileName;
            public short pLineNo1;
            public short pLineNo2;
            public string pMessage;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCrdPrm
        {
            public short dimension;
            public short profile1;
            public short profile2;
            public short profile3;
            public short profile4;
            public short profile5;
            public short profile6;
            public short profile7;
            public short profile8;

            public double synVelMax;
            public double synAccMax;
            public short evenTime;
            public short setOriginFlag;
            public int originPos1;
            public int originPos2;
            public int originPos3;
            public int originPos4;
            public int originPos5;
            public int originPos6;
            public int originPos7;
            public int originPos8;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCrdBufOperation
        {
            public short flag;
            public ushort delay;
            public short doType;
            public ushort doMask;
            public ushort doValue;
            public ushort dataExt1;
            public ushort dataExt2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCrdData
        {
            public short motionType;
            public short circlePlat;
            public int posX;
            public int posY;
            public int posZ;
            public int posA;
            public double radius;
            public short circleDir;
            public double lCenterX;
            public double lCenterY;
            public double vel;
            public double acc;
            public short velEndZero;
            public TCrdBufOperation operation;

            public double cosX;
            public double cosY;
            public double cosZ;
            public double cosA;
            public double velEnd;
            public double velEndAdjust;
            public double r;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TTrigger
        {
            public short encoder;
            public short probeType;
            public short probeIndex;
            public short offset;
            public short windowOnly;
            public int firstPosition;
            public int lastPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TTriggerStatus
        {
            public short execute;
            public short done;
            public int position;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct T2DCompareData
        {
            public int px;
            public int py;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct T2DComparePrm
        {
            public short encx;
            public short ency;
            public short source;
            public short outputType;
            public short startLevel;
            public short time;
            public short maxerr;
            public short threshold;
        }

        [DllImport(DLL)]
        public static extern short GT_SetCardNo(Card card, short index);
        [DllImport(DLL)]
        public static extern short GT_GetCardNo(Card card, out short index);

        [DllImport(DLL)]
        public static extern short GT_GetVersion(Card card, out string pVersion);
        [DllImport(DLL)]
        public static extern short GT_GetInterfaceBoardSts(Card card, out short pStatus);
        [DllImport(DLL)]
        public static extern short GT_SetInterfaceBoardSts(Card card, short type);

        [DllImport(DLL)]
        public static extern short GT_Open(Card card, short channel = 0, short param = 1);
        [DllImport(DLL)]
        public static extern short GT_Close(Card card);

        [DllImport(DLL)]
        public static extern short GT_LoadConfig(Card card, string pFile);

        [DllImport(DLL)]
        public static extern short GT_AlarmOff(Card card, Axis axis);
        [DllImport(DLL)]
        public static extern short GT_AlarmOn(Card card, Axis axis);
        [DllImport(DLL)]
        public static extern short GT_LmtsOn(Card card, Axis axis, short limitType);
        [DllImport(DLL)]
        public static extern short GT_LmtsOff(Card card, Axis axis, short limitType);
        [DllImport(DLL)]
        public static extern short GT_ProfileScale(Card card, Axis axis, short alpha, short beta);
        [DllImport(DLL)]
        public static extern short GT_EncScale(Card card, Axis axis, short alpha, short beta);
        [DllImport(DLL)]
        public static extern short GT_StepDir(Card card, short step);
        [DllImport(DLL)]
        public static extern short GT_StepPulse(Card card, short step);
        [DllImport(DLL)]
        public static extern short GT_SetMtrBias(Card card, short dac, short bias);
        [DllImport(DLL)]
        public static extern short GT_GetMtrBias(Card card, short dac, out short pBias);
        [DllImport(DLL)]
        public static extern short GT_SetMtrLmt(Card card, short dac, short limit);
        [DllImport(DLL)]
        public static extern short GT_GetMtrLmt(Card card, short dac, out short pLimit);
        [DllImport(DLL)]
        public static extern short GT_EncSns(Card card, ushort sense);
        [DllImport(DLL)]
        public static extern short GT_EncOn(Card card, short encoder);
        [DllImport(DLL)]
        public static extern short GT_EncOff(Card card, short encoder);
        [DllImport(DLL)]
        public static extern short GT_SetPosErr(Card card, short control, int error);
        [DllImport(DLL)]
        public static extern short GT_GetPosErr(Card card, short control, out int pError);
        [DllImport(DLL)]
        public static extern short GT_SetStopDec(Card card, short profile, double decSmoothStop, double decAbruptStop);
        [DllImport(DLL)]
        public static extern short GT_GetStopDec(Card card, short profile, out double pDecSmoothStop, out double pDecAbruptStop);
        [DllImport(DLL)]
        public static extern short GT_LmtSns(Card card, ushort sense);
        [DllImport(DLL)]
        public static extern short GT_CtrlMode(Card card, Axis axis, short mode);
        [DllImport(DLL)]
        public static extern short GT_SetStopIo(Card card, Axis axis, short stopType, short inputType, short inputIndex);
        [DllImport(DLL)]
        public static extern short GT_GpiSns(Card card, ushort sense);
        [DllImport(DLL)]
        public static extern short GT_SetAdcFilter(Card card, short adc, short filterTime);
        [DllImport(DLL)]
        public static extern short GT_SetAxisPrfVelFilter(Card card, Axis axis, short filterNumExp);
        [DllImport(DLL)]
        public static extern short GT_GetAxisPrfVelFilter(Card card, Axis axis, out short pFilterNumExp);
        [DllImport(DLL)]
        public static extern short GT_SetAxisEncVelFilter(Card card, Axis axis, short filterNumExp);
        [DllImport(DLL)]
        public static extern short GT_GetAxisEncVelFilter(Card card, Axis axis, out short pFilterNumExp);
        [DllImport(DLL)]
        public static extern short GT_SetAxisInputShaping(Card card, Axis axis, short enable, short count, double k);

        [DllImport(DLL)]
        public static extern short GT_SetDo(Card card, short doType, int value);
        [DllImport(DLL)]
        public static extern short GT_SetDoBit(Card card, short doType, short doIndex, short value);
        [DllImport(DLL)]
        public static extern short GT_GetDo(Card card, short doType, out int pValue);
        [DllImport(DLL)]
        public static extern short GT_SetDoBitReverse(Card card, short doType, short doIndex, short value, short reverseTime);
        [DllImport(DLL)]
        public static extern short GT_SetDoMask(Card card, short doType, ushort doMask, int value);
        [DllImport(DLL)]
        public static extern short GT_EnableDoBitPulse(Card card, short doType, short doIndex, ushort highLevelTime, ushort lowLevelTime, int pulseNum, short firstLevel);
        [DllImport(DLL)]
        public static extern short GT_DisableDoBitPulse(Card card, short doType, short doIndex);

        [DllImport(DLL)]
        public static extern short GT_GetDi(Card card, short diType, out int pValue);
        [DllImport(DLL)]
        public static extern short GT_GetDiReverseCount(Card card, short diType, short diIndex, out uint reverseCount, short count);
        [DllImport(DLL)]
        public static extern short GT_SetDiReverseCount(Card card, short diType, short diIndex, ref uint reverseCount, short count);
        [DllImport(DLL)]
        public static extern short GT_GetDiRaw(Card card, short diType, out int pValue);

        [DllImport(DLL)]
        public static extern short GT_SetDac(Card card, short dac, ref short value, short count);
        [DllImport(DLL)]
        public static extern short GT_GetDac(Card card, short dac, out short value, short count = 1, IntPtr pClock = default(IntPtr));

        [DllImport(DLL)]
        public static extern short GT_GetAdc(Card card, short adc, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetAdcValue(Card card, short adc, out short pValue, short count = 1, IntPtr pClock = default(IntPtr));

        [DllImport(DLL)]
        public static extern short GT_SetEncPos(Card card, short encoder, int encPos);
        [DllImport(DLL)]
        public static extern short GT_GetEncPos(Card card, short encoder, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetEncPosPre(Card card, short encoder, out double pValue, short count, uint pClock);
        [DllImport(DLL)]
        public static extern short GT_GetEncVel(Card card, short encoder, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));

        [DllImport(DLL)]
        public static extern short GT_SetCaptureMode(Card card, Axis encoder, short mode);
        [DllImport(DLL)]
        public static extern short GT_GetCaptureMode(Card card, Axis encoder, out short pMode, short count);
        [DllImport(DLL)]
        public static extern short GT_GetCaptureStatus(Card card, Axis encoder, out short pStatus, out int pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_SetCaptureSense(Card card, Axis encoder, short mode, short sense);
        [DllImport(DLL)]
        public static extern short GT_ClearCaptureStatus(Card card, Axis encoder);
        [DllImport(DLL)]
        public static extern short GT_SetCaptureRepeat(Card card, Axis encoder, short count);
        [DllImport(DLL)]
        public static extern short GT_GetCaptureRepeatStatus(Card card, Axis encoder, out short pCount);
        [DllImport(DLL)]
        public static extern short GT_GetCaptureRepeatPos(Card card, Axis encoder, out int pValue, short startNum, short count);
        [DllImport(DLL)]
        public static extern short GT_SetCaptureEncoder(Card card, short trigger, short encoder);
        [DllImport(DLL)]
        public static extern short GT_GetCaptureWidth(Card card, short trigger, out short pWidth, short count);
        [DllImport(DLL)]
        public static extern short GT_GetCaptureHomeGpi(Card card, short trigger, out short pHomeSts, out short pHomePos, out short pGpiSts, out short pGpiPos, short count);

        [DllImport(DLL)]
        public static extern short GT_Reset(Card card);
        [DllImport(DLL)]
        public static extern short GT_GetClock(Card card, out uint pClock, out uint pLoop);
        [DllImport(DLL)]
        public static extern short GT_GetClockHighPrecision(Card card, out uint pClock);

        [DllImport(DLL)]
        public static extern short GT_GetSts(Card card, Axis axis, out int pSts, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_ClrSts(Card card, Axis axis, short count);
        [DllImport(DLL)]
        public static extern short GT_AxisOn(Card card, Axis axis);
        [DllImport(DLL)]
        public static extern short GT_AxisOff(Card card, Axis axis);
        [DllImport(DLL)]
        public static extern short GT_Stop(Card card, int mask, int option);
        [DllImport(DLL)]
        public static extern short GT_SetPrfPos(Card card, Axis axis, int prfPos);
        [DllImport(DLL)]
        public static extern short GT_SynchAxisPos(Card card, int mask);
        [DllImport(DLL)]
        public static extern short GT_ZeroPos(Card card, Axis axis, short count);

        [DllImport(DLL)]
        public static extern short GT_SetSoftLimit(Card card, Axis axis, int positive, int negative);
        [DllImport(DLL)]
        public static extern short GT_GetSoftLimit(Card card, Axis axis, out int pPositive, out int pNegative);
        [DllImport(DLL)]
        public static extern short GT_SetAxisBand(Card card, Axis axis, int band, int time);
        [DllImport(DLL)]
        public static extern short GT_GetAxisBand(Card card, Axis axis, out int pBand, out int pTime);
        [DllImport(DLL)]
        public static extern short GT_SetBacklash(Card card, Axis axis, int compValue, double compChangeValue, int compDir);
        [DllImport(DLL)]
        public static extern short GT_GetBacklash(Card card, Axis axis, out int pCompValue, out double pCompChangeValue, out int pCompDir);
        [DllImport(DLL)]
        public static extern short GT_SetLeadScrewComp(Card card, Axis axis, short n, int startPos, int lenPos, out int pCompPos, out int pCompNeg);
        [DllImport(DLL)]
        public static extern short GT_EnableLeadScrewComp(Card card, Axis axis, short mode);
        [DllImport(DLL)]
        public static extern short GT_GetCompensate(Card card, Axis axis, out double pPitchError, out double pCrossError, out double pBacklashError, out double pEncPos, out double pPrfPos);

        [DllImport(DLL)]
        public static extern short GT_EnableGantry(Card card, short gantryMaster, short gantrySlave, double masterKp, double slaveKp);
        [DllImport(DLL)]
        public static extern short GT_DisableGantry(Card card);
        [DllImport(DLL)]
        public static extern short GT_SetGantryErrLmt(Card card, int gantryErrLmt);
        [DllImport(DLL)]
        public static extern short GT_GetGantryErrLmt(Card card, out int pGantryErrLmt);

        [DllImport(DLL)]
        public static extern short GT_GetPrfPos(Card card, short profile, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetPrfVel(Card card, short profile, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetPrfAcc(Card card, short profile, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetPrfMode(Card card, short profile, out int pValue, short count = 1, IntPtr pClock = default(IntPtr));

        [DllImport(DLL)]
        public static extern short GT_GetAxisPrfPos(Card card, Axis axis, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetAxisPrfVel(Card card, Axis axis, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetAxisPrfAcc(Card card, Axis axis, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetAxisEncPos(Card card, Axis axis, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetAxisEncVel(Card card, Axis axis, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetAxisEncAcc(Card card, Axis axis, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));
        [DllImport(DLL)]
        public static extern short GT_GetAxisError(Card card, Axis axis, out double pValue, short count = 1, IntPtr pClock = default(IntPtr));

        [DllImport(DLL)]
        public static extern short GT_SetLongVar(Card card, short index, int value);
        [DllImport(DLL)]
        public static extern short GT_GetLongVar(Card card, short index, out int pValue);
        [DllImport(DLL)]
        public static extern short GT_SetDoubleVar(Card card, short index, double pValue);
        [DllImport(DLL)]
        public static extern short GT_GetDoubleVar(Card card, short index, out double pValue);

        [DllImport(DLL)]
        public static extern short GT_SetControlFilter(Card card, short control, short index);
        [DllImport(DLL)]
        public static extern short GT_GetControlFilter(Card card, short control, out short pIndex);

        [DllImport(DLL)]
        public static extern short GT_SetPid(Card card, short control, short index, ref TPid pPid);
        [DllImport(DLL)]
        public static extern short GT_GetPid(Card card, short control, short index, out TPid pPid);

        [DllImport(DLL)]
        public static extern short GT_SetKvffFilter(Card card, short control, short index, short kvffFilterExp, double accMax);
        [DllImport(DLL)]
        public static extern short GT_GetKvffFilter(Card card, short control, short index, out short pKvffFilterExp, out double pAccMax);

        [DllImport(DLL)]
        public static extern short GT_Update(Card card, int mask);
        [DllImport(DLL)]
        public static extern short GT_SetPos(Card card, Axis axis, int pos);
        [DllImport(DLL)]
        public static extern short GT_GetPos(Card card, Axis axis, out int pPos);
        [DllImport(DLL)]
        public static extern short GT_SetVel(Card card, Axis axis, double vel);
        [DllImport(DLL)]
        public static extern short GT_GetVel(Card card, Axis axis, out double pVel);

        [DllImport(DLL)]
        public static extern short GT_PrfTrap(Card card, Axis axis);
        [DllImport(DLL)]
        public static extern short GT_SetTrapPrm(Card card, Axis axis, ref TTrapPrm pPrm);
        [DllImport(DLL)]
        public static extern short GT_GetTrapPrm(Card card, Axis axis, out TTrapPrm pPrm);

        [DllImport(DLL)]
        public static extern short GT_PrfJog(Card card, Axis axis);
        [DllImport(DLL)]
        public static extern short GT_SetJogPrm(Card card, Axis axis, ref TJogPrm pPrm);
        [DllImport(DLL)]
        public static extern short GT_GetJogPrm(Card card, Axis axis, out TJogPrm pPrm);

        [DllImport(DLL)]
        public static extern short GT_PrfPt(Card card, short profile, short mode);
        [DllImport(DLL)]
        public static extern short GT_SetPtLoop(Card card, short profile, int loop);
        [DllImport(DLL)]
        public static extern short GT_GetPtLoop(Card card, short profile, out int pLoop);
        [DllImport(DLL)]
        public static extern short GT_PtSpace(Card card, short profile, out short pSpace, short fifo);
        [DllImport(DLL)]
        public static extern short GT_PtData(Card card, short profile, double pos, int time, short type, short fifo);
        [DllImport(DLL)]
        public static extern short GT_PtDataWN(Card card, short profile, double pos, int time, short type, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_PtClear(Card card, short profile, short fifo);
        [DllImport(DLL)]
        public static extern short GT_PtStart(Card card, int mask, int option);
        [DllImport(DLL)]
        public static extern short GT_SetPtMemory(Card card, short profile, short memory);
        [DllImport(DLL)]
        public static extern short GT_GetPtMemory(Card card, short profile, out short pMemory);
        [DllImport(DLL)]
        public static extern short GT_PtGetSegNum(Card card, short profile, out int pSegNum);

        [DllImport(DLL)]
        public static extern short GT_PrfGear(Card card, short profile, short dir);
        [DllImport(DLL)]
        public static extern short GT_SetGearMaster(Card card, short profile, short masterIndex, short masterType, short masterItem);
        [DllImport(DLL)]
        public static extern short GT_GetGearMaster(Card card, short profile, out short pMasterIndex, out short pMasterType, out short pMasterItem);
        [DllImport(DLL)]
        public static extern short GT_SetGearRatio(Card card, short profile, int masterEven, int slaveEven, int masterSlope);
        [DllImport(DLL)]
        public static extern short GT_GetGearRatio(Card card, short profile, out int pMasterEven, out int pSlaveEven, out int pMasterSlope);
        [DllImport(DLL)]
        public static extern short GT_GearStart(Card card, int mask);
        [DllImport(DLL)]
        public static extern short GT_SetGearEvent(Card card, short profile, short gearEvent, int startPara0, int startPara1);
        [DllImport(DLL)]
        public static extern short GT_GetGearEvent(Card card, short profile, out short pEvent, out int pStartPara0, out int pStartPara1);

        [DllImport(DLL)]
        public static extern short GT_PrfFollow(Card card, short profile, short dir);
        [DllImport(DLL)]
        public static extern short GT_SetFollowMaster(Card card, short profile, short masterIndex, short masterType, short masterItem);
        [DllImport(DLL)]
        public static extern short GT_GetFollowMaster(Card card, short profile, out short pMasterIndex, out short pMasterType, out short pMasterItem);
        [DllImport(DLL)]
        public static extern short GT_SetFollowLoop(Card card, short profile, int loop);
        [DllImport(DLL)]
        public static extern short GT_GetFollowLoop(Card card, short profile, out int pLoop);
        [DllImport(DLL)]
        public static extern short GT_SetFollowEvent(Card card, short profile, short followEvent, short masterDir, int pos);
        [DllImport(DLL)]
        public static extern short GT_GetFollowEvent(Card card, short profile, out short pFollowEvent, out short pMasterDir, out int pPos);
        [DllImport(DLL)]
        public static extern short GT_FollowSpace(Card card, short profile, out short pSpace, short fifo);
        [DllImport(DLL)]
        public static extern short GT_FollowData(Card card, short profile, int masterSegment, double slaveSegment, short type, short fifo);
        [DllImport(DLL)]
        public static extern short GT_FollowClear(Card card, short profile, short fifo);
        [DllImport(DLL)]
        public static extern short GT_FollowStart(Card card, int mask, int option);
        [DllImport(DLL)]
        public static extern short GT_FollowSwitch(Card card, int mask);
        [DllImport(DLL)]
        public static extern short GT_SetFollowMemory(Card card, short profile, short memory);
        [DllImport(DLL)]
        public static extern short GT_GetFollowMemory(Card card, short profile, out short memory);
        [DllImport(DLL)]
        public static extern short GT_GetFollowStatus(Card card, short profile, out short pFifoNum, out short pSwitchStatus);
        [DllImport(DLL)]
        public static extern short GT_SetFollowPhasing(Card card, short profile, short profilePhasing);
        [DllImport(DLL)]
        public static extern short GT_GetFollowPhasing(Card card, short profile, out short pProfilePhasing);

        [DllImport(DLL)]
        public static extern short GT_Compile(Card card, string pFileName, out TCompileInfo pWrongInfo);
        [DllImport(DLL)]
        public static extern short GT_Download(Card card, string pFileName);

        [DllImport(DLL)]
        public static extern short GT_GetFunId(Card card, string pFunName, out short pFunId);
        [DllImport(DLL)]
        public static extern short GT_Bind(Card card, short thread, short funId, short page);

        [DllImport(DLL)]
        public static extern short GT_RunThread(Card card, short thread);
        [DllImport(DLL)]
        public static extern short GT_StopThread(Card card, short thread);
        [DllImport(DLL)]
        public static extern short GT_PauseThread(Card card, short thread);

        [DllImport(DLL)]
        public static extern short GT_GetThreadSts(Card card, short thread, out TThreadSts pThreadSts);

        [DllImport(DLL)]
        public static extern short GT_GetVarId(Card card, string pFunName, string pVarName, out TVarInfo pVarInfo);
        [DllImport(DLL)]
        public static extern short GT_SetVarValue(Card card, short page, ref TVarInfo pVarInfo, ref double pValue, short count);
        [DllImport(DLL)]
        public static extern short GT_GetVarValue(Card card, short page, ref TVarInfo pVarInfo, out double pValue, short count);

        [DllImport(DLL)]
        public static extern short GT_SetCrdPrm(Card card, short crd, ref TCrdPrm pCrdPrm);
        [DllImport(DLL)]
        public static extern short GT_GetCrdPrm(Card card, short crd, out TCrdPrm pCrdPrm);
        [DllImport(DLL)]
        public static extern short GT_CrdSpace(Card card, short crd, out int pSpace, short fifo);
        [DllImport(DLL)]
        public static extern short GT_CrdData(Card card, short crd, ref TCrdData pCrdData, short fifo);
        [DllImport(DLL)]
        public static extern short GT_CrdDataCircle(Card card, short crd, ref TCrdData pCrdData, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXY(Card card, short crd, int x, int y, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZ(Card card, short crd, int x, int y, int z, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZA(Card card, short crd, int x, int y, int z, int a, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYG0(Card card, short crd, int x, int y, double synVel, double synAcc, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZG0(Card card, short crd, int x, int y, int z, double synVel, double synAcc, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZAG0(Card card, short crd, int x, int y, int z, int a, double synVel, double synAcc, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYR(Card card, short crd, int x, int y, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYC(Card card, short crd, int x, int y, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcYZR(Card card, short crd, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcYZC(Card card, short crd, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcZXR(Card card, short crd, int z, int x, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcZXC(Card card, short crd, int z, int x, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYZ(Card card, short crd, int x, int y, int z, double interX, double interY, double interZ, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYOverride2(Card card, short crd, int x, int y, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZOverride2(Card card, short crd, int x, int y, int z, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZAOverride2(Card card, short crd, int x, int y, int z, int a, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYG0Override2(Card card, short crd, int x, int y, double synVel, double synAcc, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZG0Override2(Card card, short crd, int x, int y, int z, double synVel, double synAcc, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZAG0Override2(Card card, short crd, int x, int y, int z, int a, double synVel, double synAcc, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYROverride2(Card card, short crd, int x, int y, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYCOverride2(Card card, short crd, int x, int y, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcYZROverride2(Card card, short crd, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcYZCOverride2(Card card, short crd, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcZXROverride2(Card card, short crd, int z, int x, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcZXCOverride2(Card card, short crd, int z, int x, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixXYRZ(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixXYCZ(Card card, short crd, int x, int y, int z, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixYZRX(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixYZCX(Card card, short crd, int x, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixZXRY(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixZXCY(Card card, short crd, int x, int y, int z, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixXYRZOverride2(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixXYCZOverride2(Card card, short crd, int x, int y, int z, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixYZRXOverride2(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixYZCXOverride2(Card card, short crd, int x, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixZXRYOverride2(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixZXCYOverride2(Card card, short crd, int x, int y, int z, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYWN(Card card, short crd, int x, int y, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZWN(Card card, short crd, int x, int y, int z, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZAWN(Card card, short crd, int x, int y, int z, int a, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYG0WN(Card card, short crd, int x, int y, double synVel, double synAcc, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZG0WN(Card card, short crd, int x, int y, int z, double synVel, double synAcc, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZAG0WN(Card card, short crd, int x, int y, int z, int a, double synVel, double synAcc, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYRWN(Card card, short crd, int x, int y, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYCWN(Card card, short crd, int x, int y, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcYZRWN(Card card, short crd, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcYZCWN(Card card, short crd, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcZXRWN(Card card, short crd, int z, int x, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcZXCWN(Card card, short crd, int z, int x, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYZWN(Card card, short crd, int x, int y, int z, double interX, double interY, double interZ, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYOverride2WN(Card card, short crd, int x, int y, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZOverride2WN(Card card, short crd, int x, int y, int z, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZAOverride2WN(Card card, short crd, int x, int y, int z, int a, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYG0Override2WN(Card card, short crd, int x, int y, double synVel, double synAcc, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZG0Override2WN(Card card, short crd, int x, int y, int z, double synVel, double synAcc, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_LnXYZAG0Override2WN(Card card, short crd, int x, int y, int z, int a, double synVel, double synAcc, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYROverride2WN(Card card, short crd, int x, int y, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcXYCOverride2WN(Card card, short crd, int x, int y, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcYZROverride2WN(Card card, short crd, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcYZCOverride2WN(Card card, short crd, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcZXROverride2WN(Card card, short crd, int z, int x, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_ArcZXCOverride2WN(Card card, short crd, int z, int x, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixXYRZWN(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixXYCZWN(Card card, short crd, int x, int y, int z, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixYZRXWN(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixYZCXWN(Card card, short crd, int x, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixZXRYWN(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixZXCYWN(Card card, short crd, int x, int y, int z, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixXYRZOverride2WN(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixXYCZOverride2WN(Card card, short crd, int x, int y, int z, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixYZRXOverride2WN(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixYZCXOverride2WN(Card card, short crd, int x, int y, int z, double yCenter, double zCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixZXRYOverride2WN(Card card, short crd, int x, int y, int z, double radius, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_HelixZXCYOverride2WN(Card card, short crd, int x, int y, int z, double zCenter, double xCenter, short circleDir, double synVel, double synAcc, double velEnd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufIO(Card card, short crd, ushort doType, ushort doMask, ushort doValue, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufEnableDoBitPulse(Card card, short crd, short doType, short doIndex, ushort highLevelTime, ushort lowLevelTime, int pulseNum, short firstLevel, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufDisableDoBitPulse(Card card, short crd, short doType, short doIndex, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufDelay(Card card, short crd, ushort delayTime, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufComparePulse(Card card, short crd, short level, short outputType, short time, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufDA(Card card, short crd, short chn, short daValue, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufLmtsOn(Card card, short crd, Axis axis, short limitType, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufLmtsOff(Card card, short crd, Axis axis, short limitType, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufSetStopIo(Card card, short crd, Axis axis, short stopType, short inputType, short inputIndex, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufMove(Card card, short crd, short moveAxis, int pos, double vel, double acc, short modal, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufGear(Card card, short crd, short gearAxis, int pos, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufGearPercent(Card card, short crd, short gearAxis, int pos, short accPercent, short decPercent, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufStopMotion(Card card, short crd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufSetVarValue(Card card, short crd, short pageId, out TVarInfo pVarInfo, double value, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufJumpNextSeg(Card card, short crd, Axis axis, short limitType, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufSynchPrfPos(Card card, short crd, short encoder, short profile, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufVirtualToActual(Card card, short crd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufSetLongVar(Card card, short crd, short index, int value, short fifo);
        [DllImport(DLL)]
        public static extern short GT_BufSetDoubleVar(Card card, short crd, short index, double value, short fifo);
        [DllImport(DLL)]
        public static extern short GT_CrdStart(Card card, short mask, short option);
        [DllImport(DLL)]
        public static extern short GT_CrdStartStep(Card card, short mask, short option);
        [DllImport(DLL)]
        public static extern short GT_CrdStepMode(Card card, short mask, short option);
        [DllImport(DLL)]
        public static extern short GT_SetOverride(Card card, short crd, double synVelRatio);
        [DllImport(DLL)]
        public static extern short GT_SetOverride2(Card card, short crd, double synVelRatio);
        [DllImport(DLL)]
        public static extern short GT_InitLookAhead(Card card, short crd, short fifo, double T, double accMax, short n, ref TCrdData pLookAheadBuf);
        [DllImport(DLL)]
        public static extern short GT_GetLookAheadSpace(Card card, short crd, out int pSpace, short fifo);
        [DllImport(DLL)]
        public static extern short GT_GetLookAheadSegCount(Card card, short crd, out int pSegCount, short fifo);
        [DllImport(DLL)]
        public static extern short GT_CrdClear(Card card, short crd, short fifo);
        [DllImport(DLL)]
        public static extern short GT_CrdStatus(Card card, short crd, out short pRun, out int pSegment, short fifo);
        [DllImport(DLL)]
        public static extern short GT_SetUserSegNum(Card card, short crd, int segNum, short fifo);
        [DllImport(DLL)]
        public static extern short GT_GetUserSegNum(Card card, short crd, out int pSegment, short fifo);
        [DllImport(DLL)]
        public static extern short GT_GetUserSegNumWN(Card card, short crd, out int pSegment, short fifo);
        [DllImport(DLL)]
        public static extern short GT_GetRemainderSegNum(Card card, short crd, out int pSegment, short fifo);
        [DllImport(DLL)]
        public static extern short GT_SetCrdStopDec(Card card, short crd, double decSmoothStop, double decAbruptStop);
        [DllImport(DLL)]
        public static extern short GT_GetCrdStopDec(Card card, short crd, out double pDecSmoothStop, out double pDecAbruptStop);
        [DllImport(DLL)]
        public static extern short GT_SetCrdLmtStopMode(Card card, short crd, short lmtStopMode);
        [DllImport(DLL)]
        public static extern short GT_GetCrdLmtStopMode(Card card, short crd, out short pLmtStopMode);
        [DllImport(DLL)]
        public static extern short GT_GetUserTargetVel(Card card, short crd, out double pTargetVel);
        [DllImport(DLL)]
        public static extern short GT_GetSegTargetPos(Card card, short crd, out int pTargetPos);
        [DllImport(DLL)]
        public static extern short GT_GetCrdPos(Card card, short crd, out double pPos);
        [DllImport(DLL)]
        public static extern short GT_GetCrdVel(Card card, short crd, out double pSynVel);
        [DllImport(DLL)]
        public static extern short GT_SetCrdSingleMaxVel(Card card, short crd, ref double pMaxVel);
        [DllImport(DLL)]
        public static extern short GT_GetCrdSingleMaxVel(Card card, short crd, out double pMaxVel);
        [DllImport(DLL)]
        public static extern short GT_BufLaserOn(Card card, short crd, short fifo, short channel);
        [DllImport(DLL)]
        public static extern short GT_BufLaserOff(Card card, short crd, short fifo, short channel);
        [DllImport(DLL)]
        public static extern short GT_BufLaserPrfCmd(Card card, short crd, double laserPower, short fifo, short channel);
        [DllImport(DLL)]
        public static extern short GT_BufLaserFollowRatio(Card card, short crd, double ratio, double minPower, double maxPower, short fifo, short channel);
        [DllImport(DLL)]
        public static extern short GT_BufLaserFollowMode(Card card, short crd, short source, short fifo, short channel, double startPower);

        [DllImport(DLL)]
        public static extern short GT_PrfPvt(Card card, short profile);
        [DllImport(DLL)]
        public static extern short GT_SetPvtLoop(Card card, short profile, int loop);
        [DllImport(DLL)]
        public static extern short GT_GetPvtLoop(Card card, short profile, out int pLoopCount, out int pLoop);
        [DllImport(DLL)]
        public static extern short GT_PvtStatus(Card card, short profile, out short pTableId, out double pTime, short count);
        [DllImport(DLL)]
        public static extern short GT_PvtStart(Card card, int mask);
        [DllImport(DLL)]
        public static extern short GT_PvtTableSelect(Card card, short profile, short tableId);

        [DllImport(DLL)]
        public static extern short GT_PvtTable(Card card, short tableId, int count, ref double pTime, ref double pPos, ref double pVel);
        [DllImport(DLL)]
        public static extern short GT_PvtTableEx(Card card, short tableId, int count, ref double pTime, ref double pPos, ref double pVelBegin, ref double pVelEnd);
        [DllImport(DLL)]
        public static extern short GT_PvtTableComplete(Card card, short tableId, int count, ref double pTime, ref double pPos, ref double pA, ref double pB, ref double pC, double velBegin, double velEnd);
        [DllImport(DLL)]
        public static extern short GT_PvtTablePercent(Card card, short tableId, int count, ref double pTime, ref double pPos, ref double pPercent, double velBegin);
        [DllImport(DLL)]
        public static extern short GT_PvtPercentCalculate(Card card, int n, ref double pTime, ref double pPos, ref double pPercent, double velBegin, ref double pVel);
        [DllImport(DLL)]
        public static extern short GT_PvtTableContinuous(Card card, short tableId, int count, ref double pPos, ref double pVel, ref double pPercent, ref double pVelMax, ref double pAcc, ref double pDec, double timeBegin);
        [DllImport(DLL)]
        public static extern short GT_PvtContinuousCalculate(Card card, int n, ref double pPos, ref double pVel, ref double pPercent, ref double pVelMax, ref double pAcc, ref double pDec, ref double pTime);

        [DllImport(DLL)]
        public static extern short GT_HomeInit(Card card);
        [DllImport(DLL)]
        public static extern short GT_Home(Card card, Axis axis, int pos, double vel, double acc, int offset);
        [DllImport(DLL)]
        public static extern short GT_Index(Card card, Axis axis, int pos, int offset);
        [DllImport(DLL)]
        public static extern short GT_HomeStop(Card card, Axis axis, int pos, double vel, double acc);
        [DllImport(DLL)]
        public static extern short GT_HomeSts(Card card, Axis axis, out ushort pStatus);

        [DllImport(DLL)]
        public static extern short GT_HandwheelInit(Card card);
        [DllImport(DLL)]
        public static extern short GT_SetHandwheelStopDec(Card card, short slave, double decSmoothStop, double decAbruptStop);
        [DllImport(DLL)]
        public static extern short GT_StartHandwheel(Card card, short slave, short master, short masterEven, short slaveEven, short intervalTime, double acc, double dec, double vel, short stopWaitTime);
        [DllImport(DLL)]
        public static extern short GT_EndHandwheel(Card card, short slave);

        [DllImport(DLL)]
        public static extern short GT_SetTrigger(Card card, short i, ref TTrigger pTrigger);
        [DllImport(DLL)]
        public static extern short GT_GetTrigger(Card card, short i, out TTrigger pTrigger);
        [DllImport(DLL)]
        public static extern short GT_GetTriggerStatus(Card card, short i, out TTriggerStatus pTriggerStatus, short count);
        [DllImport(DLL)]
        public static extern short GT_ClearTriggerStatus(Card card, short i);

        [DllImport(DLL)]
        public static extern short GT_SetComparePort(Card card, short channel, short hsio0, short hsio1);

        [DllImport(DLL)]
        public static extern short GT_ComparePulse(Card card, short level, short outputType, short time);
        [DllImport(DLL)]
        public static extern short GT_CompareStop(Card card);
        [DllImport(DLL)]
        public static extern short GT_CompareStatus(Card card, out short pStatus, out int pCount);
        [DllImport(DLL)]
        public static extern short GT_CompareData(Card card, short encoder, short source, short pulseType, short startLevel, short time, ref int pBuf1, short count1, ref int pBuf2, short count2);
        [DllImport(DLL)]
        public static extern short GT_CompareLinear(Card card, short encoder, short channel, int startPos, int repeatTimes, int interval, short time, short source);

        [DllImport(DLL)]
        public static extern short GT_SetEncResponseCheck(Card card, short control, short dacThreshold, double minEncVel, int time);
        [DllImport(DLL)]
        public static extern short GT_GetEncResponseCheck(Card card, short control, out short pDacThreshold, out double pMinEncVel, out int pTime);
        [DllImport(DLL)]
        public static extern short GT_EnableEncResponseCheck(Card card, short control);
        [DllImport(DLL)]
        public static extern short GT_DisableEncResponseCheck(Card card, short control);

        [DllImport(DLL)]
        public static extern short GT_2DCompareMode(Card card, short chn, short mode);
        [DllImport(DLL)]
        public static extern short GT_2DComparePulse(Card card, short chn, short level, short outputType, short time);
        [DllImport(DLL)]
        public static extern short GT_2DCompareStop(Card card, short chn);
        [DllImport(DLL)]
        public static extern short GT_2DCompareClear(Card card, short chn);
        [DllImport(DLL)]
        public static extern short GT_2DCompareStatus(Card card, short chn, out short pStatus, out int pCount, out short pFifo, out short pFifoCount, out short pBufCount);
        [DllImport(DLL)]
        public static extern short GT_2DCompareSetPrm(Card card, short chn, ref T2DComparePrm pPrm);
        [DllImport(DLL)]
        public static extern short GT_2DCompareData(Card card, short chn, short count, ref T2DCompareData pBuf, short fifo);
        [DllImport(DLL)]
        public static extern short GT_2DCompareStart(Card card, short chn);

        [DllImport(DLL)]
        public static extern short GT_SetAxisMode(Card card, Axis axis, short mode);
        [DllImport(DLL)]
        public static extern short GT_GetAxisMode(Card card, Axis axis, out short pMode);
        [DllImport(DLL)]
        public static extern short GT_SetHSIOOpt(Card card, ushort value, short channel);
        [DllImport(DLL)]
        public static extern short GT_GetHSIOOpt(Card card, out ushort pValue, short channel);
        [DllImport(DLL)]
        public static extern short GT_LaserPowerMode(Card card, short laserPowerMode, double maxValue, double minValue, short channel, short delaymode);
        [DllImport(DLL)]
        public static extern short GT_LaserPrfCmd(Card card, double outputCmd, short channel);
        [DllImport(DLL)]
        public static extern short GT_LaserOutFrq(Card card, double outFrq, short channel);
        [DllImport(DLL)]
        public static extern short GT_SetPulseWidth(Card card, uint width, short channel);
        [DllImport(DLL)]
        public static extern short GT_SetWaitPulse(Card card, ushort mode, double waitPulseFrq, double waitPulseDuty, short channel);
        [DllImport(DLL)]
        public static extern short GT_SetPreVltg(Card card, ushort mode, double voltageValue, short channel);
        [DllImport(DLL)]
        public static extern short GT_SetLevelDelay(Card card, ushort offDelay, ushort onDelay, short channel);
        [DllImport(DLL)]
        public static extern short GT_EnaFPK(Card card, ushort time1, ushort time2, ushort laserOffDelay, short channel);
        [DllImport(DLL)]
        public static extern short GT_DisFPK(Card card, short channel);
        [DllImport(DLL)]
        public static extern short GT_SetLaserDisMode(Card card, short mode, short source, ref int pPos, ref double pScale, short channel);
        [DllImport(DLL)]
        public static extern short GT_SetLaserDisRatio(Card card, ref double pRatio, double minPower, double maxPower, short channel);
        [DllImport(DLL)]
        public static extern short GT_SetWaitPulseEx(Card card, ushort mode, double waitPulseFrq, double waitPulseDuty);
        [DllImport(DLL)]
        public static extern short GT_SetLaserMode(Card card, short mode);
    }
}
