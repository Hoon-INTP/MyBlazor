using HDF.PInvoke;
using Hdf5Viewer.Shared;
using System.Runtime.InteropServices;
using System.Text;

namespace Hdf5Viewer.Server.Services
{
    public unsafe class HDF5Service
    {
        public Hdf5TreeNode GetHdf5TreeStructure(string filePath)
        {
            var rootNode = new Hdf5TreeNode { Name = "Root", Path = "/", Type = "Root" };
            try
            {
                long fileId = H5F.open(filePath, H5F.ACC_RDONLY);
                if (fileId >= 0)
                {
                    BuildTree(fileId, "/", rootNode);
                    H5F.close(fileId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building HDF5 tree: {ex.Message}");
            }
            return rootNode;
        }

        private unsafe void BuildTree(long fileId, string groupPath, Hdf5TreeNode parentNode)
        {
            long groupId = H5G.open(fileId, groupPath);
            if (groupId < 0) return;

            H5O.info_t objectInfo = new H5O.info_t();
            H5O.get_info(groupId, ref objectInfo);

            for (ulong i = 0; i < objectInfo.num_attrs; ++i)
            {
                byte[] nameBuffer = new byte[1024];
                byte[] dotBytes = System.Text.Encoding.ASCII.GetBytes(".");
                IntPtr nameSize = H5L.get_name_by_idx(
                groupId, dotBytes, H5.index_t.NAME, H5.iter_order_t.INC, i,
                nameBuffer, nameBuffer.Length, H5P.DEFAULT
                );

                string childName = Encoding.ASCII.GetString(nameBuffer, 0, (int)nameSize);

                string childPath = $"{groupPath}{(groupPath == "/" ? "" : "/")}{childName}";
                var childNode = new Hdf5TreeNode { Name = childName, Path = childPath };

                byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(childName);
                
                H5O.get_info_by_name(groupId, nameBytes, ref objectInfo);
                

                childNode.Type = objectInfo.type switch
                {
                    H5O.type_t.GROUP => "Group",
                    H5O.type_t.DATASET => "Dataset",
                    H5O.type_t.NAMED_DATATYPE => "Datatype",
                    _ => "Unknown"
                };

                if (objectInfo.type == H5O.type_t.GROUP)
                {
                    BuildTree(fileId, childPath, childNode);
                }

                parentNode.Children.Add(childNode);
            }
            H5G.close(groupId);
        }

        public Hdf5TableData GetTableData(string filePath, string datasetPath)
        {
            var tableData = new Hdf5TableData();
            try
            {
                long fileId = H5F.open(filePath, H5F.ACC_RDONLY);
                long datasetId = H5D.open(fileId, datasetPath);
                
                if (datasetId >= 0)
                {
                    ProcessDataset(datasetId, tableData, datasetPath);
                    H5D.close(datasetId);
                }
                H5F.close(fileId);
            }
            catch (Exception ex)
            {
                tableData.RowData.Add(new List<string> { $"Error: {ex.Message}" });
            }
            return tableData;
        }

        private unsafe void ProcessDataset(long datasetId, Hdf5TableData tableData, string datasetPath)
        {
            long spaceId = H5D.get_space(datasetId);
            int rank = H5S.get_simple_extent_ndims(spaceId);

            ulong[] dims = new ulong[rank];
            ulong[] maxDims = new ulong[rank];
            H5S.get_simple_extent_dims(spaceId, dims, maxDims);
            H5S.close(spaceId);

            if (rank > 2)
            {
                tableData.RowData.Add(new List<string> { "Unsupported data rank" });
                return;
            }

            long typeId = H5D.get_type(datasetId);
            H5T.class_t classType = H5T.get_class(typeId);

            // 컬럼 설정
            if (rank == 2)
            {
                for (ulong i = 0; i < dims[1]; i++)
                    tableData.ColumnNames.Add($"Column {i + 1}");
            }
            else
            {
                tableData.ColumnNames.Add(datasetPath.Split('/').Last());
            }

            // 데이터 읽기
            int typeSize = H5T.get_size(typeId).ToInt32();
            byte[] buffer = new byte[(int)(dims[0] * (rank == 2 ? dims[1] : 1) * (ulong)typeSize)];
            
            fixed (byte* ptr = buffer)
            {
                H5D.read(datasetId, typeId, H5S.ALL, H5S.ALL, 0, new IntPtr(ptr));
            }

            ConvertData(tableData, buffer, typeId, dims, rank);
            H5T.close(typeId);
        }

        private unsafe void ConvertData(Hdf5TableData tableData, byte[] buffer, long typeId, ulong[] dims, int rank)
        {
            int typeSize = H5T.get_size(typeId).ToInt32();
            int elements = (int)(dims[0] * (rank == 2 ? dims[1] : 1));

            fixed (byte* ptr = buffer)
            {
                for (int i = 0; i < elements; i++)
                {
                    IntPtr elementPtr = new IntPtr(ptr + i * typeSize);
                    string value = Marshal.PtrToStringAnsi(elementPtr) ?? string.Empty;
                    
                    if (rank == 2)
                    {
                        int row = i / (int)dims[1];
                        int col = i % (int)dims[1];
                        
                        if (col == 0) tableData.RowData.Add(new List<string>());
                        tableData.RowData[row].Add(value);
                    }
                    else
                    {
                        tableData.RowData.Add(new List<string> { value });
                    }
                }
            }
        }
    }
}
