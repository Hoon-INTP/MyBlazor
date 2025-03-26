using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;
using DevExpress.Blazor;
using System.Data;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.XtraEditors.Filtering;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5GridBase : ComponentBase
    {
        [Parameter]
        public List<List<DatasetData>>? NodeData { get; set; } = null;

        protected DataTable convertedData;

        protected bool IsDataLoading = false;

        //protected List<DatasetData>? NodeData { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if(NodeData != null)
            {
                await Task.Run(() => convertedData = ConvertToDataTable(NodeData));
            }
        }

        private DataTable ConvertToDataTable(List<List<DatasetData>> nodeData)
        {
            var dt = new DataTable();

            for(int j = 0; j < nodeData.Count; j++)
            {
                dt.Columns.Add($"Data_{j}", typeof(DatasetData));
            }

            int i = 0;
            while(i < nodeData.Count)
            {
                DataRow row = dt.NewRow();
                for(int j = 0; j < nodeData.Count; j++)
                {
                    row[$"Data_{j}"] = nodeData[j][i];
                }

                dt.Rows.Add(row);
                i++;
            }

            return dt;
        }

        protected RenderFragment BuildColumnsGrid()
        {
            List<List<DatasetData>> temp = NodeData;

            RenderFragment columns = b =>
            {
                foreach (var data in temp)
                {
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            b.OpenComponent(0, typeof(DxGridDataColumn));
                            b.AddAttribute(0, "FieldName", item.Data);
                            b.CloseComponent();
                        }
                    }
                }
            };
            return columns;
        }

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void ConsoleData()
        {
            if(NodeData == null)
            {
                Console.WriteLine("NodeData is null");
                return;
            }

            Console.WriteLine($"NodeData [{NodeData[0][0].Name} : {NodeData[0][0].Data}] Count: {NodeData.Count}");

        }

        protected bool bDebugMode = false;
        protected void EnableDebugMode()
        {
            bDebugMode = !bDebugMode;
        }

        protected static string FormatArray(object value) => value switch
        {
            int[] array => string.Join(", ", array),
            double[] array => string.Join(", ", Array.ConvertAll(array, x => x.ToString("F2"))),
            string[] array => string.Join(" | ", array),
            _ => "Not an array"
        };
    }
}
