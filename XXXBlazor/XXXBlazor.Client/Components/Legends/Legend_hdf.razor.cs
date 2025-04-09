using Microsoft.AspNetCore.Components;
using System.Collections.Immutable;
using System.Data;
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5LegendBase : ComponentBase
    {
        [Parameter]
        public List<string>? NewSeriesList { get; set; }
        protected List<string>? ShowSeriesList { get; set; }

        protected bool isDataChanged = false;
        protected bool isButtonClicked = false;

        protected bool isVisiblePopupSelectSeries { get; set; }  = false;

        protected Dictionary<string, bool> OldShowSeries { get; set; } = new Dictionary<string, bool>();
        protected Dictionary<string, bool> TempSelection = new Dictionary<string, bool>();

        [Parameter]
        public EventCallback<Dictionary<string, bool>> ShowSeriesChanged { get; set; }

        protected override void OnInitialized()
        {
            InitializeShowSeries();
        }

        private void InitializeShowSeries()
        {
            if ( null != NewSeriesList )
            {
                OldShowSeries.Clear();

                foreach(string item in NewSeriesList)
                {
                    OldShowSeries[item] = false;
                }
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if(NewSeriesList != ShowSeriesList)
            {
                ShowSeriesList = NewSeriesList?.ToList() ?? new List<string>();
            }


        }

        protected void CheckedChanged(string seriesName, bool bMode)
        {
            TempSelection[seriesName] = bMode;
        }

        protected override bool ShouldRender()
        {
            bool ShouldRender = true;

            return ShouldRender;
        }

        private void InitTempSelection()
        {
            TempSelection = new Dictionary<string, bool>(OldShowSeries);
        }

        protected async Task OnOK()
        {
            bool IsAllFalse = TempSelection.Values.All(v => v == false);

            if ( IsAllFalse )
            {
                if ( ShowSeriesList.Any() )
                {
                    TempSelection[ShowSeriesList[0]] = true;
                }
            }

            OldShowSeries = new Dictionary<string, bool>(TempSelection);

            await ShowSeriesChanged.InvokeAsync(TempSelection);

            isVisiblePopupSelectSeries = false;
        }

        protected void OnClose()
        {
            isVisiblePopupSelectSeries = false;
        }

        protected void OpenSelectSeriesPopup()
        {
            if(null == ShowSeriesList) return;

            InitTempSelection();

            isVisiblePopupSelectSeries = true;
        }

    }
}
