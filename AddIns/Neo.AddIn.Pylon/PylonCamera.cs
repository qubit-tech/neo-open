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

        //sealed class DataTransfer : Contracts.Cameras.IDataTransfer<byte[]>
        //{
        //    public PylonCamera Camera { get; private set; }
        //    public Guid Id { get; private set; }
        //    public DataTransfer(PylonCamera camera, Guid acqId)
        //    {
        //        this.Camera = camera;
        //        this.Id = acqId;
        //    }
        //    public byte[] GetData()
        //    {
        //        return this.Camera.RetrieveData(this.Id);
        //    }
        //}

        ConcurrentDictionary<Guid, Task<byte[]>> _acqusitions = new ConcurrentDictionary<Guid, Task<byte[]>>();

        Camera _camera = null;
        BlockingCollection<Action> _actionQueue;

        static BlockingCollection<Action> StartQueue()
        {
            var actions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
            new Thread(() =>
            {
                Debug.WriteLine(string.Format("SingleThreadTaskScheduler thread [{0}] started.", Thread.CurrentThread.ManagedThreadId));
                Action act;
                while (actions.TryTake(out act, -1))
                {
                    act();
                }
                Debug.WriteLine(string.Format("SingleThreadTaskScheduler thread [{0}] stopped.", Thread.CurrentThread.ManagedThreadId));
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
            this.SN = camera.CameraInfo[CameraInfoKey.SerialNumber];
            this.ModelName = camera.CameraInfo[CameraInfoKey.ModelName];
            this.Vendor = camera.CameraInfo[CameraInfoKey.VendorName];

            //camera.CameraOpened += Configuration.SoftwareTrigger;
            camera.CameraOpened += Configuration.AcquireSingleFrame;
            camera.Open();

            this.PixelWidth = (int)camera.Parameters[PLCamera.Width].GetValue();
            this.PixelHeight = (int)camera.Parameters[PLCamera.Height].GetValue();
            this.PixelFormat = camera.Parameters[PLCamera.PixelFormat].GetValue();

            if (!camera.Parameters[PLCamera.ShutterMode].IsEmpty)
            {
                var shutterMode = camera.Parameters[PLCamera.ShutterMode].GetValue();
                this.ShutterMode = (Contracts.Cameras.ShutterMode)Enum.Parse(typeof(Contracts.Cameras.ShutterMode), shutterMode);
            }
            else if (!camera.Parameters[PLCamera.GlobalResetReleaseModeEnable].IsEmpty)
            {
                this.ShutterMode = camera.Parameters[PLCamera.GlobalResetReleaseModeEnable].GetValue() ? Contracts.Cameras.ShutterMode.GlobalResetRelease : Contracts.Cameras.ShutterMode.Rolling;
            }

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

            //camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByUser);

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

        async Task<Guid> AcquireAsync()
        {
            var modelName = this.ModelName;
            var cam = this._camera;
            var grabber = cam.StreamGrabber;
            var tcs = new TaskCompletionSource<byte[]>();
            grabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByUser);
            var acqId = Guid.NewGuid();
            //if (!cam.WaitForFrameTriggerReady(5000, TimeoutHandling.Return))
            //{
            //    tcs.SetException(new Exception(string.Format("Camera[{0}] execute software trigger failed.", modelName)));
            //}
            //else
            {
                //cam.ExecuteSoftwareTrigger();
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
                //cam.WaitForFrameTriggerReady(-1, TimeoutHandling.Return);
                //return new DataTransfer(tcs.Task);
                switch (this.ShutterMode)
                {
                    case Contracts.Cameras.ShutterMode.Global:
                    case Contracts.Cameras.ShutterMode.GlobalResetRelease:
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(this.ExposureMS));
                            //return new DataTransfer(tcs.Task);
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
            }
            AddAcqusition(acqId, tcs.Task);
            return acqId;
        }

        public Guid Acquire()
        {
            //var acqId = AcquireAsync().Result;
            //return new DataTransfer(this, acqId);
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
        }
    }
}
