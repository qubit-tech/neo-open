namespace Neo.AddIn

open System
open System.IO.Ports

type SerialPortConfig = {
    BaudRate : int
    DataBits : int
    Parity : Parity
    StopBits : StopBits
}

type BarcodesScannerConfig = {
    PortNames : string[]
    SerialPort : SerialPortConfig
    StartCommand : byte[]
    StopCommand : byte[]
    TrimStart : char[]
    TrimEnd : char[]
    //ReadTimeout : int
}

module internal Internal =
    type Message = 
        | Scan of string[] * TimeSpan * AsyncReplyChannel<string[]>
        | Stop of AsyncReplyChannel<unit>

    open Neo.Configuration.Toml

    let parseCfg tokens defaultCfg = 
        let updateSerialPort f cfg =
            { cfg with SerialPort = f cfg.SerialPort }
        let ignoreToken t = (sprintf "ignore %A") t
        let rec loop ts cfg errs =
            match ts with
            | [] -> cfg, errs |> List.rev
            | t::ts ->
                let updateCfg f cfg = 
                    match f cfg with
                    | Some cfg -> cfg, errs
                    | None -> cfg, (ignoreToken t)::errs
                let cfg, errs =
                    match t with
                    | KeyValue ("baud-rate", TomlValue.Int x) -> cfg |> updateSerialPort (fun sp -> { sp with BaudRate = x }), errs
                    | KeyValue ("data-bits", TomlValue.Int x) -> cfg |> updateSerialPort (fun sp -> { sp with DataBits = x }), errs
                    | KeyValue ("stop-bits", TomlValue.Int x) -> 
                        cfg 
                        |> updateCfg (fun cfg -> 
                            match x with
                            | 0 -> Some StopBits.None 
                            | 1 -> Some StopBits.One  
                            | 2 -> Some StopBits.Two  
                            | _ -> None
                            |> Option.map (fun bits -> cfg |> updateSerialPort (fun sp -> { sp with StopBits = bits }))
                           )                            
                    | KeyValue ("stop-bits", TomlValue.Float x) -> 
                        cfg 
                        |> updateCfg (fun cfg -> 
                            match x with
                            | 0. -> Some StopBits.None 
                            | 1. -> Some StopBits.One  
                            | 1.5 -> Some StopBits.OnePointFive
                            | 2. -> Some StopBits.Two  
                            | _ -> None
                            |> Option.map (fun bits -> cfg |> updateSerialPort (fun sp -> { sp with StopBits = bits }))
                           )
                    | KeyValue ("parity", TomlValue.String x) -> 
                        cfg 
                        |> updateCfg (fun cfg -> 
                            match x with
                            | "n" | "N" -> Some Parity.None
                            | "o" | "O" -> Some Parity.Odd
                            | "e" | "E" -> Some Parity.Even
                            | _ -> None 
                            |> Option.map (fun p -> cfg |> updateSerialPort (fun sp -> { sp with Parity = p }))
                           )
                    | KeyValue ("serial-ports", TomlValue.Array (NodeArray.Strings xs)) -> 
                        { cfg with PortNames = xs |> List.filter (String.IsNullOrWhiteSpace >> not) |> List.distinct |> List.sort |> List.toArray }, errs
                    | KeyValue ("start-command", TomlValue.Array (NodeArray.Ints xs)) when xs |> List.forall (fun x -> x >= 0 && x <= 255) -> 
                        { cfg with StartCommand = xs |> List.map byte |> List.toArray}, errs
                    | KeyValue ("stop-command", TomlValue.Array (NodeArray.Ints xs)) when xs |> List.forall (fun x -> x >= 0 && x <= 255) -> 
                        { cfg with StopCommand = xs |> List.map byte |> List.toArray}, errs
                    | KeyValue ("trim-start", TomlValue.String s) -> 
                        { cfg with TrimStart = s.ToCharArray() }, errs
                    | KeyValue ("trim-end", TomlValue.String s) -> 
                        { cfg with TrimEnd = s.ToCharArray() }, errs
                    | _ -> cfg, (ignoreToken t)::errs
                loop ts cfg errs
        loop tokens defaultCfg []

