using Microsoft.AspNetCore.Components;
using System.Data;
using System.Diagnostics;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5GridBase : ComponentBase
    {
        [Parameter]
        public DataTable? DisplayData { get; set; }
        private DataTable? OldDisplayData { get; set; }

        protected bool needRender = false;

        protected bool IsDataLoading => needRender;

        private Stopwatch renderTimer = new Stopwatch();

        static int counter = 0;

        protected override async Task OnParametersSetAsync()
        {
            if ( !DataTableCompare.AreEqual(OldDisplayData, DisplayData) )
            {
                needRender = true;
                counter = 0;
                OldDisplayData = DisplayData.Copy();
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
            //Console.WriteLine($"Grid Render Total Cnt[{counter}] Time: {renderTimer.ElapsedMilliseconds} ms");
        }

        protected override bool ShouldRender()
        {
            return needRender;
        }
    }
}
