﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace PitchAnnotator
{
    /// <summary>
    /// This class contains information about each image entry found in the images folder and their corresponding annotation files found in the annotations folder
    /// This class also can return proper view object for to be shown in the ListViewer
    /// </summary>
    class ImageEntry: IComparable
    {
        /// <summary>
        /// Absolute path to the image file
        /// </summary>
        public string ImageAddress;

        /// <summary>
        /// Name of the image file, without extension and address
        /// </summary>
        public string ImageName;

        /// <summary>
        /// A boolean showing whether image file has annotation file already or not
        /// </summary>
        public bool HasAnnotation;

        /// <summary>
        /// Absolute path to the annotation file
        /// </summary>
        public string AnnotationAddress;

        /// <summary>
        /// Constructor for the image entry object. It is assumed that the provided address indeed points to an image file
        /// </summary>
        public ImageEntry(string address, string annotationPath)
        {
            this.ImageAddress = address;
            this.ImageName = Path.GetFileNameWithoutExtension(address);
            this.AnnotationAddress = Path.Combine(annotationPath, ImageName + ".json");
            this.HasAnnotation = File.Exists(this.AnnotationAddress);
        }

        public Grid GetListViewItem()
        {
            Grid g = new Grid();
            g.Background = HasAnnotation ? Brushes.LightGreen : Brushes.PaleVioletRed;
            g.Children.Add(new Label() { Content = ImageName });
            return g;
        }

        /// <summary>
        /// IComparable interface is implemented such that files without annotation are put on the top of the list so the chances of them getting annotated would be higher.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            ImageEntry other = obj as ImageEntry;

            if (other == null)
                throw new ArgumentException("Object isn't of type ImageEntry");
            else
            {
                if (this.HasAnnotation == other.HasAnnotation)
                    return this.ImageName.CompareTo(other.ImageName);
                else if (this.HasAnnotation)
                    return 1;
                else
                    return -1;
            }

        }
    }
}
