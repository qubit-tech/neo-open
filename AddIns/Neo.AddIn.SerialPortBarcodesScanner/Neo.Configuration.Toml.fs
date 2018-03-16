module Neo.Configuration.Toml

// based on 
// https://github.com/mackwic/To.ml
// https://github.com/seliopou/toml

open System
open System.Globalization

[<RequireQualifiedAccess>]
type NodeArray =
  | Bools   of bool   list
  | Ints    of int    list
  | Floats  of float  list
  | Strings of string list
  | Dates   of DateTime list
  | Arrays  of NodeArray list 
  override __.ToString () =
    let inline f xs = List.map string xs |> String.concat ", "
    match __ with
    | Bools   bs -> f bs 
    | Ints    is -> f is 
    | Floats  fs -> f fs
    | Strings ss -> f ss
    | Dates   ds -> f ds
    | Arrays ars -> f ars

[<RequireQualifiedAccess>]
type TomlValue =
  | Bool   of bool
  | Int    of int
  | Float  of float
  | String of string
  | Date   of DateTime
  | Array  of NodeArray
  override __.ToString () =
    match __ with
    | Bool   b -> sprintf "TBool(%b)"   b
    | Int    i -> sprintf "TInt(%d)"    i
    | Float  f -> sprintf "TFloat(%f)"  f
    | String s -> sprintf "TString(%s)" s
    | Date   d -> sprintf "TDate(%A)" d
    | Array  a -> sprintf "[%O]" a

type Token = 
    | KeyGroup of string list 
    | KeyValue of string * TomlValue

module Parser =
    open FParsec
    module internal Internals =
        let spc = many (anyOf [' '; '\t']) |>> ignore
        let lexeme p = p .>> spc
        let comment = pchar '#' .>>. restOfLine false |>> ignore
        let line p = p .>> lexeme newline
        let blanks = lexeme (skipMany ((comment <|> spc) .>> lexeme newline))


        let ls s = lexeme <| pstring s
        let zee    = ls "Z"
        let quote  = ls "\""
        let lbrace = pstring "[" .>> spaces
        let rbrace = pstring "]" .>> spaces
        let comma  = pstring "," .>> spaces
        let period = ls "."
        let equal  = ls "="
        let ptrue  = ls "true"  >>% true
        let pfalse = ls "false" >>% false

        let pdate' = 
            fun s -> 
              try preturn (DateTime.Parse (s, null, DateTimeStyles.RoundtripKind))               
              with _ -> fail "date format error"


        let pbool  = ptrue <|> pfalse <?> "pbool"
        let pstr   = between quote quote (manySatisfy ((<>)'"')) <?> "pstr"
        let pint   = attempt pint32 <?> "pint"
        let pfloat = attempt pfloat <?> "pfloat"
        let pdate  = attempt (spc >>. anyString 20 .>> spc >>= pdate') <?> "pdate"

        let parray elem = attempt (between lbrace rbrace (sepBy (elem .>> spaces) comma))
        let pboolarray  = parray pbool  |>> NodeArray.Bools   <?> "pboolarray"
        let pdatearray  = parray pdate  |>> NodeArray.Dates   <?> "pdatearray"
        let pintarray   = parray pint   |>> NodeArray.Ints    <?> "pintarray"
        let pstrarray   = parray pstr   |>> NodeArray.Strings <?> "pstrarray"
        let pfloatarray = parray pfloat |>> NodeArray.Floats  <?> "pfloatarray"
        let rec parrayarray = 
          parray (pboolarray <|> pdatearray <|> pintarray <|> pstrarray <|> pfloatarray) 
          |>> NodeArray.Arrays <?> "parrayarray"

        let value = 
          (pbool       |>> TomlValue.Bool ) <|> 
          (pdate       |>> TomlValue.Date ) <|> 
          (pstr        |>> TomlValue.String)<|> 
          (pint        |>> TomlValue.Int  ) <|> 
          (pfloat      |>> TomlValue.Float) <|> 
          (pboolarray  |>> TomlValue.Array) <|>
          (pdatearray  |>> TomlValue.Array) <|>
          (pstrarray   |>> TomlValue.Array) <|>
          (pintarray   |>> TomlValue.Array) <|>
          (pfloatarray |>> TomlValue.Array) <|>
          (parrayarray |>> TomlValue.Array)
  
        let keyvalue = 
          let key = many1Chars (noneOf " \t\n=")
          lexeme key .>>. (equal >>. value) |>> KeyValue

        let keygroup = 
          let key = lexeme (many1Chars (noneOf " \t\n]."))
          blanks >>. between lbrace rbrace (sepBy key period) |>> KeyGroup

        let document = blanks >>. many (keygroup <|> keyvalue .>> blanks)

    open Internals

    let parse text = run document text
//        match run document text with
//        | Success(tokens,_,_) -> tokens
//        | _ -> []

