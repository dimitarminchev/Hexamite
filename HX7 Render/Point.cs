using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace HX7_Render
{
    public class Point
    {
        /// <summary>
        /// Point Identificator
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Point Catesian Coordinates.
        /// </summary>
        public Point3D Position { get; set; }


        /// <summary>
        /// List containing all points connected to this one.
        /// </summary>
        public IList<Edge> Connected { get; set; }

        /// <summary>
        /// Vusial State of the Point.
        /// </summary>
        public Visual3D Visual { get; set; }

        /// <summary>
        /// Default Point Constructor.
        /// </summary>
        public Point()
        {
            this.Connected = new List<Edge>();
            this.Name = "";
        }

        /// <summary>
        /// Overloaded Point Constructor.
        /// </summary>
        public Point(string name, Point3D position) : this()
        {
            this.Name = name;
            this.Position = position;
        }
    }
}
