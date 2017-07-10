//Copyright 2017, Barend Venter
//This code is liscensed under the MIT license, see LICENSE

//You will need to install the NFugue library to build this, get it off NuGet
//Either supply a seed, or just run it without arguments, and it'll play a short canon

open NFugue
open System.Text
open fsrandomflow
open fsrandomflow.RVar

//Unwords function for getting a string out of a sequence of words
let rec intersperse x xs = 
    if Seq.isEmpty xs || Seq.isEmpty (Seq.tail xs) then xs 
    else seq { yield Seq.head xs; yield x; yield! intersperse x (Seq.tail xs) }
let unwords words = 
    let sb = System.Text.StringBuilder()
    intersperse " " words |> Seq.iter (sb.Append >> ignore)
    sb.ToString()

//Instrument choices for the ensemble
let voice1 = Midi.Instrument.ChoirAahs
let voice2 = Midi.Instrument.VoiceOohs
let voice3 = Midi.Instrument.AcousticGuitarNylon
let voice4 = Midi.Instrument.Harpsichord

//Major scale
let ionian = [| "I"; "ii"; "iii"; "IV"; "V"; "vi"; "viio" |]
//Minor scales
let harmonicMinor = [| "i"; "iio"; "III+"; "iv"; "V"; "VI"; "nviio" |]
let melodicMinor  = [| "i"; "ii"; "III+"; "IV"; "V"; "nvio"; "nviio" |]
let aeolian       = [| "i"; "iio"; "III"; "iv"; "V"; "VI"; "nviio" |]
//Major modal scales
let dorian     = [| "i"; "ii"; "III"; "IV"; "v"; "vio"; "VII"|]
let phyrgian   = [| "i"; "II"; "III"; "iv"; "vo"; "VI"; "vii"|]
let lydian     = [| "I"; "II"; "iii"; "ivo"; "V"; "vi"; "vii"|]
let mixolydian = [| "I"; "ii"; "iiio"; "IV"; "v"; "vi"; "VII"|]
let locrian    = [| "io"; "II"; "iii"; "iv"; "V"; "VI"; "vii"|]

//Pecking order:
//Major scale half the time
//Harmonic minor quarter of the time
//Other minors 1/8th of the time.
//Modes get equal share for the remainder
let scale = 
    let total  = 100.0//%
    let major  = total/2.0
    let hminor = total/4.0
    let ominor = total/(8.0*2.0) //two other minors
    let mode   = (total-major-hminor-ominor)/5.0 //5 modes
    RVar.oneOfWeighted [ (major,ionian)
                       ; (hminor,harmonicMinor)
                       ; (ominor,melodicMinor)
                       ; (ominor,aeolian)
                       ; (mode,dorian)
                       ; (mode,phyrgian)
                       ; (mode,lydian)
                       ; (mode,mixolydian)
                       ; (mode,locrian) ]

//Get a string of Staccato markup to give to NFugue
let progression = randomly {
        //Get a scale
        let! chordChoices = scale
        //Define the chord types with array indexes
        let tonic = RVar.constant 0
        let perfect = RVar.oneOf[3;4]
        let dissonant = RVar.oneOf[1;6]
        let consonant = RVar.oneOf[2;5]
        let nondissonnant = RVar.union[tonic;perfect;consonant]
        let any = RVar.union[tonic;perfect;consonant;dissonant]
        //Get the eight note chord progression for the canon
        let! results = RVar.sequence [tonic; any; any; perfect; any; any; any; perfect]
        return results
               |> Seq.map(fun i -> chordChoices.[i])
               |> unwords
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
