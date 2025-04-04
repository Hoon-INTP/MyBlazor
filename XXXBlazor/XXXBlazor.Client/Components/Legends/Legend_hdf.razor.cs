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

        [Parameter]
        public bool isVisiblePopupSelectSeries { get; set; }
        protected Dictionary<string, bool> ShowSeries = new Dictionary<string, bool>();
        protected Dictionary<string, bool> TempSelection = new Dictionary<string, bool>();

        protected override void OnParametersSet()
        {
            if(!DataTableCompare.AreEqual(DisplayData, NewData))
            {
                isDataChanged = true;
                DisplayData = NewData.Copy();

                ShowSeries.Clear();

                foreach(DataColumn col in DisplayData.Columns)
                {
                   ShowSeries.Add(col.ColumnName, false);
                }
            }
            else
            {
                isDataChanged = false;
            }
        }

        protected void CheckedChanged(string seriesName, bool bMode)
        {
            ShowSeries[seriesName] = bMode;
        }

        protected override bool ShouldRender()
        {
            bool ShouldRender = isDataChanged;

            return ShouldRender;
        }

        protected void OnOK()
        {
            SetShowSeries(true);
            isVisiblePopupSelectSeries = false;
        }

        protected void OnClose()
        {
            Console.WriteLine("OnClose()");
            //SetShowSeries(false);
            isVisiblePopupSelectSeries = false;
        }

        private void InitTempSelection()
        {
            TempSelection.Clear();

            foreach (var item in ShowSeries)
            {
                TempSelection.Add(item.Key, false);
            }
        }

        private void SetShowSeries(bool bMode = true)
        {
            if(bMode)
            {
                foreach (var item in TempSelection)
                {
                    if (item.Value == false)
                    {
                        ShowSeries[item.Key] = item.Value;
                    }
                }
            }

            InitTempSelection();
        }
    }
}
