//Copyright 2017, Barend Venter
//This code is liscensed under the MIT license, see LICENSE

//You will need to install the NFugue library to build this, get it off NuGet
//Either supply a seed, or just run it without arguments, and it'll play a short canon

open NFugue
open System.Text
open fsrandomflow
open fsrandomflow.RVar

//Instrument choices for the ensemble
let voice1 = Midi.Instrument.ChoirAahs
let voice2 = Midi.Instrument.VoiceOohs
let voice3 = Midi.Instrument.AcousticGuitarNylon
let voice4 = Midi.Instrument.Harpsichord

//Get a string of Staccato markup to give to NFugue
let progression = randomly {
        //Choose a major scale one half of the time, harmonic minor a quarter of the time, other minors one eight of the time each
        let! (melodic,harmonic) = 
            RVar.oneOfWeighted [| (0.5,(true, true)) ; (0.125,(false, false)) ; (0.25,(true, false)) ; (0.125,(false, true)) |]
        //White space separator
        let w = RVar.constant " "
        //Define the chord types
        let tonic = RVar.constant (if (melodic && harmonic) then "I" else "i")
        let perfect = RVar.oneOf([| (if melodic then "IV" else "iv") ; (if (melodic || harmonic) then "V" else "v") |])
        let dissonant = RVar.oneOf([| (if melodic then "ii" else "iio") ; 
                                      (if (melodic <> harmonic) then "nviio" else if melodic then "viio" else "VII") |])
        let consonant = RVar.oneOf([| (if (melodic <> harmonic) then "III+" else if melodic then "iii" else "III") ;
                                      (if not melodic then "VI" else if harmonic then "vi" else "nvio") |])
        let nondissonnant = RVar.union[tonic;perfect;consonant]
        let any = RVar.union[tonic;perfect;consonant;dissonant]
        //Fold using a string builder and return
        let appendNote (str : StringBuilder) (next : string) = 
            str.Append(next)
        let builder = new StringBuilder()
        let! results = RVar.sequence [tonic; w; any; w; any; w; perfect; w; any; w; any; w; any; w; perfect]
        return (Array.fold appendNote builder results).ToString()
    }

//Get a chord progression to use as the foundation for the canon
let canonBase = randomly {
        //Choose a key
        let! root = RVar.oneOf([|"A"; "B"; "C"; "D"; "E"; "F"; "G"|])
        let! minor = RVar.CoinFlip
        //Get a chord progression
        let! chords = progression
        //Transpose the progression to the chosen key and return it
        return NFugue.Theory.ChordProgression(chords).SetKey(root)
    }

//Get a random canon, ready to be played
let canon =
        //Start on the tonic for the first chord
        let getfst = randomly {
                let! rest = RVar.shuffle[1;2]
                return Array.ofSeq (seq { yield 0; yield! rest })
            }
        //End on the fifth for the last chord
        let getlst = randomly {
                let! rest = RVar.shuffle[1;2]
                return Array.ofSeq (seq {yield! rest; yield 0})
            }
        randomly {
            //Get a chord progression
            let! prog = canonBase
            let notes = prog.GetChords() |> Array.map(fun x -> x.GetNotes())
            //Choose the notes in the chords to be played
            let! start = getfst
            let! ending = getlst
            let! mids = RVar.take 6 (RVar.shuffle[0;1;2])
            let result = seq { yield start; yield! mids; yield ending }
            //Extract the chosen notes out of the chords
            let getLine n = result |> Seq.map(fun x -> x.[n]) 
            let melody() = 
                seq {yield! getLine 0; yield! getLine 1; yield! getLine 2}
                |> Seq.mapi(fun i j -> notes.[i % 8].[j].GetPattern() :> NFugue.Patterns.IPatternProducer)
                |> Array.ofSeq
                |> (fun x -> NFugue.Patterns.Pattern(x))
            //Set up and return a track table after writing the canon into it
            let tracks = Patterns.TrackTable(13,0.5)
            let (m1,m2,m3,m4) = ((melody()).SetInstrument(voice1).SetVoice(1).AddToEachNoteToken("4"),
                                 (melody()).SetInstrument(voice2).SetVoice(2).AddToEachNoteToken("3"),
                                 (melody()).SetInstrument(voice3).SetVoice(3).AddToEachNoteToken("2"),
                                 (melody()).SetInstrument(voice4).SetVoice(4).AddToEachNoteToken("1"))
            return tracks.Add(1,0,m1).Add(2,4,m2).Add(3,8,m3).Add(4,12,m4)
        }
     
[<EntryPoint>]
let main argv = 
    use player = new NFugue.Playing.Player()
    let seed = if argv.Length = 0 then let x = System.DateTime.Now.Ticks in (int)x else (int)argv.[0]
    System.Console.WriteLine("Playing canon {0}", seed)
    player.Play(RVar.runrvar seed canon)
    0 // return an integer exit code
