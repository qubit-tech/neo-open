using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.AddIn;
using Basler.Pylon;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;

namespace Neo.AddIn.Pylon
{
    public sealed class PylonCamera : Contracts.Cameras.CameraBase, Contracts.Cameras.ICamera, IDisposable
    {
        const int MAX_BUFFER_COUNT = 5;

        ConcurrentDictionary<Guid, Task<byte[]>> _acqusitions = new ConcurrentDictionary<Guid, Task<byte[]>>();

        Camera _camera = null;
        BlockingCollection<Action> _actionQueue;

        // 启动后台处理任务。
        static BlockingCollection<Action> StartQueue()
        {
            var actions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
            new Thread(() =>
            {
                Action act;
                while (actions.TryTake(out act, -1))
                {
                    act();
                }
            })
            { IsBackground = true }.Start();
            return actions;
        }

        public PylonCamera()
        {
        }

        ~PylonCamera()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            if (_actionQueue != null)
            {
                _actionQueue.CompleteAdding();
                _actionQueue = null;
            }

            if (_camera != null)
            {
                _camera.Close();
                _camera.Dispose();
                _camera = null;
            }

            GC.SuppressFinalize(this);
        }

        public void Initialize()
        {
            var camera = new Camera();

            // 获取相机基本信息。
            this.SN = camera.CameraInfo[CameraInfoKey.SerialNumber];
            this.ModelName = camera.CameraInfo[CameraInfoKey.ModelName];
            this.Vendor = camera.CameraInfo[CameraInfoKey.VendorName];

            // 打开相机获取详细信息。
            camera.CameraOpened += Configuration.AcquireSingleFrame;
            camera.Open();

            this.PixelWidth = (int)camera.Parameters[PLCamera.Width].GetValue();
            this.PixelHeight = (int)camera.Parameters[PLCamera.Height].GetValue();
            this.PixelFormat = camera.Parameters[PLCamera.PixelFormat].GetValue();

            // 获取曝光模式，GlobalResetReleaseModeEnable配合硬件可以模拟全局曝光。
            if (!camera.Parameters[PLCamera.ShutterMode].IsEmpty)
            {
                var shutterMode = camera.Parameters[PLCamera.ShutterMode].GetValue();
                this.ShutterMode = (Contracts.Cameras.ShutterMode)Enum.Parse(typeof(Contracts.Cameras.ShutterMode), shutterMode);
            }
            else if (!camera.Parameters[PLCamera.GlobalResetReleaseModeEnable].IsEmpty)
            {
                this.ShutterMode = camera.Parameters[PLCamera.GlobalResetReleaseModeEnable].GetValue() ? Contracts.Cameras.ShutterMode.GlobalResetRelease : Contracts.Cameras.ShutterMode.Rolling;
            }

            // 获取曝光时间，单位：毫秒
            switch (camera.CameraInfo[CameraInfoKey.DeviceType])
            {
                case "BaslerUsb":
                    this.ExposureMS = camera.Parameters[PLUsbCamera.ExposureTime].GetValue() / 1000.0;
                    break;
                case "BaslerGigE":
                    this.ExposureMS = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue() / 1000.0;
                    break;
                case "BaslerCameraLink":
                    this.ExposureMS = camera.Parameters[PLCameraLinkCamera.ExposureTimeAbs].GetValue() / 1000.0;
                    break;
                case "Basler1394":
                    this.ExposureMS = camera.Parameters[PL1394Camera.ExposureTimeAbs].GetValue() / 1000.0;
                    break;
                default:
                    throw new Exception(string.Format("Unsupported device type: {0}", camera.CameraInfo[CameraInfoKey.DeviceType]));
            }

            this._camera = camera;
            _actionQueue = StartQueue();
        }

        public string SN { get; private set; }
        public string Vendor { get; private set; }
        public string ModelName { get; private set; }
        public string PixelFormat { get; private set; }
        public int PixelHeight { get; private set; }
        public int PixelWidth { get; private set; }
        public double ExposureMS { get; private set; }

        public Contracts.Cameras.ShutterMode ShutterMode { get; private set; }

        void AddAcqusition(Guid acqId, Task<byte[]> task)
        {
            if (_acqusitions.Count >= MAX_BUFFER_COUNT)
            {
                var removeId = _acqusitions.Keys.OrderBy(id => id).First();
                Task<byte[]> removeTask;
                _acqusitions.TryRemove(removeId, out removeTask);
            }
            _acqusitions[acqId] = task;
        }

        // 启动异步采集，返回任务Id。
        async Task<Guid> AcquireAsync()
        {
            var modelName = this.ModelName;
            var cam = this._camera;
            var grabber = cam.StreamGrabber;
            var tcs = new TaskCompletionSource<byte[]>();

            // 启动采集。
            grabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByUser);

            // 添加后台获取数据。
            var acqId = Guid.NewGuid();
            _actionQueue.Add(() =>
            {
                using (var result = grabber.RetrieveResult(5000, TimeoutHandling.Return))
                {
                    if (result.GrabSucceeded)
                    {
                        var data = (byte[])result.PixelData;
                        tcs.SetResult((byte[])data.Clone());
                    }
                    else
                    {
                        tcs.SetException(new Exception(string.Format("Camera[{0}] grab frame failed.", modelName)));
                    }
                }
            });

            // 如果是整帧曝光，仅需等待曝光时间结束；如果是逐行曝光，需等待采集结束。
            switch (this.ShutterMode)
            {
                case Contracts.Cameras.ShutterMode.Global:
                case Contracts.Cameras.ShutterMode.GlobalResetRelease:
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(this.ExposureMS));
                    }
                    break;
                default:
                    {
                        try
                        {
                            await tcs.Task;
                        }
                        catch
                        { }
                    }
                    break;
            }

            AddAcqusition(acqId, tcs.Task);
            return acqId;
        }

        public Guid Acquire()
        {
            return AcquireAsync().Result;
        }

        public byte[] RetrieveData(Guid acqId)
        {
            Task<byte[]> task;
            if (!_acqusitions.TryRemove(acqId, out task))
            {
                throw new Exception("Acqusition does not exists.");
            }
            return task.Result;
        }

        public void WarmUp()
        {
            // 简单插件，不做资源优化。
        }
    }
}
