module FsLab.Formatters.Themes.DefaultWhite
open FsLab.Formatters

// Background colors
fsi.HtmlPrinterParameters.["background-color"] <- "transparent"
fsi.HtmlPrinterParameters.["background-color-highlighted"] <- "#fdfdfd"
fsi.HtmlPrinterParameters.["background-color-alternate"] <- "#f4f4f4"

// Text & foreground colors
fsi.HtmlPrinterParameters.["border-color"] <- "#a0a0a0"
fsi.HtmlPrinterParameters.["scrollbar-color"] <- "#c1c1c1"
fsi.HtmlPrinterParameters.["text-color"] <- "#000000"
fsi.HtmlPrinterParameters.["text-color-subtle"] <- "#a0a0a0"

// Other visual styles
fsi.HtmlPrinterParameters.["font-family"] <- "sans-serif"
fsi.HtmlPrinterParameters.["chart-color-palette"] <- 
    "#1f77b4,#aec7e8,#ff7f0e,#ffbb78,#2ca02c,#98df8a,"+
    "#d62728,#ff9896,#9467bd,#c5b0d5,#8c564b,#c49c94,#e377c2,"+
    "#f7b6d2,#7f7f7f,#c7c7c7,#bcbd22,#dbdb8d,#17becf,#9edae5"

// Grids, matrices and related
fsi.HtmlPrinterParameters.["float-format"] <- "G4"        // For example: System.Math.PI.ToString("G4")
fsi.HtmlPrinterParameters.["grid-row-counts"] <- "8,4"    // no. rows of a grid that appears before & after '...'
fsi.HtmlPrinterParameters.["grid-column-counts"] <- "3,3" // no. cols of a grid that appears before & after '...'
