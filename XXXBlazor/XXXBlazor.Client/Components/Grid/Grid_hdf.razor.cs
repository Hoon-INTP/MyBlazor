using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5GridBase : ComponentBase
    {
        [Parameter]
        public Hdf5TreeNode? selectedNode { get; set; } = null;

        protected bool IsDataLoading = false;

        protected List<DatasetData>? NodeData { get; set; }

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
                    await LoadNodeData(selectedNode);
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

        private async Task LoadNodeData(Hdf5TreeNode SelNode)
        {
            try
            {
                Console.WriteLine("LoadNodeData 시작");
                IsDataLoading = true;
                if (SelNode != null)
                {
                    if (SelNode.NodeType == Hdf5NodeType.Group)
                    {
                        // 데이터셋을 리스트에 저장 후 각 데이터셋을 전부 Load
                    }
                    else if (SelNode.NodeType == Hdf5NodeType.Dataset)
                    {
                        await Task.Run(() =>
                        {
                            var dataList = new List<DatasetData>();

                            if(SelNode.Data is double[] doubleArray)
                            {
                                foreach (var val in doubleArray)
                                {
                                    dataList.Add(new DatasetData { Name = SelNode.Name, Data = val });
                                }
                            }
                            else if(SelNode.Data is int[] intArray)
                            {
                                foreach (var val in intArray)
                                {
                                    dataList.Add(new DatasetData { Name = SelNode.Name, Data = val });
                                }
                            }
                            else if(SelNode.Data is string[] stringArray)
                            {
                                foreach (var val in stringArray)
                                {
                                    dataList.Add(new DatasetData { Name = SelNode.Name, Data = val });
                                }
                            }
                            else
                            {
                                throw new Exception("Not supported data type");
                            }

                            NodeData = dataList;
                        });
                    }
                }
            }
            catch
            {

            }
            finally
            {
                IsDataLoading = false;
                Console.WriteLine("LoadNodeData 종료");
            }
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
