// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open fsrandomflow
open fsrandomflow.RVar
open NGraphics

//A galactic belt, a set of offsets
type GalaxyLine = { rotation: float; bendAmount: float; bendOffset: float }

//A star
type Star = { posX: float; posY: float }

//Get a star not in the galaxy
let getGeneralStar = randomly {
        let! x = RVar.UniformZeroToOne
        let! y = RVar.UniformZeroToOne
        return { posX = x; posY = y }
    }

let getGalaxyLine = randomly {
        let! bendPoint = RVar.Normal(0.0,0.5)
        let! bendAmt = RVar.Normal(0.0,0.3)
        let! rot = RVar.UniformInterval(0.0,2.0*System.Math.PI,false,true)
        return { rotation = rot; bendAmount = bendAmt; bendOffset = bendPoint } 
    }
    
let bendPoint bend (x,y) o = ((1.0-(abs o)) * bend * y,(1.0-(abs o)) * bend * x)

let getGalaxyPoint {rotation=theta; bendAmount=bend; bendOffset=magnitude} run error =
    let (dx,dy) = bendPoint bend (sin theta * magnitude, cos theta * magnitude) run
    let (bx,by) = (sin theta * (0.5 + run), cos theta * (0.5 + run))
    let (cx,cy) = (by*error,bx*error)
    let x = bx+dx+cx
    let y = by+dy+cy
    (x,y)

let rec getGalaxyStar galaxyLine = randomly {
        let! run = RVar.UniformIntervalOpen((-1.0),1.0)
        let! error = RVar.Normal(0.0,0.01)
        let (x,y) = getGalaxyPoint galaxyLine run error
        if(x >= 0.0 && y >= 0.0 && x <= 1.0 && y <= 1.0) then return { posX=x; posY=y }
        else return! getGalaxyStar galaxyLine
    }

let getStars n = randomly {
        let! galaxyLine = getGalaxyLine
        return! RVar.take n <| randomly {
                    let! inGalaxy = RVar.probability 0.8
                    if inGalaxy then return! getGalaxyStar galaxyLine
                    else return! getGeneralStar
                }
    }
[<EntryPoint>]
let main argv = 
    let runner = if argv.Length < 1 then RVar.runrvarIO else RVar.runrvar (int(argv.[0]))
    let stars = runner (getStars 1000)  
    let platform = new NGraphics.SystemDrawingPlatform()
    let canvas = platform.CreateImageCanvas(new NGraphics.Size(1.0,1.0), scale=1000.0)
    for {posX=x; posY=y} in stars do 
        canvas.FillEllipse(x,y,0.002,0.002,NGraphics.Colors.White)
    //0 // return an integer exit code
    canvas.SaveState()
    canvas.GetImage().SaveAsPng("out.png")
    0