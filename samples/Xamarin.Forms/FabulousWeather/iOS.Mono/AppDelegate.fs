﻿namespace FabulousWeather.iOS.Mono

open System
open UIKit
open Foundation
open Xamarin.Essentials
open Xamarin.Forms
open Xamarin.Forms.Platform.iOS
open FabulousWeather
open Fabulous
open Fabulous.XamarinForms

[<Register("AppDelegate")>]
type AppDelegate() =
    inherit FormsApplicationDelegate()

    override this.FinishedLaunching(app, options) =
        UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.LightContent, true)
        
        Forms.Init()

        let application = Program.createApplication App.program ()
        this.LoadApplication(application)

        base.FinishedLaunching(app, options)

module Main =
    [<EntryPoint>]
    let main args =
        UIApplication.Main(args, null, typeof<AppDelegate>)
        0
