using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Lines of code 53
/// </summary>
namespace Lab_2_oos
{
    class IgorSevoDemands
    {
        // const are implicitly static, and readonly NOT(I guess so)
        private readonly int MAX_TREE_DEPTH = 12;
        private readonly int MAX_FILENAME_LENGTH = 25;
        private readonly int MAX_FOLDERNAME_LENGTH = 25;
        private readonly int MAX_FILES_PER_FOLDER = 16;
        private readonly int MAX_FILE_CAPACITY = 32 * 1024 * 1024; // or MAX_FILE_CAPACITY = 33_554_432B
        private readonly int FILE_EXTENSION_LENGTH = 3;
        private readonly int INITIAL_CAPACITY = 512 * 1024 * 1024; // or INITIAL_CAPACITY = 536_870_912B

        public int MaxTreeDepth
        {
            get => MAX_TREE_DEPTH;
        }
        public int MaxFilenameLength
        {
            get => MAX_FILENAME_LENGTH;
        }
        public int MaxFolderNameLength
        {
            get => MAX_FOLDERNAME_LENGTH;
        }
        public int MaxFilesPerFolder
        {
            get => MAX_FILES_PER_FOLDER;
        }
        public int MaxFileCapacity
        {
            get => MAX_FILE_CAPACITY;
        }
        public int FileExtensionLength
        {
            get => FILE_EXTENSION_LENGTH;
        }
        public int InitialCapacity
        {
            get => INITIAL_CAPACITY;
        }
    }
}
