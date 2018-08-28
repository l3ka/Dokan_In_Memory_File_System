using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using DokanNet;

/// <summary>
/// Lines of code 486
/// 
/// Igor Sevo demands are
/// 1. MAX_TREE_DEPTH = 12
/// 2. MAX_FILENAME_LENGTH = 25
/// 3. MAX_FOLDERNAME_LENGTH = 25
/// 4. MAX_FILES_PER_FOLDER = 16
/// 5. MAX_FILE_CAPACITY = 32 * 1024 * 1024   or MAX_FILE_CAPACITY = 33_554_432B
/// 6. FILE_EXTENSION_LENGTH = 3
/// 7. INITIAL_CAPACITY = 512 * 1024 * 1024   or INITIAL_CAPACITY = 536_870_912B
/// 
/// My system constraints are:
/// 1. 
/// 
/// LekachFileSystem operations:
/// 1. Add files and folders
/// 2. Delete files and folders
/// 3. Moving files and folders
/// 4. Renaming files and folders
/// 5. Search for files and folders
/// 6. Write into the file
/// 7. Read from the file
/// 8. Displaying basic file information(creation date, date of last change, file size)
/// 9. Displaying basic information about the mount file system(number of byte, number of free bytes, total capacity)
/// </summary>
namespace Lab_2_oos
{
    class LekachFileSystem : IDokanOperations
    {
        private readonly IgorSevoDemands igorSevoDemands;
        private int freeBytesAvailable;
        private int totalNumberOfFreeBytes;
        private readonly object fileSystemLocker = new object();
        // .NET doesn't have anything that exactly tell you the size of an object, best shot is GetObjectSize() method
        private Dictionary<string, NodeLekachFile> files = new Dictionary<string, NodeLekachFile>();
        private HashSet<string> directories = new HashSet<string>();

        public LekachFileSystem()
        {
            igorSevoDemands = new IgorSevoDemands();
            (freeBytesAvailable, totalNumberOfFreeBytes) = (igorSevoDemands.InitialCapacity - GetObjectSize(files) - GetObjectSize(directories), igorSevoDemands.InitialCapacity);
            directories.Add(@"\"); // Root directories
        }

        /// <summary>Calculates the lenght in bytes of an object and returns the size</summary>
        private int GetObjectSize(object TestObject)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            byte[] Array;
            bf.Serialize(ms, TestObject);
            Array = ms.ToArray();
            return Array.Length;
        }

