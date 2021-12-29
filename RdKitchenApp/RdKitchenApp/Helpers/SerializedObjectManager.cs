using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace RdKitchenApp.Helpers
{
    public class SerializedObjectManager
    {
        string savePath(string dir) { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dir); }
        public void DeleteAll()
        {
            File.Delete(savePath("BranchId"));
        }
        public void SaveData(object serializedData, string path)
        {
            var save = serializedData;

            var binaryFormatter = new BinaryFormatter();
            using (var fileStream = File.Create(savePath(path)))
            {
                binaryFormatter.Serialize(fileStream, save);
            }
        }
        public object RetrieveData(string path)
        {
            object load = null;

            if (File.Exists(savePath(path)))
            {
                var binaryFormatter = new BinaryFormatter();
                using (var fileStream = File.Open(savePath(path), FileMode.Open))
                {
                    load = (object)binaryFormatter.Deserialize(fileStream);

                    return load;
                }
            }

            return null;
        }
    }
}
