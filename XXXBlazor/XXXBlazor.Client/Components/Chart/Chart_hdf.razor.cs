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

        [Parameter]
        public Dictionary<string, bool>? NewLegendSetting { get; set; } = new Dictionary<string, bool>();
        protected Dictionary<string, bool>? DisplayLegendSetting { get; set; } = new Dictionary<string, bool>();

        protected bool isDataChanged = false;
        protected bool isDoneFirstRender = false;
        protected bool isChartVisible = false;
        protected bool isLengendVisible = false;
        protected bool IsLegendChanged = false;
        protected int chartRenderKey = 0;

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
                if ( !DataTableCompare.AreEqual(DisplayData, NewData) )
                {
                    isDataChanged = true;
                    isChartVisible = false;

                    DisplayData = (NewData != null) ? NewData.Copy() : new DataTable();

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
            //bool needRender = isDataChanged || !isDoneFirstRender || (!isChartVisible && DisplayData != null) || IsLegendChanged;
            //return needRender;

            bool needRender = isDataChanged || IsLegendChanged;
            return needRender;
        }

        protected object GetDataValue(DataRow row, DataColumn col)
        {
            var datasetData = (DatasetData)row[col.ColumnName];
            return datasetData.Data;
        }

    }
}
