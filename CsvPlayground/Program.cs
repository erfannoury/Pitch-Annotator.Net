using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
namespace CsvPlayground
{
    class Program
    {

        /// <summary>
        /// This class is used for mapping Line objects to proper csv output and also for converting back csv records to Line objects
        /// </summary>
        public sealed class LineMapping : CsvClassMap<Line>
        {
            public LineMapping()
            {
                Map(m => Line.X1Property).Index(0).Name("X1");
                Map(m => Line.X2Property).Index(1).Name("X2");
                Map(m => Line.Y1Property).Index(2).Name("Y1");
                Map(m => Line.Y2Property).Index(3).Name("Y2");
            }
        }
        [STAThread]
        static void Main(string[] args)
        {
            Line l1 = new Line() { X1 = 23.4, X2 = 243.23, Y1 = 23.4, Y2 = 2.00 };
            //StreamReader textreader = File.OpenText(@"E:\Code Vault\Github\Pitch-Annotator.Net\dataset\groundTruth\test\1.csv");
            //TextReader textreader = new StreamReader(@"E:\Code Vault\Github\Pitch-Annotator.Net\dataset\groundTruth\test\1.csv");
            
            
            //CsvReader csvr = new CsvReader(textreader);
            //var lines = new List<Line>();
            //while(csvr.Read())
            //{
                //lines.Add(new Line() { X1 = csvr.GetField<float>(0), X2 = csvr.GetField<float>(1), Y1 = csvr.GetField<float>(2), Y2 = csvr.GetField<float>(3) });
                //lines.Add(csvr.GetRecord<Line>());
            //}
            using (var textwriter = new StreamWriter(@"E:\Code Vault\Github\Pitch-Annotator.Net\dataset\groundTruth\test\out.csv"))
            {
                CsvWriter csvw = new CsvWriter(textwriter, new CsvConfiguration() { HasHeaderRecord = false });
                csvw.WriteField<double>(l1.X1);
                csvw.WriteField<double>(l1.X2);
                csvw.WriteField<double>(l1.Y1);
                csvw.WriteField<double>(l1.Y2);
                csvw.NextRecord();
            }
        }
    }
}
