using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;
using System.Data;
using System.Diagnostics;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5ChartBase : ComponentBase
    {
        [Parameter]
        public DataTable? DisplayData { get; set; }
        private DataTable? OldDisplayData { get; set; }
        //[Parameter]
        //public List<List<DatasetData>>? DisplayData { get; set; }
        //public List<List<DatasetData>>? OldDisplayData { get; set; }

        protected bool needRender = false;

        private Stopwatch renderTimer = new Stopwatch();

        static int counter = 0;
        static int counter1 = 0;

        protected IEnumerable<DatasetData>? TestData = null;

        protected override async Task OnParametersSetAsync()
        {
            Console.WriteLine($"Chart OnParametersSetAsync");
            counter1++;

            if ( OldDisplayData != DisplayData )
            {
                Console.WriteLine($"Chart OnParametersSetAsync: needRender {counter1}");
                needRender = true;
                counter = 0;
                OldDisplayData = DisplayData;
                Console.WriteLine($"{DisplayData!=null}");
                renderTimer = new Stopwatch();
                renderTimer.Start();
            }
            else
            {
                needRender = false;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            renderTimer.Stop();
            counter++;
            Console.WriteLine($"Chart Render Total Cnt[{counter}] Time: {renderTimer.ElapsedMilliseconds} ms");
        }

        protected override bool ShouldRender()
        {
            return needRender;
        }

        protected object GetDataValue(DataRow row, DataColumn col)
        {
            var datasetData = (DatasetData)row[col.ColumnName];
            return datasetData.Data;
        }


        protected void PrintField()
        {
            StateHasChanged();
            //foreach (DataColumn col in DisplayData.Columns)
            //{
            //    foreach ( DataRow row in DisplayData.Rows )
            //    {
            //        Console.WriteLine($"ArgField: {DisplayData.Rows.IndexOf(row) + 1} ValField: {(DatasetData)row[col.ColumnName]}");
            //    }
            //}
        }
    }
}