open Internal
open FParsec

type SerialPortBarcodesScanner() = 
    inherit System.MarshalByRefObject()
    static let defaultCfg = {
        PortNames = [|"COM1"|]
        SerialPort = 
            {
                BaudRate = 9600
                DataBits = 8
                Parity = Parity.None
                StopBits = StopBits.One 
            }
        StartCommand = [||] // [|22uy; 84uy; 13uy|]
        StopCommand = [||] // [|22uy; 85uy; 13uy|]
        TrimStart = [|' '; '\t'; '\r'; '\n'; '\000'|]
        TrimEnd = [|' '; '\t'; '\r'; '\n'; '\000'|]
    }
    let mutable cfg = defaultCfg
    let init () = 
        let dll = typeof<SerialPortBarcodesScanner>.Assembly.Location
        let file = System.IO.Path.ChangeExtension(dll, ".toml")
        if System.IO.File.Exists(file) then
            let text = System.IO.File.ReadAllText(file)
            match Neo.Configuration.Toml.Parser.parse text with
            | Success(tokens, _, _) -> 
                let cfg', errs = parseCfg tokens defaultCfg
                cfg <- cfg'
                errs |> List.iter (printfn "warning: %s")
            | Failure(err, _, _) -> failwith err
    let scanAgent = MailboxProcessor.Start(fun inbox ->
        async {
            while true do
                let! (ids, timeout : TimeSpan, ch : _ AsyncReplyChannel, cancel) = inbox.Receive()
                let scan cancel id = async {
                    use sp = new SerialPort(id, cfg.SerialPort.BaudRate, cfg.SerialPort.Parity, cfg.SerialPort.DataBits, cfg.SerialPort.StopBits)
                    sp.Open()
                    let sendCmd (cmd : byte[]) =
                        if cmd.Length > 0 then sp.Write(cmd, 0, cmd.Length)
                    sendCmd cfg.StartCommand
                    let sw = System.Diagnostics.Stopwatch.StartNew()
                    let buf : byte[] = Array.zeroCreate 1024
                    let rec wait () = async {
                        if (timeout.TotalMilliseconds >= 0. && sw.Elapsed > timeout) || !cancel 
                        then return false
                        else
                            if sp.BytesToRead > 0 
                            then return true
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
                    let! code = read ()
                    sendCmd cfg.StopCommand
                    return code
                }
                let safeScan cancel id = async {
                    try 
                        let! code = scan cancel id
                        return code.TrimStart(cfg.TrimStart).TrimEnd(cfg.TrimEnd)
                    with
                    | ex -> 
                        Console.Error.WriteLine(ex.Message)
                        return "";
                }
                        
                let! codes = ids |> Array.map (safeScan cancel) |> Async.Parallel
                ch.Reply codes
        })
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec loop cancel = async {
            let! msg = inbox.Receive()
            cancel := true
            match msg with
            | Stop ch -> 
                ch.Reply ()
                return! loop cancel
            | Scan (ids, timeout, ch) ->
                let cancel = ref false
                scanAgent.Post(ids, timeout, ch, cancel)
                return! loop cancel
        }
        loop (ref false)
    )        
    override __.InitializeLifetimeService() = null
    member __.Initialize() = init ()
    member __.GetIDs() = cfg.PortNames
    member __.ScanOnceAsync(ids, timeout) = agent.PostAndAsyncReply(fun ch -> Scan(ids, timeout, ch)) |> Async.StartAsTask
    member __.ScanOnce(ids, timeout) = agent.PostAndReply(fun ch -> Scan(ids, timeout, ch))
    member __.Stop() = agent.PostAndReply(Stop)
    interface Contracts.Barcodes.IBarcodesScanner with
        member __.Initialize() = __.Initialize()
        member __.GetIDs() = __.GetIDs()
        member __.ScanOnce(ids, timeout) = __.ScanOnce(ids, timeout)
        member __.Stop() = __.Stop()

