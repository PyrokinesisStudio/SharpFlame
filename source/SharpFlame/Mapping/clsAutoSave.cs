using System;

namespace SharpFlame.Mapping
{
    public class clsAutoSave
    {
        public int ChangeCount;
        public DateTime SavedDate;

        public clsAutoSave()
        {
            SavedDate = DateTime.Now;
        }
    }
}