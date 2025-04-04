using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;
using System.Data;
using System.Diagnostics;
using DevExpress.Blazor;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5ChartBase : ComponentBase
    {
        [Parameter]
        public DataTable? NewData { get; set; }
        protected DataTable? DisplayData;

        protected bool isDataChanged = false;
        protected bool isDoneFirstRender = false;
        protected bool isChartVisible = false;
        protected bool isLengendVisible = false;
        protected bool isSeriesChanged = false;
        protected int chartRenderKey = 0;

        protected Dictionary<string, bool> ShowSeries = new Dictionary<string, bool>();

        protected bool isVisiblePopupSelectSeries { get; set; }

        //필수?
        private SemaphoreSlim _dataProcessingSemaphore = new SemaphoreSlim(1, 1);

        protected override async Task OnInitializedAsync()
        {
            isChartVisible = (DisplayData != null);
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            await _dataProcessingSemaphore.WaitAsync();

            try
            {
                bool _isDataChanged = false;

                _isDataChanged = !DataTableCompare.AreEqual(DisplayData, NewData);

                if ( _isDataChanged )
                {
                    ShowSeries.Clear();
                    isDataChanged = true;
                    isChartVisible = false;


                    DisplayData = NewData != null ? NewData.Copy() : null;



                    foreach(DataColumn col in DisplayData.Columns)
                    {
                        ShowSeries.Add(col.ColumnName, false);
                    }

                    isSeriesChanged = true;


                    chartRenderKey++;

                    isChartVisible = true;
                }
                else
                {
                    isDataChanged = false;

                    if (!isChartVisible && DisplayData != null)
                    {
                        isChartVisible = true;
                        StateHasChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnParametersSetAsync Error: {ex.Message}");
            }
            finally
            {
                _dataProcessingSemaphore.Release();
            }

        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                isDoneFirstRender = true;
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        protected override bool ShouldRender()
        {
            bool needRender = isDataChanged || !isDoneFirstRender || (!isChartVisible && DisplayData != null) || isSeriesChanged;
            return needRender;
        }

        protected object GetDataValue(DataRow row, DataColumn col)
        {
            var datasetData = (DatasetData)row[col.ColumnName];
            return datasetData.Data;
        }

        protected void CheckedChanged(string seriesName, bool bMode)
        {
            ShowSeries[seriesName] = bMode;
        }

    }
}
