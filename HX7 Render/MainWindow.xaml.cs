using HelixToolkit.Wpf;
using PropertyTools.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Windows.Media;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics;
using System.Linq;
using System.Windows;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HX7_Render
{
    public partial class MainWindow
    {
        /// <summary>
        /// Serial Interface.
        /// </summary>
        private SerialInterface _port;

        /// <summary>
        /// Serial Interface On Data Received variables
        /// </summary>
        private string dataBlock = null;

        /// <summary>
        /// Graph, Points and Edges.
        /// </summary>
        private Dictionary<string, List<Tuple<string, double>>> graph = new Dictionary<string, List<Tuple<string, double>>>();
        private Dictionary<string, Point> points = new Dictionary<string, Point>();

        /// <summary>
        /// Magic Nimbers Here.
        /// </summary>
        private double NORM_FACTOR = 200; // Normalization Scale Factor
        private double EDGE_FACTOR = 10; // Edge's Radius
        private Brush EDGE_COLOR = Brushes.Red; // Edge's Color
        private Brush VERTEX_COLOR = Brushes.Blue; // Vertex's Color        

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Step 1. Reading the distances between points and forming graph.
            // graph = Graph();

            // Step 2. Calculate Points Coordinates from the graph
            points = Points();

            // Step 3. Vizualizing the graph (points and lines)
            RunVizualize();
        }

        /// <summary>
        /// Connect Window Sends Serial Interface Port
        /// </summary>
        void onPortSelected(SerialInterface port)
        {
            try
            {
                _port = port;
                _port.NewSerialDataRecieved += new EventHandler<SerialDataEventArgs>(_DataRecieved);
                _port.StartListening();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Serial Interface On Data Received
        /// </summary>
        private async void _DataRecieved(object sender, SerialDataEventArgs e)
        {
            // This application is connected to a GPS sending ASCCI characters, so data is converted to text
            string dataReceived = Encoding.ASCII.GetString(e.Data);

            // Forming Data Block
            if (dataReceived.Length > 0) dataBlock += dataReceived;
            string[] splittedDataBlock = dataBlock.Split(new char[] { '\n' });

            //  Process Received Data   
            foreach (var line in splittedDataBlock)
            {
                try
                {
                    // Find measurements
                    // if (line.Contains("R") && line.Contains("T") && line.Contains("A") && line.IndexOf("A") != line.Length - 1)
                    {

                        // NEW
                        char delimiter = ' ';
                        int componentcount = 3;
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var components = line.Split(new[] { delimiter });
                        if (components.Length < componentcount) continue;

                        // "R" = _node, "T" = _key, "A" = _value 
                        string _node = components[0].Replace("R", "");
                        string _key = components[1].Replace("T", "");
                        double _value = double.Parse(components[3].Replace("A", ""));



                        // OLD
                        // "R" = _node, "T" = _key, "A" = _value 
                        //string _node = line.Substring(line.IndexOf("R") + 1, line.IndexOf("T") - line.IndexOf("R") - 2);
                        //string _key = line.Substring(line.IndexOf("T") + 1, line.IndexOf("Q") - line.IndexOf("T") - 2);
                        //double _value = double.Parse(line.Substring(line.IndexOf("A") + 1, line.Length - line.IndexOf("A") - 1));



                        // without self connected nodes
                        if (_node != _key)
                        {
                            // If node exist?
                            if (graph.ContainsKey(_node))
                            {
                                Tuple<string, double> Exist = graph[_node].Find(i => i.Item1 == _key);
                                if (Exist != null)
                                {
                                    // update connection
                                    graph[_node].Remove(Exist);
                                    graph[_node].Add(new Tuple<string, double>(_key, _value));
                                }
                                else
                                {
                                    // add new connection
                                    graph[_node].Add(new Tuple<string, double>(_key, _value));
                                }

                            }
                            else
                            {
                                // add new node
                                graph.Add(_node, new List<Tuple<string, double>>() { new Tuple<string, double>(_key, _value) });
                            }
                        }
                    }
                }
                catch (Exception ex) {; ; }


                // Check for completion
                if (graph.Count > 4)
                {
                    bool Complete = true;
                    foreach (var item in graph)
                    {
                        if (item.Value.Count != graph.Count - 1) Complete = false;
                    }
                    if (Complete)
                    {
                        // Stop Serial Port Listning
                        _port.StopListening();

                        try
                        {
                            // Rest of the data block
                            dataBlock = null;

                            // Calculate Points Coordinates from the graph
                            points = Points();
                            
                            // Vizualizing the graph (points and lines)
                            if (points.Count > 4) await RunVizualize();
                        }
                        catch (Exception ex) { ;; }

                        // Start Serial Port Listning
                        _port.StartListening();
                    }
                }
            }
        }

        /// <summary>
        /// Step 1. Reading the distances between points and forming graph.
        /// </summary>
        /// <returns>Full graph containing distances to each point.</returns>
        private Dictionary<string, List<Tuple<string, double>>> Graph()
        {
            // The result graph
            var graph = new Dictionary<string, List<Tuple<string, double>>>();

            // Demo Data Source Packet 
            string[] node = new string[20]
            {
                "21", "22", "23", "24", // T25
                "22", "23", "24", "25", // T21
                "21", "23", "24", "25", // T22
                "21", "22", "24", "25", // T23
                "21", "22", "23", "25"  // T24
            };
            string[] key = new string[20]
            {
                "25", "25", "25", "25", // T25
                "21", "21", "21", "21", // T21
                "22", "22", "22", "22", // T22
                "23", "23", "23", "23", // T23
                "24", "24", "24", "24"  // T24
            };
            int[] value = new int[20]
            {
                2000, 2000, 2000, 2000, // T25
                1400, 1000, 1000, 2000, // T21
                1400, 1000, 1000, 2000, // T22
                1000, 1000, 1400, 2000, // T23
                1000, 1000, 1400, 2000  // T24
            };


            // Adding values from the dtasource packet to the graph
            for (int index = 0; index < node.Length; index++)
            {
                if (graph.ContainsKey(node[index])) graph[node[index]].Add(new Tuple<string, double>(key[index], value[index]));
                else graph.Add(node[index], new List<Tuple<string, double>>() { new Tuple<string, double>(key[index], value[index]) });
            }

            // Returning the result
            return graph;
        }

        /// <summary>
        /// Step 2. Reading the distances between points and forming graph.
        /// </summary>
        /// <returns>Full graph containing distances to each point.</returns>
        private Dictionary<string, Point> Points()
        {            
            double zero = 0.1f; // Absolutely zero = 0.1mm

            // Find the stationary rectangle points
            var candidates = new Dictionary<string, List<Tuple<string, double>>>();
            foreach (var node in graph)
            {
                double eps = 25f; // 25 mm max error distance
                List<Tuple<string, double>> a = node.Value;
                List<Tuple<string, double>> b = node.Value;
                for (int i = 0; i < a.Count; i++)
                    for (int j = 0; j < b.Count; j++)
                    {
                        if (i != j && a[i].Item2 >= b[j].Item2 - eps && a[i].Item2 <= b[j].Item2 + eps)
                        {
                            if (candidates.ContainsKey(node.Key)) candidates[node.Key].Add(new Tuple<string, double>(a[i].Item1, a[i].Item2));
                            else candidates.Add(node.Key, new List<Tuple<string, double>>() { new Tuple<string, double>(a[i].Item1, a[i].Item2) });
                        }
                    }
            }

            // Process Rectangle Candidates
            candidates = candidates.Where(n => n.Value.Count == 2).ToDictionary(n => n.Key, x => x.Value);
            candidates = candidates.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value); // Sort

            // Rectangle distances
            var first4 = new Dictionary<string, Point>();
            foreach (var node in candidates)
            {
                foreach (var item in node.Value)
                {
                    double x = zero, y = zero, z = zero;
                    string node_from = node.Key;    // from
                    string node_to = item.Item1;    // to
                    double node_range = item.Item2; // distance 

                    // first point
                    if (first4.Count == 0)
                    {
                        first4[node_to] = new Point(node_to, new Point3D(x, y, z));
                        x = x + node_range;
                        first4[node_from] = new Point(node_from, new Point3D(x, y, z));
                    }

                    // other points
                    else if (first4.ContainsKey(node_from) && !first4.ContainsKey(node_to))
                    {
                        x = zero + first4[node_from].Position.X;
                        y = zero + first4[node_from].Position.Y;
                        if (x > zero) y = zero + node_range;
                        else if (y > zero) x = zero + node_range;
                        if (x == zero && y == zero) y = zero + node_range;
                        first4[node_to] = new Point(node_to, new Point3D(x, y, z));
                    }
                }
            }

            // The result nodes
            var nodes = first4;



            //// NEW
            //var nodes = new Dictionary<string, Point>();
            //nodes.Add("21", new Point("21", new Point3D(   10.1, 10.1,   10.1 )));
            //nodes.Add("22", new Point("22", new Point3D( 1000.1, 10.1,   10.1 )));
            //nodes.Add("23", new Point("23", new Point3D( 1000.1, 10.1, 1000.1 )));
            //nodes.Add("24", new Point("24", new Point3D(   10.1, 10.1, 1000.1 )));
            //var first4 = nodes;



            // Adding the point not in rectangle
            foreach (var node in graph)
            {
                if (!nodes.ContainsKey(node.Key)) // Not added?
                {
                    var f4 = new List<string>(first4.Keys);
                    var a1 = new Point3D(first4[f4[0]].Position.X, first4[f4[0]].Position.Y, first4[f4[0]].Position.Z);
                    var a2 = new Point3D(first4[f4[1]].Position.X, first4[f4[1]].Position.Y, first4[f4[1]].Position.Z);
                    var a3 = new Point3D(first4[f4[2]].Position.X, first4[f4[2]].Position.Y, first4[f4[2]].Position.Z);
                    var a4 = new Point3D(first4[f4[3]].Position.X, first4[f4[3]].Position.Y, first4[f4[3]].Position.Z);

                    double d21 = Euclidean(first4[f4[1]].Position, first4[f4[0]].Position);
                    double d31 = Euclidean(first4[f4[2]].Position, first4[f4[0]].Position);
                    double d41 = Euclidean(first4[f4[3]].Position, first4[f4[0]].Position);

                    var r = node.Value.ToDictionary(l => l.Item1, l => l.Item2);
                    var r1 = r[f4[0]];
                    var r2 = r[f4[1]];
                    var r3 = r[f4[2]];
                    var r4 = r[f4[3]];

                    double b21 = (Math.Pow(r1, 2) - Math.Pow(r2, 2) + Math.Pow(d21, 2)) / 2;
                    double b31 = (Math.Pow(r1, 2) - Math.Pow(r3, 2) + Math.Pow(d31, 2)) / 2;
                    double b41 = (Math.Pow(r1, 2) - Math.Pow(r4, 2) + Math.Pow(d41, 2)) / 2;

                    // Linear Equation Systems
                    // http://numerics.mathdotnet.com/LinearEquations.html
                    var A = Matrix<double>.Build.DenseOfArray(new double[,]
                    {
                        { a2.X-a1.X + zero, a2.Y-a1.Y + zero, a2.Z-a1.Z + zero },
                        { a3.X-a1.X + zero, a3.Y-a1.Y + zero, a3.Z-a1.Z + zero },
                        { a4.X-a1.X + zero, a4.Y-a1.Y + zero, a4.Z-a1.Z + zero }
                    });
                    var B = Vector<double>.Build.Dense(new double[] { b21, b31, b41 });
                    var X = A.Solve(B);


                    // TODO: fix coordinates


                    // Get potitive values only
                    double x = zero + Math.Abs(X[0]);
                    double y = zero + Math.Abs(X[1]);
                    double z = zero + Math.Abs(X[2]);

                    // Average new and old measurement
                    if (points.ContainsKey(node.Key)) // Already exist?
                    {
                        x = (points[node.Key].Position.X + x) / 2;
                        y = (points[node.Key].Position.Y + y) / 2;
                        z = (points[node.Key].Position.Z + z) / 2;
                    }

                    // Ratio Factor
                    var ratio = ((d21 / r1 + d21 / r2) + (d31 / r1 + d31 / r3) + (d41 / r1 + d41 / r4)) / 3;
                    if (x / r1 > ratio) x = x / (x / r1);
                    if (y / r1 > ratio) y = y / (y / r1);
                    if (z / r1 > ratio) z = z / (z / r1);



                    // Add to the nodes list
                    nodes[node.Key] = new Point(node.Key, new Point3D(x,y,z));
                }
            }

            // Normalization
            var maxPoint = nodes.Max(node => node.Value.Position.DistanceTo(new Point3D(zero, zero, zero)));
            foreach (var data in nodes)
            {
                var newPoint = data.Value.Position.ToVector3D() / maxPoint * NORM_FACTOR;
                data.Value.Position = newPoint.ToPoint3D();
            }

            // Returnig the result
            return nodes;
        }

        /// <summary>
        /// Return the Euclidean distance between 2 points
        /// </summary>
        private double Euclidean(Point3D a, Point3D b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2.0f) + Math.Pow(a.Y - b.Y, 2.0f) + Math.Pow(a.Z - b.Z, 2.0f));
        }


        /// <summary>
        /// Step 3. Vizualizing the graph (points and lines)
        /// </summary>
        private async Task Vizualize()
        {
            // processed and visualized links
            var passed = new Dictionary<Tuple<string, string>, bool>();

            // Clear ViewPort Children and Add the Sun 
            await ViewPort.Dispatcher.InvokeAsync(() => { ViewPort.Children.Clear(); });
            await AddChild(new SunLight());

            // Vizualizing the graph (points and lines)
            foreach (var node in graph)
            {
                // Add point
                var sphere = new SphereVisual3D();
                sphere.Radius = EDGE_FACTOR;
                sphere.Material = new DiffuseMaterial(EDGE_COLOR);
                sphere.Center = points[node.Key].Position;
                await AddChild(sphere);

                // Add point text
                var cpt = sphere.Center;
                var offset = 20;
                cpt.Offset(offset, offset, offset);
                var pointText = new TextVisual3D()
                {
                    Text = node.Key,
                    FontSize = NORM_FACTOR / 4,
                    FontWeight = FontWeights.ExtraBold,
                    Background = Brushes.Transparent,
                    Foreground = EDGE_COLOR,
                    Position = cpt
                };
                await AddChild(pointText);

                // Avarage the weights of the edges
                foreach (var connection in node.Value)
                {
                    var edgeTo = points[node.Key].Connected.FirstOrDefault(n => n.From == points[node.Key]);
                    if (edgeTo != null)
                    {
                        var edgeFrom = points[edgeTo.To.Name].Connected.FirstOrDefault(n => n.To.Name == node.Key);
                        if (edgeFrom != null)
                        {
                            double avg = (edgeTo.Value + edgeFrom.Value) / 2;
                            edgeTo.Value = avg;
                            edgeFrom.Value = avg;
                        }
                    }
                }

                // Vizualizing the links
                foreach (var connection in node.Value)
                {
                    // show only not visualized links
                    if (passed.Keys.Any(k => k.Item1 == node.Key && k.Item2 == connection.Item1) ||
                        passed.Keys.Any(k => k.Item2 == node.Key && k.Item1 == connection.Item1)) continue;
                    passed.Add(new Tuple<string, string>(node.Key, connection.Item1), true);

                    // Add link
                    var link = new LinesVisual3D();
                    link.Color = (VERTEX_COLOR as SolidColorBrush).Color;
                    link.Points.Add(points[node.Key].Position);
                    link.Points.Add(points[connection.Item1].Position);
                    await AddChild(link);
                    points[node.Key].Connected.Remove(points[node.Key].Connected.FirstOrDefault(n => n.To == points[connection.Item1]));
                    points[node.Key].Connected.Add(new VisualEdge()
                    {
                        Visual = link,
                        From = points[node.Key],
                        To = points[connection.Item1],
                        Value = connection.Item2
                    });

                    // Add link text
                    var linkText = new TextVisual3D()
                    {
                        Text = connection.Item2.ToString(),
                        FontSize = NORM_FACTOR / 4,
                        FontWeight = FontWeights.ExtraBold,
                        Background = Brushes.Transparent,
                        Foreground = VERTEX_COLOR,
                    };
                    var textpos = (points[node.Key].Position.ToVector3D() + points[connection.Item1].Position.ToVector3D()) / 2;
                    linkText.Position = textpos.ToPoint3D();
                    await AddChild(linkText);
                }

            }

            // NEW: Automatically fits into viewport frame
            await ViewPort.Dispatcher.InvokeAsync(() => { ViewPort.ZoomExtents(); ; });        
        }

        // Run Vizualizing the graph 
        private async Task RunVizualize()
        {
            await ViewPort.Dispatcher.InvokeAsync(async () =>
            {
                await Vizualize();  
                graph.Clear();
                // points.Clear();
            });
        }

        // Add Children to ViewPort
        private async Task AddChild(Visual3D text)
        {
            await Application.Current.Dispatcher.InvokeAsync(() => { ViewPort.Children.Add(text); });
        }

        /// <summary>
        /// Connect Button Click.
        /// </summary>
        private void Connect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow();
            window.onPortSelected += onPortSelected;
            window.Show();
        }

        /// <summary>
        /// Scrypt Button Click.
        /// </summary>
        private void Scrypt_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ScryptWindow window = new ScryptWindow();
            window._port = this._port;
            window.Show();
        }

        /// <summary>
        /// About Button Click.
        /// </summary>
        private void About_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MessageBox.Show("This product provides visual representation of the data received by HX7 range devices. Developed in Burgas Free University by Dimitar Minchev, PhD of Informatics", "HX Render", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Exit Button Click.
        /// </summary>
        private void Exit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Exit", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes) Close();
        }
    }
}
