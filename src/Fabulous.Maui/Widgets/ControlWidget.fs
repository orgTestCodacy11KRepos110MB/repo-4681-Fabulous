﻿namespace Fabulous.Maui.Widgets

open Fabulous.Widgets

type IApplicationControlWidget =
    inherit IApplicationWidget
    inherit IControlWidget

type IWindowControlWidget =
    inherit IWindowWidget
    inherit IControlWidget

type IViewControlWidget =
    inherit IViewWidget
    inherit IControlWidget