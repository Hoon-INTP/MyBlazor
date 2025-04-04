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

        protected override void OnParametersSet()
        {
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

        protected override bool ShouldRender()
        {
            return needRender;
        }
    }
}
