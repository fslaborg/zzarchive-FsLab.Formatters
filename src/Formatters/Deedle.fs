module FsLab.Formatters.Deedle

// --------------------------------------------------------------------------------------
// Formatting for Deedle frames and series
// --------------------------------------------------------------------------------------

open Suave
open Suave.Operators
open Deedle
open Deedle.Internal
open FSharp.Data
open FsLab.Formatters

// --------------------------------------------------------------------------------------
// CSS styles for the table, scrollbars & c.
// --------------------------------------------------------------------------------------

let gridStyles = """
  .grid {
    font-size:11pt;
    font-family:@font-family;
    color:@text-color;
  }

  .grid ::-webkit-scrollbar  {
    width:6px;
    height:6px;
    background:transparent;
  }

  .grid ::-webkit-scrollbar-track {
    background:transparent;
  }

  .grid ::-webkit-scrollbar-thumb {
    border-radius:3px;
    background-color:@scrollbar-color;
  }

  .grid .faded {
    color:@text-color-subtle;
  }

  .grid tr {
    background-color: @background-color-highlighted;
  }

  .grid tbody tr:nth-child(odd) {
    background-color: @background-color-alternate;
  }

  .grid thead tr {
    background: @background-color-highlighted;
  }

  .grid table {
    top:0px;
    left:0px;
    width:100%;
    border-spacing: 0;
    border-collapse: collapse;
  }

  .grid td, .grid th {
    border-bottom:1px solid @border-color;
    padding:4px 10px 4px 10px;
    min-width:50px;
  }

  .grid thead th {
    border-bottom:3px solid @border-color;
  }

  .grid th {
    padding:4px 20px 4px 10px;
    text-align:left;
    font-weight:bold;
  }

  .live-grid {
    position:relative;
    overflow:hidden;
  }

  .live-grid .scroller {
    overflow-y: scroll;
    position:absolute;
    width:100%;
  }

  .live-grid table {
    position:absolute;
  }"""

// --------------------------------------------------------------------------------------
// JavaScript (only when in online mode) that implements lazy loading
// --------------------------------------------------------------------------------------

let gridLiveScript = """
  function setupGrid(id, viewHeight, serviceUrl) {

    // Create table with given column names & specified number of empty rows
    function createRows(rowCount, columns) {
      var head = $(id + " .head").empty();
      $("<th />").html("#").appendTo(head);
      for (var i = 0; i < columns.length; i++) {
        $("<th />").html(columns[i]).appendTo(head);
      }

      var rows = [];
      var body = $(id + " .body").empty();
      for (var i = 0; i < rowCount; i++) {
        var row = { columns: [] };
        var tr = $("<tr />").appendTo(body);
        var th = $("<th />").html("&nbsp;").appendTo(tr);
        for (var j = 0; j < columns.length; j++) {
          row.columns.push($("<td />").html("&nbsp;").appendTo(tr));
        }
        row.key = th;
        row.tr = tr;
        rows.push(row);
      }
      return rows;
    }

    // Once we receive meta-data about the grid from the servier, 
    // we create the grid, set height of scrollbar and register 
    // scroll event to update the data on change
    function initialize(meta) {
      var rowHeight = $(id + " tbody tr").height() - 1; // Magic constant
      var thHeight = $(id + " thead tr").height() + 2; // Magic constant 
      var totalRows = meta.rows;
      var viewCount = Math.ceil((viewHeight - thHeight) / rowHeight - 1);
      var tableHeight = rowHeight * Math.min(viewCount, totalRows);

      // Resize and report new size to FSI container (if defined)
      $(id + " .spacer").css("min-height", (rowHeight * totalRows) + "px");
      $(id).height(tableHeight + thHeight);
      $(id + " .scroller").css("margin-top", thHeight + "px");
      $(id + " .scroller").height(tableHeight);
      if (window.fsiResizeContent) window.fsiResizeContent();

      // Create table rows of the view
      var rows = createRows(viewCount, meta.columns);
      
      // Update that gets triggered once the current one is done
      var nextUpdate = null;

      // Update the displayed data on scroll
      function update(offset) {
        nextUpdate = offset;
        for (var i = 0; i < viewCount; i++) {
          rows[i].tr.show();
          rows[i].key.html("(" + (offset + i) + ")").addClass("faded");
          for (var j = 0; j < rows[i].columns.length; j++)
            rows[i].columns[j].html("(...)").addClass("faded");
        }

        $.ajax({ url: serviceUrl + "/rows/" + offset + "?count=" + viewCount }).done(function (res) {
          var data = JSON.parse(res);
          for (var i = 0; i < viewCount; i++) {
            var point = data[i];
            if (point == null) rows[i].tr.hide();
            else {
              rows[i].tr.show();
              rows[i].key.removeClass("faded").html(point.key);
              for (var j = 0; j < rows[i].columns.length; j++)
                rows[i].columns[j].removeClass("faded").html(point.columns[j]);
            }
          }
          if (nextUpdate != null && nextUpdate != offset) {
            console.log("Next: {0}", nextUpdate);
            update(nextUpdate);
          }
          nextUpdate = null;
        });
      }

      // Setup scroll handler & call to load first block of data
      $(id + " .scroller").on("scroll", function () {
        var offset = Math.ceil($(id + " .scroller").scrollTop() / rowHeight);
        if (nextUpdate == null)
          update(offset);
        else
          nextUpdate = offset;
      });
      update(0);
    }

    $.ajax({ url: serviceUrl + "/metadata" }).done(function (res) {
      initialize(JSON.parse(res));
    });
  }"""

