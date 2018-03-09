using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO.Compression;

namespace DictationProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataPath = @"D:\Documents\Josh\learnDotNet\dotnet-core-mac-linux-getting-started\m2\";
            var uploadsPath = Path.Combine(dataPath, "uploads");
            var readyPath = Path.Combine(dataPath, "ready_for_transcription");

            //loop through the subfolders in the given dir
            foreach (var subfolder in Directory.GetDirectories(uploadsPath))
            {
                //path for metadata
                var metadataFilePath = Path.Combine(subfolder, "metadata.json");
                //print out what we are doing 
                Console.WriteLine($"Reading {metadataFilePath}");

                //get the metadata file
                var metadataCollection = GetMetaData(metadataFilePath);

                //loop all the audio files in the given metadata file
                foreach (var metadata in metadataCollection)
                {
                    //get the path
                    var audioFilePath = Path.Combine(subfolder, metadata.File.FileName);

                    //check the checksum
                    var md5Checksum = getChecksum(audioFilePath);

                    //make sure the checksums it matches
                    if (md5Checksum.Replace("-", "").ToLower() != metadata.File.Md5Checksum)
                    {
                        throw new Exception($"Checksum not verified! {metadata.File.FileName} is Correpted");
                    }

                    //make a guid
                    var uniqueId = Guid.NewGuid();
                    metadata.File.FileName = uniqueId + ".wav";
                    var newPath = Path.Combine(readyPath, metadata.File.FileName);

                    //make the zip file
                    CreateCompressedFile(audioFilePath, newPath);

                    //make the metadataFile
                    SaveSingleMetadata(metadata, newPath + ".json");
                }
            }

        }

        private static void CreateCompressedFile(string inputFilePath, string outputFilePath)
        {
            //add extenion
            outputFilePath += ".zip";
            //update the user
            Console.WriteLine($"Creating {outputFilePath}");

            //input stream
            // var inputStream = File.Open(inputFilePath, FileMode.Open);
            //output stream
            var outputStream = File.Create(outputFilePath);
            //Create the zip file
            var zipFile = new ZipArchive(outputStream, ZipArchiveMode.Create);
            //add the audiofile to the zip
            zipFile.CreateEntryFromFile(inputFilePath, Path.GetFileName(inputFilePath), CompressionLevel.Optimal);

            //release it and write to hd
            zipFile.Dispose();




        }

        private static string getChecksum(string audioFilePath)
        {
            //get the file Stream
            var fileStream = File.Open(audioFilePath, FileMode.Open);

            //get an instance
            var md5 = System.Security.Cryptography.MD5.Create();
            //run the hash
            var md5Bytes = md5.ComputeHash(fileStream);

            //close the file
            fileStream.Dispose();

            //convert the Bytes to a string
            return BitConverter.ToString(md5Bytes);
        }

        private static List<Metadata> GetMetaData(string metadataFilePath)
        {
            //get the metadata file
            var metadataFileStream = File.Open(metadataFilePath, FileMode.Open);
            //make the settings for serializer
            var settings = new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ssZ")
            };
            //get a converter for json to Metadata
            var serializer = new DataContractJsonSerializer(typeof(List<Metadata>), settings);
            //read the file in, this returns a plain obj so make sure you cast
            return (List<Metadata>)serializer.ReadObject(metadataFileStream);
        }

        private static void SaveSingleMetadata(Metadata metadata, string metadataFilePath)
        {
            //tell the user
            Console.WriteLine($"Creating {metadataFilePath}");
            
            //get the metadata file
            var metadataFileStream = File.Open(metadataFilePath, FileMode.Create);
            //make the settings for serializer
            var settings = new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ssZ"),
                EmitTypeInformation = EmitTypeInformation.Never
            };
            //get a converter for json to Metadata
            var serializer = new DataContractJsonSerializer(typeof(List<Metadata>), settings);
            
            //write the file out
            serializer.WriteObject(metadataFileStream, metadata);
            //close the file
            metadataFileStream.Dispose();
        }
    }
}