        public void Cleanup(string fileName, DokanFileInfo info)
        {
            if (info.DeleteOnClose == true)
            {
                if (directories.Contains(fileName))
                {
                    // string tempDirPathName = fileName;
                    // The @ option lets you specify a string without having to escape any characters
                    int pathCount = fileName.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length;
                    foreach (var directory in directories.Where(directoryPath => directoryPath.StartsWith(fileName + @"\") && directoryPath.Length > fileName.Length + 1 && directoryPath.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
                        directories.Remove(directory);
                    foreach (var file in files.Where(filePath => filePath.Key.StartsWith(fileName + @"\") && filePath.Key.Length > fileName.Length + 1 && filePath.Key.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
                    {
                        totalNumberOfFreeBytes += files[file.Key].Bytes.Length;
                        freeBytesAvailable += files[file.Key].Bytes.Length;
                        files.Remove(file.Key);
                    }
                    directories.Remove(fileName);
                }
                if (files.Keys.Contains(fileName))
                {
                    totalNumberOfFreeBytes += files[fileName].Bytes.Length;
                    freeBytesAvailable += files[fileName].Bytes.Length;
                    files.Remove(fileName);
                }
            }
        }

        public void CloseFile(string fileName, DokanFileInfo info) { }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            if (Path.GetFileName(fileName).Length > igorSevoDemands.MaxFilenameLength)
            {
                // throw new Exception("IgorSevoDemands: MAX FILE/FOLDER NAME LENGTH EXCEEDED!!!");
                Console.WriteLine("IgorSevoDemands: MAX FILE/FOLDER NAME LENGTH EXCEEDED!!!");
                return NtStatus.Unsuccessful;
            }
            if (fileName == "\\")
                return NtStatus.Success;
            if (access == DokanNet.FileAccess.ReadAttributes && mode == FileMode.Open)
                return NtStatus.Success;
            if (mode == FileMode.CreateNew)
            {
                if (Path.GetFileName(fileName).Length > igorSevoDemands.MaxFilenameLength)
                {
                    // throw new Exception("IgorSevoDemands: MAX FILE NAME LENGTH EXCEEDED!!!");
                    Console.WriteLine("IgorSevoDemands: MAX FILE NAME LENGTH EXCEEDED!!!");
                    return NtStatus.Unsuccessful;
                }
                if (attributes == FileAttributes.Directory || info.IsDirectory)
                {
                    if (Path.GetFileName(fileName).Length > igorSevoDemands.MaxFolderNameLength)
                    {
                        // throw new Exception("IgorSevoDemands: MAX FOLDER NAME LENGTH EXCEEDED!!!");
                        Console.WriteLine("IgorSevoDemands: MAX FOLDER NAME LENGTH EXCEEDED!!!");
                        return NtStatus.Unsuccessful;
                    }
                    if (fileName.Count(separator => separator == '\\') <= igorSevoDemands.MaxTreeDepth)
                        directories.Add(fileName);
                    else
                        return NtStatus.Unsuccessful;
                }
                else if (!files.Keys.Contains(fileName))
                {
                    /* // Smth for debuging
                    string pathName = Path.GetFileName(fileName);
                    int pathNameLast = pathName.LastIndexOf('.');
                    string tempString = Path.GetFileName(fileName).Substring(Path.GetFileName(fileName).LastIndexOf('.') + 1);
                    int tempLength = tempString.Length;
                    */
                    if (Path.GetFileName(fileName).Substring(Path.GetFileName(fileName).LastIndexOf('.') + 1).Length != igorSevoDemands.FileExtensionLength)
                    {
                        // throw new Exception("IgorSevoDemands: EXTENSION IS NOT LEGAL!!!");
                        Console.WriteLine("IgorSevoDemands: EXTENSION IS NOT LEGAL!!!");
                        return NtStatus.Unsuccessful;
                    }
                    if (files.Where(filePath => filePath.Key.Contains(fileName.Substring(0, fileName.LastIndexOf(@"\") + 1))).Count() >= igorSevoDemands.MaxFilesPerFolder)
                    {
                        // throw new Exception("IgorSevoDemands: MAX NUMBER OF FILES PER ONE FOLDER EXCEEDED!!!");
                        Console.WriteLine("IgorSevoDemands: MAX NUMBER OF FILES PER ONE FOLDER EXCEEDED!!!");
                        return NtStatus.Unsuccessful;
                    }
                    NodeLekachFile newNodeLekachFile = new NodeLekachFile();
                    (newNodeLekachFile.DateCreate, newNodeLekachFile.DateModified) = (DateTime.Now, DateTime.Now);
                    files.Add(fileName, newNodeLekachFile);
                }
            }
            return NtStatus.Success;
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
            if (!info.IsDirectory)
                return DokanResult.Error;
            int pathCount = fileName.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length;

            // A weak attempt to reduce the number of foreach loops down :)
            /* Ask Igora how to do this 
            var collection = files.Keys.Join(directories, file => file, directory => directory, (file, directory) => new { X = file, Y = directory });
            foreach (var x in collection)
            {
                files.Remove(x.X);
                directories.Remove(x.Y);
            }
            */
            // Implemented for deletion
            ArrayList tmp = new ArrayList();
            foreach (var x in directories.Where(directoryPath => directoryPath.Contains(fileName)))
                tmp.Add(x);
            foreach (var x in files.Where(filePath => filePath.Key.Contains(fileName)))
                tmp.Add(x.Key);
            foreach (string x in tmp)
            {
                files.Remove(x);
                directories.Remove(x);
            }
            tmp.Clear();
            
            
            // foreach (var directory in directories.Where(directoryPath => directoryPath.StartsWith(fileName + @"\") && directoryPath.Length > fileName.Length + 1 && directoryPath.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
               // directories.Remove(directory);
            foreach (var file in files.Where(filePath => filePath.Key.StartsWith(fileName + @"\") && filePath.Key.Length > fileName.Length + 1 && filePath.Key.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
            {
                totalNumberOfFreeBytes += files[file.Key].Bytes.Length;
                freeBytesAvailable += files[file.Key].Bytes.Length;
                // files.Remove(file.Key); // Redundant
            }
            return NtStatus.Success; // return DokanResult.Success;
        }

        public NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            if (info.IsDirectory)
                return NtStatus.Error; // return DokanResult.Error;
            return NtStatus.Success;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            files = new List<FileInformation>();
            if (fileName == @"\")
                fileName = "";
            int pathCount = fileName.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length;
            foreach (var directory in directories.Where(directoryPath => directoryPath.StartsWith(fileName + @"\") && directoryPath.Length > fileName.Length + 1 && directoryPath.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
            {
                FileInformation fileInformation = new FileInformation();
                (fileInformation.Attributes, fileInformation.FileName) = (FileAttributes.Directory, Path.GetFileName(directory));
                files.Add(fileInformation);
            }
            foreach (var file in this.files.Where(filePath => filePath.Key.StartsWith(fileName + @"\") && filePath.Key.Length > fileName.Length + 1 && filePath.Key.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
            {
                FileInformation fileInformation = new FileInformation();
                (fileInformation.FileName, fileInformation.Length, fileInformation.CreationTime, fileInformation.LastWriteTime) = (Path.GetFileName(file.Key), file.Value.Bytes.Length, file.Value.DateCreate, file.Value.DateModified);
                files.Add(fileInformation);
            }
            return NtStatus.Success;
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        {
            files = new FileInformation[0];
            return NtStatus.NotImplemented; // return DokanResult.NotImplemented;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = new FileInformation[0];
            return NtStatus.NotImplemented; // return DokanResult.NotImplemented;
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, DokanFileInfo info)
        {
            (freeBytesAvailable, totalNumberOfBytes, totalNumberOfFreeBytes) = (this.freeBytesAvailable, igorSevoDemands.InitialCapacity, this.totalNumberOfFreeBytes);
            return NtStatus.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            if (directories.Contains(fileName))
            {
                fileInfo = new FileInformation()
                {
                    FileName = Path.GetFileName(fileName),
                    Attributes = FileAttributes.Directory,
                    CreationTime = null,
                    LastWriteTime = null
                };
            }
            else if (files.ContainsKey(fileName))
            {
                fileInfo = new FileInformation()
                {
                    FileName = Path.GetFileName(fileName),
                    // Length = new FileInfo(fileName).Length, // There is no path when copying a file from my laptop file system to my "Dokan"file system
                    Length = files[fileName].Bytes.Length,
                    Attributes = FileAttributes.Normal,
                    CreationTime = files[fileName].DateCreate,
                    LastWriteTime = files[fileName].DateModified
                };
            }
            else
            {
                fileInfo = default(FileInformation);
                return NtStatus.Error;
            }
            return NtStatus.Success;

        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            security = null;
            return NtStatus.Success;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info)
        {
            volumeLabel = "Lekach";
            fileSystemName = "LekachFileSystem";
            features = FileSystemFeatures.None;
            return NtStatus.Success;
        }

        public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus Mounted(DokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            /* if (replace)
                return NtStatus.NotImplemented;
                */
            if (oldName == newName)
                return NtStatus.Success;
            if (directories.Contains(oldName))
            {
                int pathCount = oldName.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length;
                if (directories.Contains(newName) || Path.GetFileName(newName).Length > igorSevoDemands.MaxFolderNameLength) // Questionable second condition 
                    return NtStatus.NotImplemented;

                // New code in try/catch block
                try
                {
                    ArrayList temp = new ArrayList();
                    foreach (var x in files.Where(filePath => filePath.Key.Contains(oldName)))
                        temp.Add(x.Key);
                    foreach (string x in temp)
                    {
                        string tempString = x;
                        tempString = tempString.Replace(oldName, newName);
                        NodeLekachFile tempNode = files[x];
                        files.Remove(x);
                        files.Add(tempString, tempNode);
                    }
                    temp.Clear();
                    foreach (var x in directories.Where(filePath => filePath.Contains(oldName)))
                        temp.Add(x);
                    foreach (string x in temp)
                    {
                        string tempString = x;
                        tempString = tempString.Replace(oldName, newName);
                        directories.Remove(oldName);
                        directories.Add(tempString);
                    }
                    temp.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                /* In fact works, throw Exception in only 1 special case
                directories.Add(newName);
                foreach (var file in files.Where(filePath => filePath.Key.StartsWith(oldName + @"\") && filePath.Key.Length > oldName.Length + 1 && filePath.Key.Split(@"\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length == pathCount + 1))
                {
                    files.Add(newName + @"\" + Path.GetFileName(file.Key), file.Value);
                    files.Remove(file.Key);
                }
                directories.Remove(oldName);
                */
                return NtStatus.Success;
            }
            else if (files.Keys.Contains(oldName))
            {
                if (files.Keys.Contains(newName) || Path.GetFileName(newName).Length > igorSevoDemands.MaxFilenameLength) // Questionable second condition
                    return NtStatus.NotImplemented;
                files.Add(newName, files[oldName]);
                files.Remove(oldName);
                return NtStatus.Success;
            }
            return NtStatus.Unsuccessful;
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            lock (fileSystemLocker)
            {
                byte[] file = files[fileName].Bytes;
                int i = 0;
                for (i = 0; i < file.Length && i < buffer.Length; ++i)
                    buffer[i] = file[i];
                bytesRead = i;
            }
            /*
            byte[] file = files[fileName].Bytes;
            int i = 0;
            for (i = 0; i < file.Length && i < buffer.Length; ++i)
                buffer[i] = file[i];
            bytesRead = i;
            */
            return NtStatus.Success;
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus Unmounted(DokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            if (files[fileName].Bytes.Length + buffer.Length > igorSevoDemands.MaxFileCapacity)
            {
                // throw new Exception("IgorSevoDemands: MAX FILE CAPACITY EXCEEDED!!!");
                bytesWritten = 0;
                files[fileName].DateModified = DateTime.Now;
                Console.WriteLine("IgorSevoDemands: MAX FILE CAPACITY EXCEEDED!!!");
                return NtStatus.Unsuccessful;
            }
            if (fileName.Count(separator => separator == '\\') > igorSevoDemands.MaxTreeDepth)
            {
                // throw new Exception("IgorSevoDemands: MAX TREE DEPTH EXCEEDED!!!");
                bytesWritten = 0;
                files[fileName].DateModified = DateTime.Now;
                Console.WriteLine("IgorSevoDemands: MAX TREE DEPTH EXCEEDED!!!");
                return NtStatus.Unsuccessful;
            }
            if (Path.GetFileName(fileName).Length > igorSevoDemands.MaxFilenameLength)
            {
                // throw new Exception("IgorSevoDemands: MAX FILE/FOLDER NAME LENGTH EXCEEDED!!!");
                bytesWritten = 0;
                files[fileName].DateModified = DateTime.Now;
                Console.WriteLine("IgorSevoDemands: MAX FILE/FOLDER NAME LENGTH EXCEEDED!!!");
                return NtStatus.Unsuccessful;
            }
            if (Path.GetFileName(fileName).Substring(Path.GetFileName(fileName).LastIndexOf('.') + 1).Length != igorSevoDemands.FileExtensionLength)
            {
                // throw new Exception("IgorSevoDemands: EXTENSION IS NOT LEGAL!!!");
                bytesWritten = 0;
                files[fileName].DateModified = DateTime.Now;
                Console.WriteLine("IgorSevoDemands: EXTENSION IS NOT LEGAL!!!");
                return NtStatus.Unsuccessful;
            }
            lock (fileSystemLocker)
            {
                byte[] file = new byte[buffer.Length];
                int i = 0;
                for (i = 0; i < buffer.Length; ++i)
                    file[i] = buffer[i];
                (files[fileName].Bytes, bytesWritten) = (file, i);
                totalNumberOfFreeBytes -= bytesWritten;
                freeBytesAvailable -= bytesWritten;
                files[fileName].DateModified = DateTime.Now;
            }
            /*
            byte[] file = new byte[buffer.Length];
            int i = 0;
            for (i = 0; i < buffer.Length; ++i)
                file[i] = buffer[i];
            (files[fileName].Bytes, bytesWritten) = (file, i);
            totalNumberOfFreeBytes -= bytesWritten;
            freeBytesAvailable -= bytesWritten;
            files[fileName].DateModified = DateTime.Now;
            */
            return NtStatus.Success;
        }
    }
}
