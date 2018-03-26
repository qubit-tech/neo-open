namespace Neo.AddIn

open System
open System.IO.Ports

type ScannerModelConfig = {
    ModelName : string
    BaudRate : int
    DataBits : int
    Parity : Parity
    StopBits : StopBits
    StartCommand : byte[]
    StopCommand : byte[]
    TrimStart : char[]
    TrimEnd : char[]
}

type ScannerConfig = {
    GroupId : int
    PortNames : string list
    ScannerModel : ScannerModelConfig
}

type BarcodesScannerConfig = {
    Scanners : ScannerConfig list
}

module internal Internal =
    open Neo.Configuration.Toml

    type Message = 
        | Scan of TimeSpan * AsyncReplyChannel<string>
        | Stop of AsyncReplyChannel<unit>

    let defaultModelCfg = {
        ModelName = "default"

        BaudRate = 9600
        DataBits = 8
        Parity = Parity.None
        StopBits = StopBits.One 
            
        StartCommand = [||] // [|22uy; 84uy; 13uy|]
        StopCommand = [||] // [|22uy; 85uy; 13uy|]
        TrimStart = [|' '; '\t'; '\r'; '\n'; '\000'|]
        TrimEnd = [|' '; '\t'; '\r'; '\n'; '\000'|]
    }

    let defaultScannerCfg = {
        GroupId = 1
        PortNames = []
        ScannerModel = defaultModelCfg
    }

    let inline invalidCfgValue (key : TomlKey) = 
        failwithf "Invalid value for key \"%s\"." (key.Path |> String.concat ".")

    let tryFindBytesOrString key toml f cfg =
        match Toml.tryFind key toml with
        | Some (TomlValue.String s) -> 
            let bytes = System.Text.Encoding.ASCII.GetBytes s
            f key bytes cfg
        | Some (TomlValue.Array (TomlArray.Integers xs)) -> 
            if xs |> List.exists (fun x -> x < 0L || x > 255L) then invalidCfgValue key
            let bytes = xs |> List.map byte |> List.toArray
            f key bytes cfg
        | None -> cfg
        | _ -> invalidCfgValue key

    let tryFindString key toml f cfg =
        match Toml.tryFind key toml with
        | Some (TomlValue.String s) -> f key s cfg
        | None -> cfg
        | _ -> invalidCfgValue key

    let tryFindStrings key toml f cfg =
        match Toml.tryFind key toml with
        | Some (TomlValue.Array (TomlArray.Strings xs)) -> f key xs cfg
        | None -> cfg
        | _ -> invalidCfgValue key

    let tryFindInteger key toml f cfg =
        match Toml.tryFind key toml with
        | Some (TomlValue.Integer s) -> f key s cfg
        | None -> cfg
        | _ -> invalidCfgValue key

    let tryFindTables key toml f cfg =
        match Toml.tryFind key toml with
        | Some (TomlValue.Array (TomlArray.Tables xs)) -> f key xs cfg
        | None -> cfg
        | _ -> invalidCfgValue key

    let loadModel tomlModel =
        defaultModelCfg
        |> tryFindBytesOrString (TomlKey ["start-command"]) tomlModel (fun _ x cfg -> { cfg with StartCommand = x })
        |> tryFindBytesOrString (TomlKey ["stop-command"]) tomlModel (fun _ x cfg -> { cfg with StopCommand = x })
        |> tryFindString (TomlKey ["name"]) tomlModel (fun _ x cfg -> { cfg with ModelName = x })
        |> tryFindString (TomlKey ["trim-start"]) tomlModel (fun _ x cfg -> { cfg with TrimStart = x.ToCharArray() })
        |> tryFindString (TomlKey ["trim-end"]) tomlModel (fun _ x cfg -> { cfg with TrimEnd = x.ToCharArray() })
        |> tryFindInteger (TomlKey ["baud-rate"]) tomlModel (fun _ x cfg -> { cfg with BaudRate = int x })
        |> tryFindInteger (TomlKey ["stop-bits"]) tomlModel (fun key x cfg ->
            match x with
            | 0L -> StopBits.None 
            | 1L -> StopBits.One  
            | 2L -> StopBits.Two  
            | _ -> invalidCfgValue key
            |> fun x -> { cfg with StopBits = x }
           )
        |> tryFindString (TomlKey ["parity"]) tomlModel (fun key x cfg ->
            match x with
            | "n" | "N" -> Parity.None
            | "o" | "O" -> Parity.Odd
            | "e" | "E" -> Parity.Even
            | _ -> invalidCfgValue key
            |> fun x -> { cfg with Parity = x }
           )

    let loadScanner models defaultScannerCfg tomlScanner =
        defaultScannerCfg
        |> tryFindString (TomlKey ["model"]) tomlScanner (fun _ x cfg ->
            match Map.tryFind x models with
            | Some m -> { cfg with ScannerModel = m }
            | None -> failwithf "Can not find the model [%s]" x)
        |> tryFindStrings (TomlKey ["serial-ports"]) tomlScanner (fun _ xs cfg -> { cfg with PortNames = xs })
        |> tryFindInteger (TomlKey ["group-id"]) tomlScanner (fun _ x cfg -> { cfg with GroupId = int x})
        
    let loadCfg file =
        let toml = 
            match Toml.ofFile file with
            | Ok x -> x
            | Error e -> failwithf "Invalid toml file, error: %A." e

        let models = 
            let xs = tryFindTables (TomlKey ["model"]) toml (fun _ tabs _ -> List.map loadModel tabs) []
            xs
            |> List.map (fun x -> x.ModelName, x)
            |> Map.ofList
            |> fun models -> 
                if models.Count <> xs.Length then failwith "Duplicated model names."
                models
        let defaultScannerCfg = 
            Map.tryFind "default" models 
            |> Option.map (fun m -> { defaultScannerCfg with ScannerModel = m})
            |> Option.defaultValue defaultScannerCfg

        let scanners = tryFindTables (TomlKey ["scanner"]) toml (fun _ xs _ -> xs |> List.map (loadScanner models defaultScannerCfg)) []
        do
            let portss = scanners |> List.map (fun s -> s.PortNames |> Set.ofSeq)
            let cnt = portss |> Seq.sumBy (fun xs -> xs.Count)
            let union = portss |> Set.unionMany
            if cnt <> union.Count then failwith "Duplicated serial ports."
        { Scanners = scanners }

