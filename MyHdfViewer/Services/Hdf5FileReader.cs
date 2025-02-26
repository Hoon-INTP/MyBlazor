using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;
using MyHdfViewer.Models;

namespace MyHdfViewer.Services
{
    public class Hdf5FileReader : IHdf5FileReader
    {
        /// <summary>
        /// Convert HDF5 File to Hdf5FileModel
        /// </summary>
        /// <param name="filePath">HDF5 File Path</param>
        /// <returns>Hdf5FileModel</returns>
        public Hdf5FileModel ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"파일을 찾을 수 없습니다: {filePath}");

            Console.WriteLine($"Reading HDF5 file: {filePath}");

            long fileId = H5F.open(filePath, H5F.ACC_RDONLY);
            if (fileId < 0)
                throw new Exception($"HDF5 파일을 열 수 없습니다: {filePath}");

            try
            {
                long rootGroupId = H5G.open(fileId, "/");
                if (rootGroupId < 0)
                    throw new Exception("루트 그룹을 열 수 없습니다.");

                Hdf5Group rootGroup = ReadGroup(rootGroupId, "/");
                
                string fileName = Path.GetFileName(filePath);

                var fileModel = new Hdf5FileModel
                {
                    FilePath = filePath,
                    FileName = fileName,
                    RootGroup = rootGroup
                };

                H5G.close(rootGroupId);

                return fileModel;
            }
            finally
            {
                H5F.close(fileId);
            }
        }

        /// <summary>
        /// Convert Group information to Hdf5Group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="groupPath">Group Path</param>
        /// <returns>Hdf5Group</returns>
        private Hdf5Group ReadGroup(long groupId, string groupPath)
        {
            string groupName = groupPath == "/" ? "/" : Path.GetFileName(groupPath);

            var group = new Hdf5Group
            {
                Name = groupName,
                Path = groupPath
            };

            ReadAttributes(groupId, group);

            H5G.info_t groupInfo = new H5G.info_t();
            H5G.get_info(groupId, ref groupInfo);
            ulong memberCount = groupInfo.nlinks;

            for (ulong i = 0; i < memberCount; i++)
            {
                int maxNameSize = 2048;
                StringBuilder nameBuilder = new StringBuilder(maxNameSize);

                byte[] dotBytes = Encoding.ASCII.GetBytes(".");

                H5L.info_t linkInfo = new H5L.info_t();
                long linkType = H5L.get_info_by_idx(groupId, dotBytes, H5.index_t.NAME, H5.iter_order_t.NATIVE, i, ref linkInfo);
                
                if (linkType >= 0)
                {
                    H5L.get_name_by_idx(groupId, ".", H5.index_t.NAME, H5.iter_order_t.NATIVE, i, nameBuilder, new IntPtr(maxNameSize));
                    string memberName = nameBuilder.ToString();
                    string memberPath = groupPath == "/" ? $"/{memberName}" : $"{groupPath}/{memberName}";

                    long objectId = H5O.open(groupId, memberName, H5P.DEFAULT);
                    if (objectId >= 0)
                    {
                        try
                        {
                            H5O.info_t objectInfo = new H5O.info_t();
                            H5O.get_info(objectId, ref objectInfo);
                            var objectType = objectInfo.type;

                            switch (objectType)
                            {
                                case H5O.type_t.GROUP:
                                    Hdf5Group subgroup = ReadGroup(objectId, memberPath);
                                    group.Children.Add(subgroup);
                                    break;

                                case H5O.type_t.DATASET:
                                    Hdf5Dataset dataset = ReadDataset(objectId, memberName, memberPath);
                                    group.Children.Add(dataset);
                                    break;

                                default:
                                    var otherNode = new Hdf5Group
                                    {
                                        Name = memberName,
                                        Path = memberPath,
                                        NodeType = Models.Hdf5NodeType.Other
                                    };
                                    ReadAttributes(objectId, otherNode);
                                    group.AddChild(otherNode);
                                    break;
                            }
                        }
                        finally
                        {
                            H5O.close(objectId);
                        }
                    }
                }
            }

            return group;
        }

        /// <summary>
        /// Convert Dataset information to Hdf5Dataset
        /// </summary>
        /// <param name="datasetId">Dataset ID</param>
        /// <param name="name">Dataset Name</param>
        /// <param name="path">Dataset Path</param>
        /// <returns>Hdf5Dataset</returns>
        private Hdf5Dataset ReadDataset(long datasetId, string name, string path)
        {
            long typeId = H5D.get_type(datasetId);
            string dataType = GetDataType(typeId);
            H5T.close(typeId);

            long spaceId = H5D.get_space(datasetId);
            int rank = H5S.get_simple_extent_ndims(spaceId);
            ulong[] dims = new ulong[rank];
            H5S.get_simple_extent_dims(spaceId, dims, null);
            int[] dimensions = dims.Select(d => (int)d).ToArray();
            H5S.close(spaceId);

            var dataset = new Hdf5Dataset
            {
                Name = name,
                Path = path,
                DataType = dataType,
                Dimensions = dimensions,
                DatasetId = datasetId
            };

            ReadAttributes(datasetId, dataset);

            long dataSize = dimensions.Aggregate(1L, (acc, dim) => acc * dim);
            if (dataSize <= 1000)
            {
                LoadDatasetData(dataset);
            }

            return dataset;
        }

        /// <summary>
        /// Load Dataset real data (for lazy load)
        /// </summary>
        public void LoadDatasetData(Hdf5Dataset dataset)
        {
            if (dataset.IsDataLoaded)
                return;

            long datasetId = dataset.DatasetId;
            if (datasetId <= 0)
            {
                long fileId = H5F.open(GetFilePathFromDatasetPath(dataset.Path), H5F.ACC_RDONLY);
                datasetId = H5D.open(fileId, dataset.Path);
                
                if (datasetId <= 0)
                {
                    throw new Exception($"데이터셋을 열 수 없습니다: {dataset.Path}");
                }
                
                try
                {
                    LoadDataForDataset(dataset, datasetId);
                }
                finally
                {
                    H5D.close(datasetId);
                    H5F.close(fileId);
                }
            }
            else
            {
                LoadDataForDataset(dataset, datasetId);
            }
        }

        /// <summary>
        /// Extract File Path from Dataset Path
        /// </summary>
        private string GetFilePathFromDatasetPath(string datasetPath)
        {
            throw new NotImplementedException("파일 경로 관리 로직이 필요합니다.");
        }

        /// <summary>
        /// Load data to Dataset
        /// </summary>
        private void LoadDataForDataset(Hdf5Dataset dataset, long datasetId)
        {
            long typeId = H5D.get_type(datasetId);
            H5T.class_t typeClass = H5T.get_class(typeId);

            object data;
            switch (typeClass)
            {
                case H5T.class_t.INTEGER:
                    data = ReadIntegerData(datasetId, dataset.Dimensions);
                    break;
                case H5T.class_t.FLOAT:
                    data = ReadFloatData(datasetId, dataset.Dimensions);
                    break;
                case H5T.class_t.STRING:
                    data = ReadStringData(datasetId, typeId, dataset.Dimensions);
                    break;
                default:
                    data = $"지원되지 않는 데이터 타입: {typeClass}";
                    break;
            }

            dataset.Data = data;

            H5T.close(typeId);
        }

        /// <summary>
        /// Read Integer Data
        /// </summary>
        private object ReadIntegerData(long datasetId, int[] dimensions)
        {
            int totalElements = dimensions.Aggregate(1, (acc, dim) => acc * dim);
            int[] data = new int[totalElements];

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                H5D.read(datasetId, H5T.NATIVE_INT32, H5S.ALL, H5S.ALL, H5P.DEFAULT, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }

            if (dimensions.Length == 1)
                return data;

            if (dimensions.Length == 2)
            {
                int[,] array2D = new int[dimensions[0], dimensions[1]];
                for (int i = 0; i < dimensions[0]; i++)
                {
                    for (int j = 0; j < dimensions[1]; j++)
                    {
                        array2D[i, j] = data[i * dimensions[1] + j];
                    }
                }
                return array2D;
            }

            return data;
        }

        /// <summary>
        /// Read Double Data
        /// </summary>
        private object ReadFloatData(long datasetId, int[] dimensions)
        {
            int totalElements = dimensions.Aggregate(1, (acc, dim) => acc * dim);
            double[] data = new double[totalElements];

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                H5D.read(datasetId, H5T.NATIVE_DOUBLE, H5S.ALL, H5S.ALL, H5P.DEFAULT, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }

            if (dimensions.Length == 1)
                return data;

            if (dimensions.Length == 2)
            {
                double[,] array2D = new double[dimensions[0], dimensions[1]];
                for (int i = 0; i < dimensions[0]; i++)
                {
                    for (int j = 0; j < dimensions[1]; j++)
                    {
                        array2D[i, j] = data[i * dimensions[1] + j];
                    }
                }
                return array2D;
            }

            return data;
        }

        /// <summary>
        /// Read String Data
        /// </summary>
        private object ReadStringData(long datasetId, long typeId, int[] dimensions)
        {
            nint stringLength = H5T.get_size(typeId);
            int totalElements = dimensions.Aggregate(1, (acc, dim) => acc * dim);
            
            if (H5T.is_variable_str(typeId) > 0)
            {
                string[] strData = new string[totalElements];
                // 가변 길이 문자열 처리 로직 (구현 필요)
                return strData;
            }
            
            byte[] buffer = new byte[totalElements * stringLength.ToInt32()];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                H5D.read(datasetId, typeId, H5S.ALL, H5S.ALL, H5P.DEFAULT, handle.AddrOfPinnedObject());
                
                string[] strData = new string[totalElements];
                for (int i = 0; i < totalElements; i++)
                {
                    int offset = i * stringLength.ToInt32();
                    strData[i] = Encoding.ASCII.GetString(buffer, offset, stringLength.ToInt32()).TrimEnd('\0');
                }
                
                return strData;
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Add Attributes
        /// </summary>
        /// <param name="objectId">Target Object ID</param>
        /// <param name="node">Node for attribute</param>
        private void ReadAttributes(long objectId, Hdf5Node node)
        {
            H5O.info_t objInfo = new H5O.info_t();
            H5O.get_info(objectId, ref objInfo);
            int attributeCount = (int)objInfo.num_attrs;

            for (int i = 0; i < attributeCount; i++)
            {
                long attrId = H5A.open_by_idx(objectId, ".", H5.index_t.NAME, H5.iter_order_t.NATIVE, (ulong)i, H5P.DEFAULT, H5P.DEFAULT);
                if (attrId >= 0)
                {
                    try
                    {
                        byte[] tempBuffer = Array.Empty<byte>();
                        nint nameSize = H5A.get_name(attrId, 0, tempBuffer);
                        byte[] nameBuffer = new byte[nameSize.ToInt32() + 1];
                        H5A.get_name(attrId, nameSize + 1, nameBuffer);
                        string attrName = Encoding.ASCII.GetString(nameBuffer).TrimEnd('\0');

                        long attrTypeId = H5A.get_type(attrId);
                        long attrSpaceId = H5A.get_space(attrId);

                        int rank = H5S.get_simple_extent_ndims(attrSpaceId);
                        ulong[] dims = new ulong[rank];
                        ulong[] maxDims = new ulong[rank];
                        H5S.get_simple_extent_dims(attrSpaceId, dims, maxDims);

                        object attrValue = ReadAttributeValue(attrId, attrTypeId);

                        var attribute = new Hdf5Attribute
                        {
                            Name = attrName,
                            Value = attrValue
                        };
                        
                        node.Attributes.Add(attribute);

                        H5T.close(attrTypeId);
                        H5S.close(attrSpaceId);
                    }
                    finally
                    {
                        H5A.close(attrId);
                    }
                }
            }
        }

        /// <summary>
        /// Read Attribute Value
        /// </summary>
        /// <param name="attrId">Attribute ID</param>
        /// <param name="typeId">Attribute Type ID</param>
        /// <returns>Attribute Value</returns>
        private object ReadAttributeValue(long attrId, long typeId)
        {
            H5T.class_t typeClass = H5T.get_class(typeId);
            
            switch (typeClass)
            {
                case H5T.class_t.STRING:
                    IntPtr size = new IntPtr(H5T.get_size(typeId).ToInt32());
                    IntPtr buffer = Marshal.AllocHGlobal(size.ToInt32());
                    try
                    {
                        H5A.read(attrId, typeId, buffer);
                        return Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }

                case H5T.class_t.INTEGER:
                    int[] intArray = new int[1] { 0 };
                    GCHandle intHandle = GCHandle.Alloc(intArray, GCHandleType.Pinned);
                    try
                    {
                        H5A.read(attrId, H5T.NATIVE_INT32, intHandle.AddrOfPinnedObject());
                        return intArray[0];
                    }
                    finally
                    {
                        intHandle.Free();
                    }

                case H5T.class_t.FLOAT:
                    double[] doubleArray = new double[1] { 0.0 };
                    GCHandle doubleHandle = GCHandle.Alloc(doubleArray, GCHandleType.Pinned);
                    try
                    {
                        H5A.read(attrId, H5T.NATIVE_DOUBLE, doubleHandle.AddrOfPinnedObject());
                        return doubleArray[0];
                    }
                    finally
                    {
                        doubleHandle.Free();
                    }

                default:
                    return $"[지원되지 않는 속성 타입: {typeClass}]";
            }
        }

        /// <summary>
        /// H5T.class_t to string
        /// </summary>
        /// <param name="typeId">Type ID</param>
        /// <returns>Type String</returns>
        private string GetDataType(long typeId)
        {
            H5T.class_t typeClass = H5T.get_class(typeId);

            switch (typeClass)
            {
                case H5T.class_t.INTEGER:
                    return "Integer";
                case H5T.class_t.FLOAT:
                    return "Float";
                case H5T.class_t.STRING:
                    return "String";
                case H5T.class_t.COMPOUND:
                    return "Compound";
                case H5T.class_t.ARRAY:
                    return "Array";
                default:
                    return $"Other ({typeClass})";
            }
        }
    }
}
