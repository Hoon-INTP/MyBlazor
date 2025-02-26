using System;
using System.Text;
using System.Runtime.InteropServices;
using HDF.PInvoke;

namespace MyHdfViewer.Utility
{
    /// <summary>
    /// 테스트용 HDF5 샘플 파일을 생성하는 유틸리티 클래스
    /// </summary>
    public static class Hdf5SampleCreator
    {
        /// <summary>
        /// 2개의 그룹과 2개의 데이터셋, 1개의 속성을 가진 샘플 HDF5 파일을 생성합니다.
        /// </summary>
        /// <param name="filePath">생성할 파일 경로</param>
        /// <returns>성공 여부</returns>
        public static bool CreateSampleFile(string filePath)
        {
            // 파일 생성
            long fileId = H5F.create(filePath, H5F.ACC_TRUNC);
            if (fileId < 0)
                return false;

            try
            {
                // 루트 그룹에 속성 추가
                AddRootAttribute(fileId);

                // Group1 생성 및 데이터셋 추가
                CreateGroup1WithDataset(fileId);

                // Group2 생성 및 데이터셋 추가
                CreateGroup2WithDataset(fileId);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"샘플 파일 생성 중 오류 발생: {ex.Message}");
                return false;
            }
            finally
            {
                // 파일 닫기
                H5F.close(fileId);
            }
        }

        /// <summary>
        /// 루트 그룹에 속성을 추가합니다.
        /// </summary>
        /// <param name="fileId">파일 ID</param>
        private static void AddRootAttribute(long fileId)
        {
            // 루트 그룹 열기
            long rootId = H5G.open(fileId, "/");
            
            try
            {
                // 문자열 타입 생성
                long strType = H5T.copy(H5T.C_S1);
                H5T.set_size(strType, new IntPtr(-1)); // 가변 길이 문자열
                
                // 속성 공간 생성 (스칼라)
                long spaceId = H5S.create(H5S.class_t.SCALAR);
                
                // 속성 생성
                long attrId = H5A.create(rootId, "Description", strType, spaceId);
                
                // 속성 값 설정
                string description = "샘플 HDF5 파일";
                byte[] descBytes = Encoding.ASCII.GetBytes(description);
                GCHandle handle = GCHandle.Alloc(descBytes, GCHandleType.Pinned);
                try
                {
                    H5A.write(attrId, strType, handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
                
                // 리소스 해제
                H5A.close(attrId);
                H5S.close(spaceId);
                H5T.close(strType);
            }
            finally
            {
                // 루트 그룹 닫기
                H5G.close(rootId);
            }
        }

        /// <summary>
        /// Group1과 데이터셋을 생성합니다.
        /// </summary>
        /// <param name="fileId">파일 ID</param>
        private static void CreateGroup1WithDataset(long fileId)
        {
            // Group1 생성
            long group1Id = H5G.create(fileId, "Group1");
            
            try
            {
                // 데이터셋 공간 생성 (1차원 배열)
                long spaceId = H5S.create_simple(1, new ulong[] { 10 }, null);
                
                // 데이터셋 생성
                long datasetId = H5D.create(group1Id, "Dataset1", H5T.NATIVE_INT, spaceId);
                
                try
                {
                    // 데이터 준비 및 쓰기
                    int[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                    GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                    H5D.write(datasetId, H5T.NATIVE_INT, H5S.ALL, H5S.ALL, H5P.DEFAULT, handle.AddrOfPinnedObject());
                }
                finally
                {
                    // 데이터셋 닫기
                    H5D.close(datasetId);
                }
                
                // 공간 해제
                H5S.close(spaceId);
            }
            finally
            {
                // 그룹 닫기
                H5G.close(group1Id);
            }
        }

        /// <summary>
        /// Group2와 데이터셋을 생성합니다.
        /// </summary>
        /// <param name="fileId">파일 ID</param>
        private static void CreateGroup2WithDataset(long fileId)
        {
            // Group2 생성
            long group2Id = H5G.create(fileId, "Group2");
            
            try
            {
                // 데이터셋 공간 생성 (2차원 배열)
                long spaceId = H5S.create_simple(2, new ulong[] { 3, 3 }, null);
                
                // 데이터셋 생성
                long datasetId = H5D.create(group2Id, "Dataset2", H5T.NATIVE_DOUBLE, spaceId);
                
                try
                {
                    // 데이터 준비 및 쓰기
                    double[,] data = {
                        { 1.1, 1.2, 1.3 },
                        { 2.1, 2.2, 2.3 },
                        { 3.1, 3.2, 3.3 }
                    };
                    
                    GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                    try
                    {
                        H5D.write(datasetId, H5T.NATIVE_DOUBLE, H5S.ALL, H5S.ALL, H5P.DEFAULT, handle.AddrOfPinnedObject());
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
                finally
                {
                    // 데이터셋 닫기
                    H5D.close(datasetId);
                }
                
                // 공간 해제
                H5S.close(spaceId);
            }
            finally
            {
                // 그룹 닫기
                H5G.close(group2Id);
            }
        }
    }
}