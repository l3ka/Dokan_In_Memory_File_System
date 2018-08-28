using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Lines of code 39
/// </summary>
namespace Lab_2_oos
{
    class NodeLekachFile
    {
        private byte[] bytes;
        private DateTime dateCreate;
        private DateTime dateModified;

        public NodeLekachFile() => bytes = new byte[0];

        public byte[] Bytes
        {
            get => bytes;
            set => bytes = value;
        }

        public DateTime DateCreate
        {
            get => dateCreate;
            set => dateCreate = value;
        }

        public DateTime DateModified
        {
            get => dateModified;
            set => dateModified = value;
        }
    }
}
