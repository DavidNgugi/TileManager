using System;

namespace TileManager
{
    class Mode
    {
        public bool Pan     { get; set; }
        public bool Select  { get; set; }
        public bool Zoom    { get; set; }

        public void Reset() 
        {
            Pan     = false;
            Select  = false;
            Zoom    = false;
        }

        public void Status() {
            Console.WriteLine("*************************************");
            Console.WriteLine("Mode Pan: " + Pan.ToString());
            Console.WriteLine("Mode Select: " + Select.ToString());
            Console.WriteLine("Mode Zoom: " + Zoom.ToString());
        }
    }
}
