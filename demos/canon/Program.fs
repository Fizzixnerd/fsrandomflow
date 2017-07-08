//Copyright 2017, Barend Venter
//This code is liscensed under the MIT license, see LICENSE

//You will need to install the NFugue library to build this, get it off NuGet
//Either supply a seed, or just run it without arguments, and it'll play a short canon

open NFugue
open System.Text
open fsrandomflow
open fsrandomflow.RVar

let union v1 v2 = randomly {
        let! v1' = v1
        let! v2' = v2
        return! oneOf([|v1'; v2'|])
    }

let progression major = randomly {
        let tonic = RVar.constant (if major then "I" else "i")
        let w = RVar.constant " "
        let perfect = RVar.oneOf([| (if major then "iv" else "IV") ; (if major then "v" else "V") |])
        let dissonant = RVar.oneOf([| (if major then "ii" else "II") ; (if major then "vii" else "VII") |])
        let consonant = RVar.oneOf([| (if major then "iii" else "III") ; (if major then "vi" else "VI") |])
        let nondissonnant = union perfect consonant
        let any = union dissonant nondissonnant
        let appendNote (str : StringBuilder) (next : string) = 
            str.Append(" ").Append(next)
        let builder = new StringBuilder()
        let! results = RVar.sequence [tonic; w; nondissonnant; w; nondissonnant; w; perfect; w; tonic; w; nondissonnant; w; any; w; tonic]
        return results
               |> Array.fold (fun s t -> s+t) ""
    }

let canonBase = randomly {
        let! root = RVar.oneOf([|"A"; "B"; "C"; "D"; "E"; "F"; "G"|])
        let! major = RVar.CoinFlip 
        let! chords = progression major
        return NFugue.Theory.ChordProgression(chords).SetKey(root)
    }

let canon =
        let getfst = randomly {
                let! rest = RVar.shuffle[1;2]
                return Array.ofSeq (seq { yield 0; yield! rest })
            }
        let getlst = randomly {
                let! rest = RVar.shuffle[1;2]
                return Array.ofSeq (seq {yield! rest; yield 0})
            }
        randomly {
            let! prog = canonBase 
            let notes = prog.GetChords() |> Array.map(fun x -> x.GetNotes())
            let! start = getfst
            let! ending = getlst
            let! mids = RVar.take 6 (RVar.shuffle[0;1;2])
            let result = seq { yield start; yield! mids; yield ending }
            let getLine n = result |> Seq.map(fun x -> x.[n]) 
            let melody ()= 
                seq {yield! getLine 0; yield! getLine 1; yield! getLine 2}
                |> Seq.mapi(fun i j -> notes.[i % 8].[j].GetPattern() :> NFugue.Patterns.IPatternProducer)
                |> Array.ofSeq
                |> (fun x -> NFugue.Patterns.Pattern(x))
            let tracks = Patterns.TrackTable(13,0.5)
            let (m1,m2,m3,m4) = ((melody()).SetInstrument(Midi.Instrument.ChoirAahs).SetVoice(1).AddToEachNoteToken("4"),
                                 (melody()).SetInstrument(Midi.Instrument.VoiceOohs).SetVoice(2).AddToEachNoteToken("3"),
                                 (melody()).SetInstrument(Midi.Instrument.AcousticGuitarNylon).SetVoice(3).AddToEachNoteToken("2"),
                                 (melody()).SetInstrument(Midi.Instrument.Harpsichord).SetVoice(4).AddToEachNoteToken("1"))
            return tracks.Add(1,0,m1).Add(2,4,m2).Add(3,8,m3).Add(4,12,m4)
        }
     
[<EntryPoint>]
let main argv = 
    use player = new NFugue.Playing.Player()
    let seed = if argv.Length = 0 then let x = System.DateTime.Now.Ticks in (int)x else (int)argv.[0]
    System.Console.WriteLine("Playing canon {0}", seed)
    player.Play(RVar.runrvar seed canon)
    0 // return an integer exit code
