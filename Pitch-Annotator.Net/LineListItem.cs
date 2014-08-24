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
        /// <summary>
        /// This is set to true, when the corresponding LineListItem is selected (so that when the item is selected, mouse_leave event be disabled)
        /// </summary>
        public bool IsItemSelected;

        /// <summary>
        /// This is a reference to the corresponding line item
        /// </summary>
        public Line LineReference;

        public LineListItem(Line lineref)
        {
            this.LineReference = lineref;

            this.MouseEnter += LineListItem_MouseEnter;
            this.MouseLeave += LineListItem_MouseLeave;


            this.IsItemSelected = false;
        }

        /// <summary>
        /// Event raised when mouse enteres this element
        /// </summary>
        void LineListItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if(!this.IsItemSelected)
            {
                this.LineReference.Stroke = Brushes.Red;
            }
        }
        /// <summary>
        /// Event raised when mouse leaves this element
        /// </summary>
        void LineListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            this.LineReference.Stroke = Brushes.Orange;
        }
    }
}
