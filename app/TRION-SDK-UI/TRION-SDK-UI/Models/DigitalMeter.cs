using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class DigitalMeter
{
    public double Value { get; set; }
    private readonly Dictionary<string, double> Data = [];
    public string Unit => "";

    public void AddSample(double sample)
    {
        Value = sample;
    }

}
