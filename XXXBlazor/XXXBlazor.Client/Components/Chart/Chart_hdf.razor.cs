using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5ChartBase : ComponentBase
    {
        protected bool IsDataLoading = true;
        protected IEnumerable<DatasetData> TestData;


        protected override async Task OnInitializedAsync()
        {
            StateHasChanged();
            List<DatasetData> temp = new List<DatasetData>();

            for(int i = 0; i < 60; i++)
            {
                temp.Add(new DatasetData() { Name = "Test1", Index = i, Data = i*1.1 + 0.3 });
                //await Task.Delay(100);
                Console.WriteLine($"Data Added [{i}]");
            }

            await Task.Delay(100);

            TestData = temp.AsQueryable();

            IsDataLoading = true;
        }
    }
}