open Internal

type BarcodeScanner(port, cfg : ScannerConfig) =
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec idle () = 
            async {
                let! msg = inbox.Receive()
                match msg with
                | Scan (t, ch) -> 
                    return! scan t ch
                | Stop ch -> 
                    ch.Reply ()
                    return! idle ()
            }
        and scan (timeout : TimeSpan) (ch : _ AsyncReplyChannel) = async {
            try
                use sp = new SerialPort(port, cfg.ScannerModel.BaudRate, cfg.ScannerModel.Parity, cfg.ScannerModel.DataBits, cfg.ScannerModel.StopBits)
                sp.Open()
                let sendCmd (cmd : byte[]) = if cmd.Length > 0 then sp.Write(cmd, 0, cmd.Length)
                sendCmd cfg.ScannerModel.StartCommand
                let sw = System.Diagnostics.Stopwatch.StartNew()
                let buf : byte[] = Array.zeroCreate 1024
                let rec wait () = async {
                    if sp.BytesToRead > 0 then return true
                    elif (timeout.TotalMilliseconds >= 0. && sw.Elapsed > timeout) || inbox.CurrentQueueLength > 0 then return false
                    else
                        do! Async.Sleep 20
                        return! wait ()
                }
                let read () = async { 
                    let! ok = wait ()
                    if ok then
                        let rec loop s = async {
                            if s >= buf.Length then return s
                            else
                                let bytesToRead = sp.BytesToRead |> min (buf.Length - s)
                                if bytesToRead > 0 then
                                    let r = sp.Read(buf, s, bytesToRead)
                                    return! loop (s + r)
                                else
                                    do! Async.Sleep(100)
                                    if sp.BytesToRead = 0 then return s
                                    else return! loop s
                        }
                        let! r = loop 0
                        return sp.Encoding.GetString(buf, 0, r)
                    else 
                        return ""
                }
                let! input = read ()
                sendCmd cfg.ScannerModel.StopCommand
                let code = input.TrimStart(cfg.ScannerModel.TrimStart).TrimEnd(cfg.ScannerModel.TrimEnd)
                ch.Reply code
            with
            | _ -> ch.Reply ""
            return! idle()
        }
        idle())
    member __.Id = port
    member __.AsyncScan(timeout) = agent.PostAndAsyncReply(fun ch -> Scan (timeout, ch))
    member __.AsyncStop() = agent.PostAndAsyncReply(fun ch -> Stop ch)

type SerialPortBarcodesScanner() = 
    inherit System.MarshalByRefObject()

    let cfg, err = 
        let dll = typeof<SerialPortBarcodesScanner>.Assembly.Location
        let file = System.IO.Path.ChangeExtension(dll, ".toml")
        let mutable cfg = {
            Scanners = []
        }        
        try 
            if System.IO.File.Exists(file) then cfg <- loadCfg file
            cfg, ""
        with
        | ex -> cfg, ex.Message

    let groups = 
        cfg.Scanners 
        |> Seq.groupBy (fun x -> x.GroupId) |> Seq.map (fun (k, v) -> k, Seq.toArray v) 
        |> Map.ofSeq

    let ids = 
        groups 
        |> Map.map (fun _ v -> v |> Seq.collect (fun x -> x.PortNames) |> Seq.toArray)

    let scanners = 
        cfg.Scanners  
        |> Seq.collect (fun x -> x.PortNames |> List.map (fun p -> { x with PortNames = [p] }))
        |> Seq.distinctBy (fun x -> x.PortNames.[0]) 
        |> Seq.map (fun x ->
            let port = x.PortNames.[0]
            port, BarcodeScanner(port, x))
        |> Map.ofSeq

    let init () = if err = "" then () else failwith err

    override __.InitializeLifetimeService() = null

    member __.Initialize() = init ()

    member __.GetIDs(group) = ids |> Map.tryFind group |> Option.defaultValue [||]

    member __.ScanOnce(ids, timeout) = 
        let codes =
            ids 
            |> Seq.distinct 
            |> Seq.choose (fun x -> Map.tryFind x scanners)
            |> Seq.map (fun s -> async {
                let! code = s.AsyncScan(timeout)
                return s.Id, code
               })
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Map.ofSeq
        ids |> Array.map (fun i -> Map.tryFind i codes |> Option.defaultValue "")

    member __.Stop(ids) = 
        ids 
        |> Seq.distinct 
        |> Seq.choose (fun x -> Map.tryFind x scanners)
        |> Seq.map (fun s -> s.AsyncStop())
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    interface Contracts.Barcodes.IBarcodesScanner with
        member __.Initialize() = __.Initialize()
        member __.GetIDs(group) = __.GetIDs(group)
        member __.ScanOnce(ids, timeout) = __.ScanOnce(ids, timeout)
        member __.Stop(ids) = __.Stop(ids)
