namespace Neo.AddIn.Hikvision.MVC

open System
open MvCamCtrl.NET

//type C = MvCamCtrl.NET.MyCamera
module internal Internals = 
    let verify s = 
        match s with
        | 0 -> ()
        | _ -> failwithf "MV_ERROR(%d)" s
    
    type Info = 
        { Vendor : string
          SN : string
          ModelName : string
          PixelFormat : string
          LineWidth : int
          TileHeight : int }
    
    type Result<'T> = 
        | Ok of 'T
        | Err of exn
        with 
            member inline __.Get() = 
                match __ with
                | Ok x -> x
                | Err ex -> raise ex
    
    type ReplyChannel<'T> = AsyncReplyChannel<Result<'T>>
    

    type MvCamCtrl.NET.MyCamera with
        
        member cam.GetStringValue(key) = 
            let mutable value = Unchecked.defaultof<_>
            cam.MV_CC_GetStringValue_NET(key, &value) |> verify
            value.chCurValue
        
        member cam.GetEnumValue(key) = 
            let mutable value = Unchecked.defaultof<_>
            cam.MV_CC_GetEnumValue_NET(key, &value) |> verify
            value.nCurValue
        
        member cam.GetUInt32Value(key) = 
            let mutable value = Unchecked.defaultof<_>
            cam.MV_CC_GetIntValue_NET(key, &value) |> verify
            value.nCurValue

    type QueueMSG<'T> = 
        | Dequeue of 'T AsyncReplyChannel
        | Enqueue of 'T

    type Queue<'T> () = 
        let id = Guid.NewGuid()
        let agent = MailboxProcessor.Start(fun inbox ->
            let xs = System.Collections.Generic.Queue()
            let cnt = ref 0
            let rec loop () = 
                inbox.Scan (
                    function
                    | Dequeue ch -> 
                        if xs.Count > 0 then
                            Some <| async {
                                xs.Dequeue() |> ch.Reply
                                return! loop()
                            }
                        else None
                    | Enqueue x -> 
                        Some <| async {
                            xs.Enqueue x
                            return! loop()
                        }
                )
            loop ())
        member __.Id = id
        member __.Dequeue(?timeout) = 
            agent.TryPostAndReply(Dequeue, ?timeout = timeout)
        member __.Enqueue(x) =
            agent.Post(Enqueue x)

    type MSG = 
        | WarmUp
        | CoolDown of AsyncReplyChannel<bool>
        | GetInfo of ReplyChannel<Info>
        | Acquire of int * ReplyChannel<Guid>
        | GetQueue of ReplyChannel<Queue<byte[]> option>
        //| Retrieve of Guid * int * ReplyChannel<byte[] option>

    let pixelFormatToString fmt =
        match fmt with
        | 0x01080001u -> "mono8"
        | 0x02180014u -> "rgb24"
        | 0x01080009u -> "rggb8"
        | _ -> "not supported pixel format"
open Internals

