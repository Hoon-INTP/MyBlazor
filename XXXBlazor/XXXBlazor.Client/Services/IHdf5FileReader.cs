
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Services
{
    /// <summary>
    /// Create Hdf5FileModel from HDF5 File MemoryStream
    /// </summary>
    public interface IHdf5FileReader
    {
        /// <summary>
        /// Return Hdf5FileModel from MemoryStream HDF5 file
        /// </summary>
        /// <param name="stream">MemoryStream of HDF5 file</param>
        /// <param name="fileName">name of HDF5 file</param>
        /// <returns>Hdf5FileModel</returns>
        Hdf5FileModel ReadHdf5FromStream(MemoryStream stream, string fileName);

        /// <summary>
        /// Return Hdf5FileModel from temporary file
        /// </summary>
        /// <param name="path">path of HDF5 temporary file</param>
        /// <param name="fileName">name of HDF5 file</param>
        /// <returns>Hdf5FileModel</returns>
        Hdf5FileModel ReadHdf5FromTempFile(string path, string fileName);
    }
}
