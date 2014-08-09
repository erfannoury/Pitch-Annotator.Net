using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;

namespace PitchAnnotator
{
    /// <summary>
    /// Class for creating appropriate ListItems for the ListView showing line layers for each image
    /// It has the full functionality of the Label class, but also contains a reference to a Line object
    /// </summary>
    class LineListItem : Label
    {
        public Line LineReference;
    }
}
