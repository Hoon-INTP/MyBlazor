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

        protected IEnumerable<DatasetData>? TestData = null;

        protected override async Task OnParametersSetAsync()
        {
            if ( OldDisplayData != DisplayData )
            {
                needRender = true;
                counter = 0;
                OldDisplayData = DisplayData;
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

        protected void PrintField()
        {
            foreach (DataColumn col in DisplayData.Columns)
            {
                foreach ( DataRow row in DisplayData.Rows )
                {
                    Console.WriteLine($"ArgField: {DisplayData.Rows.IndexOf(row) + 1} ValField: {(DatasetData)row[col.ColumnName]}");
                }
            }
        }
    }
}
