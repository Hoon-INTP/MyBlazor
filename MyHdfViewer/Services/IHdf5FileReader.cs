
using MyHdfViewer.Models;

namespace MyHdfViewer.Services
{
    /// <summary>
    /// Create Hdf5FileModel from HDF5 File MemoryStream
    /// </summary>
    public interface IHdf5FileReader
    {
        /// <summary>
        /// Return Hdf5FileModel from MemoryStream HDF5 File
        /// </summary>
        /// <param name="fileName">Name of HDF5 File</param>
        /// <returns>Hdf5FileModel</returns>
        Hdf5FileModel ReadHdf5FromStream(MemoryStream stream, string fileName);
    }
}
