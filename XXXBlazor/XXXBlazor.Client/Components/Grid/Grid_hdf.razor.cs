using Microsoft.AspNetCore.Components;
using System.Data;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5GridBase : ComponentBase
    {
        [Parameter]
        public DataTable? DisplayData { get; set; }
        private DataTable? OldDisplayData { get; set; }

        protected bool needRender = false;
        protected bool IsDataLoading => needRender;

        protected override async Task OnParametersSetAsync()
        {
            //Console.WriteLine("Grid ParamSetting");
            if ( !DataTableCompare.AreEqual(OldDisplayData, DisplayData) )
            {
                needRender = true;
                OldDisplayData = DisplayData.Copy();
            }
            else
            {
                needRender = false;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {

        }

        protected override bool ShouldRender()
        {
            return needRender;
        }
    }
}
