using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hangon.Services {
    public class Storage<DataType> {
        public static async Task SaveObjectsAsync(DataType sourceData, String targetFileName) {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(targetFileName, CreationCollisionOption.ReplaceExisting);
            var outStream = await file.OpenStreamForWriteAsync(); // ERREUR NON GEREE ICI?

            DataContractSerializer serializer = new DataContractSerializer(typeof(DataType));
            serializer.WriteObject(outStream, sourceData);
            await outStream.FlushAsync();
            outStream.Dispose();
        }

        public static async Task<DataType> RestoreObjectsAsync(string filename) {
            try {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);

                var inStream = await file.OpenStreamForReadAsync();

                //Deserialize the objetcs
                DataContractSerializer serializer = new DataContractSerializer(typeof(DataType));
                DataType data = (DataType)serializer.ReadObject(inStream);
                inStream.Dispose();

                return data;

            } catch {
                DataType data = default(DataType);
                return data;
            }

        }
    }
}
