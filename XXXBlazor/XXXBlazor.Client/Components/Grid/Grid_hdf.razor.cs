using Microsoft.AspNetCore.Components;
using System.Data;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5GridBase : ComponentBase
    {
        [Parameter]
        public DataTable? NewData { get; set; }
        protected DataTable? DisplayData { get; set; }

        [Parameter]
        public Dictionary<string, bool>? NewLegendSetting { get; set; } = new Dictionary<string, bool>();
        protected Dictionary<string, bool>? DisplayLegendSetting { get; set; } = new Dictionary<string, bool>();


        protected bool IsDataLoading => IsDataChanged;

        private bool IsDataChanged = false;
        private bool IsLegendChanged = false;

        protected override void OnParametersSet()
        {
            if ( !DataTableCompare.AreEqual(DisplayData, NewData) )
            {
                IsDataChanged = true;

                DisplayData = (NewData != null) ? NewData.Copy() : new DataTable();
            }
            else
            {
                IsDataChanged = false;
            }

            if ( NewLegendSetting != DisplayLegendSetting )
            {
                DisplayLegendSetting = NewLegendSetting;
                IsLegendChanged = true;
            }
            else
            {
                IsLegendChanged = false;
            }
        }

        protected override bool ShouldRender()
        {
            bool needRender = IsDataChanged || IsLegendChanged;
            return needRender;
        }
    }
}
