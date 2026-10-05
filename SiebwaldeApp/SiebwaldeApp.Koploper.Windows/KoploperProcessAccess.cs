namespace SiebwaldeApp.Koploper.Windows
{
    /// <summary>
    /// Windows process-access rights used by the read-only Koploper adapter. The read/query
    /// masks are the minimal rights needed to locate and read the target; <see cref="WriteRightsMask"/>
    /// aggregates every write-capable right so a caller or test can prove that a handle was
    /// opened without requesting any of them.
    /// </summary>
    public static class KoploperProcessAccess
    {
        // Read / query rights.
        public const uint PROCESS_VM_READ = 0x0010;
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;

        /// <summary>
        /// Minimal read-only access for the memory reader: read memory and query limited
        /// process information. Value 0x1010.
        /// </summary>
        public const uint MemoryReaderDesiredAccess = PROCESS_VM_READ | PROCESS_QUERY_LIMITED_INFORMATION; // 0x1010

        /// <summary>
        /// Read-only access needed by the locator to enumerate the 32-bit module list and read
        /// the module file path: full query information plus read memory. Value 0x0410.
        /// </summary>
        public const uint LocatorDesiredAccess = PROCESS_QUERY_INFORMATION | PROCESS_VM_READ; // 0x0410

        // Write-capable rights, each declared so the aggregate below is self-documenting.
        public const uint PROCESS_VM_WRITE = 0x0020;
        public const uint PROCESS_VM_OPERATION = 0x0008;
        public const uint PROCESS_CREATE_THREAD = 0x0002;
        public const uint PROCESS_CREATE_PROCESS = 0x0080;
        public const uint PROCESS_DUP_HANDLE = 0x0040;
        public const uint PROCESS_SET_INFORMATION = 0x0200;
        public const uint PROCESS_SET_QUOTA = 0x0100;
        public const uint PROCESS_SUSPEND_RESUME = 0x0800;
        public const uint PROCESS_TERMINATE = 0x0001;
        public const uint WRITE_DAC = 0x00040000;
        public const uint WRITE_OWNER = 0x00080000;
        public const uint STANDARD_RIGHTS_WRITE = 0x00020000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint DELETE = 0x00010000;

        /// <summary>
        /// Aggregate of every write-capable access right. A read-only handle (as opened by this
        /// adapter) must have none of these bits set.
        /// </summary>
        public const uint WriteRightsMask =
            PROCESS_VM_WRITE |
            PROCESS_VM_OPERATION |
            PROCESS_CREATE_THREAD |
            PROCESS_CREATE_PROCESS |
            PROCESS_DUP_HANDLE |
            PROCESS_SET_INFORMATION |
            PROCESS_SET_QUOTA |
            PROCESS_SUSPEND_RESUME |
            PROCESS_TERMINATE |
            WRITE_DAC |
            WRITE_OWNER |
            STANDARD_RIGHTS_WRITE |
            GENERIC_WRITE |
            DELETE;
    }
}
