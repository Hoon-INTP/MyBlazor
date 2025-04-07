using System.Data;
using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using DevExpress.Blazor;
using XXXBlazor.Client.Services;
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Pages
{
    public class XXXViewerBase : ComponentBase, IDisposable
    {
        [Inject] protected IHdf5FileReader iHdf5Reader { get; set; }
        [Inject] protected IJSRuntime JSRuntime { get; set; }

        protected const int MaxFileSize = 15 * 1024 * 1024;

        protected bool isLoading = false;
        protected string? errorMessage = null;

        protected int processedChunks = 0;
        protected int totalChunks = 0;
        private DotNetObjectReference<XXXViewer>? dotNetObjectRef;
        private ConcurrentDictionary<int, byte[]> processedData = new ConcurrentDictionary<int, byte[]>();

        // JS 초기화 여부 추적
        protected bool isJsInitialized = true;
        private int initializationAttempts = 0;
        private const int MAX_INITIALIZATION_ATTEMPTS = 3;

        protected int SelectedFilesCount { get; set; }

        // File Data
        protected Hdf5TreeNode? fileModel;
        protected Hdf5TreeNode? selectedNode;

        // Node Data
        protected List<List<DatasetData>>? nodeData;
        private XXXCache<string, List<List<DatasetData>>> nodeCache = new(20);

        // Display Data
        public DataTable? GridData;
        public DataTable? ChartData;
        public DataTable? LegendData;

        // Legend Data
        public Dictionary<string, bool> SeriesSettings { get; set; }

        //protected override async Task OnAfterRenderAsync(bool firstRender)
        //{
        //    if (firstRender)
        //    {
        //        await InitializeJavaScript();
        //    }
        //}

        private async Task InitializeJavaScript()
        {
            if (isJsInitialized || initializationAttempts >= MAX_INITIALIZATION_ATTEMPTS)
                return;

            try
            {
                initializationAttempts++;

                dotNetObjectRef = DotNetObjectReference.Create((XXXViewer)this);
                await JSRuntime.InvokeVoidAsync("initializeWorkers", dotNetObjectRef);

                var jsInlineCheck = await JSRuntime.InvokeAsync<bool>("window.hasOwnProperty", "processChunk");
                if (!jsInlineCheck)
                {
                    throw new Exception("JavaScript 메서드가 제대로 초기화되지 않았습니다.");
                }

                isJsInitialized = true;
                errorMessage = null;
            }
            catch (Exception ex)
            {
                errorMessage = $"JavaScript 초기화 오류 ({initializationAttempts}/{MAX_INITIALIZATION_ATTEMPTS}): {ex.Message}";

                if (initializationAttempts < MAX_INITIALIZATION_ATTEMPTS)
                {
                    await Task.Delay(1000);
                    await InitializeJavaScript();
                }
            }
            finally
            {
                StateHasChanged();
            }
        }

        protected async Task RetryInitialize()
        {
            initializationAttempts = 0;
            await InitializeJavaScript();
        }

        protected async void SelectedFilesChanged(IEnumerable<UploadFileInfo> files)
        {
            SelectedFilesCount = files.ToList().Count();

            if(SelectedFilesCount == 0)
            {
                errorMessage = null;
                fileModel = null;
                selectedNode = null;

                LegendData = GridData = ChartData = new DataTable().Copy();
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnFilesUploading(FilesUploadingEventArgs args)
        {
            try
            {
                /* // JS가 초기화되지 않았다면 작업 중단
                if (!isJsInitialized)
                {
                    errorMessage = "JavaScript가 아직 초기화되지 않았습니다. 잠시 후 다시 시도해주세요.";
                    return;
                }
                */

                fileModel = null;
                selectedNode = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                errorMessage = null;
                isLoading = true;
                processedChunks = 0;
                processedData.Clear();
                StateHasChanged();

                var file = args.Files[0];

                if (file == null)
                {
                    errorMessage = "Select a file first.";
                    return;
                }

                if (!file.Name.EndsWith(".h5") && !file.Name.EndsWith(".hdf5"))
                {
                    errorMessage = "Invalid file format. Please select an HDF5 file.";
                    return;
                }

                string fileName = file.Name;

                var stopwatch = Stopwatch.StartNew();

                using (var memoryStream = new MemoryStream())
                {
                    using (var fileStream = file.OpenReadStream(maxAllowedSize: 500 * 1024 * 1024))
                    {
                        if(file.Size > 5 * 1024 * 1024)
                        {
                            int chunkSize1 = 64 * 1024;
                            byte[] buffer = new byte[chunkSize1];
                            int bytesRead;
                            long totalBytesRead = 0;

                            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await memoryStream.WriteAsync(buffer, 0, bytesRead);
                                totalBytesRead += bytesRead;

                                if (totalBytesRead % (4 * chunkSize1) == 0)
                                {
                                    await Task.Yield();
                                }
                            }
                        }
                        else
                        {
                            //Console.WriteLine("under 5MB");
                            await fileStream.CopyToAsync(memoryStream);
                        }
                    }

                    memoryStream.Position = 0;

                    stopwatch.Stop();
                    //Console.WriteLine($"[Elapsed Time: {stopwatch.ElapsedMilliseconds}ms]");

                    await Task.Run(() => fileModel = iHdf5Reader.LoadFromStream(memoryStream, fileName));

                    selectedNode = fileModel;
                }

                //Console.WriteLine($"{fileModel.ToTreeString()}");

                /*
                // JSInterop 의 WebWorker로 백그라운드 Read
                // 청크 처리 설정
                int chunkSize = 16 * 1024;
                long fileSize = fileData.Length;
                totalChunks = (int)Math.Ceiling((double)fileSize / chunkSize);

                var tasks = new List<Task>();

                int maxParallelism = Math.Max(1, Environment.ProcessorCount - 1);
                using var semaphore = new SemaphoreSlim(maxParallelism);

                for (int i = 0; i < totalChunks; i++)
                {
                    int chunkIndex = i;
                    long start = chunkIndex * chunkSize;
                    long end = Math.Min(start + chunkSize, fileSize);

                    tasks.Add(Task.Run(async () => {
                        try
                        {
                            await semaphore.WaitAsync();

                            // 메모리에서 직접 청크 추출
                            int length = (int)(end - start);
                            byte[] buffer = new byte[length];
                            Buffer.BlockCopy(fileData, (int)start, buffer, 0, length);

                            // JavaScript 워커에게 처리 요청
                            await JSRuntime.InvokeVoidAsync("processChunk", chunkIndex, buffer);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                // 타임아웃 추가
                int timeoutSeconds = 30;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                try
                {
                    while (processedChunks < totalChunks)
                    {
                        if (cts.Token.IsCancellationRequested)
                            throw new TimeoutException($"처리 시간이 {timeoutSeconds}초를 초과했습니다.");

                        await Task.Delay(100, cts.Token);
                        if (processedChunks % 10 == 0)
                            StateHasChanged(); // 진행 상태 업데이트
                    }
                }
                catch (TaskCanceledException)
                {
                    throw new TimeoutException($"처리 시간이 {timeoutSeconds}초를 초과했습니다.");
                }

                byte[] completeData = CombineChunks();
                errorMessage = $"파일 '{file.Name}' ({fileSize:N0} 바이트)을 성공적으로 처리했습니다.";

                */
            }
            catch (Exception exception)
            {
                errorMessage = $"Error occurs during file loading: {exception.Message}";
                //Console.WriteLine($"Error during file processing: {exception}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected Stopwatch stopwatch = new Stopwatch();

        protected async Task HandleNodeClicked(Hdf5TreeNode node)
        {
            selectedNode = node;

            nodeData = await LoadNodeData(node);

            GridData = ChartData = ConvertToDataTable(nodeData).Copy();

            LegendData = GridData.Copy();
        }

        private async Task<List<List<DatasetData>>> LoadNodeData(Hdf5TreeNode SelNode)
        {
            if (nodeCache.TryGet(SelNode.Path, out var cachedData))
            {
                //Console.WriteLine("캐시 로드 됨");
                return cachedData;
            }

            List<List<DatasetData>> NodeData = new List<List<DatasetData>>();

            try
            {
                if (SelNode != null)
                {
                    if (SelNode.NodeType == Hdf5NodeType.Group)
                    {
                        await Task.Run(() =>
                        {
                            Parallel.ForEach(SelNode.Children, child =>
                            {
                                var dataList = new List<DatasetData>();

                                if (child.NodeType == Hdf5NodeType.Dataset)
                                {
                                    if (child.Data is double[] doubleArray)
                                    {
                                        foreach (var val in doubleArray)
                                        {
                                            dataList.Add(new DatasetData { Name = child.Name, Data = val });
                                        }
                                    }
                                    else if (child.Data is int[] intArray)
                                    {
                                        foreach (var val in intArray)
                                        {
                                            dataList.Add(new DatasetData { Name = child.Name, Data = val });
                                        }
                                    }
                                    else if (child.Data is string[] stringArray)
                                    {
                                        foreach (var val in stringArray)
                                        {
                                            dataList.Add(new DatasetData { Name = child.Name, Data = val });
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Not supported data type");
                                    }

                                    lock (NodeData)
                                    {
                                        NodeData.Add(dataList);
                                    }
                                }
                            });
                        });
                    }
                    else if (SelNode.NodeType == Hdf5NodeType.Dataset)
                    {
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

                            NodeData.Add(dataList);
                        }
                    }
                }
            }
            catch
            {

            }
            finally
            {
                nodeCache.Set(SelNode.Path, NodeData);
            }

            return NodeData;
        }

        private DataTable ConvertToDataTable(List<List<DatasetData>> nodeData)
        {
            var dt = new DataTable();

            int RowCnt, ColCnt;

            if ( 0 == nodeData.Count ) return dt;

            RowCnt = nodeData.Count;

            if ( 0 == nodeData[0].Count ) return dt;

            ColCnt = nodeData[0].Count;

            string[] ColNames = new string[RowCnt];
            for (int i = 0; i < RowCnt; i++)
            {
                ColNames[i] = nodeData[i][0].Name;
                dt.Columns.Add(ColNames[i], typeof(DatasetData));
            }

            dt.BeginLoadData();

            for(int i = 0; i < ColCnt; i++)
            {
                DataRow row = dt.NewRow();
                for(int j = 0; j < RowCnt; j++)
                {
                    row[$"{ColNames[j]}"] = nodeData[j][i];
                }

                dt.Rows.Add(row);
            }

            dt.EndLoadData();

            return dt;
        }

        private void ClearCache()
        {
            nodeCache.Clear();
        }

        protected void OnSeriesSettingsChanged(Dictionary<string, bool> updatedVSettings)
        {
            SeriesSettings.Clear();
            foreach (var kvp in updatedVSettings)
            {
                SeriesSettings.Add(kvp.Key, kvp.Value);
            }
        }

        [JSInvokable]
        public void OnChunkProcessed(int chunkIndex, byte[] _processedData)
        {
            try
            {
                processedData[chunkIndex] = _processedData;
                processedChunks++;

                if (processedChunks % 10 == 0 || processedChunks == totalChunks)
                {
                    InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnChunkProcessed Error: {ex.Message}");
            }
        }

        protected byte[] CombineChunks()
        {
            int totalLength = processedData.Values.Sum(chunk => chunk.Length);
            byte[] result = new byte[totalLength];
            int offset = 0;

            foreach (var chunk in processedData.OrderBy(kvp => kvp.Key))
            {
                Buffer.BlockCopy(chunk.Value, 0, result, offset, chunk.Value.Length);
                offset += chunk.Value.Length;
            }

            return result;
        }

        public void Dispose()
        {
            try
            {
                if (isJsInitialized && dotNetObjectRef != null)
                {
                    JSRuntime.InvokeVoidAsync("disposeWorkers");
                }

                fileModel = null;
                selectedNode = null;
                ClearCache();

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch
            {

            }
            finally
            {
                dotNetObjectRef?.Dispose();
            }
        }
    }
}
