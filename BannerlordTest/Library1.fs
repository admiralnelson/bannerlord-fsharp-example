namespace BannerlordControlSpaceSpeed

open System
open System.Diagnostics
open TaleWorlds.MountAndBlade
open TaleWorlds
open TaleWorlds.Core
open TaleWorlds.InputSystem
open TaleWorlds.CampaignSystem
open TaleWorlds.Library

module Helper = 
    let mutable counter = 1

    let Print x = 
        InformationManager.DisplayMessage(new InformationMessage(x))
    let MessageboxOK title msg onOK = 
        InformationManager.ShowInquiry(new InquiryData(title, msg, true, false, "Accept", "Decline", onOK, fun() -> ()))
    let Messagebox title msg onOk onCancel =
         InformationManager.ShowInquiry(new InquiryData(title, msg, true, true, "Accept", "Decline", onOk, onCancel))
    let GenerateSaveGameName () = 
        counter <- counter + 1
        if counter >= 3 then counter <- 0
        match Campaign.Current with
        | null -> ""
        | _ -> "Ctrl+s save " + Campaign.Current.MainParty.Name.ToString() + " " + counter.ToString()

    
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

    let private DetectInputWithModifier key combination callback released deltaTime = 
        match combination with
        | InputKey.LeftControl 
        | InputKey.RightControl when released && ((Input.IsKeyReleased combination && Input.IsDown key) || (Input.IsDown  combination && Input.IsKeyReleased key)) -> callback deltaTime
        | InputKey.LeftShift 
        | InputKey.RightShift when released && Input.IsKeyReleased combination && Input.IsKeyReleased key -> callback deltaTime 
        | InputKey.LeftControl 
        | InputKey.RightControl when not released &&  Input.IsKeyDown combination &&  Input.IsKeyDown key -> callback deltaTime       
        | InputKey.LeftShift 
        | InputKey.RightShift when not released && Input.IsKeyDown combination && Input.IsKeyDown key -> callback deltaTime
        | _ -> ()
      
    let private DetectInput key callback released deltaTime  =
        if Input.IsKeyReleased key && released then callback deltaTime
        if Input.IsKeyDown key && not released then callback deltaTime

    let Tick (deltaTime: float32) =
        queue |> List.iter(
        fun item -> 
            match item with 
            | (key, comb, callback, released) -> match comb with
                                                | InputKey.LeftControl 
                                                | InputKey.RightControl -> DetectInputWithModifier key comb callback released deltaTime
                                                | InputKey.Invalid -> DetectInput key callback released deltaTime
            )


module WorldMapHandler = 

    let FastForward deltaTime =
        match Campaign.Current with
        | null -> ()
        | _ -> Campaign.Current.SetTimeSpeed 2

        
    let Normal deltaTime = 
        match Campaign.Current with
        | null -> ()
        | _ -> Campaign.Current.SetTimeSpeed 1

    let mutable debouncer = 0.0f
    let Save deltaTime = 
        debouncer <- debouncer + deltaTime
        if debouncer < 0.3f then ()
        else 
            debouncer <- 0.0f
            match Campaign.Current with
            | null -> ()
            | _ -> Campaign.Current.SaveHandler.SaveAs(Helper.GenerateSaveGameName())
       



type MySubmodule() =
  inherit MountAndBlade.MBSubModuleBase()

    override this.OnApplicationTick deltaTime = 
        MyInputSystem.Tick deltaTime 

    override this.OnBeforeInitialModuleScreenSetAsRoot () = 

        MyInputSystem.RegisterInputChording InputKey.Space InputKey.LeftControl (
            fun (deltaTime) -> 
                WorldMapHandler.FastForward deltaTime)

        MyInputSystem.RegisterInputChordingRelease InputKey.Space InputKey.LeftControl (
            fun (deltaTime) -> 
                WorldMapHandler.Normal deltaTime)

        MyInputSystem.RegisterInputChording InputKey.S InputKey.LeftControl (
            fun (deltaTime) ->
                WorldMapHandler.Save deltaTime)


    