using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;
using DevExpress.Blazor;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5GridBase : ComponentBase
    {
        [Parameter]
        public Hdf5TreeNode? selectedNode { get; set; } = null;

        [Parameter]
        public List<List<DatasetData>>? NodeData { get; set; } = null;

        protected bool IsDataLoading = false;

        //protected List<DatasetData>? NodeData { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if(NodeData != null)
            {
                await Task.Run(() => NodeData.Clear());
            }

            if (selectedNode != null)
            {
                if(selectedNode.NodeType == Hdf5NodeType.Dataset)
                {
                    //await LoadNodeData(selectedNode);
                    Console.WriteLine("Dataset");
                }
                else if(selectedNode.NodeType == Hdf5NodeType.Group)
                {
                    Console.WriteLine("Group");
                }
            }
            else
            {

            }
        }

        protected RenderFragment BuildColumnsGrid()
        {
            List<List<DatasetData>> temp = NodeData;

            RenderFragment columns = b =>
            {
                foreach (var data in temp)
                {
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            b.OpenComponent(0, typeof(DxGridDataColumn));
                            b.AddAttribute(0, "FieldName", item.Data);
                            b.CloseComponent();
                        }
                    }
                }
            };
            return columns;
        }

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void ConsoleData()
        {
            if(selectedNode == null)
            {
                Console.WriteLine("selectedNode is null");
                return;
            }

            if(selectedNode.Data is double[] doubleArray)
            {
                foreach (var val in doubleArray)
                {
                    Console.WriteLine(val);
                }
            }
            else if(selectedNode.Data is int[] intArray)
            {
                foreach (var val in intArray)
                {
                    Console.WriteLine(val);
                }
            }
            else if(selectedNode.Data is string[] stringArray)
            {
                foreach (var val in stringArray)
                {
                    Console.WriteLine(val);
                }
            }
            else
            {
                Console.WriteLine("Not supported data type");
            }
        }

        protected bool bDebugMode = false;
        protected void EnableDebugMode()
        {
            bDebugMode = !bDebugMode;
        }

        protected static string FormatArray(object value) => value switch
        {
            int[] array => string.Join(", ", array),
            double[] array => string.Join(", ", Array.ConvertAll(array, x => x.ToString("F2"))),
            string[] array => string.Join(" | ", array),
            _ => "Not an array"
        };
    }
}
