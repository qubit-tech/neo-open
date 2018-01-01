using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Neo.AddIn.Solidtech
{
    public struct Axis
    {
        public int Id { get; private set; }
        public static Axis Create(int cardId, int axisId)
        {
            if (axisId >= 4 || axisId < 0)
                throw new ArgumentOutOfRangeException("axisId");
            return new Axis { Id = cardId * 4 + axisId };
        }
    }

    static class Api
    {
        public const string DLL = "pci9014.dll";

        //open/close
        [DllImport(DLL)]
        public static extern int p9014_initial(ref int pCardCount, int[] pBoardId);
        [DllImport(DLL)]
        public static extern int p9014_close();

        //pulse input/output configuration
        [DllImport(DLL)]
        public static extern int p9014_set_pls_outmode(Axis axis, int pls_outmode);
        [DllImport(DLL)]
        public static extern int p9014_set_pls_iptmode(Axis axis, int pls_iptmode);

        [DllImport(DLL)]
        public static extern int p9014_set_t_profile(Axis axis, double start_vel, double max_vel, double acc, double dec);
        [DllImport(DLL)]
        public static extern int p9014_set_s_profile(Axis axis, double start_vel, double max_vel, double acc, double dec, double jerk_percent);

        //single axis driving functions
        //dist_mode:  0  incremental coordinate; 1 - absolute coordinate
        //vel_mode:   0  low speed mode without acc/dec;
        //			  1 high speed mode without acc/dec;
        //			  2 high speed mode with acc/dec
        [DllImport(DLL)]
        public static extern int p9014_pmove(Axis axis, int dist, int dist_mode, int vel_mode);
        [DllImport(DLL)]
        public static extern int p9014_vmove(Axis axis, int plus_dir, int vel_mode);
        [DllImport(DLL)]
        public static extern int p9014_stop(Axis axis, int emg_stop);

        //home return
        //mode:		0	- low speed, ORG only;
        //			2   - low speed,  ORG + EZ;
        [DllImport(DLL)]
        public static extern int p9014_home_config(Axis axis, int mode, int org_level, int ez_level);
        [DllImport(DLL)]
        public static extern int p9014_home_move(Axis axis, int plus_dir);

        //position counter control
        [DllImport(DLL)]
        public static extern int p9014_set_pos(Axis axis, int cntr_no, int pos);
        [DllImport(DLL)]
        public static extern int p9014_get_pos(Axis axis, int cntr_no, ref int pPos);

        //I/O control
        [DllImport(DLL)]
        public static extern int p9014_set_do(int card_no, uint data);
        [DllImport(DLL)]
        public static extern int p9014_set_do_bit(int card_no, uint bit_no, uint data);
        [DllImport(DLL)]
        public static extern int p9014_get_do(int card_no, ref uint pData);
        [DllImport(DLL)]
        public static extern int p9014_get_di(int card_no, ref uint pData);
        [DllImport(DLL)]
        public static extern int p9014_get_di_bit(int card_no, uint bit_no, ref uint pData);

        //axis switch input
        [DllImport(DLL)]
        public static extern int p9014_get_io_status(Axis axis, ref uint pStatus);

        //axis's motion status
        [DllImport(DLL)]
        public static extern int p9014_get_motion_status(Axis axis, ref uint pStatus);
        [DllImport(DLL)]
        public static extern int p9014_get_current_speed(Axis axis, ref double pSpeed);

        //version information
        [DllImport(DLL)]
        public static extern int p9014_get_version(Axis axis, ref uint pApi_ver, ref uint pDriver_ver, ref uint pLogic_ver);
        [DllImport(DLL)]
        public static extern int p9014_get_revision(int card_no, ref int pLogic_revision);

        //set End Limit Input active level
        [DllImport(DLL)]
        public static extern int p9014_set_el_level(Axis axis, int active_level);

        //set stop mode(decelerate to stop, suddenly stop) on error
        [DllImport(DLL)]
        public static extern int p9014_set_error_stop_mode(Axis axis, int stop_mode);

        //config alarm input
        [DllImport(DLL)]
        public static extern int p9014_set_alarm(Axis axis, int enable, int active_level);

        //2015-2-10
        [DllImport(DLL)]
        public static extern int p9014_comp_enable(int card_no, int enable, int active_level, int ref_source, int length);
        [DllImport(DLL)]
        public static extern int p9014_comp_get_fifoStatus(int card_no, ref uint stat);
        [DllImport(DLL)]
        public static extern int p9014_comp_add_ref(int card_no, int PosRef);
        [DllImport(DLL)]
        public static extern int p9014_comp_get_curRef(int card_no, ref int PosRef);
        [DllImport(DLL)]
        public static extern int p9014_comp_clr_fifo(int card_no);
        [DllImport(DLL)]
        public static extern int p9014_comp_get_matchCount(int card_no, ref uint Count);
        [DllImport(DLL)]
        public static extern int p9014_comp_clr_matchCount(int card_no);
    }
}