type LineScanCamera(log) = 
    inherit MarshalByRefObject()
    let cooldown = 60 * 1000
    let keepAlive = ref None

    let reply f (ch : _ AsyncReplyChannel) = 
        try 
            ch.Reply(f() |> Ok)
        with ex -> ch.Reply(Err ex)
    
    let agent = 
        MailboxProcessor.Start(fun inbox -> 
            let queue = ref None
            let proc (cam : MyCamera) msg = 
                let rec loop msg = async { 
                    
                    match !queue with
                    | Some (q, cnt) when !cnt <= 0 -> cam.MV_CC_StopGrabbing_NET() |> ignore
                    | _ -> ()

                    match msg with
                    | WarmUp -> ()
                    | CoolDown _ -> ()
                    | GetInfo ch -> 
                        ch |> reply (fun _ -> 
                                  { SN = cam.GetStringValue("DeviceSerialNumber")
                                    Vendor = cam.GetStringValue("DeviceVendorName")
                                    ModelName = cam.GetStringValue("DeviceModelName")
                                    PixelFormat = cam.GetEnumValue("PixelFormat") |> pixelFormatToString
                                    LineWidth = cam.GetUInt32Value("Width") |> int
                                    TileHeight = cam.GetUInt32Value("Height") |> int })
                    | Acquire (cnt, ch) ->
                        ch |> reply (fun _ ->
                            cam.MV_CC_StopGrabbing_NET() |> ignore
                            let q = Queue()
                            queue := Some (q, ref cnt)
                            cam.MV_CC_StartGrabbing_NET() |> ignore
                            log "The camera has started grabbing."
                            q.Id
                        )
                    | GetQueue ch ->
                        ch |> reply (fun _ -> !queue |> Option.map fst)
//                    | Retrieve (id, timeout, ch) ->
//                        ch |> reply (fun  _ ->
//                            match !queue with
//                            | Some (q, _) when id = q.Id  -> q.Dequeue(timeout)
//                            | _ -> raise <| System.InvalidOperationException()
//                        )
                    match msg with
                    | CoolDown ch -> 
                        ch.Reply false
                        return ()
                    | _ ->
                        let! msg = inbox.TryReceive(cooldown)
                        match msg with
                        | Some msg -> return! loop msg
                        | None -> return ()
                }
                loop msg
            async { 
                while true do
                    let! msg = inbox.Receive()
                    match msg with
                    | CoolDown ch -> ch.Reply true
                    | GetQueue ch -> ch.Reply(!queue |> Option.map fst |> Ok)
                    | WarmUp | Acquire _ | GetInfo _ ->
                        let cam = 
                            try 
                                let mutable devList = Unchecked.defaultof<_>
                                MyCamera.MV_CC_EnumDevices_NET
                                    (MyCamera.MV_USB_DEVICE ||| MyCamera.MV_GIGE_DEVICE |> uint32, &devList) |> verify
                                if devList.nDeviceNum = 0u then failwith "No camera found."
                                let mutable devInfo = System.Runtime.InteropServices.Marshal.PtrToStructure(devList.pDeviceInfo.[0], typeof<MyCamera.MV_CC_DEVICE_INFO>) |> unbox
                                let cam = MvCamCtrl.NET.MyCamera()
                                cam.MV_CC_CreateDevice_NET(&devInfo) |> verify
                                cam.MV_CC_OpenDevice_NET() |> verify
                                let callback = MyCamera.cbOutputdelegate(fun ptr frameInfo _ ->
                                    match !queue with
                                    | Some (q, cnt) when !cnt > 0 ->
                                        let len = frameInfo.nFrameLen |> int
                                        let data : byte[] = Array.zeroCreate len
                                        System.Runtime.InteropServices.Marshal.Copy(ptr, data, 0, len)
                                        q.Enqueue(data)
                                        decr cnt
                                        if !cnt <= 0 then 
                                            cam.MV_CC_StopGrabbing_NET() |> ignore
                                            log "The camera has stopped grabbing."
                                    | _ -> ()
                                )
                                keepAlive := Some callback
                                cam.MV_CC_RegisterImageCallBack_NET(callback, 0n) |> verify
                                Some cam
                            with ex -> 
                                match msg with
                                | WarmUp -> ()
                                | CoolDown ch -> ch.Reply false
                                | GetInfo ch -> ch.Reply(Err ex)
                                | Acquire (_, ch) -> ch.Reply(Err ex)
                                | GetQueue ch -> ch.Reply(!queue |> Option.map fst |> Ok)
                                //| Retrieve (_, _, ch) -> ch.Reply(Err ex)
                                None
                        match cam with
                        | Some cam -> 
                            log "The camera has been opened."
                            do! proc cam msg
                            cam.MV_CC_CloseDevice_NET() |> ignore
                            cam.MV_CC_DestroyDevice_NET() |> ignore
                            log "The camera has been closed."
                        | None -> ()
            })
    
    let getInfo = 
        let info = ref None
        fun () -> 
            match !info with
            | Some x -> x
            | None -> agent.PostAndReply(GetInfo).Get()
    //override __.Finalize() = agent.Post(CoolDown)
    new () = LineScanCamera(ignore)
    override __.InitializeLifetimeService() = null
    member __.Initialize() = ()
    member __.SN = getInfo().SN
    member __.Vendor = getInfo().Vendor
    member __.ModelName = getInfo().ModelName
    member __.LineWidth = getInfo().LineWidth
    member __.TileHeight = getInfo().TileHeight
    member __.PixelFormat = getInfo().PixelFormat
    member __.WarmUp() = agent.Post(WarmUp)
    member __.CoolDown() = 
        while  not <| agent.PostAndReply(CoolDown) do
            ()
    member __.Acquire(tileCount) = agent.PostAndReply(fun ch -> Acquire(tileCount, ch)).Get()
    member __.RetrieveData(acquireId, timeout) = 
        match agent.PostAndReply(GetQueue).Get() with
        | Some q when q.Id = acquireId -> 
            match q.Dequeue(timeout) with
            | Some x -> x
            | None -> raise <| System.TimeoutException()
        | _ -> raise <| System.InvalidOperationException("retrieve data failed")
    interface Neo.AddIn.Contracts.Cameras.ILineScanCamera with
        member __.SN = __.SN
        member __.Vendor = __.Vendor
        member __.ModelName = __.ModelName
        member __.LineWidth = __.LineWidth
        member __.TileHeight = __.TileHeight
        member __.PixelFormat = __.PixelFormat
        member __.WarmUp() = __.WarmUp()
        member __.Initialize() = __.Initialize()
        member __.Acquire(tileCount) = __.Acquire(tileCount)
        member __.RetrieveData(acquireId, timeout) = __.RetrieveData(acquireId, timeout)

