using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using System.Windows.Controls;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using System.IO;

namespace JsonPlayground
{
    class myLine
    {
        public float X1;
        public float X2;
        public float Y1;
        public float Y2;
        public myLine(float x1, float x2, float y1, float y2)
        {
            this.X1 = x1;
            this.X2 = x2;
            this.Y1 = y1;
            this.Y2 = y2;
        }
    }

    public class myLineJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //List<Line> ll = new List<Line>
            //{
            //                    new Line() { X1 = 12.5, Y1 = 13.4, X2 = 2.5, Y2 = 5.6 },
            //                    new Line() { X1 = 12.25, Y1 = 183.4, X2 = 24.5, Y2 = 54.6 }
            //};
            //Console.WriteLine(JsonConvert.SerializeObject(ll, Formatting.Indented));


            List<myLine> fl = new List<myLine>
            {
                new myLine(23,24,45,21),
                new myLine(23,44,745,23),
                new myLine(12,345,56,54)
            };

            Console.WriteLine(JsonConvert.SerializeObject(fl, Formatting.Indented));
            Console.WriteLine(JsonConvert.SerializeObject(new myLine(23, 434, 12, 54), Formatting.Indented));

            List<myLine> flo = JsonConvert.DeserializeObject<List<myLine>>(@"[{'X1': 383, 'X2': 574, 'Y1': 176, 'Y2': 236},
{
    'X1': 23.0,
    'X2': 24.0,
    'Y1': 45.0,
    'Y2': 21.0
  }]");
            Console.WriteLine(flo);

            JsonSchema schema = new JsonSchema();
            schema.Type = JsonSchemaType.Array;
            schema.Properties = new Dictionary<string, JsonSchema>
            {
                {"X1", new JsonSchema() {Type = JsonSchemaType.Float}},
                {"Y1", new JsonSchema() {Type = JsonSchemaType.Float}},
                {"X2", new JsonSchema() {Type = JsonSchemaType.Float}},
                {"Y2", new JsonSchema() {Type = JsonSchemaType.Float}}
            };
            Console.WriteLine(schema.ToString());

            JObject lobj;
            using (StreamReader file = File.OpenText(@"E:\Code Vault\Github\Pitch-Annotator.Net\dataset\groundTruth\test\1.json"))
            {
                lobj = JObject.Parse(file.ReadToEnd());
                bool valid = lobj.IsValid(schema);
                Console.WriteLine(valid);
            }
        }
    }
}
