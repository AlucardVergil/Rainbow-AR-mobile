using System;
using System.IO;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Utility functions related to data
    /// </summary>
    public class DataUtils
    {
        /// <summary>
        /// Get the current player's data directory located in the writable persistent data path given by unity.
        /// The subdirectory is determined by the application's directory, thus allowing for multiple installations to be run at the same time without interference.
        /// This methods is thread-safe.
        /// </summary>
        /// <returns>The path to the data directory</returns>
        public static string GetDataDirectory()
        {
            lock (dataLock)
            {
                if (dataDirectory == null)
                {
                    var sep = Path.DirectorySeparatorChar;

                    // generate a hash value based on the application's path to get a deterministic subdirectory name per installation directory
                    string dataSubDir = Crypt.CreateMD5Hash(AppDomain.CurrentDomain.BaseDirectory);

                    var result = $"{Application.persistentDataPath}{sep}{dataSubDir}";

                    Directory.CreateDirectory(result);

                    dataDirectory = result;
                }

                return dataDirectory;
            }
        }

        private static readonly object dataLock = new();
        private static string dataDirectory;

    }
} // end namespace Cortex