namespace BannerlordTest

open System
open System.Diagnostics
open MountAndBlade
open TaleWorlds
open TaleWorlds.Core
open TaleWorlds.InputSystem
open TaleWorlds.CampaignSystem

module Helper = 
    let Print x = 
        InformationManager.DisplayMessage(new InformationMessage(x))

    let MessageboxOK title msg onOK = 
        InformationManager.ShowInquiry(new InquiryData(title, msg, true, false, "Accept", "Decline", onOK, fun() -> ()))
    let Messagebox(title, msg, onOk, onCancel) =
         InformationManager.ShowInquiry(new InquiryData(title, msg, true, true, "Accept", "Decline", onOk, onCancel))

    
module MyInputSystem = 
    
    let mutable private queue = []
    
    
    //keyAndCallback[0]: the key
    //keyAndCallback[1]: combination, InputKey.Invalid if I don't want any key chording
    //keyAndCallback[2]: the callback
    let RegisterInputChording key combination callback = 
        queue <- (key, combination, callback, false) :: queue
      
    let RegisterInputChordingRelease key combination callback = 
        queue <- (key, combination, callback, true) :: queue

    let RegisterInput key callback = 
        queue <- (key, InputKey.Invalid, callback, false) :: queue

    let RegisterInputRelease key callback = 
        queue <- (key, InputKey.Invalid, callback, true) :: queue

    let private DetectInputWithModifier key combination callback released = 
        match combination with
        | InputKey.LeftControl 
        | InputKey.RightControl when released && ((Input.IsKeyReleased combination && Input.IsDown key) || (Input.IsDown  combination && Input.IsKeyReleased key)) -> callback()   
        | InputKey.LeftShift 
        | InputKey.RightShift when released && Input.IsKeyReleased combination && Input.IsKeyReleased key -> callback()   
        | InputKey.LeftControl 
        | InputKey.RightControl when not released &&  Input.IsKeyDown combination &&  Input.IsKeyDown key -> callback()        
        | InputKey.LeftShift 
        | InputKey.RightShift when not released && Input.IsKeyDown combination && Input.IsKeyDown key -> callback()   
        | _ -> ()
      
    let private DetectInput key callback released =
        if Input.IsKeyReleased key && released then callback()
        if Input.IsKeyDown key && not released then callback()

    
    let Tick() =
        queue |> List.iter(
        fun item -> 
            match item with 
            | (key, comb, callback, released) -> match comb with
                                                | InputKey.LeftControl 
                                                | InputKey.RightControl -> DetectInputWithModifier key comb callback released
                                                | InputKey.Invalid -> DetectInput key callback released
         )



module WorldMapHandler = 
    let mutable private  wasPressed = false

    let FastForward() =
        match Campaign.Current with
        | null -> ()
        | _ -> Helper.Print "fast forward"
               Campaign.Current.SetTimeSpeed 2

        
    let Normal() = 
        match Campaign.Current with
        | null -> ()
        | _ ->  wasPressed <- false
                Campaign.Current.SetTimeSpeed 1
        



type MySubmodule() =
  inherit MountAndBlade.MBSubModuleBase()

    override this.OnApplicationTick dt = 
        MyInputSystem.Tick()

    override this.OnBeforeInitialModuleScreenSetAsRoot () = 
        Helper.Print "working"
        Debug.Print "test"
        Helper.MessageboxOK "test" "called from F#" (Action(fun() -> Helper.Print("ok was pressed")))

        MyInputSystem.RegisterInputChording InputKey.Space InputKey.LeftControl (
            fun () -> 
                WorldMapHandler.FastForward())

        MyInputSystem.RegisterInputChordingRelease InputKey.Space InputKey.LeftControl (
            fun () -> 
                WorldMapHandler.Normal()
                Helper.Print "ctrl+space released")

        MyInputSystem.RegisterInput InputKey.Tab (fun () -> Helper.Print "tab was pressed")

    