using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;
using XXXBlazor.Client.Services;


namespace XXXBlazor.Client.Pages
{
    public class Hdf5LegendBase : ComponentBase, IDisposable
    {
        [Inject]
        protected Hdf5StateService StateService { get; set; }

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

            StateService.TreeNodeChanged += HandleTreeNodeChanged;
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

        private async void HandleTreeNodeChanged(Hdf5TreeNode node, List<string> newLegendData)
        {
            InitializeWithFirstSelected(newLegendData);

            await InvokeAsync(StateHasChanged);
        }

        private async void InitializeWithFirstSelected(List<string> newLegendData)
        {
            if (newLegendData != null && newLegendData.Any())
            {
                TempSelection.Clear();
                OldShowSeries.Clear();

                foreach (string item in newLegendData)
                {
                    bool isSelected = item == newLegendData[0];
                    TempSelection[item] = isSelected;
                    OldShowSeries[item] = isSelected;
                }

                await ShowSeriesChanged.InvokeAsync(TempSelection);
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

        protected void OnClickSelectAll()
        {
            var keys = TempSelection.Keys.ToList();
            foreach (var key in keys)
            {
                TempSelection[key] = true;
            }

            StateHasChanged();
        }

        protected void OnClickDeSelectAll()
        {
            var keys = TempSelection.Keys.ToList();
            foreach (var key in keys)
            {
                TempSelection[key] = false;
            }

            TempSelection[ShowSeriesList[0]] = true;

            StateHasChanged();
        }

        protected void OpenSelectSeriesPopup()
        {
            if(null == ShowSeriesList) return;

            InitTempSelection();

            isVisiblePopupSelectSeries = true;
        }

        public void Dispose()
        {
            // 이벤트 구독 해제
            StateService.TreeNodeChanged -= HandleTreeNodeChanged;
        }
    }
}