/// JavaScript that calls the function defined in `gridLiveScript` for a given grid
let gridLiveScriptCustom id height serviceUrl =
  sprintf "<script type=\"text/javascript\">
      $(function () { setupGrid(\"#%s\", %d, \"%s\"); });
    </script>" id height serviceUrl

/// Default body for live loaded frames 
let gridLiveBody id = "<div class=\"grid live-grid\" id=\"" + id + """">
  <table>
    <thead>
      <tr class="head"><th>#</th><th>&nbsp;</th></tr>
    </thead>
    <tbody class="body">
      <tr><th>&nbsp;</th><td>&nbsp;</td></tr>
    </tbody>
  </table>
  <div class="scroller">
    <div class="spacer"></div>
  </div>
</div>
"""

// --------------------------------------------------------------------------------------
// Implementation of the grid formatting
// --------------------------------------------------------------------------------------

let (|Float|_|) (v:obj) = if v :? float then Some(v :?> float) else None
let (|Float32|_|) (v:obj) = if v :? float32 then Some(v :?> float32) else None

/// Format value as a single-literal paragraph
let formatValue (floatFormat:string) def = function
  | OptionalValue.Present(Float v) -> v.ToString(floatFormat)
  | OptionalValue.Present(Float32 v) -> v.ToString(floatFormat)
  | OptionalValue.Present(v) -> v.ToString()
  | _ -> def

/// Returns unique ID
let nextGridId =
  let counter = ref 0
  let pid = System.Diagnostics.Process.GetCurrentProcess().Id
  fun () -> incr counter; sprintf "fslab-grid-%d-%d" pid counter.Value



// Formatting in offline mode
let mapSteps (startCount, endCount) g input =
  input
  |> Seq.startAndEnd startCount endCount
  |> Seq.map (function Choice1Of3 v | Choice3Of3 v -> g (Some v) | _ -> g None)
  |> List.ofSeq

let mapStepsIndexed (startCount, endCount) g count =
  if count >= startCount + endCount then
    [ for i in 1 .. startCount do yield g(Some i)
      yield g None
      for i in count-endCount .. count-1 do yield g(Some i) ]
  else
    [ for i in 0 .. count-1 do yield g(Some i) ]

let registerStandaloneGrid colKeys rowCount getRow =
  let id = nextGridId()
  let frows = Styles.getNumberRange "grid-row-counts"
  let fcols = Styles.getNumberRange "grid-column-counts"

  // Generate the table head
  let sb = System.Text.StringBuilder()
  sb.Append("<table><thead><tr class=\"head\"><th>#</th>") |> ignore
  colKeys |> mapSteps fcols (function
    | Some (k:string) -> sb.Append("<th>").Append(k).Append("</th>")
    | _ -> sb.Append("<th>...</th>")) |> ignore
  sb.Append("</tr></thead><tbody>") |> ignore

  let rows =
    rowCount |> mapStepsIndexed frows (fun rowIndex ->
      // Get row or generate row consisting of ... for all columns
      let rowKey, row =
        match rowIndex with
        | Some i -> getRow i
        | _ -> box "...", colKeys |> Array.map (fun _ -> "...")

      // Generate row, using ... for one column if there are too many
      sb.Append("<tr><th>" + rowKey.ToString() + "</th>") |> ignore
      colKeys.Length |> mapStepsIndexed fcols (function
        | None -> sb.Append("<td>...</td>")
        | Some i -> sb.Append("<td>" + row.[i] + "</td>")) |> ignore
      sb.Append("</tr>") )
  
  let table = sb.Append("</tbody></table>").ToString()
  seq [ "style", Styles.replaceStyles gridStyles ],
  "<div class=\"grid\" id=\"" + id + "\">" + table + "</div>"



// Background server for live grids
type GridJson = JsonProvider<"""{
    "metadata":{"columns":["Foo","Bar"], "rows":100},
    "row":{"key":"Foo","columns":["Foo","Bar"]}
  }""">

let registerLiveGrid colKeys rowCount getRow =
  let metadata = GridJson.Metadata(colKeys, rowCount).ToString()
  let app =
    Writers.setHeader  "Access-Control-Allow-Origin" "*" >=>
    Writers.setHeader "Access-Control-Allow-Headers" "content-type" >=>
    choose [
      Filters.pathScan "/%d/metadata" (fun _ ->
          Successful.OK (metadata) )
      Filters.pathScan "/%d/rows/%d" (fun (_, row) -> request (fun r ->
          let count = int (Utils.Choice.orDefault "100" (r.queryParam("count")))
          let count = min rowCount (row + count) - row
          let rows =
            Array.init count (fun i ->
              let row = row + i
              let key, cols = getRow row
              GridJson.Row(string key, cols).JsonValue)
          JsonValue.Array(rows).ToString()
          |> Successful.OK ))
      Filters.pathScan "/%d/%s" (fun (n, _) ctx ->
          let url = ctx.request.url.ToString().Replace("/" + string n + "/", "/")
          let ctx =
            { ctx with
                request = { ctx.request with url = System.Uri(url) }
                runtime = { ctx.runtime with homeDirectory = __SOURCE_DIRECTORY__ } }
          Files.browseHome ctx)
    ]
  let url = Server.instance.Value.AddPart(app)
  let id = nextGridId()
  seq [ "style", Styles.replaceStyles gridStyles;
        "script", gridLiveScript
        "script", gridLiveScriptCustom id 500 url ], 
  gridLiveBody id


// --------------------------------------------------------------------------------------
// Register printers using the fsi object
// --------------------------------------------------------------------------------------

type ISeriesOperation<'R> =
  abstract Invoke<'K, 'V when 'K : equality> : Series<'K, 'V> -> 'R

let (|Series|_|) (value:obj) =
  value.GetType()
  |> Seq.unfold (fun t -> if t = null then None else Some(t, t.BaseType))
  |> Seq.tryFind (fun t -> t.Name = "Series`2")
  |> Option.map (fun t -> t.GetGenericArguments())

let invokeSeriesOperation tys obj (op:ISeriesOperation<'R>) =
  typeof<ISeriesOperation<'R>>.GetMethod("Invoke")
    .MakeGenericMethod(tys).Invoke(op, [| obj |]) :?> 'R

let registerFormattable (obj:IFsiFormattable) =
  let floatFormat = Styles.getStyle "float-format"
  let registerGrid =
    if Styles.standaloneHtmlOutput() then registerStandaloneGrid
    else registerLiveGrid

  match obj with
  | Series tys ->
    { new ISeriesOperation<_> with
        member x.Invoke(s) =
          let colKeys = [| "Value" |]
          let rowCount = s.KeyCount
          let getRow index =
            box (s.GetKeyAt(index)),
            [| formatValue floatFormat "N/A" (s.TryGetAt(index)) |]
          registerGrid colKeys rowCount getRow }
    |> invokeSeriesOperation tys obj
  | :? IFrame as f ->
    { new IFrameOperation<_> with
        member x.Invoke(df) =
          let colKeys = df.ColumnKeys |> Seq.map (box >> string) |> Array.ofSeq
          let rowCount = df.RowCount
          let getRow index =
            box (df.GetRowKeyAt(int64 index)),
            df.GetRowAt(index).Vector.DataSequence
              |> Seq.map (formatValue floatFormat "N/A")
              |> Array.ofSeq
          registerGrid colKeys rowCount getRow }
    |> f.Apply
  | _ -> Seq.empty, "(Error: Deedle object implements IFsiFormattable, but it's not a frame or series)"

fsi.AddHtmlPrinter(fun (obj:Deedle.Internal.IFsiFormattable) ->
  registerFormattable obj)

