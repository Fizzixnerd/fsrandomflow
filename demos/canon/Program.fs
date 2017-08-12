//Copyright 2017 Barend Venter
//This code liscensed under the MIT license, see LICENSE
//Get it at github.com/barendventer/fsrandomflow
//You will need to install the NFugue library to build this, get it off NuGet
//Either supply a seed, or just run it without arguments, and it'll play a short canon

open NFugue
open System.Text
open System.IO
open FsRandomFlow
open FsRandomFlow.RVar

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

//The notes
let noteStrings = [| "C"; "C#"; "D"; "Eb"; "E"; "F"; "F#"; "G"; "G#"; "A"; "Bb"; "B" |]
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

//Get notes from a scale, using a list of chords
let getScaleNotes (mode : string[]) (key: string) =
    NFugue.Theory.ChordProgression(mode).SetKey(key).GetChords()
    |> Seq.map (fun x -> x.GetNotes().[0].ToString())
    |> Array.ofSeq

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
    let mode   = (total-major-hminor-ominor-ominor)/5.0 //5 modes
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
let progression (chordChoices : string []) = randomly {
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

//Model a little wigged and hatted man to articulate the instructions a little with eights and halves
let rec articulate keyboard (incoming : string seq) =
    //Done playing, move on
    if Seq.isEmpty incoming then Seq.empty
    else if Seq.isEmpty (Seq.tail incoming) then incoming
    else
        let s0 = Seq.head incoming
        let s1 = Seq.head (Seq.tail incoming)
        //If the two strings are the same, return a half note
        if s0.ToUpper() = s1.ToUpper() then seq {yield (s0 + "h"); yield! articulate keyboard (Seq.tail (Seq.tail incoming))}
        else
            let keyboardSize = Array.length keyboard
            //Tween any third intervals that are within the scale, but not accross the octave
            let i0 = keyboard |> Array.tryFindIndex(fun (s: string) -> s.ToUpper() = s0.ToUpper()) 
            let i1 = keyboard |> Array.tryFindIndex(fun (s: string) -> s.ToUpper() = s1.ToUpper())
            match (i0, i1) with
                | (Some(i0'),Some(i1')) ->
                    let imin = min i0' i1'
                    let imax = max i0' i1'
                    if imax - imin = 2 then seq { yield s0 + "i"; yield keyboard.[imin+1] + "i"; yield! articulate keyboard (Seq.tail incoming) }
                    else seq { yield s0; yield! articulate keyboard (Seq.tail incoming) }
                | _ -> seq {yield s0; yield! articulate keyboard (Seq.tail incoming)}

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
            let! key = RVar.oneOf(noteStrings)
            let! mode = scale
            let! prog = progression mode
            let chords = NFugue.Theory.ChordProgression(prog).SetKey(key)
            let keyboard = getScaleNotes mode key
            let notes = chords.GetChords() |> Array.map(fun x -> x.GetNotes())
            //Choose the notes in the chords to be played
            let! start = getfst
            let! ending = getlst
            let! mids = RVar.take 6 (RVar.shuffle[0;1;2])
            let noteLine = seq { yield start; yield! mids; yield ending }
            //Extract the chosen notes out of the chords
            let getLine n = noteLine |> Seq.map(fun x -> x.[n]) 
            let notes = 
                seq {yield! getLine 0; yield! getLine 1; yield! getLine 2}
                |> Seq.mapi(fun i j -> notes.[i % 8].[j].ToString())//.GetPattern()) :> NFugue.Patterns.IPatternProducer)
                |> articulate keyboard
                |> unwords
                |> NFugue.Patterns.Pattern
            let melody() = NFugue.Patterns.Pattern(notes)
            //Set up and return a track table after writing the canon into itue
            let tracks = Patterns.TrackTable(13,0.5)
            let (m1,m2,m3,m4) = ((melody()).SetInstrument(voice1).SetVoice(1),
                                 (melody()).SetInstrument(voice2).SetVoice(2),
                                 (melody()).SetInstrument(voice3).SetVoice(3),
                                 (melody()).SetInstrument(voice4).SetVoice(4))
            return tracks.Add(1,0,m1).Add(2,4,m2).Add(3,8,m3).Add(4,12,m4)
        }

let printUsage () = 
    System.Console.WriteLine("usage: canon [ -o ] [ name ]")

let getSeed str = 
    let nseed = ref 0
    if System.Int32.TryParse(str,nseed) then nseed.Value
    else str.ToUpper().GetHashCode()

let writeCanon path seed = 
    let music = RVar.runrvar seed canon
    NFugue.Midi.Conversion.MidiFileConverter.SavePatternToMidi(music, path)

let playCanon name seed = 
    let music = RVar.runrvar seed canon
    System.Console.WriteLine("Playing canon {0} (integer seed: {1})", name, seed)
    System.Console.WriteLine("You can use \"{0} -o {1}\" to save this to a file",
                                System.Environment.GetCommandLineArgs().[0],
                                name)
    use player = new NFugue.Playing.Player()
    player.Play(music)

let play () =
    let seed = (int)System.DateTime.Now.Ticks
    playCanon (seed.ToString()) seed

//Append a file extension to the output if the user didn't give one
let appendMid (str : string) = if not (str.EndsWith(".mid")) then str+".mid" else str

[<EntryPoint>]
let main argv = 
    //NFugue's unfortunate dependency on Sanford.Multimedia.Midi means it can only work on windows and nowhere else
    if System.Environment.OSVersion.Platform = System.PlatformID.Win32NT
    then 
        let nargs = argv.Length
        if nargs = 2 && argv.[0] = "-o" then writeCanon (appendMid argv.[1]) (getSeed argv.[1])
        else if nargs = 1 then playCanon (argv.[0]) (getSeed argv.[0])
        else if nargs = 0 then play()
        else printUsage()
    else
        System.Console.WriteLine("This platform is not supported")
    0 // return an integer exit code