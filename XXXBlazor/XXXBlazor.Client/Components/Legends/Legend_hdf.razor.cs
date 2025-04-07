using Microsoft.AspNetCore.Components;
using System.Data;
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5LegendBase : ComponentBase
    {
        [Parameter]
        public DataTable? NewData { get; set; }
        protected DataTable? DisplayData;
        protected bool isDataChanged = false;
        protected bool isButtonClicked = false;


        protected bool isVisiblePopupSelectSeries { get; set; }

        [Parameter]
        public  Dictionary<string, bool> ShowSeries { get; set; } = new Dictionary<string, bool>();
        protected Dictionary<string, bool> TempSelection = new Dictionary<string, bool>();

        [Parameter]
        public EventCallback<Dictionary<string, bool>> ShowSeriesChanged { get; set; }

        protected override void OnInitialized()
        {
            InitializeShowSeries();
        }

        private void InitializeShowSeries()
        {
            // DisplayData가 null이면 초기화 불필요
            if (DisplayData == null) return;

            // 기존 ShowSeries 값 초기화
            ShowSeries.Clear();

            // 모든 컬럼에 대해 기본값으로 초기화
            foreach (DataColumn col in DisplayData.Columns)
            {
                if (col.ColumnName != null)
                    ShowSeries[col.ColumnName] = false; // 인덱서 사용으로 Add 대신 사용
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if(!DataTableCompare.AreEqual(DisplayData, NewData))
            {
                isDataChanged = true;
                DisplayData = NewData.Copy();

                InitializeShowSeries();

                if ( ShowSeries != null )
                    await ShowSeriesChanged.InvokeAsync(ShowSeries);
            }
            else
            {
                isDataChanged = false;
            }
        }

        protected void CheckedChanged(string seriesName, bool bMode)
        {
            if (TempSelection != null && seriesName != null)
                TempSelection[seriesName] = bMode;
        }

        protected override bool ShouldRender()
        {
            bool ShouldRender = isDataChanged || isButtonClicked;

            return ShouldRender;
        }

        protected async Task OnOK()
        {
            SetShowSeries(true);
            isButtonClicked = true;

            await ShowSeriesChanged.InvokeAsync(ShowSeries);

            isVisiblePopupSelectSeries = false;
            isButtonClicked = false;
        }

        protected void OnClose()
        {
            Console.WriteLine("OnClose()");
            //SetShowSeries(false);
            isVisiblePopupSelectSeries = false;
            isButtonClicked = true;
            StateHasChanged();
            isButtonClicked = false;
        }

        protected void OpenSelectSeriesPopup()
        {
            if (DisplayData == null || DisplayData.Columns.Count == 0) return;

            InitTempSelection();

            isVisiblePopupSelectSeries = true;
            isButtonClicked = true;
            StateHasChanged();
            isButtonClicked = false;
        }

        private void InitTempSelection()
        {
            TempSelection.Clear();

            if (ShowSeries == null) return;

            foreach (var item in ShowSeries)
            {
                //TempSelection[item.Key] = item.Value;
                TempSelection.Add(item.Key, item.Value);
            }
        }

        private void SetShowSeries(bool bMode = true)
        {
            //if(bMode)
            if (bMode && TempSelection != null && ShowSeries != null)
            {
                foreach (var item in TempSelection)
                {
                    ShowSeries[item.Key] = item.Value;
                }
            }
        }
    }
}
