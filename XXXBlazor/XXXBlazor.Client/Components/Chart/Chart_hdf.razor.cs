using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;
using System.Data;
using System.Diagnostics;
using DevExpress.Blazor;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5ChartBase : ComponentBase
    {
        protected DxChartBase hdfChart;

        [Parameter]
        public DataTable? DisplayData { get; set; }
        protected DataTable? OldDisplayData;

        protected bool isDataChanged = false;
        protected bool isDoneFirstRender = false;
        protected bool isChartVisible = false;
        protected bool isLengendVisible = false;
        protected int chartRenderKey = 0;

        //필수?
        private SemaphoreSlim _dataProcessingSemaphore = new SemaphoreSlim(1, 1);

        protected override async Task OnInitializedAsync()
        {
            // 초기 상태 설정
            isChartVisible = (OldDisplayData != null);
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            await _dataProcessingSemaphore.WaitAsync();

            try
            {
                bool _isDataChanged = false;

                await Task.Run(() =>
                {
                    _isDataChanged = !DataTableCompare.AreEqual(OldDisplayData, DisplayData);
                });

                if ( _isDataChanged )
                {
                    isDataChanged = true;
                    isChartVisible = false;

                    StateHasChanged();
                    await Task.Delay(10);

                    await Task.Run(() =>
                    {
                        OldDisplayData = DisplayData != null ? DisplayData.Copy() : null;
                    });

                    chartRenderKey++;

                    isChartVisible = true;
                }
                else
                {
                    isDataChanged = false;

                    if (!isChartVisible && OldDisplayData != null)
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
            Console.WriteLine("{0}{1:HH:mm:ss.fff}", "O", DateTime.Now);
            await base.OnAfterRenderAsync(firstRender);
        }

        protected override bool ShouldRender()
        {
            bool needRender = isDataChanged || !isDoneFirstRender || (!isChartVisible && OldDisplayData != null);
            Console.WriteLine("{0}{1:HH:mm:ss.fff}",needRender?"O":"X", DateTime.Now);
            return needRender;
        }

        protected object GetDataValue(DataRow row, DataColumn col)
        {
            var datasetData = (DatasetData)row[col.ColumnName];
            return datasetData.Data;
        }


        protected async Task PrintField()
        {
            //await Task.Run(hdfChart.RefreshData);
        }
    }
}
